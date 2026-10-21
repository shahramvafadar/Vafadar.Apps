using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization;
using Vafadar.Zanance.App.Diagnostics;
using Vafadar.Zanance.App.Features.Home;
using Vafadar.Zanance.App.Presentation;

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    /// <summary>Qualifies original physical quantities and plain ratios on actual independent Release/AOT controls, preserving base data and OS settings.</summary>
    private static async Task<object> ReviewPhysicalRatesAsync(App app, IServiceProvider services, string folder, string comparison, CancellationToken ct)
    {
        async Task Focused()
        {
            while ((Shell.Current?.CurrentPage ?? app.Windows[0].Page)?.Handler?.PlatformView is not Android.Views.View
                { IsShown: true, IsAttachedToWindow: true, HasWindowFocus: true } view || view.Context?.PackageName != Package) await Task.Delay(100, ct);
        }
        if (AppInfo.Current.PackageName != "pro.vafadar.zanance.androidhomereview" || services.GetRequiredService<Vafadar.Zanance.App.Security.AppLockService>().IsLocked)
            throw new ReviewCheckException("Unlocked original independent numeric caller required.");
        var recover = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.Intent?.GetBooleanExtra("review_physical_rate_receipt", false) == true;
        var receipt = recover ? await RecoverPhysicalRateReceiptAsync(services, folder, comparison, ct) : null;
        // D-437: independent root replacement belongs to the actual Activity, rather than an old cached Home view.
        // Require the actual owned Activity window first; every later form/action still requires its own focused native view.
        while (Microsoft.Maui.ApplicationModel.Platform.CurrentActivity is not { PackageName: Package, Window.DecorView: { IsShown: true, IsAttachedToWindow: true, HasWindowFocus: true } })
            await Task.Delay(100, ct);
        LogReport("physical-rates-root-focus", comparison, new { ProcessId = Environment.ProcessId, ActualOwnedWindowFocused = true });
        app.OpenReviewShell();
        while (Shell.Current.CurrentPage is not HomePage { IsLoaded: true }) await Task.Delay(50, ct);
        LogReport("physical-rates-home-attached", comparison, new { ProcessId = Environment.ProcessId, ActualHomeLoaded = true });
        await Focused(); await ((HomePage)Shell.Current.CurrentPage).PendingPublication.WaitAsync(ct);
        var baseline = await ImportFinancialNativeReview.StoredAsync(services, ct);
        var local = services.GetRequiredService<ILocalizationService>(); var theme = services.GetRequiredService<ThemeService>();
        var language = local.CurrentLanguage; var choice = theme.Choice;
        var resume = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.Intent?.GetBooleanExtra("review_plan_resume_actions", false) == true;
        var guideOnly = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.Intent?.GetBooleanExtra("review_physical_rate_guide", false) == true;
        var outcomesOnly = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.Intent?.GetBooleanExtra("review_physical_rate_outcomes", false) == true;
        using var original = outcomesOnly || guideOnly ? await PlanEditorNativeReview.ContinuePhysicalRateOutcomesAsync(services, Path.Combine(folder, "original-physical-rates"), ct) : resume ? await PlanEditorNativeReview.ContinuePhysicalRatesAsync(services, Path.Combine(folder, "original-physical-rates"), ct)
            : await PlanEditorNativeReview.CreateAsync(services, Path.Combine(folder, "original-physical-rates"), ct);
        if (!resume) await original.PreparePhysicalRatesAsync(ct);
        bool Completed(string stage) => File.Exists(Path.Combine(folder, stage + ".json"));
        static bool Visible(Element item)
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
        async Task Scroll(VisualElement target)
        {
            for (Element? parent = target.Parent; parent is not null; parent = parent.Parent)
                if (parent is ScrollView viewport) { await viewport.ScrollToAsync(target, ScrollToPosition.MakeVisible, false); await Task.Delay(30, ct); return; }
        }
        static IEnumerable<Android.Views.View> NativeNodes(Android.Views.View view)
        {
            yield return view;
            if (view is Android.Views.ViewGroup group)
                for (var i = 0; i < group.ChildCount; i++)
                    if (group.GetChildAt(i) is { } child) foreach (var nested in NativeNodes(child)) yield return nested;
        }
        async Task Invoke(Page page, System.Windows.Input.ICommand command, bool? consent)
        {
            var button = PlanEditorNativeReview.Views(page).OfType<Button>().Single(item => Visible(item) && ReferenceEquals(item.Command, command));
            await Scroll(button);
            await PlanEditorNativeReview.WaitAsync(() => button.Handler?.PlatformView is Android.Widget.Button { IsShown: true, HasWindowFocus: true, Enabled: true }, ct);
            if (!Visible(button) || button.Handler?.PlatformView is not Android.Widget.Button native || !native.PerformClick()) throw new IOException("Actual original money Save unavailable.");
            await Task.Yield();
            if (consent is { } answer)
            {
                if (!OperatingSystem.IsAndroidVersionAtLeast(29)) throw new IOException("Original account dialog inspection requires API 29.");
                var caption = services.GetRequiredService<Translator>()[answer ? "Common_Delete" : "Common_Cancel"];
                Android.Widget.Button? reply = null;
                await PlanEditorNativeReview.WaitAsync(() =>
                {
                    if (!OperatingSystem.IsAndroidVersionAtLeast(29)) return false;
                    reply = Android.Views.Inspectors.WindowInspector.GlobalWindowViews
                        .Where(window => window.Context?.PackageName == Package && window.IsShown && window.HasWindowFocus)
                        .SelectMany(NativeNodes).OfType<Android.Widget.Button>()
                        .SingleOrDefault(button => button.IsShown && button.Enabled && button.Text == caption);
                    return reply is not null;
                }, ct);
                if (reply?.HasWindowFocus != true || !reply.PerformClick()) throw new IOException("Original account confirmation target retired.");
                LogReport("physical-rates-native-consent", comparison, new { Caption = caption, Positive = answer, ProcessId = Environment.ProcessId });
            }
        }
        async Task Observe(Page page, string stage)
        {
            if (resume && Completed(stage)) return;
            await Focused(); await Task.Delay(150, ct); var readings = new List<object>();
            foreach (var label in PlanEditorNativeReview.Views(page).OfType<Label>().Where(Visible).Where(item => item.Text is { } text && PlanEditorNativeReview.PhysicalRatesCaptions(page).Contains(text)))
            {
                await Scroll(label);
                if (label.Handler?.PlatformView is not Android.Widget.TextView { Layout: { } layout } text || Vafadar.Core.Text.Digits.ToAscii(text.Text ?? string.Empty) != Vafadar.Core.Text.Digits.ToAscii(label.Text ?? string.Empty)
                    || layout.LineCount == 0 || Enumerable.Range(0, layout.LineCount).Any(line => layout.GetEllipsisCount(line) != 0))
                {
                    var native = label.Handler?.PlatformView as Android.Widget.TextView;
                    var failure = new { Stage = stage, Label = label.Text, Native = native?.Text, Width = native?.Width, Height = native?.Height,
                        Lines = native?.Layout?.LineCount, Ellipses = native?.Layout is { } current ? Enumerable.Range(0, current.LineCount).Select(current.GetEllipsisCount).ToArray() : [] };
                    await File.WriteAllTextAsync(Path.Combine(folder, stage + "-failed-native.json"), JsonSerializer.Serialize(failure), ct);
                    LogReport("physical-rates-failed-native", comparison, failure);
                    throw new IOException("Original monetary caption is incomplete.");
                }
                // The actual label presenter applies the selected native digit shapes without changing punctuation or money.
                readings.Add(new { label.Text, NativeText = text.Text, text.Width, text.Height, Lines = layout.LineCount });
            }
            var expectedInputs = original.PhysicalRatesInputs(page);
            foreach (var entry in PlanEditorNativeReview.Views(page).OfType<Entry>().Where(Visible))
            {
                if (SemanticProperties.GetDescription(entry) is not { } description || !expectedInputs.TryGetValue(description, out var raw)) continue;
                await Scroll(entry);
                if (entry.Text != raw || entry.Handler?.PlatformView is not Android.Widget.EditText { IsShown: true, HasWindowFocus: true } input || input.Text != raw)
                    throw new IOException("Actual original physical/rate raw input differs.");
                if (raw.Length == 0 && Vafadar.Core.Text.Digits.ToAscii(input.Hint ?? "") != Vafadar.Core.Text.Digits.ToAscii(entry.Placeholder ?? ""))
                    throw new IOException("Actual original numeric placeholder differs.");
                readings.Add(new { entry.Text, entry.Placeholder, NativePlaceholder = input.Hint, input.Width, input.Height });
            }
            var row = new { Stage = stage, ProcessId = Environment.ProcessId, NativeReadings = readings };
            await File.WriteAllTextAsync(Path.Combine(folder, stage + ".json"), JsonSerializer.Serialize(row), ct);
            original.Observations.Add(row); LogReport("physical-rates-observation", comparison, row);
        }
        try
        {

            if (guideOnly)
            {
                await SetTheme(ThemeChoice.Light); await SetLanguageAsync(app, local, "en", ct);
                await original.PhysicalRestoredGuideAsync(Observe, ct);
            }
            else
            {
            if (outcomesOnly) await original.PhysicalRateActionsAsync(Invoke, Observe, ct);
            foreach (var name in new[] { "en", "fa", "de" }) foreach (var value in new[] { ThemeChoice.Light, ThemeChoice.Dark })
            {
                await SetTheme(value); await SetLanguageAsync(app, local, name, ct);
                if (outcomesOnly) await original.PhysicalRateFeedbackAsync(name + "-" + value, Observe, ct, Completed);
                else await original.PhysicalRatesInputContextAsync(name + "-" + value, Invoke, Observe, ct, resume ? Completed : null);
            }
            if (outcomesOnly) await original.FinishPhysicalRateActionsAsync(Invoke, Observe, ct);
            }
            if (baseline != await ImportFinancialNativeReview.StoredAsync(services, ct)) throw new IOException("Original base installation rows changed.");

            return new { Comparison = comparison, OriginalBaseStoreUnchanged = true, Scope = guideOnly ? "physical-rate-guide" : outcomesOnly ? "physical-rate-outcomes" : "physical-rate-inputs", OriginalNumericRowsUnchanged = !outcomesOnly,
                NativeTypeSaves = outcomesOnly ? 1 : 0, NativeRateSaves = outcomesOnly ? 1 : 0, NativeConcurrentRateVersion = outcomesOnly ? 1 : 0, NativeRateDeletes = outcomesOnly ? 1 : 0, NativeLedgerEntries = 0,
                NoAcceptedCommandReplayed = true, RecoveredOriginalOutcome = receipt, OriginalPreparedSourceReused = resume, Observations = original.Observations, Device = DeviceEvidence() };
        }
        finally { await SetTheme(choice); await SetLanguageAsync(app, local, language.CultureName, ct); }
    }
}
