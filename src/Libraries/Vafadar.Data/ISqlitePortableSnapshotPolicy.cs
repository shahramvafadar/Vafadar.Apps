using Microsoft.Data.Sqlite;

namespace Vafadar.Data;

/// <summary>Application-owned removal of device-only metadata from an isolated portable copy, never the live source.</summary>
public interface ISqlitePortableSnapshotPolicy
{
    /// <summary>Sanitizes only the supplied copy schema; identifiers are supplied by the exporter, never a package.</summary>
    Task PrepareAsync(SqliteConnection connection, string schema, CancellationToken cancellationToken);
}
