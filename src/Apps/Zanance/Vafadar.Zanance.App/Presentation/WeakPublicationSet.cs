using System.Collections;

namespace Vafadar.Zanance.App.Presentation;

/// <summary>Tracks live native page owners without extending the lifetime of a completed page.</summary>
/// <remarks>Registration and retirement run on the owning UI thread, like the Shell navigation graph.</remarks>
internal sealed class WeakPublicationSet<T> : IEnumerable<T> where T : class
{
    private readonly List<WeakReference<T>> _owners = [];

    /// <summary>Registers an actual page once by reference, pruning owners already collected.</summary>
    internal void Add(T owner)
    {
        ArgumentNullException.ThrowIfNull(owner);
        _owners.RemoveAll(reference => !reference.TryGetTarget(out _));
        if (!_owners.Any(reference => reference.TryGetTarget(out var existing) && ReferenceEquals(existing, owner)))
            _owners.Add(new(owner));
    }

    /// <summary>Releases registrations after the original root has retired its live owners.</summary>
    internal void Clear() => _owners.Clear();

    /// <summary>Freezes only currently live owners for one synchronous retirement pass.</summary>
    public IEnumerator<T> GetEnumerator()
    {
        // D-675: a popped modal must not remain rooted by Shell. During retirement, however,
        // keep each surviving original owner alive until its command/read authority is revoked.
        var live = new List<T>();
        _owners.RemoveAll(reference =>
        {
            if (!reference.TryGetTarget(out var owner)) return true;
            live.Add(owner);
            return false;
        });
        return live.GetEnumerator();
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
