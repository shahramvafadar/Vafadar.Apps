#if DEBUG || ZANANCE_INDEPENDENT_HOME_REVIEW
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.App.Features.Goals;
using Vafadar.Zanance.App.Features.Holdings;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

internal sealed partial class PlanEditorNativeReview
{
    /// <summary>Qualifies only changed original detail reads, retained drafts, retry and actual departure.</summary>
    internal async Task GoalDetailContextAsync(string context, bool quantity, Func<Page, VisualElement, Task> invoke,
        Func<Page, Task> back, Func<Page, string, Task> observe, CancellationToken ct, bool retryObserved = false)
    {
        var before = await AllMoneyRowsAsync(ct); var goals = SecondaryGoals; var holdings = SecondaryHoldings;
        var saved = (await goals.GetGoalsAsync(ct)).Single(goal => goal.Type == (quantity ? GoalType.HoldingQuantity : GoalType.Earmark));
        var dates = new DateFormatter(Localization);
        var presenter = new GoalPresenter(Translator, dates, Localization, holdings, new HoldingText(Translator, Localization, dates));
        var model = new GoalDetailViewModel(Store, goals, Plans, holdings, presenter, Translator, dates, Localization, Time, new MoneyInteraction());
        model.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = saved.Id });
        var page = new GoalDetailPage(model); string? route = null;
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Factory.GateRequested = false; Factory.Gate = gate;
        try
        {
            route = await ShowAsync(page);
            await WaitAsync(() => Factory.GateRequested && model.IsLoading && page.IsLoaded, ct);
            if (model.CanView || model.CanInteract || model.EditCommand.CanExecute(null) || page.FindByName<Grid>("ReadySurface").IsVisible)
                throw new IOException("Original detail became visible/actionable before its complete read.");
            await observe(page, context + "-loading"); gate.TrySetResult();
            await page.PendingPublication.WaitAsync(ct); await WaitAsync(() => model.CanInteract && model.Summary?.Id == saved.Id, ct);
            var packet = await goals.ReadDetailSnapshotAsync(ct);
            var progress = await presenter.EvaluateAsync(packet.Finances.Goals, packet.Finances.Accounts, packet.Finances.Entries,
                DateOnly.FromDateTime(Time.GetLocalNow().DateTime));
            var expected = presenter.Row(saved, progress.Single(item => item.Goal.Id == saved.Id), packet.Finances.Goals.Work);
            if (model.Summary != expected) throw new IOException("Original detail financial packet differs from its established presenter.");
            await observe(page, context + "-ready");
            // Publish an unsaved original-format draft; this is native binding observation, not keyboard/IME acceptance.
            var raw = MoneyText.ForInput(quantity ? 2345 : 1234, "EUR", Localization.CurrentCulture);
            if (quantity) model.AssumedPriceText = raw;
            else { model.DirectionIndex = 1; model.AmountText = raw; }
            var account = model.SelectedAccount?.Id; var oldRow = model.History.FirstOrDefault();
            if (!retryObserved)
            {
                Factory.RefuseOnce = true;
                try { await model.LoadAsync(); throw new IOException("Declared original detail read refusal was not observed."); }
                catch (IOException) when (model.HasReadFailed) { }
                if (model.CanView || model.CanInteract || model.EditCommand.CanExecute(null)
                    || page.FindByName<Grid>("ReadySurface").IsVisible || !page.FindByName<VerticalStackLayout>("ErrorSurface").IsVisible)
                    throw new IOException("Original detail did not retain its local failed-read cover.");
                await observe(page, context + "-failed");
                await invoke(page, page.FindByName<Button>("RetryLoadButton"));
                await WaitAsync(() => model.CanInteract, ct);
            }
            // A declared observer-only continuation retains the already accepted native Retry and repairs only its reading.
            if ((quantity ? model.AssumedPriceText : model.AmountText) != raw || model.SelectedAccount?.Id != account
                || !quantity && model.DirectionIndex != 1 || model.Summary != expected)
                throw new IOException("Original detail retry changed its unsaved original draft or financial packet.");
            if (!retryObserved && oldRow is not null && model.DeleteAllocationCommand.CanExecute(oldRow))
                throw new IOException("An old allocation row became actionable after original detail Retry.");
            await observe(page, context + "-retried");
            var oldEdit = model.EditCommand;
            await back(page); await WaitAsync(() => !ReferenceEquals(Shell.Current.CurrentPage, page), ct);
            if (model.CanInteract || model.CanView || oldEdit.CanExecute(null)) throw new IOException("Original detail commands survived native departure.");
            await oldEdit.ExecuteAsync(null);
            if (before != await AllMoneyRowsAsync(ct)) throw new IOException("Original detail qualification changed accepted source rows.");
            Observations.Add(new { Context = context, Goal = saved.Id, Quantity = quantity, ProcessId = Environment.ProcessId,
                CompleteOriginalRowsUnchanged = true, NativeRetry = true, RetainedNativeRetry = retryObserved, NativeBack = true,
                RetainedDraft = raw, RetiredCommandRefused = true, AcceptedWrites = 0 });
        }
        finally
        {
            gate.TrySetResult(); Factory.Gate = null; Factory.RefuseOnce = false; model.Retire();
            if (route is not null) await LeaveAsync(route);
        }
    }

    /// <summary>Continues only the new failed Edit validation path without repeating completed detail cohorts.</summary>
    internal async Task GoalDetailEditFailureContextAsync(string context, Func<Page, VisualElement, Task> invoke,
        Func<Page, Task> back, Func<Page, string, Task> observe, CancellationToken ct)
    {
        var before = await AllMoneyRowsAsync(ct); var goals = SecondaryGoals; var holdings = SecondaryHoldings;
        var saved = (await goals.GetGoalsAsync(ct)).Single(goal => goal.Type == GoalType.Earmark);
        var dates = new DateFormatter(Localization);
        var presenter = new GoalPresenter(Translator, dates, Localization, holdings, new HoldingText(Translator, Localization, dates));
        var model = new GoalDetailViewModel(Store, goals, Plans, holdings, presenter, Translator, dates, Localization, Time, new MoneyInteraction());
        model.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = saved.Id }); var page = new GoalDetailPage(model);
        var route = await ShowAsync(page);
        try
        {
            await page.PendingPublication.WaitAsync(ct); await WaitAsync(() => model.CanInteract, ct);
            var summary = model.Summary; var raw = MoneyText.ForInput(123, "EUR", Localization.CurrentCulture); model.AmountText = raw;
            var edit = Views(page).OfType<Button>().Single(button => ReferenceEquals(button.Command, model.EditCommand));
            Factory.RefuseOnce = true; await invoke(page, edit);
            await WaitAsync(() => model.HasReadFailed && !model.IsBusy, ct);
            if (model.CanView || model.CanInteract || model.AmountText != raw || model.Summary != summary)
                throw new IOException("Original failed Edit did not retain its covered draft.");
            await observe(page, context + "-edit-failed");
            await invoke(page, page.FindByName<Button>("RetryLoadButton")); await WaitAsync(() => model.CanInteract, ct);
            if (model.AmountText != raw || model.Summary != summary) throw new IOException("Original Edit Retry changed its draft/source.");
            await observe(page, context + "-edit-retried"); await back(page);
            await WaitAsync(() => !ReferenceEquals(Shell.Current.CurrentPage, page), ct);
            if (before != await AllMoneyRowsAsync(ct)) throw new IOException("Original Edit failure probe changed source rows.");
            Observations.Add(new { Context = context, ProcessId = Environment.ProcessId, NativeEditRefused = true,
                NativeRetry = true, NativeBack = true, OriginalDraftPreserved = true, AcceptedWrites = 0 });
        }
        finally { Factory.RefuseOnce = false; model.Retire(); await LeaveAsync(route); }
    }

    /// <summary>Gets only original detail captions required by this changed read/publication scope.</summary>
    internal static HashSet<string> GoalDetailCaptions(GoalDetailViewModel model) =>
        new[] { model.Summary?.Name, model.Summary?.FundedText, model.Summary?.RemainingText,
            model.Summary?.PlanText, model.Summary?.WarningText, model.TrendText, model.CapacityQuantityText,
            model.CoverageText, model.AssumedPriceLabel }.Where(text => !string.IsNullOrEmpty(text)).Cast<string>().ToHashSet(StringComparer.Ordinal);
}
#endif
