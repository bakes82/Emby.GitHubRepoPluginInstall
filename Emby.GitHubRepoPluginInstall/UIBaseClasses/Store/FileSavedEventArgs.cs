﻿using System;
using Emby.Web.GenericEdit;

namespace Emby.GitHubRepoPluginInstall.UIBaseClasses.Store;

public class FileSavedEventArgs : EventArgs
{
    /// <summary>Initializes a new instance of the <see cref="FileSavedEventArgs" /> class.</summary>
    /// <param name="options">The options.</param>
    public FileSavedEventArgs(EditableOptionsBase options)
    {
        Options = options;
    }

    /// <summary>Gets the options.</summary>
    /// <value>The options.</value>
    public EditableOptionsBase Options { get; }
}