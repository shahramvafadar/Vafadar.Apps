using System.Security.Cryptography;
using Vafadar.Backup.Security;
using Vafadar.Data;
using Vafadar.Zanance.App.Security;

namespace Vafadar.SqlCipher.Compatibility.Android;

/// <summary>Exercises the original verifier against actual native files after journal/history interruption.</summary>
internal static class RecoveryPendingReferenceCompatibility
{
    /// <summary>Requires missing committed candidates to fail before other reference reads or native key creation.</summary>
    internal static async Task<string> RunAsync(string folder)
    {
        SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_sqlcipher());
        if (Directory.Exists(folder)) throw new IOException("Use a fresh independent pending-reference fixture.");
        Directory.CreateDirectory(folder);
        foreach (var phase in new[] { DatabaseGenerationPhase.CandidateVerified, DatabaseGenerationPhase.Selected })
        {
            var root = Path.Combine(folder, phase.ToString()); Directory.CreateDirectory(root);
            var safetyRoot = Path.Combine(root, "safety"); var portableRoot = Path.Combine(root, "portable");
            Directory.CreateDirectory(safetyRoot); Directory.CreateDirectory(portableRoot);
            var original = Path.Combine(root, "zanance.db"); var identities = new SqliteDatabaseIdentityCatalogue(Path.Combine(root, "identities.sqlite"), () => { });
            await identities.CreateNewAsync([""]); var initial = (await identities.ReadAsync()).Single().InitialCandidateKey;
            var binding = new DatabaseKeyBinding(initial.ProfileId, initial.InstallationId, Guid.NewGuid());
            var history = new SqliteProfileRestoreHistory(original + ".restore-history.sqlite", initial, () => { }); await history.CreateNewAsync();
            var intent = new DatabaseGenerationCheckpoint(binding.ProfileId, initial.GenerationId, binding.GenerationId); await history.ReserveAsync(intent);
            var candidate = original + "." + binding.GenerationId.ToString("N") + ".cipher.db";
            var journal = new SqliteDatabaseGenerationJournal(candidate + ".restore.sqlite", intent); await journal.CreateNewAsync(); var state = intent;
            while (state.Phase < phase)
            {
                var next = state.Advance(state.ProfileId, state.CandidateGeneration, state.Phase + 1);
                await journal.CompareAndCommitAsync(state, next); state = next;
            }
            if (phase == DatabaseGenerationPhase.Selected)
            {
                await File.WriteAllTextAsync(candidate + ".before-restore.cipher.db", "Unopened fictitious rollback");
                await File.WriteAllTextAsync(candidate + ".preferences.cipher.db", "Unopened fictitious preferences");
            }
            var key = RandomNumberGenerator.GetBytes(32);
            try
            {
                var identity = new byte[48]; binding.ProfileId.TryWriteBytes(identity.AsSpan(0, 16), bigEndian: true, out _);
                binding.InstallationId.TryWriteBytes(identity.AsSpan(16, 16), bigEndian: true, out _); binding.GenerationId.TryWriteBytes(identity.AsSpan(32, 16), bigEndian: true, out _);
                var envelope = DeviceSafetyEncryption.Protect("Fictitious authenticated unrelated reference"u8, key, identity);
                try { await File.WriteAllBytesAsync(Path.Combine(safetyRoot, "packet.vbs"), envelope); }
                finally { CryptographicOperations.ZeroMemory(envelope); }
                var verifier = new ProfileRecoveryReferenceVerifier(identities,
                    new AndroidProfileRetirementFiles(new Dictionary<string, string> { ["database"] = root, ["portable"] = portableRoot, ["safety"] = safetyRoot }, () => { }),
                    new AndroidGenerationFileDurability(root), [], "4.19.0", 1024 * 1024, () => { });
                var refused = false;
                try { await verifier.VerifyAsync(binding, original, key); }
                catch (IOException error) when (error.Message == "Selected recovery generation is missing.") { refused = true; }
                if (!refused || File.Exists(candidate) || (await history.ReadAsync())[^1].Selected || await journal.ReadAsync() != state
                    || Directory.EnumerateFiles(root, "*.key*").Any()) throw new IOException("Pending reference authority was accepted or changed.");
            }
            finally { CryptographicOperations.ZeroMemory(key); }
        }
        return "{\"VerifiedMissingCandidateRefused\":true,\"SelectedUnmirroredCandidateRefused\":true,\"HistoryAndJournalUnchanged\":true,\"NoReplacementDataOrKeys\":true}";
    }
}
