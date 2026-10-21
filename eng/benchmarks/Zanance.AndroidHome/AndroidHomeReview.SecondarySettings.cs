using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization;
using Vafadar.Zanance.App.Diagnostics;
using Vafadar.Zanance.App.Features.Home;
using Vafadar.Zanance.App.Presentation;

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    /// <summary>Qualifies original display-unit definitions on actual independent Release/AOT controls, preserving base data and OS settings.</summary>
    private static async Task<object> ReviewSecondarySettingsAsync(App app, IServiceProvider services, string folder, string comparison, CancellationToken ct)
    {
        async Task Focused()
        {
            while ((Shell.Current?.CurrentPage ?? app.Windows[0].Page)?.Handler?.PlatformView is not Android.Views.View
                { IsShown: true, IsAttachedToWindow: true, HasWindowFocus: true } view || view.Context?.PackageName != Package) await Task.Delay(100, ct);
        }
        await AwaitOriginalMoneyWindowAsync(ct); app.OpenReviewShell();
        while (Shell.Current.CurrentPage is not HomePage { IsLoaded: true }) await Task.Delay(50, ct);
        await Focused(); await ((HomePage)Shell.Current.CurrentPage).PendingPublication.WaitAsync(ct);
        var baseline = await ImportFinancialNativeReview.StoredAsync(services, ct);
        var local = services.GetRequiredService<ILocalizationService>(); var theme = services.GetRequiredService<ThemeService>();
        var language = local.CurrentLanguage; var choice = theme.Choice;
        var resume = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.Intent?.GetBooleanExtra("review_plan_resume_actions", false) == true;
        var osReview = MonetaryOsScope.Requested();
        osReview?.RequireCompleted("secondary-settings", Path.Combine(folder, "original-secondary-settings"));
        using var original = resume ? await PlanEditorNativeReview.ContinueSecondarySettingsAsync(services, Path.Combine(folder, "original-secondary-settings"), ct)
            : await PlanEditorNativeReview.CreateAsync(services, Path.Combine(folder, "original-secondary-settings"), ct);
        if (!resume) await original.PrepareSecondarySettingsAsync(ct);
        var osSource = osReview is null ? null : await original.AllMoneyRowsAsync(ct);
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
            osReview?.RequireReadOnly(page, command, original.Factory.RefuseOnce);
            var button = PlanEditorNativeReview.Views(page).OfType<Button>().Single(item => Visible(item) && ReferenceEquals(item.Command, command));
            await Scroll(button);
            await PlanEditorNativeReview.WaitAsync(() => button.Handler?.PlatformView is Android.Widget.Button { IsShown: true, HasWindowFocus: true, Enabled: true }, ct);
            if (!Visible(button) || button.Handler?.PlatformView is not Android.Widget.Button native || !native.PerformClick()) throw new IOException("Actual original money Save unavailable.");
            await Task.Yield();
            if (consent is { } answer)
            {
                if (!OperatingSystem.IsAndroidVersionAtLeast(29)) throw new IOException("Original account dialog inspection requires API 29.");
                var caption = services.GetRequiredService<Translator>()[answer ? "Reconcile_Adjust" : "Common_Cancel"];
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
                LogReport("secondary-settings-native-consent", comparison, new { Caption = caption, Positive = answer, ProcessId = Environment.ProcessId });
            }
        }
        async Task Observe(Page page, string stage)
        {
            if (resume && Completed(stage)) return;
            if (stage.EndsWith("-forecast-accept", StringComparison.Ordinal) || stage.EndsWith("-forecast-cancel", StringComparison.Ordinal))
            {
                if (!OperatingSystem.IsAndroidVersionAtLeast(29)) throw new IOException("Original prompt inspection requires API 29.");
                var translator = services.GetRequiredService<Translator>();
                var caption = translator.Format("Forecast_AssumeAmountMessage", "EUR");
                Android.Views.View? dialog = null;
                await PlanEditorNativeReview.WaitAsync(() =>
                {
                    if (!OperatingSystem.IsAndroidVersionAtLeast(29)) return false;
                    dialog = Android.Views.Inspectors.WindowInspector.GlobalWindowViews.Where(window => window.Context?.PackageName == Package && window.IsShown && window.HasWindowFocus)
                        .SingleOrDefault(window => NativeNodes(window).OfType<Android.Widget.EditText>().Any(input => input.Text == "12.34"));
                    return dialog is not null;
                }, ct);
                var input = NativeNodes(dialog!).OfType<Android.Widget.EditText>().Single();
                if (!NativeNodes(dialog!).OfType<Android.Widget.TextView>().Any(text => text.Text == caption)) throw new IOException("Original forecast prompt caption changed.");
                var promptReadings = new List<object>();
                if (osReview is not null)
                {
                    foreach (var text in NativeNodes(dialog!).OfType<Android.Widget.TextView>().Where(text => text.IsShown && !string.IsNullOrEmpty(text.Text)))
                    {
                        await PlanEditorNativeReview.WaitAsync(() => text.Layout is { LineCount: > 0 }, ct);
                        var layout = text.Layout!;
                        if (Enumerable.Range(0, layout.LineCount).Any(line => layout.GetEllipsisCount(line) != 0))
                            throw new ReviewCheckException("Original OS-scaled forecast prompt text is ellipsized.");
                        MonetaryOsScope.RequireCompleteHeight(text, layout);
                        if (text is Android.Widget.Button answerTarget)
                        {
                            using var visible = new Android.Graphics.Rect();
                            var density = text.Resources?.DisplayMetrics?.Density ?? 0;
                            if (density <= 0 || answerTarget.Width / density < 44 || answerTarget.Height / density < 44
                                || !answerTarget.GetGlobalVisibleRect(visible) || visible.Width() != answerTarget.Width || visible.Height() != answerTarget.Height)
                                throw new ReviewCheckException("Original OS-scaled forecast answer target is incomplete.");
                        }
                        promptReadings.Add(new { NativeText = text.Text, text.Width, text.Height, Lines = layout.LineCount, Button = text is Android.Widget.Button });
                    }
                }
                var promptObservation = new { Stage = stage, OriginalUnit = "EUR", OriginalInput = input.Text, NativeCaption = caption,
                    NativeReadings = promptReadings, ProcessId = Environment.ProcessId };
                await File.WriteAllTextAsync(Path.Combine(folder, stage + ".json"), JsonSerializer.Serialize(promptObservation), ct);
                original.Observations.Add(promptObservation);
                var answer = translator[stage.EndsWith("-forecast-accept", StringComparison.Ordinal) ? "Common_Ok" : "Common_Cancel"];
                var button = NativeNodes(dialog!).OfType<Android.Widget.Button>().Single(value => value.Text == answer && value.IsShown && value.Enabled);
                if (!button.HasWindowFocus || !button.PerformClick()) throw new IOException("Original forecast prompt answer retired.");
                await Task.Yield(); return;
            }
            await Focused(); await Task.Delay(150, ct); var readings = new List<object>();
            foreach (var label in PlanEditorNativeReview.Views(page).OfType<Label>().Where(Visible).Where(item => item.Text is { } text && PlanEditorNativeReview.SecondarySettingsCaptions(page).Contains(text)))
            {
                await Scroll(label);
                if (label.Handler?.PlatformView is not Android.Widget.TextView { Layout: { } layout } text || Vafadar.Core.Text.Digits.ToAscii(text.Text ?? string.Empty) != Vafadar.Core.Text.Digits.ToAscii(label.Text ?? string.Empty)
                    || layout.LineCount == 0 || Enumerable.Range(0, layout.LineCount).Any(line => layout.GetEllipsisCount(line) != 0))
                {
                    var native = label.Handler?.PlatformView as Android.Widget.TextView;
                    var failure = new { Stage = stage, Label = label.Text, Native = native?.Text, Width = native?.Width, Height = native?.Height,
                        Lines = native?.Layout?.LineCount, Ellipses = native?.Layout is { } current ? Enumerable.Range(0, current.LineCount).Select(current.GetEllipsisCount).ToArray() : [] };
                    await File.WriteAllTextAsync(Path.Combine(folder, stage + "-failed-native.json"), JsonSerializer.Serialize(failure), ct);
                    LogReport("secondary-settings-failed-native", comparison, failure);
                    throw new IOException("Original monetary caption is incomplete.");
                }
                if (osReview is not null) MonetaryOsScope.RequireCompleteHeight(text, layout);
                // The actual label presenter applies the selected native digit shapes without changing punctuation or money.
                readings.Add(new { label.Text, NativeText = text.Text, text.Width, text.Height, Lines = layout.LineCount });
            }
            foreach (var entry in PlanEditorNativeReview.Views(page).OfType<Entry>().Where(Visible).Where(item => item.Text is { } text && PlanEditorNativeReview.SecondarySettingsRaw(page).Contains(text)))
            {
                await Scroll(entry);
                if (entry.Handler?.PlatformView is not Android.Widget.EditText { IsShown: true, HasWindowFocus: true } input || input.Text != entry.Text)
                {
                    var native = entry.Handler?.PlatformView as Android.Widget.EditText;
                    var failure = new { Stage = stage, Raw = entry.Text, Native = native?.Text, Visible = native?.IsShown, Focused = native?.HasWindowFocus };
                    LogReport("secondary-settings-failed-native", comparison, failure);
                    throw new IOException("Original monetary raw input differs.");
                }
                readings.Add(new { entry.Text, input.Width, input.Height });
            }
            var row = new { Stage = stage, ProcessId = Environment.ProcessId, NativeReadings = readings };
            await File.WriteAllTextAsync(Path.Combine(folder, stage + ".json"), JsonSerializer.Serialize(row), ct);
            original.Observations.Add(row); LogReport("secondary-settings-observation", comparison, row);
        }
        try
        {

            foreach (var name in new[] { "en", "fa", "de" }) foreach (var value in new[] { ThemeChoice.Light, ThemeChoice.Dark })
            {
                await SetTheme(value); await SetLanguageAsync(app, local, name, ct);
                await original.SecondarySettingsContextAsync((osReview?.Prefix ?? "") + name + "-" + value, Invoke, Observe, ct, resume ? Completed : null);
            }
            await original.VerifySecondarySettingsAsync(ct);
            if (osSource is not null && osSource != await original.AllMoneyRowsAsync(ct)) throw new IOException("Original secondary OS scope changed source rows.");
            if (baseline != await ImportFinancialNativeReview.StoredAsync(services, ct)) throw new IOException("Original base installation rows changed.");

            return new { Comparison = comparison, OriginalBaseStoreUnchanged = true, NativeEstimateSaves = 1, NativeLedgerEntries = 0,
                NoAcceptedCommandReplayed = true, OriginalAcceptedActionsReused = resume, Scope = osReview?.Phase,
                OsAcceptedCommands = osReview is null ? (int?)null : 0, OsSourceUnchanged = osReview is not null, Observations = original.Observations, Device = DeviceEvidence() };
        }
        finally { await SetTheme(choice); await SetLanguageAsync(app, local, language.CultureName, ct); }
    }
}
