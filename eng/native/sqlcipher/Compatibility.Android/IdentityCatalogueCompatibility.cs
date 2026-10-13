using System.Security.Cryptography;
using System.Text;
using Vafadar.Data;

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
            await catalogue.CreateNewAsync(["", Guid.NewGuid().ToString("N")]);
            var rows = await catalogue.ReadAsync();
            await File.WriteAllTextAsync(Path.Combine(folder, "identity-fingerprint.txt"), Fingerprint(rows));
            var main = rows.Single(row => row.LegacyProfileId == "");
            await new SqliteDatabaseGenerationJournal(Path.Combine(folder, "main-generation.sqlite"), main.InitialMigration).CreateNewAsync();
            return "{\"CommittedIdentityCatalogue\":true,\"MainInitialJournalCreated\":true}";
        }
        var reopened = await catalogue.ReadAsync();
        if (reopened.Count != 2 || Fingerprint(reopened) != await File.ReadAllTextAsync(Path.Combine(folder, "identity-fingerprint.txt")))
            throw new IOException("Identity catalogue changed across process restart.");
        var initial = reopened.Single(row => row.LegacyProfileId == "");
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

    private static string Fingerprint(IReadOnlyList<DatabaseProfileIdentity> rows) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
        string.Join("\n", rows.Select(row => $"{row.InstallationId:N}|{row.LegacyProfileId}|{row.ProfileId:N}|{row.OriginalGeneration:N}|{row.CandidateGeneration:N}")))));
}
