using System.Security.Cryptography;
using Microsoft.Data.Sqlite;

namespace Vafadar.Data;

/// <summary>Owns one verified initial protected generation and its retained mutable device key.</summary>
/// <remarks>Hosts retain this owner while published, drain contexts before disposal and separately resolve later
/// restored generations. Native provider, all-profile startup, PIN/recovery UI and retained-file policy are not activated here.</remarks>
public sealed class SqlCipherProfileGeneration : IDisposable
{
    private readonly Lock sync = new();
    private byte[]? key;
    private readonly Action validate;

    private SqlCipherProfileGeneration(string path, string cipherVersion, DatabaseKeyBinding binding, byte[] key, Action validate)
    { Path = path; CipherVersion = cipherVersion; Binding = binding; this.key = key; this.validate = validate; }

    /// <summary>The exact existing keyed candidate, never a fallback original.</summary>
    public string Path { get; }
    /// <summary>The reviewed provider identity for factory reopen.</summary>
    public string CipherVersion { get; }
    /// <summary>The existing device key context; no key bytes are exposed.</summary>
    public DatabaseKeyBinding Binding { get; }

    /// <summary>Explicitly creates a first-migration journal only while the original is plaintext and no candidate/rollback exists.</summary>
    public static async Task InitializeJournalAsync<TContext>(LocalDatabaseLocation<TContext> location,
        SqliteDatabaseIdentityCatalogue identities, string legacyProfile, string originalPath,
        DatabaseAccessGate.ExclusiveLease owner, Action validateAccess, CancellationToken ct = default) where TContext : LocalDbContext
    {
        var state = await CaptureAsync(location, identities, legacyProfile, originalPath, owner, validateAccess, ct).ConfigureAwait(false);
        var candidate = Candidate(state.Original, state.Identity);
        foreach (var path in new[] { candidate, candidate + ".rollback.db" })
            if (File.Exists(path) || Directory.Exists(path)) throw new IOException("Existing ciphertext requires its captured journal, not initialization.");
        if (!File.Exists(state.Original) || (File.GetAttributes(state.Original) & FileAttributes.ReparsePoint) != 0)
            throw new IOException("Captured plaintext original is missing or redirected.");
        using (var file = new FileStream(state.Original, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        { var header = new byte[16]; file.ReadExactly(header); if (!header.AsSpan().SequenceEqual("SQLite format 3\0"u8)) throw new InvalidDataException("Journal initialization requires the known plaintext original."); }
        Check(location, owner, validateAccess, ct);
        await new SqliteDatabaseGenerationJournal(Journal(state.Original), state.Identity.InitialMigration).CreateNewAsync(ct).ConfigureAwait(false);
        Check(location, owner, validateAccess, ct);
    }

    /// <summary>Resumes only existing journal state, performs actual migration/keyed cleanup and retains the reopened key for publication.</summary>
    public static async Task<SqlCipherProfileGeneration> OpenInitialAsync<TContext>(LocalDatabaseLocation<TContext> location,
        SqliteDatabaseIdentityCatalogue identities, string legacyProfile, string originalPath, IDatabaseGenerationKeyStore keys,
        IDatabaseGenerationFileDurability durability, DatabaseAccessGate.ExclusiveLease owner, Action validateAccess,
        int maximumBytes, string cipherVersion, CancellationToken ct = default) where TContext : LocalDbContext
    {
        ArgumentNullException.ThrowIfNull(keys); ArgumentNullException.ThrowIfNull(durability);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumBytes); ArgumentException.ThrowIfNullOrWhiteSpace(cipherVersion);
        var state = await CaptureAsync(location, identities, legacyProfile, originalPath, owner, validateAccess, ct).ConfigureAwait(false);
        void Validate() { Check(location, owner, validateAccess, ct); }
        var journal = new SqliteDatabaseGenerationJournal(Journal(state.Original), state.Identity.InitialMigration);
        var session = new SqlCipherGenerationMigrationSession(state.Identity.InitialMigration, state.Identity.InstallationId,
            state.Original, journal, keys, durability, Validate, maximumBytes, cipherVersion);
        await DatabaseGenerationMigration.ResumeAsync(state.Identity.InitialMigration, session, ct).ConfigureAwait(false); Validate();
        var key = await keys.ReadAsync(state.Identity.InitialCandidateKey, ct).ConfigureAwait(false);
        try
        {
            Validate(); if (key.Length != 32) throw new CryptographicException("Selected generation key is invalid.");
            // Verify the exact retained read, not merely a previous temporary key read made by the session.
            await using var reopened = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = session.CandidatePath,
                Password = Convert.ToBase64String(key), Mode = SqliteOpenMode.ReadOnly, Pooling = false }.ToString());
            await reopened.OpenAsync(ct).ConfigureAwait(false);
            using var check = reopened.CreateCommand(); check.CommandText = "PRAGMA cipher_version";
            if ((Convert.ToString(await check.ExecuteScalarAsync(ct).ConfigureAwait(false)) ?? "").Split(' ')[0] != cipherVersion)
                throw new InvalidDataException("Retained key requires the reviewed cipher provider.");
            check.CommandText = "PRAGMA integrity_check";
            if (!string.Equals(await check.ExecuteScalarAsync(ct).ConfigureAwait(false) as string, "ok", StringComparison.Ordinal))
                throw new InvalidDataException("Retained key generation integrity failed.");
            Validate();
            return new(session.CandidatePath, cipherVersion, state.Identity.InitialCandidateKey, key, validateAccess);
        }
        catch { CryptographicOperations.ZeroMemory(key); throw; }
    }

    /// <summary>Creates a closed unpooled existing-only keyed connection for the original protected factory.</summary>
    public SqliteConnection CreateConnection()
    {
        lock (sync)
        {
            validate(); ObjectDisposedException.ThrowIf(key is null, this);
            return new(new SqliteConnectionStringBuilder { DataSource = Path, Password = Convert.ToBase64String(key),
                Mode = SqliteOpenMode.ReadWrite, Pooling = false, ForeignKeys = true }.ToString());
        }
    }

    /// <summary>Publishes verified path/key construction together under the actual location's current maintenance owner.</summary>
    public void Publish<TContext>(LocalDatabaseLocation<TContext> location, DatabaseAccessGate.ExclusiveLease owner) where TContext : LocalDbContext
    {
        ArgumentNullException.ThrowIfNull(location); ArgumentNullException.ThrowIfNull(owner);
        lock (sync)
        {
            validate(); ObjectDisposedException.ThrowIf(key is null, this);
            location.MoveToProtected(Path, CreateConnection, CipherVersion, owner);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        lock (sync) { if (key is not null) CryptographicOperations.ZeroMemory(key); key = null; }
    }

    private static async Task<(DatabaseProfileIdentity Identity, string Original)> CaptureAsync<TContext>(LocalDatabaseLocation<TContext> location,
        SqliteDatabaseIdentityCatalogue identities, string legacy, string originalPath, DatabaseAccessGate.ExclusiveLease owner,
        Action validate, CancellationToken ct) where TContext : LocalDbContext
    {
        ArgumentNullException.ThrowIfNull(location); ArgumentNullException.ThrowIfNull(identities); ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(validate); ArgumentException.ThrowIfNullOrWhiteSpace(originalPath);
        Check(location, owner, validate, ct);
        if (!System.IO.Path.IsPathFullyQualified(originalPath) || System.IO.Path.GetExtension(originalPath) != ".db") throw new ArgumentException("Capture an absolute owned original database path.");
        var original = System.IO.Path.GetFullPath(originalPath);
        var identity = (await identities.ReadAsync(ct).ConfigureAwait(false)).Single(row => row.LegacyProfileId == legacy);
        Check(location, owner, validate, ct);
        if (location.Path != original && location.Path != Candidate(original, identity)) throw new InvalidOperationException("Initial bootstrap belongs to another current endpoint.");
        return (identity, original);
    }

    private static string Journal(string original) => original + ".generation.sqlite";
    private static string Candidate(string original, DatabaseProfileIdentity identity) => original + "." + identity.CandidateGeneration.ToString("N") + ".cipher.db";
    private static void Check<TContext>(LocalDatabaseLocation<TContext> location, DatabaseAccessGate.ExclusiveLease owner, Action validate, CancellationToken ct) where TContext : LocalDbContext
    { ct.ThrowIfCancellationRequested(); location.Access.RequireOwner(owner); validate(); }
}
