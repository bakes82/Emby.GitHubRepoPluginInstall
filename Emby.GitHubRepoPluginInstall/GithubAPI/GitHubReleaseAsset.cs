using System;
using System.Runtime.Serialization;

namespace Emby.GitHubRepoPluginInstall.GithubAPI;

public class GitHubReleaseAsset
{
    [DataMember(Name = "url")]
    public string Url { get; set; }

    [DataMember(Name = "id")]
    public long Id { get; set; }

    [DataMember(Name = "node_id")]
    public string NodeId { get; set; }

    [DataMember(Name = "name")]
    public string Name { get; set; }

    [DataMember(Name = "label")]
    public string Label { get; set; }

    [DataMember(Name = "uploader")]
    public GitHubUser Uploader { get; set; }

    [DataMember(Name = "content_type")]
    public string ContentType { get; set; }

    [DataMember(Name = "state")]
    public string State { get; set; }

    [DataMember(Name = "size")]
    public long Size { get; set; }

    [DataMember(Name = "download_count")]
    public int DownloadCount { get; set; }

    [DataMember(Name = "created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [DataMember(Name = "updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }

    [DataMember(Name = "browser_download_url")]
    public string BrowserDownloadUrl { get; set; }

    public bool IsDll => Name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase);
}