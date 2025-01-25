using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Plugins.UI;
using MediaBrowser.Model.Plugins.UI.Views;

namespace Emby.GitHubRepoPluginInstall.UIBaseClasses;

public abstract class ControllerBase : IPluginUIPageController
{
    /// <summary>Initializes a new instance of the <see cref="ControllerBase" /> class.</summary>
    /// <param name="pluginId">The plugin identifier.</param>
    protected ControllerBase(string pluginId)
    {
        PluginId = pluginId;
    }

    public string PluginId { get; }

    public abstract PluginPageInfo PageInfo { get; }

    public virtual Task Initialize(CancellationToken token)
    {
        return Task.CompletedTask;
    }

    public abstract Task<IPluginUIView> CreateDefaultPageView();
}