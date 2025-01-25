using System.ComponentModel;
using Emby.GitHubRepoPluginInstall.Models;
using Emby.Web.GenericEdit;
using MediaBrowser.Model.Attributes;

namespace Emby.GitHubRepoPluginInstall.UI;

public class RepoConfigUi : EditableOptionsBase
{
    public override string EditorTitle => "Manage GitHub Repositories";

    public override string EditorDescription => "Configure the repositories to download plugins from.";

    [DisplayName("GitHub Repo URL")]
    [Description("Enter the URL for the GitHub repository.")]
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