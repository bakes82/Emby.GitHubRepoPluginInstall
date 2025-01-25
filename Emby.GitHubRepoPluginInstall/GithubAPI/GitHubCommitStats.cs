using System.Runtime.Serialization;

namespace Emby.GitHubRepoPluginInstall.GithubAPI;

public class GitHubCommitStats
{
    [DataMember(Name = "total")]
    public int Total { get; set; }

    [DataMember(Name = "additions")]
    public int Additions { get; set; }

    [DataMember(Name = "deletions")]
    public int Deletions { get; set; }
}