using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Vafadar.Data;
using Vafadar.Zanance.Core;

namespace Vafadar.Zanance.App.Security;

internal sealed partial class AndroidProtectedInstallation
{
    /// <summary>Continues the recorded original SQLite family rename before any cipher identity or key is created.</summary>
    private static async Task ResumeLegacyNameAsync(AndroidProfileRetirementFiles files,
        AndroidGenerationFileDurability durability, string root, Action validate, CancellationToken ct)
    {
        const string recordName = "legacy-database-name.json";
        var names = await files.ListNamesAsync("database", ct).ConfigureAwait(false);
        var legacy = ZananceApp.LegacyDatabaseFileName; var current = ZananceApp.DatabaseFileName;
        if (!names.Contains(recordName, StringComparer.Ordinal) && !names.Contains(recordName + ".creating", StringComparer.Ordinal))
        {
            if (!names.Contains(legacy, StringComparer.Ordinal)) return;
            var suffixes = new[] { "-wal", "-shm", "-journal", string.Empty };
            if (suffixes.Any(suffix => names.Contains(current + suffix, StringComparer.Ordinal))
                || names.Any(name => name.Contains("cipher", StringComparison.Ordinal) || name.EndsWith(".sqlite", StringComparison.Ordinal)
                    || name.StartsWith(RecordName, StringComparison.Ordinal))
                || Directory.Exists(Path.Combine(root, "database-keys"))
                    && Directory.EnumerateFileSystemEntries(Path.Combine(root, "database-keys")).Any())
                throw new IOException("Legacy rename conflicts with current or protected storage.");
            var header = await files.ReadAsync("database", legacy, 16, true, ct).ConfigureAwait(false);
            try { if (!header.AsSpan().SequenceEqual("SQLite format 3\0"u8)) throw new IOException("Legacy original is not plaintext SQLite."); }
            finally { CryptographicOperations.ZeroMemory(header); }
            var captured = new List<ProfileRetirementArtifact>();
            foreach (var suffix in suffixes)
                if (names.Contains(legacy + suffix, StringComparer.Ordinal))
                    captured.Add(await files.CaptureAsync("database", legacy + suffix, ct).ConfigureAwait(false));
            var bytes = JsonSerializer.SerializeToUtf8Bytes(new LegacyNameIntent(1, captured.ToArray()), LegacyNameJson.Default.LegacyNameIntent);
            validate();
            await using (var output = new FileStream(Path.Combine(root, recordName + ".creating"), FileMode.CreateNew, FileAccess.Write, FileShare.None))
            { await output.WriteAsync(bytes, ct).ConfigureAwait(false); output.Flush(true); }
        }
        var path = Path.Combine(root, recordName);
        if (!File.Exists(path))
        {
            await durability.SynchronizeFileAsync(path + ".creating", ct).ConfigureAwait(false);
            validate(); File.Move(path + ".creating", path, overwrite: false);
            await durability.SynchronizeDirectoryAsync(ct).ConfigureAwait(false);
        }
        var record = await files.ReadAsync("database", recordName, 16384, false, ct).ConfigureAwait(false);
        var intent = JsonSerializer.Deserialize(record, LegacyNameJson.Default.LegacyNameIntent)
            ?? throw new IOException("Legacy rename authority is absent.");
        var allowed = new[] { legacy + "-wal", legacy + "-shm", legacy + "-journal", legacy };
        if (intent.Schema != 1 || intent.Files is not { Length: > 0 and <= 4 }
            || intent.Files[^1].Name != legacy || intent.Files.Any(file => file.Root != "database" || !allowed.Contains(file.Name, StringComparer.Ordinal))
            || intent.Files.Select(file => file.Name).Distinct(StringComparer.Ordinal).Count() != intent.Files.Length)
            throw new IOException("Legacy rename authority differs.");
        var done = path + ".done";
        var digest = System.Text.Encoding.ASCII.GetBytes(Convert.ToHexStringLower(SHA256.HashData(record)));
        if (File.Exists(done))
        {
            if (names.Contains(legacy, StringComparer.Ordinal)) throw new IOException("Completed legacy rename has a competing original.");
            var stored = await files.ReadAsync("database", recordName + ".done", 64, false, ct).ConfigureAwait(false);
            if (!stored.AsSpan().SequenceEqual(digest)) throw new IOException("Legacy rename completion differs.");
            await durability.SynchronizeFileAsync(done, ct).ConfigureAwait(false); return;
        }
        await durability.SynchronizeFileAsync(path, ct).ConfigureAwait(false);
        // Moving sidecars first leaves the main legacy filename available until its whole family is recorded.
        foreach (var source in intent.Files)
        {
            var target = current + source.Name[legacy.Length..];
            names = await files.ListNamesAsync("database", ct).ConfigureAwait(false);
            var hasSource = names.Contains(source.Name, StringComparer.Ordinal);
            if (hasSource && names.Contains(target, StringComparer.Ordinal)) throw new IOException("Legacy rename has two competing files.");
            var actual = await files.CaptureAsync("database", hasSource ? source.Name : target, ct).ConfigureAwait(false);
            if (actual.NativeIdentity != source.NativeIdentity || actual.RootIdentity != source.RootIdentity
                || actual.Length != source.Length || actual.Sha256 != source.Sha256)
                throw new IOException("Legacy rename object changed.");
            validate();
            if (hasSource) File.Move(Path.Combine(root, source.Name), Path.Combine(root, target), overwrite: false);
            await durability.SynchronizeFileAsync(Path.Combine(root, target), ct).ConfigureAwait(false);
        }
        // Complete only after rechecking the exact renamed objects. Partial staging is never authority.
        var stagedDone = done + ".creating";
        if (File.Exists(stagedDone))
        {
            var partial = await files.ReadAsync("database", recordName + ".done.creating", 64, false, ct).ConfigureAwait(false);
            if (!digest.AsSpan(0, partial.Length).SequenceEqual(partial))
                throw new IOException("Staged legacy completion differs.");
        }
        validate();
        await using (var output = new FileStream(stagedDone, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
        { await output.WriteAsync(digest, ct).ConfigureAwait(false); output.SetLength(digest.Length); output.Flush(true); }
        await durability.SynchronizeFileAsync(stagedDone, ct).ConfigureAwait(false);
        var completed = await files.ReadAsync("database", recordName + ".done.creating", 64, false, ct).ConfigureAwait(false);
        if (!completed.AsSpan().SequenceEqual(digest)) throw new IOException("Staged legacy completion read-back differs.");
        validate(); File.Move(stagedDone, done, overwrite: false);
        await durability.SynchronizeDirectoryAsync(ct).ConfigureAwait(false);
    }
}

/// <summary>Immutable original file identities used to resume the pre-encryption brand rename.</summary>
internal sealed record LegacyNameIntent(int Schema, ProfileRetirementArtifact[] Files);

/// <summary>Serializes recorded legacy naming authority without reflection in trimmed Android builds.</summary>
[JsonSerializable(typeof(LegacyNameIntent))]
internal partial class LegacyNameJson : JsonSerializerContext { }
