using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Vafadar.Zanance.App.Presentation;

/// <summary>
/// Publishes a complete presentation snapshot with one Reset so BindableLayout can rebind its existing rows instead
/// of disconnecting and rebuilding every native control on each Home reload (D-92). Use on the presentation thread.
/// </summary>
public sealed class SnapshotCollection<T> : ObservableCollection<T>
{
    /// <summary>
    /// Materializes first, preserving the prior snapshot on enumeration failure, then publishes all rows together.
    /// Always rebind fresh items, including equal records whose computed semantic colours depend on the theme.
    /// </summary>
    public void ReplaceAll(IEnumerable<T> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        CheckReentrancy();
        var snapshot = items.ToArray();
        Items.Clear();
        foreach (var item in snapshot) { Items.Add(item); }
        OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
        OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }
}
