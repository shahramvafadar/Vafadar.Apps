using Microsoft.Data.Sqlite;

namespace Vafadar.Data;

/// <summary>Stores non-secret migration selection in a versioned SQLite rollback-journal transaction.</summary>
/// <remarks>The caller owns an existing private directory and holds the exclusive profile/access lease.
/// This store contains only identities and a phase, never a key, financial data or a portable path.
/// SQLite/VFS commit guarantees do not replace physical-device power-loss acceptance.</remarks>
public sealed class SqliteDatabaseGenerationJournal
{
    private const int ApplicationId = 0x565a474a;
    private const string Schema = "CREATE TABLE generation_state (id INTEGER PRIMARY KEY CHECK(id=1), profile TEXT NOT NULL, original TEXT NOT NULL, candidate TEXT NOT NULL, phase INTEGER NOT NULL CHECK(phase BETWEEN 0 AND 4))";
    private readonly string path;
    private readonly DatabaseGenerationCheckpoint identity;

    /// <summary>Captures a trusted local journal path and immutable expected profile/generation identity.</summary>
    public SqliteDatabaseGenerationJournal(string path, DatabaseGenerationCheckpoint identity)
    {
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!Path.IsPathFullyQualified(path)) throw new ArgumentException("Journal requires an absolute private path.");
        this.path = Path.GetFullPath(path);
        this.identity = new(identity.ProfileId, identity.OriginalGeneration, identity.CandidateGeneration);
        ValidateDirectory();
    }

    /// <summary>Explicitly creates a new original-state journal; existing or interrupted files are never replaced.</summary>
    public Task CreateNewAsync(CancellationToken cancellationToken = default) => PublishNewAsync(null, cancellationToken);

    /// <summary>Publishes complete captured initial state with explicit native file/directory synchronization.</summary>
    public Task CreateNewAsync(IDatabaseGenerationFileDurability durability, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(durability); return PublishNewAsync(durability, cancellationToken);
    }

    private Task PublishNewAsync(IDatabaseGenerationFileDurability? durability, CancellationToken ct) =>
        SqliteMetadataPublication.CreateAsync(path,
            stage => new SqliteDatabaseGenerationJournal(stage, identity).CreateCoreAsync(ct),
            async target =>
            {
                if (await new SqliteDatabaseGenerationJournal(target, identity).ReadAsync(ct).ConfigureAwait(false) != identity)
                    throw new InvalidOperationException("Staged journal is not the captured Original state.");
            }, ValidateDirectory, durability, ct);

    private async Task CreateCoreAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateDirectory();
        // Reserve this exact new path before SQLite opens it; accidental ReadWriteCreate could replace missing state.
        using (var reservation = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            reservation.Flush(flushToDisk: true);
        await using var connection = await OpenAsync(cancellationToken, creating: true).ConfigureAwait(false);
        using var transaction = connection.BeginTransaction(deferred: false);
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"PRAGMA application_id={ApplicationId}; PRAGMA user_version=1; {Schema}; " +
            "INSERT INTO generation_state VALUES(1,$profile,$original,$candidate,0)";
        command.Parameters.AddWithValue("$profile", identity.ProfileId.ToString("N"));
        command.Parameters.AddWithValue("$original", identity.OriginalGeneration.ToString("N"));
        command.Parameters.AddWithValue("$candidate", identity.CandidateGeneration.ToString("N"));
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        transaction.Commit();
    }

    /// <summary>Reopens actual state, letting SQLite recover its own hot journal; missing or malformed state fails closed.</summary>
    public async Task<DatabaseGenerationCheckpoint> ReadAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
        using var transaction = connection.BeginTransaction(deferred: true);
        return await ReadStateAsync(connection, transaction, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Commits exactly one next checkpoint only when the actual stored state still equals the expected state.</summary>
    public async Task CompareAndCommitAsync(DatabaseGenerationCheckpoint expected, DatabaseGenerationCheckpoint next,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(expected);
        ArgumentNullException.ThrowIfNull(next);
        RequireIdentity(expected);
        if (expected.Advance(expected.ProfileId, expected.CandidateGeneration, expected.Phase + 1) != next)
            throw new InvalidOperationException("Journal transition is not the next bound checkpoint.");
        await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
        using var transaction = connection.BeginTransaction(deferred: false);
        var actual = await ReadStateAsync(connection, transaction, cancellationToken).ConfigureAwait(false);
        if (actual != expected) throw new InvalidOperationException("Journal checkpoint changed.");
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "UPDATE generation_state SET phase=$next WHERE id=1 AND phase=$expected";
        command.Parameters.AddWithValue("$next", (int)next.Phase);
        command.Parameters.AddWithValue("$expected", (int)expected.Phase);
        if (await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) != 1)
            throw new InvalidOperationException("Journal checkpoint was not updated.");
        cancellationToken.ThrowIfCancellationRequested();
        transaction.Commit();
    }

    private async Task<SqliteConnection> OpenAsync(CancellationToken cancellationToken, bool creating = false)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateDirectory();
        if (!File.Exists(path) || (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
            throw new IOException("Captured journal is missing or redirected.");
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        { DataSource = path, Mode = SqliteOpenMode.ReadWrite, Pooling = false, DefaultTimeout = 5 }.ToString());
        try
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            // Validate the captured format/context before changing any existing database journal mode.
            if (!creating)
            {
                using var validation = connection.BeginTransaction(deferred: true);
                await ReadStateAsync(connection, validation, cancellationToken).ConfigureAwait(false);
            }
            using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA journal_mode=DELETE";
            if (!string.Equals(Convert.ToString(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false)), "delete", StringComparison.OrdinalIgnoreCase))
                throw new IOException("Journal rollback mode was not established.");
            // EXTRA also syncs the containing directory after rollback-journal deletion.
            command.CommandText = "PRAGMA synchronous=EXTRA; PRAGMA trusted_schema=OFF; PRAGMA synchronous";
            if (Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false)) != 3)
                throw new IOException("Journal durable synchronization was not established.");
            return connection;
        }
        catch { await connection.DisposeAsync().ConfigureAwait(false); throw; }
    }

    private async Task<DatabaseGenerationCheckpoint> ReadStateAsync(SqliteConnection connection, SqliteTransaction transaction,
        CancellationToken cancellationToken)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "PRAGMA application_id";
        if (Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false)) != ApplicationId)
            throw new IOException("Journal application identity differs.");
        command.CommandText = "PRAGMA user_version";
        if (Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false)) != 1)
            throw new IOException("Unsupported journal format.");
        command.CommandText = "SELECT count(*) FROM sqlite_schema WHERE name NOT GLOB 'sqlite_*'";
        if (Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false)) != 1)
            throw new IOException("Journal schema contains unexpected objects.");
        command.CommandText = "SELECT sql FROM sqlite_schema WHERE name='generation_state' AND type='table'";
        if (!string.Equals(Convert.ToString(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false)), Schema, StringComparison.Ordinal))
            throw new IOException("Journal schema differs.");
        command.CommandText = "SELECT id,profile,original,candidate,phase FROM generation_state";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false) || reader.GetInt64(0) != 1 ||
            reader.GetFieldType(4) != typeof(long) || reader.GetInt64(4) is < 0 or > 4 ||
            !Guid.TryParseExact(reader.GetString(1), "N", out var profile) ||
            !Guid.TryParseExact(reader.GetString(2), "N", out var original) ||
            !Guid.TryParseExact(reader.GetString(3), "N", out var candidate))
            throw new IOException("Journal checkpoint is invalid.");
        var phase = (DatabaseGenerationPhase)reader.GetInt64(4);
        var state = new DatabaseGenerationCheckpoint(profile, original, candidate);
        RequireIdentity(state);
        while (state.Phase < phase) state = state.Advance(profile, candidate, state.Phase + 1);
        if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) throw new IOException("Journal checkpoint is not unique.");
        return state;
    }

    private void RequireIdentity(DatabaseGenerationCheckpoint state)
    {
        if (state.ProfileId != identity.ProfileId || state.OriginalGeneration != identity.OriginalGeneration || state.CandidateGeneration != identity.CandidateGeneration)
            throw new IOException("Journal profile/generation identity differs.");
    }

    private void ValidateDirectory()
    {
        var parent = Path.GetDirectoryName(path)!;
        if (!Directory.Exists(parent) || (File.GetAttributes(parent) & FileAttributes.ReparsePoint) != 0)
            throw new IOException("Trusted journal directory is missing or redirected.");
    }
}
