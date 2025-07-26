using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Emby.GitHubRepoPluginInstall.GithubAPI;
using Emby.GitHubRepoPluginInstall.Models;
using Emby.GitHubRepoPluginInstall.Storage;
using MediaBrowser.Common;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Activity;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Tasks;

namespace Emby.GitHubRepoPluginInstall.ScheduledTasks;

public class UpdatePlugins : IScheduledTask, IConfigurableScheduledTask
{
    private readonly IActivityManager _activityManager;
    private readonly IApplicationHost _applicationHost;
    private readonly ILogger          _logger;
    private readonly IUserManager     _userManager;
    private readonly IJsonSerializer  _jsonSerializer;

    public UpdatePlugins(ILogManager logManager,
                         IApplicationHost applicationHost,
                         IUserManager userManager,
                         IActivityManager activityManager,
                         IJsonSerializer jsonSerializer)
    {
        _logger          = logManager.GetLogger("UpdatePluginsScheduledTask");
        _applicationHost = applicationHost;
        _userManager     = userManager;
        _activityManager = activityManager;
        _jsonSerializer  = jsonSerializer;
    }

    public bool IsHidden  => false;
    public bool IsEnabled => true;
    public bool IsLogged  => true;

    public async Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
    {
        var adminUser        = GetAdminUser();
        var store            = new SecurePluginOptionsStore(_applicationHost, _logger, GitHubRepPluginInstall.PluginName);
        var applicationPaths = _applicationHost.Resolve<IApplicationPaths>();

        var pluginUiOptions = store.GetOptions();

        var totalCollections = pluginUiOptions.Repos.Count;
        var processedRepos   = 0;
        var downloads        = 0;

        using var gitHubClient = new GitHubApiClient(pluginUiOptions.GitHubToken, _jsonSerializer, _logger);

        foreach (var repo in pluginUiOptions.Repos.Where(x => x.AutoUpdate))
        {
            try
            {
                var release = await gitHubClient.GetLatestReleaseAsync(repo, cancellationToken).ConfigureAwait(false);
                if (release == null)
                {
                    _activityManager.Create(new ActivityLogEntry
                                            {
                                                Name          = $"Release for {repo.Repository} NOT Updated",
                                                Overview      = Helpers.ActivityLogHelper.CreateWarningHtml(
                                                    $"No Release Found for {repo.Repository}",
                                                    "The repository does not have any releases available.",
                                                    $"Repository: {repo.Owner}/{repo.Repository}\nURL: {repo.Url}\nPre-release enabled: {repo.GetPreRelease}"),
                                                ShortOverview = null,
                                                Type          = "GithubRepoPluginUpdateFailed",
                                                ItemId        = null,
                                                Date          = DateTimeOffset.Now,
                                                UserId        = adminUser?.InternalId.ToString(),
                                                Severity      = LogSeverity.Warn
                                            });
                    continue;
                }

                if (!release.TagName.Equals(repo.LastVersionDownloaded, StringComparison.OrdinalIgnoreCase))
                {
                    var fileName = await gitHubClient.DownloadReleaseAsync(release, applicationPaths.PluginsPath, null, cancellationToken).ConfigureAwait(false);
                    downloads++;
                    repo.LastVersionDownloaded = release.TagName;
                    repo.FileName              = fileName;

                    _activityManager.Create(new ActivityLogEntry
                                            {
                                                Name = $"Plugin {repo.Repository} updated to {repo.LastVersionDownloaded}",
                                                Overview = Helpers.ActivityLogHelper.CreateSuccessHtml(
                                                    $"Plugin {repo.Repository} Successfully Updated",
                                                    "Plugin has been downloaded and installed.",
                                                    repo.LastVersionDownloaded,
                                                    release.Body ?? release.GitHubCommit?.GitHubCommitDetails?.Message ?? "No release notes available."),
                                                ShortOverview = null,
                                                Type          = "PluginInstalled",
                                                ItemId        = null,
                                                Date          = DateTimeOffset.Now,
                                                //UserId        = adminUser?.InternalId.ToString(),
                                                Severity = LogSeverity.Info
                                            });
                }
                else
                {
                    _activityManager.Create(new ActivityLogEntry
                                            {
                                                Name          = $"Release for {repo.Repository} Already Up to Date",
                                                Overview      = Helpers.ActivityLogHelper.CreateInfoHtml(
                                                    $"Plugin {repo.Repository} is Up to Date",
                                                    "No update required - you have the latest version.",
                                                    $"Current Version: {repo.LastVersionDownloaded}\nRepository: {repo.Owner}/{repo.Repository}"),
                                                ShortOverview = null,
                                                Type          = "GithubRepoPluginUpdateFailed",
                                                ItemId        = null,
                                                Date          = DateTimeOffset.Now,
                                                UserId        = adminUser?.InternalId.ToString(),
                                                Severity      = LogSeverity.Info
                                            });
                }

                repo.LastDateTimeChecked = DateTime.UtcNow;
            }
            catch (UnauthorizedAccessException ex)
            {
                _activityManager.Create(new ActivityLogEntry
                                        {
                                            Name          = $"Authentication Failed for {repo.Repository}",
                                            Overview      = Helpers.ActivityLogHelper.CreateErrorHtml(
                                                $"Authentication Failed for {repo.Repository}",
                                                "GitHub API returned 401 Unauthorized. Please check your Personal Access Token.",
                                                ex.Message,
                                                null),
                                            ShortOverview = null,
                                            Type          = "GithubRepoPluginUpdateFailed",
                                            ItemId        = null,
                                            Date          = DateTimeOffset.Now,
                                            UserId        = adminUser?.InternalId.ToString(),
                                            Severity      = LogSeverity.Error
                                        });
            }
            catch (Exception ex)
            {
                _activityManager.Create(new ActivityLogEntry
                                        {
                                            Name          = $"Release {repo.Repository} NOT Updated",
                                            Overview      = Helpers.ActivityLogHelper.CreateErrorHtml(
                                                $"Failed to Update Plugin {repo.Repository}",
                                                "An error occurred while trying to update the plugin.",
                                                ex.Message,
                                                ex.StackTrace),
                                            ShortOverview = null,
                                            Type          = "GithubRepoPluginUpdateFailed",
                                            ItemId        = null,
                                            Date          = DateTimeOffset.Now,
                                            UserId        = adminUser?.InternalId.ToString(),
                                            Severity      = LogSeverity.Fatal
                                        });
            }

            store.SetOptions(pluginUiOptions);
            processedRepos++;
            var progressPercentage = (double)processedRepos / totalCollections * 100;
            progress.Report(progressPercentage);

            cancellationToken.ThrowIfCancellationRequested();
        }

        if (downloads > 0 && pluginUiOptions.RestartServerAfterInstall)
            _applicationHost.Restart();
        else if (downloads > 0) _applicationHost.NotifyPendingRestart();
    }

    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        return new[]
               {
                   new TaskTriggerInfo
                   {
                       Type = TaskTriggerInfo.TriggerInterval,
                       IntervalTicks = TimeSpan.FromHours(24)
                                               .Ticks
                   }
               };
    }

    public string Name        { get; } = "Update Plugins From Github Repos";
    public string Key         { get; } = nameof(UpdatePlugins);
    public string Description { get; } = "Updates plugins that are marked auto update.";
    public string Category    { get; } = "Github Repo Plugins Update";

    private User GetAdminUser()
    {
        return _userManager.GetUsers(new UserQuery
                                     {
                                         IsAdministrator = true
                                     })
                           .Items.FirstOrDefault();
    }
}