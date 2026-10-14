using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Vafadar.Data;
using Vafadar.Core.Settings;
using Vafadar.Zanance.App.Profiles;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.CompiledModel;

namespace Vafadar.Zanance.App.Startup;

/// <summary>Composes original application startup, profile switching and new-profile preparation through one protected session.</summary>
/// <remarks>Native registration supplies its anchored directory, real key/durability effects and generation-aware
/// package receiver. This host does not infer installation authority, activate a native provider or delete retained files.</remarks>
public sealed class ProtectedProfileStorage
{
    private readonly LocalDatabaseLocation<ZananceDbContext> location;
    private readonly Func<ProfileService> profiles;
    private readonly string identityPath;
    private readonly IDatabaseGenerationKeyStore keys;
    private readonly IDatabaseGenerationFileDurability durability;
    private readonly Action validateDirectory;
    private readonly Func<string, ProfileRestoreReference, DatabaseAccessGate.ExclusiveLease, SqlCipherPackageRestoreReceiver> createReceiver;
    private readonly Func<DatabaseProfileIdentity, string, ProfileRestoreReference, DatabaseAccessGate.ExclusiveLease, SqlCipherPackageRestoreReceiver>? createBoundReceiver;
    private readonly int maximumBytes;
    private readonly string cipherVersion;

