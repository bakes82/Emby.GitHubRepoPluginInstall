using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Model.Logging;

namespace Emby.GitHubRepoPluginInstall.Security;

public class SecureStorage : ISecureStorage
{
    private readonly ILogger _logger;
    private readonly byte[] _key;

    public SecureStorage(ILogger logger)
    {
        _logger = logger;
        // Generate a consistent key based on machine name - basic obfuscation only
        var machineId = Environment.MachineName + "EmbyGitHubPlugin2024";
        using (var sha256 = SHA256.Create())
        {
            _key = sha256.ComputeHash(Encoding.UTF8.GetBytes(machineId));
        }
    }

    public string Protect(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return plainText;

        try
        {
            // Simple XOR-based obfuscation (not secure encryption, just basic hiding)
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var obfuscatedBytes = new byte[plainBytes.Length];
            
            for (int i = 0; i < plainBytes.Length; i++)
            {
                obfuscatedBytes[i] = (byte)(plainBytes[i] ^ _key[i % _key.Length]);
            }
            
            return Convert.ToBase64String(obfuscatedBytes);
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to protect data: {0}", ex.Message);
            // Fallback to base64 encoding if protection fails
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(plainText));
        }
    }

    public string Unprotect(string protectedText)
    {
        if (string.IsNullOrEmpty(protectedText))
        {
            _logger.Debug("Unprotect called with empty text");
            return protectedText;
        }

        try
        {
            var obfuscatedBytes = Convert.FromBase64String(protectedText);
            var plainBytes = new byte[obfuscatedBytes.Length];
            
            for (int i = 0; i < obfuscatedBytes.Length; i++)
            {
                plainBytes[i] = (byte)(obfuscatedBytes[i] ^ _key[i % _key.Length]);
            }
            
            var result = Encoding.UTF8.GetString(plainBytes);
            _logger.Debug($"Successfully unprotected data (length: {result.Length})");
            return result;
        }
        catch (FormatException ex)
        {
            _logger.Debug($"Failed to decode as obfuscated data: {ex.Message}, trying fallback");
            // Try to decode as plain base64 (fallback for non-protected data)
            try
            {
                var bytes = Convert.FromBase64String(protectedText);
                var result = Encoding.UTF8.GetString(bytes);
                // If it looks like a valid token, return it
                if (result.StartsWith("ghp_") || result.StartsWith("github_pat_"))
                {
                    _logger.Debug("Found plain base64 encoded token");
                    return result;
                }
            }
            catch
            {
                // Ignore
            }
            
            // If all else fails, return as-is (might be plain text from old version)
            _logger.Debug("Returning text as-is (plain text fallback)");
            return protectedText;
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to unprotect data: {0}", ex.Message);
            return protectedText;
        }
    }

    public Task<string> ProtectAsync(string plainText)
    {
        return Task.FromResult(Protect(plainText));
    }

    public Task<string> UnprotectAsync(string protectedText)
    {
        return Task.FromResult(Unprotect(protectedText));
    }
}