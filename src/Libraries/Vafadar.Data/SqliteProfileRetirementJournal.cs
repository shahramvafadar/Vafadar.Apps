using Microsoft.Data.Sqlite;

namespace Vafadar.Data;

/// <summary>Records exact non-secret retirement authority/barriers in a private SQLite rollback journal.</summary>
/// <remarks>Native host must verify actual catalogue/removal/reconciliation effects before each advance.
/// This journal never enumerates/deletes artifacts or keys, grants new authority on missing state, or retires main.</remarks>
public sealed partial class SqliteProfileRetirementJournal
{
    private const int ApplicationId = 0x565a5052;
    private const string Schema = "CREATE TABLE retirement_state (id INTEGER PRIMARY KEY CHECK(id=1), legacy TEXT NOT NULL, profile TEXT NOT NULL, installation TEXT NOT NULL, operation TEXT NOT NULL, phase INTEGER NOT NULL CHECK(phase BETWEEN 0 AND 3))";
    private readonly string path;
    private readonly ProfileRetirementCheckpoint identity;
    private readonly Action validate;

    /// <summary>Captures one absolute private path, immutable request and actual application maintenance validation.</summary>
    public SqliteProfileRetirementJournal(string path, ProfileRetirementCheckpoint identity, Action validateAccess)
    {
        ArgumentNullException.ThrowIfNull(identity); ArgumentNullException.ThrowIfNull(validateAccess);
        if (!Path.IsPathFullyQualified(path)) throw new ArgumentException("Retirement journal requires an absolute private path.");
        this.path = Path.GetFullPath(path); this.identity = new(identity.LegacyProfileId, identity.ProfileId, identity.InstallationId, identity.OperationId);
        validate = validateAccess; Check(CancellationToken.None);
    }

    /// <summary>Explicitly publishes or resumes the exact authorized request with native synchronization.</summary>
    public Task CreateNewAsync(IDatabaseGenerationFileDurability durability, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(durability);
        return SqliteMetadataPublication.CreateAsync(path,
            stage => new SqliteProfileRetirementJournal(stage, identity, validate).CreateCoreAsync(ct),
            async target =>
            {
                if (await new SqliteProfileRetirementJournal(target, identity, validate).ReadAsync(ct).ConfigureAwait(false) != identity)
                    throw new IOException("Staged retirement is not the captured authorized request.");
            }, () => Check(ct), durability, ct);
    }

