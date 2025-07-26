using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Emby.GitHubRepoPluginInstall.Models;
using Emby.Web.GenericEdit;
using MediaBrowser.Common;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;

namespace Emby.GitHubRepoPluginInstall.UIBaseClasses.Store;

public class AsyncSimpleFileStore<TOptionType> : SimpleContentStore<TOptionType>, IAsyncStore<TOptionType>
    where TOptionType : EditableOptionsBase, new()
{
    private static readonly Dictionary<Type, PropertyInfo[]> _propertyCache = new Dictionary<Type, PropertyInfo[]>();
    private static readonly object _propertyCacheLock = new object();
    private readonly IFileSystem _fileSystem;
    private readonly IJsonSerializer _jsonSerializer;
    private readonly SemaphoreSlim _lockSemaphore = new SemaphoreSlim(1, 1);
    private readonly ILogger _logger;
    private readonly string _pluginConfigPath;
    private readonly string _pluginFullName;
    private long _lastFileModifiedTicks;

    private TOptionType _options;

    public AsyncSimpleFileStore(IApplicationHost applicationHost, ILogger logger, string pluginFullName)
    {
        _logger = logger;
        _pluginFullName = pluginFullName;
        _jsonSerializer = applicationHost.Resolve<IJsonSerializer>();
        _fileSystem = applicationHost.Resolve<IFileSystem>();

        var applicationPaths = applicationHost.Resolve<IApplicationPaths>();
        _pluginConfigPath = applicationPaths.PluginConfigurationsPath;

        if (!_fileSystem.DirectoryExists(_pluginConfigPath)) 
            _fileSystem.CreateDirectory(_pluginConfigPath);

        OptionsFileName = $"{pluginFullName}.json";
    }

    public virtual string OptionsFileName { get; }
    public string OptionsFilePath => Path.Combine(_pluginConfigPath, OptionsFileName);

    public event EventHandler<FileSavingEventArgs> FileSaving;
    public event EventHandler<FileSavedEventArgs> FileSaved;

    public override TOptionType GetOptions()
    {
        return GetOptionsAsync().GetAwaiter().GetResult();
    }

    public async Task<TOptionType> GetOptionsAsync(CancellationToken cancellationToken = default)
    {
        await _lockSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_options == null) 
                return await ReloadOptionsAsync(cancellationToken).ConfigureAwait(false);

            if (_fileSystem.FileExists(OptionsFilePath))
            {
                var lastModifiedTicks = _fileSystem.GetLastWriteTimeUtc(OptionsFilePath).UtcTicks;
                if (lastModifiedTicks > _lastFileModifiedTicks)
                {
                    return await ReloadOptionsAsync(cancellationToken).ConfigureAwait(false);
                }
            }

            return _options;
        }
        finally
        {
            _lockSemaphore.Release();
        }
    }

    public async Task<TOptionType> ReloadOptionsAsync(CancellationToken cancellationToken = default)
    {
        await _lockSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var tempOptions = _options ?? new TOptionType();

            try
            {
                if (!_fileSystem.FileExists(OptionsFilePath))
                {
                    _options = tempOptions;
                    _lastFileModifiedTicks = 0;
                    return tempOptions;
                }

                _lastFileModifiedTicks = _fileSystem.GetLastWriteTimeUtc(OptionsFilePath).UtcTicks;

                using (var stream = _fileSystem.OpenRead(OptionsFilePath))
                {
                    var deserialized = await Task.Run(() => 
                        tempOptions.DeserializeFromJsonStream(stream, _jsonSerializer), cancellationToken)
                        .ConfigureAwait(false);

                    _options = deserialized as TOptionType;
                }
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error loading plugin options for {0} from {1}", ex, _pluginFullName, OptionsFilePath);
                _options = tempOptions;
            }

            return _options ?? new TOptionType();
        }
        finally
        {
            _lockSemaphore.Release();
        }
    }

    public TOptionType ReloadOptions()
    {
        return ReloadOptionsAsync().GetAwaiter().GetResult();
    }

    public override void SetOptions(TOptionType newOptions)
    {
        SetOptionsAsync(newOptions).GetAwaiter().GetResult();
    }

    public async Task SetOptionsAsync(TOptionType newOptions, CancellationToken cancellationToken = default)
    {
        if (newOptions == null) 
            throw new ArgumentNullException(nameof(newOptions));

        var savingArgs = new FileSavingEventArgs(newOptions);
        FileSaving?.Invoke(this, savingArgs);

        if (savingArgs.Cancel) 
            return;

        await _lockSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Create a dictionary to store only properties that are not marked with DontSave
            var filteredOptions = new Dictionary<string, object>();
            var properties = GetCachedProperties(typeof(TOptionType));

            foreach (var property in properties)
            {
                if (property.GetCustomAttributes(typeof(DontSaveAttribute), false).Any())
                    continue;

                if (property.CanRead)
                {
                    var value = property.GetValue(newOptions);
                    filteredOptions[property.Name] = value;
                }
            }

            using (var stream = _fileSystem.GetFileStream(OptionsFilePath, FileOpenMode.Create, FileAccessMode.Write))
            {
                // Serialize the filtered dictionary instead of the full object
                await Task.Run(() => _jsonSerializer.SerializeToStream(filteredOptions, stream, new JsonSerializerOptions
                {
                    Indent = true
                }), cancellationToken).ConfigureAwait(false);
            }

            _options = newOptions;
            _lastFileModifiedTicks = DateTime.UtcNow.Ticks;
        }
        finally
        {
            _lockSemaphore.Release();
        }

        var savedArgs = new FileSavedEventArgs(newOptions);
        FileSaved?.Invoke(this, savedArgs);
    }

    private static PropertyInfo[] GetCachedProperties(Type type)
    {
        lock (_propertyCacheLock)
        {
            if (_propertyCache.TryGetValue(type, out var cachedProperties)) 
                return cachedProperties;

            var properties = type.GetProperties();
            _propertyCache[type] = properties;
            return properties;
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _lockSemaphore?.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}