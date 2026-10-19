namespace Vafadar.Zanance.Data;

/// <summary>Signals unchanged original Home input superseded by persisted definitions in the same authorized file.</summary>
public sealed class HomeSnapshotSupersededException : InvalidOperationException
{
    /// <summary>Keeps ordinary reviewed actions strict while classifying a Home-only persisted source change.</summary>
    internal static InvalidOperationException SourceChanged(bool homeRefresh, string message) =>
        homeRefresh ? new HomeSnapshotSupersededException() : new InvalidOperationException(message);

    /// <summary>Creates the data reader's classified refusal; arbitrary failures must not authorize a refresh retry.</summary>
    internal HomeSnapshotSupersededException() : base("The original Home source changed before publication; read it again.") { }
}
