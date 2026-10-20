#if (DEBUG && WINDOWS) || ZANANCE_INDEPENDENT_HOME_REVIEW
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

/// <summary>Original actual detail-page action states over one separately owned fictitious application store.</summary>
internal static class HoldingFeeActionNativeReview
{
    /// <summary>Expected existing domain state, without changing any application eligibility algorithm.</summary>
    internal sealed record Case(string Name, Guid EntryId, bool ManagedHolding, bool RefundAvailable);

    /// <summary>Continues the exact already seeded independent rows without repeating a financial writer.</summary>
    internal static async Task<IReadOnlyList<Case>> ReadPreparedAsync(IServiceProvider services, CancellationToken ct)
    {
        if (AppInfo.Current.PackageName != "pro.vafadar.zanance.androidhomereview") throw new IOException("Independent retained action identity required.");
        var store = services.GetRequiredService<ZananceStore>();
        var rows = await store.GetEntriesAsync(cancellationToken: ct);
        var names = new[] { "D411 original refundable fee", "D411 original action holding", "D411 original ordinary expense", "D411 original income" };
        if (rows.Count != 4 || names.Any(name => rows.Count(row => row.Title == name) != 1)) throw new IOException("Exact original action rows required.");
        return [new("holding-fee", rows.Single(row => row.Title == names[0]).Id, true, true),
            new("holding-principal", rows.Single(row => row.Title == names[1]).Id, true, false),
            new("ordinary-expense", rows.Single(row => row.Title == names[2]).Id, false, true),
            new("ordinary-income", rows.Single(row => row.Title == names[3]).Id, false, false)];
    }

    /// <summary>Seeds only the independent application identity before read-only native action checks.</summary>
    internal static async Task<IReadOnlyList<Case>> PrepareAsync(IServiceProvider services, CancellationToken ct)
    {
        var package = AppInfo.Current.PackageName;
        if (!package.StartsWith("pro.vafadar.zanance.storepreview.", StringComparison.Ordinal)
            && package != "pro.vafadar.zanance.androidhomereview") throw new IOException("Independent detail action identity required.");
        var store = services.GetRequiredService<ZananceStore>(); var holdings = services.GetRequiredService<HoldingStore>();
        await store.EnsureDefaultCategoriesAsync(ct); var settings = await store.GetSettingsAsync(ct);
        settings.Mode = ExperienceMode.Advanced; await store.SaveSettingsAsync(settings, ct);
        var day = DateOnly.FromDateTime(services.GetRequiredService<TimeProvider>().GetLocalNow().DateTime);
        var cash = new Account { Name = "D411 original action cash", CurrencyCode = "EUR", OpeningDate = day, OpeningBalance = 100000 };
        await store.SaveAccountAsync(cash, ct);
        var type = new AssetType { Name = "D411 original action holding", Kind = AssetKind.CountableItem,
            Dimension = AssetDimension.Count, PriceCurrencyCode = "EUR" };
        await holdings.SaveTypeAsync(type, ct); var place = await holdings.EnsureDefaultLocationAsync("D411 original place", ct);
        var item = new AssetEvent { AssetTypeId = type.Id, LocationId = place.Id, Kind = AssetEventKind.Purchase,
            Date = day, Quantity = 1000, BasisAmount = 2500, Note = "D411 original action purchase" };
        var principal = new LedgerEntry { Kind = EntryKind.AssetPurchase, AccountId = cash.Id, Date = day, Amount = 2500, Title = type.Name };
        var fee = new LedgerEntry { Kind = EntryKind.Expense, AccountId = cash.Id, Date = day, Amount = 200,
            CategoryId = await holdings.FeesCategoryAsync(ct), Title = "D411 original refundable fee" };
        if (!(await holdings.SaveEventAsync(item, [principal, fee], true, ct)).Succeeded) throw new IOException("Independent original action purchase failed.");
        var ordinary = new LedgerEntry { Kind = EntryKind.Expense, AccountId = cash.Id, Date = day, Amount = 300, Title = "D411 original ordinary expense" };
        var income = new LedgerEntry { Kind = EntryKind.Income, AccountId = cash.Id, Date = day, Amount = 500, Title = "D411 original income" };
        if (!(await store.SaveEntriesAsync([ordinary, income], [], ordinary.Id, ct)).Succeeded) throw new IOException("Independent original action rows failed.");
        return [new("holding-fee", fee.Id, true, true), new("holding-principal", principal.Id, true, false),
            new("ordinary-expense", ordinary.Id, false, true), new("ordinary-income", income.Id, false, false)];
    }
}
#endif
