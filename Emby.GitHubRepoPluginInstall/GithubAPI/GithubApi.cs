using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Emby.GitHubRepoPluginInstall.Models;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;

namespace Emby.GitHubRepoPluginInstall.GithubAPI;

public class GitHubApiClient
{
    private readonly HttpClient      _client;
    private readonly IJsonSerializer _jsonSerializer;
    private readonly ILogger         _logger;

    public GitHubApiClient(string githubToken, HttpClient httpClient, IJsonSerializer jsonSerializer, ILogger logger)
    {
        _jsonSerializer = jsonSerializer;
        _logger         = logger;
        _client         = httpClient;

        _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("GitHubReleaseManager", "1.0"));

        if (!string.IsNullOrEmpty(githubToken))
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", githubToken);
    }

    public async Task<List<GitHubRelease>> GetLatestReleasesAsync(ReposToProcess _repo)
    {
        return await GetLatestReleasesAsync(new List<ReposToProcess>
                                            {
                                                _repo
                                            });
    }

    public async Task<GitHubRelease> GetLatestReleaseAsync(ReposToProcess _repo)
    {
        var release = await GetLatestReleasesAsync(new List<ReposToProcess>
                                                   {
                                                       _repo
                                                   });
        return release.OrderByDescending(x => x.PublishedAt)
                      .FirstOrDefault();
    }

    public async Task<List<GitHubRelease>> GetLatestReleasesAsync(List<ReposToProcess> _repositories)
    {
        var returnData = new List<GitHubRelease>();

        foreach (var repo in _repositories)
            try
            {
                var response =
                    await _client.GetAsync($"https://api.github.com/repos/{repo.Owner}/{repo.Repository}/releases");
                response.EnsureSuccessStatusCode();

                using (var stream = await response.Content.ReadAsStreamAsync())
                {
                    var gitHubReleases = _jsonSerializer.DeserializeFromStream<List<GitHubRelease>>(stream);
                    if (gitHubReleases != null)
                    {
                        if (repo.GetPreRelease == false)
                            gitHubReleases = gitHubReleases.Where(x => !x.PreRelease)
                                                           .ToList();

                        gitHubReleases = gitHubReleases.OrderByDescending(x => x.PublishedAt)
                                                       .Take(1)
                                                       .ToList();

                        foreach (var gitHubRelease in gitHubReleases)
                        {
                            gitHubRelease.BaseRepoUrl  = $"https://api.github.com/repos/{repo.Owner}/{repo.Repository}";
                            gitHubRelease.GitHubCommit = await GetCommitDetailsAsync(gitHubRelease);
                        }

                        returnData.AddRange(gitHubReleases);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error getting releases for {repo.Owner}/{repo.Repository}: {ex.Message}");
            }

        return returnData;
    }

    public async Task<GitHubCommit> GetCommitDetailsAsync(GitHubRelease release)
    {
        if (string.IsNullOrEmpty(release.TargetCommitish))
            throw new InvalidOperationException("No commit hash available for this release.");

        if (string.IsNullOrEmpty(release.BaseRepoUrl))
            throw new InvalidOperationException("Unable to determine repository information from release URL.");

        try
        {
            var response = await _client.GetAsync($"{release.BaseRepoUrl}/commits/{release.TargetCommitish}");
            response.EnsureSuccessStatusCode();

            using (var stream = await response.Content.ReadAsStreamAsync())
            {
                return _jsonSerializer.DeserializeFromStream<GitHubCommit>(stream);
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Error fetching commit details: {ex.Message}", ex);
        }
    }

    public async Task<string> DownloadReleaseAsync(GitHubRelease release, string destinationPath)
    {
        var downloadUrl = release.Assets.Where(x => x.IsDll)
                                 .OrderByDescending(x => x.UpdatedAt)
                                 .First()
                                 .BrowserDownloadUrl;
        if (string.IsNullOrEmpty(downloadUrl))
        {
            _logger.Error($"No download URL available for release {release.Url} that has a DLL asset.");
            throw new InvalidOperationException("No download URL available for this release that has a DLL asset.");
        }

        var fileName = Path.GetFileName(downloadUrl);
        var fullPath = Path.Combine(destinationPath, fileName);
        var tempPath = Path.Combine(destinationPath, $"{fileName}.temp");

        try
        {
            var response = await _client.GetAsync(downloadUrl);
            response.EnsureSuccessStatusCode();

            // First download to a temporary file
            using (var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await response.Content.CopyToAsync(fs);
            }

            // If download was successful, replace the existing file
            if (File.Exists(fullPath)) File.Delete(fullPath);
            File.Move(tempPath, fullPath);
        }
        catch (Exception ex)
        {
            // Clean up temp file if it exists
            if (File.Exists(tempPath))
                try
                {
                    File.Delete(tempPath);
                }
                catch
                {
                    // Ignore cleanup errors
                }

            throw new Exception($"Error downloading release: {ex.Message}", ex);
        }

        return fileName;
    }

    public void Dispose()
    {
        _client?.Dispose();
    }
}