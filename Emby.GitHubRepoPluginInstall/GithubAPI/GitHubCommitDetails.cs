using System.Runtime.Serialization;

namespace Emby.GitHubRepoPluginInstall.GithubAPI;

public class GitHubCommitDetails
{
    [DataMember(Name = "author")]
    public GitHubCommitAuthor Author { get; set; }

    [DataMember(Name = "committer")]
    public GitHubCommitAuthor Committer { get; set; }

    [DataMember(Name = "message")]
    public string Message { get; set; }

    [DataMember(Name = "tree")]
    public GitHubCommitTree Tree { get; set; }

    [DataMember(Name = "url")]
    public string Url { get; set; }

    [DataMember(Name = "comment_count")]
    public int CommentCount { get; set; }

    [DataMember(Name = "verification")]
    public GitHubCommitVerification Verification { get; set; }
}