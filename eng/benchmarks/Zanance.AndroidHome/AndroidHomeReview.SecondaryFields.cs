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
    private static async Task<object> ReviewSecondaryFieldsAsync(App app, IServiceProvider services, string folder, string comparison, CancellationToken ct)
    {
        async Task Focused()
        {
            while ((Shell.Current?.CurrentPage ?? app.Windows[0].Page)?.Handler?.PlatformView is not Android.Views.View
                { IsShown: true, IsAttachedToWindow: true, HasWindowFocus: true } view || view.Context?.PackageName != Package) await Task.Delay(100, ct);
        }
        await Focused(); app.OpenReviewShell();
        while (Shell.Current.CurrentPage is not HomePage { IsLoaded: true }) await Task.Delay(50, ct);
        await Focused(); await ((HomePage)Shell.Current.CurrentPage).PendingPublication.WaitAsync(ct);
        var baseline = await ImportFinancialNativeReview.StoredAsync(services, ct);
        var local = services.GetRequiredService<ILocalizationService>(); var theme = services.GetRequiredService<ThemeService>();
        var language = local.CurrentLanguage; var choice = theme.Choice;
        var resume = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.Intent?.GetBooleanExtra("review_plan_resume_actions", false) == true;
        using var original = resume ? await PlanEditorNativeReview.ContinueSecondaryFieldsAsync(services, Path.Combine(folder, "original-secondary-fields"), ct)
            : await PlanEditorNativeReview.CreateAsync(services, Path.Combine(folder, "original-secondary-fields"), ct);
        if (!resume) await original.PrepareSecondaryFieldsAsync(ct);
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
                LogReport("secondary-fields-native-consent", comparison, new { Caption = caption, Positive = answer, ProcessId = Environment.ProcessId });
            }
        }
        async Task Observe(Page page, string stage)
        {
            if (resume && Completed(stage)) return;
            await Focused(); await Task.Delay(150, ct); var readings = new List<object>();
            foreach (var label in PlanEditorNativeReview.Views(page).OfType<Label>().Where(Visible).Where(item => item.Text is { } text && PlanEditorNativeReview.SecondaryFieldsCaptions(page).Contains(text)))
            {
                await Scroll(label);
                if (label.Handler?.PlatformView is not Android.Widget.TextView { Layout: { } layout } text || Vafadar.Core.Text.Digits.ToAscii(text.Text ?? string.Empty) != Vafadar.Core.Text.Digits.ToAscii(label.Text ?? string.Empty)
                    || layout.LineCount == 0 || Enumerable.Range(0, layout.LineCount).Any(line => layout.GetEllipsisCount(line) != 0))
                {
                    var native = label.Handler?.PlatformView as Android.Widget.TextView;
                    var failure = new { Stage = stage, Label = label.Text, Native = native?.Text, Width = native?.Width, Height = native?.Height,
                        Lines = native?.Layout?.LineCount, Ellipses = native?.Layout is { } current ? Enumerable.Range(0, current.LineCount).Select(current.GetEllipsisCount).ToArray() : [] };
                    await File.WriteAllTextAsync(Path.Combine(folder, stage + "-failed-native.json"), JsonSerializer.Serialize(failure), ct);
                    LogReport("secondary-fields-failed-native", comparison, failure);
                    throw new IOException("Original monetary caption is incomplete.");
                }
                // The actual label presenter applies the selected native digit shapes without changing punctuation or money.
                readings.Add(new { label.Text, NativeText = text.Text, text.Width, text.Height, Lines = layout.LineCount });
            }
            foreach (var entry in PlanEditorNativeReview.Views(page).OfType<Entry>().Where(Visible).Where(item => item.Text is { } text && PlanEditorNativeReview.SecondaryFieldsRaw(page).Contains(text)))
            {
                await Scroll(entry);
                if (entry.Handler?.PlatformView is not Android.Widget.EditText { IsShown: true, HasWindowFocus: true } input || input.Text != entry.Text)
                {
                    var native = entry.Handler?.PlatformView as Android.Widget.EditText;
                    var failure = new { Stage = stage, Raw = entry.Text, Native = native?.Text, Visible = native?.IsShown, Focused = native?.HasWindowFocus };
                    LogReport("secondary-fields-failed-native", comparison, failure);
                    throw new IOException("Original monetary raw input differs.");
                }
                readings.Add(new { entry.Text, input.Width, input.Height });
            }
            var row = new { Stage = stage, ProcessId = Environment.ProcessId, NativeReadings = readings };
            await File.WriteAllTextAsync(Path.Combine(folder, stage + ".json"), JsonSerializer.Serialize(row), ct);
            original.Observations.Add(row); LogReport("secondary-fields-observation", comparison, row);
        }
        try
        {

            foreach (var name in new[] { "en", "fa", "de" }) foreach (var value in new[] { ThemeChoice.Light, ThemeChoice.Dark })
            {
                await SetTheme(value); await SetLanguageAsync(app, local, name, ct);
                await original.SecondaryFieldsContextAsync(name + "-" + value, Invoke, Observe, ct, resume ? Completed : null);
            }
            await original.VerifySecondaryFieldsAsync(ct);
            if (baseline != await ImportFinancialNativeReview.StoredAsync(services, ct)) throw new IOException("Original base installation rows changed.");

            return new { Comparison = comparison, OriginalBaseStoreUnchanged = true, NativeGoalAllocations = 1, NativeAssumedPriceSaves = 1, NativeOccurrenceOverrides = 1, NativePartialPayments = 1, NativeBillSaves = 1, NativeLedgerEntries = 3, NativeNewLedgerEntries = 2,
                NoAcceptedCommandReplayed = true, OriginalAcceptedActionsReused = resume, Observations = original.Observations, Device = DeviceEvidence() };
        }
        finally { await SetTheme(choice); await SetLanguageAsync(app, local, language.CultureName, ct); }
    }
}
