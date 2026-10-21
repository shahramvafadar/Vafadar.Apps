#if (DEBUG && WINDOWS) || ZANANCE_INDEPENDENT_HOME_REVIEW
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Sharing;
using Vafadar.Zanance.App.Features.Sync;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Core.Sharing;
using Vafadar.Zanance.Data.Sharing;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.App.Diagnostics;

internal sealed partial class PlanEditorNativeReview
{
    /// <summary>Gets original complete cost, repayment and individual allocation captions from actual production models.</summary>
    internal static HashSet<string> SecondaryHouseholdCaptions(Page page) => page.BindingContext is HouseholdCostsViewModel model
        ? new[] { model.AmountUnitName, model.AmountUnitNote, model.AmountFormatNote, model.AmountContextError, model.AmountError, model.SharesError }
            .Concat(model.Shares.SelectMany(row => new[] { row.UnitName, row.UnitNote, row.FormatNote, row.Error }))
            .Where(value => !string.IsNullOrEmpty(value)).Select(value => value!).ToHashSet(StringComparer.Ordinal) : [];
    /// <summary>Gets only actual original shared monetary draft text, never a manufactured native control.</summary>
    internal static HashSet<string> SecondaryHouseholdRaw(Page page) => page.BindingContext is HouseholdCostsViewModel model
        ? model.Shares.Select(row => row.Text).Append(model.AmountText).ToHashSet(StringComparer.Ordinal) : [];
    /// <summary>Reviews real original protected cost and repayment pages without new financial writes or replacement membership.</summary>
    internal async Task SecondaryHouseholdContextAsync(IServiceProvider protectedSource, Action validate, string context,
        Func<Page, System.Windows.Input.ICommand, bool?, Task> invoke, Func<Page, string, Task> observe, CancellationToken ct, Func<string, bool>? completed = null, bool freshRepayment = false)
    {
        var units = DisplayUnits.All; var culture = Localization.FormattingCultureName; string? route = null; var pendingDisplay = false;
        var before = await ImportFinancialNativeReview.StoredAsync(protectedSource, ct);
        var store = protectedSource.GetRequiredService<HouseholdCostStore>();
        try
        {
            foreach (var repayment in new[] { false, true })
            {
                var key = repayment ? "shared-repayment" : "shared-cost";
                if (completed?.Invoke(context + "-" + key + "-unit-feedback") == true) continue;
                DisplayUnits.Set([]); await SetTabMoneyCultureAsync("en-US", ct);
                var model = new HouseholdCostsViewModel(store, new SecondaryHouseholdEffects(validate), new MoneyInteraction(), Localization,
                    new DateFormatter(Localization), Translator, Time);
                var page = new HouseholdCostsPage(model); route = await ShowAsync(page);
                await WaitAsync(() => model.CanWrite && page.Width > 0, ct);
                if (repayment && freshRepayment)
                {
                    // A missing saved repayment is not permission to seed one. Review the actual new unsaved editor instead.
                    await model.NewSettlementCommand.ExecuteAsync(null);
                    model.Payer = model.Contacts.First(); model.Recipient = model.Contacts.Skip(1).First();
                }
                else if (repayment) await model.EditSettlementCommand.ExecuteAsync(model.Settlements.First());
                else await model.EditCostCommand.ExecuteAsync(model.Costs.First(row => row.Source.Kind == HouseholdCostKind.Expense));
                model.AmountText = "1,234";
                foreach (var share in model.Shares) share.Text = "1,234";
                Localization.SetFormattingCulture("de-DE"); pendingDisplay = true;
                if (model.Shares.Count > 0 && model.Shares.Any(row => row.FormatNote is null)) throw new IOException("Original shared allocation parser note missing.");
                if (!repayment && completed?.Invoke(context + "-" + key + "-original") != true) await observe(page, context + "-" + key + "-original");
                DisplayUnits.Set([new(model.CurrencyCode, "Hundred " + model.CurrencyCode, 2)]);
                await invoke(page, model.SaveCommand, null);
                if (model.SaveCommand.ExecutionTask is { } pending) await pending.WaitAsync(ct);
                if (model.AmountContextError is null || !model.HasEditor || !model.CanWrite || before != await ImportFinancialNativeReview.StoredAsync(protectedSource, ct))
                    throw new IOException("Original shared cost/repayment stale unit did not remain field-local and unchanged.");
                await observe(page, context + "-" + key + "-unit-feedback");
                await ReturnMoneyTabAsync(pendingDisplay, ct); pendingDisplay = false; Routing.UnRegisterRoute(route); _routes.Remove(route); route = null;
            }
        }
        finally
        {
            await ReturnMoneyTabAsync(pendingDisplay, ct);
            if (route is not null) { Routing.UnRegisterRoute(route); _routes.Remove(route); }
            DisplayUnits.Set(units); await SetTabMoneyCultureAsync(culture, ct);
        }
    }
    /// <summary>Provides only foreground lifetime over an already protected fictional source; every network/publication path refuses.</summary>
    private sealed class SecondaryHouseholdEffects(Action validate) : IPersonalSyncEnvironment
    {
        /// <inheritdoc />
        public event EventHandler? StatusChanged { add { } remove { } }
        /// <inheritdoc />
        public bool Available => true;
        /// <inheritdoc />
        public PersonalSyncAccess Capture() => new(validate, CancellationToken.None, "original-protected-household-native-review");
        /// <inheritdoc />
        public PersonalSyncConnection ReadConnection() => throw new IOException("No network connection belongs to monetary presentation.");
        /// <inheritdoc />
        public PersonalSyncTransport Open(LocalSyncPeer peer, Action verifyAccess) => throw new IOException("No transport belongs to monetary presentation.");
        /// <inheritdoc />
        public Task PublishAppliedAsync(CancellationToken ct) => throw new IOException("No accepted publication belongs to read-only monetary presentation.");
        /// <inheritdoc />
        public void RememberPeer(LocalSyncPeer peer) => throw new IOException();
        /// <inheritdoc />
        public void ConnectionChanged() => throw new IOException();
        /// <inheritdoc />
        public Guid CreationOperation(string requestDigest) => throw new IOException();
        /// <inheritdoc />
        public void CompleteCreation(string requestDigest, Guid operation) => throw new IOException();
        /// <inheritdoc />
        public Task<PersonalSyncRenderedPage> RenderAttachmentPageAsync(PersonalSyncAttachmentContent content, int pageIndex, CancellationToken ct) => throw new IOException();
    }
}
#endif
