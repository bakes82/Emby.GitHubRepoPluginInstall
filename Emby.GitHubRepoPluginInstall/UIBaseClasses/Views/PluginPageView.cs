using System.Threading.Tasks;
using MediaBrowser.Model.Plugins.UI.Views;

namespace Emby.GitHubRepoPluginInstall.UIBaseClasses.Views;

internal abstract class PluginPageView : PluginViewBase, IPluginPageView
{
    protected PluginPageView(string pluginId) : base(pluginId)
    {
        PluginId = pluginId;
    }

    public bool ShowSave { get; set; } = true;

    public bool ShowBack { get; set; } = false;

    public bool AllowSave { get; set; } = true;

    public bool AllowBack { get; set; } = true;

    public virtual Task<IPluginUIView> OnSaveCommand(string itemId, string commandId, string data)
    {
        return Task.FromResult((IPluginUIView)this);
    }
}