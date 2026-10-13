using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Vafadar.Data;

namespace Vafadar.SqlCipher.Compatibility;

/// <summary>Actual native capture/removal callbacks, supplied by each platform without copying the cleanup algorithm.</summary>
internal sealed record RetirementNativeFiles(
    Func<string, string, CancellationToken, Task<ProfileRetirementArtifact>> Capture,
    Func<ProfileRetirementCheckpoint, SqliteProfileRetirementJournal, IDatabaseGenerationFileDurability, CancellationToken, Task> Remove,
    Func<SqliteProfileRetirementJournal, CancellationToken, Task> VerifyRemoved);

/// <summary>Shared exact inventory, native key-envelope cleanup and cold identity-retirement scenario.</summary>
internal static class SharedRetirementCompatibility
{
    /// <summary>Runs only one explicit create or cold continuation in its independently owned fictitious directory.</summary>
    internal static async Task<string> RunAsync(string folder, bool create,
        Func<string, IDatabaseGenerationKeyStore> makeKeys, Func<string, IDatabaseGenerationFileDurability> makeDurability,
        Func<string, RetirementNativeFiles> makeFiles)
    {
        SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_sqlcipher());
        var context = Path.Combine(folder, "request.json");
        ProfileRetirementCheckpoint request; DatabaseProfileIdentity main;
        if (create)
        {
            if (Directory.Exists(folder)) throw new IOException("Retirement proof requires a fresh independently owned directory.");
            Directory.CreateDirectory(folder);
            var legacy = Guid.NewGuid().ToString("N"); var catalogue = Identities(folder);
            await catalogue.CreateNewAsync(["", legacy], makeDurability(folder));
            var rows = await catalogue.ReadAsync(); var target = rows.Single(row => row.LegacyProfileId == legacy);
            request = new(legacy, target.ProfileId, target.InstallationId, Guid.NewGuid());
            main = rows.Single(row => row.LegacyProfileId == "");
            await WriteIdsAsync(context, [request.LegacyProfileId, request.ProfileId.ToString("N"), request.InstallationId.ToString("N"), request.OperationId.ToString("N")]);
            await WriteIdsAsync(Path.Combine(folder, "main.json"), [main.InstallationId.ToString("N"), main.ProfileId.ToString("N"), main.OriginalGeneration.ToString("N"), main.CandidateGeneration.ToString("N")]);
            await File.WriteAllTextAsync(Path.Combine(folder, "fictitious-artifact.bin"), "Independent non-financial retirement fixture.");
            await File.WriteAllTextAsync(Path.Combine(folder, "catalogue.txt"), request.LegacyProfileId);
            var createdKeys = makeKeys(folder); await createdKeys.PrepareAsync(main.InitialCandidateKey, CancellationToken.None);
            await createdKeys.PrepareAsync(new(request.ProfileId, request.InstallationId, request.OperationId), CancellationToken.None);
        }
        else
        {
            var ids = await ReadIdsAsync(context); request = new(ids[0], Parse(ids[1]), Parse(ids[2]), Parse(ids[3]));
            var savedMain = await ReadIdsAsync(Path.Combine(folder, "main.json"));
            main = new(Parse(savedMain[0]), "", Parse(savedMain[1]), Parse(savedMain[2]), Parse(savedMain[3]));
        }
        var native = makeDurability(folder); var fault = new FaultAfterCatalogue(native); var files = makeFiles(folder);
        var journal = new SqliteProfileRetirementJournal(Path.Combine(folder, "retirement.sqlite"), request, () => { });
        var keyName = $"{request.ProfileId:N}-{request.OperationId:N}.key";
        if (create)
        {
            await CheckChangedObjectsAsync(folder, makeDurability, makeFiles);
            await journal.CreateNewAsync(native);
            await journal.CaptureArtifactsAsync([await files.Capture("database", "fictitious-artifact.bin", CancellationToken.None),
                await files.Capture("database", keyName, CancellationToken.None)], native);
        }
        var host = new Effects(folder, main, native, fault, files, journal, create);
        var coordinator = new ProfileRetirementCoordinator(journal, fault, host);
        if (create)
        {
            try { await coordinator.ResumeAsync(); throw new IOException("Expected explicit synchronization failure."); }
            catch (IOException error) when (error.Message == FaultAfterCatalogue.Failure) { }
            if ((await journal.ReadAsync()).Phase != ProfileRetirementPhase.CatalogueRemoved
                || !File.Exists(Path.Combine(folder, "fictitious-artifact.bin")) || !File.Exists(Path.Combine(folder, keyName))
                || !(await Identities(folder).ReadAsync()).Any(row => row.ProfileId == request.ProfileId))
                throw new IOException("Failed native barrier allowed cleanup.");
            return "{\"CatalogueCommitted\":true,\"InjectedFlushFailure\":true,\"ArtifactsRetained\":true,\"NativeKeyEnvelopeRetained\":true,\"ChangedObjectRefusals\":3}";
        }
        await coordinator.ResumeAsync(); await coordinator.ResumeAsync();
        if ((await journal.ReadAsync()).Phase != ProfileRetirementPhase.Completed || File.Exists(Path.Combine(folder, keyName)))
            throw new IOException("Cold exact retirement did not finish.");
        var existingKeys = makeKeys(folder); var remaining = await existingKeys.ReadAsync(main.InitialCandidateKey, CancellationToken.None);
        try { if (remaining.Length != 32) throw new IOException("Retirement damaged the retained main key."); }
        finally { CryptographicOperations.ZeroMemory(remaining); }
        return "{\"ColdResume\":true,\"NativeBarriersRequalified\":true,\"CapturedFictitiousFilesRemoved\":true,\"ExactIdentityRetired\":true,\"MainBindingPreserved\":true,\"TargetKeyEnvelopeRemoved\":true,\"MainKeyReadable\":true}";
    }
    private static SqliteDatabaseIdentityCatalogue Identities(string folder) => new(Path.Combine(folder, "identities.sqlite"), () => { });
    /// <summary>Checks changed bytes, same-content file replacement and replacement of an emptied namespace root.</summary>
    internal static async Task CheckChangedObjectsAsync(string folder, Func<string, IDatabaseGenerationFileDurability> makeDurability,
        Func<string, RetirementNativeFiles> makeFiles)
    {
        foreach (var kind in new[] { "bytes", "file-object", "root-object" })
        {
            var scenario = Path.Combine(folder, "negative-" + kind); var objects = Path.Combine(scenario, "objects");
            Directory.CreateDirectory(objects); var native = makeDurability(scenario); var files = makeFiles(objects);
            await File.WriteAllTextAsync(Path.Combine(objects, "a.bin"), "Original A");
            await File.WriteAllTextAsync(Path.Combine(objects, "b.bin"), "Original B");
            var request = new ProfileRetirementCheckpoint(Guid.NewGuid().ToString("N"), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
            var journal = new SqliteProfileRetirementJournal(Path.Combine(scenario, "retirement.sqlite"), request, () => { });
            await journal.CreateNewAsync(native);
            await journal.CaptureArtifactsAsync([await files.Capture("database", "a.bin", CancellationToken.None),
                await files.Capture("database", "b.bin", CancellationToken.None)], native);
            await journal.CompareAndAdvanceAsync(request, native);
            if (kind == "root-object") { Directory.Move(objects, objects + ".retained"); Directory.CreateDirectory(objects); }
            else
            {
                if (kind == "file-object") File.Move(Path.Combine(objects, "b.bin"), Path.Combine(objects, "retained-old.bin"));
                await File.WriteAllTextAsync(Path.Combine(objects, "b.bin"), kind == "bytes" ? "Changed B!" : "Original B");
            }
            var refused = false;
            try { await files.Remove(request.Advance(), journal, native, CancellationToken.None); }
            catch (IOException) { refused = true; }
            var retained = kind == "root-object" ? objects + ".retained" : objects;
            if (!refused || await File.ReadAllTextAsync(Path.Combine(retained, "a.bin")) != "Original A"
                || await File.ReadAllTextAsync(Path.Combine(retained, "b.bin")) != (kind == "bytes" ? "Changed B!" : "Original B"))
                throw new IOException("Changed native retirement object was not preserved before cleanup.");
        }
    }
    private static Guid Parse(string value) => Guid.ParseExact(value, "N");
    private static Task WriteIdsAsync(string path, string[] ids) => File.WriteAllTextAsync(path, JsonSerializer.Serialize(ids, RetirementJsonContext.Default.StringArray));
    private static async Task<string[]> ReadIdsAsync(string path)
    {
        var ids = JsonSerializer.Deserialize(await File.ReadAllTextAsync(path), RetirementJsonContext.Default.StringArray);
        return ids is { Length: 4 } ? ids : throw new IOException("Fictitious retirement identity packet differs.");
    }
    private sealed class FaultAfterCatalogue(IDatabaseGenerationFileDurability native) : IDatabaseGenerationFileDurability
    {
        internal const string Failure = "Injected retirement native synchronization failure.";
        internal bool Fail;
        public long AvailableBytes => native.AvailableBytes;
        public Task SynchronizeFileAsync(string path, CancellationToken ct) => Fail ? Task.FromException(new IOException(Failure)) : native.SynchronizeFileAsync(path, ct);
        public Task SynchronizeDirectoryAsync(CancellationToken ct) => native.SynchronizeDirectoryAsync(ct);
    }
    private sealed class Effects(string folder, DatabaseProfileIdentity main, IDatabaseGenerationFileDurability native,
        FaultAfterCatalogue fault, RetirementNativeFiles files, SqliteProfileRetirementJournal journal, bool inject) : IProfileRetirementEffects
    {
        public async Task ValidateAsync(ProfileRetirementCheckpoint checkpoint, CancellationToken ct)
        {
            var catalogue = await File.ReadAllTextAsync(Path.Combine(folder, "catalogue.txt"), ct);
            if (catalogue != (checkpoint.Phase == ProfileRetirementPhase.Authorized ? checkpoint.LegacyProfileId : ""))
                throw new IOException("Fictitious catalogue does not match retirement.");
            if (checkpoint.Phase >= ProfileRetirementPhase.ArtifactsRemoved) await files.VerifyRemoved(journal, ct);
            var rows = await Identities(folder).ReadAsync(ct);
            if (rows.Single(row => row.LegacyProfileId == "") != main) throw new IOException("Retirement changed main identity.");
            if (checkpoint.Phase == ProfileRetirementPhase.Completed && rows.Any(row => row.ProfileId == checkpoint.ProfileId))
                throw new IOException("Recorded identity retirement did not occur.");
        }
        public async Task RemoveCatalogueAsync(ProfileRetirementCheckpoint checkpoint, CancellationToken ct)
        {
            await File.WriteAllTextAsync(Path.Combine(folder, "catalogue.txt"), "", ct);
            await native.SynchronizeFileAsync(Path.Combine(folder, "catalogue.txt"), ct); await native.SynchronizeDirectoryAsync(ct);
            fault.Fail = inject;
        }
        public Task RemoveArtifactsAsync(ProfileRetirementCheckpoint checkpoint, CancellationToken ct) => files.Remove(checkpoint, journal, native, ct);
        public Task RetireIdentityAsync(ProfileRetirementCheckpoint checkpoint, CancellationToken ct) =>
            Identities(folder).RetireAsync(checkpoint, journal, native, token => files.VerifyRemoved(journal, token), ct);
    }
}

/// <summary>Trim-safe serializer for independent non-secret fixture identities.</summary>
[JsonSerializable(typeof(string[]))]
internal partial class RetirementJsonContext : JsonSerializerContext;
