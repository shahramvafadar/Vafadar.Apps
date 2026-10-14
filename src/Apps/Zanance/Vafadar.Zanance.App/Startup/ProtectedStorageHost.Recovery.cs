using System.Security.Cryptography;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Zanance.App.Profiles;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Security;

internal abstract partial class ProtectedStorageHost
{
    private SqliteRecoveryEnvelopeCatalogue? recoveryCatalogue;
    /// <summary>Reports existing enrollment without exposing a secret or replacing its public identity.</summary>
    internal bool RecoveryEnrolled => recoveryCatalogue is not null;

    /// <summary>Resumes interrupted public capture with existing device keys before startup enables further key creation.</summary>
    private async Task ResumeRecoveryCaptureAsync(SqliteRecoveryEnvelopeCatalogue catalogue,
        IReadOnlyDictionary<string, string> roots, CancellationToken ct)
    {
        var validate = nativeValidation ?? throw new InvalidOperationException("Native validation is unavailable.");
        var location = services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
        using var owner = await location.Access.AcquireExclusiveAsync(ct).ConfigureAwait(false);
        var profiles = services.GetRequiredService<ProfileService>().CaptureStorage();
        void Check() { location.Access.RequireDrainedOwner(owner); profiles.Validate(); validate(); }
        var identities = await new SqliteDatabaseIdentityCatalogue(identityPath!, Check).ReadAsync(ct).ConfigureAwait(false);
        if (identities.Count == 0 || identities.Any(row => row.InstallationId != catalogue.Identity.InstallationId))
            throw new IOException("Public recovery identity differs from the retained installation.");
        var files = CreateFiles(roots, Check);
        var names = await files.ListNamesAsync("keys", ct).ConfigureAwait(false);
        var bindings = new HashSet<DatabaseKeyBinding>();
        foreach (var name in names.Where(name => name.EndsWith(".key", StringComparison.Ordinal)
            || name.EndsWith(".key.recovered", StringComparison.Ordinal)))
        {
            if (name.Length < 69 || name[32] != '-' || !Guid.TryParseExact(name.AsSpan(0, 32), "N", out var profile)
                || !Guid.TryParseExact(name.AsSpan(33, 32), "N", out var generation)
                || !identities.Any(row => row.ProfileId == profile))
                throw new IOException("Recovery capture requires reconciliation of unknown retained keys.");
            _ = await files.CaptureAsync("keys", name, ct).ConfigureAwait(false);
            bindings.Add(new(profile, catalogue.Identity.InstallationId, generation));
        }
        var recorded = await catalogue.ReadBindingsAsync(ct).ConfigureAwait(false);
        if (identities.Any(row => !bindings.Contains(row.InitialCandidateKey) && !recorded.Contains(row.InitialCandidateKey)))
            throw new IOException("Interrupted recovery capture is missing an original key and mapping.");
        foreach (var binding in bindings)
        {
            Check(); var key = await keys!.ReadAsync(binding, ct).ConfigureAwait(false);
            try { await catalogue.CaptureAsync(binding, key, ct).ConfigureAwait(false); }
            finally { CryptographicOperations.ZeroMemory(key); }
        }
        Check();
    }

