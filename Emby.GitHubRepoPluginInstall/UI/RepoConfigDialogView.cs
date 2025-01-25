using System.Threading.Tasks;
using Emby.GitHubRepoPluginInstall.Models;
using Emby.GitHubRepoPluginInstall.UIBaseClasses.Views;
using MediaBrowser.Model.Plugins.UI.Views;

namespace Emby.GitHubRepoPluginInstall.UI;

internal class RepoConfigDialogView : PluginDialogView
{
    public RepoConfigDialogView(string pluginId, ReposToProcess savedRepoConfig = null) : base(pluginId)
    {
        var uiVars = new RepoConfigUi();

        if (savedRepoConfig != null)
        {
            uiVars.Id                      = savedRepoConfig.Id;
            uiVars.Url                     = savedRepoConfig.Url;
            uiVars.AllowPreReleaseVersions = savedRepoConfig.GetPreRelease;
        }

        ContentData = uiVars;

        PluginId    = pluginId;
        AllowCancel = true;
        AllowOk     = true;
    }

    public RepoConfigUi RepoConfigUi => ContentData as RepoConfigUi;

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
        //if (1 == 1) return Task.FromResult((IPluginUIView)this);

        return base.RunCommand(itemId, commandId, data);
    }
}