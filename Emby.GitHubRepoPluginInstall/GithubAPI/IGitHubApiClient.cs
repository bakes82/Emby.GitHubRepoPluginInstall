using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Emby.GitHubRepoPluginInstall.Models;

namespace Emby.GitHubRepoPluginInstall.GithubAPI;

public interface IGitHubApiClient
{
    Task<List<GitHubRelease>> GetLatestReleasesAsync(ReposToProcess repo, CancellationToken cancellationToken = default);
    Task<GitHubRelease> GetLatestReleaseAsync(ReposToProcess repo, CancellationToken cancellationToken = default);
    Task<List<GitHubRelease>> GetLatestReleasesAsync(List<ReposToProcess> repositories, CancellationToken cancellationToken = default);
    Task<GitHubCommit> GetCommitDetailsAsync(GitHubRelease release, CancellationToken cancellationToken = default);
    Task<string> DownloadReleaseAsync(GitHubRelease release, string destinationPath, IProgress<double> progress = null, CancellationToken cancellationToken = default);
    Task<bool> ValidateRepositoryAsync(string owner, string repository, CancellationToken cancellationToken = default);
}