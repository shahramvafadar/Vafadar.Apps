using Vafadar.Data;

namespace Vafadar.Testing;

/// <summary>Releases production SQLite pools only for files owned by an isolated test directory.</summary>
public static class SqliteTestPools
{
    /// <summary>Clears the known pools for existing owned databases, including nested profile/fixture folders.</summary>
    /// <remarks>Dispose test-owned contexts and service providers first; this never clears process-wide pools.</remarks>
    public static void Clear(TemporaryDirectory directory)
    {
        ArgumentNullException.ThrowIfNull(directory);
        if (!Directory.Exists(directory.Path))
        {
            return;
        }

        foreach (var path in Directory.EnumerateFiles(directory.Path, "*.db", SearchOption.AllDirectories))
        {
            LocalDatabasePools.ClearFile(path);
        }
    }
}
