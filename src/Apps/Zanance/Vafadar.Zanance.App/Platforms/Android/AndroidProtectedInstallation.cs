using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Zanance.App.Profiles;
using Vafadar.Zanance.App.Startup;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Security;

/// <summary>Records explicitly verified fresh/legacy authority before the covered host prepares any key.</summary>
/// <remarks>A missing identity alone never means a new installation. Existing recorded authority cannot
/// be replaced after financial/key artifacts appear, and interrupted publication retains its original record.</remarks>
internal sealed partial class AndroidProtectedInstallation(IServiceProvider services)
{
    private const string RecordName = "protected-installation.json";
    private const int MaximumRecordBytes = 8192;

    /// <summary>Reads recorded authority or preflights the complete original catalogue before first publication.</summary>
    internal async Task<ProtectedStorageInitialization> BeginAsync(CancellationToken ct)
    {
        var location = services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
        using var owner = await location.Access.AcquireExclusiveAsync(ct).ConfigureAwait(false);
        var capture = services.GetRequiredService<ProfileService>().CaptureStorage();
        var root = Path.GetDirectoryName(location.Path)!;
        void Check() { ct.ThrowIfCancellationRequested(); location.Access.RequireDrainedOwner(owner); capture.Validate(); }
        var files = new AndroidProfileRetirementFiles(new Dictionary<string, string> { ["database"] = root }, Check);
        var durability = new AndroidGenerationFileDurability(root);
        var path = Path.Combine(root, RecordName);
        await ResumeLegacyNameAsync(files, durability, root, Check, ct).ConfigureAwait(false);
        var names = await files.ListNamesAsync("database", ct).ConfigureAwait(false);
        var expectedHash = CatalogueHash(capture);
        if (names.Contains(RecordName, StringComparer.Ordinal))
        {
            var record = await ReadAsync(files, RecordName, ct).ConfigureAwait(false);
            if (record.Complete) return ProtectedStorageInitialization.ExistingInstallation;
            if (record.CatalogueHash != expectedHash) throw new IOException("Pending installation catalogue changed.");
            await durability.SynchronizeFileAsync(path, ct).ConfigureAwait(false);
            return record.Mode;
        }
        var stagedName = RecordName + ".creating";
        if (names.Contains(stagedName, StringComparer.Ordinal))
        {
            var saved = await ReadAsync(files, stagedName, ct).ConfigureAwait(false);
            if (saved.Complete || saved.CatalogueHash != expectedHash) throw new IOException("Staged installation authority differs.");
            await durability.SynchronizeFileAsync(path + ".creating", ct).ConfigureAwait(false);
            Check(); File.Move(path + ".creating", path, overwrite: false);
            await durability.SynchronizeDirectoryAsync(ct).ConfigureAwait(false);
            return (await ReadAsync(files, RecordName, ct).ConfigureAwait(false)).Mode;
        }
        // No published authority may be reconstructed from a missing record after native work started.
        if (names.Any(name => name.Contains("cipher", StringComparison.Ordinal) || name.EndsWith(".key", StringComparison.Ordinal)
            || name.EndsWith(".sqlite", StringComparison.Ordinal) || name.Contains("recovery", StringComparison.Ordinal))
            || Directory.Exists(Path.Combine(root, "database-keys")) && Directory.EnumerateFileSystemEntries(Path.Combine(root, "database-keys")).Any())
            throw new IOException("Existing protected state requires its recorded installation authority.");
        var originals = capture.OriginalPaths.Values.Select(Path.GetFileName).ToHashSet(StringComparer.Ordinal);
        var present = originals.Count(name => names.Contains(name!, StringComparer.Ordinal));
        ProtectedStorageInitialization mode;
        if (present == originals.Count)
        {
            foreach (var name in originals)
            {
                var header = await files.ReadAsync("database", name!, 16, true, ct).ConfigureAwait(false);
                try { if (!header.AsSpan().SequenceEqual("SQLite format 3\0"u8)) throw new IOException("Original is not known plaintext SQLite."); }
                finally { CryptographicOperations.ZeroMemory(header); }
            }
            mode = ProtectedStorageInitialization.LegacyPlaintextProfiles;
        }
        else if (present == 0 && originals.Count == 1 && capture.OriginalPaths.ContainsKey("")
            && !names.Any(name => name.Contains(".db", StringComparison.Ordinal) || name.Contains(".sqlite", StringComparison.Ordinal)))
        {
            // This is a complete fresh-root/canonical-main attestation, not an absent identity fallback.
            mode = ProtectedStorageInitialization.NewInstallation;
        }
        else throw new IOException("Missing or foreign original storage requires recovery, not empty replacement.");
        var initial = new AndroidInstallationRecord(1, mode, false, expectedHash);
        var encoded = JsonSerializer.SerializeToUtf8Bytes(initial, AndroidInstallationJson.Default.AndroidInstallationRecord);
        Check();
        await using (var output = new FileStream(path + ".creating", FileMode.CreateNew, FileAccess.Write, FileShare.None))
        { await output.WriteAsync(encoded, ct).ConfigureAwait(false); output.Flush(true); }
        await durability.SynchronizeFileAsync(path + ".creating", ct).ConfigureAwait(false);
        if (await ReadAsync(files, stagedName, ct).ConfigureAwait(false) != initial) throw new IOException("Installation authority read-back differs.");
        Check(); File.Move(path + ".creating", path, overwrite: false);
        await durability.SynchronizeDirectoryAsync(ct).ConfigureAwait(false);
        return (await ReadAsync(files, RecordName, ct).ConfigureAwait(false)).Mode;
    }

