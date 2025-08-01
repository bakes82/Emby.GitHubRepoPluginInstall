using System.Threading.Tasks;
using Emby.GitHubRepoPluginInstall.Models;
using Emby.GitHubRepoPluginInstall.UIBaseClasses.Views;
using MediaBrowser.Model.Plugins.UI.Views;

namespace Emby.GitHubRepoPluginInstall.UI;

internal class RegistryConfigDialogView : PluginDialogView
{
    public RegistryConfigDialogView(string pluginId, PluginRegistry savedRegistry = null) : base(pluginId)
    {
        var uiVars = new RegistryConfigUi();

        if (savedRegistry != null)
        {
            uiVars.Id = savedRegistry.Id;
            uiVars.Name = savedRegistry.Name;
            uiVars.RawUrl = savedRegistry.RawUrl;
            uiVars.Enabled = savedRegistry.Enabled;
        }

        ContentData = uiVars;

        PluginId = pluginId;
        AllowCancel = true;
        AllowOk = true;
    }

    public RegistryConfigUi RegistryConfigUi => ContentData as RegistryConfigUi;

    public override Task Cancel()
    {
        return Task.CompletedTask;
    }

    public override Task OnOkCommand(string providerId, string commandId, string data)
    {
        return Task.CompletedTask;
    }

    public override Task<IPluginUIView> RunCommand(string itemId, string commandId, string data)
    {
        return base.RunCommand(itemId, commandId, data);
    }
}