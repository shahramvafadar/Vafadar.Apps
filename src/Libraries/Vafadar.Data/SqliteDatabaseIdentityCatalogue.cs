using Microsoft.Data.Sqlite;

namespace Vafadar.Data;

/// <summary>Durable non-secret installation/profile identities and captured initial migration generations.</summary>
/// <remarks>This is identity bootstrap, not a live selector or portable preference. Missing/corrupt state never
/// regenerates on read. Caller owns the trusted private directory and exclusive application maintenance lease.</remarks>
public sealed class SqliteDatabaseIdentityCatalogue
{
    private const int ApplicationId = 0x565a4944;
    private const string InstallationSchema = "CREATE TABLE installation_identity (id INTEGER PRIMARY KEY CHECK(id=1), installation TEXT NOT NULL)";
    private const string ProfileSchema = "CREATE TABLE profile_identity (legacy TEXT PRIMARY KEY NOT NULL, profile TEXT UNIQUE NOT NULL, original TEXT UNIQUE NOT NULL, candidate TEXT UNIQUE NOT NULL)";
    private readonly string path;
    private readonly Action validate;

    /// <summary>Captures a private existing directory/path and current maintenance/access validation.</summary>
    public SqliteDatabaseIdentityCatalogue(string path, Action validateAccess)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path); ArgumentNullException.ThrowIfNull(validateAccess);
        if (!Path.IsPathFullyQualified(path)) throw new ArgumentException("Identity catalogue requires an absolute private path.");
        this.path = Path.GetFullPath(path); validate = validateAccess; ValidateDirectory();
    }

    /// <summary>Explicitly bootstraps one new installation and known legacy profiles, including the empty-id main profile.</summary>
    public async Task CreateNewAsync(IReadOnlyList<string> legacyProfiles, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(legacyProfiles); Check(ct);
        foreach (var legacy in legacyProfiles) ValidateLegacy(legacy);
        if (!legacyProfiles.Contains("", StringComparer.Ordinal) || legacyProfiles.Distinct(StringComparer.Ordinal).Count() != legacyProfiles.Count)
            throw new ArgumentException("Initial identities require exactly one main and unique known profiles.");
        ValidateDirectory();
        foreach (var suffix in new[] { "", "-journal", "-wal", "-shm" })
            if (File.Exists(path + suffix) || Directory.Exists(path + suffix)) throw new IOException("Identity catalogue already exists or has interrupted state.");
        using (var reservation = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None)) reservation.Flush(true);
        await using var connection = await OpenAsync(creating: true, ct).ConfigureAwait(false);
        using var transaction = connection.BeginTransaction(deferred: false);
        using var command = connection.CreateCommand(); command.Transaction = transaction;
        var installation = Guid.NewGuid();
        command.CommandText = $"PRAGMA application_id={ApplicationId}; PRAGMA user_version=1; {InstallationSchema}; {ProfileSchema}; INSERT INTO installation_identity VALUES(1,$installation)";
        command.Parameters.AddWithValue("$installation", installation.ToString("N"));
        await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        foreach (var legacy in legacyProfiles)
            await InsertAsync(connection, transaction, legacy, ct).ConfigureAwait(false);
        Check(ct); transaction.Commit();
        var readback = await ReadAsync(ct).ConfigureAwait(false);
        if (readback.Any(row => row.InstallationId != installation) || readback.Count != legacyProfiles.Count)
            throw new IOException("Identity bootstrap readback differs.");
    }

    /// <summary>Reopens validated existing identities without manufacturing an installation, profile or key.</summary>
    public async Task<IReadOnlyList<DatabaseProfileIdentity>> ReadAsync(CancellationToken ct = default)
    {
        Check(ct); await using var connection = await OpenAsync(false, ct).ConfigureAwait(false);
        using var transaction = connection.BeginTransaction(deferred: true);
        return await ReadCoreAsync(connection, transaction, ct).ConfigureAwait(false);
    }

    /// <summary>Explicitly registers a new known legacy profile; repeat registration retains the exact committed identity.</summary>
    public async Task<DatabaseProfileIdentity> RegisterAsync(string legacyProfile, CancellationToken ct = default)
    {
        ValidateLegacy(legacyProfile); Check(ct);
        await using var connection = await OpenAsync(false, ct).ConfigureAwait(false);
        using var transaction = connection.BeginTransaction(deferred: false);
        var rows = await ReadCoreAsync(connection, transaction, ct).ConfigureAwait(false);
        if (rows.SingleOrDefault(row => row.LegacyProfileId == legacyProfile) is { } existing) return existing;
        await InsertAsync(connection, transaction, legacyProfile, ct).ConfigureAwait(false); Check(ct); transaction.Commit();
        var readback = await ReadAsync(ct).ConfigureAwait(false);
        return readback.Single(row => row.LegacyProfileId == legacyProfile);
    }

    private async Task InsertAsync(SqliteConnection connection, SqliteTransaction transaction, string legacy, CancellationToken ct)
    {
        Check(ct); using var command = connection.CreateCommand(); command.Transaction = transaction;
        command.CommandText = "INSERT INTO profile_identity VALUES($legacy,$profile,$original,$candidate)";
        command.Parameters.AddWithValue("$legacy", legacy); command.Parameters.AddWithValue("$profile", Guid.NewGuid().ToString("N"));
        command.Parameters.AddWithValue("$original", Guid.NewGuid().ToString("N")); command.Parameters.AddWithValue("$candidate", Guid.NewGuid().ToString("N"));
        await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    private async Task<SqliteConnection> OpenAsync(bool creating, CancellationToken ct)
    {
        Check(ct); ValidateDirectory();
        if (!File.Exists(path) || (File.GetAttributes(path) & (FileAttributes.ReparsePoint | FileAttributes.Directory)) != 0)
            throw new IOException("Identity catalogue is missing or redirected.");
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        { DataSource = path, Mode = SqliteOpenMode.ReadWrite, Pooling = false, DefaultTimeout = 5 }.ToString());
        try
        {
            await connection.OpenAsync(ct).ConfigureAwait(false);
            if (!creating)
            {
                using var validation = connection.BeginTransaction(deferred: true);
                await ReadCoreAsync(connection, validation, ct).ConfigureAwait(false);
            }
            Check(ct); using var command = connection.CreateCommand(); command.CommandText = "PRAGMA journal_mode=DELETE";
            if (!string.Equals(Convert.ToString(await command.ExecuteScalarAsync(ct).ConfigureAwait(false)), "delete", StringComparison.OrdinalIgnoreCase))
                throw new IOException("Identity catalogue rollback mode unavailable.");
            command.CommandText = "PRAGMA synchronous=EXTRA; PRAGMA trusted_schema=OFF; PRAGMA synchronous";
            if (Convert.ToInt64(await command.ExecuteScalarAsync(ct).ConfigureAwait(false)) != 3)
                throw new IOException("Identity catalogue durable synchronization unavailable.");
            return connection;
        }
        catch { await connection.DisposeAsync().ConfigureAwait(false); throw; }
    }

    private async Task<IReadOnlyList<DatabaseProfileIdentity>> ReadCoreAsync(SqliteConnection connection, SqliteTransaction transaction, CancellationToken ct)
    {
        Check(ct); using var command = connection.CreateCommand(); command.Transaction = transaction;
        command.CommandText = "PRAGMA application_id";
        if (Convert.ToInt64(await command.ExecuteScalarAsync(ct).ConfigureAwait(false)) != ApplicationId) throw new IOException("Foreign identity catalogue.");
        command.CommandText = "PRAGMA user_version";
        if (Convert.ToInt64(await command.ExecuteScalarAsync(ct).ConfigureAwait(false)) != 1) throw new IOException("Unsupported identity catalogue.");
        command.CommandText = "SELECT count(*) FROM sqlite_schema WHERE name NOT GLOB 'sqlite_*'";
        if (Convert.ToInt64(await command.ExecuteScalarAsync(ct).ConfigureAwait(false)) != 2) throw new IOException("Identity catalogue has extra schema.");
        foreach (var table in new[] { ("installation_identity", InstallationSchema), ("profile_identity", ProfileSchema) })
        {
            command.CommandText = "SELECT sql FROM sqlite_schema WHERE type='table' AND name=$name";
            command.Parameters.Clear(); command.Parameters.AddWithValue("$name", table.Item1);
            if (Convert.ToString(await command.ExecuteScalarAsync(ct).ConfigureAwait(false)) != table.Item2) throw new IOException("Identity catalogue schema differs.");
        }
        command.Parameters.Clear(); command.CommandText = "SELECT id,installation FROM installation_identity";
        Guid installation;
        await using (var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false))
        {
            if (!await reader.ReadAsync(ct).ConfigureAwait(false) || reader.GetFieldType(0) != typeof(long) || reader.GetInt64(0) != 1 || reader.GetFieldType(1) != typeof(string))
                throw new IOException("Installation identity missing or incorrectly typed.");
            installation = Parse(reader.GetString(1));
            if (await reader.ReadAsync(ct).ConfigureAwait(false)) throw new IOException("Installation identity is not unique.");
        }
        command.CommandText = "SELECT legacy,profile,original,candidate FROM profile_identity ORDER BY legacy";
        var rows = new List<DatabaseProfileIdentity>();
        await using (var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false))
        {
            while (await reader.ReadAsync(ct).ConfigureAwait(false))
            {
                if (Enumerable.Range(0, 4).Any(column => reader.GetFieldType(column) != typeof(string)))
                    throw new IOException("Profile identity is incorrectly typed.");
                var legacy = reader.GetString(0); ValidateLegacy(legacy);
                var profile = Parse(reader.GetString(1)); var original = Parse(reader.GetString(2)); var candidate = Parse(reader.GetString(3));
                if (original == candidate) throw new IOException("Initial generations must differ.");
                rows.Add(new(installation, legacy, profile, original, candidate));
            }
        }
        if (!rows.Any(row => row.LegacyProfileId.Length == 0)) throw new IOException("Main profile identity missing.");
        Check(ct); return rows.AsReadOnly();
    }

    private static Guid Parse(string value)
    {
        if (!Guid.TryParseExact(value, "N", out var id) || id == Guid.Empty || value != id.ToString("N")) throw new IOException("Identity is invalid or noncanonical.");
        return id;
    }
    private static void ValidateLegacy(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.Length != 0) _ = Parse(value);
    }
    private void Check(CancellationToken ct) { ct.ThrowIfCancellationRequested(); validate(); }
    private void ValidateDirectory()
    {
        var parent = Path.GetDirectoryName(path)!;
        if (!Directory.Exists(parent) || (File.GetAttributes(parent) & FileAttributes.ReparsePoint) != 0) throw new IOException("Private identity directory is missing or redirected.");
    }
}

/// <summary>Stable local bootstrap identities; initial generations are not a live database selector.</summary>
/// <param name="InstallationId">Private non-portable installation identity.</param>
/// <param name="LegacyProfileId">Empty for main or canonical existing local profile id.</param>
/// <param name="ProfileId">Nonempty device-key profile binding.</param>
/// <param name="OriginalGeneration">Captured original generation for first migration.</param>
/// <param name="CandidateGeneration">Captured distinct candidate for first migration.</param>
public sealed record DatabaseProfileIdentity(Guid InstallationId, string LegacyProfileId, Guid ProfileId, Guid OriginalGeneration, Guid CandidateGeneration)
{
    /// <summary>Gets the exact initial journal identity; its actual stored phase must still be reopened/verified.</summary>
    public DatabaseGenerationCheckpoint InitialMigration => new(ProfileId, OriginalGeneration, CandidateGeneration);
    /// <summary>Gets candidate key context, never a key or authority to create a replacement.</summary>
    public DatabaseKeyBinding InitialCandidateKey => new(ProfileId, InstallationId, CandidateGeneration);
}
