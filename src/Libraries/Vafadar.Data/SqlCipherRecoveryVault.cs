using Microsoft.Data.Sqlite;
using System.Security.Cryptography;

namespace Vafadar.Data;

/// <summary>Protects optional recovery private material with official SQLCipher and a caller-owned random 256-bit recovery key.</summary>
/// <remarks>The recovery key is never persisted, logged or inferred from device keys. This vault has no financial data,
/// does not repair device envelopes or bypass app authentication, and cannot authorize a missing existing vault.</remarks>
public sealed class SqlCipherRecoveryVault
{
    private const int ApplicationId = 0x565a5256;
    private const string Schema = "CREATE TABLE recovery_private (id INTEGER PRIMARY KEY CHECK(id=1), installation TEXT NOT NULL, recovery TEXT NOT NULL, public BLOB NOT NULL, private BLOB NOT NULL)";
    private readonly string path;
    private readonly DatabaseRecoveryIdentity identity;
    private readonly string version;
    private readonly IDatabaseGenerationFileDurability durability;
    private readonly Action validate;

    /// <summary>Captures trusted private storage, expected non-secret identity and reviewed native cipher version.</summary>
    public SqlCipherRecoveryVault(string path, DatabaseRecoveryIdentity identity, string cipherVersion,
        IDatabaseGenerationFileDurability durability, Action validateAccess)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path); ArgumentNullException.ThrowIfNull(identity);
        ArgumentException.ThrowIfNullOrWhiteSpace(cipherVersion); ArgumentNullException.ThrowIfNull(durability);
        ArgumentNullException.ThrowIfNull(validateAccess);
        if (!Path.IsPathFullyQualified(path)) throw new ArgumentException("Recovery vault requires an absolute private path.");
        this.path = Path.GetFullPath(path); this.identity = identity; version = cipherVersion;
        this.durability = durability; validate = validateAccess;
    }

    /// <summary>Publishes or resumes an exact staged encrypted vault without replacing existing private material.</summary>
    public Task CreateNewAsync(DatabaseRecoveryMaterial material, ReadOnlyMemory<byte> recoveryKey, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(material); RequireKey(recoveryKey.Span);
        if (!SameIdentity(material.Identity)) throw new CryptographicException("Recovery material belongs to another vault.");
        return SqliteMetadataPublication.CreateAsync(path,
            target => CreateCoreAsync(target, material, recoveryKey, ct),
            async target =>
            {
                using var read = await ReadCoreAsync(target, recoveryKey, ct).ConfigureAwait(false);
                var expected = material.ExportPrivateKey(); var actual = read.ExportPrivateKey();
                try { if (!CryptographicOperations.FixedTimeEquals(expected, actual)) throw new CryptographicException("Staged recovery private material differs."); }
                finally { CryptographicOperations.ZeroMemory(expected); CryptographicOperations.ZeroMemory(actual); }
            }, () => Check(ct), durability, ct);
    }

    /// <summary>Unlocks only existing exact private material; wrong recovery keys and corrupt/foreign content fail closed.</summary>
    public Task<DatabaseRecoveryMaterial> OpenAsync(ReadOnlyMemory<byte> recoveryKey, CancellationToken ct = default)
    { RequireKey(recoveryKey.Span); return ReadCoreAsync(path, recoveryKey, ct); }

    private async Task CreateCoreAsync(string target, DatabaseRecoveryMaterial material, ReadOnlyMemory<byte> key, CancellationToken ct)
    {
        Check(ct); ValidatePath(target);
        var privateKey = material.ExportPrivateKey();
        try
        {
            using (var reservation = new FileStream(target, FileMode.CreateNew, FileAccess.Write, FileShare.None)) reservation.Flush(true);
            await using var connection = await OpenConnectionAsync(target, key, creating: true, ct).ConfigureAwait(false);
            using var transaction = connection.BeginTransaction(deferred: false);
            using var command = connection.CreateCommand(); command.Transaction = transaction;
            command.CommandText = $"PRAGMA application_id={ApplicationId}; PRAGMA user_version=1; {Schema}; INSERT INTO recovery_private VALUES(1,$installation,$recovery,$public,$private)";
            command.Parameters.AddWithValue("$installation", identity.InstallationId.ToString("N"));
            command.Parameters.AddWithValue("$recovery", identity.RecoveryId.ToString("N"));
            command.Parameters.AddWithValue("$public", identity.ExportPublicKey()); command.Parameters.AddWithValue("$private", privateKey);
            await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false); Check(ct); transaction.Commit();
        }
        finally { CryptographicOperations.ZeroMemory(privateKey); }
    }
    private async Task<DatabaseRecoveryMaterial> ReadCoreAsync(string target, ReadOnlyMemory<byte> key, CancellationToken ct)
    {
        Check(ct); await using var connection = await OpenConnectionAsync(target, key, creating: false, ct).ConfigureAwait(false);
        using var transaction = connection.BeginTransaction(deferred: true);
        using var command = connection.CreateCommand(); command.Transaction = transaction;
        command.CommandText = "PRAGMA application_id";
        if (Convert.ToInt64(await command.ExecuteScalarAsync(ct).ConfigureAwait(false)) != ApplicationId) throw new InvalidDataException("Foreign recovery vault.");
        command.CommandText = "PRAGMA user_version";
        if (Convert.ToInt64(await command.ExecuteScalarAsync(ct).ConfigureAwait(false)) != 1) throw new InvalidDataException("Unsupported recovery vault.");
        command.CommandText = "SELECT count(*) FROM sqlite_schema WHERE name NOT GLOB 'sqlite_*'";
        if (Convert.ToInt64(await command.ExecuteScalarAsync(ct).ConfigureAwait(false)) != 1) throw new InvalidDataException("Unexpected recovery schema.");
        command.CommandText = "SELECT sql FROM sqlite_schema WHERE name='recovery_private'";
        if (Convert.ToString(await command.ExecuteScalarAsync(ct).ConfigureAwait(false)) != Schema) throw new InvalidDataException("Recovery schema differs.");
        command.CommandText = "SELECT id,installation,recovery,length(public),length(private),public,private FROM recovery_private";
        await using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
        if (!await reader.ReadAsync(ct).ConfigureAwait(false) || reader.GetFieldType(0) != typeof(long) || reader.GetInt64(0) != 1
            || reader.GetFieldType(1) != typeof(string) || reader.GetString(1) != identity.InstallationId.ToString("N")
            || reader.GetFieldType(2) != typeof(string) || reader.GetString(2) != identity.RecoveryId.ToString("N")
            || reader.GetInt64(3) is <= 0 or > 1024 || reader.GetInt64(4) is <= 0 or > 4096
            || reader.GetFieldType(5) != typeof(byte[]) || reader.GetFieldType(6) != typeof(byte[]))
            throw new InvalidDataException("Recovery identity/types/bounds differ.");
        var publicKey = reader.GetFieldValue<byte[]>(5); var privateKey = reader.GetFieldValue<byte[]>(6);
        DatabaseRecoveryMaterial? result = null;
        try
        {
            if (!publicKey.AsSpan().SequenceEqual(identity.ExportPublicKey())) throw new CryptographicException("Recovery public material differs.");
            result = DatabaseRecoveryMaterial.Import(identity, privateKey);
            if (await reader.ReadAsync(ct).ConfigureAwait(false)) throw new InvalidDataException("Recovery vault is not unique.");
            await reader.DisposeAsync().ConfigureAwait(false);
            command.CommandText = "PRAGMA cipher_integrity_check";
            await using (var integrity = await command.ExecuteReaderAsync(ct).ConfigureAwait(false))
                if (await integrity.ReadAsync(ct).ConfigureAwait(false)) throw new CryptographicException("Recovery cipher integrity differs.");
            command.CommandText = "PRAGMA integrity_check";
            if (Convert.ToString(await command.ExecuteScalarAsync(ct).ConfigureAwait(false)) != "ok") throw new InvalidDataException("Recovery database integrity differs.");
            Check(ct); var accepted = result; result = null; return accepted;
        }
        finally { result?.Dispose(); CryptographicOperations.ZeroMemory(privateKey); }
    }
    private async Task<SqliteConnection> OpenConnectionAsync(string target, ReadOnlyMemory<byte> key, bool creating, CancellationToken ct)
    {
        RequireKey(key.Span); Check(ct); ValidatePath(target);
        if (!File.Exists(target) || (File.GetAttributes(target) & (FileAttributes.ReparsePoint | FileAttributes.Directory)) != 0)
            throw new IOException("Recovery vault is missing or redirected.");
        var file = new FileInfo(target);
        if (!creating && file.Length is <= 16 or > 1024 * 1024) throw new IOException("Recovery vault exceeds its bound.");
        if (!creating)
        {
            using var input = new FileStream(target, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var header = new byte[16]; input.ReadExactly(header);
            if (header.AsSpan().SequenceEqual("SQLite format 3\0"u8)) throw new InvalidDataException("Recovery private material must remain encrypted.");
        }
        // Password is supplied as a connection property, never an interpolated SQL command or log.
        // Managed strings cannot be completely erased; the caller still owns/clears the mutable recovery key.
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = target,
            Password = Convert.ToBase64String(key.Span), Mode = creating ? SqliteOpenMode.ReadWrite : SqliteOpenMode.ReadOnly, Pooling = false }.ToString());
        try
        {
            await connection.OpenAsync(ct).ConfigureAwait(false);
            using var command = connection.CreateCommand(); command.CommandText = "PRAGMA cipher_version";
            if ((Convert.ToString(await command.ExecuteScalarAsync(ct).ConfigureAwait(false)) ?? "").Split(' ')[0] != version)
                throw new InvalidDataException("Recovery requires the reviewed native cipher.");
            if (creating)
            {
                command.CommandText = "PRAGMA journal_mode=DELETE";
                if (Convert.ToString(await command.ExecuteScalarAsync(ct).ConfigureAwait(false)) != "delete") throw new IOException("Recovery rollback journal is unavailable.");
                command.CommandText = "PRAGMA synchronous=EXTRA; PRAGMA temp_store=MEMORY; PRAGMA synchronous";
                if (Convert.ToInt64(await command.ExecuteScalarAsync(ct).ConfigureAwait(false)) != 3) throw new IOException("Recovery synchronization is unavailable.");
            }
            Check(ct); return connection;
        }
        catch { await connection.DisposeAsync().ConfigureAwait(false); throw; }
    }
    private bool SameIdentity(DatabaseRecoveryIdentity other) => other.InstallationId == identity.InstallationId
        && other.RecoveryId == identity.RecoveryId && other.ExportPublicKey().AsSpan().SequenceEqual(identity.ExportPublicKey());
    private static void RequireKey(ReadOnlySpan<byte> key)
    { if (key.Length != 32) throw new CryptographicException("Recovery requires a 256-bit caller-owned key."); }
    private void ValidatePath(string target)
    {
        var directory = Path.GetDirectoryName(target)!;
        if (!Directory.Exists(directory) || (File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0)
            throw new IOException("Recovery requires existing trusted private storage.");
    }
    private void Check(CancellationToken ct) { ct.ThrowIfCancellationRequested(); validate(); }
}
