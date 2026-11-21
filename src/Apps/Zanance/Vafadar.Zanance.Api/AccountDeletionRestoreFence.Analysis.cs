using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Api.Analysis;
using Vafadar.Zanance.Core.Analysis;

namespace Vafadar.Zanance.Api;

public static partial class AccountDeletionRestoreFence
{
    internal static Task RecordAnalysisAsync(ServiceStorage storage, Guid receipt, string owner, Guid request, CancellationToken ct) =>
        WriteAnalysisMarkerAsync(storage, new(receipt, OwnerHash(owner), request), ct);

    private sealed record AnalysisRetirement(Guid Receipt, string Owner, Guid Request);

    private sealed record AnalysisMarker(string Path, Guid Request)
    {
        internal async Task<AnalysisRetirement> ReadAsync(CancellationToken ct)
        {
            var file = new FileInfo(Path);
            if (!file.Exists || (file.Attributes & FileAttributes.ReparsePoint) != 0 || file.Length != 140)
                throw new IOException("Invalid analysis retirement marker.");
            var parts = (await File.ReadAllTextAsync(Path, ct)).Split('\n');
            if (parts.Length != 4 || parts[0] != "1" || !Guid.TryParseExact(parts[1], "D", out var request) || request != Request ||
                !Guid.TryParseExact(parts[2], "D", out var receipt) || receipt == Guid.Empty ||
                parts[3].Length != 64 || !parts[3].All(character => character is >= '0' and <= '9' or >= 'a' and <= 'f'))
                throw new IOException("Invalid analysis retirement marker.");
            return new(receipt, parts[3], request);
        }
    }

    private static IEnumerable<AnalysisMarker> AnalysisMarkers(string storage)
    {
        var root = RootOf(storage);
        if (!Directory.Exists(root)) yield break;
        foreach (var path in Directory.EnumerateFileSystemEntries(root, "*.analysis-retired"))
        {
            if (!Guid.TryParseExact(Path.GetFileNameWithoutExtension(path), "N", out var id) || id == Guid.Empty)
                throw new IOException("Invalid analysis retirement marker filename.");
            yield return new(path, id);
        }
    }

    private static async Task WriteAnalysisMarkerAsync(ServiceStorage storage, AnalysisRetirement marker, CancellationToken ct)
    {
        if (marker.Receipt == Guid.Empty || marker.Request == Guid.Empty)
            throw new IOException("Original analysis retirement identities are required.");
        var root = RootOf(storage.Root);
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, marker.Request.ToString("N") + ".analysis-retired");
        if (File.Exists(path))
        {
            if (await new AnalysisMarker(path, marker.Request).ReadAsync(ct) != marker)
                throw new IOException("The analysis retirement marker differs.");
            return;
        }
        await PublishMarkerAsync(storage.Root, path, $"1\n{marker.Request:D}\n{marker.Receipt:D}\n{marker.Owner}", ct);
    }

    private static async Task<bool> AnalysisContentRetiredAsync(ServiceDbContext db, AnalysisRetirement marker, CancellationToken ct)
    {
        var row = await db.Set<AiRequestRow>().AsNoTracking().Where(row => row.Id == marker.Request)
            .Select(row => new { row.UserId, row.State, Retired = row.ProtectedPayload == "" && row.ProtectedText == "" })
            .SingleOrDefaultAsync(ct);
        if (row is not null)
            return row.State == AiRequestState.Completed && row.Retired && OwnerHash(row.UserId) == marker.Owner &&
                await db.AccountDeletionRequests.AnyAsync(request => request.UserId == row.UserId && request.ReceiptId == marker.Receipt, ct);
        var owners = await FindRestoredOwnersAsync(db, marker.Owner, ct);
        // Older snapshots can lack the original request but still accept another submission from its owner.
        return owners.Count == 0 || owners.Count == 1 &&
            await db.AccountDeletionRequests.AnyAsync(request => request.UserId == owners[0] && request.ReceiptId == marker.Receipt, ct);
    }

    private static async Task<int> ReconcileAnalysisAsync(ServiceDbContext db, ServiceStorage storage, TimeProvider time, CancellationToken ct)
    {
        var outstanding = 0;
        foreach (var path in AnalysisMarkers(storage.Root).ToArray())
        {
            var marker = await path.ReadAsync(ct);
            if (await AnalysisContentRetiredAsync(db, marker, ct)) continue;
            // A restore predating the provider receipt must stay offline; the marker does not invent that receipt.
            var row = await db.Set<AiRequestRow>().Where(row => row.Id == marker.Request)
                .Select(row => new { row.UserId, row.State }).SingleOrDefaultAsync(ct);
            if (row is null)
                await BindRestoredRequestAsync(db, marker.Receipt, marker.Owner, time, ct);
            else if (row.State == AiRequestState.Completed && OwnerHash(row.UserId) == marker.Owner &&
                await db.Set<AiPilotDeletionRow>().AnyAsync(effect => effect.RequestId == marker.Request && effect.ConfirmedAt >= effect.StartedAt, ct) &&
                await BindRestoredRequestAsync(db, marker.Receipt, marker.Owner, time, ct))
                await AccountAnalysisRetirement.RunAsync(db, storage, marker.Receipt, time, ct, releaseUnclaimedReservations: false);
            if (!await AnalysisContentRetiredAsync(db, marker, ct)) outstanding++;
        }
        return outstanding;
    }
}
