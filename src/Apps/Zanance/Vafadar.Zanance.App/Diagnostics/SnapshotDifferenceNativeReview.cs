#if (DEBUG && WINDOWS) || ZANANCE_INDEPENDENT_HOME_REVIEW
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Forecast;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Forecasts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Diagnostics;

/// <summary>Reads original saved comparisons against deliberately historical fictional rows, without changing the OS clock.</summary>
internal static class SnapshotDifferenceNativeReview
{
    /// <summary>Changes only the independent fixture's audit timestamps during one historical setup.</summary>
    private sealed class SourceClock(TimeProvider original) : TimeProvider
    {
        internal DateTimeOffset Now = original.GetUtcNow().AddDays(-2);
        public override DateTimeOffset GetUtcNow() => Now;
        public override TimeZoneInfo LocalTimeZone => original.LocalTimeZone;
    }

    /// <summary>Renders one original immutable snapshot in each requested independent language/theme/width context.</summary>
    internal static async Task<object> RunAsync(IServiceProvider services, string root, IReadOnlyList<int> widths,
        Func<string, ThemeChoice, int, Task> context, Func<SnapshotPage, string, Task> observe, CancellationToken ct)
    {
        var package=AppInfo.Current.PackageName;
        if (package!="pro.vafadar.zanance.androidhomereview" && !package.StartsWith("pro.vafadar.zanance.storepreview.",StringComparison.Ordinal))
            throw new IOException("Independent original snapshot identity required.");
        var original=await ImportFinancialNativeReview.StoredAsync(services,ct);
        var realTime=services.GetRequiredService<TimeProvider>(); var clock=new SourceClock(realTime);
        var path=Path.Combine(root,"original-snapshot-difference.db"); var exists=File.Exists(path);
        await using var fixture=new ServiceCollection().AddSingleton<TimeProvider>(clock).AddZananceData(path).BuildServiceProvider();
        await Task.Run(()=>fixture.MigrateLocalDatabase<ZananceDbContext>(),ct);
        var store=fixture.GetRequiredService<ZananceStore>(); var factory=fixture.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        if (!exists)
        {
            var day=DateOnly.FromDateTime(clock.GetLocalNow().DateTime);
            var account=new Account { Name="Original comparison scope",CurrencyCode="EUR",OpeningDate=day.AddDays(-1),OpeningBalance=10000 };
            var outside=new Account { Name="Outside the saved scope",CurrencyCode="EUR",OpeningDate=day.AddDays(-1),OpeningBalance=0,IncludeInTotals=false };
            await store.SaveAccountAsync(account,ct); await store.SaveAccountAsync(outside,ct);
            var forecast=ForecastCalculator.Compute([account,outside],[],[],[],day,day.AddDays(2)).Single();
            var snapshot=ForecastSnapshot.From("Original historical comparison",forecast,[account.Id],day,null,"0.1.0");
            await store.SaveForecastSnapshotAsync(snapshot,ct);
            clock.Now=realTime.GetUtcNow();
            LedgerEntry Row(EntryKind kind,long amount) => new() { AccountId=account.Id,Kind=kind,Amount=amount,Date=day.AddDays(1),Title="Fictitious "+kind };
            var expense=Row(EntryKind.Expense,2000);var refund=Row(EntryKind.Refund,2500);
            var income=Row(EntryKind.Income,100);var reversal=Row(EntryKind.IncomeReversal,300);
            var transfer=Row(EntryKind.Transfer,700);transfer.ToAccountId=outside.Id;transfer.ToAmount=700;
            var increase=Row(EntryKind.Adjustment,200);increase.Direction=AdjustmentDirection.Increase;
            var decrease=Row(EntryKind.Adjustment,100);decrease.Direction=AdjustmentDirection.Decrease;
            var purchase=Row(EntryKind.AssetPurchase,1100);var sale=Row(EntryKind.AssetSale,1300);
            var plan=new Schedule { Name="Plan added after saving",AccountId=account.Id,Amount=900,
                Rule=new() { Frequency=Frequency.Once,Start=day.AddDays(1) } };
            var planned=Row(EntryKind.Expense,900);planned.ScheduleId=plan.Id;
            var backdated=Row(EntryKind.Expense,400);backdated.Date=day;
            await using var db=await factory.CreateDbContextAsync(ct);
            db.Settings.Add(new ZananceSettings { DefaultAccountId=account.Id });
            db.Schedules.Add(plan);
            db.Entries.AddRange(expense,refund,income,reversal,transfer,increase,decrease,purchase,sale,planned,backdated);
            // The fixture represents historical imported facts; it is not a valid new trade/refund form submission.
            await db.SaveChangesAsync(ct);
        }
        var before=await ImportFinancialNativeReview.StoredAsync(fixture,ct);
        var saved=AssertSingle(await store.GetForecastSnapshotsAsync(ct));
        var text=services.GetRequiredService<Translator>();var local=services.GetRequiredService<ILocalizationService>();
        var results=new List<JsonElement>();
        foreach (var width in widths) foreach (var language in new[] { "en","fa","de" }) foreach (var theme in new[] { ThemeChoice.Light,ThemeChoice.Dark })
        {
            var key=width+"-"+language+"-"+theme;var receipt=Path.Combine(root,key+"-snapshot-difference.json");
            if (File.Exists(receipt))
            {
                var retained=JsonSerializer.Deserialize<JsonElement>(await File.ReadAllTextAsync(receipt,ct));
                if (!retained.GetProperty("OriginalRowsUnchanged").GetBoolean()) throw new IOException("Retained comparison refused.");
                results.Add(retained);continue;
            }
            await context(language,theme,width);
            var vm=new SnapshotViewModel(store,text,new DateFormatter(local),local,realTime,fixture.GetRequiredService<ForecastWorkStore>());
            vm.ApplyQueryAttributes(new Dictionary<string,object> { ["id"]=saved.Id });await vm.LoadAsync();
            string Money(long amount)=>MoneyText.Format(amount,"EUR",local.CurrentCulture,showPlus:true);
            if (vm.NotFound || vm.NotStarted || !vm.CanCompare || vm.Points.Count!=3 || vm.Points[^1].Actual!=86 || vm.Parts.Count!=6)
                throw new IOException("Original historical comparison is incomplete.");
            foreach (var expected in new[] { ("Snapshot_RecordedLater",-400L),("Snapshot_UnplannedSpending",500L),
                ("Snapshot_UnplannedIncome",-200L),("Snapshot_OtherMovements",-400L),("Snapshot_PlansChanged",-900L),("Snapshot_Difference",-1400L) })
            {
                var part=vm.Parts.Single(item=>item.Label==text[expected.Item1]);
                if (part.Amount!=Money(expected.Item2)) throw new IOException("Original cash explanation changed.");
                if (expected.Item1=="Snapshot_UnplannedIncome" && !part.IsNegative
                    || expected.Item1=="Snapshot_OtherMovements" && part.IsNegative)
                    throw new IOException("Original income reversal or neutral movement color is wrong.");
            }
            var page=new SnapshotPage(vm);await Shell.Current.Navigation.PushModalAsync(page);
            try { await observe(page,key); }
            finally { if (ReferenceEquals(Shell.Current.Navigation.ModalStack.LastOrDefault(),page)) await Shell.Current.Navigation.PopModalAsync(); }
            if (before!=await ImportFinancialNativeReview.StoredAsync(fixture,ct)) throw new IOException("Original comparison changed stored rows.");
            var result=JsonSerializer.SerializeToElement(new { Width=width,Language=language,Theme=theme.ToString(),Parts=6,
                RecordedLater=-400,Spending=500,Income=-200,OtherMovements=-400,Residual=-900,Difference=-1400,
                OriginalRowsUnchanged=true,FinancialSaves=0,HistoricalFixtureClockOnly=true });
            await File.WriteAllTextAsync(receipt,JsonSerializer.Serialize(result),ct);results.Add(result);
        }
        if (original!=await ImportFinancialNativeReview.StoredAsync(services,ct)) throw new IOException("Independent base rows changed.");
        return new { Scope="snapshot-difference",Observations=results,OriginalBaseStoreUnchanged=true,FinancialSaves=0 };
    }

    /// <summary>Returns only the explicitly owned original saved snapshot.</summary>
    private static ForecastSnapshot AssertSingle(IReadOnlyList<ForecastSnapshot> snapshots) =>
        snapshots.Count==1 ? snapshots[0] : throw new IOException("One original snapshot required.");

    /// <summary>Finds actual visible comparison captions and amount packets by their original row identity.</summary>
    internal static IReadOnlyList<Label> Targets(SnapshotPage page)
    {
        var vm=(SnapshotViewModel)page.BindingContext;var views=PlanEditorNativeReview.Views(page).OfType<Label>().ToList();
        var result=new List<Label>();
        foreach (var part in vm.Parts)
        {
            result.Add(views.Single(label=>ReferenceEquals(label.BindingContext,part) && label.Text==part.Label));
            result.Add(views.Single(label=>ReferenceEquals(label.BindingContext,part) && label.Text==part.Amount));
        }
        result.Add(views.Single(label=>label.Text==Translator.Instance["Snapshot_PartsNote"]));
        return result;
    }
}
#endif
