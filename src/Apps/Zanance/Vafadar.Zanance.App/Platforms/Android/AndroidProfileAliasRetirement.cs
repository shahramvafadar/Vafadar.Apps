using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Vafadar.Data;

namespace Vafadar.Zanance.App.Security;

/// <summary>Records authenticated independent wrapping aliases before profile removal and retires them only after exact file cleanup.</summary>
/// <remarks>Shared legacy aliases and unauthenticated unavailable aliases remain retained. No alias is inferred from its name.</remarks>
internal sealed class AndroidProfileAliasRetirement(string root, IReadOnlyDictionary<string,string> roots,
    AndroidRecoveryKeyStore keys, AndroidGenerationFileDurability durability, Action validate)
{
    private const int MaximumBytes = 2 * 1024 * 1024;
    private AndroidProfileRetirementFiles Files => new(roots, validate);
    private static string Name(ProfileRetirementCheckpoint state) => "profile-alias-retirement-" + state.OperationId.ToString("N") + ".json";

    /// <summary>Authenticates the complete captured envelope inventory while catalogue and files are still retained.</summary>
    internal async Task CaptureAsync(ProfileRetirementCheckpoint expected, SqliteProfileRetirementJournal journal, bool mayCreate, CancellationToken ct)
    {
        var state = await journal.SynchronizeAndReadAsync(durability, ct).ConfigureAwait(false);
        if (state != expected || state.Phase != ProfileRetirementPhase.Authorized) throw new IOException("Native alias capture requires authorized retirement.");
        var artifacts = await journal.ReadArtifactsAsync(ct).ConfigureAwait(false);
        var aliases = new HashSet<Guid>(); var files = Files;
        foreach (var artifact in artifacts.Where(file => file.Root == "keys" &&
            (file.Name.EndsWith(".key", StringComparison.Ordinal) || file.Name.EndsWith(".key.recovered", StringComparison.Ordinal))))
        {
            if (await files.CaptureAsync("keys", artifact.Name, ct).ConfigureAwait(false) != artifact)
                throw new IOException("Alias capture envelope changed.");
            if (artifact.Name.Length < 69 || !Guid.TryParseExact(artifact.Name.AsSpan(33,32), "N", out var generation)
                || !artifact.Name.StartsWith(state.ProfileId.ToString("N") + "-", StringComparison.Ordinal))
                throw new IOException("Alias capture has a foreign generation.");
            var binding = new DatabaseKeyBinding(state.ProfileId, state.InstallationId, generation);
            var key = await keys.ReadAsync(binding, ct).ConfigureAwait(false);
            CryptographicOperations.ZeroMemory(key); // Existing exact native or receipt-bound recovered ownership is mandatory.
            var envelope = await files.ReadAsync("keys", artifact.Name, 82, false, ct).ConfigureAwait(false);
            try
            {
                if (envelope.Length != 82) continue;
                try
                { aliases.Add(AndroidDatabaseKeyProtector.CaptureIndependentAlias(envelope, binding, artifact.Name.EndsWith(".recovered", StringComparison.Ordinal))); }
                catch (CryptographicException) when (!artifact.Name.EndsWith(".recovered", StringComparison.Ordinal)
                    && artifacts.Any(file => file.Root == "keys" && file.Name == artifact.Name + ".recovered"))
                { /* Receipt-bound recovered ownership does not authenticate an unavailable old alias; retain it. */ }
            }
            finally { CryptographicOperations.ZeroMemory(envelope); }
        }
        foreach (var alias in aliases)
            await AndroidAliasReferences.RequireUnreferencedAsync(alias, validate, ct, root, artifacts, roots["keys"]).ConfigureAwait(false);
        var packet = new ProfileAliasRetirementPacket(1, state, Digest(artifacts), aliases.Order().ToArray());
        var bytes = JsonSerializer.SerializeToUtf8Bytes(packet, ProfileAliasJson.Default.ProfileAliasRetirementPacket);
        var name = Name(state); var path = Path.Combine(root, name);
        var names = await files.ListNamesAsync("database", ct).ConfigureAwait(false);
        if (!names.Contains(name, StringComparer.Ordinal))
        {
            if (!mayCreate) throw new IOException("Missing native alias capture cannot be reconstructed after catalogue publication.");
            var stage = path + ".creating";
            if (!File.Exists(stage))
            {
                validate();
                await using var output = new FileStream(stage, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                await output.WriteAsync(bytes, ct).ConfigureAwait(false); output.Flush(true);
            }
            var staged = await files.ReadAsync("database", name + ".creating", MaximumBytes, false, ct).ConfigureAwait(false);
            if (!staged.AsSpan().SequenceEqual(bytes)) throw new IOException("Staged alias authority differs.");
            await durability.SynchronizeFileAsync(stage, ct).ConfigureAwait(false);
            validate(); File.Move(stage, path, overwrite:false);
            await durability.SynchronizeDirectoryAsync(ct).ConfigureAwait(false);
        }
        var stored = await files.ReadAsync("database", name, MaximumBytes, false, ct).ConfigureAwait(false);
        if (!stored.AsSpan().SequenceEqual(bytes)) throw new IOException("Captured alias authority differs.");
        await durability.SynchronizeFileAsync(path, ct).ConfigureAwait(false);
    }

    /// <summary>Requires durable catalogue removal, exact envelope cleanup and absence of every remaining installation reference.</summary>
    internal async Task RemoveAsync(ProfileRetirementCheckpoint expected, SqliteProfileRetirementJournal journal, CancellationToken ct)
    {
        var packet = await ReadAsync(journal, ct).ConfigureAwait(false);
        var state = await journal.SynchronizeAndReadAsync(durability, ct).ConfigureAwait(false);
        if (state != expected || state.Phase != ProfileRetirementPhase.CatalogueRemoved)
            throw new IOException("Alias removal requires durable catalogue removal.");
        var removing = await journal.ReadArtifactsAsync(ct).ConfigureAwait(false);
        foreach (var alias in packet.Aliases)
            await AndroidAliasReferences.RequireUnreferencedAsync(alias, validate, ct, root, removing, roots["keys"]).ConfigureAwait(false);
        await Files.RemoveAsync(state, journal, durability, ct).ConfigureAwait(false);
        foreach (var alias in packet.Aliases)
        {
            _ = await ReadAsync(journal, ct).ConfigureAwait(false);
            await AndroidAliasReferences.RequireUnreferencedAsync(alias, validate, ct, root).ConfigureAwait(false);
            AndroidDatabaseKeyProtector.RemoveCapturedIndependentAlias(alias, validate);
        }
        await VerifyAsync(journal, ct).ConfigureAwait(false);
    }

    /// <summary>Requalifies recorded native alias authority and verifies completed file/alias effects without repeating deletion.</summary>
    internal async Task VerifyAsync(SqliteProfileRetirementJournal journal, CancellationToken ct)
    {
        var packet = await ReadAsync(journal, ct).ConfigureAwait(false);
        await Files.VerifyRemovedAsync(journal, ct).ConfigureAwait(false);
        foreach (var alias in packet.Aliases) AndroidDatabaseKeyProtector.RequireIndependentAliasAbsent(alias);
    }

    private async Task<ProfileAliasRetirementPacket> ReadAsync(SqliteProfileRetirementJournal journal, CancellationToken ct)
    {
        var state = await journal.SynchronizeAndReadAsync(durability, ct).ConfigureAwait(false);
        if (state.Phase < ProfileRetirementPhase.CatalogueRemoved) throw new IOException("Native alias cleanup precedes catalogue removal.");
        var name = Name(state);
        var bytes = await Files.ReadAsync("database", name, MaximumBytes, false, ct).ConfigureAwait(false);
        var packet = JsonSerializer.Deserialize(bytes, ProfileAliasJson.Default.ProfileAliasRetirementPacket)
            ?? throw new IOException("Native alias retirement authority is absent.");
        var original = new ProfileRetirementCheckpoint(state.LegacyProfileId,state.ProfileId,state.InstallationId,state.OperationId);
        if (packet.Schema != 1 || packet.Request != original || packet.Aliases is null || packet.Aliases.Length > 4096
            || packet.Aliases.Any(alias => alias == Guid.Empty) || packet.Aliases.Distinct().Count() != packet.Aliases.Length
            || packet.ArtifactDigest != Digest(await journal.ReadArtifactsAsync(ct).ConfigureAwait(false)))
            throw new IOException("Native alias retirement authority changed.");
        await durability.SynchronizeFileAsync(Path.Combine(root,name), ct).ConfigureAwait(false);
        if (!(await Files.ReadAsync("database",name,MaximumBytes,false,ct).ConfigureAwait(false)).AsSpan().SequenceEqual(bytes))
            throw new IOException("Native alias authority changed during synchronization.");
        validate(); return packet;
    }
    private static string Digest(IReadOnlyList<ProfileRetirementArtifact> artifacts) => Convert.ToHexStringLower(SHA256.HashData(
        JsonSerializer.SerializeToUtf8Bytes(artifacts.ToArray(), ProfileAliasJson.Default.ProfileRetirementArtifactArray)));
}

/// <summary>Non-secret authenticated alias identities bound to one immutable profile retirement request and file inventory.</summary>
internal sealed record ProfileAliasRetirementPacket(int Schema, ProfileRetirementCheckpoint Request, string ArtifactDigest, Guid[] Aliases);

/// <summary>Serializes non-secret alias authority bound to one reviewed profile retirement.</summary>
[JsonSerializable(typeof(ProfileAliasRetirementPacket))]
[JsonSerializable(typeof(ProfileRetirementArtifact[]))]
internal partial class ProfileAliasJson : JsonSerializerContext { }
