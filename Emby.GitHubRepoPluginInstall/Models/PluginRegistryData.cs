using System.Collections.Generic;

namespace Emby.GitHubRepoPluginInstall.Models;

public class PluginRegistryData
{
    public string Name { get; set; }
    public string Description { get; set; }
    public List<PluginRegistryEntry> Plugins { get; set; } = new List<PluginRegistryEntry>();
}

public class PluginRegistryEntry
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string Url { get; set; }
    
    // Added at runtime
    public string RegistrySource { get; set; }
}