using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Emby.GitHubRepoPluginInstall.Models;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;

namespace Emby.GitHubRepoPluginInstall.GithubAPI;

public class GitHubApiClient : IDisposable, IGitHubApiClient
{
    private readonly HttpClient      _client;
    private readonly IJsonSerializer _jsonSerializer;
    private readonly ILogger         _logger;
    private readonly bool            _disposeClient;
    
    // Simple in-memory cache
    private static readonly Dictionary<string, CacheItem> _cache = new Dictionary<string, CacheItem>();
    private static readonly object _cacheLock = new object();
    
    private class CacheItem
    {
        public object Data { get; set; }
        public DateTime ExpiryTime { get; set; }
        
        public bool IsExpired => DateTime.UtcNow > ExpiryTime;
    }

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
        {
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", githubToken);
            _logger.Debug($"GitHub token configured (length: {githubToken.Length})");
        }
        else
        {
            _logger.Warn("No GitHub token provided - API requests may be rate limited or fail");
        }
    }

    public async Task<List<GitHubRelease>> GetLatestReleasesAsync(ReposToProcess _repo, CancellationToken cancellationToken = default)
    {
        return await GetLatestReleasesAsync(new List<ReposToProcess>
                                            {
                                                _repo
                                            }, cancellationToken);
    }

    public async Task<GitHubRelease> GetLatestReleaseAsync(ReposToProcess _repo, CancellationToken cancellationToken = default)
    {
        var release = await GetLatestReleasesAsync(new List<ReposToProcess>
                                                   {
                                                       _repo
                                                   }, cancellationToken);
        return release.OrderByDescending(x => x.PublishedAt)
                      .FirstOrDefault();
    }

    public async Task<GitHubRelease> GetLatestReleaseAsync(ReposToProcess _repo, bool bypassCache, CancellationToken cancellationToken = default)
    {
        if (!bypassCache)
            return await GetLatestReleaseAsync(_repo, cancellationToken);
            
        return await GetLatestReleaseForRepoAsync(_repo, cancellationToken, true);
    }

    public async Task<List<GitHubRelease>> GetLatestReleasesAsync(List<ReposToProcess> _repositories, CancellationToken cancellationToken = default)
    {
        var tasks = _repositories.Select(repo => GetLatestReleaseForRepoAsync(repo, cancellationToken)).ToArray();
        var results = await Task.WhenAll(tasks);
        
        return results.Where(r => r != null).ToList();
    }

    private async Task<GitHubRelease> GetLatestReleaseForRepoAsync(ReposToProcess repo, CancellationToken cancellationToken = default, bool bypassCache = false)
    {
        var cacheKey = $"releases_{repo.Owner}_{repo.Repository}_{repo.GetPreRelease}";
        
        // Check cache first (unless bypassed)
        if (!bypassCache && TryGetFromCache<GitHubRelease>(cacheKey, out var cachedRelease))
        {
            _logger.Debug($"Returning cached release for {repo.Owner}/{repo.Repository}");
            return cachedRelease;
        }

        try
        {
            var url = $"https://api.github.com/repos/{repo.Owner}/{repo.Repository}/releases";
            var response = await ExecuteWithRetryAsync(() => _client.GetAsync(url, cancellationToken)).ConfigureAwait(false);
            
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new UnauthorizedAccessException($"GitHub API returned 401 Unauthorized for {repo.Owner}/{repo.Repository}. Please check your GitHub token is valid and has the necessary permissions.");
            }
            
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.Warn($"Repository {repo.Owner}/{repo.Repository} not found (404). Possible causes: " +
                           "1) Repository doesn't exist, " +
                           "2) Repository is private and your token lacks 'repo' scope, " +
                           "3) Repository name/owner is incorrect.");
                return null;
            }
            
            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                _logger.Error($"Access forbidden (403) for {repo.Owner}/{repo.Repository}. " +
                            "This usually means your token lacks the required 'repo' scope for private repositories.");
                return null;
            }
            
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
                    latestRelease.GitHubCommit = await GetCommitDetailsAsync(latestRelease, cancellationToken).ConfigureAwait(false);
                    
                    // Cache the result for 15 minutes
                    AddToCache(cacheKey, latestRelease, TimeSpan.FromMinutes(15));
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

    public async Task<GitHubCommit> GetCommitDetailsAsync(GitHubRelease release, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(release.TargetCommitish))
            throw new InvalidOperationException("No commit hash available for this release.");

        if (string.IsNullOrEmpty(release.BaseRepoUrl))
            throw new InvalidOperationException("Unable to determine repository information from release URL.");

        var cacheKey = $"commit_{release.BaseRepoUrl}_{release.TargetCommitish}";
        
        if (TryGetFromCache<GitHubCommit>(cacheKey, out var cachedCommit))
        {
            return cachedCommit;
        }

        try
        {
            var url = $"{release.BaseRepoUrl}/commits/{release.TargetCommitish}";
            var response = await ExecuteWithRetryAsync(() => _client.GetAsync(url, cancellationToken)).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            using (var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
            {
                var commit = _jsonSerializer.DeserializeFromStream<GitHubCommit>(stream);
                AddToCache(cacheKey, commit, TimeSpan.FromHours(1));
                return commit;
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Error fetching commit details: {ex.Message}", ex);
        }
    }

    public async Task<string> DownloadReleaseAsync(GitHubRelease release, string destinationPath, IProgress<double> progress = null, CancellationToken cancellationToken = default)
    {
        var dllAsset = release.Assets?.FirstOrDefault(x => x.IsDll);
        if (dllAsset == null)
        {
            _logger.Error($"No DLL asset found for release {release.Url}");
            throw new InvalidOperationException("No DLL asset found for this release.");
        }

        // For private repos, use the API URL instead of browser download URL
        var downloadUrl = !string.IsNullOrEmpty(_client.DefaultRequestHeaders.Authorization?.Parameter) 
            ? dllAsset.Url  // Use API URL for authenticated requests
            : dllAsset.BrowserDownloadUrl; // Use browser URL for public repos
            
        if (string.IsNullOrEmpty(downloadUrl))
        {
            _logger.Error($"No download URL available for release {release.Url} that has a DLL asset.");
            throw new InvalidOperationException("No download URL available for this release that has a DLL asset.");
        }
        
        _logger.Debug($"Using download URL: {downloadUrl}");

        var fileName = downloadUrl == dllAsset.Url ? dllAsset.Name : Path.GetFileName(downloadUrl);
        var fullPath = Path.Combine(destinationPath, fileName);
        var tempPath = Path.Combine(destinationPath, $"{fileName}.temp");

        try
        {
            // For API URLs, we need to accept octet-stream
            var request = new HttpRequestMessage(HttpMethod.Get, downloadUrl);
            if (downloadUrl == dllAsset.Url)
            {
                request.Headers.Accept.Clear();
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));
            }
            
            var response = await ExecuteWithRetryAsync(() => _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)).ConfigureAwait(false);
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

    public async Task<bool> ValidateRepositoryAsync(string owner, string repository, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"https://api.github.com/repos/{owner}/{repository}";
            var response = await ExecuteWithRetryAsync(() => _client.GetAsync(url, cancellationToken)).ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    // Basic retry logic without external dependencies
    private async Task<HttpResponseMessage> ExecuteWithRetryAsync(Func<Task<HttpResponseMessage>> operation, int maxRetries = 3)
    {
        Exception lastException = null;
        
        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                var response = await operation().ConfigureAwait(false);
                
                // Check if we should retry based on status code
                if (ShouldRetry(response.StatusCode) && attempt < maxRetries)
                {
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt)); // Exponential backoff
                    _logger.Warn($"Request failed with {response.StatusCode}, retrying in {delay.TotalSeconds} seconds (attempt {attempt + 1}/{maxRetries + 1})");
                    await Task.Delay(delay).ConfigureAwait(false);
                    continue;
                }
                
                return response;
            }
            catch (Exception ex) when (ShouldRetryException(ex) && attempt < maxRetries)
            {
                lastException = ex;
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt)); // Exponential backoff
                _logger.Warn($"Request failed with {ex.GetType().Name}: {ex.Message}, retrying in {delay.TotalSeconds} seconds (attempt {attempt + 1}/{maxRetries + 1})");
                await Task.Delay(delay).ConfigureAwait(false);
            }
        }
        
        throw lastException ?? new Exception("Max retries exceeded");
    }

    private static bool ShouldRetry(HttpStatusCode statusCode)
    {
        return statusCode == HttpStatusCode.RequestTimeout ||
               statusCode == HttpStatusCode.InternalServerError ||
               statusCode == HttpStatusCode.BadGateway ||
               statusCode == HttpStatusCode.ServiceUnavailable ||
               statusCode == HttpStatusCode.GatewayTimeout ||
               statusCode == HttpStatusCode.TooManyRequests;
    }

    private static bool ShouldRetryException(Exception ex)
    {
        return ex is HttpRequestException ||
               ex is TaskCanceledException ||
               ex is TimeoutException;
    }

    // Simple cache methods
    private bool TryGetFromCache<T>(string key, out T value)
    {
        lock (_cacheLock)
        {
            if (_cache.TryGetValue(key, out var cacheItem) && !cacheItem.IsExpired)
            {
                value = (T)cacheItem.Data;
                return true;
            }
            
            // Remove expired item
            if (_cache.ContainsKey(key))
            {
                _cache.Remove(key);
            }
            
            value = default(T);
            return false;
        }
    }

    private void AddToCache<T>(string key, T value, TimeSpan expiry)
    {
        lock (_cacheLock)
        {
            _cache[key] = new CacheItem
            {
                Data = value,
                ExpiryTime = DateTime.UtcNow.Add(expiry)
            };
            
            // Simple cleanup - remove expired items if cache gets too large
            if (_cache.Count > 100)
            {
                var expiredKeys = _cache.Where(kvp => kvp.Value.IsExpired).Select(kvp => kvp.Key).ToList();
                foreach (var expiredKey in expiredKeys)
                {
                    _cache.Remove(expiredKey);
                }
            }
        }
    }

    public async Task<List<PluginRegistryEntry>> GetAllRegistryPluginsAsync(List<PluginRegistry> registries, CancellationToken cancellationToken = default)
    {
        var allPlugins = new List<PluginRegistryEntry>();
        
        foreach (var registry in registries.Where(r => r.Enabled))
        {
            try
            {
                PluginRegistryData registryData = null;
                
                // Handle embedded registry
                if (registry.RawUrl == "embedded://default")
                {
                    var assembly = typeof(GitHubApiClient).Assembly;
                    var resourceName = assembly.GetManifestResourceNames()
                        .FirstOrDefault(n => n.EndsWith("DefaultRegistry.plugins.json"));

                    if (resourceName != null)
                    {
                        using (var stream = assembly.GetManifestResourceStream(resourceName))
                        {
                            registryData = _jsonSerializer.DeserializeFromStream<PluginRegistryData>(stream);
                        }
                    }
                }
                else
                {
                    // Handle regular URL registries
                    var response = await _client.GetAsync(registry.RawUrl, cancellationToken).ConfigureAwait(false);
                    if (response.IsSuccessStatusCode)
                    {
                        using (var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                        {
                            registryData = _jsonSerializer.DeserializeFromStream<PluginRegistryData>(stream);
                        }
                    }
                }
                
                if (registryData?.Plugins != null)
                {
                    foreach (var plugin in registryData.Plugins)
                    {
                        plugin.RegistrySource = registry.Name;
                        
                        // Check for duplicates by URL (case-insensitive)
                        var existingPlugin = allPlugins.FirstOrDefault(p => 
                            string.Equals(p.Url, plugin.Url, StringComparison.OrdinalIgnoreCase));
                        
                        if (existingPlugin == null)
                        {
                            allPlugins.Add(plugin);
                        }
                        else
                        {
                            _logger.Debug($"Skipping duplicate plugin URL: {plugin.Url} from {registry.Name} (already exists from {existingPlugin.RegistrySource})");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to fetch registry {registry.Name} from {registry.RawUrl}: {ex.Message}");
            }
        }
        
        return allPlugins;
    }

    public void Dispose()
    {
        if (_disposeClient)
            _client?.Dispose();
    }
}