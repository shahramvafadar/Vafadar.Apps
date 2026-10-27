using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Backup;
using Vafadar.Core.Settings;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Zanance.App.Profiles;
using Vafadar.Zanance.App.Startup;
using Vafadar.Zanance.Core;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Security;

/// <summary>Stages explicit portable recovery in a new private installation while retaining every old database and key file.</summary>
/// <remarks>A ready receipt is published only after actual encrypted restore, cold verification and preference persistence.
/// Activation replays under covered startup. Device PIN/authentication state is never imported or removed.</remarks>
internal abstract class ProtectedBackupRecovery(IServiceProvider services) : IProtectedBackupRecovery
{
    internal const string ActiveRootKey = "protected.storage.root";
    private const string PendingRootKey = "protected.storage.pending-recovery";
    private const string Prefix = "recovered-data-";
    private const string ReceiptName = "recovery-activation.json";
    private const string DoneName = "recovery-activation.done";
    private const int MaximumReceiptBytes = 2 * 1024 * 1024;
    private readonly SemaphoreSlim operations = new(1, 1);

    /// <summary>Creates a no-replace private staging root before original application restore begins.</summary>
    protected abstract void CreateStagingDirectory(string parent, string path);
    /// <summary>Creates actual native file and directory synchronization effects.</summary>
    protected abstract IDatabaseGenerationFileDurability CreateDurability(string root);
    /// <summary>Creates exact native reference observations under the supplied ownership check.</summary>
    protected abstract IProfileRetirementReferenceFiles CreateFiles(IReadOnlyDictionary<string, string> roots, Action validate);
    /// <summary>Composes the original protected application host for this isolated staged installation.</summary>
    protected abstract ProtectedStorageHost CreateStageHost(IServiceProvider provider);

    /// <summary>Resolves the recorded relative root name only; construction performs no file, database or key work.</summary>
    internal static string MainPath(string basePath, ISettingsStore settings)
    {
        var id = settings.Get(ActiveRootKey);
        return id is null ? basePath : Path.Combine(Root(Path.GetDirectoryName(basePath)!, id), ZananceApp.DatabaseFileName);
    }

    private static string Root(string baseRoot, string id)
    {
        if (!Guid.TryParseExact(id, "N", out var parsed) || parsed == Guid.Empty || id != parsed.ToString("N"))
            throw new IOException("Protected recovery root identity is invalid.");
        return Path.Combine(Path.GetFullPath(baseRoot), Prefix + id);
    }

