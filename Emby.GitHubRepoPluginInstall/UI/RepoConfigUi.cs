using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Emby.GitHubRepoPluginInstall.Models;
using Emby.Web.GenericEdit;
using Emby.Web.GenericEdit.Common;
using Emby.Web.GenericEdit.Elements;
using MediaBrowser.Model.Attributes;

namespace Emby.GitHubRepoPluginInstall.UI;

public class RepoConfigUi : EditableOptionsBase
{
    public override string EditorTitle => "Manage GitHub Repositories";

    public override string EditorDescription => "Configure the repositories to download plugins from.";

    [Browsable(false)]
    public List<PluginRegistryEntry> AvailablePlugins { get; set; } = new List<PluginRegistryEntry>();

    [Browsable(false)]
    public IEnumerable<EditorSelectOption> PluginList
    {
        get
        {
            var options = new List<EditorSelectOption>();
            
            // Add empty option
            options.Add(new EditorSelectOption 
            { 
                Name = "-- Manual Entry --", 
                Value = "",
                ShortName = "Manual",
                IsEnabled = true
            });
            
            // Add plugins grouped by registry
            foreach (var plugin in AvailablePlugins.OrderBy(p => p.RegistrySource).ThenBy(p => p.Name))
            {
                options.Add(new EditorSelectOption
                {
                    Name = $"[{plugin.RegistrySource}] {plugin.Name}",
                    Value = plugin.Url,
                    ShortName = plugin.Name,
                    ToolTip = plugin.Description ?? "No description available",
                    IsEnabled = true
                });
            }
            
            return options;
        }
    }

    [DisplayName("Select from Registry")]
    [Description("Choose a plugin from available registries or enter URL manually below")]
    [SelectItemsSource(nameof(PluginList))]
    [VisibleCondition(nameof(PluginList), SimpleCondition.IsNotNullOrEmpty)]
    [AutoPostBack("PluginSelected", nameof(SelectedPlugin))]
    public string SelectedPlugin { get; set; }

    public SpacerItem Spacer1 { get; set; } = new SpacerItem();

    [DisplayName("GitHub Repo URL")]
    [Description("Enter the URL for the GitHub repository (or select from dropdown above)")]
    [Required]
    public string Url { get; set; }

    [DisplayName("Allow PreRelease Versions")]
    public bool AllowPreReleaseVersions { get; set; }

    [DisplayName("Auto Update")]
    public bool AutoUpdate { get; set; } = true;

    [Browsable(false)]
    [DontSave]
    public string Id { get; set; }
}