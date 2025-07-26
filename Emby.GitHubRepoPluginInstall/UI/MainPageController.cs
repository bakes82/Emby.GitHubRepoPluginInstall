using System.Threading.Tasks;
using Emby.GitHubRepoPluginInstall.Storage;
using Emby.GitHubRepoPluginInstall.UIBaseClasses;
using MediaBrowser.Common;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Plugins.UI.Views;
using MediaBrowser.Model.Serialization;

namespace Emby.GitHubRepoPluginInstall.UI;

internal class MainPageController : ControllerBase
{
    private readonly ILogger                  _logger;
    private readonly PluginInfo               _pluginInfo;
    private readonly SecurePluginOptionsStore _pluginOptionsStore;
    private readonly IApplicationHost   _applicationHost;
    private readonly IJsonSerializer    _jsonSerializer;

    /// <summary>Initializes a new instance of the <see cref="ControllerBase" /> class.</summary>
    /// <param name="pluginInfo">The plugin information.</param>
    /// <param name="pluginOptionsStore"></param>
    /// <param name="logger"></param>
    /// <param name="applicationHost"></param>
    /// <param name="jsonSerializer"></param>
    public MainPageController(PluginInfo pluginInfo,
                              SecurePluginOptionsStore pluginOptionsStore,
                              ILogger logger,
                              IApplicationHost applicationHost,
                              IJsonSerializer jsonSerializer) : base(pluginInfo.Id)
    {
        _pluginInfo         = pluginInfo;
        _pluginOptionsStore = pluginOptionsStore;
        _logger             = logger;
        _applicationHost    = applicationHost;
        _jsonSerializer     = jsonSerializer;
        PageInfo = new PluginPageInfo
                   {
                       Name             = "GitHubRepoPluginInstall",
                       EnableInMainMenu = true,
                       DisplayName      = "GitHub Repo Plugin Install",
                       MenuIcon         = "list_alt",
                       IsMainConfigPage = false
                   };
    }

    public override PluginPageInfo PageInfo { get; }

    public override Task<IPluginUIView> CreateDefaultPageView()
    {
        IPluginUIView view =
            new MainPageView(_pluginInfo, _pluginOptionsStore, _logger, _applicationHost, _jsonSerializer);
        return Task.FromResult(view);
    }
}