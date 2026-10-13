using Microsoft.Data.Sqlite;

namespace Vafadar.Data;

public sealed partial class SqliteProfileRetirementJournal
{
    private const string ArtifactSchema = "CREATE TABLE retirement_artifacts (root TEXT NOT NULL, name TEXT NOT NULL, native TEXT NOT NULL, length INTEGER NOT NULL, sha256 TEXT NOT NULL, rootnative TEXT NOT NULL, PRIMARY KEY(root,name))";

    /// <summary>Captures an immutable bounded artifact inventory while the exact request is still Authorized.</summary>
    /// <remarks>The host first verifies complete ownership/reference discovery through native handles.
    /// Repeated capture may verify the same list but cannot replace it or add later files.</remarks>
    public async Task CaptureArtifactsAsync(IReadOnlyList<ProfileRetirementArtifact> artifacts,
        IDatabaseGenerationFileDurability durability, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(artifacts); ArgumentNullException.ThrowIfNull(durability);
        var captured = OrderArtifacts(artifacts);
        await using (var connection = await OpenAsync(false, ct).ConfigureAwait(false))
        {
            using var transaction = connection.BeginTransaction(deferred: false);
            if ((await ReadCoreAsync(connection, transaction, ct).ConfigureAwait(false)).Phase != ProfileRetirementPhase.Authorized)
                throw new InvalidOperationException("Retirement inventory must precede catalogue removal.");
            using var command = connection.CreateCommand(); command.Transaction = transaction;
            command.CommandText = "SELECT count(*) FROM sqlite_schema WHERE name='retirement_artifacts'";
            if (Convert.ToInt64(await command.ExecuteScalarAsync(ct).ConfigureAwait(false)) == 0)
            {
                command.CommandText = ArtifactSchema; await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                foreach (var artifact in captured)
                {
                    command.CommandText = "INSERT INTO retirement_artifacts VALUES($root,$name,$native,$length,$hash,$rootnative)";
                    command.Parameters.Clear(); command.Parameters.AddWithValue("$root", artifact.Root);
                    command.Parameters.AddWithValue("$name", artifact.Name); command.Parameters.AddWithValue("$native", artifact.NativeIdentity);
                    command.Parameters.AddWithValue("$length", artifact.Length); command.Parameters.AddWithValue("$hash", artifact.Sha256);
                    command.Parameters.AddWithValue("$rootnative", artifact.RootIdentity);
                    await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                }
            }
            if (!(await ReadArtifactsCoreAsync(connection, transaction, ct).ConfigureAwait(false)).SequenceEqual(captured))
                throw new IOException("Existing retirement inventory differs; it cannot be replaced.");
            Check(ct); transaction.Commit();
        }
        await durability.SynchronizeFileAsync(path, ct).ConfigureAwait(false);
        await durability.SynchronizeDirectoryAsync(ct).ConfigureAwait(false); Check(ct);
        if (!(await ReadArtifactsAsync(ct).ConfigureAwait(false)).SequenceEqual(captured))
            throw new IOException("Retirement inventory readback differs.");
    }

    /// <summary>Reopens only the existing immutable inventory; absence never means an empty artifact set.</summary>
    public async Task<IReadOnlyList<ProfileRetirementArtifact>> ReadArtifactsAsync(CancellationToken ct = default)
    {
        await using var connection = await OpenAsync(false, ct).ConfigureAwait(false);
        using var transaction = connection.BeginTransaction(deferred: true);
        return await ReadArtifactsCoreAsync(connection, transaction, ct).ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<ProfileRetirementArtifact>> ReadArtifactsCoreAsync(SqliteConnection connection,
        SqliteTransaction transaction, CancellationToken ct)
    {
        Check(ct); using var command = connection.CreateCommand(); command.Transaction = transaction;
        command.CommandText = "SELECT sql FROM sqlite_schema WHERE name='retirement_artifacts'";
        if (Convert.ToString(await command.ExecuteScalarAsync(ct).ConfigureAwait(false)) != ArtifactSchema)
            throw new IOException("Captured retirement inventory is missing or modified.");
        command.CommandText = "SELECT root,name,native,length,sha256,rootnative FROM retirement_artifacts LIMIT 4097";
        var artifacts = new List<ProfileRetirementArtifact>();
        await using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            if (artifacts.Count == 4096 || new[] { 0, 1, 2, 4, 5 }.Any(index => reader.GetFieldType(index) != typeof(string))
                || reader.GetFieldType(3) != typeof(long)) throw new IOException("Retirement inventory exceeds its bound or has invalid types.");
            try { artifacts.Add(new(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetInt64(3), reader.GetString(4), reader.GetString(5))); }
            catch (ArgumentException error) { throw new IOException("Retirement inventory contains an invalid reference.", error); }
        }
        Check(ct); return OrderArtifacts(artifacts);
    }

    private static ProfileRetirementArtifact[] OrderArtifacts(IReadOnlyList<ProfileRetirementArtifact> artifacts)
    {
        if (artifacts.Count > 4096 || artifacts.Any(item => item is null)
            || artifacts.Select(item => item.Root + "/" + item.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() != artifacts.Count)
            throw new IOException("Retirement inventory is excessive or ambiguous.");
        return artifacts.OrderBy(item => item.Root, StringComparer.Ordinal).ThenBy(item => item.Name, StringComparer.Ordinal).ToArray();
    }

    /// <summary>Prevents native cleanup from deleting its own retained authority and rollback sidecars.</summary>
    public void RequireArtifactPath(string artifactPath)
    {
        foreach (var suffix in new[] { "", "-journal", "-wal", "-shm", ".creating" })
            if (string.Equals(Path.GetFullPath(artifactPath), path + suffix, StringComparison.OrdinalIgnoreCase))
                throw new IOException("Retirement cannot delete its own authority.");
    }
}
