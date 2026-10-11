#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Vafadar.Localization;
using Vafadar.Maui.Controls;
using Vafadar.Zanance.App.Features.Commerce;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    // D-149 / AT-151: native choices and real explicit Save in the independently owned fictitious database only.
    private static async Task ReviewActiveBudgetDefinitionsAsync(App app, ActiveItemsPage page, ActiveItemsViewModel vm,
        IDbContextFactory<ZananceDbContext> factory, string folder, string language, Func<Task<string>> stored, string before)
    {
        var viewport = VisualDescendants(page).OfType<ScrollView>().Single();
        var chips = VisualDescendants(page).OfType<ChoiceChips>().Single();
        InvokeChoiceReview(VisualDescendants(chips).OfType<Button>().ElementAt(3));
        await vm.PendingKindChange; await Task.Delay(350);
        var eur = vm.Items.Single(row => row.Name.Contains("EUR", StringComparison.Ordinal));
        var usd = vm.Items.Single(row => row.Name.Contains("USD", StringComparison.Ordinal));
        var history = vm.Items.Single(row => row.Name.Contains("GBP", StringComparison.Ordinal));
        File.WriteAllText(Path.Combine(folder, language + "-active-budget-definitions-inputs.json"), JsonSerializer.Serialize(new
        { vm.KindIndex, NativeDigits = Vafadar.Localization.Formatting.NativeDigits.IsEnabled,
            Rows = vm.Items.Select(row => new { row.Name, row.DetailText, row.StateText, row.IsEligible, row.CanToggle }),
            SavedMethodCaption = Translator.Instance["Budget_MethodEnvelopes"], HistoryCaption = Translator.Instance["ActiveItems_History"] }));
        if (vm.KindIndex != 3 || vm.Items.Count != 3 || vm.Items.Count(row => row.IsEligible) != 2
            || history.CanToggle || history.StateText != Translator.Instance["ActiveItems_History"]
            || !eur.DetailText!.Contains("Fictitious household account with a complete long identity 4", StringComparison.Ordinal)
            || !eur.DetailText.Contains(Translator.Instance["Budget_MethodEnvelopes"], StringComparison.Ordinal))
            throw new InvalidOperationException("Actual budget groups lost copies, archived account scope or original paid method/history.");

        Button Target(ActiveItemRow row) => VisualDescendants(page).OfType<Button>().Single(button => ReferenceEquals(button.CommandParameter, row));
        async Task ToggleAsync(ActiveItemRow row)
        {
            var target = Target(row); await ScrollToViewIfNeededAsync(viewport, target);
            InvokeChoiceReview(target); await Task.Delay(150);
        }
        var observations = new List<object>();
        async Task CheckAsync(string scenario)
        {
            foreach (var label in VisualDescendants(page).OfType<Label>().Where(Visible)) CheckCaption(label);
            foreach (var button in VisualDescendants(page).OfType<Button>().Where(Visible))
            {
                if (button.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button native || native.ActualWidth < 44 || native.ActualHeight < 44)
                    throw new InvalidOperationException("Budget choice lost its native 44-pixel target.");
                var peer = new Microsoft.UI.Xaml.Automation.Peers.ButtonAutomationPeer(native);
                if (button.CommandParameter is ActiveItemRow row && (peer.GetName() != row.AccessibilityText || native.IsEnabled != row.CanToggle))
                    throw new InvalidOperationException("Budget choice lost complete original native name or eligibility.");
                observations.Add(new { scenario, Name = peer.GetName(), native.ActualWidth, native.ActualHeight, native.IsEnabled });
            }
            if (await stored() != before) throw new InvalidOperationException("Budget presentation or failed Save changed complete stored rows.");
            await CaptureAsync(app, folder, language + "-active-budget-definitions-" + scenario);
        }
        await CheckAsync("original"); await ToggleAsync(usd); await ToggleAsync(eur);
        if (!vm.OverCapacity || vm.CanSave || vm.CapacityError is null) throw new InvalidOperationException("Native budget capacity did not guard explicit Save.");
        await viewport.ScrollToAsync(0, 0, false); await Task.Delay(200); await CheckAsync("capacity");
        await ToggleAsync(eur);
        if (!usd.IsSelected || eur.IsSelected || !vm.CanSave) throw new InvalidOperationException("Native definition draft lost the explicit single choice.");
        await using var db = await factory.CreateDbContextAsync();
        await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER FictitiousBudgetChoiceFailure BEFORE INSERT ON ResourceSelections BEGIN SELECT RAISE(ABORT, 'Fictitious budget choice failure'); END;");
        try
        {
            await viewport.ScrollToAsync(0, viewport.ContentSize.Height, false); await Task.Delay(200);
            InvokeWrappingAction(page, vm.SaveCommand); if (vm.SaveCommand.ExecutionTask is { } rejected) await rejected;
            await Task.Delay(250);
            if (!usd.IsSelected || !vm.IsDirty || !vm.CanSave || vm.Feedback != Translator.Instance["ActiveItems_SaveFailed"])
                throw new InvalidOperationException("Rejected native definition Save lost its exact draft and retry feedback.");
            await CheckAsync("rejected-save");
        }
        finally { await db.Database.ExecuteSqlRawAsync("DROP TRIGGER IF EXISTS FictitiousBudgetChoiceFailure;"); }
        InvokeWrappingAction(page, vm.SaveCommand); if (vm.SaveCommand.ExecutionTask is { } saved) await saved;
        await Task.Delay(300);
        if (ReferenceEquals(Shell.Current.CurrentPage, page) || vm.IsDirty || vm.CanSave)
            throw new InvalidOperationException("Committed native choice did not retire its review and return.");
        var after = JsonSerializer.Deserialize<SortedDictionary<string, List<string>>>(await stored())!;
        var original = JsonSerializer.Deserialize<SortedDictionary<string, List<string>>>(before)!;
        if (after["ResourceSelections"].Count != 1 || !after["ResourceSelections"][0].Contains("USD|0|", StringComparison.Ordinal)
            || after.Any(table => table.Key != "ResourceSelections" && !table.Value.SequenceEqual(original[table.Key])))
            throw new InvalidOperationException("Explicit native choice changed financial rows or saved a row id instead of definition identity.");
        // Remove only the choice created above in this exact independent review file; retain all original financial packets.
        await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM ResourceSelections WHERE Kind = {(int)Core.Commerce.QuotaKind.BudgetDefinitions};");
        if (await stored() != before) throw new InvalidOperationException("Independent budget review did not restore all 25 tables.");
        File.WriteAllText(Path.Combine(folder, language + "-active-budget-definitions-proof.json"), JsonSerializer.Serialize(new
        {
            ActualNativeFourthKindAndRows = true, CopiesShareDefinition = true, HistoryAndArchivedScopePreserved = true,
            OriginalPaidMethodShownWithoutGrant = true, CapacityAndRejectedSaveRetainDraft = true,
            ExplicitCanonicalSaveOnly = true, Complete25TablesRestored = true, NativeReturn = true,
            IndependentFixtureOnly = true, observations,
        }));
    }
}
#endif
