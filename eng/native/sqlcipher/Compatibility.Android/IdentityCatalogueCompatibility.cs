using System.Security.Cryptography;
using System.Text;
using Vafadar.Data;
using Vafadar.Zanance.App.Security;

namespace Vafadar.SqlCipher.Compatibility.Android;

/// <summary>Reopens actual committed identity state in a new independent Android process.</summary>
internal static class IdentityCatalogueCompatibility
{
    internal static async Task<string> RunAsync(string folder, bool create)
    {
        if (create)
        {
            if (Directory.Exists(folder)) throw new IOException("Use a fresh identity fixture.");
            Directory.CreateDirectory(folder);
        }
        using var owner = await new DatabaseAccessGate().AcquireExclusiveAsync();
        var catalogue = new SqliteDatabaseIdentityCatalogue(Path.Combine(folder, "identities.sqlite"), owner.Validate);
        if (create)
        {
            var other = Guid.NewGuid().ToString("N");
            await File.WriteAllTextAsync(Path.Combine(folder, "other-profile.txt"), other);
            await catalogue.CreateNewAsync(["", other], new PublicationBarrier(folder));
            throw new IOException("Independent publication barrier unexpectedly returned.");
        }
        if (File.Exists(Path.Combine(folder, "identities.sqlite"))) throw new IOException("Interrupted publication exposed a final catalogue.");
        await catalogue.CreateNewAsync(["", await File.ReadAllTextAsync(Path.Combine(folder, "other-profile.txt"))], new AndroidGenerationFileDurability(folder));
        var reopened = await catalogue.ReadAsync();
        if (reopened.Count != 2 || Fingerprint(reopened) != await File.ReadAllTextAsync(Path.Combine(folder, "identity-fingerprint.txt")))
            throw new IOException("Identity catalogue changed across process restart.");
        var initial = reopened.Single(row => row.LegacyProfileId == "");
        await new SqliteDatabaseGenerationJournal(Path.Combine(folder, "main-generation.sqlite"), initial.InitialMigration).CreateNewAsync();
        var journal = await new SqliteDatabaseGenerationJournal(Path.Combine(folder, "main-generation.sqlite"), initial.InitialMigration).ReadAsync();
        if (journal != initial.InitialMigration || initial.ProfileId == Guid.Empty || initial.InitialCandidateKey.InstallationId != initial.InstallationId)
            throw new IOException("Reopened main key/journal context differs.");
        var existing = reopened.Single(row => row.LegacyProfileId != "");
        if (await catalogue.RegisterAsync(existing.LegacyProfileId) != existing) throw new IOException("Registration replaced existing binding.");
        var added = await catalogue.RegisterAsync(Guid.NewGuid().ToString("N"));
        var final = await catalogue.ReadAsync();
        if (final.Count != 3 || added.InstallationId != initial.InstallationId || final.Single(row => row.LegacyProfileId == "") != initial)
            throw new IOException("New registration changed installation or main binding.");
        return "{\"IdentityStableAfterProcessRestart\":true,\"NonemptyMainKeyAndActualJournalBinding\":true,\"ExistingRegistrationUnchanged\":true,\"NewProfileRetainsInstallation\":true,\"NoKeyPreparationOrAppActivation\":true}";
    }

    private sealed class PublicationBarrier(string folder) : IDatabaseGenerationFileDurability
    {
        private readonly AndroidGenerationFileDurability actual = new(folder);
        public long AvailableBytes => actual.AvailableBytes;
        public Task SynchronizeDirectoryAsync(CancellationToken ct) => actual.SynchronizeDirectoryAsync(ct);
        public async Task SynchronizeFileAsync(string path, CancellationToken ct)
        {
            await actual.SynchronizeFileAsync(path, ct);
            var rows = await new SqliteDatabaseIdentityCatalogue(path, () => { }).ReadAsync(ct);
            await File.WriteAllTextAsync(Path.Combine(folder, "identity-fingerprint.txt"), Fingerprint(rows), ct);
            await File.WriteAllTextAsync(Path.Combine(folder, "identity-staged.json"), "{\"IdentityStagedBeforePublication\":true}", ct);
            await Task.Delay(Timeout.Infinite, ct);
        }
    }

    private static string Fingerprint(IReadOnlyList<DatabaseProfileIdentity> rows) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
        string.Join("\n", rows.Select(row => $"{row.InstallationId:N}|{row.LegacyProfileId}|{row.ProfileId:N}|{row.OriginalGeneration:N}|{row.CandidateGeneration:N}")))));
}
