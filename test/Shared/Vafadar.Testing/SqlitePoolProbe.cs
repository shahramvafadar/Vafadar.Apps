using Microsoft.Data.Sqlite;

namespace Vafadar.Testing;

/// <summary>An actual SQLite TEMP-table marker that survives an idle pooled connection, but not pool retirement.</summary>
public static class SqlitePoolProbe
{
    /// <summary>Creates a marker on the exact connection string without altering persistent database rows.</summary>
    public static void Mark(string connectionString)
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "CREATE TEMP TABLE PoolIsolationMarker(Value TEXT); INSERT INTO PoolIsolationMarker VALUES ('retained');";
        command.ExecuteNonQuery();
    }

    /// <summary>Returns the marker if this exact pooled native session is retained; otherwise returns null.</summary>
    public static string? Read(string connectionString)
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Value FROM temp.PoolIsolationMarker";
        try
        {
            return (string?)command.ExecuteScalar();
        }
        catch (SqliteException exception) when (exception.SqliteErrorCode == 1 && exception.Message.Contains("no such table", StringComparison.Ordinal))
        {
            return null;
        }
    }
}