    /// <summary>Restores existing retained generation keys from the explicitly supplied code while startup remains covered.</summary>
    internal async Task RecoverDeviceKeysAsync(string code, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(code);
        if (!code.StartsWith("ZRK1-", StringComparison.Ordinal))
            throw new CryptographicException("Recovery code format differs.");
        byte[] secret;
        try { secret = Convert.FromBase64String(code[5..]); }
        catch (FormatException) { throw new CryptographicException("Recovery code format differs."); }
        try
        {
            if (secret.Length != 32) throw new CryptographicException("Recovery code length differs.");
            await preparation.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                var catalogue = recoveryCatalogue ?? throw new IOException("Optional recovery metadata is unavailable.");
                var native = keys ?? throw new IOException("Device key storage is unavailable.");
                var sync = durability ?? throw new IOException("Native durability is unavailable.");
                var location = services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
                using var owner = await location.Access.AcquireExclusiveAsync(ct).ConfigureAwait(false);
                if (location.IsProtected) throw new InvalidOperationException("Key recovery requires covered startup.");
                var profiles = services.GetRequiredService<ProfileService>();
                var capture = profiles.CaptureStorage();
                void Check() { location.Access.RequireDrainedOwner(owner); capture.Validate(); nativeValidation!(); }
                var root = Path.GetDirectoryName(location.Path)!;
                var roots = new Dictionary<string, string>
                { ["database"] = root, ["keys"] = Path.Combine(root, "database-keys"),
                    ["portable"] = Path.Combine(FileSystem.AppDataDirectory, "backups"), ["safety"] = Path.Combine(FileSystem.AppDataDirectory, "backups-safety") };
                var files = CreateFiles(roots, Check);
                var verifier = new ProfileRecoveryReferenceVerifier(new SqliteDatabaseIdentityCatalogue(identityPath!, Check),
                    files, sync, Vafadar.Localization.LocalizationService.PortableKeys, "4.19.0",
                    Vafadar.Backup.Security.DeviceSafetyEncryption.MaximumPackageBytes, Check);
                var vault = new SqlCipherRecoveryVault(Path.Combine(root, "recovery-private.cipher.db"),
                    catalogue.Identity, "4.19.0", sync, Check);
                var recovery = new Vafadar.Zanance.App.Startup.ProtectedProfileRecovery(location, profiles, identityPath!,
                    vault.OpenAsync, catalogue, native,
                    (binding, original, key, lease, token) => verifier.VerifyAsync(binding, original, key, token));
                await recovery.RecoverAsync(secret, owner, ct).ConfigureAwait(false);
            }
            finally { preparation.Release(); }
        }
        finally { CryptographicOperations.ZeroMemory(secret); }
    }

    /// <summary>Creates only an in-memory optional recovery draft; the user records its code before native publication.</summary>
    internal async Task<ProtectedRecoveryDraft> CreateRecoveryDraftAsync(CancellationToken ct)
    {
        var location = services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
        using var owner = await location.Access.AcquireExclusiveAsync(ct).ConfigureAwait(false);
        Session.RequireStable(owner);
        var identities = await new SqliteDatabaseIdentityCatalogue(identityPath!, owner.Validate).ReadAsync(ct).ConfigureAwait(false);
        if (recoveryCatalogue is not null)
            throw new InvalidOperationException("Recovery is already enrolled; its existing code remains authoritative.");
        var installation = identities.Select(row => row.InstallationId).Distinct().Single();
        var root = Path.GetDirectoryName(location.Path)!;
        if (File.Exists(Path.Combine(root, "recovery-private.cipher.db"))
            || File.Exists(Path.Combine(root, "recovery-private.cipher.db.creating"))
            || File.Exists(Path.Combine(root, "recovery-public.sqlite")))
            throw new RecoveryEnrollmentIncompleteException();
        return new(DatabaseRecoveryMaterial.Create(installation), RandomNumberGenerator.GetBytes(32));
    }

    /// <summary>Resumes the user's acknowledged setup draft from its exact encrypted vault after process interruption.</summary>
    internal async Task<ProtectedRecoveryDraft> ResumeRecoveryDraftAsync(string code, CancellationToken ct)
    {
        if (!code.StartsWith("ZRK1-", StringComparison.Ordinal)) throw new CryptographicException("Recovery code format differs.");
        byte[] secret;
        try { secret = Convert.FromBase64String(code[5..]); }
        catch (FormatException) { throw new CryptographicException("Recovery code format differs."); }
        try
        {
            if (secret.Length != 32) throw new CryptographicException("Recovery code length differs.");
            var location = services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
            using var owner = await location.Access.AcquireExclusiveAsync(ct).ConfigureAwait(false);
            Session.RequireStable(owner);
            var identities = await new SqliteDatabaseIdentityCatalogue(identityPath!, owner.Validate).ReadAsync(ct).ConfigureAwait(false);
            var installation = identities.Select(row => row.InstallationId).Distinct().Single();
            var root = Path.GetDirectoryName(location.Path)!;
            var vaultPath = Path.Combine(root, "recovery-private.cipher.db");
            if (!File.Exists(vaultPath)) vaultPath += ".creating";
            var material = await SqlCipherRecoveryVault.OpenExistingAsync(vaultPath, installation, secret, "4.19.0",
                durability!, owner.Validate, ct).ConfigureAwait(false);
            var draft = new ProtectedRecoveryDraft(material, secret) { RecordedByUser = true };
            secret = []; return draft;
        }
        finally { CryptographicOperations.ZeroMemory(secret); }
    }

    /// <summary>Publishes acknowledged private/public material and wraps every existing recorded device key before enablement.</summary>
    internal async Task SaveRecoveryDraftAsync(ProtectedRecoveryDraft draft, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(draft);
        if (!draft.RecordedByUser) throw new InvalidOperationException("Recovery code must be recorded before publishing private material.");
        var location = services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
        using var owner = await location.Access.AcquireExclusiveAsync(ct).ConfigureAwait(false);
        Session.RequireStable(owner);
        var capture = services.GetRequiredService<ProfileService>().CaptureStorage();
        void Validate() { location.Access.RequireDrainedOwner(owner); Session.RequireStable(owner); capture.Validate(); }
        var identities = await new SqliteDatabaseIdentityCatalogue(identityPath!, Validate).ReadAsync(ct).ConfigureAwait(false);
        if (identities.Any(row => row.InstallationId != draft.Material.Identity.InstallationId))
            throw new IOException("Recovery draft belongs to another installation.");
        var root = Path.GetDirectoryName(location.Path)!;
        var native = keys ?? throw new InvalidOperationException("Protected keys are unavailable.");
        var sync = durability ?? throw new InvalidOperationException("Protected durability is unavailable.");
        var keyRoot = Path.Combine(root, "database-keys");
        var files = CreateFiles(new Dictionary<string, string> { ["keys"] = keyRoot }, Validate);
        var names = await files.ListNamesAsync("keys", ct).ConfigureAwait(false);
        var bindings = new HashSet<DatabaseKeyBinding>();
        foreach (var name in names.Where(name => name.EndsWith(".key", StringComparison.Ordinal) || name.EndsWith(".key.recovered", StringComparison.Ordinal)))
        {
            if (name.Length < 69 || name[32] != '-' || !Guid.TryParseExact(name.AsSpan(0, 32), "N", out var profile)
                || !Guid.TryParseExact(name.AsSpan(33, 32), "N", out var generation)
                || !identities.Any(row => row.ProfileId == profile))
                throw new IOException("Unknown key namespace requires reconciliation before recovery enrollment.");
            _ = await files.CaptureAsync("keys", name, ct).ConfigureAwait(false);
            bindings.Add(new(profile, draft.Material.Identity.InstallationId, generation));
        }
        if (identities.Any(row => !bindings.Contains(row.InitialCandidateKey)))
            throw new IOException("Recovery enrollment is missing an original generation.");
        // Preflight every retained native key before publishing either recovery file. A later
        // interruption is resumed against the same public identity, never a replacement draft.
        foreach (var binding in bindings)
        {
            Validate(); var existingKey = await native.ReadAsync(binding, ct).ConfigureAwait(false);
            CryptographicOperations.ZeroMemory(existingKey);
        }
        var vaultPath = Path.Combine(root, "recovery-private.cipher.db");
        var publicPath = Path.Combine(root, "recovery-public.sqlite");
        var vault = new SqlCipherRecoveryVault(vaultPath, draft.Material.Identity, "4.19.0", sync, Validate);
        if (!File.Exists(vaultPath)) await vault.CreateNewAsync(draft.Material, draft.Secret, ct).ConfigureAwait(false);
        using (var verified = await vault.OpenAsync(draft.Secret, ct).ConfigureAwait(false))
            if (verified.Identity.RecoveryId != draft.Material.Identity.RecoveryId) throw new CryptographicException("Published recovery draft differs.");
        var validateNative = nativeValidation ?? throw new InvalidOperationException("Native validation is unavailable.");
        var catalogue = new SqliteRecoveryEnvelopeCatalogue(publicPath, draft.Material.Identity, sync, validateNative);
        if (!File.Exists(publicPath)) await catalogue.CreateNewAsync(ct).ConfigureAwait(false);
        var stored = await SqliteRecoveryEnvelopeCatalogue.ReadExistingIdentityAsync(publicPath, sync, Validate, ct).ConfigureAwait(false);
        if (stored.RecoveryId != draft.Material.Identity.RecoveryId) throw new CryptographicException("Existing recovery identity differs.");
        foreach (var binding in bindings)
        {
            Validate(); var key = await native.ReadAsync(binding, ct).ConfigureAwait(false);
            try { await catalogue.CaptureAsync(binding, key, ct).ConfigureAwait(false); }
            finally { CryptographicOperations.ZeroMemory(key); }
        }
        native.Attach(catalogue); recoveryCatalogue = catalogue;
        // Retirement must now remove public mappings only after its durable catalogue-removal barrier.
        var roots = new Dictionary<string, string>
        { ["database"] = root, ["keys"] = keyRoot, ["portable"] = Path.Combine(FileSystem.AppDataDirectory, "backups"), ["safety"] = Path.Combine(FileSystem.AppDataDirectory, "backups-safety") };
        retirement = CreateRetirement(root, roots, identityPath!,
            services.GetRequiredService<Vafadar.Core.Settings.ISettingsPersistence>(), native, sync, validateNative, catalogue);
    }
}

