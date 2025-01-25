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
    private readonly ILogger            _logger;
    private readonly PluginOptionsStore _store;
    private readonly IApplicationHost   _appHost;
    private readonly IJsonSerializer    _jsonSerializer;
    private readonly PluginInfo         _pluginInfo;

    public MainPageView(PluginInfo pluginInfo,
                        PluginOptionsStore store,
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

        CreateReleaseListAsync()
            .Wait();
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
        if (commandKey == "Add"    ||
            commandKey == "Remove" ||
            commandKey == "Edit"   ||
            commandKey == "Save"   ||
            commandKey == "Download")
            return true;

        return base.IsCommandAllowed(commandKey);
    }

    public override async Task<IPluginUIView> RunCommand(string itemId, string commandId, string data)
    {
        if (commandId == "Download")
        {
            var item = PluginUiOptions.Repos.Find(x => x.Id == itemId);
            var gitHubClient =
                new GitHubApiClient(PluginUiOptions.GitHubToken, new HttpClient(), _jsonSerializer, _logger);
            var release = await gitHubClient.GetLatestReleasesAsync(item);
            var applicationPaths = _appHost.Resolve<IApplicationPaths>();
            var fileName = await gitHubClient.DownloadReleaseAsync(release.First(), applicationPaths.PluginsPath);
            item.LastVersionDownloaded = release.First()
                                                .TagName;
            item.LastDateTimeChecked = DateTime.UtcNow;
            item.FileName            = fileName;

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

            CreateReleaseListAsync()
                .Wait();

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
        PluginUiOptions.Releases = new GenericItemList();

        if (!PluginUiOptions.GitHubToken.IsNullOrEmpty())
        {
            var gitHubClient =
                new GitHubApiClient(PluginUiOptions.GitHubToken, new HttpClient(), _jsonSerializer, _logger);
            foreach (var repo in PluginUiOptions.Repos)
            {
                var release = await gitHubClient.GetLatestReleaseAsync(repo);

                _logger.Info(_jsonSerializer.SerializeToString(release, new JsonSerializerOptions
                                                                        {
                                                                            Indent = true
                                                                        }));

                var note = release.Body?.Replace("\n", "<br/>") ??
                           release.GitHubCommit.GitHubCommitDetails.Message.Replace("\n", "<br/>");

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
                                                                       release.Assets.First()
                                                                              .UpdatedAt.ToString(),
                                                       Icon     = IconNames.message,
                                                       IconMode = ItemListIconMode.LargeRegular
                                                   }
                                               }
                                };

                PluginUiOptions.Releases.Add(itemToAdd);
            }
        }
    }
}