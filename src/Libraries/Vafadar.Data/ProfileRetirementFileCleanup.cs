namespace Vafadar.Data;

/// <summary>Shared ordered file-cleanup algorithm over actual native captured-object effects.</summary>
public static class ProfileRetirementFileCleanup
{
    /// <summary>Requalifies existing authority, preflights every object, then removes and verifies only that inventory.</summary>
    public static async Task RemoveAsync(ProfileRetirementCheckpoint expected, SqliteProfileRetirementJournal journal,
        IDatabaseGenerationFileDurability metadataDurability,
        Func<ProfileRetirementArtifact, bool, CancellationToken, Task> inspectOrDelete,
        Func<CancellationToken, Task> verifyRemoved, Action validateAccess, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(expected); ArgumentNullException.ThrowIfNull(journal);
        ArgumentNullException.ThrowIfNull(metadataDurability); ArgumentNullException.ThrowIfNull(inspectOrDelete);
        ArgumentNullException.ThrowIfNull(verifyRemoved); ArgumentNullException.ThrowIfNull(validateAccess);
        void Check() { ct.ThrowIfCancellationRequested(); validateAccess(); }
        Check();
        if (expected.Phase != ProfileRetirementPhase.CatalogueRemoved
            || await journal.SynchronizeAndReadAsync(metadataDurability, ct).ConfigureAwait(false) != expected)
            throw new InvalidOperationException("File cleanup requires the exact durable catalogue-removal barrier.");
        var captured = await journal.ReadArtifactsAsync(ct).ConfigureAwait(false);
        foreach (var artifact in captured) { Check(); await inspectOrDelete(artifact, false, ct).ConfigureAwait(false); }
        foreach (var artifact in captured)
        {
            Check();
            if (await journal.ReadAsync(ct).ConfigureAwait(false) != expected)
                throw new InvalidOperationException("Retirement changed before exact file deletion.");
            await inspectOrDelete(artifact, true, ct).ConfigureAwait(false);
        }
        await verifyRemoved(ct).ConfigureAwait(false); Check();
    }
}
