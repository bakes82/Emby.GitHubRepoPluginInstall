using System.Runtime.Serialization;

namespace Emby.GitHubRepoPluginInstall.GithubAPI;

public class GitHubCommitFile
{
    [DataMember(Name = "sha")]
    public string Sha { get; set; }

    [DataMember(Name = "filename")]
    public string Filename { get; set; }

    [DataMember(Name = "status")]
    public string Status { get; set; }

    [DataMember(Name = "additions")]
    public int Additions { get; set; }

    [DataMember(Name = "deletions")]
    public int Deletions { get; set; }

    [DataMember(Name = "changes")]
    public int Changes { get; set; }

    [DataMember(Name = "blob_url")]
    public string BlobUrl { get; set; }

    [DataMember(Name = "raw_url")]
    public string RawUrl { get; set; }

    [DataMember(Name = "contents_url")]
    public string ContentsUrl { get; set; }

    [DataMember(Name = "patch")]
    public string Patch { get; set; }
}