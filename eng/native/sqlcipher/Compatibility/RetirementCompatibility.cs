using System.Runtime.Versioning;
using System.Text.Json;
using Vafadar.Data;

namespace Vafadar.SqlCipher.Compatibility;

/// <summary>Checks retirement ordering with actual private Windows file effects in two independent processes.</summary>
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
            request = new(Guid.NewGuid().ToString("N"), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
            await File.WriteAllTextAsync(context, JsonSerializer.Serialize(new[] { request.LegacyProfileId,
                request.ProfileId.ToString("N"), request.InstallationId.ToString("N"), request.OperationId.ToString("N") }));
            await File.WriteAllTextAsync(Path.Combine(folder, "catalogue.txt"), request.LegacyProfileId);
            await File.WriteAllTextAsync(Path.Combine(folder, "fictitious-artifact.bin"), "Independent non-financial retirement fixture.");
            await File.WriteAllTextAsync(Path.Combine(folder, "identity.txt"), request.ProfileId.ToString("N"));
        }
        else
        {
            var ids = JsonSerializer.Deserialize<string[]>(await File.ReadAllTextAsync(context))!;
            request = new(ids[0], Guid.ParseExact(ids[1], "N"), Guid.ParseExact(ids[2], "N"), Guid.ParseExact(ids[3], "N"));
        }
        var native = new WindowsGenerationFileDurability(folder);
        var durability = new FaultAfterCatalogue(native);
        var host = new FileEffects(folder, native, durability, create);
        var journal = new SqliteProfileRetirementJournal(Path.Combine(folder, "retirement.sqlite"), request, () => { });
        if (create) await journal.CreateNewAsync(native);
        var coordinator = new ProfileRetirementCoordinator(journal, durability, host);
        if (create)
        {
            try { await coordinator.ResumeAsync(); throw new IOException("Expected explicit synchronization failure."); }
            catch (IOException error) when (error.Message == FaultAfterCatalogue.Failure) { }
            if ((await journal.ReadAsync()).Phase != ProfileRetirementPhase.CatalogueRemoved
                || !File.Exists(Path.Combine(folder, "fictitious-artifact.bin")) || !File.Exists(Path.Combine(folder, "identity.txt")))
                throw new IOException("Failed native barrier allowed cleanup.");
            return "{\"CatalogueCommitted\":true,\"InjectedFlushFailure\":true,\"ArtifactsRetained\":true}";
        }
        await coordinator.ResumeAsync(); await coordinator.ResumeAsync();
        if ((await journal.ReadAsync()).Phase != ProfileRetirementPhase.Completed)
            throw new IOException("Cold retirement did not complete.");
        return "{\"ColdResume\":true,\"NativeBarriersRequalified\":true,\"CapturedFictitiousFilesRemoved\":true}";
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
        FaultAfterCatalogue fault, bool inject) : IProfileRetirementEffects
    {
        private string Catalogue => Path.Combine(folder, "catalogue.txt");
        private string Artifact => Path.Combine(folder, "fictitious-artifact.bin");
        private string Identity => Path.Combine(folder, "identity.txt");
        public async Task ValidateAsync(ProfileRetirementCheckpoint checkpoint, CancellationToken ct)
        {
            using var root = new WindowsPrivateFiles(folder);
            using var catalogue = root.OpenFile(Catalogue, writable: false);
            var value = await File.ReadAllTextAsync(Catalogue, ct);
            if (value != (checkpoint.Phase == ProfileRetirementPhase.Authorized ? checkpoint.LegacyProfileId : ""))
                throw new IOException("Fictitious catalogue does not match the recorded retirement barrier.");
            if (checkpoint.Phase >= ProfileRetirementPhase.ArtifactsRemoved && File.Exists(Artifact))
                throw new IOException("Recorded artifact removal did not occur.");
            if (checkpoint.Phase == ProfileRetirementPhase.Completed && File.Exists(Identity))
                throw new IOException("Recorded identity retirement did not occur.");
        }
        public async Task RemoveCatalogueAsync(ProfileRetirementCheckpoint checkpoint, CancellationToken ct)
        {
            await File.WriteAllTextAsync(Catalogue, "", ct);
            await native.SynchronizeFileAsync(Catalogue, ct); await native.SynchronizeDirectoryAsync(ct);
            fault.Fail = inject;
        }
        public Task RemoveArtifactsAsync(ProfileRetirementCheckpoint checkpoint, CancellationToken ct) => RemoveAsync(Artifact, ct);
        public Task RetireIdentityAsync(ProfileRetirementCheckpoint checkpoint, CancellationToken ct) => RemoveAsync(Identity, ct);
        private async Task RemoveAsync(string path, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (File.Exists(path))
            {
                using (var root = new WindowsPrivateFiles(folder))
                using (var handle = root.OpenFile(path, writable: true)) { }
                File.Delete(path);
            }
            await native.SynchronizeDirectoryAsync(ct);
        }
    }
}
