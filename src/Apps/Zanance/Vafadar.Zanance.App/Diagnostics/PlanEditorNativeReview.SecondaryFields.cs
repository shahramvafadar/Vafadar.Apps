#if DEBUG || ZANANCE_INDEPENDENT_HOME_REVIEW
using Microsoft.Extensions.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using System.Text.Json;
using Vafadar.Maui.Mvvm;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Goals;
using Vafadar.Zanance.App.Features.Holdings;
using Vafadar.Zanance.App.Features.Plans;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

internal sealed partial class PlanEditorNativeReview
{
    /// <summary>Requires only original bill readback after its known accepted command outlived the Task-identity observer.</summary>
    internal bool SecondaryBillReadbackPending { get; private set; }
    private GoalStore SecondaryGoals => new(Factory, new ReviewAccess());
    private HoldingStore SecondaryHoldings => new(Factory, new ReviewAccess());
    /// <summary>Declares one independent account/two goals/two plans and one bill advance before native input.</summary>
    internal async Task PrepareSecondaryFieldsAsync(CancellationToken ct)
    {
        Store.Changed -= Accepted; Plans.Changed -= Accepted;
        if ((await Store.GetAccountsAsync(cancellationToken: ct)).Count != 0) throw new IOException("Fresh secondary monetary source required.");
        var day = DateOnly.FromDateTime(Time.GetLocalNow().DateTime);
        await Store.EnsureDefaultCategoriesAsync(ct);
        var account = new Account { Name = "Original secondary cash", CurrencyCode = "EUR", OpeningBalance = 1000000, OpeningDate = day.AddDays(-90) };
        await Store.SaveAccountAsync(account, ct);
        var type = new AssetType { Name = "Original assumed item", Dimension = AssetDimension.Count, PriceCurrencyCode = "EUR", Divisible = true };
        await SecondaryHoldings.SaveTypeAsync(type, ct);
        foreach (var quantity in new[] { false, true })
        {
            var goal = new Goal { Name = quantity ? "Original secondary quantity" : "Original secondary earmark", CurrencyCode = "EUR",
                Type = quantity ? GoalType.HoldingQuantity : GoalType.Earmark, AssetTypeId = quantity ? type.Id : null, TargetAmount = 10000000, TargetDate = day.AddMonths(3) };
            await SecondaryGoals.SaveGoalWithContributionPlanAsync(goal, new ContributionPlan { GoalId = goal.Id, Amount = 1000,
                AssumedPricePerUnitMilli = quantity ? 1000000 : null, Rule = new() { Start = day, Frequency = Frequency.Monthly } }, ct);
        }
        var category = (await Store.GetCategoriesAsync(cancellationToken: ct)).Single(row => row.SystemKey == "Food");
        var plan = new Schedule { Name = "Original secondary occurrence", Kind = EntryKind.Expense, AccountId = account.Id,
            CategoryId = category.Id, Amount = 1000, Rule = new() { Start = day } };
        await Plans.SaveScheduleAsync(plan, ct);
        var bill = new Schedule { Name = "Original secondary bill", Kind = EntryKind.Expense, AccountId = account.Id,
            CategoryId = category.Id, Amount = 1000, Rule = new() { Start = day.AddDays(-7) } };
        await Plans.SaveScheduleAsync(bill, ct);
        var advance = await Store.SaveEntryAsync(new LedgerEntry { Kind = EntryKind.Expense, AccountId = account.Id, CategoryId = category.Id,
            ScheduleId = bill.Id, Amount = 1000, Date = day.AddDays(-7), Title = "Original secondary advance" }, ct);
        if (!advance.Succeeded) throw new IOException("Declared bill advance refused.");
        await File.WriteAllTextAsync(Path.Combine(_folder, "secondary-fields-prepared.json"), await AllMoneyRowsAsync(ct), ct);
    }
    /// <summary>Continues only exact original source checkpoints; never retries an uncertain accepted command.</summary>
    internal static async Task<PlanEditorNativeReview> ContinueSecondaryFieldsAsync(IServiceProvider host, string folder, CancellationToken ct)
    {
        var result = new PlanEditorNativeReview(host, folder); var checkpoint = "secondary-fields-prepared.json";
        foreach (var key in new[] { "goal", "price", "override", "partial", "bill" })
        {
            var path = "secondary-fields-" + key + "-accepted.json";
            if (File.Exists(Path.Combine(folder, path))) checkpoint = path;
            else if (File.Exists(Path.Combine(folder, "secondary-fields-" + key + ".marker")))
            {
                if (key != "bill") throw new IOException("Inspect original accepted secondary source without replay before continuing.");
                await result.RecoverSecondaryBillObservationAsync(ct); checkpoint = path;
            }
        }
        if (await File.ReadAllTextAsync(Path.Combine(folder, checkpoint), ct) != await result.AllMoneyRowsAsync(ct))
            throw new IOException("Original secondary monetary source differs from its exact checkpoint.");
        result.SecondaryBillReadbackPending = File.Exists(Path.Combine(folder, "secondary-fields-bill-observer-recovery.json"))
            && !File.Exists(Path.Combine(folder, "secondary-fields-bill-native-readback.complete"));
        return result;
    }
    /// <summary>Inspects the exact known bill commit; only one declared extra expense may differ from the last complete checkpoint.</summary>
    private async Task RecoverSecondaryBillObservationAsync(CancellationToken ct)
    {
        var before = JsonSerializer.Deserialize<Dictionary<string, string[]>>(await File.ReadAllTextAsync(Path.Combine(_folder, "secondary-fields-partial-accepted.json"), ct))!;
        var current = await AllMoneyRowsAsync(ct); var after = JsonSerializer.Deserialize<Dictionary<string, string[]>>(current)!;
        if (!before.Keys.Order(StringComparer.Ordinal).SequenceEqual(after.Keys.Order(StringComparer.Ordinal))
            || before.Keys.Where(key => key != "Entries").Any(key => !before[key].SequenceEqual(after[key]))
            || before["Entries"].Length != 2 || after["Entries"].Length != 3 || before["Entries"].Except(after["Entries"], StringComparer.Ordinal).Any())
            throw new IOException("Original accepted bill changed more than its one declared expense.");
        var entries = await Store.GetEntriesAsync(cancellationToken: ct);
        var bill = (await Plans.GetSchedulesAsync(ct)).Single(row => row.Name == "Original secondary bill");
        var extra = entries.Single(row => row.Amount == 234);
        if (entries.Count != 3 || !entries.Select(row => row.Amount).Order().SequenceEqual(new long[] { 234, 400, 1000 })
            || extra.Kind != EntryKind.Expense || extra.AccountId != bill.AccountId || extra.CategoryId != bill.CategoryId
            || extra.ScheduleId is not null || extra.Review != ReviewState.Confirmed || extra.Source != EntrySource.Manual
            || extra.Date != bill.Rule.Start.AddDays(7) || extra.Title != Translator.Format("Settlement_ExtraTitle", bill.Name))
            throw new IOException("Original accepted bill does not match its declared account/category/date/amount.");
        await File.WriteAllTextAsync(Path.Combine(_folder, "secondary-fields-bill-accepted.json"), current, ct);
        await File.WriteAllTextAsync(Path.Combine(_folder, "secondary-fields-bill-observer-recovery.json"), JsonSerializer.Serialize(new
        { OriginalExpense = extra.Id, OriginalAmount = extra.Amount, UnchangedTables = before.Count - 1, AddedEntries = 1, CommandsReplayed = 0 }), ct);
    }
    /// <summary>Gets only the affected actual raw monetary values.</summary>
    internal static HashSet<string> SecondaryFieldsRaw(Page page) => page.BindingContext switch
    {
        GoalDetailViewModel goal => new[] { goal.AmountText, goal.AssumedPriceText }.ToHashSet(StringComparer.Ordinal),
        OccurrenceViewModel occurrence => new[] { occurrence.ActualAmountText, occurrence.OverrideAmountText }.ToHashSet(StringComparer.Ordinal),
        SettlementViewModel bill => new[] { bill.ActualText }.ToHashSet(StringComparer.Ordinal),
        _ => [],
    };
    /// <summary>Gets original parser/unit captions and actual field-local review from the original page model.</summary>
    internal static HashSet<string> SecondaryFieldsCaptions(Page page)
    {
        IEnumerable<string?> values = page.BindingContext switch
        {
            GoalDetailViewModel goal => [goal.UnitNote, goal.AmountFormatNote, goal.AmountError, goal.PriceUnitNote, goal.PriceFormatNote,
                goal.PriceError, goal.AssumedPriceLabel, goal.SaveError],
            OccurrenceViewModel occurrence => [occurrence.PaymentUnitName, occurrence.PaymentUnitNote, occurrence.PaymentFormatNote,
                occurrence.OverrideUnitName, occurrence.OverrideUnitNote, occurrence.OverrideFormatNote, occurrence.PaymentAmountError, occurrence.OverrideAmountError],
            SettlementViewModel bill => [bill.UnitNote, bill.InputFormatNote, bill.AmountError, bill.SaveFeedback, bill.Error],
            _ => [],
        };
        return values.Where(value => !string.IsNullOrEmpty(value)).Select(value => value!).ToHashSet(StringComparer.Ordinal);
    }
    private bool SecondaryAccepted(string key) => File.Exists(Path.Combine(_folder, "secondary-fields-" + key + "-accepted.json"));
    private async Task AcceptSecondaryAsync(string key, Action<EventHandler> attach, Action<EventHandler> detach,
        Func<Task> invoke, Func<Task> verify, CancellationToken ct)
    {
        if (SecondaryAccepted(key)) return;
        var marker = Path.Combine(_folder, "secondary-fields-" + key + ".marker");
        if (File.Exists(marker)) throw new IOException("Do not replay uncertain accepted secondary input.");
        void Committed(object? sender, EventArgs args) => File.WriteAllText(marker, "Original native secondary input committed: " + key);
        attach(Committed);
        try { await invoke(); await verify(); }
        finally { detach(Committed); }
        if (!File.Exists(marker)) throw new IOException("Original secondary input acceptance was not observed.");
        await File.WriteAllTextAsync(Path.Combine(_folder, "secondary-fields-" + key + "-accepted.json"), await AllMoneyRowsAsync(ct), ct);
    }
    /// <summary>Checks four real secondary pages per context and accepts five declared monetary actions only once for the entire source.</summary>
    internal async Task SecondaryFieldsContextAsync(string context, Func<Page, System.Windows.Input.ICommand, bool?, Task> invoke,
        Func<Page, string, Task> observe, CancellationToken ct, Func<string, bool>? completed = null)
    {
        var units = DisplayUnits.All; var culture = Localization.FormattingCultureName; var before = await AllMoneyRowsAsync(ct);
        var goals = SecondaryGoals; var holdings = SecondaryHoldings; var effects = new MoneyInteraction();
        var dates = new DateFormatter(Localization); string? route = null; var pendingDisplay = false;
        async Task Act(Page page, System.Windows.Input.ICommand command, bool? consent)
        {
            var previous = (command as IAsyncRelayCommand)?.ExecutionTask; var entered = false;
            var owner = page.BindingContext as ViewModelBase;
            void BusyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs args)
            { if (args.PropertyName == nameof(ViewModelBase.IsBusy) && owner?.IsBusy == true) entered = true; }
            if (owner is not null) owner.PropertyChanged += BusyChanged;
            try
            {
                await invoke(page, command, consent);
                if (command is IAsyncRelayCommand asyncCommand)
                {
                    // A synchronous completed command can reuse its completed Task. Its actual busy transition also witnesses invocation.
                    await WaitAsync(() => asyncCommand.ExecutionTask is not null && (entered || !ReferenceEquals(previous, asyncCommand.ExecutionTask)), ct);
                    await asyncCommand.ExecutionTask!.WaitAsync(ct);
                }
            }
            finally { if (owner is not null) owner.PropertyChanged -= BusyChanged; }
        }
        async Task Leave()
        {
            await ReturnMoneyTabAsync(pendingDisplay, ct); pendingDisplay = false;
            if (route is not null) { Routing.UnRegisterRoute(route); _routes.Remove(route); route = null; }
        }
        async Task View(Page page, string stage) { if (completed?.Invoke(context + "-" + stage) != true) await observe(page, context + "-" + stage); }
        try
        {
            if (SecondaryBillReadbackPending)
            {
                DisplayUnits.Set([new("EUR", "Hundred euros", 2)]); await SetTabMoneyCultureAsync("de-DE", ct);
                var bill = (await Plans.GetSchedulesAsync(ct)).Single(row => row.Name == "Original secondary bill");
                var readback = new SettlementViewModel(Store, Translator, Localization, Time, effects); await readback.LoadAsync(bill.Id);
                var readbackPage = new SettlementPage(readback); route = await ShowAsync(readbackPage); await WaitAsync(() => readbackPage.Width > 0, ct);
                if (readback.CanSave || !string.IsNullOrEmpty(readback.ActualText) || before != await AllMoneyRowsAsync(ct))
                    throw new IOException("Original accepted bill readback was not an unchanged empty new draft.");
                await View(readbackPage, "bill-source-readback");
                await File.WriteAllTextAsync(Path.Combine(_folder, "secondary-fields-bill-native-readback.complete"), context, ct);
                SecondaryBillReadbackPending = false; await Leave();
            }
            foreach (var quantity in new[] { false, true })
            {
                var key = quantity ? "price" : "goal";
                if (completed?.Invoke(context + "-" + key + "-review") == true && SecondaryAccepted(key)) continue;
                DisplayUnits.Set([]); await SetTabMoneyCultureAsync("en-US", ct);
                var saved = (await goals.GetGoalsAsync(ct)).Single(row => row.Type == (quantity ? GoalType.HoldingQuantity : GoalType.Earmark));
                var presenter = new GoalPresenter(Translator, dates, Localization, holdings, new HoldingText(Translator, Localization, dates));
                var model = new GoalDetailViewModel(Store, goals, Plans, holdings, presenter, Translator, dates, Localization, Time, effects);
                model.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = saved.Id });
                var page = new GoalDetailPage(model); route = await ShowAsync(page);
                await WaitAsync(() => model.CanInteract && page.Width > 0, ct);
                if (quantity) model.AssumedPriceText = "1,234";
                else { model.SelectedAccount = model.Accounts.Single(); model.AmountText = "1,234"; }
                Localization.SetFormattingCulture("de-DE"); pendingDisplay = true; await ((IThemeAware)model).RefreshThemeAsync();
                if (quantity ? model.PriceFormatNote is null : model.AmountFormatNote is null) throw new IOException("Original secondary goal parser note missing.");
                await View(page, key + "-original");
                DisplayUnits.Set([new("EUR", "Hundred euros", 2)]); await ((IThemeAware)model).RefreshThemeAsync();
                if (quantity ? model.PriceError is null || model.CanSaveAssumedPrice : model.AmountError is null || model.CanAllocate)
                    throw new IOException("Original goal secondary input borrowed another unit.");
                await View(page, key + "-review");
                if (before != await AllMoneyRowsAsync(ct)) throw new IOException("Original goal secondary observation changed rows.");
                if (!SecondaryAccepted(key))
                {
                    if (quantity) { model.AssumedPriceText = ""; model.AssumedPriceText = "0,1234"; }
                    else { model.AmountText = ""; model.AmountText = "0,1234"; }
                    await AcceptSecondaryAsync(key, handler => goals.Changed += handler, handler => goals.Changed -= handler,
                        () => Act(page, quantity ? model.SaveAssumedPriceCommand : model.AllocateCommand, null), async () =>
                        {
                            await WaitAsync(() => !model.IsBusy && model.CanInteract, ct);
                            if (quantity ? (await goals.GetContributionPlansAsync(ct)).Single(row => row.GoalId == saved.Id).AssumedPricePerUnitMilli != 1234000
                                : (await goals.GetAllocationsAsync(saved.Id, ct)).Single().Amount != 1234)
                                throw new IOException("Actual secondary goal Save changed original monetary meaning.");
                        }, ct);
                    before = await AllMoneyRowsAsync(ct); await View(page, key + "-accepted");
                }
                await Leave();
            }
            if (completed?.Invoke(context + "-occurrence-review") != true || !SecondaryAccepted("partial"))
            {
                DisplayUnits.Set([]); await SetTabMoneyCultureAsync("en-US", ct);
                var plan = (await Plans.GetSchedulesAsync(ct)).Single(row => row.Name == "Original secondary occurrence");
                var model = new OccurrenceViewModel(Store, Plans, Translator, Localization, dates, Time, new(Plans, Translator, effects));
                model.ApplyQueryAttributes(new Dictionary<string, object> { ["plan"] = plan.Id, ["date"] = plan.Rule.Start });
                var page = new OccurrencePage(model); route = await ShowAsync(page);
                await WaitAsync(() => model.Loading.IsReady && page.Width > 0, ct); await page.PendingPublication.WaitAsync(ct);
                model.ShowChange = true; model.ActualAmountText = "1,234"; model.OverrideAmountText = "1,234";
                Localization.SetFormattingCulture("de-DE"); pendingDisplay = true; await ((IThemeAware)model).RefreshThemeAsync();
                if (model.PaymentFormatNote is null || model.OverrideFormatNote is null) throw new IOException("Original occurrence parser captions missing.");
                await View(page, "occurrence-original");
                DisplayUnits.Set([new("EUR", "Hundred euros", 2)]);
                await Act(page, model.PayPartCommand, null); await WaitAsync(() => !model.IsBusy && model.PaymentAmountError is not null, ct);
                await Act(page, model.SaveChangeCommand, null); await WaitAsync(() => !model.IsBusy && model.OverrideAmountError is not null, ct);
                await View(page, "occurrence-review");
                if (before != await AllMoneyRowsAsync(ct)) throw new IOException("Original occurrence review changed rows.");
                if (!SecondaryAccepted("override"))
                {
                    model.OverrideAmountText = ""; model.OverrideAmountText = "0,1234";
                    await AcceptSecondaryAsync("override", handler => Plans.Changed += handler, handler => Plans.Changed -= handler,
                        () => Act(page, model.SaveChangeCommand, null), async () =>
                        {
                            await WaitAsync(() => !model.IsBusy && model.Loading.IsReady && !model.ShowChange, ct);
                            if ((await Plans.GetStatesAsync(plan.Id, ct)).Single().Amount != 1234) throw new IOException("Original override acceptance differs.");
                        }, ct);
                    before = await AllMoneyRowsAsync(ct); await View(page, "override-accepted");
                }
                if (!SecondaryAccepted("partial"))
                {
                    model.ActualAmountText = ""; model.ActualAmountText = "0,04";
                    await AcceptSecondaryAsync("partial", handler => Store.Changed += handler, handler => Store.Changed -= handler,
                        () => Act(page, model.PayPartCommand, null), async () =>
                        {
                            await WaitAsync(() => !model.IsBusy && model.Loading.IsReady, ct);
                            if ((await Store.GetEntriesAsync(cancellationToken: ct)).Single(row => row.ScheduleId == plan.Id).Amount != 400)
                                throw new IOException("Original partial payment acceptance differs.");
                        }, ct);
                    before = await AllMoneyRowsAsync(ct); await View(page, "partial-accepted");
                }
                await Leave();
            }
            if (completed?.Invoke(context + "-bill-review") != true || !SecondaryAccepted("bill"))
            {
                DisplayUnits.Set([]); await SetTabMoneyCultureAsync("en-US", ct);
                var plan = (await Plans.GetSchedulesAsync(ct)).Single(row => row.Name == "Original secondary bill");
                var model = new SettlementViewModel(Store, Translator, Localization, Time, effects); await model.LoadAsync(plan.Id);
                model.From = plan.Rule.Start; model.To = plan.Rule.Start.AddDays(7);
                var page = new SettlementPage(model); route = await ShowAsync(page); await WaitAsync(() => page.Width > 0, ct);
                model.ActualText = "1,234"; Localization.SetFormattingCulture("de-DE"); pendingDisplay = true;
                await ((IThemeAware)model).RefreshThemeAsync();
                if (model.InputFormatNote is null) throw new IOException("Original bill parser note missing.");
                await View(page, "bill-original");
                DisplayUnits.Set([new("EUR", "Hundred euros", 2)]); await ((IThemeAware)model).RefreshThemeAsync();
                if (model.AmountError is null) throw new IOException("Original bill borrowed another unit.");
                await Act(page, model.SaveCommand, null); await WaitAsync(() => !model.IsBusy, ct);
                await View(page, "bill-review");
                if (before != await AllMoneyRowsAsync(ct)) throw new IOException("Original bill review changed rows.");
                if (!SecondaryAccepted("bill"))
                {
                    model.ActualText = ""; model.ActualText = "0,1234";
                    await AcceptSecondaryAsync("bill", handler => Store.Changed += handler, handler => Store.Changed -= handler,
                        () => Act(page, model.SaveCommand, null), async () =>
                        {
                            await WaitAsync(() => !model.IsBusy && model.HasCommitted, ct);
                            if ((await Store.GetEntriesAsync(cancellationToken: ct)).Count != 3) throw new IOException("Original bill accepted unexpected extra ledger rows.");
                        }, ct);
                    before = await AllMoneyRowsAsync(ct); await View(page, "bill-accepted");
                }
                await Leave();
            }
            if (before != await AllMoneyRowsAsync(ct)) throw new IOException("Secondary monetary context changed original source after its accepted checkpoint.");
        }
        finally { await Leave(); DisplayUnits.Set(units); await SetTabMoneyCultureAsync(culture, ct); }
    }
    /// <summary>Checks actual stored actions rather than treating capture counts as financial evidence.</summary>
    internal async Task VerifySecondaryFieldsAsync(CancellationToken ct)
    {
        if (new[] { "goal", "price", "override", "partial", "bill" }.Any(key => !SecondaryAccepted(key))) throw new IOException("Original secondary acceptance receipt missing.");
        var goals = SecondaryGoals; var earmark = (await goals.GetGoalsAsync(ct)).Single(row => row.Type == GoalType.Earmark);
        var quantity = (await goals.GetGoalsAsync(ct)).Single(row => row.Type == GoalType.HoldingQuantity);
        if ((await goals.GetAllocationsAsync(earmark.Id, ct)).Single().Amount != 1234
            || (await goals.GetContributionPlansAsync(ct)).Single(row => row.GoalId == quantity.Id).AssumedPricePerUnitMilli != 1234000)
            throw new IOException("Original goal/price accepted amounts differ.");
        var entries = await Store.GetEntriesAsync(cancellationToken: ct);
        if (entries.Count != 3 || !entries.Select(row => row.Amount).Order().SequenceEqual(new long[] { 234, 400, 1000 }) || entries.Any(row => row.Kind != EntryKind.Expense))
            throw new IOException("Original secondary financial acceptance differs.");
    }
}
#endif
