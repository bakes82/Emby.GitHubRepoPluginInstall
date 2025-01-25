using System;

namespace Emby.GitHubRepoPluginInstall.Models;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
public class DontSaveAttribute : Attribute
{
}