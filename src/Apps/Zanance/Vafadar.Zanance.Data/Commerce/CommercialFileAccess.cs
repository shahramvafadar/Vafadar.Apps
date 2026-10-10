using Vafadar.Zanance.Core.Commerce;

namespace Vafadar.Zanance.Data.Commerce;

/// <summary>Cached rights bound to the actual operation's database file, shared by writer and recovery boundaries.</summary>
internal sealed class CommercialFileAccess
{
    private readonly ICommercialWriteAccessSource _source;
    private readonly string _path;
    private readonly CommercialWriteAccess _snapshot;

    /// <summary>Captures synchronous cached facts before any writer wait or backup stream await.</summary>
    public CommercialFileAccess(ICommercialWriteAccessSource source, string databasePath)
    {
        _source = source;
        _path = Path.GetFullPath(databasePath);
        _snapshot = source.Capture(_path) ?? throw new InvalidOperationException("A commercial access source returned no snapshot.");
        if (_snapshot.Enforced && !string.Equals(_path, _snapshot.DatabasePath, StringComparison.Ordinal))
            throw new InvalidOperationException("The commercial snapshot belongs to another database.");
    }

    /// <summary>Gets whether the current operation uses commercial checks.</summary>
    public bool Enforced => _snapshot.Enforced;

    /// <summary>Gets this actual file's immutable capability context; inactive operations have none.</summary>
    public CapabilityContext? Context => _snapshot.Context;

    /// <summary>Rejects retired facts without consulting a subsequently selected profile or a network provider.</summary>
    public void EnsureCurrent()
    {
        if (Enforced && _source.Capture(_path) != _snapshot)
            throw new InvalidOperationException("Commercial access changed during the operation; retry with current rights.");
    }

    /// <summary>Checks a concrete operation; personal payment never substitutes for exact shared membership.</summary>
    public void DemandFeature(CommercialFeature feature)
    {
        EnsureCurrent();
        if (!Enforced) return;
        var permission = PlanPolicy.Check(feature, Context!);
        if (permission != FeaturePermission.Allowed) throw new CommercialWriteRejectedException(feature, permission);
    }
}
