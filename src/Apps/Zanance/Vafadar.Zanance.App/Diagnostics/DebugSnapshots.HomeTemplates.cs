#if DEBUG && WINDOWS
using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization;
using Vafadar.Zanance.App.Features.Commerce;
using Vafadar.Zanance.App.Features.Entries;
using Vafadar.Zanance.App.Features.Home;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Records original navigation parameters only inside an independently owned native fixture.</summary>
    private sealed class HomeTemplateReviewInteraction : IAppInteraction
    {
        internal readonly List<(string Route, IDictionary<string, object>? Parameters)> Routes = [];
        internal readonly List<Exception> Failures = [];
        public Task NavigateAsync(string route, IDictionary<string, object>? parameters = null)
        { Routes.Add((route, parameters)); return Task.CompletedTask; }
        public Task ShowFailureAsync(Exception exception) { Failures.Add(exception); return Task.CompletedTask; }
        public Task AlertAsync(string title, string message, string cancel) => throw new InvalidOperationException("Unexpected fixture alert.");
        public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel) => Task.FromResult(false);
        public Task<string?> PromptAsync(string title, string message, string accept, string cancel, string? initialValue = null, int maxLength = -1)
            => throw new InvalidOperationException("Unexpected fixture prompt.");
        public Task<string?> ChooseAsync(string title, string cancel, params string[] actions) => throw new InvalidOperationException("Unexpected fixture choice.");
    }

    // D-155 / AT-157: actual Home/entry/chooser controls and original reviewed navigation, never release activation.
    private static async Task ReviewHomeTemplateAsync(App app, IServiceProvider services, IServiceProvider fixture,
        IDbContextFactory<ZananceDbContext> factory, ActiveItemsReviewAccess access, string folder, string language,
        Func<Task<string>> stored, string before)
    {
        var translator = services.GetRequiredService<Translator>(); var localization = services.GetRequiredService<ILocalizationService>();
        var effects = new HomeTemplateReviewInteraction(); var store = new ZananceStore(factory, TimeProvider.System, access);
        var choices = new ResourceChoiceStore(factory, access); var observations = new List<object>();
        var presenter = ActivatorUtilities.CreateInstance<Features.Goals.GoalPresenter>(services, fixture.GetRequiredService<HoldingStore>());
        var home = ActivatorUtilities.CreateInstance<HomeViewModel>(services, store, fixture.GetRequiredService<PlanStore>(),
            fixture.GetRequiredService<GoalStore>(), fixture.GetRequiredService<HoldingStore>(), presenter, effects);
        var page = new HomePage(home, store); await Shell.Current.Navigation.PushAsync(page);
        async Task AwaitHomeAsync()
        {
            for (var attempt = 0; attempt < 40 && !ReferenceEquals(Shell.Current.CurrentPage, page); attempt++) await Task.Delay(50);
            if (!ReferenceEquals(Shell.Current.CurrentPage, page)) throw new InvalidOperationException("Native Back did not return to the original Home.");
            // Native Back queues the appearance event. Await its actual publication before changing fictitious inputs.
            await Task.Delay(150); await page.PendingPublication;
            if (!home.TemplateNavigation.CanInteract) throw new InvalidOperationException("Home did not publish its current original review.");
        }
        await AwaitHomeAsync(); await Task.Delay(150);
        if (home.QuickTemplates.Count != 6 || home.QuickTemplates.Any(t => t.CanUse) || !home.TemplateNavigation.CanChoose)
            throw new InvalidOperationException("Home guessed an original active subset.");
        if (Environment.GetEnvironmentVariable("VAFADAR_SNAPSHOT_TEMPLATE_ORDER") == "1")
        {
            var expected = (await store.GetTemplatesAsync()).Take(6).Select(t => t.Id).ToArray();
            var actual = VisualDescendants(page).OfType<Button>().Where(b => b.CommandParameter is QuickTemplate)
                .Select(b => ((QuickTemplate)b.CommandParameter).Id).ToArray();
            if (!expected.SequenceEqual(home.QuickTemplates.Select(t => t.Id)) || !expected.SequenceEqual(actual))
                throw new InvalidOperationException("The actual Home targets changed original SQLite name order.");
        }
        static string Financial(string json)
        {
            var rows = JsonSerializer.Deserialize<SortedDictionary<string, List<string>>>(json)!;
            rows.Remove("ResourceSelections"); return JsonSerializer.Serialize(rows);
        }
        async Task CheckAsync(ContentPage current, string scenario, params string?[] changedCaptions)
        {
            if (!ReferenceEquals(Shell.Current.CurrentPage, current)) throw new InvalidOperationException("The Home template page was retired.");
            await Task.Delay(250);
            foreach (var label in VisualDescendants(current).OfType<Label>().Where(Visible).Where(l => changedCaptions.Contains(l.Text))) CheckCaption(label);
            foreach (var button in VisualDescendants(current).OfType<Button>().Where(Visible).Where(b => b.CommandParameter is QuickTemplate
                || WrappingActions(current).Any(action => Visible(action) && VisualDescendants(action).Contains(b))))
            {
                if (button.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button native || native.ActualWidth < 44 || native.ActualHeight < 44)
                    throw new InvalidOperationException("A Home template target lost its native minimum size.");
                var peer = Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.CreatePeerForElement(native)
                    ?? throw new InvalidOperationException("Missing Home template peer.");
                if (button.CommandParameter is QuickTemplate template && (peer.GetName() != template.Description || native.IsEnabled != template.CanUse))
                    throw new InvalidOperationException("The original Home template lost its name or selected enablement.");
                observations.Add(new { scenario, Name = peer.GetName(), native.ActualWidth, native.ActualHeight, native.IsEnabled });
            }
            if (Financial(await stored()) != Financial(before)) throw new InvalidOperationException("Template navigation changed complete original financial rows.");
            await CaptureAsync(app, folder, language + "-home-template-" + scenario);
        }
        if (Environment.GetEnvironmentVariable("VAFADAR_SNAPSHOT_ONLY") != "home-template-destination")
        {
        await CheckAsync(page, "original-unavailable", home.TemplateNavigation.WorkText, translator["ActiveItems_Title"]);
        var choose = WrappingActions(page).Single(action => action.Command == home.TemplateNavigation.ChooseCommand);
        InvokeChoiceReview(VisualDescendants(choose).OfType<Button>().Single());
        if (home.TemplateNavigation.ChooseCommand.ExecutionTask is { } choosing) await choosing;
        var destination = effects.Routes.Single();
        if (destination.Route != AppRoutes.ActiveItemsRoute || !Equals(destination.Parameters!["kind"], QuotaKind.QuickTemplates))
            throw new InvalidOperationException("The native Home choice did not retain its original typed kind.");
        var chooser = new ActiveItemsViewModel(choices, translator, effects); var chooserPage = new ActiveItemsPage(chooser, localization);
        chooserPage.ApplyQueryAttributes(new Dictionary<string, object>(destination.Parameters!));
        await Shell.Current.Navigation.PushAsync(chooserPage); await chooser.EnsureLoadedAsync(); await Task.Delay(300);
        if (chooser.KindIndex != 4 || chooser.Items.Count != 8 || chooser.Items.Any(row => row.IsSelected))
            throw new InvalidOperationException("The actual initial chooser guessed original selection.");
        await CheckAsync(chooserPage, "initial-template-kind", translator["ActiveItems_Intro"]);
        InvokeSnapshotBack(chooserPage); await AwaitHomeAsync();
        }
        var source = await choices.ReadAsync(QuotaKind.QuickTemplates); var selected = (await store.GetTemplatesAsync()).OrderBy(t => t.SortOrder).Last().Id;
        await choices.SaveAsync(source, [selected]); await home.LoadAsync(); await Task.Delay(300);
        if (home.QuickTemplates[0].Id != selected || !home.QuickTemplates[0].CanUse || home.QuickTemplates.Skip(1).Any(t => t.CanUse))
            throw new InvalidOperationException("The selected later original was unreachable on Home.");
        if (Environment.GetEnvironmentVariable("VAFADAR_SNAPSHOT_ONLY") != "home-template-destination")
            await CheckAsync(page, "chosen-later-original", home.TemplateNavigation.WorkText);
        if (Environment.GetEnvironmentVariable("VAFADAR_SNAPSHOT_TEMPLATE_ORDER") == "1")
        {
            home.TemplateNavigation.FailLoad(); await Task.Delay(100);
            var retry = WrappingActions(page).Single(action => action.Command == home.TemplateNavigation.ReloadCommand && Visible(action));
            InvokeChoiceReview(VisualDescendants(retry).OfType<Button>().Single());
            if (home.TemplateNavigation.ReloadCommand.ExecutionTask is { } reloading) await reloading;
            await Task.Delay(150);
            var refreshed = VisualDescendants(page).OfType<Button>().Single(b => b.CommandParameter is QuickTemplate t && t.Id == selected);
            if (!home.TemplateNavigation.CanInteract || home.TemplateNavigation.Feedback is not null
                || !home.QuickTemplates.Single(t => t.Id == selected).CanUse
                || refreshed.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button { IsEnabled: true })
                throw new InvalidOperationException("Native Retry froze transient busy state into original shortcut availability.");
            await CheckAsync(page, "read-only-retry", home.TemplateNavigation.WorkText);
        }
        var shortcut = VisualDescendants(page).OfType<Button>().Single(b => b.CommandParameter is QuickTemplate t && t.Id == selected);
        InvokeChoiceReview(shortcut); if (home.UseTemplateCommand.ExecutionTask is { } opening) await opening;
        var query = new Dictionary<string, object>(effects.Routes.Last().Parameters!);
        if (effects.Routes.Last().Route != AppRoutes.EntryEditorRoute || query["templateVisit"] is not TemplateVisit)
            throw new InvalidOperationException("The real Home button lost its original handoff.");
        async Task<EntryEditorViewModel> DestinationAsync(Dictionary<string, object> parameters, bool retainedDraft)
        {
            var editor = ActivatorUtilities.CreateInstance<EntryEditorViewModel>(services, store, effects);
            // Shell initializes a pushed query-aware binding context with its empty query first. Await that actual
            // publication before replaying the recorded typed destination; otherwise the fixture erases its own result.
            editor.AmountText = "42";
            if (retainedDraft) editor.EntryTitle = "Complete retained fictitious draft";
            var editorPage = new EntryEditorPage(editor); await Shell.Current.Navigation.PushAsync(editorPage);
            var loading = typeof(EntryEditorViewModel).GetField("_loading", BindingFlags.Instance | BindingFlags.NonPublic)!;
            for (var attempt = 0; attempt < 40 && (!editor.QuickActions.CanInteract || editor.TemplateChips.Count != 8
                || (bool)loading.GetValue(editor)!); attempt++) await Task.Delay(50);
            if (!editor.QuickActions.CanInteract || editor.TemplateChips.Count != 8 || (bool)loading.GetValue(editor)!)
                throw new InvalidOperationException("The native initial entry query did not finish.");
            // Invoke the same awaited query body used by the native wrapper after its default publication completes.
            var method = typeof(EntryEditorViewModel).GetMethod("ApplyQueryAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
            await (Task)method.Invoke(editor, [parameters])!; return editor;
        }
        var valid = await DestinationAsync(new(query), false); var validPage = (EntryEditorPage)Shell.Current.CurrentPage;
        if (valid.Account?.Id != home.TemplateNavigation.Items[0].AccountId || valid.EntryTitle != "Complete original source"
            || !Vafadar.Zanance.Core.Money.MoneyText.TryParse(valid.AmountText, "EUR", localization.CurrentCulture, out var amount) || amount != 12345)
            throw new InvalidOperationException("The original Home handoff did not fill the actual detached editor.");
        await CheckAsync(validPage, "original-entry-draft");
        await Shell.Current.Navigation.PopAsync(); await AwaitHomeAsync();
        await using (var db = await factory.CreateDbContextAsync())
            await db.Templates.Where(t => t.Id == selected).ExecuteUpdateAsync(u => u.SetProperty(t => t.Amount, 98765));
        var stale = await DestinationAsync(new(query), true); var stalePage = (EntryEditorPage)Shell.Current.CurrentPage;
        if (stale.AmountText != "42" || stale.EntryTitle != "Complete retained fictitious draft" || effects.Failures.Count != 1
            || stale.QuickActions.Feedback != translator["Templates_SourceChanged"])
            throw new InvalidOperationException("A newer same-id original replaced the retained destination draft.");
        await Task.Delay(250);
        var feedbackLabels = VisualDescendants(stalePage).OfType<Label>().Where(Visible).Where(l => l.Text == stale.QuickActions.Feedback).ToArray();
        if (feedbackLabels.Length != 1 || stale.QuickActions.Feedback != translator["Templates_SourceChanged"])
            throw new InvalidOperationException("The original destination failure feedback was lost after native publication.");
        CheckCaption(feedbackLabels[0]);
        await CaptureAsync(app, folder, language + "-home-template-stale-entry-feedback"); await Shell.Current.Navigation.PopAsync(); await AwaitHomeAsync();
        await using (var db = await factory.CreateDbContextAsync())
            await db.Templates.Where(t => t.Id == selected).ExecuteUpdateAsync(u => u.SetProperty(t => t.Amount, 12345));
        await home.LoadAsync(); await Task.Delay(250); await Shell.Current.Navigation.PopAsync();
        await using (var db = await factory.CreateDbContextAsync()) await db.Database.ExecuteSqlRawAsync("DELETE FROM ResourceSelections");
        if (await stored() != before) throw new InvalidOperationException("Complete Home fixture rows were not restored.");
        File.WriteAllText(Path.Combine(folder, language + "-home-template-proof.json"), JsonSerializer.Serialize(new
        { ActualNativeHomeOriginalChoiceAndEntry = true, LaterChosenOriginalReachable = true, TypedInitialKindWithoutSave = true,
            ChangedSameIdDraftRetained = true, ActualDestinationQueryAppliedAfterShellInitialization = true, VisiblePinnedDestinationFailure = true, Complete25TablesRestored = true, ReleaseRegistrationInactive = true, observations }));
    }
}
#endif
