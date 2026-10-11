using Microsoft.Data.Sqlite;

namespace Vafadar.Data;

/// <summary>Releases only the pooled connections for an application's known local database file.</summary>
public static class LocalDatabasePools
{
    /// <summary>Closes idle connections and retires borrowed connections for the named full-path file.</summary>
    /// <remarks>
    /// Covers the local factory and the default/read-only/read-write diagnostic connection strings. Call after
    /// disposing owned contexts before deleting a file. Arbitrary connection-string variants are not enumerated;
    /// their owners must clear their exact connection pool. Other databases in the process remain untouched.
    /// </remarks>
    public static void ClearFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var fullPath = Path.GetFullPath(path);
        Clear(new SqliteConnectionStringBuilder { DataSource = fullPath });
        foreach (var mode in new[] { SqliteOpenMode.ReadWriteCreate, SqliteOpenMode.ReadWrite, SqliteOpenMode.ReadOnly })
        {
            Clear(new SqliteConnectionStringBuilder { DataSource = fullPath, Mode = mode });
        }
    }

    private static void Clear(SqliteConnectionStringBuilder builder)
    {
        // D-137: ClearAllPools would also close unrelated profiles and parallel tests' native SQLite handles.
        using var connection = new SqliteConnection(builder.ToString());
        SqliteConnection.ClearPool(connection);
    }
}
