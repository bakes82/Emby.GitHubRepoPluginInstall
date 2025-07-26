using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Emby.GitHubRepoPluginInstall.GithubAPI;
using Emby.GitHubRepoPluginInstall.Models;
using Emby.GitHubRepoPluginInstall.Storage;
using Emby.GitHubRepoPluginInstall.UIBaseClasses.Views;
using Emby.Media.Common.Extensions;
using Emby.Web.GenericEdit.Elements;
using Emby.Web.GenericEdit.Elements.List;
using MediaBrowser.Common;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Plugins.UI.Views;
using MediaBrowser.Model.Serialization;

namespace Emby.GitHubRepoPluginInstall.UI;

internal class MainPageView : PluginPageView
{
    private readonly ILogger                  _logger;
    private readonly SecurePluginOptionsStore _store;
    private readonly IApplicationHost   _appHost;
    private readonly IJsonSerializer    _jsonSerializer;
    private readonly PluginInfo         _pluginInfo;

    public MainPageView(PluginInfo pluginInfo,
                        SecurePluginOptionsStore store,
                        ILogger logger,
                        IApplicationHost appHost,
                        IJsonSerializer jsonSerializer) : base(pluginInfo.Id)
    {
        _pluginInfo     = pluginInfo;
        _store          = store;
        _logger         = logger;
        _appHost        = appHost;
        _jsonSerializer = jsonSerializer;
        ShowSave        = false;
        var data = store.GetOptions();

        ContentData = data;

        try
        {
            CreateReleaseListAsync().Wait();
        }
        catch (Exception ex)
        {
            _logger.ErrorException("Error in CreateReleaseListAsync", ex);
        }
    }

    public PluginUIOptions PluginUiOptions => ContentData as PluginUIOptions;

    public override Task<IPluginUIView> OnSaveCommand(string itemId, string commandId, string data)
    {
        _store.SetOptions(PluginUiOptions);
        return base.OnSaveCommand(itemId, commandId, data);
    }

    public override bool IsCommandAllowed(string commandKey)
    {
        _logger.Info($"Command Key is {commandKey}");
        if (commandKey == "Add"       ||
            commandKey == "Remove"    ||
            commandKey == "Edit"      ||
            commandKey == "Save"      ||
            commandKey == "Download"  ||
            commandKey == "UpdateAll" ||
            commandKey == "CheckAll")
            return true;

        return base.IsCommandAllowed(commandKey);
    }

