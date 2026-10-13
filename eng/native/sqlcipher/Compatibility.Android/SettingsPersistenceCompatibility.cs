using Microsoft.Maui.Storage;
using Vafadar.Maui.Hosting;

namespace Vafadar.SqlCipher.Compatibility.Android;

/// <summary>Checks original MAUI settings persistence in this independent package across process restart.</summary>
internal static class SettingsPersistenceCompatibility
{
    /// <summary>Writes fictitious values or verifies the exact retained snapshot without rewriting it.</summary>
    internal static async Task<string> RunAsync(string folder, bool create)
    {
        var store = new MauiSettingsStore(Preferences.Default);
        var persistence = new AndroidSettingsPersistence(Preferences.Default);
        const string valueKey = "native-proof.persisted-value";
        const string removedKey = "native-proof.removed-value";
        if (create)
        {
            if (Directory.Exists(folder)) throw new IOException("Use a fresh independent settings fixture.");
            Directory.CreateDirectory(folder);
            var expected = Guid.NewGuid().ToString("N");
            store.Set(valueKey, expected);
            store.Set(removedKey, "fictitious");
            store.Remove(removedKey);
            using var canceled = new CancellationTokenSource();
            canceled.Cancel();
            try
            {
                await persistence.PersistAsync(canceled.Token);
                throw new IOException("Canceled persistence unexpectedly completed.");
            }
            catch (OperationCanceledException) { }
            await persistence.PersistAsync();
            await File.WriteAllTextAsync(Path.Combine(folder, "expected.txt"), expected);
            return "{\"OriginalMauiWritesPersisted\":true,\"CanceledPersistenceRejected\":true}";
        }
        var retained = await File.ReadAllTextAsync(Path.Combine(folder, "expected.txt"));
        if (store.Get(valueKey) != retained || store.Get(removedKey) is not null)
            throw new IOException("Original MAUI snapshot did not survive process restart.");
        // Clean only these independently owned fictitious values, using the same qualified adapter.
        store.Remove(valueKey);
        await persistence.PersistAsync();
        return "{\"OriginalMauiValueRetainedAfterRestart\":true,\"RemovalRetainedAfterRestart\":true}";
    }
}
