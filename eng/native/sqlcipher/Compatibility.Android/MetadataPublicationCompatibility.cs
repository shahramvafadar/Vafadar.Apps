using Vafadar.Data;
using Vafadar.Zanance.App.Security;

namespace Vafadar.SqlCipher.Compatibility.Android;

/// <summary>Interrupts actual native journal/history staging before publication, using only private fictitious records.</summary>
internal static class MetadataPublicationCompatibility
{
    /// <summary>Stages or resumes the same captured metadata across externally terminated processes.</summary>
    internal static async Task<string> RunAsync(string folder, string phase)
    {
        if (phase == "metadata-journal")
        {
            if (Directory.Exists(folder)) throw new IOException("Use a fresh independent metadata fixture.");
            Directory.CreateDirectory(folder);
            await File.WriteAllLinesAsync(Path.Combine(folder, "binding.txt"), Enumerable.Range(0, 4).Select(_ => Guid.NewGuid().ToString("N")));
        }
        var ids = (await File.ReadAllLinesAsync(Path.Combine(folder, "binding.txt"))).Select(value => Guid.ParseExact(value, "N")).ToArray();
        var captured = new DatabaseGenerationCheckpoint(ids[0], ids[1], ids[2]);
        var journalPath = Path.Combine(folder, "generation.sqlite");
        var historyPath = Path.Combine(folder, "history.sqlite");
        var journal = new SqliteDatabaseGenerationJournal(journalPath, captured);
        var history = new SqliteProfileRestoreHistory(historyPath, new(ids[0], ids[3], ids[2]), () => { });
        var durability = new AndroidGenerationFileDurability(folder);
        if (phase == "metadata-journal")
        {
            await journal.CreateNewAsync(new Barrier(folder, durability));
            throw new IOException("Journal staging barrier unexpectedly returned.");
        }
        if (phase == "metadata-history")
        {
            if (File.Exists(journalPath)) throw new IOException("Interrupted journal was already published.");
            await journal.CreateNewAsync(durability);
            await history.CreateInitialIntentAsync(captured, journal, new Barrier(folder, durability));
            throw new IOException("History staging barrier unexpectedly returned.");
        }
        if (File.Exists(historyPath)) throw new IOException("Interrupted history was already published.");
        await history.CreateInitialIntentAsync(captured, journal, durability);
        if (await journal.ReadAsync() != captured || (await history.ReadAsync()) is not [{ Selected: false } row] || row.CandidateGeneration != ids[2])
            throw new IOException("Native captured metadata changed after staged restart.");
        if (File.Exists(journalPath + ".creating") || File.Exists(historyPath + ".creating")) throw new IOException("Published metadata retained staging aliases.");
        return "{\"JournalAndHistoryStagingResumed\":true,\"CapturedBindingsUnchanged\":true,\"InitialIntentStillPending\":true,\"NoKeyPreparationOrFinancialData\":true}";
    }

    private sealed class Barrier(string folder, AndroidGenerationFileDurability actual) : IDatabaseGenerationFileDurability
    {
        /// <inheritdoc />
        public long AvailableBytes => actual.AvailableBytes;
        /// <inheritdoc />
        public Task SynchronizeDirectoryAsync(CancellationToken ct) => actual.SynchronizeDirectoryAsync(ct);
        /// <inheritdoc />
        public async Task SynchronizeFileAsync(string path, CancellationToken ct)
        {
            await actual.SynchronizeFileAsync(path, ct);
            await File.WriteAllTextAsync(Path.Combine(folder, "metadata-staged.json"), "{\"CommittedMetadataStaged\":true}", ct);
            await Task.Delay(Timeout.Infinite, ct);
        }
    }
}
