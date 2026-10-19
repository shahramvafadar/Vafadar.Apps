using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Zanance.App.Features.Commerce.Billing;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Money;

namespace Vafadar.Zanance.App.Features.Commerce;

/// <summary>Actual store choice with all original provider price phases.</summary>
public sealed record BillingOfferRow(NativeBillingOffer Original, string Title, string Terms, string Action);

public sealed partial class ProductPlansViewModel
{
    private readonly IProductBillingEnvironment? _billing;
    private ProductBillingPresentation? _billingPresentation;
    private CancellationTokenSource? _billingPage;
    private long _billingEpoch;
    private bool _billingReview;
    /// <summary>Only the selected native mobile edition exposes payment controls.</summary>
    public bool ShowBilling => _billing?.Available == true;
    /// <summary>Store work has its own covered surface; the comparison remains readable.</summary>
    [ObservableProperty] public partial bool BillingBusy { get; private set; }
    /// <summary>Content-free translated current outcome.</summary>
    [ObservableProperty] public partial string BillingStatusText { get; private set; } = "";
    /// <summary>Actual eligible store choices, without design prices or synthetic checkout.</summary>
    [ObservableProperty] public partial IReadOnlyList<BillingOfferRow> BillingOffers { get; private set; } = [];
    /// <summary>Original verified purchase facts, separately scoped to device and identity; never an entitlement grant.</summary>
    public IReadOnlyList<BillingOwnedPurchaseRow> BillingOwnedPurchases => OwnedPurchases();
    /// <summary>Only verified ready state can start a new native payment.</summary>
    public bool CanBuyBilling => ShowBilling && _billingPage is not null && !_billingReview && !BillingBusy && _billingPresentation?.Status == ProductBillingStatus.Ready;
    /// <summary>Actual owned-purchase restoration never launches payment.</summary>
    public bool CanRestoreBilling => ShowBilling && _billingPage is not null && !_billingReview && !BillingBusy && _billingPresentation?.CanRestore == true;
    /// <summary>Uncertain original work requires continuation.</summary>
    public bool CanContinueBilling => ShowBilling && _billingPage is not null && !_billingReview && !BillingBusy && _billingPresentation?.Status == ProductBillingStatus.Pending;
    /// <summary>Management does not depend on a verifier outage.</summary>
    public bool CanManageBilling => ShowBilling && _billingPage is not null && !_billingReview && !BillingBusy;
    /// <summary>A retired or unavailable read can be retried without inventing an original payment.</summary>
    public bool CanRetryBilling => CanManageBilling && _billingPresentation?.Status is ProductBillingStatus.VerificationUnavailable or ProductBillingStatus.NotConfigured;
    /// <summary>Payment evidence remains separate from inactive commercial quotas.</summary>
    public string BillingPlanText => _billingPresentation is { } state && (state.Local?.Snapshot is not null || state.Identity?.Snapshot is not null)
        ? _translator.Format("Billing_VerifiedPlan", state.Plan.ToString()) : _translator["Billing_NoVerifiedState"];
    /// <summary>Store Plus remains account-free; Pro uses actual identity.</summary>
    public string BillingIdentityText => _translator[_billingPresentation?.ProRestoreRequired == true ? "Billing_ProRestoreRequired" :
        _billingPresentation?.HasIdentity == true ? "Billing_IdentityPresent" : "Billing_IdentityOptional"];
    /// <summary>Retires observation without deleting original protected intent or an outstanding SDK task.</summary>
    public void DeactivateBilling()
    {
        _billingEpoch++; _billingPage?.Cancel(); _billingPage?.Dispose(); _billingPage = null; BillingOffers = []; BillingBusy = false; _billingPresentation = null; OnPropertyChanged(nameof(BillingOwnedPurchases));
    }
    private async Task LoadBillingAsync()
    {
        if (!ShowBilling) return;
        _billingEpoch++; _billingPage?.Cancel(); _billingPage?.Dispose(); _billingPage = new(); BillingBusy = false;
        await RunBillingAsync(token => _billing!.LoadAsync(token));
    }
    partial void OnBillingBusyChanged(bool value) => NotifyBillingCommands();
    private void NotifyBillingCommands()
    {
        foreach (var name in new[] { nameof(CanBuyBilling), nameof(CanRestoreBilling), nameof(CanContinueBilling), nameof(CanManageBilling), nameof(CanRetryBilling) }) OnPropertyChanged(name);
        BuyBillingCommand.NotifyCanExecuteChanged(); RestoreBillingCommand.NotifyCanExecuteChanged(); ContinueBillingCommand.NotifyCanExecuteChanged(); ManageBillingCommand.NotifyCanExecuteChanged(); BillingIdentityCommand.NotifyCanExecuteChanged();
        RetryBillingCommand.NotifyCanExecuteChanged();
    }
    private void RefreshBillingDisplay()
    {
        if (!ShowBilling) return;
        BillingStatusText = _translator[BillingBusy ? "Billing_Loading" : _billingPresentation?.OfferChanged == true ? "Billing_OfferChanged" :
            "Billing_Status_" + (_billingPresentation?.Status.ToString() ?? "VerificationUnavailable")];
        BillingOffers = _billingPresentation?.Offers.Select(offer => new BillingOfferRow(offer, offer.Title,
            Terms(offer), _translator["Billing_ContinueStore"])).ToArray() ?? [];
        OnPropertyChanged(nameof(BillingPlanText)); OnPropertyChanged(nameof(BillingIdentityText)); OnPropertyChanged(nameof(BillingOwnedPurchases)); NotifyBillingCommands();
    }
    private string Terms(NativeBillingOffer offer)
    {
        var terms = string.Join(Environment.NewLine, offer.Offer.Phases.Select(Phase));
        if (offer.Replacement is { } replacement)
        {
            // Renewal dates use the current regional/calendar formatter; the app never estimates a store credit.
            var date = _billingDates?.Format(replacement.CurrentPaidUntil) ?? throw new InvalidOperationException("Billing date formatter required.");
            terms += Environment.NewLine + _translator.Format("Billing_Replacement_" + replacement.Timing,
                BillingProducts.Plan(replacement.FromProduct).ToString(), date);
        }
        else if (offer.ExistingSubscription) terms += Environment.NewLine + _translator["Billing_LifetimeKeepsSubscription"];
        return terms + Environment.NewLine + _translator["Market_EcbFreeOriginal"];
    }
    private string Phase(BillingPricePhase phase)
    {
        var period = _translator[phase.Period switch { "P14D" => "Billing_PeriodTrial", "P1M" => "Billing_PeriodMonth", "P1Y" => "Billing_PeriodYear", _ => "Billing_PeriodOnce" }];
        return _translator.Format(phase.Recurring ? "Billing_PhaseRenewal" : phase.Period.Length == 0 ? "Billing_PhaseOnce" : "Billing_PhaseIntro",
            MoneyText.ProviderPrice(phase.FormattedPrice), period);
    }
    private async Task RunBillingAsync(Func<CancellationToken, Task<ProductBillingPresentation>> operation, bool retainedWork = false)
    {
        if (!ShowBilling || BillingBusy || _billingPage is null) return;
        var epoch = _billingEpoch; var token = _billingPage.Token;
        BillingBusy = true; BillingOffers = []; BillingStatusText = _translator["Billing_Loading"];
        try
        {
            var result = await operation(token);
            if (epoch == _billingEpoch && !token.IsCancellationRequested) _billingPresentation = result;
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested)
        {
            // An interrupted read is not evidence that payment started. Actual native work keeps its original continuation.
            if (epoch == _billingEpoch) _billingPresentation = new(retainedWork ? ProductBillingStatus.Pending : ProductBillingStatus.VerificationUnavailable,
                null, null, [], retainedWork, false);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception) when (token.IsCancellationRequested) { }
        catch (Exception) when (!token.IsCancellationRequested)
        {
            // Never pass provider bodies, signed receipts, SDK diagnostics or credentials to a general logger.
            if (epoch == _billingEpoch) _billingPresentation = new(ProductBillingStatus.VerificationUnavailable, null, null, [], false, false);
        }
        finally { if (epoch == _billingEpoch) { BillingBusy = false; RefreshBillingDisplay(); } }
    }
    [RelayCommand(CanExecute = nameof(CanBuyBilling))]
    private async Task BuyBillingAsync(BillingOfferRow? row)
    {
        if (row is null || !BillingOffers.Contains(row)) return;
        var epoch = _billingEpoch;
        bool accepted; _billingReview = true; NotifyBillingCommands();
        try
        {
            accepted = await _interaction.ConfirmAsync(_translator["Billing_ReviewTitle"], row.Title + Environment.NewLine + row.Terms +
                Environment.NewLine + _translator["Billing_ReviewTerms"], _translator["Billing_ContinueStore"], _translator["Common_Cancel"]);
        }
        finally { _billingReview = false; NotifyBillingCommands(); }
        if (!accepted || epoch != _billingEpoch || !CanBuyBilling) return;
        await RunBillingAsync(token => _billing!.PurchaseAsync(row.Original, token), retainedWork: true);
    }
    [RelayCommand(CanExecute = nameof(CanRestoreBilling))]
    private async Task RestoreBillingAsync()
    {
        var epoch = _billingEpoch;
        bool accepted; _billingReview = true; NotifyBillingCommands();
        try
        {
            accepted = await _interaction.ConfirmAsync(_translator["Billing_Restore"], _translator["Billing_RestoreExplanation"],
                _translator["Billing_Restore"], _translator["Common_Cancel"]);
        }
        finally { _billingReview = false; NotifyBillingCommands(); }
        if (!accepted || epoch != _billingEpoch || !CanRestoreBilling) return;
        await RunBillingAsync(token => _billing!.RestoreAsync(token), retainedWork: true);
    }
    [RelayCommand(CanExecute = nameof(CanContinueBilling))]
    private Task ContinueBillingAsync() => RunBillingAsync(token => _billing!.ContinueAsync(token), retainedWork: true);
    [RelayCommand(CanExecute = nameof(CanRetryBilling))]
    private Task RetryBillingAsync() => RunBillingAsync(token => _billing!.LoadAsync(token));
    [RelayCommand(CanExecute = nameof(CanManageBilling))]
    private Task BillingIdentityAsync() => _interaction.NavigateAsync(AppRoutes.BillingIdentityRoute);
    [RelayCommand(CanExecute = nameof(CanManageBilling))]
    private async Task ManageBillingAsync()
    {
        if (_billingPage is not { } page) return;
        try { await _billing!.ManageAsync(page.Token); }
        catch (OperationCanceledException) when (page.IsCancellationRequested) { }
        catch (Exception) when (!page.IsCancellationRequested) { BillingStatusText = _translator["Billing_Status_VerificationUnavailable"]; }
    }
}