    /// <summary>Marks only successfully prepared protected storage complete, retaining its same original authority.</summary>
    internal async Task CompleteAsync(CancellationToken ct)
    {
        var location = services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
        using var owner = await location.Access.AcquireExclusiveAsync(ct).ConfigureAwait(false);
        if (!location.IsProtected) throw new InvalidOperationException("Installation completion requires protected publication.");
        var root = Path.GetDirectoryName(location.Path)!;
        void Check() { ct.ThrowIfCancellationRequested(); location.Access.RequireDrainedOwner(owner); }
        var files = new AndroidProfileRetirementFiles(new Dictionary<string, string> { ["database"] = root }, Check);
        var durability = new AndroidGenerationFileDurability(root);
        var current = await ReadAsync(files, RecordName, ct).ConfigureAwait(false);
        var path = Path.Combine(root, RecordName);
        if (current.Complete) { await durability.SynchronizeFileAsync(path, ct).ConfigureAwait(false); return; }
        var captured = await files.CaptureAsync("database", RecordName, ct).ConfigureAwait(false);
        var stageName = RecordName + ".complete-" + Guid.NewGuid().ToString("N");
        var bytes = JsonSerializer.SerializeToUtf8Bytes(current with { Complete = true }, AndroidInstallationJson.Default.AndroidInstallationRecord);
        await using (var output = new FileStream(Path.Combine(root, stageName), FileMode.CreateNew, FileAccess.Write, FileShare.None))
        { await output.WriteAsync(bytes, ct).ConfigureAwait(false); output.Flush(true); }
        await durability.SynchronizeFileAsync(Path.Combine(root, stageName), ct).ConfigureAwait(false);
        if (await ReadAsync(files, stageName, ct).ConfigureAwait(false) != current with { Complete = true }
            || await files.CaptureAsync("database", RecordName, ct).ConfigureAwait(false) != captured)
            throw new IOException("Installation completion changed its captured authority.");
        Check(); File.Move(Path.Combine(root, stageName), path, overwrite: true);
        await durability.SynchronizeDirectoryAsync(ct).ConfigureAwait(false);
        if (!(await ReadAsync(files, RecordName, ct).ConfigureAwait(false)).Complete) throw new IOException("Installation completion was not published.");
    }

    private static async Task<AndroidInstallationRecord> ReadAsync(AndroidProfileRetirementFiles files, string name, CancellationToken ct)
    {
        var bytes = await files.ReadAsync("database", name, MaximumRecordBytes, false, ct).ConfigureAwait(false);
        var value = JsonSerializer.Deserialize(bytes, AndroidInstallationJson.Default.AndroidInstallationRecord)
            ?? throw new IOException("Installation authority is absent.");
        if (value.Schema != 1 || value.Mode is not (ProtectedStorageInitialization.NewInstallation or ProtectedStorageInitialization.LegacyPlaintextProfiles)
            || value.CatalogueHash is null || value.CatalogueHash.Length != 64 || value.CatalogueHash.Any(character => !char.IsAsciiHexDigitLower(character)))
            throw new IOException("Installation authority is invalid.");
        return value;
    }

    private static string CatalogueHash(ProfileStorageCapture capture) => Convert.ToHexStringLower(SHA256.HashData(
        Encoding.UTF8.GetBytes(string.Join("\n", capture.OriginalPaths.OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => pair.Key + "|" + pair.Value)))));
}

/// <summary>Non-secret device-only initialization authority, never a portable preference or financial row.</summary>
internal sealed record AndroidInstallationRecord(int Schema, ProtectedStorageInitialization Mode, bool Complete, string CatalogueHash);

/// <summary>Serializes non-secret protected installation authority in trimmed Android builds.</summary>
[JsonSerializable(typeof(AndroidInstallationRecord))]
internal partial class AndroidInstallationJson : JsonSerializerContext { }
