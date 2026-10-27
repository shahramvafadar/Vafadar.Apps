using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Data.Sqlite;
using Vafadar.Core.Settings;

namespace Vafadar.Data;

/// <summary>Retains original and restored allowlisted preferences in an encrypted, generation-bound restore capsule.</summary>
/// <remarks>Pending capsules replay before unlock. Once actual preference persistence is durably marked, later
/// user edits are retained. The caller retains this capsule and its key with the database rollback.</remarks>
public sealed class SqlCipherRestorePreferences
{
    private readonly string path;
    private readonly DatabaseKeyBinding binding;
    private readonly IDatabaseGenerationKeyStore keys;
    private readonly IDatabaseGenerationFileDurability durability;
    private readonly string cipherVersion;
    private readonly string[] allowed;
    private const int MaximumBytes = 1024 * 1024;

    /// <summary>Captures private capsule identity and the application's explicit portable preference allowlist.</summary>
    public SqlCipherRestorePreferences(string path, DatabaseKeyBinding binding, IDatabaseGenerationKeyStore keys,
        IDatabaseGenerationFileDurability durability, string cipherVersion, IReadOnlyList<string> allowed)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path); ArgumentNullException.ThrowIfNull(binding);
        ArgumentNullException.ThrowIfNull(keys); ArgumentNullException.ThrowIfNull(durability);
        ArgumentException.ThrowIfNullOrWhiteSpace(cipherVersion); ArgumentNullException.ThrowIfNull(allowed);
        if (!Path.IsPathFullyQualified(path)) throw new ArgumentException("Restore capsule requires an absolute private path.");
        if (allowed.Any(string.IsNullOrWhiteSpace) || allowed.Distinct(StringComparer.Ordinal).Count() != allowed.Count)
            throw new ArgumentException("Portable preference allowlist is invalid.");
        this.path = Path.GetFullPath(path); this.binding = binding; this.keys = keys; this.durability = durability;
        this.cipherVersion = cipherVersion; this.allowed = [.. allowed];
    }

    /// <summary>Validates portable JSON and captures existing values without changing the preference store.</summary>
    public RestorePreferencePacket Capture(ReadOnlyMemory<byte>? portable, ISettingsStore settings) =>
        CapturePortable(portable, settings, allowed);

    /// <summary>Captures the optional original preference source from a validated complete package.</summary>
    public RestorePreferencePacket CapturePackage(IReadOnlyDictionary<string, ReadOnlyMemory<byte>> entries, ISettingsStore settings) =>
        CapturePackagePreferences(entries, settings, allowed);

    /// <summary>Keeps an omitted source distinct from a present malformed or empty JSON payload.</summary>
    public static RestorePreferencePacket CapturePackagePreferences(IReadOnlyDictionary<string, ReadOnlyMemory<byte>> entries,
        ISettingsStore settings, IReadOnlyList<string> allowed)
    {
        ArgumentNullException.ThrowIfNull(entries);
        // ReadOnlyMemory has an implicit array conversion: a conditional null arm can
        // become present empty memory. An omitted optional source must stay nullable null.
        ReadOnlyMemory<byte>? portable = null;
        if (entries.TryGetValue("display-settings.json", out var display)) portable = display;
        return CapturePortable(portable, settings, allowed);
    }

    /// <summary>Validates and snapshots only explicit portable preferences before any native recovery effects.</summary>
    public static RestorePreferencePacket CapturePortable(ReadOnlyMemory<byte>? portable, ISettingsStore settings,
        IReadOnlyList<string> allowed)
    {
        ArgumentNullException.ThrowIfNull(allowed);
        if (allowed.Any(string.IsNullOrWhiteSpace) || allowed.Distinct(StringComparer.Ordinal).Count() != allowed.Count)
            throw new ArgumentException("Portable preference allowlist is invalid.");
        ArgumentNullException.ThrowIfNull(settings);
        var imported = new Dictionary<string, string?>(StringComparer.Ordinal);
        if (portable is { } bytes)
        {
            if (bytes.Length > MaximumBytes) throw new InvalidDataException("Portable preferences exceed the restore bound.");
            imported = JsonSerializer.Deserialize(bytes.Span, RestorePreferencesJsonContext.Default.DictionaryStringString)
                ?? throw new InvalidDataException("Portable preferences are missing.");
        }
        var original = new Dictionary<string, string?>(StringComparer.Ordinal);
        var restored = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var name in allowed)
        {
            var value = settings.Get(name); original.Add(name, value);
            // Old packages and omitted keys keep their current value, including explicit null removal.
            restored.Add(name, imported.TryGetValue(name, out var incoming) ? incoming : value);
        }
        return new(original, restored);
    }

    /// <summary>Creates a new protected capsule after the candidate key exists; never replaces interrupted state.</summary>
    public async Task CreateNewAsync(RestorePreferencePacket packet, Action validateAccess, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(packet); ArgumentNullException.ThrowIfNull(validateAccess);
        ValidatePacket(packet); Check(validateAccess, ct); ValidatePath();
        foreach (var suffix in new[] { "", "-journal", "-wal", "-shm" })
            if (File.Exists(path + suffix) || Directory.Exists(path + suffix)) throw new IOException("Restore capsule already exists.");
        var before = JsonSerializer.SerializeToUtf8Bytes(packet.Original, RestorePreferencesJsonContext.Default.DictionaryStringString);
        var after = JsonSerializer.SerializeToUtf8Bytes(packet.Restored, RestorePreferencesJsonContext.Default.DictionaryStringString);
        try
        {
            if (before.Length > MaximumBytes || after.Length > MaximumBytes) throw new InvalidDataException("Captured preferences exceed the restore bound.");
            using (var reservation = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None)) reservation.Flush(true);
            await using (var connection = await OpenAsync(creating: true, ct).ConfigureAwait(false))
            {
                Check(validateAccess, ct);
                using var transaction = connection.BeginTransaction(deferred: false);
                using var command = connection.CreateCommand(); command.Transaction = transaction;
                command.CommandText = "PRAGMA application_id=1448751696; PRAGMA user_version=2; " +
                    "CREATE TABLE restore_preferences(id INTEGER PRIMARY KEY CHECK(id=1), binding TEXT NOT NULL, original BLOB NOT NULL, restored BLOB NOT NULL, applied INTEGER NOT NULL CHECK(applied BETWEEN 0 AND 1)); " +
                    "INSERT INTO restore_preferences VALUES(1,$binding,$original,$restored,0)";
                command.Parameters.AddWithValue("$binding", Identity()); command.Parameters.AddWithValue("$original", before);
                command.Parameters.AddWithValue("$restored", after); await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                Check(validateAccess, ct); transaction.Commit();
            }
            await durability.SynchronizeFileAsync(path, ct).ConfigureAwait(false); Check(validateAccess, ct);
            var reopened = await ReadAsync(validateAccess, ct).ConfigureAwait(false);
            if (!Equal(packet.Original, reopened.Original) || !Equal(packet.Restored, reopened.Restored))
                throw new IOException("Restore preference readback differs.");
        }
        finally { CryptographicOperations.ZeroMemory(before); CryptographicOperations.ZeroMemory(after); }
    }

    /// <summary>Reads only existing keyed state; wrong context, malformed content or missing key fails closed.</summary>
    public async Task<RestorePreferencePacket> ReadAsync(Action validateAccess, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(validateAccess); Check(validateAccess, ct);
        await using var connection = await OpenAsync(false, ct).ConfigureAwait(false); Check(validateAccess, ct);
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA application_id";
        if (Convert.ToInt64(await command.ExecuteScalarAsync(ct).ConfigureAwait(false)) != 1448751696) throw new InvalidDataException("Foreign restore capsule.");
        command.CommandText = "PRAGMA user_version";
        if (Convert.ToInt64(await command.ExecuteScalarAsync(ct).ConfigureAwait(false)) != 2) throw new InvalidDataException("Unsupported restore capsule.");
        command.CommandText = "SELECT id,binding,length(original),length(restored),original,restored,applied FROM restore_preferences";
        await using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
        if (!await reader.ReadAsync(ct).ConfigureAwait(false) || reader.GetInt64(0) != 1 || reader.GetString(1) != Identity() ||
            reader.GetInt64(2) is <= 0 or > MaximumBytes || reader.GetInt64(3) is <= 0 or > MaximumBytes ||
            reader.GetFieldType(6) != typeof(long) || reader.GetInt64(6) is < 0 or > 1)
            throw new InvalidDataException("Restore capsule identity or bounds differ.");
        var before = reader.GetFieldValue<byte[]>(4); var after = reader.GetFieldValue<byte[]>(5);
        try
        {
            var packet = new RestorePreferencePacket(
                JsonSerializer.Deserialize(before, RestorePreferencesJsonContext.Default.DictionaryStringString) ?? throw new InvalidDataException("Original preferences missing."),
                JsonSerializer.Deserialize(after, RestorePreferencesJsonContext.Default.DictionaryStringString) ?? throw new InvalidDataException("Restored preferences missing."), reader.GetInt64(6) == 1);
            ValidatePacket(packet);
            if (await reader.ReadAsync(ct).ConfigureAwait(false)) throw new InvalidDataException("Restore capsule has extra rows.");
            Check(validateAccess, ct); return packet;
        }
        finally { CryptographicOperations.ZeroMemory(before); CryptographicOperations.ZeroMemory(after); }
    }

    /// <summary>Replays the durable selected preference packet while application access is covered/exclusive.</summary>
    public void Apply(RestorePreferencePacket packet, ISettingsStore settings, Action validateAccess, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(settings); ArgumentNullException.ThrowIfNull(validateAccess); ValidatePacket(packet);
        Check(validateAccess, ct);
        if (packet.Applied) return;
        foreach (var name in allowed) { Check(validateAccess, ct); settings.Set(name, packet.Restored[name]); }
        Check(validateAccess, ct);
        if (allowed.Any(name => settings.Get(name) != packet.Restored[name])) throw new IOException("Restored preference publication differs.");
    }

    /// <summary>Durably records one-time replay only after the caller's actual preference persistence succeeds.</summary>
    internal async Task MarkAppliedAsync(Action validateAccess, CancellationToken ct)
    {
        _ = await ReadAsync(validateAccess, ct).ConfigureAwait(false); Check(validateAccess, ct);
        await using (var connection = await OpenAsync(false, ct).ConfigureAwait(false))
        {
            using var mode = connection.CreateCommand(); mode.CommandText = "PRAGMA journal_mode=DELETE";
            if (!string.Equals(Convert.ToString(await mode.ExecuteScalarAsync(ct).ConfigureAwait(false)), "delete", StringComparison.OrdinalIgnoreCase)) throw new IOException("Replay marker rollback mode unavailable.");
            mode.CommandText = "PRAGMA synchronous=EXTRA; PRAGMA synchronous";
            if (Convert.ToInt64(await mode.ExecuteScalarAsync(ct).ConfigureAwait(false)) != 3) throw new IOException("Replay marker synchronization unavailable.");
            using var transaction = connection.BeginTransaction(deferred: false); using var command = connection.CreateCommand(); command.Transaction = transaction;
            command.CommandText = "UPDATE restore_preferences SET applied=1 WHERE id=1 AND binding=$binding";
            command.Parameters.AddWithValue("$binding", Identity());
            if (await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false) != 1) throw new IOException("Replay marker identity changed.");
            Check(validateAccess, ct); transaction.Commit();
        }
        await durability.SynchronizeFileAsync(path, ct).ConfigureAwait(false); Check(validateAccess, ct);
        if (!(await ReadAsync(validateAccess, ct).ConfigureAwait(false)).Applied) throw new IOException("Replay marker readback differs.");
    }

    private async Task<SqliteConnection> OpenAsync(bool creating, CancellationToken ct)
    {
        ValidatePath();
        if (!File.Exists(path) || (File.GetAttributes(path) & (FileAttributes.ReparsePoint | FileAttributes.Directory)) != 0)
            throw new IOException("Restore capsule is missing or redirected.");
        if (!creating)
        {
            using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var header = new byte[16]; file.ReadExactly(header);
            if (header.AsSpan().SequenceEqual("SQLite format 3\0"u8)) throw new InvalidDataException("Restore capsule must be encrypted.");
        }
        var key = await keys.ReadAsync(binding, ct).ConfigureAwait(false);
        SqliteConnection? connection = null;
        try
        {
            if (key.Length != 32) throw new CryptographicException("Restore capsule key is invalid.");
            connection = new(new SqliteConnectionStringBuilder { DataSource = path, Password = Convert.ToBase64String(key),
                Mode = SqliteOpenMode.ReadWrite, Pooling = false }.ToString());
            await connection.OpenAsync(ct).ConfigureAwait(false);
            using var command = connection.CreateCommand(); command.CommandText = "PRAGMA cipher_version";
            if ((Convert.ToString(await command.ExecuteScalarAsync(ct).ConfigureAwait(false)) ?? "").Split(' ')[0] != cipherVersion)
                throw new InvalidDataException("Restore capsule requires reviewed cipher identity.");
            // Existing state is read only at the SQL level, including foreign binding/schema validation.
            if (!creating) return connection;
            command.CommandText = "PRAGMA journal_mode=DELETE";
            if (!string.Equals(Convert.ToString(await command.ExecuteScalarAsync(ct).ConfigureAwait(false)), "delete", StringComparison.OrdinalIgnoreCase))
                throw new IOException("Restore capsule rollback journal unavailable.");
            command.CommandText = "PRAGMA synchronous=EXTRA; PRAGMA trusted_schema=OFF; PRAGMA synchronous";
            if (Convert.ToInt64(await command.ExecuteScalarAsync(ct).ConfigureAwait(false)) != 3) throw new IOException("Restore capsule synchronization unavailable.");
            return connection;
        }
        catch { if (connection is not null) await connection.DisposeAsync().ConfigureAwait(false); throw; }
        finally { CryptographicOperations.ZeroMemory(key); }
    }

    private void ValidatePath()
    {
        var directory = Path.GetDirectoryName(path)!;
        if (!Directory.Exists(directory) || (File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0)
            throw new IOException("Private restore capsule directory is missing or redirected.");
    }
    private string Identity() => $"{binding.ProfileId:N}|{binding.InstallationId:N}|{binding.GenerationId:N}";
    private void ValidatePacket(RestorePreferencePacket packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        if (packet.Original.Count != allowed.Length || packet.Restored.Count != allowed.Length ||
            allowed.Any(name => !packet.Original.ContainsKey(name) || !packet.Restored.ContainsKey(name)))
            throw new InvalidDataException("Restore capsule preference allowlist differs.");
    }
    private static bool Equal(Dictionary<string, string?> first, Dictionary<string, string?> second) =>
        first.Count == second.Count && first.All(item => second.TryGetValue(item.Key, out var value) && item.Value == value);
    private static void Check(Action validate, CancellationToken ct) { ct.ThrowIfCancellationRequested(); validate(); }
}

/// <summary>Owned allowlisted previous/restored values and durable replay state; contains no device security or credentials.</summary>
/// <param name="Original">Captured previous values.</param>
/// <param name="Restored">Captured restored values.</param>
/// <param name="Applied">Actual capsule replay marker; new capsule creation always starts pending.</param>
public sealed record RestorePreferencePacket(Dictionary<string, string?> Original, Dictionary<string, string?> Restored, bool Applied = false);

[JsonSerializable(typeof(Dictionary<string, string?>))]
internal sealed partial class RestorePreferencesJsonContext : JsonSerializerContext;
