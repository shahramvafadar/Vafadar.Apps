using Microsoft.Data.Sqlite;

namespace Vafadar.Data;

/// <summary>Retains a private ordered chain of original/target restore references before key/staging work.</summary>
/// <remarks>References are not permission to open files. Selected status mirrors a reopened actual journal;
/// key/file/preference replay verification remains required. No abandoned record, key or rollback is pruned here.</remarks>
public sealed class SqliteProfileRestoreHistory
{
    private const int ApplicationId = 0x565a5248;
    private const string Schema = "CREATE TABLE restore_history (sequence INTEGER PRIMARY KEY, installation TEXT NOT NULL, profile TEXT NOT NULL, original TEXT NOT NULL, candidate TEXT NOT NULL, selected INTEGER NOT NULL CHECK(selected BETWEEN 0 AND 1))";
    private readonly string path;
    private readonly DatabaseKeyBinding initial;
    private readonly Action validate;

    /// <summary>Captures the stable initial key binding and current private maintenance validation.</summary>
    public SqliteProfileRestoreHistory(string path, DatabaseKeyBinding initial, Action validateAccess)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path); ArgumentNullException.ThrowIfNull(initial); ArgumentNullException.ThrowIfNull(validateAccess);
        if (!Path.IsPathFullyQualified(path)) throw new ArgumentException("Restore history requires an absolute private path.");
        this.path = Path.GetFullPath(path); this.initial = initial; validate = validateAccess; ValidateDirectory();
    }

    /// <summary>Explicitly initializes references after the caller verified its current initial encrypted generation.</summary>
    public Task CreateNewAsync(CancellationToken ct = default) => CreateCoreAsync(true, ct);

    /// <summary>Records pending initial ancestry against an existing Original journal before any native key work.</summary>
    public async Task CreateInitialIntentAsync(DatabaseGenerationCheckpoint captured, SqliteDatabaseGenerationJournal journal, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(journal); CheckIdentity(captured); Check(ct);
        if (captured.CandidateGeneration != initial.GenerationId || captured.Phase != DatabaseGenerationPhase.Original ||
            await journal.ReadAsync(ct).ConfigureAwait(false) != captured)
            throw new InvalidOperationException("Initial intent requires the exact existing Original migration journal.");
        await CreateCoreAsync(false, ct).ConfigureAwait(false);
    }

    private async Task CreateCoreAsync(bool selected, CancellationToken ct)
    {
        Check(ct); ValidateDirectory();
        foreach (var suffix in new[] { "", "-journal", "-wal", "-shm" })
            if (File.Exists(path + suffix) || Directory.Exists(path + suffix)) throw new IOException("Restore history already exists or is interrupted.");
        using (var reservation = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None)) reservation.Flush(true);
        await using var connection = await OpenAsync(true, ct).ConfigureAwait(false);
        using var transaction = connection.BeginTransaction(deferred: false); using var command = connection.CreateCommand(); command.Transaction = transaction;
        command.CommandText = $"PRAGMA application_id={ApplicationId}; PRAGMA user_version=1; {Schema}; INSERT INTO restore_history VALUES(0,$installation,$profile,$original,$candidate,$selected)";
        command.Parameters.AddWithValue("$selected", selected ? 1 : 0);
        Bind(command, initial.GenerationId, initial.GenerationId); await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        Check(ct); transaction.Commit();
        var rows = await ReadAsync(ct).ConfigureAwait(false);
        if (rows.Count != 1 || rows[0].Selected != selected) throw new IOException("Restore history bootstrap readback differs.");
    }

    /// <summary>Mirrors completed initial migration only after the caller retained and verified its existing native key.</summary>
    internal async Task MarkInitialSelectedAsync(DatabaseGenerationCheckpoint captured, SqliteDatabaseGenerationJournal journal, CancellationToken ct)
    {
        CheckIdentity(captured); Check(ct); var actual = await journal.ReadAsync(ct).ConfigureAwait(false);
        if (actual.ProfileId != initial.ProfileId || actual.CandidateGeneration != initial.GenerationId ||
            actual.OriginalGeneration != captured.OriginalGeneration || actual.Phase != DatabaseGenerationPhase.Completed)
            throw new InvalidOperationException("Initial migration has not completed its actual journal.");
        await using var connection = await OpenAsync(false, ct).ConfigureAwait(false);
        using var transaction = connection.BeginTransaction(deferred: false);
        var rows = await ReadCoreAsync(connection, transaction, ct).ConfigureAwait(false);
        if (rows.Count != 1) throw new InvalidOperationException("Restored ancestry cannot be overwritten by initial completion.");
        if (rows[0].Selected) return;
        using var command = connection.CreateCommand(); command.Transaction = transaction;
        command.CommandText = "UPDATE restore_history SET selected=1 WHERE sequence=0";
        if (await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false) != 1) throw new IOException("Initial ancestry commit differs.");
        Check(ct); transaction.Commit();
        if (!(await ReadAsync(ct).ConfigureAwait(false))[0].Selected) throw new IOException("Initial ancestry readback differs.");
    }

    /// <summary>Reads complete existing references without manufacturing a missing history or choosing a fallback generation.</summary>
    public async Task<IReadOnlyList<ProfileRestoreReference>> ReadAsync(CancellationToken ct = default)
    {
        Check(ct); await using var connection = await OpenAsync(false, ct).ConfigureAwait(false);
        using var transaction = connection.BeginTransaction(deferred: true);
        return await ReadCoreAsync(connection, transaction, ct).ConfigureAwait(false);
    }

    /// <summary>Durably reserves the next captured transition; a matching existing reservation retains its target.</summary>
    public async Task ReserveAsync(DatabaseGenerationCheckpoint captured, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(captured); CheckIdentity(captured); Check(ct);
        await using var connection = await OpenAsync(false, ct).ConfigureAwait(false);
        using var transaction = connection.BeginTransaction(deferred: false);
        var rows = await ReadCoreAsync(connection, transaction, ct).ConfigureAwait(false); var last = rows[^1];
        if (last.Sequence != 0 && last.OriginalGeneration == captured.OriginalGeneration && last.CandidateGeneration == captured.CandidateGeneration) return;
        if (!last.Selected || last.CandidateGeneration != captured.OriginalGeneration || rows.Any(row => row.CandidateGeneration == captured.CandidateGeneration))
            throw new InvalidOperationException("Restore history changed or has unresolved intent.");
        using var command = connection.CreateCommand(); command.Transaction = transaction;
        command.CommandText = "INSERT INTO restore_history VALUES($sequence,$installation,$profile,$original,$candidate,0)";
        command.Parameters.AddWithValue("$sequence", checked(last.Sequence + 1)); Bind(command, captured.OriginalGeneration, captured.CandidateGeneration);
        await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false); Check(ct); transaction.Commit();
        await RequireAsync(captured, ct).ConfigureAwait(false);
    }

    /// <summary>Requires the latest durable reservation to match this actual package receiver, including after restart.</summary>
    public async Task RequireAsync(DatabaseGenerationCheckpoint captured, CancellationToken ct = default)
    {
        CheckIdentity(captured); var last = (await ReadAsync(ct).ConfigureAwait(false))[^1];
        if (last.Sequence == 0 || last.OriginalGeneration != captured.OriginalGeneration || last.CandidateGeneration != captured.CandidateGeneration)
            throw new InvalidOperationException("Package receiver does not own the latest restore reference.");
    }

    /// <summary>Requires the same stable installation/profile key namespace before restore effects.</summary>
    internal void RequireBinding(DatabaseKeyBinding binding)
    {
        ArgumentNullException.ThrowIfNull(binding);
        if (binding.InstallationId != initial.InstallationId || binding.ProfileId != initial.ProfileId)
            throw new InvalidOperationException("Restore key binding belongs to another installation or profile.");
    }

    /// <summary>Matches the private reference store captured by the receiver, without reading any key.</summary>
    internal bool IsSameStore(SqliteProfileRestoreHistory other) => path == other.path && initial == other.initial;

    /// <summary>Requires the complete initial key context recorded by the stable identity catalogue.</summary>
    internal void RequireInitialBinding(DatabaseKeyBinding binding)
    {
        RequireBinding(binding);
        if (initial.GenerationId != binding.GenerationId)
            throw new InvalidOperationException("Restore ancestry starts from another initial generation.");
    }

    /// <summary>Mirrors actual selected journal state after receiver key/file verification; this is not preference durability or package completion.</summary>
    internal async Task RecordSelectedAsync(DatabaseGenerationCheckpoint captured, SqliteDatabaseGenerationJournal journal, CancellationToken ct)
    {
        CheckIdentity(captured); Check(ct); var actual = await journal.ReadAsync(ct).ConfigureAwait(false);
        if (actual.ProfileId != initial.ProfileId || actual.OriginalGeneration != captured.OriginalGeneration ||
            actual.CandidateGeneration != captured.CandidateGeneration || !actual.CandidateSelected)
            throw new InvalidOperationException("Actual restore journal is not the captured selected generation.");
        await using var connection = await OpenAsync(false, ct).ConfigureAwait(false); using var transaction = connection.BeginTransaction(deferred: false);
        var last = (await ReadCoreAsync(connection, transaction, ct).ConfigureAwait(false))[^1];
        if (last.Sequence == 0 || last.OriginalGeneration != captured.OriginalGeneration || last.CandidateGeneration != captured.CandidateGeneration)
            throw new InvalidOperationException("Restore reference changed before selection readback.");
        using var command = connection.CreateCommand(); command.Transaction = transaction;
        command.CommandText = "UPDATE restore_history SET selected=1 WHERE sequence=$sequence"; command.Parameters.AddWithValue("$sequence", last.Sequence);
        if (await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false) != 1) throw new IOException("Restore reference selection was not recorded.");
        Check(ct); transaction.Commit();
        if (!(await ReadAsync(ct).ConfigureAwait(false))[^1].Selected) throw new IOException("Restore reference selection readback differs.");
    }

    private async Task<SqliteConnection> OpenAsync(bool creating, CancellationToken ct)
    {
        Check(ct); ValidateDirectory();
        if (!File.Exists(path) || (File.GetAttributes(path) & (FileAttributes.ReparsePoint | FileAttributes.Directory)) != 0) throw new IOException("Restore history missing or redirected.");
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path, Mode = SqliteOpenMode.ReadWrite, Pooling = false, DefaultTimeout = 5 }.ToString());
        try
        {
            await connection.OpenAsync(ct).ConfigureAwait(false);
            if (!creating) { using var validation = connection.BeginTransaction(deferred: true); await ReadCoreAsync(connection, validation, ct).ConfigureAwait(false); }
            using var command = connection.CreateCommand(); command.CommandText = "PRAGMA journal_mode=DELETE";
            if (!string.Equals(Convert.ToString(await command.ExecuteScalarAsync(ct).ConfigureAwait(false)), "delete", StringComparison.OrdinalIgnoreCase)) throw new IOException("Restore history rollback mode unavailable.");
            command.CommandText = "PRAGMA synchronous=EXTRA; PRAGMA trusted_schema=OFF; PRAGMA synchronous";
            if (Convert.ToInt64(await command.ExecuteScalarAsync(ct).ConfigureAwait(false)) != 3) throw new IOException("Restore history synchronization unavailable.");
            Check(ct); return connection;
        }
        catch { await connection.DisposeAsync().ConfigureAwait(false); throw; }
    }

    private async Task<IReadOnlyList<ProfileRestoreReference>> ReadCoreAsync(SqliteConnection connection, SqliteTransaction transaction, CancellationToken ct)
    {
        Check(ct); using var command = connection.CreateCommand(); command.Transaction = transaction;
        command.CommandText = "PRAGMA application_id"; if (Convert.ToInt64(await command.ExecuteScalarAsync(ct).ConfigureAwait(false)) != ApplicationId) throw new IOException("Foreign restore history.");
        command.CommandText = "PRAGMA user_version"; if (Convert.ToInt64(await command.ExecuteScalarAsync(ct).ConfigureAwait(false)) != 1) throw new IOException("Unsupported restore history.");
        command.CommandText = "SELECT count(*) FROM sqlite_schema WHERE name NOT GLOB 'sqlite_*'"; if (Convert.ToInt64(await command.ExecuteScalarAsync(ct).ConfigureAwait(false)) != 1) throw new IOException("Extra restore history schema.");
        command.CommandText = "SELECT sql FROM sqlite_schema WHERE type='table' AND name='restore_history'"; if (Convert.ToString(await command.ExecuteScalarAsync(ct).ConfigureAwait(false)) != Schema) throw new IOException("Restore history schema differs.");
        command.CommandText = "SELECT sequence,installation,profile,original,candidate,selected FROM restore_history ORDER BY sequence";
        var rows = new List<ProfileRestoreReference>(); var seen = new HashSet<Guid>();
        await using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            if (reader.GetFieldType(0) != typeof(long) || reader.GetFieldType(5) != typeof(long) || Enumerable.Range(1, 4).Any(index => reader.GetFieldType(index) != typeof(string))) throw new IOException("Incorrectly typed restore reference.");
            var sequence = reader.GetInt64(0); var original = Parse(reader.GetString(3)); var candidate = Parse(reader.GetString(4)); var selected = reader.GetInt64(5);
            if (sequence != rows.Count || Parse(reader.GetString(1)) != initial.InstallationId || Parse(reader.GetString(2)) != initial.ProfileId || selected is < 0 or > 1 || !seen.Add(candidate)) throw new IOException("Restore reference identity/sequence differs.");
            if (sequence == 0 ? original != initial.GenerationId || candidate != original
                : original != rows[^1].CandidateGeneration || !rows[^1].Selected || original == candidate) throw new IOException("Restore ancestry is invalid.");
            rows.Add(new(sequence, original, candidate, selected == 1));
        }
        if (rows.Count == 0) throw new IOException("Initial restore reference missing."); Check(ct); return rows.AsReadOnly();
    }

    private void Bind(SqliteCommand command, Guid original, Guid candidate)
    { command.Parameters.AddWithValue("$installation", initial.InstallationId.ToString("N")); command.Parameters.AddWithValue("$profile", initial.ProfileId.ToString("N")); command.Parameters.AddWithValue("$original", original.ToString("N")); command.Parameters.AddWithValue("$candidate", candidate.ToString("N")); }
    private void CheckIdentity(DatabaseGenerationCheckpoint state) { ArgumentNullException.ThrowIfNull(state); if (state.ProfileId != initial.ProfileId) throw new InvalidOperationException("Restore reference belongs to another profile."); }
    private static Guid Parse(string value) { if (!Guid.TryParseExact(value, "N", out var id) || id == Guid.Empty || value != id.ToString("N")) throw new IOException("Invalid restore reference identity."); return id; }
    private void Check(CancellationToken ct) { ct.ThrowIfCancellationRequested(); validate(); }
    private void ValidateDirectory() { var parent = Path.GetDirectoryName(path)!; if (!Directory.Exists(parent) || (File.GetAttributes(parent) & FileAttributes.ReparsePoint) != 0) throw new IOException("Private restore history directory missing or redirected."); }
}

/// <summary>Non-secret retained ancestry; Selected is not permission to open or a completed package marker.</summary>
/// <param name="Sequence">Contiguous local history order.</param>
/// <param name="OriginalGeneration">Previously selected generation.</param>
/// <param name="CandidateGeneration">Captured target, or initial generation in row zero.</param>
/// <param name="Selected">Actual selected journal was mirrored after receiver verification.</param>
public sealed record ProfileRestoreReference(long Sequence, Guid OriginalGeneration, Guid CandidateGeneration, bool Selected);
