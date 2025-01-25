using System.Runtime.Serialization;

namespace Emby.GitHubRepoPluginInstall.GithubAPI;

public class GitHubCommitTree
{
    [DataMember(Name = "sha")]
    public string Sha { get; set; }

    [DataMember(Name = "url")]
    public string Url { get; set; }
}