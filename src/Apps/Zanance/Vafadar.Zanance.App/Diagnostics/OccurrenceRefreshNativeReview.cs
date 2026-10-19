#if (DEBUG && WINDOWS) || ZANANCE_INDEPENDENT_HOME_REVIEW
using System.Text.Json;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Plans;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

/// <summary>Original retained occurrence publication over owned SQLite, actual tabs, covers and native commands.</summary>
internal static class OccurrenceRefreshNativeReview
{
    private sealed class ReadGate
    {
        internal readonly TaskCompletionSource Entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal readonly TaskCompletionSource Release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private sealed class ControlledFactory(IDbContextFactory<ZananceDbContext> original) : IDbContextFactory<ZananceDbContext>
    {
        internal int Count;
        internal ReadGate? Gate;
        internal bool FailNext;
        public ZananceDbContext CreateDbContext() => throw new IOException("Only the original asynchronous occurrence reader is qualified.");
        public async Task<ZananceDbContext> CreateDbContextAsync(CancellationToken ct = default)
        {
            Interlocked.Increment(ref Count);
            if (Gate is { } gate) { Gate = null; gate.Entered.TrySetResult(); await gate.Release.Task.WaitAsync(ct); }
            if (FailNext) { FailNext = false; throw new IOException("Owned original occurrence reader refusal."); }
            return await original.CreateDbContextAsync(ct);
        }
    }

