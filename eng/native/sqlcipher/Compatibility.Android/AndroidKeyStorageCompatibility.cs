using System.Security.Cryptography;
using Vafadar.Data;
using Vafadar.Zanance.App.Security;

namespace Vafadar.SqlCipher.Compatibility.Android;

/// <summary>Exercises actual immutable envelope files within this independent package only.</summary>
internal static class AndroidKeyStorageCompatibility
{
    /// <summary>Checks file refusal and staged interruption boundaries using only fictitious owned paths.</summary>
    internal static string Check(string fixture, byte[] key, byte[] envelope, DatabaseKeyBinding binding)
    {
        var count = 0;
        var root = Path.Combine(fixture, "storage-refusals");
        Directory.CreateDirectory(root);
        var store = new AndroidDatabaseKeyEnvelopeStore(root);
        var path = Path.Combine(root, binding.ProfileId.ToString("N") + "-" + binding.GenerationId.ToString("N") + ".key");
        void Refused(Action action)
        {
            try { action(); throw new InvalidOperationException("Unsafe stored key accepted."); }
            catch (Exception error) when (error is IOException or CryptographicException or global::Android.Systems.ErrnoException) { count++; }
        }
        void ReadAndClear() { var restored = store.ReadKey(binding); CryptographicOperations.ZeroMemory(restored); }
        store.PublishNew(binding, envelope);
        var alternative = RandomNumberGenerator.GetBytes(32);
        try { Refused(() => store.PublishNew(binding, AndroidDatabaseKeyProtector.Protect(alternative, binding))); }
        finally { CryptographicOperations.ZeroMemory(alternative); }
        var existing = store.ReadKey(binding);
        try { if (!CryptographicOperations.FixedTimeEquals(existing, key)) throw new InvalidOperationException("Duplicate replaced key."); }
        finally { CryptographicOperations.ZeroMemory(existing); }
        var changed = envelope.ToArray(); changed[^1] ^= 1;
        File.WriteAllBytes(path, changed); Refused(ReadAndClear);
        File.WriteAllBytes(path, envelope[..64]); Refused(ReadAndClear);
        File.WriteAllBytes(path, [.. envelope, 0]); Refused(ReadAndClear);
        File.Delete(path); Refused(ReadAndClear);
        Directory.CreateDirectory(path); Refused(ReadAndClear); Directory.Delete(path);
        var target = Path.Combine(root, "fictitious-target"); File.WriteAllBytes(target, envelope);
        global::Android.Systems.Os.Symlink(target, path); Refused(ReadAndClear); File.Delete(path);
        // A killed writer can leave its partial staging file. It cannot become the generation's final key.
        var orphan = Path.Combine(root, ".key-stage-" + Guid.NewGuid().ToString("N")); File.WriteAllBytes(orphan, envelope[..12]);
        store.PublishNew(binding, envelope);
        ReadAndClear();
        if (!File.Exists(orphan) || Directory.GetFiles(root, ".key-stage-*").Length != 1)
            throw new InvalidOperationException("Storage touched another writer's staged file.");
        count++;
        return "{\"AndroidKeyStorageCases\":" + count + ",\"NoOverwriteOrReplacement\":true}";
    }
}
