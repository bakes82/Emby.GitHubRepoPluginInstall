using System.Runtime.Serialization;

namespace Emby.GitHubRepoPluginInstall.GithubAPI;

public class GitHubCommitParent
{
    [DataMember(Name = "sha")]
    public string Sha { get; set; }

    [DataMember(Name = "url")]
    public string Url { get; set; }

    [DataMember(Name = "html_url")]
    public string HtmlUrl { get; set; }
}