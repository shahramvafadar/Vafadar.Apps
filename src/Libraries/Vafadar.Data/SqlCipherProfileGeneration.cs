using System.Security.Cryptography;
using Microsoft.Data.Sqlite;

namespace Vafadar.Data;

/// <summary>Owns one verified protected generation and its retained mutable device key.</summary>
/// <remarks>Hosts retain this owner while published and drain contexts before disposal. Existing history selects
/// initial or restored composition. Native provider, all-profile startup, recovery UI and retained-file policy are not activated here.</remarks>
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

    /// <summary>Resolves existing stable identities and history before opening the authoritative current generation.</summary>
    /// <remarks>Bootstrap must explicitly create the initial history after verifying initial migration.
    /// A missing history never implies initial selection. The receiver builder supplies captured native effects,
    /// not authority to choose a target: its identity is checked against the latest committed intent.</remarks>
    public static async Task<SqlCipherProfileGeneration> OpenCurrentAsync<TContext>(LocalDatabaseLocation<TContext> location,
        SqliteDatabaseIdentityCatalogue identities, string legacyProfile, string originalPath, SqliteProfileRestoreHistory history,
        Func<ProfileRestoreReference, SqlCipherPackageRestoreReceiver> createReceiver, IDatabaseGenerationKeyStore keys,
        IDatabaseGenerationFileDurability durability, DatabaseAccessGate.ExclusiveLease owner, Action validateAccess,
        int maximumBytes, string cipherVersion, CancellationToken ct = default) where TContext : LocalDbContext
    {
        ArgumentNullException.ThrowIfNull(location); ArgumentNullException.ThrowIfNull(identities);
        ArgumentNullException.ThrowIfNull(history); ArgumentNullException.ThrowIfNull(createReceiver);
        ArgumentNullException.ThrowIfNull(keys); ArgumentNullException.ThrowIfNull(durability);
        ArgumentNullException.ThrowIfNull(owner); ArgumentNullException.ThrowIfNull(validateAccess);
        ArgumentException.ThrowIfNullOrWhiteSpace(originalPath); ArgumentException.ThrowIfNullOrWhiteSpace(cipherVersion);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumBytes);
        void Validate() => Check(location, owner, validateAccess, ct);
        Validate();
        if (!System.IO.Path.IsPathFullyQualified(originalPath)) throw new ArgumentException("Current generation requires an absolute original identity path.");
        var identity = (await identities.ReadAsync(ct).ConfigureAwait(false)).Single(row => row.LegacyProfileId == legacyProfile);
        Validate(); history.RequireInitialBinding(identity.InitialCandidateKey);
        var latest = (await history.ReadAsync(ct).ConfigureAwait(false))[^1]; Validate();
        if (latest.Sequence == 0)
            return await OpenInitialAsync(location, identities, legacyProfile, originalPath, keys, durability, owner,
                validateAccess, maximumBytes, cipherVersion, ct).ConfigureAwait(false);
        var receiver = createReceiver(latest) ?? throw new InvalidOperationException("Current restore receiver is missing.");
        Validate();
        if (receiver.Captured.OriginalGeneration != latest.OriginalGeneration || receiver.Captured.CandidateGeneration != latest.CandidateGeneration ||
            System.IO.Path.GetDirectoryName(receiver.CandidatePath) != System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(originalPath)))
            throw new InvalidOperationException("Current restore receiver differs from the recorded target or private directory.");
        return await OpenRestoredAsync(location, history, receiver, keys, owner, validateAccess, cipherVersion, ct).ConfigureAwait(false);
    }

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

    /// <summary>Resumes the latest recorded restored package and retains its exact existing key for publication.</summary>
    /// <remarks>The host chooses this route whenever restore history contains an intent. Missing or incomplete
    /// state fails closed; never reopen the initial generation as a fallback. Existing contexts must be drained.</remarks>
    public static async Task<SqlCipherProfileGeneration> OpenRestoredAsync<TContext>(LocalDatabaseLocation<TContext> location,
        SqliteProfileRestoreHistory history, SqlCipherPackageRestoreReceiver receiver, IDatabaseGenerationKeyStore keys,
        DatabaseAccessGate.ExclusiveLease owner, Action validateAccess, string cipherVersion, CancellationToken ct = default)
        where TContext : LocalDbContext
    {
        ArgumentNullException.ThrowIfNull(history); ArgumentNullException.ThrowIfNull(receiver);
        ArgumentNullException.ThrowIfNull(keys); ArgumentNullException.ThrowIfNull(location);
        ArgumentNullException.ThrowIfNull(owner); ArgumentNullException.ThrowIfNull(validateAccess);
        ArgumentException.ThrowIfNullOrWhiteSpace(cipherVersion);
        void Validate() => Check(location, owner, validateAccess, ct);
        Validate(); receiver.RequireHistory(history); history.RequireBinding(receiver.Binding);
        await history.RequireAsync(receiver.Captured, ct).ConfigureAwait(false); Validate();
        // Resume verifies actual candidate/rollback and durable preference replay. Publication belongs to
        // this retained key owner, not a receiver callback captured before a cold process restart.
        await receiver.ResumeForGenerationAsync(ct).ConfigureAwait(false); Validate();
        var latest = (await history.ReadAsync(ct).ConfigureAwait(false))[^1];
        if (!latest.Selected || latest.CandidateGeneration != receiver.Binding.GenerationId)
            throw new IOException("Latest restore intent has not completed selected publication prerequisites.");
        var key = await keys.ReadAsync(receiver.Binding, ct).ConfigureAwait(false);
        try
        {
            Validate(); if (key.Length != 32) throw new CryptographicException("Selected restore key is invalid.");
            await using var reopened = new SqliteConnection(new SqliteConnectionStringBuilder
            { DataSource = receiver.CandidatePath, Password = Convert.ToBase64String(key), Mode = SqliteOpenMode.ReadOnly, Pooling = false }.ToString());
            await reopened.OpenAsync(ct).ConfigureAwait(false);
            using var check = reopened.CreateCommand(); check.CommandText = "PRAGMA cipher_version";
            if ((Convert.ToString(await check.ExecuteScalarAsync(ct).ConfigureAwait(false)) ?? "").Split(' ')[0] != cipherVersion)
                throw new InvalidDataException("Restored owner requires the reviewed cipher provider.");
            check.CommandText = "PRAGMA integrity_check";
            if (!string.Equals(await check.ExecuteScalarAsync(ct).ConfigureAwait(false) as string, "ok", StringComparison.Ordinal))
                throw new InvalidDataException("Retained restore key integrity failed.");
            Validate(); await history.RequireAsync(receiver.Captured, ct).ConfigureAwait(false); Validate();
            return new(receiver.CandidatePath, cipherVersion, receiver.Binding, key, validateAccess);
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
        if (location.Path != original && location.Path != Candidate(original, identity))
        {
            var endpoint = location.Capture();
            // D-214: a drained protected host can resolve another existing profile before tentative publication.
            // The actual catalogue supplies its binding; never move the location through a plaintext endpoint.
            if (endpoint.CreateConnection is null || System.IO.Path.GetDirectoryName(endpoint.Path) != System.IO.Path.GetDirectoryName(original))
                throw new InvalidOperationException("Initial bootstrap belongs to another current endpoint or private directory.");
        }
        return (identity, original);
    }

    private static string Journal(string original) => original + ".generation.sqlite";
    private static string Candidate(string original, DatabaseProfileIdentity identity) => original + "." + identity.CandidateGeneration.ToString("N") + ".cipher.db";
    private static void Check<TContext>(LocalDatabaseLocation<TContext> location, DatabaseAccessGate.ExclusiveLease owner, Action validate, CancellationToken ct) where TContext : LocalDbContext
    { ct.ThrowIfCancellationRequested(); location.Access.RequireOwner(owner); validate(); }
}
