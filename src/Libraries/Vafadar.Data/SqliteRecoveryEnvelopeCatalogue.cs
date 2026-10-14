using Microsoft.Data.Sqlite;
using System.Security.Cryptography;

namespace Vafadar.Data;

/// <summary>Durably stores optional public recovery identity and RSA-wrapped generation keys, never private/recovery/plain data keys.</summary>
/// <remarks>Each immutable entry has DEK-keyed HMAC confirmation, allowing device-key validation of stored ciphertext
/// without unlocking the optional recovery secret. Missing/corrupt state never authorizes replacement or bootstrap.</remarks>
public sealed class SqliteRecoveryEnvelopeCatalogue
{
    private const int ApplicationId = 0x565a5245;
    private const string IdentitySchema = "CREATE TABLE recovery_identity (id INTEGER PRIMARY KEY CHECK(id=1), installation TEXT NOT NULL, recovery TEXT NOT NULL, public BLOB NOT NULL)";
    private const string EnvelopeSchema = "CREATE TABLE recovery_envelope (profile TEXT NOT NULL, generation TEXT NOT NULL, envelope BLOB NOT NULL, confirmation BLOB NOT NULL, PRIMARY KEY(profile,generation))";
    private readonly string path;
    private readonly DatabaseRecoveryIdentity identity;
    private readonly IDatabaseGenerationFileDurability durability;
    private readonly Action validate;

    /// <summary>Captures an existing trusted private directory, exact non-secret identity and native synchronization.</summary>
    public SqliteRecoveryEnvelopeCatalogue(string path, DatabaseRecoveryIdentity identity,
        IDatabaseGenerationFileDurability durability, Action validateAccess)
    {
        ArgumentNullException.ThrowIfNull(identity); ArgumentNullException.ThrowIfNull(durability); ArgumentNullException.ThrowIfNull(validateAccess);
        if (!Path.IsPathFullyQualified(path)) throw new ArgumentException("Recovery metadata requires an absolute private path.");
        this.path = Path.GetFullPath(path); this.identity = identity; this.durability = durability; validate = validateAccess;
    }
    /// <summary>The public installation recovery identity; it cannot unlock a data key.</summary>
    public DatabaseRecoveryIdentity Identity => identity;

    /// <summary>Explicitly creates/resumes exact staged public metadata; a caller must persist its encrypted private vault first.</summary>
    public Task CreateNewAsync(CancellationToken ct = default) => SqliteMetadataPublication.CreateAsync(path,
        target => CreateCoreAsync(target, ct),
        async target => { await using var connection = await OpenAsync(target, false, ct).ConfigureAwait(false); },
        () => Check(ct), durability, ct);

    private async Task CreateCoreAsync(string target, CancellationToken ct)
    {
        Check(ct); ValidateDirectory(target);
        using (var reservation = new FileStream(target, FileMode.CreateNew, FileAccess.Write, FileShare.None)) reservation.Flush(true);
        await using var connection = await OpenAsync(target, true, ct).ConfigureAwait(false);
        using var transaction = connection.BeginTransaction(deferred: false);
        using var command = connection.CreateCommand(); command.Transaction = transaction;
        command.CommandText = $"PRAGMA application_id={ApplicationId}; PRAGMA user_version=1; {IdentitySchema}; {EnvelopeSchema}; INSERT INTO recovery_identity VALUES(1,$installation,$recovery,$public)";
        command.Parameters.AddWithValue("$installation", identity.InstallationId.ToString("N")); command.Parameters.AddWithValue("$recovery", identity.RecoveryId.ToString("N"));
        command.Parameters.AddWithValue("$public", identity.ExportPublicKey()); await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        Check(ct); transaction.Commit();
    }

