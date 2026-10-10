#if DEBUG && WINDOWS
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization;
using Vafadar.Zanance.App.Features.Plans;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

/// <summary>Own-window occurrence checks over the walk-through's fictitious database only.</summary>
internal static partial class DebugSnapshots
{
    // AT-108: native actions must retain their complete text, expose the correct input and preserve ledger invariants.
    private static async Task ReviewOccurrenceFeedbackAsync(App app, IServiceProvider services, string folder, string language)
    {
        OccurrencePage Page() => Shell.Current.CurrentPage as OccurrencePage
            ?? throw new InvalidOperationException("The actual occurrence page is missing.");
        var page = Page();
        var vm = (OccurrenceViewModel)page.BindingContext;
        var store = services.GetRequiredService<ZananceStore>();
        var plans = services.GetRequiredService<PlanStore>();
        var factory = services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        var culture = services.GetRequiredService<ILocalizationService>().CurrentCulture;
        var plan = (await plans.GetSchedulesAsync()).Single(item => item.Name == vm.Name);
        var originalDate = vm.DueDate;
        var originalDraft = (vm.ActualAmountText, vm.ActualDate, vm.DueDate, vm.OverrideAmountText, vm.OccurrenceNote, vm.ShowChange);
        var originalEntries = await store.GetEntriesAsync();
        var originalIds = originalEntries.Select(item => item.Id).ToHashSet();
        if ((await plans.GetStatesAsync(plan.Id)).Any(item => item.OriginalDate == originalDate) || !vm.IsOpen)
        { throw new InvalidOperationException("Occurrence review requires the unchanged open fictitious occurrence."); }
        async Task<string> StoredAsync() => System.Text.Json.JsonSerializer.Serialize(new
        {
            Entries = await store.GetEntriesAsync(), Accounts = await store.GetAccountsAsync(),
            Settings = await store.GetSettingsAsync(), Schedules = await plans.GetSchedulesAsync(), States = await plans.GetStatesAsync(),
        });
        var before = await StoredAsync();
        var invalid = 0;
        var valid = 0;
        var checkedActions = new HashSet<string>();
        static void CheckGlyphs(Label label)
        {
            if (label.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.TextBlock native || native.IsTextTrimmed
                || !native.IsTextScaleFactorEnabled || native.ActualHeight < 1 || native.ActualWidth < 1)
            { throw new InvalidOperationException("An occurrence caption lacks complete scaled native text."); }
            var boxes = new List<Windows.Foundation.Rect>();
            for (var offset = 0; offset <= native.ContentEnd.Offset - native.ContentStart.Offset; offset++)
            {
                if (native.ContentStart.GetPositionAtOffset(offset, Microsoft.UI.Xaml.Documents.LogicalDirection.Forward) is { } pointer)
                { boxes.Add(pointer.GetCharacterRect(Microsoft.UI.Xaml.Documents.LogicalDirection.Forward)); }
            }
            if (boxes.Count == 0 || boxes.Min(box => box.Left) < -1 || boxes.Max(box => box.Right) > label.Width + 1
                || boxes.Max(box => box.Bottom) > label.Height + 1)
            { throw new InvalidOperationException("Occurrence caption glyphs exceed the actual MAUI allocation."); }
        }
        async Task CheckActionAsync(string name)
        {
            var action = page.FindByName<Presentation.WrappingAction>(name);
            await ScrollToViewIfNeededAsync(page.FindByName<ScrollView>("ValidationViewport"), action); await Task.Delay(150);
            CheckGlyphs(VisualDescendants(action).OfType<Label>().Single());
            var button = VisualDescendants(action).OfType<Button>().Single();
            if (button.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button native || native.ActualHeight < 44
                || native.ActualWidth < 44 || !native.IsEnabled
                || Microsoft.UI.Xaml.Automation.AutomationProperties.GetName(native) != action.Text)
            { throw new InvalidOperationException("An occurrence action lacks its complete native name or 44 px target."); }
            checkedActions.Add(name);
            await CaptureAsync(app, folder, language + "-occurrence-feedback-" + name);
        }
        async Task InvokeAsync(string name)
        {
            await CheckActionAsync(name);
            var command = page.FindByName<Presentation.WrappingAction>(name).Command!;
            InvokeWrappingAction(page, command);
            if (command is CommunityToolkit.Mvvm.Input.IAsyncRelayCommand asyncCommand && asyncCommand.ExecutionTask is { } execution)
            { await execution; }
            await Task.Delay(350);
        }
        async Task InvalidAsync(string actionName, OccurrenceInput input, string scenario)
        {
            var retained = (vm.ActualAmountText, vm.ActualDate, vm.DueDate, vm.OverrideAmountText, vm.OccurrenceNote, vm.ShowChange);
            await InvokeAsync(actionName); invalid++;
            if (retained != (vm.ActualAmountText, vm.ActualDate, vm.DueDate, vm.OverrideAmountText, vm.OccurrenceNote, vm.ShowChange)
                || before != await StoredAsync() || vm.Error is not null)
            { throw new InvalidOperationException("An invalid occurrence action changed its draft or stored data."); }
            var payment = input == OccurrenceInput.Payment;
            var expected = Translator.Instance[payment ? "Plan_AmountMustBePositive" : "Amount_Invalid"];
            if ((payment ? vm.PaymentAmountError : vm.OverrideAmountError) != expected)
            { throw new InvalidOperationException("The attempted occurrence action did not explain its own field."); }
            var viewport = page.FindByName<ScrollView>("ValidationViewport");
            var target = page.FindByName<Entry>(payment ? "PaymentInput" : "OverrideInput");
            if (viewport.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.ScrollViewer nativeViewport
                || target.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.TextBox nativeTarget || nativeTarget.Text != target.Text)
            { throw new InvalidOperationException("The actual occurrence input is missing or changed."); }
            var position = nativeTarget.TransformToVisual(nativeViewport).TransformPoint(new Windows.Foundation.Point());
            if (position.Y < -1 || position.Y + nativeTarget.ActualHeight > nativeViewport.ActualHeight + 1)
            { throw new InvalidOperationException("The affected occurrence input remained outside the viewport."); }
            await CaptureAsync(app, folder, language + "-occurrence-feedback-" + scenario + "-input");
            var error = page.FindByName<Label>(payment ? "PaymentErrorLabel" : "OverrideErrorLabel");
            await ScrollToViewIfNeededAsync(viewport, error); await Task.Delay(100); CheckGlyphs(error);
            await CaptureAsync(app, folder, language + "-occurrence-feedback-" + scenario + "-error");
        }
        async Task RestoreFixtureAsync()
        {
            // Delete only this exact new fictitious occurrence state and its identified scenario payment rows.
            var createdIds = (await store.GetEntriesAsync()).Where(item => !originalIds.Contains(item.Id)
                && item.ScheduleId == plan.Id && item.OccurrenceDate == originalDate).Select(item => item.Id).ToList();
            await using var db = await factory.CreateDbContextAsync();
            db.OccurrenceStates.RemoveRange(await db.OccurrenceStates.Where(item => item.ScheduleId == plan.Id && item.OriginalDate == originalDate).ToListAsync());
            db.Entries.RemoveRange(await db.Entries.Where(item => createdIds.Contains(item.Id)).ToListAsync());
            await db.SaveChangesAsync();
            if (before != await StoredAsync()) { throw new InvalidOperationException("The occurrence fixture did not restore its exact original stored values."); }
        }
        async Task CheckOriginalRowsAsync()
        {
            var current = (await store.GetEntriesAsync()).Where(item => originalIds.Contains(item.Id));
            if (System.Text.Json.JsonSerializer.Serialize(current) != System.Text.Json.JsonSerializer.Serialize(originalEntries))
            { throw new InvalidOperationException("Occurrence actions changed original financial fields or ids."); }
        }
        try
        {
            await CheckActionAsync("PlanAction"); await CheckActionAsync("SkipAction");
            vm.ActualAmountText = string.Empty;
            await InvalidAsync("ConfirmAction", OccurrenceInput.Payment, "missing-payment");
            vm.ActualAmountText = "-1";
            await InvalidAsync("PayPartAction", OccurrenceInput.Payment, "negative-payment");
            await InvokeAsync("ChangeAction");
            if (!vm.ShowChange) { throw new InvalidOperationException("Native occurrence Edit did not expose its draft."); }
            vm.OverrideAmountText = "0";
            await InvalidAsync("SaveChangeAction", OccurrenceInput.Override, "zero-override");
            vm.OverrideAmountText = "not money";
            await InvalidAsync("SaveChangeAction", OccurrenceInput.Override, "invalid-override");
            vm.OverrideAmountText = string.Empty;
            if (vm.OverrideAmountError is not null || vm.PaymentAmountError is null)
            { throw new InvalidOperationException("Correcting an override cleared the wrong field error."); }
            vm.ActualAmountText = Core.Money.MoneyText.ForInput(1200, "EUR", culture);
            if (vm.PaymentAmountError is not null) { throw new InvalidOperationException("Correcting payment retained its old error."); }
            // A follow-up wording change rechecks every affected layout and invalid action; retain prior financial evidence separately.
            if (Environment.GetEnvironmentVariable("VAFADAR_SNAPSHOT_OCCURRENCE_MESSAGES_ONLY") == "1")
            {
                if (before != await StoredAsync()) { throw new InvalidOperationException("Message review changed stored occurrence data."); }
                File.WriteAllText(Path.Combine(folder, language + "-occurrence-feedback-message-proof.json"), System.Text.Json.JsonSerializer.Serialize(new
                {
                    NativeInvalidSaveInvocations = invalid, NativeValidSaveInvocations = 0,
                    PaymentErrorKey = "Plan_AmountMustBePositive", CorrectFieldErrorsAndIndependentCorrections = true,
                    FirstAffectedInputRevealed = true, FullNativeScaledErrorAndActionGlyphs = true,
                    NativeActions = checkedActions.Order().ToList(), CompleteOriginalStoredValuesRetained = true,
                    MessageReviewOnly = true,
                }));
                return;
            }
            foreach (var amount in new long?[] { 1234, null })
            {
                vm.ShowChange = true; vm.OverrideAmountText = amount is { } entered ? Core.Money.MoneyText.ForInput(entered, "EUR", culture) : string.Empty;
                vm.DueDate = amount is null ? originalDate : originalDate.AddDays(2); vm.OccurrenceNote = "Fictitious occurrence change";
                await InvokeAsync("SaveChangeAction"); valid++;
                var state = (await plans.GetStatesAsync(plan.Id)).Single(item => item.OriginalDate == originalDate);
                if (state.Amount != amount || state.DueDate != (amount is null ? null : originalDate.AddDays(2))
                    || state.Note != "Fictitious occurrence change" || vm.ShowChange || vm.IsBusy || vm.Error is not null)
                { throw new InvalidOperationException("A native occurrence change did not preserve the original override semantics."); }
                await CheckOriginalRowsAsync();
                if ((await store.GetEntriesAsync()).Count != originalEntries.Count)
                { throw new InvalidOperationException("Changing occurrence metadata posted a ledger entry."); }
                await RestoreFixtureAsync(); await vm.LoadAsync();
            }
            vm.ActualAmountText = Core.Money.MoneyText.ForInput(2500, "EUR", culture);
            await InvokeAsync("PayPartAction"); valid++;
            var partial = (await store.GetEntriesAsync()).Single(item => !originalIds.Contains(item.Id));
            if (!partial.IsPartialPayment || partial.Amount != 2500 || partial.ScheduleId != plan.Id || partial.OccurrenceDate != originalDate
                || partial.Kind != EntryKind.Expense || !vm.IsOpen || !vm.HasPayments)
            { throw new InvalidOperationException("The actual partial-payment action changed its ledger/open-occurrence semantics."); }
            await CheckOriginalRowsAsync();
            vm.ActualAmountText = Core.Money.MoneyText.ForInput(plan.Amount!.Value - 2500, "EUR", culture);
            await InvokeAsync("ConfirmAction"); valid++;
            await Shell.Current.GoToAsync(AppShell.OccurrenceRoute, new Dictionary<string, object> { ["plan"] = plan.Id, ["date"] = originalDate });
            await Task.Delay(500); page = Page(); vm = (OccurrenceViewModel)page.BindingContext;
            var created = (await store.GetEntriesAsync()).Where(item => !originalIds.Contains(item.Id)).ToList();
            var settled = (await plans.GetStatesAsync(plan.Id)).Single(item => item.OriginalDate == originalDate);
            if (created.Count != 2 || created.Sum(item => item.Amount) != plan.Amount || !vm.IsSettled
                || settled.Status != OccurrenceStatus.Settled || created.Count(item => item.IsPartialPayment) != 1
                || created.Any(item => item.Kind != EntryKind.Expense || item.AccountId != plan.AccountId
                    || item.ScheduleId != plan.Id || item.OccurrenceDate != originalDate || item.Source != EntrySource.Schedule)
                || created.Single(item => !item.IsPartialPayment).Id != settled.EntryId)
            { throw new InvalidOperationException("Partial and completion actions did not retain the intended payments and unique settlement."); }
            await CheckOriginalRowsAsync(); await CheckActionAsync("UndoAction");
            await RestoreFixtureAsync(); await vm.LoadAsync();
        }
        finally
        {
            await RestoreFixtureAsync();
            (vm.ActualAmountText, vm.ActualDate, vm.DueDate, vm.OverrideAmountText, vm.OccurrenceNote, vm.ShowChange) = originalDraft;
        }
        File.WriteAllText(Path.Combine(folder, language + "-occurrence-feedback-proof.json"), System.Text.Json.JsonSerializer.Serialize(new
        {
            NativeInvalidSaveInvocations = invalid, NativeValidSaveInvocations = valid,
            CorrectFieldErrorsAndIndependentCorrections = true, FirstAffectedInputRevealed = true,
            FullNativeScaledErrorAndActionGlyphs = true, NativeActions = checkedActions.Order().ToList(),
            MetadataChangesNeverPostEntries = true, ActualPartialAndUniqueCompletionRetained = true,
            OriginalFinancialFieldsAndIdsRetained = true, FictitiousDatabaseRestoredBetweenScenarios = true,
        }));
    }
}
#endif
