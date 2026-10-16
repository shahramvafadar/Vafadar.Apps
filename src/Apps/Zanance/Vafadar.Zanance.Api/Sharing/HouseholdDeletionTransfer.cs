namespace Vafadar.Zanance.Api.Sharing;

/// <summary>Offline additive privacy-authority transfer before an old service snapshot may start; never copies keys, mail, objects or financial content.</summary>
public static class HouseholdDeletionTransfer
{
    /// <summary>Validates and immutably merges only current original deletion markers. Source workers must already be drained by the owning operator.</summary>
    public static async Task<int> MergeAsync(string currentPrivateRoot, string restoredPrivateRoot, CancellationToken ct = default)
    {
        if (!Path.IsPathFullyQualified(currentPrivateRoot) || !Path.IsPathFullyQualified(restoredPrivateRoot)) throw new IOException("Explicit private roots required.");
        var sourceRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(currentPrivateRoot));
        var targetRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(restoredPrivateRoot));
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (!Directory.Exists(sourceRoot) || !Directory.Exists(targetRoot) || string.Equals(sourceRoot, targetRoot, comparison) ||
            sourceRoot.StartsWith(targetRoot + Path.DirectorySeparatorChar, comparison) || targetRoot.StartsWith(sourceRoot + Path.DirectorySeparatorChar, comparison))
            throw new IOException("Distinct original and restored private roots required.");
        var source = new HouseholdDeletionLedger(new ServiceStorage { Root = sourceRoot, LocalMailbox = false });
        var target = new HouseholdDeletionLedger(new ServiceStorage { Root = targetRoot, LocalMailbox = false });
        var offset = 0;
        while (true)
        {
            var page = source.Inspect(offset, 100);
            // Offline restore still requires the complete latest privacy ledger.
            // Worker discovery may bypass unknown names, but transfer may not.
            if (page.Scanned != page.Spaces.Count) throw new IOException("Unknown original private deletion entry prevents offline transfer.");
            foreach (var id in page.Spaces)
            {
                ct.ThrowIfCancellationRequested();
                var original = await source.ReadAsync(id, ct) ?? throw new IOException("Original private deletion disappeared during offline transfer.");
                _ = await target.RecordAsync(original, ct);
                if (await target.ReadAsync(id, ct) != original) throw new IOException("Restored private deletion authority differs.");
            }
            offset = checked(offset + page.Scanned); if (page.Scanned < 100) return offset;
        }
    }
}
