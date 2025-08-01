using System;

namespace Emby.GitHubRepoPluginInstall.Models;

public class PluginRegistry
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; }
    public string RawUrl { get; set; }
    public bool Enabled { get; set; } = true;
    public DateTime LastChecked { get; set; }
}