using System.Threading;
using System.Threading.Tasks;

namespace Emby.GitHubRepoPluginInstall.UIBaseClasses.Store;

public interface IAsyncStore<TOptionType>
{
    Task<TOptionType> GetOptionsAsync(CancellationToken cancellationToken = default);
    Task SetOptionsAsync(TOptionType options, CancellationToken cancellationToken = default);
    Task<TOptionType> ReloadOptionsAsync(CancellationToken cancellationToken = default);
}