    /// <summary>Wraps or requalifies the actual retained native key, never replacing another mapping for this generation.</summary>
    public async Task CaptureAsync(DatabaseKeyBinding binding, ReadOnlyMemory<byte> key, CancellationToken ct = default)
    {
        RequireBinding(binding); if (key.Length != 32) throw new CryptographicException("Recovery requires the exact data key."); Check(ct);
        await using (var connection = await OpenAsync(path, false, ct).ConfigureAwait(false))
        {
            using var transaction = connection.BeginTransaction(deferred: false);
            var existing = await ReadEntryAsync(connection, transaction, binding, ct).ConfigureAwait(false);
            if (existing is not null) Verify(existing.Value, binding, key.Span);
            else
            {
                using var count = connection.CreateCommand(); count.Transaction = transaction;
                count.CommandText = "SELECT count(*) FROM recovery_envelope";
                if (Convert.ToInt64(await count.ExecuteScalarAsync(ct).ConfigureAwait(false)) >= 4096)
                    throw new IOException("Recovery generation inventory exceeds its bound.");
                var envelope = identity.Protect(key.Span, binding); var confirmation = identity.Confirm(key.Span, binding, envelope);
                using var command = connection.CreateCommand(); command.Transaction = transaction;
                command.CommandText = "INSERT INTO recovery_envelope VALUES($profile,$generation,$envelope,$confirmation)";
                Bind(command, binding); command.Parameters.AddWithValue("$envelope", envelope); command.Parameters.AddWithValue("$confirmation", confirmation);
                await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            }
            Check(ct); transaction.Commit();
        }
        // Existing rows must also requalify a previously observable commit after failed native flush.
        await durability.SynchronizeFileAsync(path, ct).ConfigureAwait(false); await durability.SynchronizeDirectoryAsync(ct).ConfigureAwait(false); Check(ct);
        await using var read = await OpenAsync(path, false, ct).ConfigureAwait(false);
        using var readTransaction = read.BeginTransaction(deferred: true);
        Verify(await ReadEntryAsync(read, readTransaction, binding, ct).ConfigureAwait(false) ?? throw new IOException("Recovery mapping is missing."), binding, key.Span);
    }

