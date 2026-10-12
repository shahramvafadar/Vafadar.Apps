#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Maui.Controls;
using Vafadar.Zanance.App.Features.Commerce;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    // D-162 / AT-164: actual comparison/return targets with independent fictitious rows and original native effects.
    private static async Task ReviewProductPlansAsync(App app, IServiceProvider services, string folder, string[] languages)
    {
        var path = Path.Combine(folder, "product-plans.db");
        await using var fixture = new ServiceCollection().AddSingleton<TimeProvider>(TimeProvider.System).AddZananceData(path).BuildServiceProvider();
        fixture.MigrateLocalDatabase<ZananceDbContext>(); var factory = fixture.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        async Task<string> StoredAsync()
        {
            await using var db = await factory.CreateDbContextAsync(); await db.Database.OpenConnectionAsync();
            using var command = db.Database.GetDbConnection().CreateCommand(); var tables = new List<string>();
            command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name";
            using (var reader = await command.ExecuteReaderAsync()) while (await reader.ReadAsync()) tables.Add(reader.GetString(0));
            if (tables.Count != 25) throw new InvalidOperationException("Incomplete pre-Save fixture schema.");
            var all = new SortedDictionary<string, List<string>>();
            foreach (var table in tables)
            {
                command.CommandText = "SELECT * FROM \"" + table + "\""; using var reader = await command.ExecuteReaderAsync(); var rows = new List<string>();
                while (await reader.ReadAsync()) { var values = new object?[reader.FieldCount]; for (var i = 0; i < values.Length; i++) values[i] = reader.IsDBNull(i) ? null : reader.GetValue(i); rows.Add(JsonSerializer.Serialize(values)); }
                rows.Sort(StringComparer.Ordinal); all[table] = rows;
            }
            return JsonSerializer.Serialize(all);
        }
        var before = await StoredAsync(); var translator = services.GetRequiredService<Translator>();
        var localization = services.GetRequiredService<ILocalizationService>();
        foreach (var language in languages)
        {
            await SetLanguageAsync(app, localization, language); var parent = Shell.Current.CurrentPage;
            var vm = new ProductPlansViewModel(fixture.GetRequiredService<CommercialAccessStore>(), translator,
                services.GetRequiredService<IAppInteraction>());
            var page = new ProductPlansPage(vm, localization); ProfileNativeReads.Add(page);
            await Shell.Current.Navigation.PushAsync(page); await Task.Delay(300);
            if (!vm.CanInteract) throw new InvalidOperationException("The actual comparison did not finish its covered read.");
            var scroll = VisualDescendants(page).OfType<ScrollView>().Single();
            var chips = VisualDescendants(page).OfType<ChoiceChips>().Single();
            var actual = vm.AccessText; var nativeChoices = new List<object>();
            CheckCaption(VisualDescendants(page).OfType<Label>().Single(label => label.Text == actual), ProfileNativeReads);
            await CaptureAsync(app, folder, language + "-product-plans-current-access");
            for (var index = 0; index < 3; index++)
            {
                // Reacquire generated buttons after each actual choice; never retain a retired native target.
                var target = VisualDescendants(chips).OfType<Button>().ElementAt(index);
                var chipCaption = VisualDescendants(chips).OfType<Label>().Single(label => label.Text == vm.PlanNames[index]);
                CheckCaption(chipCaption, ProfileNativeReads);
                if (target.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button nativeChoice
                    || nativeChoice.ActualWidth < 44 || nativeChoice.ActualHeight < 44
                    || !SemanticProperties.GetDescription(target).Contains(vm.PlanNames[index], StringComparison.Ordinal))
                    throw new InvalidOperationException("The original comparison choice lost its complete native target/name.");
                await ScrollToViewIfNeededAsync(scroll, target); InvokeChoiceReview(target, ProfileNativeReads); await Task.Delay(100);
                if (vm.PlanIndex != index || vm.AccessText != actual || vm.Rows.Count != 19)
                    throw new InvalidOperationException("The comparison changed original access or lost a native choice.");
                var caption = VisualDescendants(page).OfType<Label>().Single(label => Visible(label) && label.Text == vm.Description);
                CheckCaption(caption, ProfileNativeReads);
                await CaptureAsync(app, folder, language + "-product-plans-" + index + "-top");
                foreach (var row in vm.Rows)
                {
                    var label = VisualDescendants(page).OfType<Label>().Single(item => Visible(item) && item.Text == row.Title);
                    await ScrollToViewIfNeededAsync(scroll, label); CheckCaption(label, ProfileNativeReads);
                    var value = ((VerticalStackLayout)label.Parent).Children.OfType<Label>().Last();
                    CheckCaption(value, ProfileNativeReads);
                }
                var end = VisualDescendants(page).OfType<Label>().Single(label => Visible(label) && label.Text == translator[
                    index == 2 ? "ProductPlans_Online" : index == 1 ? "ProductPlans_Lifetime" : "ProductPlans_Counting"]);
                await ScrollToViewIfNeededAsync(scroll, end); CheckCaption(end, ProfileNativeReads);
                var action = WrappingActions(page).Single(item => item.Command == vm.ContinueCommand);
                CheckCaption(VisualDescendants(action).OfType<Label>().Single(), ProfileNativeReads);
                if (await StoredAsync() != before) throw new InvalidOperationException("Comparing tiers wrote original data.");
                await CaptureAsync(app, folder, language + "-product-plans-" + index + "-end");
                nativeChoices.Add(new { Index = index, CapturedAccessRetained = true, CompleteRowCaptions = true });
            }
            var help = VisualDescendants(page).OfType<HelpButton>().Single(); var helpTarget = VisualDescendants(help).OfType<Button>().Single();
            await ScrollToViewIfNeededAsync(scroll, helpTarget); InvokeChoiceReview(helpTarget, ProfileNativeReads); await Task.Delay(500);
            if (app.Windows[0].Handler?.PlatformView is not Microsoft.UI.Xaml.Window { Content.XamlRoot: { } root })
                throw new InvalidOperationException("The comparison help has no original native root.");
            var dialog = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetOpenPopupsForXamlRoot(root)
                .Select(popup => popup.Child).OfType<Microsoft.UI.Xaml.Controls.ContentDialog>().Single();
            await CaptureWindowAsync(app.Windows[0], Path.Combine(folder, language + "-product-plans-help.png"));
            var close = FindNativeButton(dialog, translator["Common_Ok"]) ?? throw new InvalidOperationException("The help Close target is unavailable.");
            if (new Microsoft.UI.Xaml.Automation.Peers.ButtonAutomationPeer(close).GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Invoke)
                is not Microsoft.UI.Xaml.Automation.Provider.IInvokeProvider closePattern)
                throw new InvalidOperationException("The help Close has no native Invoke.");
            closePattern.Invoke(); await Task.Delay(200);
            if (vm.PlanIndex != 2 || vm.AccessText != actual) throw new InvalidOperationException("Help return lost comparison or access.");
            var returning = VisualDescendants(WrappingActions(page).Single(item => item.Command == vm.ContinueCommand)).OfType<Button>().Single();
            InvokeChoiceReview(returning, ProfileNativeReads); if (vm.ContinueCommand.ExecutionTask is { } leaving) await leaving;
            if (!ReferenceEquals(Shell.Current.CurrentPage, parent) || await StoredAsync() != before)
                throw new InvalidOperationException("The actual Continue failed to return without changing original data.");
            await File.WriteAllTextAsync(Path.Combine(folder, language + "-product-plans-proof.json"), JsonSerializer.Serialize(new
            { ActualOriginalPage = true, ThreeNativeChoices = nativeChoices, NativeHelpAndClose = true, ActualNativeContinueReturn = true,
                Complete25TablesUnchanged = true, InactiveAccessNotPaidClaim = true, NoPurchaseSaveOrActivation = true }));
        }
    }
}
#endif
