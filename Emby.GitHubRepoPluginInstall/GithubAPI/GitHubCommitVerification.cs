using System.Runtime.Serialization;

namespace Emby.GitHubRepoPluginInstall.GithubAPI;

public class GitHubCommitVerification
{
    [DataMember(Name = "verified")]
    public bool Verified { get; set; }

    [DataMember(Name = "reason")]
    public string Reason { get; set; }

    [DataMember(Name = "signature")]
    public string Signature { get; set; }

    [DataMember(Name = "payload")]
    public string Payload { get; set; }
}