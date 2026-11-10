using System.Security.Cryptography;
using Vafadar.Data;

namespace Vafadar.Zanance.App.Security;

/// <summary>Excludes current, archived and staged installation envelope references before native alias retirement.</summary>
internal static class AndroidAliasReferences
{
    /// <summary>Checks bounded direct native envelopes in every known retained root; unknown matching references block deletion.</summary>
    internal static async Task RequireUnreferencedAsync(Guid alias, Action validate, CancellationToken ct, string? additionalRoot = null,
        IReadOnlyList<ProfileRetirementArtifact>? removing = null, string? removingKeyRoot = null)
    {
        var baseRoot = FileSystem.AppDataDirectory;
        var installations = new[] { baseRoot }.Concat(Directory.EnumerateDirectories(baseRoot, "recovered-data-*")).Concat(
            additionalRoot is null ? [] : new[] { additionalRoot }).Distinct(StringComparer.Ordinal).ToArray();
        foreach (var installation in installations)
        {
            var keyRoot = Path.Combine(installation, "database-keys");
            FileAttributes attributes;
            // D-1009: Exists also hides denied metadata and broken links. Neither proves references absent before key retirement.
            try { attributes = File.GetAttributes(keyRoot); }
            catch (FileNotFoundException) { continue; }
            catch (DirectoryNotFoundException) { continue; }
            if ((attributes & FileAttributes.Directory) == 0 || (attributes & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Retained alias reference root is not an original directory.");
            var files = new AndroidProfileRetirementFiles(new Dictionary<string, string> { ["keys"] = keyRoot }, validate);
            foreach (var name in await files.ListNamesAsync("keys", ct).ConfigureAwait(false))
            {
                if (!name.EndsWith(".key", StringComparison.Ordinal) && !name.EndsWith(".key.recovered", StringComparison.Ordinal)
                    && !name.EndsWith(".key.creating", StringComparison.Ordinal) && !name.EndsWith(".key.recovered.creating", StringComparison.Ordinal)) continue;
                var excluded = keyRoot == removingKeyRoot ? removing?.SingleOrDefault(file => file.Root == "keys" && file.Name == name) : null;
                if (excluded is not null)
                {
                    if (await files.CaptureAsync("keys", name, ct).ConfigureAwait(false) != excluded)
                        throw new IOException("Excluded alias reference changed before profile cleanup.");
                    continue;
                }
                // D-1011: one sentinel byte distinguishes an oversized slot from an exact supported envelope.
                var header = await files.ReadAsync("keys", name, 83, true, ct).ConfigureAwait(false);
                try
                {
                    // Legacy version 1 uses the installation alias and has no independent alias field.
                    if (header.Length == 65 && header.AsSpan(0, 4).SequenceEqual("ZKEY"u8) && header[4] == 1) continue;
                    if (header.Length != 82 || !header.AsSpan(0, 4).SequenceEqual("ZKEY"u8)
                        || header[4] != 2 || header[5] > 1)
                        throw new IOException("Retained key reference format is unavailable; alias absence is unproven.");
                    var referenced = new Guid(header.AsSpan(6, 16), bigEndian: true);
                    if (referenced == Guid.Empty)
                        throw new IOException("Retained key reference has no independent wrapping identity.");
                    if (referenced == alias)
                        throw new IOException("Native wrapping alias still has a retained installation reference.");
                }
                finally { CryptographicOperations.ZeroMemory(header); }
            }
        }
    }

}
