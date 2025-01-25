using System;
using System.Collections.Generic;
using System.IO;
using Emby.GitHubRepoPluginInstall.Models;
using Emby.GitHubRepoPluginInstall.Storage;
using Emby.GitHubRepoPluginInstall.UI;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Plugins.UI;
using MediaBrowser.Model.Serialization;

namespace Emby.GitHubRepoPluginInstall;

public class GitHubRepPluginInstall : BasePlugin, IHasThumbImage, IHasUIPages, IHasPluginConfiguration
{
    public static readonly string PluginName = "GitHub Repo Plugin Install";

    private readonly ILogger                       _logger;
    private readonly PluginOptionsStore            _pluginOptionsStore;
    private readonly IServerApplicationHost        _applicationHost;
    public readonly  IJsonSerializer               _jsonSerializer;
    private          List<IPluginUIPageController> _pages;

    /// <summary>Initializes a new instance of the <see cref="GitHubRepPluginInstall" /> class.</summary>
    /// <param name="applicationHost">The application host.</param>
    /// <param name="logManager">The log manager.</param>
    /// <param name="jsonSerializer"></param>
    public GitHubRepPluginInstall(ILogManager logManager,
                                  IServerApplicationHost applicationHost,
                                  IJsonSerializer jsonSerializer)
    {
        _applicationHost    = applicationHost;
        _jsonSerializer     = jsonSerializer;
        _logger             = logManager.GetLogger(PluginName);
        _pluginOptionsStore = new PluginOptionsStore(applicationHost, _logger, Name);
    }

    public override        Guid   Id          => new Guid("78185FEE-2187-4703-A8D0-4E625761D6E7");
    public override        string Description => "Install plugins from GitHub repositories.";
    public sealed override string Name        => PluginName;

    /// <summary>
    ///     Gets the type of configuration this plugin uses
    /// </summary>
    /// <value>The type of the configuration.</value>
    public Type ConfigurationType => typeof(PluginUIOptions);

    /// <summary>
    ///     Completely overwrites the current configuration with a new copy
    ///     Returns true or false indicating success or failure
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <exception cref="System.ArgumentNullException">configuration</exception>
    public void UpdateConfiguration(BasePluginConfiguration configuration)
    {
    }

    /// <summary>
    ///     Gets the plugin's configuration
    /// </summary>
    /// <value>The configuration.</value>
    public BasePluginConfiguration Configuration { get; } = new BasePluginConfiguration();

    public void SetStartupInfo(Action<string> directoryCreateFn)
    {
    }

    /// <summary>Gets the thumb image format.</summary>
    /// <value>The thumb image format.</value>
    public ImageFormat ThumbImageFormat => ImageFormat.Jpg;

    /// <summary>Gets the thumb image.</summary>
    /// <returns>An image stream.</returns>
    public Stream GetThumbImage()
    {
        var type = GetType();
        return type.Assembly.GetManifestResourceStream(type.Namespace + ".ThumbImage.jpg");
    }

    public IReadOnlyCollection<IPluginUIPageController> UIPageControllers
    {
        get
        {
            if (_pages == null)
                _pages = new List<IPluginUIPageController>
                         {
                             new MainPageController(GetPluginInfo(), _pluginOptionsStore, _logger, _applicationHost,
                                                    _jsonSerializer)
                         };

            return _pages.AsReadOnly();
        }
    }
}