    /// <summary>Reads a bound existing ciphertext and unwraps through explicitly unlocked private material; no device key is generated.</summary>
    public async Task<byte[]> RecoverAsync(DatabaseKeyBinding binding, DatabaseRecoveryMaterial material, CancellationToken ct = default)
    {
        RequireBinding(binding); ArgumentNullException.ThrowIfNull(material);
        if (material.Identity.RecoveryId != identity.RecoveryId || material.Identity.InstallationId != identity.InstallationId
            || !material.Identity.ExportPublicKey().AsSpan().SequenceEqual(identity.ExportPublicKey())) throw new CryptographicException("Unlocked recovery identity differs.");
        Check(ct); await using var connection = await OpenAsync(path, false, ct).ConfigureAwait(false);
        using var transaction = connection.BeginTransaction(deferred: true);
        var entry = await ReadEntryAsync(connection, transaction, binding, ct).ConfigureAwait(false) ?? throw new IOException("Recovery mapping is missing.");
        var key = material.OpenConfirmed(entry.Envelope, entry.Confirmation, binding);
        try { Check(ct); return key; } catch { CryptographicOperations.ZeroMemory(key); throw; }
    }
    private void Verify((byte[] Envelope, byte[] Confirmation) entry, DatabaseKeyBinding binding, ReadOnlySpan<byte> key)
    {
        if (!CryptographicOperations.FixedTimeEquals(identity.Confirm(key, binding, entry.Envelope), entry.Confirmation))
            throw new CryptographicException("Stored recovery key confirmation differs.");
    }
    private async Task<(byte[] Envelope, byte[] Confirmation)?> ReadEntryAsync(SqliteConnection connection, SqliteTransaction transaction,
        DatabaseKeyBinding binding, CancellationToken ct)
    {
        using var command = connection.CreateCommand(); command.Transaction = transaction;
        command.CommandText = "SELECT length(envelope),length(confirmation),envelope,confirmation FROM recovery_envelope WHERE profile=$profile AND generation=$generation"; Bind(command, binding);
        await using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
        if (!await reader.ReadAsync(ct).ConfigureAwait(false)) return null;
        if (reader.GetInt64(0) != 384 || reader.GetInt64(1) != 32 || reader.GetFieldType(2) != typeof(byte[]) || reader.GetFieldType(3) != typeof(byte[]))
            throw new InvalidDataException("Recovery mapping bounds/types differ.");
        var result = (reader.GetFieldValue<byte[]>(2), reader.GetFieldValue<byte[]>(3));
        if (await reader.ReadAsync(ct).ConfigureAwait(false)) throw new InvalidDataException("Recovery mapping is not unique.");
        Check(ct); return result;
    }
    private async Task<SqliteConnection> OpenAsync(string target, bool creating, CancellationToken ct)
    {
        Check(ct); ValidateDirectory(target);
        if (!File.Exists(target) || (File.GetAttributes(target) & (FileAttributes.ReparsePoint | FileAttributes.Directory)) != 0)
            throw new IOException("Recovery metadata is missing or redirected.");
        if (new FileInfo(target).Length > 8 * 1024 * 1024) throw new IOException("Recovery metadata exceeds its bound.");
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = target, Mode = SqliteOpenMode.ReadWrite, Pooling = false }.ToString());
        try
        {
            await connection.OpenAsync(ct).ConfigureAwait(false);
            using var command = connection.CreateCommand(); command.CommandText = "PRAGMA journal_mode=DELETE";
            if (Convert.ToString(await command.ExecuteScalarAsync(ct).ConfigureAwait(false)) != "delete") throw new IOException("Recovery rollback mode differs.");
            command.CommandText = "PRAGMA synchronous=EXTRA; PRAGMA synchronous";
            if (Convert.ToInt64(await command.ExecuteScalarAsync(ct).ConfigureAwait(false)) != 3) throw new IOException("Recovery metadata synchronization differs.");
            if (!creating) await ValidateSchemaAsync(connection, ct).ConfigureAwait(false);
            Check(ct); return connection;
        }
        catch { await connection.DisposeAsync().ConfigureAwait(false); throw; }
    }
    private async Task ValidateSchemaAsync(SqliteConnection connection, CancellationToken ct)
    {
        using var transaction = connection.BeginTransaction(deferred: true);
        using var command = connection.CreateCommand(); command.Transaction = transaction;
        command.CommandText = "PRAGMA application_id";
        if (Convert.ToInt64(await command.ExecuteScalarAsync(ct).ConfigureAwait(false)) != ApplicationId) throw new InvalidDataException("Foreign recovery metadata.");
        command.CommandText = "PRAGMA user_version";
        if (Convert.ToInt64(await command.ExecuteScalarAsync(ct).ConfigureAwait(false)) != 1) throw new InvalidDataException("Unsupported recovery metadata.");
        command.CommandText = "SELECT count(*) FROM sqlite_schema WHERE name NOT GLOB 'sqlite_*'";
        if (Convert.ToInt64(await command.ExecuteScalarAsync(ct).ConfigureAwait(false)) != 2) throw new InvalidDataException("Unexpected recovery schema.");
        foreach (var table in new[] { ("recovery_identity", IdentitySchema), ("recovery_envelope", EnvelopeSchema) })
        {
            command.CommandText = "SELECT sql FROM sqlite_schema WHERE name=$table"; command.Parameters.Clear(); command.Parameters.AddWithValue("$table", table.Item1);
            if (Convert.ToString(await command.ExecuteScalarAsync(ct).ConfigureAwait(false)) != table.Item2) throw new InvalidDataException("Recovery schema differs.");
        }
        command.Parameters.Clear(); command.CommandText = "SELECT id,installation,recovery,length(public),public FROM recovery_identity";
        await using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
        if (!await reader.ReadAsync(ct).ConfigureAwait(false) || reader.GetFieldType(0) != typeof(long) || reader.GetInt64(0) != 1
            || reader.GetFieldType(1) != typeof(string) || reader.GetString(1) != identity.InstallationId.ToString("N")
            || reader.GetFieldType(2) != typeof(string) || reader.GetString(2) != identity.RecoveryId.ToString("N")
            || reader.GetInt64(3) is <= 0 or > 1024 || reader.GetFieldType(4) != typeof(byte[])
            || !reader.GetFieldValue<byte[]>(4).AsSpan().SequenceEqual(identity.ExportPublicKey()) || await reader.ReadAsync(ct).ConfigureAwait(false))
            throw new InvalidDataException("Recovery public identity differs.");
        Check(ct);
    }
    private static void Bind(SqliteCommand command, DatabaseKeyBinding binding)
    { command.Parameters.AddWithValue("$profile", binding.ProfileId.ToString("N")); command.Parameters.AddWithValue("$generation", binding.GenerationId.ToString("N")); }
    private void RequireBinding(DatabaseKeyBinding binding)
    { ArgumentNullException.ThrowIfNull(binding); if (binding.InstallationId != identity.InstallationId) throw new CryptographicException("Recovery belongs to another installation."); }
    private static void ValidateDirectory(string target)
    { var directory = Path.GetDirectoryName(target)!; if (!Directory.Exists(directory) || (File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0) throw new IOException("Recovery requires trusted private storage."); }
    private void Check(CancellationToken ct) { ct.ThrowIfCancellationRequested(); validate(); }
}
