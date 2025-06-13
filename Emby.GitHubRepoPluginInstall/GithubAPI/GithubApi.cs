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

public class GitHubApiClient : IDisposable
{
    private readonly HttpClient      _client;
    private readonly IJsonSerializer _jsonSerializer;
    private readonly ILogger         _logger;
    private readonly bool            _disposeClient;

    public GitHubApiClient(string githubToken, HttpClient httpClient, IJsonSerializer jsonSerializer, ILogger logger)
    {
        _jsonSerializer = jsonSerializer;
        _logger         = logger;
        _client         = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _disposeClient  = false;

        _client.DefaultRequestHeaders.Accept.Clear();
        _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _client.DefaultRequestHeaders.UserAgent.Clear();
        _client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("GitHubReleaseManager", "1.0"));

        if (!string.IsNullOrEmpty(githubToken))
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", githubToken);
    }

    public GitHubApiClient(string githubToken, IJsonSerializer jsonSerializer, ILogger logger)
    {
        _jsonSerializer = jsonSerializer;
        _logger         = logger;
        _client         = new HttpClient();
        _disposeClient  = true;

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
        var tasks = _repositories.Select(GetLatestReleaseForRepoAsync).ToArray();
        var results = await Task.WhenAll(tasks);
        
        return results.Where(r => r != null).ToList();
    }

    private async Task<GitHubRelease> GetLatestReleaseForRepoAsync(ReposToProcess repo)
    {
        try
        {
            var response = await _client.GetAsync($"https://api.github.com/repos/{repo.Owner}/{repo.Repository}/releases").ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            using (var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
            {
                var gitHubReleases = _jsonSerializer.DeserializeFromStream<List<GitHubRelease>>(stream);
                if (gitHubReleases?.Any() != true)
                    return null;

                var filteredReleases = repo.GetPreRelease 
                    ? gitHubReleases 
                    : gitHubReleases.Where(x => !x.PreRelease).ToList();

                var latestRelease = filteredReleases
                    .OrderByDescending(x => x.PublishedAt)
                    .FirstOrDefault();

                if (latestRelease != null)
                {
                    latestRelease.BaseRepoUrl = $"https://api.github.com/repos/{repo.Owner}/{repo.Repository}";
                    latestRelease.GitHubCommit = await GetCommitDetailsAsync(latestRelease).ConfigureAwait(false);
                }

                return latestRelease;
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting releases for {repo.Owner}/{repo.Repository}: {ex.Message}");
            return null;
        }
    }

    public async Task<GitHubCommit> GetCommitDetailsAsync(GitHubRelease release)
    {
        if (string.IsNullOrEmpty(release.TargetCommitish))
            throw new InvalidOperationException("No commit hash available for this release.");

        if (string.IsNullOrEmpty(release.BaseRepoUrl))
            throw new InvalidOperationException("Unable to determine repository information from release URL.");

        try
        {
            var response = await _client.GetAsync($"{release.BaseRepoUrl}/commits/{release.TargetCommitish}").ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            using (var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
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
        var dllAsset = release.Assets?.FirstOrDefault(x => x.IsDll);
        if (dllAsset == null)
        {
            _logger.Error($"No DLL asset found for release {release.Url}");
            throw new InvalidOperationException("No DLL asset found for this release.");
        }

        var downloadUrl = dllAsset.BrowserDownloadUrl;
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
            var response = await _client.GetAsync(downloadUrl).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            // First download to a temporary file
            using (var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await response.Content.CopyToAsync(fs).ConfigureAwait(false);
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
        if (_disposeClient)
            _client?.Dispose();
    }
}