    public override async Task<IPluginUIView> RunCommand(string itemId, string commandId, string data)
    {
        if (commandId == "Download")
        {
            var item = PluginUiOptions.Repos.Find(x => x.Id == itemId);
            using var gitHubClient = new GitHubApiClient(PluginUiOptions.GitHubToken, _jsonSerializer, _logger);
            var releases = await gitHubClient.GetLatestReleasesAsync(item);
            var applicationPaths = _appHost.Resolve<IApplicationPaths>();
            var fileName = await gitHubClient.DownloadReleaseAsync(releases.First(), applicationPaths.PluginsPath);
            item.LastVersionDownloaded = releases.First().TagName;
            item.LastDateTimeChecked = DateTime.UtcNow;
            item.FileName = fileName;

            _store.SetOptions(PluginUiOptions);
            _appHost.NotifyPendingRestart();

            return await Task.FromResult((IPluginUIView)this);
        }

        if (commandId == "Save")
        {
            _store.SetOptions(PluginUiOptions);
            return await Task.FromResult((IPluginUIView)this);
        }

        if (commandId == "Add")
        {
            var newView = new RepoConfigDialogView(_pluginInfo.Id);
            return await Task.FromResult<IPluginUIView>(newView);
        }

        if (commandId == "Edit")
        {
            try
            {
                var item = PluginUiOptions.Repos.Find(x => x.Id.ToString() == PluginUiOptions.SelectedItemId.First());
                var editView = new RepoConfigDialogView(_pluginInfo.Id, item);
                return await Task.FromResult<IPluginUIView>(editView);
            }
            catch (Exception exception)
            {
                //ignore exception
            }

            return await Task.FromResult((IPluginUIView)this);
        }

        if (commandId == "Remove")
        {
            try
            {
                var item = PluginUiOptions.Repos.Find(x => x.Id.ToString() == PluginUiOptions.SelectedItemId.First());

                if (item != null)
                {
                    var applicationPaths = _appHost.Resolve<IApplicationPaths>();
                    var fullPath         = Path.Combine(applicationPaths.PluginsPath, item.FileName);
                    if (File.Exists(fullPath)) File.Delete(fullPath);
                    PluginUiOptions.Repos.Remove(item);
                    _appHost.NotifyPendingRestart();
                }
            }
            catch (Exception exception)
            {
                //ignore exception
            }

            if (PluginUiOptions.Repos == null || PluginUiOptions.Repos.Count == 0)
                PluginUiOptions.Repos = new List<ReposToProcess>();
            _store.SetOptions(PluginUiOptions);

            try
            {
                CreateReleaseListAsync().Wait();
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error in CreateReleaseListAsync after removal", ex);
            }

            return await Task.FromResult((IPluginUIView)this);
        }

        if (commandId == "UpdateAll")
        {
            try
            {
                var updatedCount = 0;
                using var gitHubClient = new GitHubApiClient(PluginUiOptions.GitHubToken, _jsonSerializer, _logger);
                var applicationPaths = _appHost.Resolve<IApplicationPaths>();

                foreach (var repo in PluginUiOptions.Repos.ToList())
                {
                    try
                    {
                        var release = await gitHubClient.GetLatestReleaseAsync(repo, true);
                        if (release != null && !release.TagName.Equals(repo.LastVersionDownloaded, StringComparison.OrdinalIgnoreCase))
                        {
                            // Check if release has DLL assets before trying to download
                            if (release.Assets?.Any(x => x.IsDll) == true)
                            {
                                var dllAsset = release.Assets.FirstOrDefault(x => x.IsDll);
                                _logger.Debug($"Attempting to download {repo.Repository} v{release.TagName} - DLL asset: {dllAsset?.Name}, URL: {dllAsset?.BrowserDownloadUrl}");
                                
                                try
                                {
                                    var fileName = await gitHubClient.DownloadReleaseAsync(release, applicationPaths.PluginsPath);
                                    repo.LastVersionDownloaded = release.TagName;
                                    repo.LastDateTimeChecked = DateTime.UtcNow;
                                    repo.FileName = fileName;
                                    updatedCount++;
                                }
                                catch (Exception downloadEx)
                                {
                                    _logger.Error($"Failed to download {repo.Repository} v{release.TagName}: {downloadEx.Message}");
                                }
                            }
                            else
                            {
                                _logger.Warn($"No DLL assets found for {repo.Repository} release {release.TagName}");
                            }
                        }
                        else if (release != null)
                        {
                            repo.LastDateTimeChecked = DateTime.UtcNow;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Failed to update {repo.Repository}: {ex.Message}");
                    }
                }

                _store.SetOptions(PluginUiOptions);
                
                if (updatedCount > 0)
                {
                    _appHost.NotifyPendingRestart();
                    PluginUiOptions.Logs = new CaptionItem($"Successfully updated {updatedCount} plugin(s). Server restart required.") { IsVisible = true };
                }
                else
                {
                    PluginUiOptions.Logs = new CaptionItem("All plugins are already up to date.") { IsVisible = true };
                }
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error during bulk update", ex);
                PluginUiOptions.Logs = new CaptionItem($"Error during bulk update: {ex.Message}") { IsVisible = true };
            }

            CreateReleaseListAsync().Wait();
            return await Task.FromResult((IPluginUIView)this);
        }

        if (commandId == "CheckAll")
        {
            CreateReleaseListAsync().Wait();
            return await Task.FromResult((IPluginUIView)this);
        }

        return await base.RunCommand(itemId, commandId, data);
    }

    public override async void OnDialogResult(IPluginUIView dialogView, bool completedOk, object data)
    {
        try
        {
            _logger.Info($"{completedOk} -- {dialogView.GetType()}");
            if (dialogView is RepoConfigDialogView dialog && completedOk)
            {
                // Helper function to check if URL already exists (excluding the current item being edited)
                bool IsUrlDuplicate(string url, string currentId)
                {
                    return PluginUiOptions.Repos?.Any(x => x.Url.Equals(url, StringComparison.OrdinalIgnoreCase) &&
                                                           x.Id != currentId) ??
                           false;
                }

                if (!dialog.RepoConfigUi.Id.IsNullOrEmpty())
                {
                    // Editing existing entry
                    var item = PluginUiOptions.Repos.Find(x => x.Id == dialog.RepoConfigUi.Id);
                    if (item != null)
                    {
                        if (IsUrlDuplicate(dialog.RepoConfigUi.Url, item.Id))
                            throw new
                                InvalidOperationException($"Repository URL '{dialog.RepoConfigUi.Url}' already exists in the list.");

                        item.Url           = dialog.RepoConfigUi.Url;
                        item.GetPreRelease = dialog.RepoConfigUi.AllowPreReleaseVersions;
                        item.AutoUpdate    = dialog.RepoConfigUi.AutoUpdate;
                    }
                }
                else
                {
                    if (IsUrlDuplicate(dialog.RepoConfigUi.Url, null))
                        throw new
                            InvalidOperationException($"Repository URL '{dialog.RepoConfigUi.Url}' already exists in the list.");

                    var newEntry = new ReposToProcess
                                   {
                                       Url           = dialog.RepoConfigUi.Url,
                                       GetPreRelease = dialog.RepoConfigUi.AllowPreReleaseVersions,
                                       AutoUpdate    = dialog.RepoConfigUi.AutoUpdate
                                   };

                    if (PluginUiOptions.Repos == null)
                        PluginUiOptions.Repos = new List<ReposToProcess>
                                                {
                                                    newEntry
                                                };
                    else
                        PluginUiOptions.Repos.Add(newEntry);
                }

                _store.SetOptions(PluginUiOptions);

                await CreateReleaseListAsync();

                RaiseUIViewInfoChanged();
            }

            base.OnDialogResult(dialogView, completedOk, data);
        }
        catch (Exception e)
        {
            _logger.ErrorException(e.Message, e);
        }
    }

    private async Task CreateReleaseListAsync()
    {
        PluginUiOptions.Logs = new CaptionItem(""){IsVisible = false};
        PluginUiOptions.Releases = new GenericItemList();

        if (!PluginUiOptions.GitHubToken.IsNullOrEmpty())
        {
            using var gitHubClient = new GitHubApiClient(PluginUiOptions.GitHubToken, _jsonSerializer, _logger);
            foreach (var repo in PluginUiOptions.Repos)
            {
                try
                {
                    var release = await gitHubClient.GetLatestReleaseAsync(repo);

                    if (release == null)
                    {
                        _logger.Warn($"No release found for repository {repo.Owner}/{repo.Repository}");
                        continue;
                    }

                    _logger.Info(_jsonSerializer.SerializeToString(release, new JsonSerializerOptions
                                                                            {
                                                                                Indent = true
                                                                            }));

                    var note = release.Body?.Replace("\n", "<br/>") ??
                               release.GitHubCommit?.GitHubCommitDetails?.Message?.Replace("\n", "<br/>") ??
                               "No release notes available";

                    var itemToAdd = new GenericListItem
                                    {
                                        PrimaryText = "Repo: " + repo.Repository,
                                        SecondaryText = "Version: "         +
                                                        release.TagName     +
                                                        Environment.NewLine +
                                                        "PreRelease: "      +
                                                        release.PreRelease,
                                        Icon     = IconNames.download,
                                        IconMode = ItemListIconMode.LargeRegular,
                                        Button1 = new ButtonItem
                                                  {
                                                      Caption = "Download",
                                                      Data1   = "Download",
                                                      Data2   = repo.Id
                                                  },
                                        SubItems = new GenericItemList
                                                   {
                                                       new GenericListItem
                                                       {
                                                           PrimaryText = "Release Notes:<br/>" + note,
                                                           SecondaryText = "Updated At: " +
                                                                           (release.Assets?.FirstOrDefault()?.UpdatedAt.ToString() ?? 
                                                                            release.PublishedAt.ToString()),
                                                           Icon     = IconNames.message,
                                                           IconMode = ItemListIconMode.LargeRegular
                                                       }
                                                   }
                                    };

                    PluginUiOptions.Releases.Add(itemToAdd);
                }catch (Exception e)
                {
                    PluginUiOptions.Logs = new CaptionItem("Error: " + e.Message){IsVisible = true};
                    _logger.ErrorException(e.Message, e);
                }
            }
        }
    }

    private async Task<IPluginUIView> HandleUpdateAllCommand()
    {
        try
        {
            var updatedCount = 0;
            using var gitHubClient = new GitHubApiClient(PluginUiOptions.GitHubToken, _jsonSerializer, _logger);
            var applicationPaths = _appHost.Resolve<IApplicationPaths>();

            foreach (var repo in PluginUiOptions.Repos.ToList())
            {
                try
                {
                    var release = await gitHubClient.GetLatestReleaseAsync(repo);
                    if (release != null && !release.TagName.Equals(repo.LastVersionDownloaded, StringComparison.OrdinalIgnoreCase))
                    {
                        var fileName = await gitHubClient.DownloadReleaseAsync(release, applicationPaths.PluginsPath);
                        repo.LastVersionDownloaded = release.TagName;
                        repo.LastDateTimeChecked = DateTime.UtcNow;
                        repo.FileName = fileName;
                        updatedCount++;
                    }
                    else
                    {
                        repo.LastDateTimeChecked = DateTime.UtcNow;
                    }
                }
                catch (UnauthorizedAccessException ex)
                {
                    _logger.Error($"Authentication failed for {repo.Repository}: {ex.Message}");
                }
                catch (Exception ex)
                {
                    _logger.Error($"Failed to update {repo.Repository}: {ex.Message}");
                }
            }

            _store.SetOptions(PluginUiOptions);
            
            if (updatedCount > 0)
            {
                _appHost.NotifyPendingRestart();
                PluginUiOptions.Logs = new CaptionItem($"Successfully updated {updatedCount} plugin(s). Server restart required.") { IsVisible = true };
            }
            else
            {
                PluginUiOptions.Logs = new CaptionItem("All plugins are already up to date.") { IsVisible = true };
            }

            CreateReleaseListAsync().Wait();
        }
        catch (Exception ex)
        {
            _logger.ErrorException("Error during bulk update", ex);
            PluginUiOptions.Logs = new CaptionItem($"Error during bulk update: {ex.Message}") { IsVisible = true };
        }

        return await Task.FromResult((IPluginUIView)this);
    }

}