    private async Task CreateCoreAsync(CancellationToken ct)
    {
        Check(ct);
        using (var reservation = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None)) reservation.Flush(true);
        await using var connection = await OpenAsync(true, ct).ConfigureAwait(false);
        using var transaction = connection.BeginTransaction(deferred: false);
        using var command = connection.CreateCommand(); command.Transaction = transaction;
        command.CommandText = $"PRAGMA application_id={ApplicationId}; PRAGMA user_version=1; {Schema}; INSERT INTO retirement_state VALUES(1,$legacy,$profile,$installation,$operation,0)";
        Bind(command); await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false); Check(ct); transaction.Commit();
    }

    /// <summary>Reopens only actual exact state; absent, modified or unrelated records fail without replacement.</summary>
    public async Task<ProfileRetirementCheckpoint> ReadAsync(CancellationToken ct = default)
    {
        await using var connection = await OpenAsync(false, ct).ConfigureAwait(false);
        using var transaction = connection.BeginTransaction(deferred: true);
        return await ReadCoreAsync(connection, transaction, ct).ConfigureAwait(false);
    }

    /// <summary>Requalifies an observed committed barrier before a host resumes an irreversible effect.</summary>
    /// <remarks>A previous SQLite commit can be visible even when native synchronization failed. Reading
    /// that phase alone is insufficient: synchronize the actual file/directory and recheck unchanged state.
    /// The host must still hold exclusive maintenance ownership and verify the recorded external effects.</remarks>
    public async Task<ProfileRetirementCheckpoint> SynchronizeAndReadAsync(IDatabaseGenerationFileDurability durability,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(durability);
        var observed = await ReadAsync(ct).ConfigureAwait(false);
        await durability.SynchronizeFileAsync(path, ct).ConfigureAwait(false);
        await durability.SynchronizeDirectoryAsync(ct).ConfigureAwait(false); Check(ct);
        if (await ReadAsync(ct).ConfigureAwait(false) != observed)
            throw new IOException("Retirement changed while requalifying its native barrier.");
        return observed;
    }

    /// <summary>Commits exactly one next barrier against actual previous state, then synchronizes native metadata.</summary>
    public async Task CompareAndAdvanceAsync(ProfileRetirementCheckpoint expected, IDatabaseGenerationFileDurability durability, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(expected); ArgumentNullException.ThrowIfNull(durability);
        if (new ProfileRetirementCheckpoint(expected.LegacyProfileId, expected.ProfileId, expected.InstallationId, expected.OperationId) != identity)
            throw new InvalidOperationException("Retirement belongs to another captured request.");
        var next = expected.Advance();
        await using (var connection = await OpenAsync(false, ct).ConfigureAwait(false))
        {
            using var transaction = connection.BeginTransaction(deferred: false);
            if (await ReadCoreAsync(connection, transaction, ct).ConfigureAwait(false) != expected)
                throw new InvalidOperationException("Retirement state changed; reopen before continuing.");
            using var command = connection.CreateCommand(); command.Transaction = transaction;
            command.CommandText = "UPDATE retirement_state SET phase=$next WHERE id=1 AND phase=$expected";
            command.Parameters.AddWithValue("$next", (int)next.Phase); command.Parameters.AddWithValue("$expected", (int)expected.Phase);
            if (await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false) != 1) throw new IOException("Retirement compare-and-advance failed.");
            Check(ct); transaction.Commit();
        }
        await durability.SynchronizeFileAsync(path, ct).ConfigureAwait(false);
        await durability.SynchronizeDirectoryAsync(ct).ConfigureAwait(false); Check(ct);
        if (await ReadAsync(ct).ConfigureAwait(false) != next) throw new IOException("Retirement native readback differs.");
    }

    private async Task<SqliteConnection> OpenAsync(bool creating, CancellationToken ct)
    {
        Check(ct);
        if (!File.Exists(path) || (File.GetAttributes(path) & (FileAttributes.Directory | FileAttributes.ReparsePoint)) != 0)
            throw new IOException("Retirement journal is missing or redirected.");
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path, Mode = SqliteOpenMode.ReadWrite, Pooling = false }.ToString());
        try
        {
            await connection.OpenAsync(ct).ConfigureAwait(false);
            using var command = connection.CreateCommand(); command.CommandText = "PRAGMA journal_mode=DELETE";
            if (!string.Equals(Convert.ToString(await command.ExecuteScalarAsync(ct).ConfigureAwait(false)), "delete", StringComparison.OrdinalIgnoreCase))
                throw new IOException("Retirement rollback journal unavailable.");
            command.CommandText = "PRAGMA synchronous=EXTRA; PRAGMA trusted_schema=OFF; PRAGMA synchronous";
            if (Convert.ToInt64(await command.ExecuteScalarAsync(ct).ConfigureAwait(false)) != 3) throw new IOException("Retirement synchronization unavailable.");
            if (!creating) { using var transaction = connection.BeginTransaction(); _ = await ReadCoreAsync(connection, transaction, ct).ConfigureAwait(false); }
            return connection;
        }
        catch { await connection.DisposeAsync().ConfigureAwait(false); throw; }
    }

    private async Task<ProfileRetirementCheckpoint> ReadCoreAsync(SqliteConnection connection, SqliteTransaction transaction, CancellationToken ct)
    {
        Check(ct); using var command = connection.CreateCommand(); command.Transaction = transaction;
        command.CommandText = "PRAGMA application_id";
        if (Convert.ToInt64(await command.ExecuteScalarAsync(ct).ConfigureAwait(false)) != ApplicationId) throw new IOException("Foreign retirement journal.");
        command.CommandText = "PRAGMA user_version";
        if (Convert.ToInt64(await command.ExecuteScalarAsync(ct).ConfigureAwait(false)) != 1) throw new IOException("Unsupported retirement journal.");
        // GLOB keeps the underscore literal; LIKE would also hide an unrelated table named sqliteAlias.
        command.CommandText = "SELECT count(*) FROM sqlite_schema WHERE name NOT GLOB 'sqlite_*'";
        var schemaCount = Convert.ToInt64(await command.ExecuteScalarAsync(ct).ConfigureAwait(false));
        if (schemaCount is not (1 or 2)) throw new IOException("Unexpected retirement schema.");
        if (schemaCount == 2)
        {
            command.CommandText = "SELECT sql FROM sqlite_schema WHERE name='retirement_artifacts'";
            if (Convert.ToString(await command.ExecuteScalarAsync(ct).ConfigureAwait(false)) != ArtifactSchema)
                throw new IOException("Retirement artifact schema differs.");
        }
        command.CommandText = "SELECT sql FROM sqlite_schema WHERE name='retirement_state'";
        if (Convert.ToString(await command.ExecuteScalarAsync(ct).ConfigureAwait(false)) != Schema) throw new IOException("Retirement schema differs.");
        command.CommandText = "SELECT id,legacy,profile,installation,operation,phase FROM retirement_state";
        await using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
        if (!await reader.ReadAsync(ct).ConfigureAwait(false) || reader.GetFieldType(0) != typeof(long) || reader.GetInt64(0) != 1
            || Enumerable.Range(1, 4).Any(index => reader.GetFieldType(index) != typeof(string)) || reader.GetFieldType(5) != typeof(long))
            throw new IOException("Retirement row is absent or incorrectly typed.");
        if (reader.GetString(1) != identity.LegacyProfileId || reader.GetString(2) != identity.ProfileId.ToString("N")
            || reader.GetString(3) != identity.InstallationId.ToString("N") || reader.GetString(4) != identity.OperationId.ToString("N")
            || reader.GetInt64(5) is < 0 or > 3) throw new IOException("Retirement request identity or phase differs.");
        var result = new ProfileRetirementCheckpoint(identity.LegacyProfileId, identity.ProfileId, identity.InstallationId, identity.OperationId,
            (ProfileRetirementPhase)reader.GetInt64(5));
        if (await reader.ReadAsync(ct).ConfigureAwait(false)) throw new IOException("Retirement row is not unique.");
        Check(ct); return result;
    }
    private void Bind(SqliteCommand command)
    {
        command.Parameters.AddWithValue("$legacy", identity.LegacyProfileId); command.Parameters.AddWithValue("$profile", identity.ProfileId.ToString("N"));
        command.Parameters.AddWithValue("$installation", identity.InstallationId.ToString("N")); command.Parameters.AddWithValue("$operation", identity.OperationId.ToString("N"));
    }
    private void Check(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested(); validate();
        var directory = Path.GetDirectoryName(path)!;
        if (!Directory.Exists(directory) || (File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0)
            throw new IOException("Retirement directory is missing or redirected.");
    }
}
