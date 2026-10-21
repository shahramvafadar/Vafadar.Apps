using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization;
using Vafadar.Zanance.App.Diagnostics;
using Vafadar.Zanance.App.Features.Home;
using Vafadar.Zanance.App.Features.Plans;
using Vafadar.Zanance.App.Presentation;

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    /// <summary>Checks actual original plan/account actions and complete native form publication in independent Release/AOT.</summary>
    private static async Task<object> ReviewPlanEditorAsync(App app, IServiceProvider services, string folder, string comparison, CancellationToken ct)
    {
        if (Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.Intent?.GetBooleanExtra("review_plan_money_debt_receipt", false) == true)
            return await PlanEditorNativeReview.VerifyDebtMoneyReceiptAsync(services, folder, ct);
        async Task AwaitOriginalPlanWindowAsync()
        {
            while (true)
            {
                var current = Shell.Current?.CurrentPage ?? app.Windows[0].Page;
                if (current?.Handler?.PlatformView is Android.Views.View { IsShown: true, IsAttachedToWindow: true, HasWindowFocus: true } native
                    && native.Context?.PackageName == Package) return;
                await Task.Delay(100, ct);
            }
        }
        // A retained Activity reference can lag its focused native page after a system ANR dialog.
        // Require the actual owned view's focus; never treat an obscured page as rendered acceptance.
        await AwaitOriginalPlanWindowAsync(); app.OpenReviewShell();
        while (Shell.Current.CurrentPage is not HomePage { IsLoaded: true }) await Task.Delay(50, ct);
        await AwaitOriginalPlanWindowAsync();
        await ((HomePage)Shell.Current.CurrentPage).PendingPublication.WaitAsync(ct);
        var baseline = await ImportFinancialNativeReview.StoredAsync(services, ct);
        var moneyOnly = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.Intent?.GetBooleanExtra("review_plan_money", false) == true;
        var debtOnly = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.Intent?.GetBooleanExtra("review_plan_money_debt_only", false) == true;
        var submissionOnly = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.Intent?.GetBooleanExtra("review_plan_money_submission_only", false) == true;
        var resumeEmpty = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.Intent?.GetBooleanExtra("review_plan_resume_empty", false) == true;
        var resumeActions = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.Intent?.GetBooleanExtra("review_plan_resume_actions", false) == true;
        using var original = resumeActions && moneyOnly ? await PlanEditorNativeReview.ContinueMoneyAsync(services, Path.Combine(folder, "original-plan-form"), ct) : resumeActions ? await PlanEditorNativeReview.ContinueAsync(services, Path.Combine(folder, "original-plan-form"), ct) : resumeEmpty
            ? await PlanEditorNativeReview.ContinueEmptyAsync(services, Path.Combine(folder, "original-plan-form"), ct)
            : await PlanEditorNativeReview.CreateAsync(services, Path.Combine(folder, "original-plan-form"), ct);
        var local = services.GetRequiredService<ILocalizationService>(); var theme = services.GetRequiredService<ThemeService>();
        var language = local.CurrentLanguage; var choice = theme.Choice;
        bool Visible(Element item)
        {
            for (Element? parent = item; parent is not null; parent = parent.Parent)
                if (parent is VisualElement visual && !visual.IsVisible) return false;
            return true;
        }
        async Task SetTheme(ThemeChoice value)
        {
            if (theme.Choice == value) return;
            var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            void Rebuilt(object? sender, EventArgs args) => ready.TrySetResult();
            app.ShellRebuilt += Rebuilt;
            try { theme.Set(value); await ready.Task.WaitAsync(TimeSpan.FromSeconds(30), ct); }
            finally { app.ShellRebuilt -= Rebuilt; }
        }
        async Task Invoke(Page page, System.Windows.Input.ICommand command)
        {
            var button = PlanEditorNativeReview.Views(page).OfType<Button>().Single(b => ReferenceEquals(b.Command, command));
            for (Element? parent = button.Parent; parent is not null; parent = parent.Parent)
                if (parent is ScrollView viewport) { await viewport.ScrollToAsync(button, ScrollToPosition.MakeVisible, false); break; }
            try
            {
                await PlanEditorNativeReview.WaitAsync(() => button.Handler?.PlatformView is Android.Widget.Button
                    { IsShown: true, HasWindowFocus: true, Enabled: true }, ct);
            }
            catch
            {
                var target = button.Handler?.PlatformView as Android.Widget.Button;
                LogReport("plan-form-action-refusal", comparison, new { Stage = "native-action-refusal", button.Text,
                    SemanticName = SemanticProperties.GetDescription(button), Page = Shell.Current.CurrentPage?.GetType().Name,
                    NativeType = button.Handler?.PlatformView?.GetType().Name, target?.IsShown, target?.HasWindowFocus, target?.Enabled,
                    CommandAllowed = command.CanExecute(null), ButtonCommandIsOriginal = ReferenceEquals(button.Command, command),
                    FormVisible = (page.BindingContext as PlanEditorViewModel)?.FormVisible,
                    CanInteract = (page.BindingContext as PlanEditorViewModel)?.CanInteract });
                throw;
            }
            var native = (Android.Widget.Button)button.Handler!.PlatformView!;
            if (!Visible(button) || native.Context?.PackageName != Package
                || native.Height / (double)(native.Resources?.DisplayMetrics?.Density ?? 1) < 43.5 || !native.PerformClick())
                throw new ReviewCheckException("Original plan native action refused.");
            await Task.Yield();
        }
        async Task Observe(Page page, string stage)
        {
            var retainedObservation = Path.Combine(folder, stage + ".json");
            if (moneyOnly && File.Exists(retainedObservation))
            {
                using var receipt = JsonDocument.Parse(await File.ReadAllTextAsync(retainedObservation, ct));
                if (receipt.RootElement.GetProperty("NativeReadings").EnumerateArray().Any(row => row.TryGetProperty("Text", out var text) && text.GetString() == "1.234")) return;
                if (!File.Exists(retainedObservation + ".incomplete-original")) File.Copy(retainedObservation, retainedObservation + ".incomplete-original");
            }
            // A system ANR dialog is not a raw-field mismatch. Retain this exact page/process until its own window returns.
            while (page.Handler?.PlatformView is not Android.Views.View { IsShown: true, HasWindowFocus: true })
            {
                if (!ReferenceEquals(Shell.Current.CurrentPage, page)) throw new ReviewCheckException("Original plan observation owner changed.");
                await Task.Delay(100, ct);
            }
            await Task.Delay(150, ct); var readings = new List<object>();
            if (page.BindingContext is PlanEditorViewModel model)
            {
                var viewport = page.FindByName<ScrollView>("ValidationViewport");
                if (viewport.IsVisible != model.HasFormContent || !model.Loading.IsReady && (model.FormVisible || model.CanSave))
                    throw new ReviewCheckException("Original native plan cover differs.");
                foreach (var input in PlanEditorNativeReview.Views(page).OfType<InputView>().Where(Visible))
                {
                    await viewport.ScrollToAsync(input, ScrollToPosition.MakeVisible, false); await Task.Delay(30, ct);
                    while (input.Handler?.PlatformView is Android.Widget.TextView { HasWindowFocus: false }) await Task.Delay(100, ct);
                    if (input.Handler?.PlatformView is not Android.Widget.TextView { IsShown: true, HasWindowFocus: true } native
                        || (native.Text ?? string.Empty) != (input.Text ?? string.Empty))
                    {
                        LogReport("plan-form-field-refusal", comparison, new { Stage = "native-field-refusal", input.Text,
                            NativeType = input.Handler?.PlatformView?.GetType().Name,
                            NativeText = (input.Handler?.PlatformView as Android.Widget.TextView)?.Text,
                            Shown = (input.Handler?.PlatformView as Android.Widget.TextView)?.IsShown,
                            Focus = (input.Handler?.PlatformView as Android.Widget.TextView)?.HasWindowFocus, model.FormVisible, model.Loading.IsReady });
                        throw new ReviewCheckException("Original native plan raw field differs.");
                    }
                    readings.Add(new { input.Text, native.Width, native.Height, native.HasWindowFocus });
                }
                if (model.HasFormContent)
                {
                    var picker = page.FindByName<Picker>("AccountInput");
                    await viewport.ScrollToAsync(picker, ScrollToPosition.MakeVisible, false); await Task.Delay(30, ct);
                    if (picker.Handler?.PlatformView is not Android.Widget.TextView { IsShown: true, HasWindowFocus: true } selected
                        || selected.Text != picker.SelectedItem?.ToString()) throw new ReviewCheckException("Original native plan account selection differs.");
                    readings.Add(new { AccountId = model.Account?.Id, Text = selected.Text, selected.Width, selected.Height });
                }
                foreach (var label in PlanEditorNativeReview.Views(page).OfType<Label>().Where(Visible)
                    .Where(l => !string.IsNullOrEmpty(l.Text) && (l.Text == model.LoadError || l.Text == model.SaveError || l.Text == model.WorkText
                        || moneyOnly && (l.Text == model.AmountError || l.Text == model.ToAmountError || l.Text == model.AmountFormatNote || l.Text == model.DestinationAmountFormatNote || l.Text == model.AmountUnitName || l.Text == model.DestinationAmountUnitName)
                        || l.Text == original.Translator["Plan_FormLoading"] || l.Text == original.Translator["Editor_RetryRead"])))
                {
                    if (label.Handler?.PlatformView is not Android.Widget.TextView { Layout: { } layout } text || text.LineCount == 0
                        || Enumerable.Range(0, layout.LineCount).Any(line => layout.GetEllipsisCount(line) != 0))
                        throw new ReviewCheckException("Original native plan feedback was clipped.");
                    readings.Add(new { label.Text, text.Width, text.Height, Lines = layout.LineCount });
                }
                // A hidden Android viewport has no scroll completion callback; it is not an observable surface.
                if (viewport.IsVisible) await viewport.ScrollToAsync(0, 0, false);
            }
            var observation = new { Stage = stage, NativeReadings = readings, ProcessId = Environment.ProcessId };
            await File.WriteAllTextAsync(Path.Combine(folder, stage + ".json"), JsonSerializer.Serialize(observation), ct);
            original.Observations.Add(observation); LogReport("plan-form-observation", comparison, observation);
        }
        try
        {
            if (moneyOnly && !resumeActions) await original.PrepareMoneyAsync(ct);
            else if (!resumeActions) await original.RunActionsAsync(Invoke, Observe, ct, skipNoAccountObservation: resumeEmpty && File.Exists(Path.Combine(folder, "native-no-account.json")));
            if (!debtOnly && !submissionOnly) foreach (var name in new[] { "en", "fa", "de" })
            foreach (var value in new[] { ThemeChoice.Light, ThemeChoice.Dark })
            {
                await SetTheme(value); await SetLanguageAsync(app, local, name, ct);
                var context = name + "-" + value;
                if (moneyOnly) { await original.MoneyContextAsync(context, Invoke, Observe, !File.Exists(Path.Combine(folder, "original-plan-form", "money-save-verified.json")), ct); continue; }
                if (resumeActions && File.Exists(Path.Combine(folder, context + "-missing.json"))) continue;
                await original.ContextAsync(context, Invoke, Observe, ct,
                    resumeReady: resumeActions && File.Exists(Path.Combine(folder, context + "-ready.json")),
                    resumeReviewed: resumeActions && File.Exists(Path.Combine(folder, context + "-work-reviewed.json")),
                    resumeWorkFailed: resumeActions && File.Exists(Path.Combine(folder, context + "-work-failed.json")));
            }
            if (submissionOnly)
            {
                await SetTheme(ThemeChoice.Light); await SetLanguageAsync(app, local, "en", ct);
                await original.SubmissionMoneyAsync(Invoke, Observe, ct);
            }
            if (debtOnly)
            {
                await SetTheme(ThemeChoice.Light); await SetLanguageAsync(app, local, "en", ct);
                using var debt = await PlanEditorNativeReview.CreateAsync(services, Path.Combine(folder, "money-debt-fixture"), ct);
                await debt.PrepareMoneyAsync(ct); await debt.DebtMoneyAsync(Observe, ct);
            }
            if (!moneyOnly) await original.ArchivedReferenceAsync(Observe, ct);
            if (baseline != await ImportFinancialNativeReview.StoredAsync(services, ct)) throw new ReviewCheckException("Original base store changed.");
            return new { OriginalBaseStoreUnchanged = true, NoAcceptedCommandReplayed = true, OriginalEmptyPreparedContinuation = resumeEmpty, OriginalAcceptedActionsReused = resumeActions,
                NativeAccountSaves = moneyOnly ? 0 : 1, NativePlanSaves = debtOnly || submissionOnly ? 0 : moneyOnly ? 1 : 2, NativeLedgerPosts = 0, Observations = original.Observations, Device = DeviceEvidence() };
        }
        finally
        {
            await original.LeaveAsync(); await SetTheme(choice); await SetLanguageAsync(app, local, language.CultureName, ct);
        }
    }
}
