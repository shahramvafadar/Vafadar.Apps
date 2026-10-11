#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization;
using Vafadar.Maui.Controls;
using Vafadar.Zanance.App.Features.Commerce;
using Vafadar.Zanance.App.Features.Templates;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    // D-154 / AT-156: real chooser and template controls, confined to the independently owned fictitious database.
    private static async Task ReviewQuickResourcesAsync(App app, IServiceProvider services, ActiveItemsPage first,
        ActiveItemsViewModel firstModel, IDbContextFactory<ZananceDbContext> factory, ActiveItemsReviewAccess access,
        string folder, string language, Func<Task<string>> stored, string before)
    {
        var translator = services.GetRequiredService<Translator>(); var localization = services.GetRequiredService<ILocalizationService>();
        var interaction = services.GetRequiredService<IAppInteraction>(); var observations = new List<object>();
        static string Financial(string json)
        {
            var rows = JsonSerializer.Deserialize<SortedDictionary<string, List<string>>>(json)!;
            rows.Remove("ResourceSelections"); return JsonSerializer.Serialize(rows);
        }
        async Task CheckAsync(ContentPage page, string scenario)
        {
            if (!ReferenceEquals(Shell.Current.CurrentPage, page)) throw new InvalidOperationException("The quick-resource review page was retired.");
            // A native kind Invoke queues binding/layout work; inspect only the newly realized current page's labels.
            for (var attempt = 0; attempt < 20; attempt++)
            {
                var pending = VisualDescendants(page).OfType<Label>().Where(Visible).Where(label => !string.IsNullOrEmpty(label.Text))
                    .Any(label => label.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.TextBlock native
                        || native.ActualWidth < 1 || native.ActualHeight < 1 || native.IsTextTrimmed);
                if (!pending) break;
                await Task.Delay(50);
            }
            File.WriteAllText(Path.Combine(folder, language + "-" + scenario + "-native-layout.json"), JsonSerializer.Serialize(
                VisualDescendants(page).OfType<Label>().Where(Visible).Select(label => new { label.Text, label.Width, label.Height,
                    Native = label.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.TextBlock native
                        ? new { native.ActualWidth, native.ActualHeight, native.IsTextTrimmed } : null })));
            foreach (var label in VisualDescendants(page).OfType<Label>().Where(Visible)) CheckCaption(label);
            foreach (var button in VisualDescendants(page).OfType<Button>().Where(Visible))
            {
                if (button.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button native || native.ActualWidth < 44 || native.ActualHeight < 44)
                    throw new InvalidOperationException("A quick-resource target lost its native minimum size.");
                var peer = Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.CreatePeerForElement(native)
                    as Microsoft.UI.Xaml.Automation.Peers.ButtonAutomationPeer ?? throw new InvalidOperationException("Missing quick-resource peer.");
                if (button.CommandParameter is ActiveItemRow row && (peer.GetName() != row.AccessibilityText || native.IsEnabled != row.CanToggle))
                    throw new InvalidOperationException("An original quick-resource choice lost its complete spoken name or availability.");
                observations.Add(new { scenario, Name = peer.GetName(), native.ActualWidth, native.ActualHeight, native.IsEnabled });
            }
            if (Financial(await stored()) != Financial(before)) throw new InvalidOperationException("Quick choice changed an original complete financial/reference/resource row.");
            await CaptureAsync(app, folder, language + "-quick-resources-" + scenario);
        }
        for (var index = 0; index < 2; index++)
        {
            var vm = index == 0 ? firstModel : new ActiveItemsViewModel(new(factory, access), translator, interaction);
            var page = index == 0 ? first : new ActiveItemsPage(vm, localization);
            if (index != 0) { await Shell.Current.Navigation.PushAsync(page); await vm.EnsureLoadedAsync(); await Task.Delay(250); }
            var chips = VisualDescendants(page).OfType<ChoiceChips>().Single();
            InvokeChoiceReview(VisualDescendants(chips).OfType<Button>().ElementAt(index == 0 ? 4 : 5)); await vm.PendingKindChange; await Task.Delay(350);
            if (vm.Items.Count != (index == 0 ? 5 : 3) || !vm.CanChoose || vm.Items.Any(row => row.IsSelected))
                throw new InvalidOperationException("Original quick resources were guessed or filtered.");
            await CheckAsync(page, index == 0 ? "templates-original" : "filters-original");
            var viewport = VisualDescendants(page).OfType<ScrollView>().Single();
            var original = vm.Items[0]; var target = VisualDescendants(page).OfType<Button>().Single(button => ReferenceEquals(button.CommandParameter, original));
            await ScrollToViewIfNeededAsync(viewport, target); InvokeChoiceReview(target); await Task.Delay(150);
            if (!original.IsSelected || !vm.CanSave) throw new InvalidOperationException("The actual native choice did not select its original identity.");
            await viewport.ScrollToAsync(0, 0, false); await Task.Delay(150); await CheckAsync(page, index == 0 ? "templates-choice" : "filters-choice");
            var save = WrappingActions(page).Single(action => action.Command == vm.SaveCommand && Visible(action));
            InvokeChoiceReview(VisualDescendants(save).OfType<Button>().Single()); if (vm.SaveCommand.ExecutionTask is { } pending) await pending;
            if (ReferenceEquals(Shell.Current.CurrentPage, page)) throw new InvalidOperationException("The explicit native Save did not return.");
            var read = await new ResourceChoiceStore(factory, access).ReadAsync(index == 0 ? QuotaKind.QuickTemplates : QuotaKind.SavedFilters);
            if (read.SelectedIds.Count != 1 || !read.SelectedIds.Contains(original.Id)) throw new InvalidOperationException("The native Save stored a guessed identity.");
        }
        var effects = new BudgetActionsReviewInteraction(); var store = new ZananceStore(factory, TimeProvider.System, access);
        var templateModel = new TemplatesViewModel(store, translator, localization, effects); var templates = new TemplatesPage(templateModel);
        await Shell.Current.Navigation.PushAsync(templates); await templateModel.LoadAsync(); await Task.Delay(350);
        if (templateModel.Templates.Count != 5 || templateModel.Actions.Review!.Work.CanApplyTemplate(templateModel.Templates[0].Id)
            != templateModel.Actions.Review.TemplateChoice.SelectedIds.Contains(templateModel.Templates[0].Id))
            throw new InvalidOperationException("Original template activity lost its separate selected right.");
        await CheckAsync(templates, "retained-template-list");
        var delete = VisualDescendants(templates).OfType<Button>().First(button => button.Command == templateModel.DeleteCommand);
        InvokeChoiceReview(delete); if (templateModel.DeleteCommand.ExecutionTask is { } deletion) await deletion;
        if (effects.Failures.Count != 0 || templateModel.Templates.Count != 5) throw new InvalidOperationException("Cancelled native deletion changed retained templates.");
        await CheckAsync(templates, "cancelled-template-delete"); InvokeSnapshotBack(templates); await Task.Delay(250);
        await using (var db = await factory.CreateDbContextAsync()) await db.Database.ExecuteSqlRawAsync("DELETE FROM ResourceSelections");
        if (await stored() != before) throw new InvalidOperationException("Complete independent fixture rows were not restored.");
        File.WriteAllText(Path.Combine(folder, language + "-quick-resources-proof.json"), JsonSerializer.Serialize(new
        { ActualNativeQuickTemplateAndFilterKinds = true, ExplicitOriginalChoicesSaved = true, OriginalTemplatesAndFilterQueriesRetained = true,
            ActualTemplateDeleteCancelled = true, Complete25TablesRestored = true, ReleaseRegistrationInactive = true, observations }));
    }
}
#endif
