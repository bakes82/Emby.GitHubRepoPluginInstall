using System.ComponentModel;
using Emby.GitHubRepoPluginInstall.Models;
using Emby.Web.GenericEdit;
using MediaBrowser.Model.Attributes;

namespace Emby.GitHubRepoPluginInstall.UI;

public class RegistryConfigUi : EditableOptionsBase
{
    public override string EditorTitle => "Manage Plugin Registry";

    public override string EditorDescription => "Add a plugin registry source to discover available plugins.";

    [DisplayName("Registry Name")]
    [Description("A friendly name for this registry")]
    [Required]
    public string Name { get; set; }

    [DisplayName("Registry URL")]
    [Description("Raw URL to the plugins.json file (e.g., https://raw.githubusercontent.com/user/repo/main/plugins.json)")]
    [Required]
    public string RawUrl { get; set; }

    [DisplayName("Enabled")]
    [Description("Enable or disable this registry")]
    public bool Enabled { get; set; } = true;

    [Browsable(false)]
    [DontSave]
    public string Id { get; set; }
}