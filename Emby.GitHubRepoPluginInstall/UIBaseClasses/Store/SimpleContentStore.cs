using System;
using Emby.Web.GenericEdit;

namespace Emby.GitHubRepoPluginInstall.UIBaseClasses.Store;

public class SimpleContentStore<TOptionType> where TOptionType : EditableOptionsBase, new()
{
    private readonly object _lockObj = new object();

    private TOptionType _options;

    public virtual TOptionType GetOptions()
    {
        lock (_lockObj)
        {
            if (_options == null) _options = new TOptionType();

            return _options;
        }
    }

    public virtual void SetOptions(TOptionType newOptions)
    {
        if (newOptions == null) throw new ArgumentNullException(nameof(newOptions));

        lock (_lockObj)
        {
            _options = newOptions;
        }
    }
}