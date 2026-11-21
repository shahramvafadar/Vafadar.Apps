using System.Globalization;
using Microsoft.EntityFrameworkCore;

namespace Vafadar.Zanance.Api;

public static partial class AccountDeletionRestoreFence
{
    private sealed record PendingRequest(Guid Receipt, string Owner, DateTimeOffset RequestedAt);

    private sealed record PendingMarker(string Path, Guid Receipt)
    {
        internal async Task<PendingRequest> ReadAsync(CancellationToken ct)
        {
            var file = new FileInfo(Path);
            if (!file.Exists || (file.Attributes & FileAttributes.ReparsePoint) != 0 || file.Length is < 105 or > 123)
                throw new IOException("Invalid pending account deletion marker.");
            var fields = (await File.ReadAllTextAsync(Path, ct)).Split('\n');
            if (fields.Length != 4 || fields[0] != "1" || fields[1] != Receipt.ToString("D") ||
                fields[2].Length != 64 || !fields[2].All(character => character is >= '0' and <= '9' or >= 'a' and <= 'f') ||
                !long.TryParse(fields[3], NumberStyles.None, CultureInfo.InvariantCulture, out var ticks) ||
                ticks <= 0 || ticks > DateTimeOffset.MaxValue.UtcTicks)
                throw new IOException("Invalid pending account deletion marker.");
            return new(Receipt, fields[2], new DateTimeOffset(ticks, TimeSpan.Zero));
        }
    }

    // Do not acknowledge a request until its database transaction committed and the
    // independent restore marker exists. On a lost acknowledgement the same row can retry.
    internal static async Task<bool> RecordAcknowledgementAsync(ServiceStorage storage, AccountDeletionReceipt receipt,
        string owner, CancellationToken ct)
    {
        try
        {
            await WritePendingMarkerAsync(storage, new(receipt.ReceiptId, OwnerHash(owner), receipt.RequestedAt), ct);
            return true;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static IEnumerable<PendingMarker> PendingMarkers(string storage)
    {
        var root = RootOf(storage);
        if (!Directory.Exists(root)) yield break;
        foreach (var path in Directory.EnumerateFileSystemEntries(root, "*.request"))
        {
            if (!Guid.TryParseExact(Path.GetFileNameWithoutExtension(path), "N", out var id) || id == Guid.Empty)
                throw new IOException("Invalid pending account deletion marker filename.");
            yield return new(path, id);
        }
    }

    private static async Task WritePendingMarkerAsync(ServiceStorage storage, PendingRequest marker, CancellationToken ct)
    {
        if (marker.Receipt == Guid.Empty) throw new IOException("The original account deletion receipt is required.");
        var root = RootOf(storage.Root);
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, marker.Receipt.ToString("N") + ".request");
        if (!File.Exists(path))
        {
            var ticks = marker.RequestedAt.UtcTicks.ToString(CultureInfo.InvariantCulture);
            try { await PublishMarkerAsync(storage.Root, path, $"1\n{marker.Receipt:D}\n{marker.Owner}\n{ticks}", ct); }
            catch (IOException) when (File.Exists(path)) { /* A concurrent acknowledgement may have published the same marker. */ }
        }
        if (await new PendingMarker(path, marker.Receipt).ReadAsync(ct) != marker)
            throw new IOException("The pending account deletion marker differs.");
    }

    private static async Task<bool> PendingRequestIsReconciledAsync(ServiceDbContext db, PendingRequest marker, CancellationToken ct)
    {
        var row = await db.AccountDeletionRequests.AsNoTracking().Where(row => row.ReceiptId == marker.Receipt)
            .Select(row => new { row.UserId, row.RequestedAt }).SingleOrDefaultAsync(ct);
        if (row is not null)
            return OwnerHash(row.UserId) == marker.Owner && row.RequestedAt == marker.RequestedAt;
        return await db.Set<AccountDeletionCompletion>().AnyAsync(row => row.ReceiptId == marker.Receipt, ct);
    }
}
