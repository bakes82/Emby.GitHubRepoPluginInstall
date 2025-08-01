using System.Collections.Generic;
using System.Threading.Tasks;
using Emby.GitHubRepoPluginInstall.Models;
using Emby.GitHubRepoPluginInstall.UIBaseClasses.Views;
using MediaBrowser.Model.Plugins.UI.Views;

namespace Emby.GitHubRepoPluginInstall.UI;

internal class RepoConfigDialogView : PluginDialogView
{
    public RepoConfigDialogView(string pluginId, ReposToProcess savedRepoConfig = null, List<PluginRegistryEntry> availablePlugins = null) : base(pluginId)
    {
        var uiVars = new RepoConfigUi();

        if (savedRepoConfig != null)
        {
            uiVars.Id                      = savedRepoConfig.Id;
            uiVars.Url                     = savedRepoConfig.Url;
            uiVars.AllowPreReleaseVersions = savedRepoConfig.GetPreRelease;
            uiVars.AutoUpdate              = savedRepoConfig.AutoUpdate;
        }

        if (availablePlugins != null)
        {
            uiVars.AvailablePlugins = availablePlugins;
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

    public override bool IsCommandAllowed(string commandKey)
    {
        if (commandKey == "PluginSelected")
            return true;
            
        return base.IsCommandAllowed(commandKey);
    }

    public override Task<IPluginUIView> RunCommand(string itemId, string commandId, string data)
    {
        if (commandId == "PluginSelected")
        {
            // When dropdown selection changes, update the URL field
            if (!string.IsNullOrEmpty(RepoConfigUi.SelectedPlugin))
            {
                RepoConfigUi.Url = RepoConfigUi.SelectedPlugin;
                // Clear the dropdown selection to show "-- Manual Entry --" again
                RepoConfigUi.SelectedPlugin = "";
            }
            return Task.FromResult((IPluginUIView)this);
        }
        
        return base.RunCommand(itemId, commandId, data);
    }
    
    public override Task OnOkCommand(string providerId, string commandId, string data)
    {
        return Task.CompletedTask;
    }
}