    /// <summary>Runs one original native context once; callbacks operate only its actual owned page and Shell.</summary>
    internal static async Task<object> RunAsync(IServiceProvider services, string folder, string context,
        Func<OccurrencePage, Task> show, Func<Task> leave, Func<Task> activate,
        Func<string, OccurrencePage, OccurrenceViewModel, Task> capture,
        Func<OccurrencePage, ICommand, Task> invoke, Func<OccurrencePage, Task> close, CancellationToken ct)
    {
        var package = AppInfo.Current.PackageName;
        if (!package.StartsWith("pro.vafadar.zanance.storepreview.", StringComparison.Ordinal)
            && package != "pro.vafadar.zanance.androidhomereview") throw new IOException("Independent occurrence review identity required.");
        Directory.CreateDirectory(folder); var file = Path.Combine(folder, "original-occurrence.db");
        if (File.Exists(file)) throw new IOException("Preserve the accepted original fixture; never replay it.");
        var local = services.GetRequiredService<ILocalizationService>(); var text = services.GetRequiredService<Translator>();
        var time = services.GetRequiredService<TimeProvider>(); var day = DateOnly.FromDateTime(time.GetLocalNow().DateTime);
        await using var fixture = new ServiceCollection().AddZananceData(file).AddSingleton(time).BuildServiceProvider();
        await Task.Run(() => fixture.MigrateLocalDatabase<ZananceDbContext>(), ct);
        var source = fixture.GetRequiredService<ZananceStore>(); var sourcePlans = fixture.GetRequiredService<PlanStore>();
        await source.EnsureDefaultCategoriesAsync(ct);
        var account = new Account { Name = "D401 original " + context, Type = AccountType.Cash, CurrencyCode = "EUR", OpeningDate = day.AddDays(-30) };
        if (!await source.SaveAccountAsync(account, ct)) throw new IOException("Owned original account refused.");
        var plan = new Schedule { Name = "D401 retained " + context, AccountId = account.Id, Kind = EntryKind.Expense,
            Amount = 1000, Rule = new() { Start = day, Frequency = Frequency.Once } };
        await sourcePlans.SaveScheduleAsync(plan, ct);
        var controlled = new ControlledFactory(fixture.GetRequiredService<IDbContextFactory<ZananceDbContext>>());
        var store = ActivatorUtilities.CreateInstance<ZananceStore>(fixture, controlled);
        var plans = ActivatorUtilities.CreateInstance<PlanStore>(fixture, controlled, store);
        var commands = new OccurrenceCommands(plans, text, services.GetRequiredService<IAppInteraction>());
        var vm = ActivatorUtilities.CreateInstance<OccurrenceViewModel>(services, store, plans, commands);
        vm.ApplyQueryAttributes(new Dictionary<string, object> { ["plan"] = plan.Id, ["date"] = day });
        var page = new OccurrencePage(vm) { FlowDirection = local.CurrentLanguage.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
        async Task Ready(Func<bool> condition)
        {
            var deadline = DateTime.UtcNow.AddSeconds(40);
            while (!condition()) { if (DateTime.UtcNow >= deadline) throw new IOException("Preserve the original occurrence publication and live process."); await Task.Delay(25, ct); }
        }
        async Task<string> Rows() => JsonSerializer.Serialize(new { Accounts = await source.GetAccountsAsync(cancellationToken: ct), Entries = await source.GetEntriesAsync(cancellationToken: ct),
            Plans = await sourcePlans.GetSchedulesAsync(cancellationToken: ct), States = await sourcePlans.GetStatesAsync(plan.Id, ct) });
        await show(page); await page.PendingPublication;
        await Ready(() => vm.Loading.IsReady && vm.IsOpen);
        if (vm.HasCandidates) throw new IOException("The original empty occurrence has an unexpected candidate.");
        var payment = MoneyText.ForInput(437, "EUR", local.CurrentCulture); var amountOverride = MoneyText.ForInput(1234, "EUR", local.CurrentCulture);
        vm.ActualAmountText = payment; vm.ActualDate = day.AddDays(-1); vm.DueDate = day.AddDays(2);
        vm.OverrideAmountText = amountOverride; vm.OccurrenceNote = "D401 original unsaved note"; vm.ToggleChangeCommand.Execute(null);
        bool Draft() => vm.ActualAmountText == payment && vm.ActualDate == day.AddDays(-1) && vm.DueDate == day.AddDays(2)
            && vm.OverrideAmountText == amountOverride && vm.OccurrenceNote == "D401 original unsaved note" && vm.ShowChange;
        await leave();
        var entry = new LedgerEntry { AccountId = account.Id, Kind = EntryKind.Expense, Amount = 1000, Date = day,
            Title = "D401 original candidate", Note = "Original accepted source row" };
        if (!(await source.SaveEntryAsync(entry, ct)).Succeeded) throw new IOException("The independent original candidate was not accepted.");
        var acceptedRows = await Rows(); await activate();
        await Ready(() => vm.Loading.IsReady && vm.Candidates.Any(row => row.Id == entry.Id));
        if (!Draft() || acceptedRows != await Rows()) throw new IOException("Retained activation replaced the original draft or source rows.");
        await capture("loaded", page, vm);

        var gate = new ReadGate(); controlled.Gate = gate; var count = controlled.Count;
        var loading = page.RefreshForNavigationAsync(); await gate.Entered.Task.WaitAsync(ct);
        var shared = page.RefreshForNavigationAsync(); var themed = ((IThemeAware)vm).RefreshThemeAsync();
        if (controlled.Count != count + 1 || vm.Loading.IsReady || !Draft()) throw new IOException("The original appearance reads did not share one cover.");
        await vm.ConfirmCommand.ExecuteAsync(null);
        if (acceptedRows != await Rows()) throw new IOException("Covered original controls posted money.");
        await capture("loading", page, vm); gate.Release.TrySetResult(); await Task.WhenAll(loading, shared, themed);
        await Ready(() => vm.Loading.IsReady);
        if (!Draft() || acceptedRows != await Rows()) throw new IOException("The coalesced original publication lost input or wrote money.");

        controlled.FailNext = true; await page.RefreshForNavigationAsync();
        if (!vm.Loading.HasFailed || vm.Loading.IsReady || !Draft() || vm.Error != text["Occurrence_ReadFailed"])
            throw new IOException("A failed original read exposed the old form or lost its draft.");
        await capture("failed", page, vm);
        var retryCount = controlled.Count; await invoke(page, vm.ReloadCommand); await Ready(() => controlled.Count > retryCount && vm.Loading.IsReady);
        if (!Draft() || acceptedRows != await Rows()) throw new IOException("Original native Retry changed the draft or accepted money.");

        var other = new Account { Name = "D401 distinct original target", Type = AccountType.Cash, CurrencyCode = "EUR", OpeningDate = day.AddDays(-30) };
        if (!await source.SaveAccountAsync(other, ct)) throw new IOException("The distinct owned target refused.");
        plan.AccountId = other.Id; await sourcePlans.SaveScheduleAsync(plan, ct); var changedTargetRows = await Rows();
        await page.RefreshForNavigationAsync();
        if (!vm.Loading.HasFailed || vm.Loading.IsReady || !Draft()) throw new IOException("The original dirty payment was retargeted.");
        await vm.ConfirmCommand.ExecuteAsync(null);
        if (changedTargetRows != await Rows()) throw new IOException("A refused target change posted money.");
        plan.AccountId = account.Id; await sourcePlans.SaveScheduleAsync(plan, ct);
        retryCount = controlled.Count; await invoke(page, vm.ReloadCommand); await Ready(() => controlled.Count > retryCount && vm.Loading.IsReady);
        if (!Draft()) throw new IOException("The original target retry lost input.");

        var partial = false;
        if (context == "en-Light")
        {
            vm.ActualAmountText = MoneyText.ForInput(400, "EUR", local.CurrentCulture); vm.ActualDate = day;
            var writerGate = new ReadGate(); controlled.Gate = writerGate; var beforeWrite = controlled.Count;
            await invoke(page, vm.PayPartCommand); await writerGate.Entered.Task.WaitAsync(ct);
            await page.RefreshForNavigationAsync();
            if (!vm.IsBusy || controlled.Count != beforeWrite + 1) throw new IOException("Activation interrupted the pending original financial command.");
            writerGate.Release.TrySetResult();
            await (((IAsyncRelayCommand)vm.PayPartCommand).ExecutionTask ?? throw new IOException("Original partial command missing."));
            await Ready(() => vm.Loading.IsReady && !vm.IsBusy);
            var rows = await source.GetEntriesAsync(cancellationToken: ct); var state = (await sourcePlans.GetStatesAsync(plan.Id, ct)).Single();
            if (rows.Count != 2 || rows.Count(row => row.IsPartialPayment && row.Amount == 400) != 1 || state.PaidAmount != 400
                || vm.ActualAmountText != MoneyText.ForInput(600, "EUR", local.CurrentCulture)) throw new IOException("Known partial acceptance did not publish its new canonical default once.");
            partial = true;
        }
        var result = new { Context = context, OriginalPageRetained = true, DraftPreserved = true, CandidateAppearedAfterActualTab = true,
            SharedReadCovered = true, FailedReadCovered = true, NativeRetryPreservedSource = true, ChangedFinancialTargetRefused = true,
            AcceptedPartialOnce = partial, NoFinancialReplay = true, SourceCandidateId = entry.Id, ProcessId = Environment.ProcessId };
        await File.WriteAllTextAsync(Path.Combine(folder, "complete.json"), JsonSerializer.Serialize(result), ct);
        await close(page); return result;
    }
}
#endif
