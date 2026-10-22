#if (DEBUG && WINDOWS) || ZANANCE_INDEPENDENT_HOME_REVIEW
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Features.Sharing;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Sharing;
using Vafadar.Zanance.Data.Sharing;

namespace Vafadar.Zanance.App.Diagnostics;

internal sealed partial class PlanEditorNativeReview
{
    /// <summary>Checks only the two changed original picker captions, with real protected membership and no financial command.</summary>
    internal async Task HouseholdCaptionContextAsync(IServiceProvider protectedSource, Action validate, string context,
        Func<Page, System.Windows.Input.ICommand, bool?, Task> invoke, Func<Page, Picker, string, bool, Task> observe, CancellationToken ct)
    {
        var store = protectedSource.GetRequiredService<HouseholdCostStore>();
        var before = await ImportFinancialNativeReview.StoredAsync(protectedSource, ct);
        var source = await store.ReadAsync(ct);
        if (!source.CanWrite || !source.Costs.Any(row => row.Kind == HouseholdCostKind.Expense))
            throw new IOException("Original protected household and existing expense required.");
        var model = new HouseholdCostsViewModel(store, new SecondaryHouseholdEffects(validate), new MoneyInteraction(), Localization,
            new Vafadar.Localization.Formatting.DateFormatter(Localization), Translator, Time);
        var page = new HouseholdCostsPage(model); var route = await ShowAsync(page);
        try
        {
            await WaitAsync(() => model.CanWrite && page.Width > 0, ct);
#if WINDOWS
            // The original PageHeader settles its first-focus/top request after 150ms. Observe after that
            // native appearance callback instead of racing it with a diagnostic scroll (D-471).
            await Task.Delay(250, ct);
#endif
            await model.NewCostCommand.ExecuteAsync(null); model.IsRefund = true;
            model.RefundOriginal = model.OriginalCosts.First();
            var refund = Views(page).OfType<Picker>().Single(picker => ReferenceEquals(picker.ItemsSource, model.OriginalCosts));
            await WaitAsync(() => refund.IsLoaded && refund.SelectedIndex >= 0, ct);
            await observe(page, refund, context + "-refund-source", false);
            await invoke(page, model.CancelEditCommand, null);
            await WaitAsync(() => !model.HasEditor, ct);

            await model.NewSettlementCommand.ExecuteAsync(null); model.UsesSharedCash = true;
            var movement = source.LinkedMovements.FirstOrDefault(row => row.Kind == EntryKind.Transfer && row.DestinationCurrencyCode is not null);
            var displayOnly = movement is null;
            if (movement is not null)
            {
                model.Date = movement.Date;
                await model.FindMovementsCommand.ExecuteAsync(null);
                model.Movement = model.Movements.Single(row => row.Source.Id == movement.Id);
            }
            else
            {
                // D-471: the retained Android source has no cash entries. A transient display packet exercises
                // the original formatter/control only; it is never a stored relationship or linkage acceptance.
                movement = new(Guid.NewGuid(), source.Costs.First().Date, EntryKind.Transfer, 1234, "EUR", 5678, "USD", "Fictitious caption preview");
                var formatter = typeof(HouseholdCostsViewModel).GetMethod("MovementCaption", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new IOException("Original movement caption formatter missing.");
                var text = (string?)formatter.Invoke(model, [movement]) ?? throw new IOException("Original movement caption missing.");
                var choice = new HouseholdMovementChoice(movement, text); model.Movements.Add(choice); model.Movement = choice;
            }
            var cash = Views(page).OfType<Picker>().Single(picker => ReferenceEquals(picker.ItemsSource, model.Movements));
            await WaitAsync(() => cash.IsLoaded && cash.SelectedIndex >= 0, ct);
            await observe(page, cash, context + "-cash-destination", displayOnly);
            await invoke(page, model.CancelEditCommand, null);
            await WaitAsync(() => !model.HasEditor, ct);
            if (before != await ImportFinancialNativeReview.StoredAsync(protectedSource, ct))
                throw new IOException("Household caption observation changed protected rows.");
        }
        finally
        {
            await ReturnMoneyTabAsync(false, ct); Routing.UnRegisterRoute(route); _routes.Remove(route);
        }
    }
}
#endif
