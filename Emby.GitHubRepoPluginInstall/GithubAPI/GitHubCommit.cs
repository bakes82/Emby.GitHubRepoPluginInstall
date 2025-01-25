using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Emby.GitHubRepoPluginInstall.GithubAPI;

public class GitHubCommit
{
    [DataMember(Name = "sha")]
    public string Sha { get; set; }

    [DataMember(Name = "node_id")]
    public string NodeId { get; set; }

    [DataMember(Name = "commit")]
    public GitHubCommitDetails GitHubCommitDetails { get; set; }

    [DataMember(Name = "url")]
    public string Url { get; set; }

    [DataMember(Name = "html_url")]
    public string HtmlUrl { get; set; }

    [DataMember(Name = "comments_url")]
    public string CommentsUrl { get; set; }

    [DataMember(Name = "author")]
    public GitHubUser Author { get; set; }

    [DataMember(Name = "committer")]
    public GitHubUser Committer { get; set; }

    [DataMember(Name = "parents")]
    public List<GitHubCommitParent> Parents { get; set; }

    [DataMember(Name = "stats")]
    public GitHubCommitStats Stats { get; set; }

    [DataMember(Name = "files")]
    public List<GitHubCommitFile> Files { get; set; }
}