using System;
using System.Runtime.Serialization;

namespace Emby.GitHubRepoPluginInstall.GithubAPI;

public class GitHubCommitAuthor
{
    [DataMember(Name = "name")]
    public string Name { get; set; }

    [DataMember(Name = "email")]
    public string Email { get; set; }

    [DataMember(Name = "date")]
    public DateTimeOffset Date { get; set; }
}