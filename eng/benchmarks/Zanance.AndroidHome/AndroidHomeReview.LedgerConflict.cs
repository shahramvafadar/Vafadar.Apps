using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;
using Vafadar.Zanance.Performance;

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    private sealed record ConflictFixture(Guid Account, Guid Purchase, Guid Refund, DateOnly Day);

    /// <summary>Exercises the corrected original protected writer/refund Undo and derived reader in a new owned UID only.</summary>
    private static async Task<object> ReviewLedgerConflictAsync(IServiceProvider services, string root,
        string comparison, CancellationToken ct)
    {
        if (Package != "pro.vafadar.zanance.ledgerconflictreview" || AppInfo.Current.PackageName != Package
            || Platform.CurrentActivity?.Intent?.GetIntExtra("review_conflict_uid", -1) != Android.OS.Process.MyUid())
            throw new ReviewCheckException("Exact newly owned native ledger-conflict package and UID required.");
        var final = Path.Combine(root, "ledger-conflict-complete.json");
        if (File.Exists(final)) throw new ReviewCheckException("Recover the completed original native report; never repeat financial commands.");
        await AwaitOriginalMoneyWindowAsync(ct);
        var factory = services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        var store = services.GetRequiredService<ZananceStore>();
        var time = services.GetRequiredService<TimeProvider>();
        var day = DateOnly.FromDateTime(time.GetLocalNow().DateTime);
        var plan = Path.Combine(root, "ledger-conflict-fixture.json");
        ConflictFixture fixture;
        if (File.Exists(plan))
        {
            fixture = JsonSerializer.Deserialize<ConflictFixture>(await File.ReadAllTextAsync(plan, ct))
                ?? throw new ReviewCheckException("Original owned native fixture plan is unreadable.");
            if (fixture.Day != day) throw new ReviewCheckException("Keep the real original fixture day; never recreate it.");
        }
        else
        {
            await using var empty = await factory.CreateDbContextAsync(ct);
            var accounts = await empty.Accounts.AsNoTracking().ToListAsync(ct);
            if (await empty.Entries.AnyAsync(ct) || accounts.Count > 1
                || accounts.Any(a => a.OpeningBalance != 0 || a.CurrencyCode != "EUR" || a.IsArchived))
                throw new ReviewCheckException("A new empty independent source is required before financial setup.");
            fixture = new(accounts.FirstOrDefault()?.Id ?? Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), day);
            await WriteNewDurableCpuOracleAsync(root, plan, JsonSerializer.Serialize(fixture), ct);
        }
        var deleteRequest = Path.Combine(root, "original-delete-request.json");
        if (File.Exists(deleteRequest))
            throw new ReviewCheckException("The original deletion receipt belongs to its process; never replay or reconstruct it after interruption.");

        await using (var inspect = await factory.CreateDbContextAsync(ct))
        {
            var rows = await inspect.Entries.AsNoTracking().ToListAsync(ct);
            var accounts = await inspect.Accounts.AsNoTracking().ToListAsync(ct);
            if (rows.Any(e => e.Id != fixture.Purchase && e.Id != fixture.Refund)
                || accounts.Any(a => a.Id != fixture.Account))
                throw new ReviewCheckException("Never reuse another source or financial preparation.");
        }
        var account = new Account(fixture.Account) { Name = "D562 native conflict", CurrencyCode = "EUR", OpeningDate = day };
        var purchase = new LedgerEntry(fixture.Purchase) { AccountId = account.Id, Kind = EntryKind.Expense,
            Amount = 1000, Date = day, Title = "D562 purchase", Note = "Owned native writer", Tags = ["D562"] };
        var refund = new LedgerEntry(fixture.Refund) { AccountId = account.Id, Kind = EntryKind.Refund,
            Amount = 400, Date = day, RefundOfId = purchase.Id, Title = "D562 refund", Note = "Original refund link", Tags = ["D562", "refund"] };
        if (!(await store.GetAccountsAsync(cancellationToken: ct)).Any(a => a.Id == fixture.Account))
        {
            await WriteNewDurableCpuOracleAsync(root, Path.Combine(root, "account-request.json"), "{}", ct);
            if (!await store.SaveAccountAsync(account, ct)) throw new ReviewCheckException("Original native account setup was refused.");
        }
        foreach (var entry in new[] { purchase, refund })
        {
            await using var inspect = await factory.CreateDbContextAsync(ct);
            var present = await inspect.Entries.AsNoTracking().SingleOrDefaultAsync(e => e.Id == entry.Id, ct);
            if (present is not null)
            {
                if (present.Amount != entry.Amount || present.AccountId != entry.AccountId || present.Date != day
                    || present.Kind != entry.Kind || present.Title != entry.Title || present.RefundOfId != entry.RefundOfId)
                    throw new ReviewCheckException("Preserve the actual accepted native entry instead of overwriting it.");
                continue;
            }
            var request = Path.Combine(root, "save-" + entry.Id.ToString("N") + ".json");
            if (File.Exists(request)) throw new ReviewCheckException("An unobserved save request needs diagnosis, not financial replay.");
            await WriteNewDurableCpuOracleAsync(root, request, JsonSerializer.Serialize(new { entry.Id, entry.Amount, entry.AccountId, day }), ct);
            if (!(await store.SaveEntryAsync(entry, ct)).Succeeded)
                throw new ReviewCheckException("Original native entry setup was refused.");
        }
        var before = (await store.GetEntriesAsync(cancellationToken: ct)).OrderBy(e => e.Id).ToArray();
        if (before.Length != 2 || before.Single(e => e.Id == refund.Id).RefundOfId != purchase.Id)
            throw new ReviewCheckException("Exact two-row native purchase/refund source required.");
        await using (var inspect = await factory.CreateDbContextAsync(ct))
            if ((await inspect.Database.GetAppliedMigrationsAsync(ct)).Last() != "20261026104025_HomeLedgerDirtyBlockConflictGuards")
                throw new ReviewCheckException("The actual corrected additive migration is required.");

        await WriteNewDurableCpuOracleAsync(root, deleteRequest, JsonSerializer.Serialize(new { purchase.Id, Comparison = comparison }), ct);
        var deleted = await store.DeleteEntryAsync(purchase.Id, ct);
        var afterDelete = await store.GetEntriesAsync(cancellationToken: ct);
        if (deleted.Count != 1 || deleted[0].Id != purchase.Id || afterDelete.Count != 1
            || afterDelete[0].Id != refund.Id || afterDelete[0].RefundOfId is not null)
            throw new ReviewCheckException("Original native deletion did not retain exactly the unlinked refund.");
        await WriteNewDurableCpuOracleAsync(root, Path.Combine(root, "original-delete-accepted.json"), "{}", ct);
        await WriteNewDurableCpuOracleAsync(root, Path.Combine(root, "original-undo-request.json"), "{}", ct);
        await store.RestoreEntriesAsync(deleted, ct); // Keep the actual file-bound receipt; never reconstruct private refund links.
        await WriteNewDurableCpuOracleAsync(root, Path.Combine(root, "original-undo-accepted.json"), "{}", ct);
        var restored = (await store.GetEntriesAsync(cancellationToken: ct)).OrderBy(e => e.Id).ToArray();
        if (restored.Length != before.Length || restored.Single(e => e.Id == refund.Id).RefundOfId != purchase.Id)
            throw new ReviewCheckException("Original native refund ownership did not return with Undo.");
        var compared = restored.Select(e => e.Copy()).ToArray();
        for (var i = 0; i < compared.Length; i++)
        {
            if (compared[i].Id != before[i].Id || compared[i].UpdatedAt < before[i].UpdatedAt)
                throw new ReviewCheckException("Original native identity/audit time changed unexpectedly.");
            compared[i].UpdatedAt = before[i].UpdatedAt; // Only an original writer's advancing audit timestamp may differ.
        }
        if (JsonSerializer.Serialize(compared) != JsonSerializer.Serialize(before))
            throw new ReviewCheckException("An original financial or metadata field changed across native Undo.");
        using var owned = new IsolatedHomeReadFactory(factory, time, true);
        var reader = new ZananceStore(owned, time, services.GetRequiredService<ICommercialWriteAccessSource>());
        var home = await reader.GetHomeSnapshotAsync(day, 0, ct);
        if (owned.DerivedBlockReads == 0 || home.Entries.Count != 2
            || JsonSerializer.Serialize(home.Entries.OrderBy(e => e.Id)) != JsonSerializer.Serialize(restored)
            || home.Entries.Sum(e => e.EffectOn(account.Id)) != -600)
            throw new ReviewCheckException("Corrected original native complete-block reader or balance differs.");
        var outcome = new { Comparison = comparison, OriginalNativeStoreAndCipher = true,
            ActualAdditiveMigration = "20261026104025_HomeLedgerDirtyBlockConflictGuards",
            OwnedFixtureFinancialSetupRows = 2, PurchaseDeleteCommands = 1, OriginalReceiptUndoCommands = 1,
            RefundIdentityAndLinkPreserved = true, AllOriginalFieldsExceptMonotonicAuditExact = true,
            ActualDerivedBlockReads = owned.DerivedBlockReads, FinalRows = 2, OriginalEntryEffect = -600,
            CompleteFinalRowsSHA256 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(restored)))),
            NoShippingMainOrRetainedCallerSourceTouched = true, NoFormOrWholeDeviceAcceptance = true, Device = DeviceEvidence() };
        await WriteNewDurableCpuOracleAsync(root, final, JsonSerializer.Serialize(outcome), ct);
        return outcome;
    }
}
