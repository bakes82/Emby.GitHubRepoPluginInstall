using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Emby.GitHubRepoPluginInstall.Services;

public interface IPluginBackupService
{
    Task<string> CreateBackupAsync(string pluginPath, string version, CancellationToken cancellationToken = default);
    Task<bool> RestoreBackupAsync(string pluginName, string version, CancellationToken cancellationToken = default);
    Task<List<PluginBackup>> GetBackupsAsync(string pluginName, CancellationToken cancellationToken = default);
    Task<bool> DeleteBackupAsync(string pluginName, string version, CancellationToken cancellationToken = default);
    Task CleanupOldBackupsAsync(string pluginName, int maxBackups = 5, CancellationToken cancellationToken = default);
}

public class PluginBackup
{
    public string PluginName { get; set; }
    public string Version { get; set; }
    public string BackupPath { get; set; }
    public DateTime CreatedDate { get; set; }
    public long FileSize { get; set; }
}