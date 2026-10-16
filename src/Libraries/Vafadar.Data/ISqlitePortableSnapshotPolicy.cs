using Microsoft.Data.Sqlite;

namespace Vafadar.Data;

/// <summary>Application-owned removal of device-only metadata from an isolated portable copy, never the live source.</summary>
public interface ISqlitePortableSnapshotPolicy
{
    /// <summary>Trusted application-owned ordinary tables whose schema is retained but whose private rows must never enter a protected portable copy.</summary>
    /// <remarks>The protected exporter owns its freshly opened source connection and its temporary native authorization callback. This list is never package input.</remarks>
    IReadOnlyCollection<string> EmptyRowTables => Array.Empty<string>();
    /// <summary>Sanitizes only the supplied copy schema; identifiers are supplied by the exporter, never a package.</summary>
    Task PrepareAsync(SqliteConnection connection, string schema, CancellationToken cancellationToken);
}