    /// <summary>Validates the borrowed package first, stages its original application restore and records explicit activation intent.</summary>
    internal async Task StageAsync(byte[] package, string? password, CancellationToken ct)
    {
        await operations.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var location = services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
            using var owner = await location.Access.AcquireExclusiveAsync(ct).ConfigureAwait(false);
            if (location.IsProtected) throw new InvalidOperationException("Backup key-loss recovery requires covered unpublished startup.");
            var profiles = services.GetRequiredService<ProfileService>(); var capture = profiles.CaptureStorage();
            var settings = services.GetRequiredService<ISettingsStore>(); var persistence = services.GetRequiredService<ISettingsPersistence>();
            if (settings.Get(PendingRootKey) is not null) throw new IOException("A recorded recovery selection requires continuation before another backup.");
            var oldRoot = settings.Get(ActiveRootKey); var oldCatalogue = profiles.CaptureRecoveryCatalogue(); var name = profiles.Current.Name;
            void Check() { ct.ThrowIfCancellationRequested(); location.Access.RequireDrainedOwner(owner); capture.Validate(); }
            await services.GetRequiredService<IBackupPackageReader>().ReadValidatedPortableAsync(package, password,
                Vafadar.Backup.Security.DeviceSafetyEncryption.MaximumPackageBytes, async (manifest, entries, token) =>
            {
                Check();
                await ProtectedProfileStorage.ValidatePortableSourcesAsync(entries,
                    Vafadar.Backup.Security.DeviceSafetyEncryption.MaximumPackageBytes, Check, token).ConfigureAwait(false);
                var portable = SqlCipherRestorePreferences.CapturePackagePreferences(entries, settings, LocalizationService.PortableKeys);
                Check();
                var baseRoot = FileSystem.AppDataDirectory; var id = Guid.NewGuid().ToString("N"); var root = Root(baseRoot, id);
                // mkdir is no-replace. A failed staging directory is retained; it cannot become an active empty installation.
                CreateStagingDirectory(baseRoot, root);
                var parent = CreateDurability(baseRoot); await parent.SynchronizeDirectoryAsync(token).ConfigureAwait(false);
                var memory = new InMemorySettingsStore();
                foreach (var pair in portable.Original) memory.Set(pair.Key, pair.Value);
                var sync = CreateDurability(root);
                var stagedPreferences = new RecoverySettingsPersistence(memory, root, sync);
                ServiceProvider CreateStageServices() => new ServiceCollection()
                    .AddZananceData(Path.Combine(root, ZananceApp.DatabaseFileName), protectionRequired: true)
                    .AddSingleton<ISettingsStore>(memory).AddSingleton<ISettingsPersistence>(stagedPreferences)
                    .AddSingleton<IProfileRuntime, RecoveryStagingRuntime>().AddSingleton(services.GetRequiredService<Translator>())
                    .AddSingleton<ProfileService>().BuildServiceProvider();
                using var stageServices = CreateStageServices();
                var host = CreateStageHost(stageServices);
                await host.PrepareAsync(token).ConfigureAwait(false);
                var stageLocation = stageServices.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
                try
                {
                    await host.RestoreAsync(manifest, entries, token).ConfigureAwait(false);
                    await stageServices.GetRequiredService<ZananceStore>().CompleteRestoredOnboardingAsync(token).ConfigureAwait(false);
                    await stagedPreferences.PersistAsync(token).ConfigureAwait(false);
                    ProfileRetirementArtifact candidate;
                    using (var stageOwner = await stageLocation.Access.AcquireExclusiveAsync(token).ConfigureAwait(false))
                    {
                        host.Session.RequireStable(stageOwner);
                        var files = CreateFiles(new Dictionary<string, string> { ["database"] = root }, stageOwner.Validate);
                        candidate = await files.CaptureAsync("database", Path.GetFileName(stageLocation.Path), token).ConfigureAwait(false);
                        host.Session.Close(stageOwner);
                    }
                    // A new original host reopens the committed generation with existing keys only.
                    using var coldServices = CreateStageServices();
                    var coldHost = CreateStageHost(coldServices);
                    await coldHost.PrepareAsync(token).ConfigureAwait(false);
                    var coldLocation = coldServices.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
                    using (var stageOwner = await coldLocation.Access.AcquireExclusiveAsync(token).ConfigureAwait(false))
                    {
                        coldHost.Session.RequireStable(stageOwner); coldHost.Session.Close(stageOwner);
                    }
                    var restored = LocalizationService.PortableKeys.ToDictionary(key => key, memory.Get, StringComparer.Ordinal);
                    var receipt = new RecoveryActivationReceipt(1, id, oldRoot, new(oldCatalogue), restored, name,
                        ProfileService.RecoveredMainCataloguePacket(name), candidate);
                    await WriteReceiptAsync(root, receipt, sync, token).ConfigureAwait(false);
                    Check(); settings.Set(PendingRootKey, id); await persistence.PersistAsync(token).ConfigureAwait(false);
                    if (settings.Get(PendingRootKey) != id) throw new IOException("Recovery activation intent persistence differs.");
                }
                finally
                {
                    if (stageLocation.IsProtected)
                    {
                        using var closeOwner = await stageLocation.Access.AcquireExclusiveAsync(CancellationToken.None).ConfigureAwait(false);
                        host.Session.Close(closeOwner);
                    }
                }
            }, ct).ConfigureAwait(false);
        }
        finally { operations.Release(); }
    }

    /// <summary>Completes recorded preference/root activation before original startup resolves identities or opens financial data.</summary>
    public async Task<bool> ReplayAsync(CancellationToken ct)
    {
        await operations.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var settings = services.GetRequiredService<ISettingsStore>(); var id = settings.Get(PendingRootKey);
            if (id is null) return false;
            var location = services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
            using var owner = await location.Access.AcquireExclusiveAsync(ct).ConfigureAwait(false);
            if (location.IsProtected) throw new InvalidOperationException("Recovery activation cannot replace a live protected endpoint.");
            var root = Root(FileSystem.AppDataDirectory, id); var sync = CreateDurability(root);
            void Check() { ct.ThrowIfCancellationRequested(); location.Access.RequireDrainedOwner(owner); _ = sync.AvailableBytes; }
            var files = CreateFiles(new Dictionary<string, string> { ["database"] = root }, Check);
            var receipt = await ReadReceiptAsync(files, ct).ConfigureAwait(false);
            var active = settings.Get(ActiveRootKey);
            if (receipt.RootId != id || active != receipt.PreviousRootId && active != id)
                throw new IOException("Recovery activation belongs to another captured installation.");
            var names = await files.ListNamesAsync("database", ct).ConfigureAwait(false);
            if (!names.Contains(DoneName, StringComparer.Ordinal))
            {
                if (await files.CaptureAsync("database", receipt.Candidate.Name, ct).ConfigureAwait(false) != receipt.Candidate)
                    throw new IOException("Staged recovered finance changed before activation.");
                foreach (var key in ProfileService.RecoveryCatalogueKeys)
                {
                    var actual = settings.Get(key); var original = receipt.OriginalCatalogue[key];
                    var target = key == "profiles.work.catalogue" ? receipt.NewCatalogue : key == "profiles.main.name" ? receipt.MainName : null;
                    if (actual != original && actual != target) throw new IOException("Device profile metadata changed during recovery activation.");
                }
                foreach (var pair in receipt.RestoredPreferences) settings.Set(pair.Key, pair.Value);
                settings.Set("profiles.work.catalogue", receipt.NewCatalogue);
                settings.Remove("profiles.list"); settings.Remove("profiles.current"); settings.Set("profiles.main.name", receipt.MainName);
                await services.GetRequiredService<ISettingsPersistence>().PersistAsync(ct).ConfigureAwait(false);
                Check(); settings.Set(ActiveRootKey, id);
                await services.GetRequiredService<ISettingsPersistence>().PersistAsync(ct).ConfigureAwait(false);
                if (settings.Get(ActiveRootKey) != id) throw new IOException("Recovered root publication differs.");
                var hash = Convert.ToHexStringLower(SHA256.HashData(await files.ReadAsync("database", ReceiptName, MaximumReceiptBytes, false, ct).ConfigureAwait(false)));
                await WriteNewAsync(Path.Combine(root, DoneName), System.Text.Encoding.ASCII.GetBytes(hash), sync, ct).ConfigureAwait(false);
            }
            else
            {
                // A visible marker after an interrupted native flush is not publication authority.
                await sync.SynchronizeFileAsync(Path.Combine(root, DoneName), ct).ConfigureAwait(false);
                await sync.SynchronizeDirectoryAsync(ct).ConfigureAwait(false);
                await ValidateDoneAsync(files, ct).ConfigureAwait(false);
            }
            Check(); location.RebindUnpublishedOriginal(Path.Combine(root, ZananceApp.DatabaseFileName), owner);
            settings.Remove(PendingRootKey); await services.GetRequiredService<ISettingsPersistence>().PersistAsync(ct).ConfigureAwait(false);
            return true;
        }
        finally { operations.Release(); }
    }

    /// <summary>Checks existing selected-root authority under the covered worker, without trusting an absent file as a fresh installation.</summary>
    public async Task ValidateSelectedAsync(CancellationToken ct)
    {
        var id = services.GetRequiredService<ISettingsStore>().Get(ActiveRootKey); if (id is null) return;
        var root = Root(FileSystem.AppDataDirectory, id); var sync = CreateDurability(root);
        var files = CreateFiles(new Dictionary<string, string> { ["database"] = root }, () => { _ = sync.AvailableBytes; });
        var receipt = await ReadReceiptAsync(files, ct).ConfigureAwait(false);
        if (receipt.RootId != id) throw new IOException("Selected recovery receipt differs.");
        await ValidateDoneAsync(files, ct).ConfigureAwait(false);
    }

    private static async Task<RecoveryActivationReceipt> ReadReceiptAsync(IProfileRetirementReferenceFiles files, CancellationToken ct)
    {
        var bytes = await files.ReadAsync("database", ReceiptName, MaximumReceiptBytes, false, ct).ConfigureAwait(false);
        var receipt = JsonSerializer.Deserialize(bytes, RecoveryActivationJson.Default.RecoveryActivationReceipt)
            ?? throw new IOException("Recovery activation receipt is missing.");
        if (receipt.Schema != 1 || receipt.OriginalCatalogue is null || receipt.RestoredPreferences is null
            || !receipt.OriginalCatalogue.Keys.Order(StringComparer.Ordinal).SequenceEqual(ProfileService.RecoveryCatalogueKeys.Order(StringComparer.Ordinal))
            || !receipt.RestoredPreferences.Keys.Order(StringComparer.Ordinal).SequenceEqual(LocalizationService.PortableKeys.Order(StringComparer.Ordinal))
            || receipt.Candidate is null || receipt.Candidate.Root != "database" || !receipt.Candidate.Name.StartsWith("zanance.db.", StringComparison.Ordinal)
            || !receipt.Candidate.Name.EndsWith(".cipher.db", StringComparison.Ordinal) || receipt.Candidate.Name.Contains('/') || receipt.Candidate.Name.Contains((char)92))
            throw new IOException("Recovery activation receipt has foreign fields.");
        _ = Root(FileSystem.AppDataDirectory, receipt.RootId);
        if (receipt.PreviousRootId is not null) _ = Root(FileSystem.AppDataDirectory, receipt.PreviousRootId);
        return receipt;
    }

    private static async Task ValidateDoneAsync(IProfileRetirementReferenceFiles files, CancellationToken ct)
    {
        var receipt = await files.ReadAsync("database", ReceiptName, MaximumReceiptBytes, false, ct).ConfigureAwait(false);
        var marker = await files.ReadAsync("database", DoneName, 64, false, ct).ConfigureAwait(false);
        if (!marker.AsSpan().SequenceEqual(System.Text.Encoding.ASCII.GetBytes(Convert.ToHexStringLower(SHA256.HashData(receipt)))))
            throw new IOException("Completed recovery authority differs from its retained receipt.");
    }

    private async Task WriteReceiptAsync(string root, RecoveryActivationReceipt receipt, IDatabaseGenerationFileDurability sync, CancellationToken ct) =>
        await WriteNewAsync(Path.Combine(root, ReceiptName), JsonSerializer.SerializeToUtf8Bytes(receipt,
            RecoveryActivationJson.Default.RecoveryActivationReceipt), sync, ct).ConfigureAwait(false);

    private async Task WriteNewAsync(string path, byte[] bytes, IDatabaseGenerationFileDurability sync, CancellationToken ct)
    {
        var staged = path + ".creating";
        var root = Path.GetDirectoryName(path)!;
        var files = CreateFiles(new Dictionary<string, string> { ["database"] = root }, () => { _ = sync.AvailableBytes; });
        if (!File.Exists(staged))
        {
            await using var output = new FileStream(staged, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            await output.WriteAsync(bytes, ct).ConfigureAwait(false); output.Flush(true);
        }
        var read = await files.ReadAsync("database", Path.GetFileName(staged), MaximumReceiptBytes, false, ct).ConfigureAwait(false);
        if (!read.AsSpan().SequenceEqual(bytes))
        {
            // Covered activation has requalified its exact ready receipt and unchanged financial candidate.
            // Only that completion marker may finish a prefix left by an interrupted private write.
            if (Path.GetFileName(path) != DoneName || read.Length >= bytes.Length
                || !bytes.AsSpan(0, read.Length).SequenceEqual(read))
                throw new IOException("Recovery metadata staging differs.");
            await using (var output = new FileStream(staged, FileMode.Open, FileAccess.Write, FileShare.None))
            { await output.WriteAsync(bytes, ct).ConfigureAwait(false); output.SetLength(bytes.Length); output.Flush(true); }
            if (!(await files.ReadAsync("database", Path.GetFileName(staged), MaximumReceiptBytes, false, ct).ConfigureAwait(false)).AsSpan().SequenceEqual(bytes))
                throw new IOException("Recovery completion repair read-back differs.");
        }
        await sync.SynchronizeFileAsync(staged, ct).ConfigureAwait(false);
        File.Move(staged, path, overwrite: false);
        await sync.SynchronizeDirectoryAsync(ct).ConfigureAwait(false);
        if (!(await files.ReadAsync("database", Path.GetFileName(path), MaximumReceiptBytes, false, ct).ConfigureAwait(false)).AsSpan().SequenceEqual(bytes))
            throw new IOException("Recovery metadata read-back differs.");
    }

    /// <summary>Persists only portable staging preferences; financial data, secrets and device catalogue are excluded.</summary>
    private sealed class RecoverySettingsPersistence(ISettingsStore settings, string root, IDatabaseGenerationFileDurability sync) : ISettingsPersistence
    {
        public async Task PersistAsync(CancellationToken ct = default)
        {
            var values = LocalizationService.PortableKeys.ToDictionary(key => key, settings.Get, StringComparer.Ordinal);
            var bytes = JsonSerializer.SerializeToUtf8Bytes(values, RecoveryActivationJson.Default.DictionaryStringString);
            var path = Path.Combine(root, "recovery-staging-preferences.json");
            await File.WriteAllBytesAsync(path, bytes, ct).ConfigureAwait(false);
            await sync.SynchronizeFileAsync(path, ct).ConfigureAwait(false);
            if (!(await File.ReadAllBytesAsync(path, ct).ConfigureAwait(false)).AsSpan().SequenceEqual(bytes))
                throw new IOException("Staging preferences were not persisted.");
        }
    }

    /// <summary>Refuses UI/authentication/profile effects in the private staging installation; only original storage bootstrap/restore runs.</summary>
    private sealed class RecoveryStagingRuntime : IProfileRuntime
    {
        public Task RunExclusiveAsync(Func<Task> action) => throw new InvalidOperationException("Staging cannot switch profiles.");
        public Task MigrateAsync(DatabaseAccessGate.ExclusiveLease owner) => throw new InvalidOperationException("Staging uses its protected bootstrap.");
        public Task<bool> AuthenticateAsync(string reason) => throw new InvalidOperationException("Staging has no authentication UI.");
        public Task ReloadLockAsync() => throw new InvalidOperationException("Staging cannot change device authentication.");
        public void DismissUndo() => throw new InvalidOperationException("Staging cannot change an original Undo offer.");
    }
}

/// <summary>Device-only root activation authority; old catalogue is retained for archived installations and never portable.</summary>
internal sealed record RecoveryActivationReceipt(int Schema, string RootId, string? PreviousRootId,
    Dictionary<string, string?> OriginalCatalogue, Dictionary<string, string?> RestoredPreferences,
    string? MainName, string NewCatalogue, ProfileRetirementArtifact Candidate);

/// <summary>Serializes retained root activation and one-time preference replay receipts.</summary>
[JsonSerializable(typeof(RecoveryActivationReceipt))]
[JsonSerializable(typeof(Dictionary<string, string?>))]
internal partial class RecoveryActivationJson : JsonSerializerContext { }
