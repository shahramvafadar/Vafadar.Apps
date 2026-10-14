using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Vafadar.Backup;
using Vafadar.Backup.Security;
using Vafadar.Data;

namespace Vafadar.Zanance.App.Security;

/// <summary>Retires only authenticated obsolete safety copies and unreferenced independent keys under durable native intent.</summary>
/// <remarks>Financial ancestry, staged/archived installations and foreign safety files remain retained. Unknown or changed
/// references refuse cleanup. This does not claim physical erasure or hardware power-loss acceptance.</remarks>
internal sealed class AndroidSafetyRetention(string root, IReadOnlyDictionary<string, string> roots,
    AndroidRecoveryKeyStore keys, SqliteRecoveryEnvelopeCatalogue? recovery,
    AndroidGenerationFileDurability durability, Action validate)
{
    private const string Prefix = "protected-safety-retirement-";
    private const int MaximumIntentBytes = 2 * 1024 * 1024;
    private AndroidProfileRetirementFiles Files => new(roots, validate);

    /// <summary>Continues existing immutable retention intents before new keys can be re-enrolled in optional recovery.</summary>
    internal async Task ResumeAsync(CancellationToken ct)
    {
        var files = Files; var names = await files.ListNamesAsync("database", ct).ConfigureAwait(false);
        foreach (var staged in names.Where(name => name.StartsWith(Prefix, StringComparison.Ordinal) && name.EndsWith(".json.creating", StringComparison.Ordinal)))
        {
            var name = staged[..^".creating".Length];
            if (names.Contains(name, StringComparer.Ordinal)) throw new IOException("Competing safety retirement authorities require reconciliation.");
            var intent = await ReadIntentAsync(files, staged, ct).ConfigureAwait(false);
            // No cleanup precedes intent publication. Recheck its complete captured inventory before adoption.
            foreach (var artifact in intent.Copies.Concat(intent.Keys))
                if (await files.CaptureAsync(artifact.Root, artifact.Name, ct).ConfigureAwait(false) != artifact)
                    throw new IOException("Staged safety retirement inventory changed.");
            if (intent.Keys.Length != 0)
                await RequireNoOtherReferencesAsync(files, intent.Binding, intent.Original, intent.Copies, intent.Keys, ct).ConfigureAwait(false);
            await durability.SynchronizeFileAsync(Path.Combine(root, staged), ct).ConfigureAwait(false);
            validate(); File.Move(Path.Combine(root, staged), Path.Combine(root, name), overwrite: false);
            await durability.SynchronizeDirectoryAsync(ct).ConfigureAwait(false);
        }
        names = await files.ListNamesAsync("database", ct).ConfigureAwait(false);
        foreach (var name in names.Where(name => name.StartsWith(Prefix, StringComparison.Ordinal) && name.EndsWith(".json", StringComparison.Ordinal)))
        {
            var intent = await ReadIntentAsync(files, name, ct).ConfigureAwait(false);
            var done = name + ".done";
            if (names.Contains(done, StringComparer.Ordinal))
            {
                await durability.SynchronizeFileAsync(Path.Combine(root, done), ct).ConfigureAwait(false);
                await durability.SynchronizeDirectoryAsync(ct).ConfigureAwait(false);
                var expected = Convert.ToHexStringLower(SHA256.HashData(await files.ReadAsync("database", name, MaximumIntentBytes, false, ct).ConfigureAwait(false)));
                var marker = await files.ReadAsync("database", done, 64, false, ct).ConfigureAwait(false);
                if (System.Text.Encoding.ASCII.GetString(marker) != expected) throw new IOException("Safety retirement completion differs.");
                continue;
            }
            await ApplyAsync(files, name, intent, ct).ConfigureAwait(false);
        }
    }

    /// <summary>Authenticates uploaded and retained packages, keeps the newest bounded set, and records exact deletion inventories.</summary>
    internal async Task TrimAsync(DatabaseProfileIdentity profile, string original, string uploadedName,
        int keep, IBackupPackageReader reader, CancellationToken ct)
    {
        await ResumeAsync(ct).ConfigureAwait(false);
        var files = Files;
        await new AndroidGenerationFileDurability(roots["safety"]).SynchronizeFileAsync(Path.Combine(roots["safety"], uploadedName), ct).ConfigureAwait(false);
        var copies = new List<(ProfileRetirementArtifact Artifact, DatabaseKeyBinding Binding, DateTimeOffset Date)>();
        foreach (var name in await files.ListNamesAsync("safety", ct).ConfigureAwait(false))
        {
            if (!name.EndsWith(".vbs", StringComparison.Ordinal)) continue;
            var artifact = await files.CaptureAsync("safety", name, ct).ConfigureAwait(false);
            var envelope = await files.ReadAsync("safety", name, DeviceSafetyEncryption.MaximumPackageBytes + 80, false, ct).ConfigureAwait(false);
            byte[]? clear = null; byte[]? key = null;
            try
            {
                var binding = Binding(envelope);
                if (binding.ProfileId != profile.ProfileId || binding.InstallationId != profile.InstallationId) continue;
                key = await keys.ReadAsync(binding, ct).ConfigureAwait(false); clear = DeviceSafetyEncryption.Open(envelope, key);
                var date = DateTimeOffset.MinValue;
                await reader.ReadValidatedPortableAsync(clear, null, DeviceSafetyEncryption.MaximumPackageBytes,
                    (manifest, _, _) => { date = manifest.CreatedAt; return Task.CompletedTask; }, ct).ConfigureAwait(false);
                if (await files.CaptureAsync("safety", name, ct).ConfigureAwait(false) != artifact)
                    throw new IOException("Safety copy changed during authentication.");
                copies.Add((artifact, binding, date));
            }
            finally
            { CryptographicOperations.ZeroMemory(envelope); if (clear is not null) CryptographicOperations.ZeroMemory(clear); if (key is not null) CryptographicOperations.ZeroMemory(key); }
        }
        if (!copies.Any(copy => copy.Artifact.Name == uploadedName)) throw new IOException("Uploaded safety copy was not authenticated.");
        if (keep <= 0) return;
        var retained = copies.OrderByDescending(copy => copy.Artifact.Name == uploadedName).ThenByDescending(copy => copy.Date)
            .ThenBy(copy => copy.Artifact.Name, StringComparer.Ordinal).Take(keep).Select(copy => copy.Artifact.Name).ToHashSet(StringComparer.Ordinal);
        foreach (var group in copies.Where(copy => !retained.Contains(copy.Artifact.Name)).GroupBy(copy => copy.Binding))
        {
            var binding = group.Key; var removedCopies = group.Select(copy => copy.Artifact).ToArray();
            var removeKey = !copies.Any(copy => copy.Binding == binding && retained.Contains(copy.Artifact.Name));
            var keyFiles = new List<ProfileRetirementArtifact>(); var aliases = new List<Guid>();
            ProfileRecoveryRetirementCapture? capture = null;
            if (removeKey)
            {
                await RequireNoOtherReferencesAsync(files, binding, original, removedCopies, [], ct).ConfigureAwait(false);
                var baseName = binding.ProfileId.ToString("N") + "-" + binding.GenerationId.ToString("N") + ".key";
                var keyNames = (await files.ListNamesAsync("keys", ct).ConfigureAwait(false))
                    .Where(name => name == baseName || name.StartsWith(baseName + ".", StringComparison.Ordinal)).ToArray();
                var recoveredName = baseName + ".recovered"; var receiptName = recoveredName + ".receipt";
                if (keyNames.Any(name => name != baseName && name != recoveredName && name != receiptName))
                    continue; // An interrupted native recovery pins this binding and its copies until explicit completion.
                var ownedKey = await keys.ReadAsync(binding, ct).ConfigureAwait(false);
                try
                {
                    foreach (var name in keyNames)
                        keyFiles.Add(await files.CaptureAsync("keys", name, ct).ConfigureAwait(false));
                    if (keyNames.Contains(recoveredName, StringComparer.Ordinal))
                    {
                        var receipt = await files.ReadAsync("keys", receiptName, KeyRecoveryReceipt.MaximumBytes, false, ct).ConfigureAwait(false);
                        KeyRecoveryReceipt.Verify(receipt, binding, keyFiles.SingleOrDefault(file => file.Name == baseName),
                            keyFiles.Single(file => file.Name == recoveredName), ownedKey);
                    }
                    foreach (var name in keyNames.Where(name => name != receiptName))
                    {
                        var envelope = await files.ReadAsync("keys", name, 82, false, ct).ConfigureAwait(false);
                        try
                        {
                            if (envelope.Length != 82) continue; // Shared legacy installation aliases retain their other references.
                            try { aliases.Add(AndroidDatabaseKeyProtector.CaptureIndependentAlias(envelope, binding, name == recoveredName)); }
                            catch (CryptographicException) when (name == baseName && keyNames.Contains(recoveredName, StringComparer.Ordinal))
                            { /* The authenticated recovered receipt retains this unavailable old alias; never guess its ownership. */ }
                        }
                        finally { CryptographicOperations.ZeroMemory(envelope); }
                    }
                }
                finally { CryptographicOperations.ZeroMemory(ownedKey); }
                if (keyFiles.Count == 0 || keyNames.Contains(receiptName, StringComparer.Ordinal) && !keyNames.Contains(recoveredName, StringComparer.Ordinal))
                    throw new IOException("Safety key inventory differs.");
                if (recovery is not null)
                {
                    var key = await keys.ReadAsync(binding, ct).ConfigureAwait(false);
                    try { capture = await recovery.CaptureRetentionBindingAsync(binding, key, ct).ConfigureAwait(false); }
                    finally { CryptographicOperations.ZeroMemory(key); }
                }
            }
            var operation = Guid.NewGuid().ToString("N"); var nameOfIntent = Prefix + operation + ".json";
            var intent = new SafetyRetentionIntent(1, operation, binding, original, removedCopies, keyFiles.ToArray(), aliases.ToArray(), capture);
            await PublishAsync(nameOfIntent, JsonSerializer.SerializeToUtf8Bytes(intent, SafetyRetentionJson.Default.SafetyRetentionIntent), ct).ConfigureAwait(false);
            await ApplyAsync(files, nameOfIntent, intent, ct).ConfigureAwait(false);
        }
    }

    private async Task ApplyAsync(AndroidProfileRetirementFiles files, string name, SafetyRetentionIntent intent, CancellationToken ct)
    {
        async Task Authority(CancellationToken token)
        {
            validate(); await durability.SynchronizeFileAsync(Path.Combine(root, name), token).ConfigureAwait(false);
            await durability.SynchronizeDirectoryAsync(token).ConfigureAwait(false);
            var actual = await ReadIntentAsync(files, name, token).ConfigureAwait(false);
            if (!JsonSerializer.SerializeToUtf8Bytes(actual, SafetyRetentionJson.Default.SafetyRetentionIntent).AsSpan()
                .SequenceEqual(JsonSerializer.SerializeToUtf8Bytes(intent, SafetyRetentionJson.Default.SafetyRetentionIntent)))
                throw new IOException("Safety retention authority changed.");
            if (intent.Keys.Length != 0)
                await RequireNoOtherReferencesAsync(files, intent.Binding, intent.Original, intent.Copies, intent.Keys, token).ConfigureAwait(false);
        }
        await Authority(ct).ConfigureAwait(false);
        // Preflight all still-present objects before the first destructive action.
        foreach (var artifact in intent.Copies.Concat(intent.Keys))
        {
            if ((await files.ListNamesAsync(artifact.Root, ct).ConfigureAwait(false)).Contains(artifact.Name, StringComparer.Ordinal)
                && await files.CaptureAsync(artifact.Root, artifact.Name, ct).ConfigureAwait(false) != artifact)
                throw new IOException("Retained deletion object changed.");
        }
        if (intent.Recovery is not null)
        {
            if (recovery is null) throw new IOException("Retained recovery catalogue is unavailable.");
            await recovery.RemoveRetentionBindingAsync(intent.Binding, intent.Recovery, Authority, ct).ConfigureAwait(false);
        }
        foreach (var artifact in intent.Copies.Concat(intent.Keys))
            await files.RemoveRetainedArtifactAsync(artifact, Authority, ct).ConfigureAwait(false);
        foreach (var alias in intent.Aliases)
        {
            await Authority(ct).ConfigureAwait(false);
            await AndroidAliasReferences.RequireUnreferencedAsync(alias, validate, ct, root).ConfigureAwait(false);
            AndroidDatabaseKeyProtector.RemoveCapturedIndependentAlias(alias, validate);
        }
        foreach (var artifact in intent.Copies.Concat(intent.Keys))
            if ((await files.ListNamesAsync(artifact.Root, ct).ConfigureAwait(false)).Contains(artifact.Name, StringComparer.Ordinal))
                throw new IOException("Retired safety artifact remains.");
        var hash = Convert.ToHexStringLower(SHA256.HashData(await files.ReadAsync("database", name, MaximumIntentBytes, false, ct).ConfigureAwait(false)));
        await PublishAsync(name + ".done", System.Text.Encoding.ASCII.GetBytes(hash), ct).ConfigureAwait(false);
    }

    private async Task RequireNoOtherReferencesAsync(AndroidProfileRetirementFiles files, DatabaseKeyBinding binding, string originalPath,
        IReadOnlyList<ProfileRetirementArtifact> removingCopies, IReadOnlyList<ProfileRetirementArtifact> removingKeys, CancellationToken ct)
    {
        validate();
        var identities = await new SqliteDatabaseIdentityCatalogue(Path.Combine(root, "database-identities.sqlite"), validate).ReadAsync(ct).ConfigureAwait(false);
        var row = identities.SingleOrDefault(row => row.ProfileId == binding.ProfileId && row.InstallationId == binding.InstallationId)
            ?? throw new IOException("Safety retention profile identity is unavailable.");
        if (Path.GetDirectoryName(originalPath) != root) throw new IOException("Safety history belongs to another private root.");
        var history = await new SqliteProfileRestoreHistory(originalPath + ".restore-history.sqlite", row.InitialCandidateKey, validate)
            .ReadAsync(ct).ConfigureAwait(false);
        if (history.Any(reference => reference.CandidateGeneration == binding.GenerationId || reference.OriginalGeneration == binding.GenerationId))
            throw new IOException("Safety retention cannot retire a financial or pending generation.");
        if (binding == row.InitialCandidateKey) throw new IOException("Safety retention cannot retire the original financial key.");
        foreach (var scope in new[] { "safety", "portable" })
            foreach (var name in await files.ListNamesAsync(scope, ct).ConfigureAwait(false))
            {
                if (!name.Contains(".vbs", StringComparison.Ordinal) || removingCopies.Any(copy => copy.Root == scope && copy.Name == name)) continue;
                var header = await files.ReadAsync(scope, name, 80, true, ct).ConfigureAwait(false);
                try { if (Binding(header) == binding) throw new IOException("Safety key still has another retained or staged copy."); }
                finally { CryptographicOperations.ZeroMemory(header); }
            }
        var keyPrefix = binding.ProfileId.ToString("N") + "-" + binding.GenerationId.ToString("N") + ".key";
        foreach (var name in await files.ListNamesAsync("keys", ct).ConfigureAwait(false))
            if (name.StartsWith(keyPrefix, StringComparison.Ordinal) && removingKeys.Count != 0 && !removingKeys.Any(key => key.Name == name))
                throw new IOException("Uncaptured key/recovery receipt requires reconciliation.");
    }

    private async Task<SafetyRetentionIntent> ReadIntentAsync(AndroidProfileRetirementFiles files, string name, CancellationToken ct)
    {
        var bytes = await files.ReadAsync("database", name, MaximumIntentBytes, false, ct).ConfigureAwait(false);
        var intent = JsonSerializer.Deserialize(bytes, SafetyRetentionJson.Default.SafetyRetentionIntent)
            ?? throw new IOException("Safety retention intent is absent.");
        var recordedName = name.EndsWith(".creating", StringComparison.Ordinal) ? name[..^".creating".Length] : name;
        if (intent.Schema != 1 || recordedName != Prefix + intent.Operation + ".json" || !Guid.TryParseExact(intent.Operation, "N", out var operation)
            || operation == Guid.Empty || intent.Binding is null || intent.Copies is not { Length: > 0 and <= 4096 }
            || intent.Keys is null || intent.Aliases is null || intent.Aliases.Any(alias => alias == Guid.Empty)
            || Path.GetDirectoryName(intent.Original) != root || !intent.Original.EndsWith(".db", StringComparison.Ordinal)
            || intent.Copies.Any(copy => copy.Root != "safety" || !copy.Name.EndsWith(".vbs", StringComparison.Ordinal))
            || intent.Keys.Length > 3 || intent.Aliases.Length > 2
            || intent.Aliases.Distinct().Count() != intent.Aliases.Length
            || intent.Copies.Select(copy => copy.Name).Distinct(StringComparer.Ordinal).Count() != intent.Copies.Length
            || intent.Keys.Select(key => key.Name).Distinct(StringComparer.Ordinal).Count() != intent.Keys.Length
            || intent.Keys.Any(key => key.Root != "keys")) throw new IOException("Safety retention intent fields differ.");
        var keyName = intent.Binding.ProfileId.ToString("N") + "-" + intent.Binding.GenerationId.ToString("N") + ".key";
        if (intent.Keys.Any(key => key.Name != keyName && key.Name != keyName + ".recovered" && key.Name != keyName + ".recovered.receipt")
            || intent.Keys.Length == 0 && (intent.Aliases.Length != 0 || intent.Recovery is not null)
            || intent.Keys.Any(key => key.Name == keyName + ".recovered.receipt")
                && !intent.Keys.Any(key => key.Name == keyName + ".recovered"))
            throw new IOException("Safety retention key authority differs.");
        return intent;
    }

    private async Task PublishAsync(string name, byte[] bytes, CancellationToken ct)
    {
        var path = Path.Combine(root, name); var staged = path + ".creating";
        if (!File.Exists(staged))
        {
            await using var stream = new FileStream(staged, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            await stream.WriteAsync(bytes, ct).ConfigureAwait(false); stream.Flush(true);
        }
        var files = Files;
        var current = await files.ReadAsync("database", name + ".creating", MaximumIntentBytes, false, ct).ConfigureAwait(false);
        if (!current.AsSpan().SequenceEqual(bytes))
        {
            // Only completion may repair its own partial write after Apply requalified all removal effects.
            if (!name.EndsWith(".json.done", StringComparison.Ordinal) || current.Length >= bytes.Length
                || !bytes.AsSpan(0, current.Length).SequenceEqual(current))
                throw new IOException("Safety retention staged publication differs.");
            validate();
            await using (var output = new FileStream(staged, FileMode.Open, FileAccess.Write, FileShare.None))
            { await output.WriteAsync(bytes, ct).ConfigureAwait(false); output.SetLength(bytes.Length); output.Flush(true); }
            if (!(await files.ReadAsync("database", name + ".creating", MaximumIntentBytes, false, ct).ConfigureAwait(false)).AsSpan().SequenceEqual(bytes))
                throw new IOException("Safety completion repair read-back differs.");
        }
        await durability.SynchronizeFileAsync(staged, ct).ConfigureAwait(false); validate();
        File.Move(staged, path, overwrite: false); await durability.SynchronizeDirectoryAsync(ct).ConfigureAwait(false);
    }

    private static DatabaseKeyBinding Binding(ReadOnlySpan<byte> envelope)
    {
        if (envelope.Length < 80 || !DeviceSafetyEncryption.IsEncrypted(envelope)) throw new IOException("Safety reference header is unknown.");
        return new(new Guid(envelope.Slice(4, 16), bigEndian: true), new Guid(envelope.Slice(20, 16), bigEndian: true),
            new Guid(envelope.Slice(36, 16), bigEndian: true));
    }
}

/// <summary>Immutable authenticated safety/file/key/alias removal authority, containing no data keys or financial payload.</summary>
internal sealed record SafetyRetentionIntent(int Schema, string Operation, DatabaseKeyBinding Binding, string Original,
    ProfileRetirementArtifact[] Copies, ProfileRetirementArtifact[] Keys, Guid[] Aliases, ProfileRecoveryRetirementCapture? Recovery);

/// <summary>Serializes immutable safety-retirement references and native object captures.</summary>
[JsonSerializable(typeof(SafetyRetentionIntent))]
internal partial class SafetyRetentionJson : JsonSerializerContext { }