    /// <summary>Captures explicit native effects without resolving profiles, opening a database or preparing a key.</summary>
    public ProtectedProfileStorage(LocalDatabaseLocation<ZananceDbContext> location, Func<ProfileService> profiles,
        string identityPath, IDatabaseGenerationKeyStore keys, IDatabaseGenerationFileDurability durability,
        Action validateDirectory,
        Func<string, ProfileRestoreReference, DatabaseAccessGate.ExclusiveLease, SqlCipherPackageRestoreReceiver> createReceiver,
        int maximumBytes, string cipherVersion)
    {
        ArgumentNullException.ThrowIfNull(location); ArgumentNullException.ThrowIfNull(profiles);
        ArgumentNullException.ThrowIfNull(keys); ArgumentNullException.ThrowIfNull(durability);
        ArgumentNullException.ThrowIfNull(validateDirectory); ArgumentNullException.ThrowIfNull(createReceiver);
        ArgumentException.ThrowIfNullOrWhiteSpace(identityPath); ArgumentException.ThrowIfNullOrWhiteSpace(cipherVersion);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumBytes);
        if (!Path.IsPathFullyQualified(identityPath) || Path.GetDirectoryName(Path.GetFullPath(identityPath)) != Path.GetDirectoryName(location.Path))
            throw new ArgumentException("Protected identities must remain in the captured private directory.");
        if (!location.RequiresProtection) throw new InvalidOperationException("Protected host registration must refuse unpublished plaintext contexts.");
        this.location = location; this.profiles = profiles; this.identityPath = Path.GetFullPath(identityPath);
        this.keys = keys; this.durability = durability; this.validateDirectory = validateDirectory;
        this.createReceiver = createReceiver; this.maximumBytes = maximumBytes; this.cipherVersion = cipherVersion;
        Session = new(location, ResolveAsync, PrepareNewAsync);
    }

    /// <summary>Composes recorded restore receivers with real settings persistence and this same publication session.</summary>
    public ProtectedProfileStorage(LocalDatabaseLocation<ZananceDbContext> location, Func<ProfileService> profiles,
        string identityPath, IDatabaseGenerationKeyStore keys, IDatabaseGenerationFileDurability durability,
        Action validateDirectory, ISettingsStore settings, ISettingsPersistence persistence, IReadOnlyList<string> portableKeys,
        int maximumBytes, string cipherVersion)
        : this(location, profiles, identityPath, keys, durability, validateDirectory,
            (_, _, _) => throw new InvalidOperationException("Recorded restore requires its captured stable identity."), maximumBytes, cipherVersion)
    {
        ArgumentNullException.ThrowIfNull(settings); ArgumentNullException.ThrowIfNull(persistence); ArgumentNullException.ThrowIfNull(portableKeys);
        var allowlist = portableKeys.ToArray();
        createBoundReceiver = (identity, original, reference, owner) =>
        {
            void Validate()
            {
                location.Access.RequireDrainedOwner(owner); validateDirectory();
                var capture = profiles().CaptureStorage(); capture.Validate();
                if (!capture.OriginalPaths.TryGetValue(identity.LegacyProfileId, out var path) || path != original)
                    throw new InvalidOperationException("Restored profile differs from its captured original.");
            }
            var candidate = original + "." + reference.CandidateGeneration.ToString("N") + ".cipher.db";
            var binding = new DatabaseKeyBinding(identity.ProfileId, identity.InstallationId, reference.CandidateGeneration);
            return SqlCipherRetainedRestore.Create(identity, original, reference, keys, durability, settings, allowlist,
                Validate, UpgradeAsync, ct => Session.AdoptRestoredAsync(original, candidate, binding, owner, ct),
                persistence.PersistAsync, maximumBytes, cipherVersion);
        };
    }

    /// <summary>The single key owner shared by startup, switching and exact restored-generation adoption.</summary>
    public SqlCipherProfileSession<ZananceDbContext> Session { get; }

    /// <summary>Connects explicit installation authority and actual profile lifecycle hosts to covered startup.</summary>
    public IAppDatabaseStartup CreateStartup(ProtectedStorageInitialization initialization,
        IProfileRetirementHost? retirement = null, IProfileCreationHost? creation = null)
    {
        return new ProtectedDatabaseStartup(location, profiles(), Session, identityPath, initialization, keys, durability,
            (_, _) => throw new InvalidOperationException("Protected restore requires the current drained startup owner."),
            maximumBytes, cipherVersion, UpgradeAsync, retirement, creation, createReceiver, createBoundReceiver);
    }

    /// <summary>Records an explicit active-profile restore before staging, retaining exact history and native synchronization.</summary>
    public async Task<SqlCipherPackageRestoreReceiver> PrepareRestoreAsync(DatabaseAccessGate.ExclusiveLease owner, CancellationToken ct = default)
    {
        Session.RequireStable(owner); validateDirectory();
        if (createBoundReceiver is null) throw new InvalidOperationException("Protected package receiver is not composed.");
        var capture = profiles().CaptureStorage(); capture.Validate();
        var original = capture.CurrentOriginalPath;
        var legacy = capture.OriginalPaths.Single(pair => pair.Value == original).Key;
        void Check() { ct.ThrowIfCancellationRequested(); Session.RequireStable(owner); validateDirectory(); capture.Validate(); }
        var identity = (await new SqliteDatabaseIdentityCatalogue(identityPath, Check).ReadAsync(ct).ConfigureAwait(false))
            .Single(row => row.LegacyProfileId == legacy);
        var historyPath = original + ".restore-history.sqlite";
        var history = new SqliteProfileRestoreHistory(historyPath, identity.InitialCandidateKey, Check);
        var latest = (await history.ReadAsync(ct).ConfigureAwait(false))[^1]; Check();
        if (!latest.Selected || location.Path != original + "." + latest.CandidateGeneration.ToString("N") + ".cipher.db")
            throw new InvalidOperationException("Active endpoint differs from the completed recorded generation.");
        var next = new DatabaseGenerationCheckpoint(identity.ProfileId, latest.CandidateGeneration, Guid.NewGuid());
        await history.ReserveAsync(next, ct).ConfigureAwait(false); Check();
        await durability.SynchronizeFileAsync(historyPath, ct).ConfigureAwait(false);
        var candidate = original + "." + next.CandidateGeneration.ToString("N") + ".cipher.db";
        var journalPath = candidate + ".restore.sqlite";
        await new SqliteDatabaseGenerationJournal(journalPath, next).CreateNewAsync(durability, ct).ConfigureAwait(false); Check();
        var reference = (await history.ReadAsync(ct).ConfigureAwait(false))[^1]; Check();
        return createBoundReceiver(identity, original, reference, owner);
    }

    /// <summary>Prepares only an explicitly requested new original without publishing or constructing plaintext contexts.</summary>
    public async Task PrepareNewAsync(string profileId, string originalPath, DatabaseAccessGate.ExclusiveLease owner, CancellationToken ct)
    {
        location.Access.RequireDrainedOwner(owner); validateDirectory();
        if (!Guid.TryParseExact(profileId, "N", out var id) || id == Guid.Empty || id.ToString("N") != profileId)
            throw new ArgumentException("New profile requires its canonical captured identifier.");
        var service = profiles();
        var request = service.ReadCreation(owner) ?? throw new InvalidOperationException("New profile requires its durable reviewed Add intent.");
        if (service.ValidateCreation(request, owner).Id != profileId)
            throw new InvalidOperationException("New profile differs from its reviewed Add intent.");
        var capture = service.CaptureStorage(); capture.Validate();
        var main = capture.OriginalPaths[""];
        var expected = Path.Combine(Path.GetDirectoryName(main)!, "zanance-" + profileId + ".db");
        if (!Path.IsPathFullyQualified(originalPath) || Path.GetFullPath(originalPath) != expected || (capture.OriginalPaths.TryGetValue(profileId, out var known) && known != expected))
            throw new InvalidOperationException("New profile differs from the captured unpublished original.");
        void Validate() { location.Access.RequireDrainedOwner(owner); validateDirectory(); capture.Validate(); }
        var identities = new SqliteDatabaseIdentityCatalogue(identityPath, Validate);
        await identities.RegisterAsync(profileId, ct).ConfigureAwait(false);
        await durability.SynchronizeFileAsync(identityPath, ct).ConfigureAwait(false);
        await durability.SynchronizeDirectoryAsync(ct).ConfigureAwait(false); Validate();
        // The active protected endpoint remains untouched while the pending profile builds its schema in memory.
        var unpublished = new LocalDatabaseLocation<ZananceDbContext>(expected, protectionRequired: true);
        using var privateOwner = await unpublished.Access.AcquireExclusiveAsync(ct).ConfigureAwait(false);
        using var prepared = await SqlCipherNewProfile.OpenAsync(unpublished, identities, profileId, expected, true,
            keys, durability, UpgradeAsync, privateOwner, Validate, maximumBytes, cipherVersion, ct).ConfigureAwait(false);
        Validate();
    }

    private async Task<SqlCipherProfileGeneration> ResolveAsync(string original, DatabaseAccessGate.ExclusiveLease owner, CancellationToken ct)
    {
        location.Access.RequireDrainedOwner(owner); validateDirectory();
        var capture = profiles().CaptureStorage(); capture.Validate();
        var row = capture.OriginalPaths.Single(pair => pair.Value == Path.GetFullPath(original));
        // A retained owner outlives this maintenance lease. Recheck this profile's actual mapping, rather
        // than retaining the whole catalogue snapshot (legitimate later Add/rename must not retire its key).
        void ValidateProfile()
        {
            validateDirectory(); var current = profiles().CaptureStorage(); current.Validate();
            if (!current.OriginalPaths.TryGetValue(row.Key, out var path) || path != row.Value)
                throw new InvalidOperationException("Protected profile original was removed or redirected.");
        }
        var identities = new SqliteDatabaseIdentityCatalogue(identityPath, ValidateProfile);
        var identity = (await identities.ReadAsync(ct).ConfigureAwait(false)).Single(item => item.LegacyProfileId == row.Key);
        capture.Validate();
        var history = new SqliteProfileRestoreHistory(row.Value + ".restore-history.sqlite", identity.InitialCandidateKey, ValidateProfile);
        var generation = await SqlCipherProfileGeneration.OpenCurrentAsync(location, identities, row.Key, row.Value, history,
            reference => createBoundReceiver is null ? createReceiver(row.Value, reference, owner) : createBoundReceiver(identity, row.Value, reference, owner), keys, durability, owner, ValidateProfile,
            maximumBytes, cipherVersion, ct).ConfigureAwait(false);
        try { capture.Validate(); return generation; }
        catch { generation.Dispose(); throw; }
    }

    private static async Task UpgradeAsync(SqliteConnection connection, CancellationToken ct)
    {
        var options = new DbContextOptionsBuilder<ZananceDbContext>().UseSqlite(connection, contextOwnsConnection: false)
            .UseModel(ZananceDbContextModel.Instance).Options;
        await using var db = new ZananceDbContext(options);
        await db.Database.MigrateAsync(ct).ConfigureAwait(false);
    }
}
