using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;

namespace Emby.GitHubRepoPluginInstall.Services;

public class PluginBackupService : IPluginBackupService
{
    private readonly IFileSystem _fileSystem;
    private readonly ILogger _logger;
    private readonly string _backupPath;

    public PluginBackupService(IApplicationHost applicationHost, ILogger logger)
    {
        _fileSystem = applicationHost.Resolve<IFileSystem>();
        _logger = logger;
        
        var applicationPaths = applicationHost.Resolve<IApplicationPaths>();
        _backupPath = Path.Combine(applicationPaths.PluginsPath, "plugin-backups");
        
        if (!_fileSystem.DirectoryExists(_backupPath))
            _fileSystem.CreateDirectory(_backupPath);
    }

    public async Task<string> CreateBackupAsync(string pluginPath, string version, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(pluginPath))
            throw new FileNotFoundException($"Plugin file not found: {pluginPath}");

        var pluginName = Path.GetFileNameWithoutExtension(pluginPath);
        var pluginBackupDir = Path.Combine(_backupPath, pluginName);
        
        if (!_fileSystem.DirectoryExists(pluginBackupDir))
            _fileSystem.CreateDirectory(pluginBackupDir);

        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        var backupFileName = $"{pluginName}_v{version}_{timestamp}.dll";
        var backupFilePath = Path.Combine(pluginBackupDir, backupFileName);

        try
        {
            using (var sourceStream = new FileStream(pluginPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var backupStream = new FileStream(backupFilePath, FileMode.Create, FileAccess.Write))
            {
                await sourceStream.CopyToAsync(backupStream, cancellationToken).ConfigureAwait(false);
            }

            _logger.Info($"Created backup: {backupFilePath}");
            return backupFilePath;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to create backup for {pluginPath}: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> RestoreBackupAsync(string pluginName, string version, CancellationToken cancellationToken = default)
    {
        var backups = await GetBackupsAsync(pluginName, cancellationToken).ConfigureAwait(false);
        var backup = backups.FirstOrDefault(b => b.Version == version);
        
        if (backup == null)
        {
            _logger.Warn($"No backup found for {pluginName} version {version}");
            return false;
        }

        if (!File.Exists(backup.BackupPath))
        {
            _logger.Error($"Backup file does not exist: {backup.BackupPath}");
            return false;
        }

        try
        {
            // Determine the target plugin path (assuming plugins directory structure)
            var pluginsDir = Path.GetDirectoryName(_backupPath.Replace("plugin-backups", "plugins"));
            var targetPath = Path.Combine(pluginsDir, $"{pluginName}.dll");

            using (var backupStream = new FileStream(backup.BackupPath, FileMode.Open, FileAccess.Read))
            using (var targetStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write))
            {
                await backupStream.CopyToAsync(targetStream, cancellationToken).ConfigureAwait(false);
            }

            _logger.Info($"Restored backup {backup.BackupPath} to {targetPath}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to restore backup for {pluginName}: {ex.Message}");
            return false;
        }
    }

    public async Task<List<PluginBackup>> GetBackupsAsync(string pluginName, CancellationToken cancellationToken = default)
    {
        var pluginBackupDir = Path.Combine(_backupPath, pluginName);
        
        if (!_fileSystem.DirectoryExists(pluginBackupDir))
            return new List<PluginBackup>();

        var backups = new List<PluginBackup>();

        await Task.Run(() =>
        {
            var files = Directory.GetFiles(pluginBackupDir, "*.dll");
            
            foreach (var file in files)
            {
                try
                {
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    var parts = fileName.Split('_');
                    
                    if (parts.Length >= 3)
                    {
                        var version = parts[1].Replace("v", "");
                        var fileInfo = new FileInfo(file);
                        
                        backups.Add(new PluginBackup
                        {
                            PluginName = pluginName,
                            Version = version,
                            BackupPath = file,
                            CreatedDate = fileInfo.CreationTimeUtc,
                            FileSize = fileInfo.Length
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warn($"Error parsing backup file {file}: {ex.Message}");
                }
            }
        }, cancellationToken).ConfigureAwait(false);

        return backups.OrderByDescending(b => b.CreatedDate).ToList();
    }

    public async Task<bool> DeleteBackupAsync(string pluginName, string version, CancellationToken cancellationToken = default)
    {
        var backups = await GetBackupsAsync(pluginName, cancellationToken).ConfigureAwait(false);
        var backup = backups.FirstOrDefault(b => b.Version == version);
        
        if (backup == null)
            return false;

        try
        {
            if (File.Exists(backup.BackupPath))
            {
                File.Delete(backup.BackupPath);
                _logger.Info($"Deleted backup: {backup.BackupPath}");
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to delete backup {backup.BackupPath}: {ex.Message}");
        }

        return false;
    }

    public async Task CleanupOldBackupsAsync(string pluginName, int maxBackups = 5, CancellationToken cancellationToken = default)
    {
        var backups = await GetBackupsAsync(pluginName, cancellationToken).ConfigureAwait(false);
        
        if (backups.Count <= maxBackups)
            return;

        var backupsToDelete = backups.Skip(maxBackups).ToList();
        
        foreach (var backup in backupsToDelete)
        {
            await DeleteBackupAsync(pluginName, backup.Version, cancellationToken).ConfigureAwait(false);
        }

        _logger.Info($"Cleaned up {backupsToDelete.Count} old backups for {pluginName}");
    }
}