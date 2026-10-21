using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization;
using Vafadar.Zanance.App.Diagnostics;
using Vafadar.Zanance.App.Features.Home;
using Vafadar.Zanance.App.Presentation;

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    /// <summary>Qualifies original goal/budget money on actual independent Release/AOT controls, preserving base data and OS settings.</summary>
    private static async Task<object> ReviewGoalBudgetMoneyAsync(App app, IServiceProvider services, string folder, string comparison, CancellationToken ct)
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
        using var original = resume ? await PlanEditorNativeReview.ContinueGoalBudgetMoneyAsync(services, Path.Combine(folder, "original-goal-budget"), ct)
            : await PlanEditorNativeReview.CreateAsync(services, Path.Combine(folder, "original-goal-budget"), ct);
        if (!resume) await original.PrepareGoalBudgetMoneyAsync(ct);
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
        async Task Invoke(Page page, System.Windows.Input.ICommand command)
        {
            var button = PlanEditorNativeReview.Views(page).OfType<Button>().Single(item => ReferenceEquals(item.Command, command));
            await Scroll(button);
            await PlanEditorNativeReview.WaitAsync(() => button.Handler?.PlatformView is Android.Widget.Button { IsShown: true, HasWindowFocus: true, Enabled: true }, ct);
            if (!Visible(button) || button.Handler?.PlatformView is not Android.Widget.Button native || !native.PerformClick()) throw new IOException("Actual original money Save unavailable.");
            await Task.Yield();
        }
        async Task Observe(Page page, string stage)
        {
            if (resume && Completed(stage)) return;
            await Focused(); await Task.Delay(150, ct); var readings = new List<object>();
            foreach (var label in PlanEditorNativeReview.Views(page).OfType<Label>().Where(Visible).Where(item => item.Text is { } text && PlanEditorNativeReview.GoalBudgetMoneyCaptions(page).Contains(text)))
            {
                await Scroll(label);
                if (label.Handler?.PlatformView is not Android.Widget.TextView { Layout: { } layout } text || Vafadar.Core.Text.Digits.ToAscii(text.Text ?? string.Empty) != Vafadar.Core.Text.Digits.ToAscii(label.Text ?? string.Empty)
                    || layout.LineCount == 0 || Enumerable.Range(0, layout.LineCount).Any(line => layout.GetEllipsisCount(line) != 0))
                {
                    var native = label.Handler?.PlatformView as Android.Widget.TextView;
                    var failure = new { Stage = stage, Label = label.Text, Native = native?.Text, Width = native?.Width, Height = native?.Height,
                        Lines = native?.Layout?.LineCount, Ellipses = native?.Layout is { } current ? Enumerable.Range(0, current.LineCount).Select(current.GetEllipsisCount).ToArray() : [] };
                    await File.WriteAllTextAsync(Path.Combine(folder, stage + "-failed-native.json"), JsonSerializer.Serialize(failure), ct);
                    LogReport("goal-budget-money-failed-native", comparison, failure);
                    throw new IOException("Original monetary caption is incomplete.");
                }
                // The actual label presenter applies the selected native digit shapes without changing punctuation or money.
                readings.Add(new { label.Text, NativeText = text.Text, text.Width, text.Height, Lines = layout.LineCount });
            }
            foreach (var entry in PlanEditorNativeReview.Views(page).OfType<Entry>().Where(Visible))
            {
                await Scroll(entry);
                if (entry.Handler?.PlatformView is not Android.Widget.EditText { IsShown: true, HasWindowFocus: true } input || input.Text != entry.Text)
                {
                    var native = entry.Handler?.PlatformView as Android.Widget.EditText;
                    var failure = new { Stage = stage, Raw = entry.Text, Native = native?.Text, Visible = native?.IsShown, Focused = native?.HasWindowFocus };
                    LogReport("goal-budget-money-failed-native", comparison, failure);
                    throw new IOException("Original monetary raw input differs.");
                }
                readings.Add(new { entry.Text, input.Width, input.Height });
            }
            var row = new { Stage = stage, ProcessId = Environment.ProcessId, NativeReadings = readings };
            await File.WriteAllTextAsync(Path.Combine(folder, stage + ".json"), JsonSerializer.Serialize(row), ct);
            original.Observations.Add(row); LogReport("goal-budget-money-observation", comparison, row);
        }
        try
        {
            var accept = !File.Exists(Path.Combine(folder, "original-goal-budget", "budget-native-accepted.json"));
            foreach (var name in new[] { "en", "fa", "de" }) foreach (var value in new[] { ThemeChoice.Light, ThemeChoice.Dark })
            {
                await SetTheme(value); await SetLanguageAsync(app, local, name, ct);
                await original.GoalBudgetMoneyContextAsync(name + "-" + value, Invoke, Observe, accept, ct, resume ? Completed : null); accept = false;
            }
            if (baseline != await ImportFinancialNativeReview.StoredAsync(services, ct)) throw new IOException("Original base installation rows changed.");
            return new { OriginalBaseStoreUnchanged = true, NativeGoalSaves = 1, NativeBudgetSaves = 1, NativeLedgerPosts = 0,
                NoAcceptedCommandReplayed = true, OriginalAcceptedActionsReused = resume, Observations = original.Observations, Device = DeviceEvidence() };
        }
        finally { await SetTheme(choice); await SetLanguageAsync(app, local, language.CultureName, ct); }
    }
}
