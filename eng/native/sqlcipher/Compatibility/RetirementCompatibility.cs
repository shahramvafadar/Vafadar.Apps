using System.Runtime.Versioning;
using System.Text.Json;
using Vafadar.Data;

namespace Vafadar.SqlCipher.Compatibility;

/// <summary>Checks retirement ordering and real identity-catalogue removal in two independent Windows processes.</summary>
/// <remarks>The catalogue/artifacts are fictitious files, not final MAUI preferences or live profile references.</remarks>
[SupportedOSPlatform("windows")]
internal static class RetirementCompatibility
{
    /// <summary>Records a native-flush failure after catalogue commit, or resumes that exact retained request.</summary>
    internal static async Task<string> RunAsync(string folder, bool create)
    {
        SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_sqlcipher());
        var context = Path.Combine(folder, "request.json");
        ProfileRetirementCheckpoint request;
        if (create)
        {
            if (Directory.Exists(folder)) throw new IOException("Retirement proof requires a new independently owned directory.");
            Directory.CreateDirectory(folder);
            var legacy = Guid.NewGuid().ToString("N");
            var catalogue = new SqliteDatabaseIdentityCatalogue(Path.Combine(folder, "identities.sqlite"), () => { });
            await catalogue.CreateNewAsync(["", legacy], new WindowsGenerationFileDurability(folder));
            var rows = await catalogue.ReadAsync(); var target = rows.Single(row => row.LegacyProfileId == legacy);
            request = new(legacy, target.ProfileId, target.InstallationId, Guid.NewGuid());
            await File.WriteAllTextAsync(context, JsonSerializer.Serialize(new[] { request.LegacyProfileId,
                request.ProfileId.ToString("N"), request.InstallationId.ToString("N"), request.OperationId.ToString("N") }));
            await File.WriteAllTextAsync(Path.Combine(folder, "catalogue.txt"), request.LegacyProfileId);
            await File.WriteAllTextAsync(Path.Combine(folder, "fictitious-artifact.bin"), "Independent non-financial retirement fixture.");
            await File.WriteAllTextAsync(Path.Combine(folder, "main.json"), JsonSerializer.Serialize(rows.Single(row => row.LegacyProfileId == "")));
        }
        else
        {
            var ids = JsonSerializer.Deserialize<string[]>(await File.ReadAllTextAsync(context))!;
            request = new(ids[0], Guid.ParseExact(ids[1], "N"), Guid.ParseExact(ids[2], "N"), Guid.ParseExact(ids[3], "N"));
        }
        var native = new WindowsGenerationFileDurability(folder);
        var durability = new FaultAfterCatalogue(native);
        var journal = new SqliteProfileRetirementJournal(Path.Combine(folder, "retirement.sqlite"), request, () => { });
        var host = new FileEffects(folder, native, durability, create, journal);
        if (create)
        {
            await journal.CreateNewAsync(native);
            var files = new WindowsProfileRetirementFiles(new Dictionary<string, string> { ["database"] = folder }, () => { });
            await journal.CaptureArtifactsAsync([await files.CaptureAsync("database", "fictitious-artifact.bin")], native);
        }
        var coordinator = new ProfileRetirementCoordinator(journal, durability, host);
        if (create)
        {
            try { await coordinator.ResumeAsync(); throw new IOException("Expected explicit synchronization failure."); }
            catch (IOException error) when (error.Message == FaultAfterCatalogue.Failure) { }
            if ((await journal.ReadAsync()).Phase != ProfileRetirementPhase.CatalogueRemoved
                || !File.Exists(Path.Combine(folder, "fictitious-artifact.bin"))
                || !(await new SqliteDatabaseIdentityCatalogue(Path.Combine(folder, "identities.sqlite"), () => { }).ReadAsync())
                    .Any(row => row.ProfileId == request.ProfileId))
                throw new IOException("Failed native barrier allowed cleanup.");
            return "{\"CatalogueCommitted\":true,\"InjectedFlushFailure\":true,\"ArtifactsRetained\":true}";
        }
        await coordinator.ResumeAsync(); await coordinator.ResumeAsync();
        if ((await journal.ReadAsync()).Phase != ProfileRetirementPhase.Completed)
            throw new IOException("Cold retirement did not complete.");
        return "{\"ColdResume\":true,\"NativeBarriersRequalified\":true,\"CapturedFictitiousFilesRemoved\":true,\"ExactIdentityRetired\":true,\"MainBindingPreserved\":true}";
    }

    private sealed class FaultAfterCatalogue(IDatabaseGenerationFileDurability native) : IDatabaseGenerationFileDurability
    {
        internal const string Failure = "Injected retirement native synchronization failure.";
        internal bool Fail;
        public long AvailableBytes => native.AvailableBytes;
        public Task SynchronizeFileAsync(string path, CancellationToken ct) => Fail
            ? Task.FromException(new IOException(Failure)) : native.SynchronizeFileAsync(path, ct);
        public Task SynchronizeDirectoryAsync(CancellationToken ct) => native.SynchronizeDirectoryAsync(ct);
    }

    private sealed class FileEffects(string folder, IDatabaseGenerationFileDurability native,
        FaultAfterCatalogue fault, bool inject, SqliteProfileRetirementJournal journal) : IProfileRetirementEffects
    {
        private string Catalogue => Path.Combine(folder, "catalogue.txt");
        private string Artifact => Path.Combine(folder, "fictitious-artifact.bin");
        private SqliteDatabaseIdentityCatalogue Identities => new(Path.Combine(folder, "identities.sqlite"), () => { });
        private WindowsProfileRetirementFiles Files => new(new Dictionary<string, string> { ["database"] = folder }, () => { });
        public async Task ValidateAsync(ProfileRetirementCheckpoint checkpoint, CancellationToken ct)
        {
            using var root = new WindowsPrivateFiles(folder);
            using var catalogue = root.OpenFile(Catalogue, writable: false);
            var value = await File.ReadAllTextAsync(Catalogue, ct);
            if (value != (checkpoint.Phase == ProfileRetirementPhase.Authorized ? checkpoint.LegacyProfileId : ""))
                throw new IOException("Fictitious catalogue does not match the recorded retirement barrier.");
            if (checkpoint.Phase >= ProfileRetirementPhase.ArtifactsRemoved && File.Exists(Artifact))
                throw new IOException("Recorded artifact removal did not occur.");
            var rows = await Identities.ReadAsync(ct);
            var main = JsonSerializer.Deserialize<DatabaseProfileIdentity>(await File.ReadAllTextAsync(Path.Combine(folder, "main.json"), ct));
            if (rows.Single(row => row.LegacyProfileId == "") != main)
                throw new IOException("Retirement changed the retained main binding.");
            if (checkpoint.Phase == ProfileRetirementPhase.Completed && rows.Any(row => row.ProfileId == checkpoint.ProfileId))
                throw new IOException("Recorded identity retirement did not occur.");
        }
        public async Task RemoveCatalogueAsync(ProfileRetirementCheckpoint checkpoint, CancellationToken ct)
        {
            await File.WriteAllTextAsync(Catalogue, "", ct);
            await native.SynchronizeFileAsync(Catalogue, ct); await native.SynchronizeDirectoryAsync(ct);
            fault.Fail = inject;
        }
        public Task RemoveArtifactsAsync(ProfileRetirementCheckpoint checkpoint, CancellationToken ct) => Files.RemoveAsync(checkpoint, journal, native, ct);
        public Task RetireIdentityAsync(ProfileRetirementCheckpoint checkpoint, CancellationToken ct) =>
            Identities.RetireAsync(checkpoint, journal, native, token => Files.VerifyRemovedAsync(journal, token), ct);
    }
}
