using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Emby.GitHubRepoPluginInstall.GithubAPI;

public class GitHubRelease
{
    [DataMember(Name = "url")]
    public string Url { get; set; }

    public string BaseRepoUrl { get; set; }

    [DataMember(Name = "assets_url")]
    public string AssetsUrl { get; set; }

    [DataMember(Name = "upload_url")]
    public string UploadUrl { get; set; }

    [DataMember(Name = "html_url")]
    public string HtmlUrl { get; set; }

    [DataMember(Name = "id")]
    public long Id { get; set; }

    [DataMember(Name = "author")]
    public GitHubUser Author { get; set; }

    [DataMember(Name = "node_id")]
    public string NodeId { get; set; }

    [DataMember(Name = "tag_name")]
    public string TagName { get; set; }

    [DataMember(Name = "target_commitish")]
    public string TargetCommitish { get; set; }

    [DataMember(Name = "name")]
    public string Name { get; set; }

    [DataMember(Name = "draft")]
    public bool Draft { get; set; }

    [DataMember(Name = "prerelease")]
    public bool PreRelease { get; set; }

    [DataMember(Name = "created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [DataMember(Name = "published_at")]
    public DateTimeOffset PublishedAt { get; set; }

    [DataMember(Name = "assets")]
    public List<GitHubReleaseAsset> Assets { get; set; }

    [DataMember(Name = "tarball_url")]
    public string TarballUrl { get; set; }

    [DataMember(Name = "zipball_url")]
    public string ZipballUrl { get; set; }

    [DataMember(Name = "body")]
    public string Body { get; set; }

    public GitHubCommit GitHubCommit { get; set; }
}