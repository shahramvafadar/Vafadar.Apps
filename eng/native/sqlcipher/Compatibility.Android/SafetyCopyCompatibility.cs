using System.Security.Cryptography;
using Vafadar.Backup;
using Vafadar.Backup.Security;
using Vafadar.Backup.Storage;
using Vafadar.Core.Hosting;
using Vafadar.Core.Settings;
using Vafadar.Data;
using Vafadar.Zanance.App.Security;

namespace Vafadar.SqlCipher.Compatibility.Android;

/// <summary>Checks actual safety-package paths and native key persistence across an independent process restart.</summary>
internal static class SafetyCopyCompatibility
{
    internal static async Task<string> RunAsync(string folder, bool create)
    {
        var context = Path.Combine(folder, "safety-context.txt");
        if (create)
        {
            if (Directory.Exists(folder)) throw new IOException("Use a fresh safety fixture.");
            Directory.CreateDirectory(folder);
            await File.WriteAllLinesAsync(context, [Guid.NewGuid().ToString("N"), Guid.NewGuid().ToString("N")]);
        }
        var ids = await File.ReadAllLinesAsync(context);
        IDatabaseGenerationKeyStore keys = new AndroidGenerationKeyStore(folder);
        if (!create) keys = new ReadOnlyKeys(keys);
        var protection = new DeviceKeySafetyCopyProtection(Guid.ParseExact(ids[0], "N"), Guid.ParseExact(ids[1], "N"), keys,
            () => { if (!Directory.Exists(folder)) throw new IOException("Retired safety context."); });
        var source = new Source();
        var service = new BackupService([source], new StaticAppEnvironment("pro.vafadar.test", "Test", new Version(1, 0), "Test", "Android"),
            new InMemorySettingsStore(), TimeProvider.System, new BackupOptions { MaxBackupsToKeep = 1 }, safetyProtection: protection);
        var storage = new LocalFolderBackupStorage(Path.Combine(folder, "safety"));
        if (create)
        {
            await service.CreateSafetyCopyAsync(storage);
            await service.CreateSafetyCopyAsync(storage);
        }
        var files = await storage.ListAsync();
        if (files.Count != 2 || service.LastBackupAt is not null) throw new IOException("Safety retention or backup state changed.");
        foreach (var file in files)
        {
            var envelope = await File.ReadAllBytesAsync(Path.Combine(folder, "safety", file.FileName));
            if (!DeviceSafetyEncryption.IsEncrypted(envelope) || envelope.AsSpan().IndexOf("fictitious-finance"u8) >= 0)
                throw new IOException("Safety content was persisted without protection.");
            await service.InspectPackageAsync(envelope);
            await service.RestorePackageAsync(envelope);
            if (!source.Restored) throw new IOException("Safety package did not restore.");
            envelope[^1] ^= 1;
            try { await service.RestorePackageAsync(envelope); throw new IOException("Tampered copy accepted."); }
            catch (CryptographicException) { }
        }
        return "{\"SafetyCopies\":2,\"NativeKeyPersistence\":true,\"ColdReadOnly\":" + (!create ? "true" : "false") + ",\"TamperRejected\":true}";
    }

    private sealed class ReadOnlyKeys(IDatabaseGenerationKeyStore inner) : IDatabaseGenerationKeyStore
    {
        public Task PrepareAsync(DatabaseKeyBinding binding, CancellationToken ct) => throw new IOException("Cold read tried replacing a key.");
        public Task<byte[]> ReadAsync(DatabaseKeyBinding binding, CancellationToken ct) => inner.ReadAsync(binding, ct);
    }

    private sealed class Source : IBackupSource
    {
        public string Name => "finance.json";
        internal bool Restored;
        public Task WriteAsync(Stream destination, CancellationToken ct) => destination.WriteAsync("fictitious-finance:250"u8.ToArray(), ct).AsTask();
        public async Task RestoreAsync(Stream source, CancellationToken ct)
        {
            using var reader = new StreamReader(source);
            if (await reader.ReadToEndAsync(ct) != "fictitious-finance:250") throw new IOException("Changed safety content.");
            Restored = true;
        }
    }
}
