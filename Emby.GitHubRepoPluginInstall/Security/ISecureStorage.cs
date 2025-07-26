using System.Threading.Tasks;

namespace Emby.GitHubRepoPluginInstall.Security;

public interface ISecureStorage
{
    Task<string> ProtectAsync(string plainText);
    Task<string> UnprotectAsync(string protectedText);
    string Protect(string plainText);
    string Unprotect(string protectedText);
}