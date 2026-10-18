#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization;
using Vafadar.Zanance.App.Features.Goals;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Checks original goal choices and their native selection without saving a financial draft.</summary>
    private static async Task ReviewGoalChoicesWindowsAsync(App app, IServiceProvider services, string folder)
    {
        var store = services.GetRequiredService<ZananceStore>(); var goals = services.GetRequiredService<GoalStore>();
        async Task<string> StoredAsync() => JsonSerializer.Serialize(new { Accounts = await store.GetAccountsAsync(),
            Entries = await store.GetEntriesAsync(), Goals = await goals.GetGoalsAsync(), Settings = await store.GetSettingsAsync() });
        var before = await StoredAsync(); var local = services.GetRequiredService<ILocalizationService>();
        var captures = new List<object>();
        foreach (var language in new[] { "en", "fa", "de" })
        foreach (var theme in new[] { ThemeChoice.Light, ThemeChoice.Dark })
        foreach (var width in new[] { 360d, 412d, 1280d })
        {
            await Shell.Current.GoToAsync("//home"); app.Windows[0].Width = width;
            await SetStoreThemeAsync(app, services.GetRequiredService<ThemeService>(), theme);
            await SetLanguageAsync(app, local, language);
            var vm = ActivatorUtilities.CreateInstance<GoalEditorViewModel>(services);
            await vm.ApplyQueryAsync(new Dictionary<string, object>());
            vm.Name = "Fictitious unsaved goal"; vm.AmountText = "1234.56"; vm.ContributionText = "12.34";
            var page = new GoalEditorPage(vm, local) { FlowDirection = local.CurrentLanguage.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
            await Shell.Current.Navigation.PushModalAsync(page, false); await Task.Delay(150);
            var scroll = VisualDescendants(page).OfType<ScrollView>().First(); var checkedCaptions = 0;
            foreach (var choice in VisualDescendants(page).OfType<Vafadar.Maui.Controls.ChoiceChips>().Where(ReimbursementVisible))
            {
                await ScrollToViewIfNeededAsync(scroll, choice); await Task.Delay(100);
                var labels = VisualDescendants(choice).OfType<Label>().ToArray();
                if (labels.Length == 0 || labels.Length != choice.ItemsSource?.Count) throw new IOException("Original goal choices incomplete.");
                foreach (var label in labels) CheckCaption(label, ProfileNativeReads);
                checkedCaptions += labels.Length;
            }
            var method = VisualDescendants(page).OfType<Vafadar.Maui.Controls.ChoiceChips>()
                .Single(value => ReferenceEquals(value.ItemsSource, vm.MethodNames));
            await ScrollToViewIfNeededAsync(scroll, method); await Task.Delay(100);
            var targets = VisualDescendants(method).OfType<Button>().ToArray();
            string Draft() => JsonSerializer.Serialize(new { vm.Name, vm.AmountText, vm.CurrencyCode, vm.ContributionText, vm.PercentText, vm.TargetDate, vm.FirstDate });
            var draft = Draft();
            if (!vm.CanChooseMethod || vm.MethodIndex != 0 || targets.Length != 3) throw new IOException("Original goal method selection unavailable.");
            InvokeChoiceReview(targets[1], ProfileNativeReads); await Task.Delay(100);
            if (vm.MethodIndex != 1) throw new IOException("Original income-share target did not select its method.");
            InvokeChoiceReview(targets[0], ProfileNativeReads); await Task.Delay(100);
            if (vm.MethodIndex != 0 || draft != Draft() || before != await StoredAsync()) throw new IOException("Choice navigation changed original draft or stored inputs.");
            var context = language + "-" + theme + "-" + width; var output = Path.Combine(folder, context);
            Directory.CreateDirectory(output);
            await CaptureWindowAsync(app.Windows[0], Path.Combine(output, "goal-choices.png"));
            await File.WriteAllTextAsync(Path.Combine(output, "editor-text.json"), JsonSerializer.Serialize(new
            { Surface = "goal-choices", CompleteChoiceCaptions = checkedCaptions, OriginalMethodSelectedAndRestored = true, DraftPreserved = true, StoredInputsUnchanged = true, NoEditorSave = true }));
            captures.Add(new { Language = context, File = "goal-choices.png" });
            await Shell.Current.Navigation.PopModalAsync(false);
        }
        await File.WriteAllTextAsync(Path.Combine(folder, "complete.json"), JsonSerializer.Serialize(new
        { Package = AppInfo.Current.PackageName, ProcessId = Environment.ProcessId, Captures = captures,
            FinancialRowsUnchangedAfterSeeding = before == await StoredAsync(), WindowsLayoutStressOnly = true }));
    }
}
#endif