/// <summary>Owns only the current recovery setup secret/material; disposal clears its owned secret buffer.</summary>
internal sealed class ProtectedRecoveryDraft(DatabaseRecoveryMaterial material, byte[] secret) : IDisposable
{
    private bool disposed;
    /// <summary>The optional standard private/public recovery material, never portable or logged.</summary>
    internal DatabaseRecoveryMaterial Material { get; } = material;
    /// <summary>The owned ephemeral vault secret; the UI shows it only for explicit setup.</summary>
    internal ReadOnlyMemory<byte> Secret => !disposed ? secret : throw new ObjectDisposedException(nameof(ProtectedRecoveryDraft));
    /// <summary>Whether the user explicitly acknowledged recording the displayed code before Save.</summary>
    internal bool RecordedByUser { get; set; }
    /// <summary>Formats the random code for the user; callers do not persist or log this string.</summary>
    internal string DisplayCode => "ZRK1-" + Convert.ToBase64String(Secret.Span);
    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed) return; disposed = true;
        CryptographicOperations.ZeroMemory(secret); Material.Dispose();
    }
}

/// <summary>Requires the originally recorded code to resume retained setup material without replacing its identity.</summary>
internal sealed class RecoveryEnrollmentIncompleteException() : IOException("Recorded recovery setup requires explicit continuation.");
