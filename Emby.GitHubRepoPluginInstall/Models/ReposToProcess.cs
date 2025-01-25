using System;
using System.ComponentModel;

namespace Emby.GitHubRepoPluginInstall.Models;

public class ReposToProcess
{
    public ReposToProcess()
    {
        Id = Guid.NewGuid()
                 .ToString("N");
    }

    public string Id            { get; set; }
    public string Url           { get; set; }
    public bool   GetPreRelease { get; set; }
    public bool   AutoUpdate    { get; set; } = true;

    [ReadOnly(true)]
    public DateTime? LastDateTimeChecked { get; set; }

    public string LastVersionDownloaded { get; set; }

    private (string Owner, string Repository) RepositoryInfo
    {
        get
        {
            if (string.IsNullOrEmpty(Url)) return (string.Empty, string.Empty);

            try
            {
                // Split the URL by '/' and get owner and repo parts
                var parts = Url.TrimEnd('/')
                               .Split('/');
                if (parts.Length >= 5)
                    return (parts[3], parts[4]
                        .Split('/')[0]);
            }
            catch
            {
                // Return empty if parsing fails
                return (string.Empty, string.Empty);
            }

            return (string.Empty, string.Empty);
        }
    }

    public string Owner      => RepositoryInfo.Owner;
    public string Repository => RepositoryInfo.Repository;
}