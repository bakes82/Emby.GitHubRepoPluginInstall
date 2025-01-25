using Emby.GitHubRepoPluginInstall.Models;
using Emby.GitHubRepoPluginInstall.UIBaseClasses.Store;
using MediaBrowser.Common;
using MediaBrowser.Model.Logging;

namespace Emby.GitHubRepoPluginInstall.Storage;

public class PluginOptionsStore : SimpleFileStore<PluginUIOptions>
{
    public PluginOptionsStore(IApplicationHost applicationHost, ILogger logger, string pluginFullName) :
        base(applicationHost, logger, pluginFullName)
    {
    }
}