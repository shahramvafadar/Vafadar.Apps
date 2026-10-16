#if BILLING
using Android.BillingClient.Api;
using Vafadar.Zanance.App.Features.Commerce.Billing;
using Vafadar.Zanance.Core.Commerce;
using NativePurchase = Android.BillingClient.Api.Purchase;

namespace Vafadar.Zanance.App.Platforms.Android;

/// <summary>Current official Play SDK effects, with one retained native payment and no local entitlement invention.</summary>
internal sealed class GooglePlayBillingStore : Java.Lang.Object, IBillingNativeStore, IPurchasesUpdatedListener, IBillingClientStateListener
{
    private readonly SemaphoreSlim _queries = new(1);
    private readonly Dictionary<string, (ProductDetails Details, string? Token, BillingProduct Product)> _offers = [];
    private BillingClient? _client;
    private TaskCompletionSource<BillingResult>? _connection;
    private TaskCompletionSource<IReadOnlyList<NativeBillingPurchase>>? _payment;
    private BillingProduct? _paymentProduct;
    private Guid? _paymentIntent;
    private string? _paymentStoreProduct;
    private string? _deferredOriginalStoreProduct;
    /// <inheritdoc />
    public BillingIssuer Distribution => BillingIssuer.GooglePlay;
    /// <inheritdoc />
    public event EventHandler? PurchasesChanged;

    /// <summary>Creates the native client only after explicit page work, without a startup connection or database effect.</summary>
    private async Task<BillingClient> ConnectedAsync(CancellationToken ct)
    {
        await _queries.WaitAsync(ct);
        Task<BillingResult>? waiting = null;
        try
        {
            _client ??= BillingClient.NewBuilder(global::Android.App.Application.Context)!
                .SetListener(this)!.EnablePendingPurchases(PendingPurchasesParams.NewBuilder()!.EnableOneTimeProducts()!.Build()!)!
                .EnableAutoServiceReconnection()!.Build()!;
            if (_client.IsReady) return _client;
            if (_connection is null || _connection.Task.IsCompleted)
            {
                _connection = new(TaskCreationOptions.RunContinuationsAsynchronously);
                _client.StartConnection(this);
            }
            waiting = _connection.Task;
        }
        finally { _queries.Release(); }
        // Cancellation retires this observer only. An outstanding SDK connection is never started again because a UI timer elapsed.
        RequireSuccess(await waiting.WaitAsync(ct)); return _client;
    }
    /// <inheritdoc />
    public void OnBillingSetupFinished(BillingResult billingResult) => _connection?.TrySetResult(billingResult);
    /// <inheritdoc />
    public void OnBillingServiceDisconnected()
    { _connection?.TrySetException(new NativeBillingRefusal("StoreDisconnected")); }

    /// <inheritdoc />
    public async Task<string> CountryAsync(CancellationToken ct)
    {
        var client = await ConnectedAsync(ct);
        var completion = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var listener = new CountryListener(completion);
        client.GetBillingConfigAsync(GetBillingConfigParams.NewBuilder()!.Build()!, listener);
        // The SDK callback owns the listener until completion, even if this screen stops observing it.
        return await completion.Task.WaitAsync(ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<NativeBillingOffer>> CatalogAsync(IReadOnlyList<BillingProductMapping> catalog, CancellationToken ct)
    {
        if (catalog.Count > 85) throw new NativeBillingRefusal("StoreCatalogBound");
        var client = await ConnectedAsync(ct); await _queries.WaitAsync(ct);
        try
        {
            if (_payment is { Task.IsCompleted: false }) throw new NativeBillingRefusal("OriginalStorePaymentPending");
            var current = catalog.Where(p => p.AvailableForPurchase).ToArray();
            var rows = new List<NativeBillingOffer>(); _offers.Clear();
            foreach (var group in current.GroupBy(p => p.Product == BillingProduct.PlusLifetime ? "inapp" : "subs"))
            {
                var queryProducts = group.Select(p => p.StoreProductId).Distinct(StringComparer.Ordinal).Select(id =>
                    QueryProductDetailsParams.Product.NewBuilder()!.SetProductId(id)!.SetProductType(group.Key)!.Build()!).ToList();
                var response = await client.QueryProductDetailsAsync(QueryProductDetailsParams.NewBuilder()!.SetProductList(queryProducts)!.Build()!);
                RequireSuccess(response.Result);
                foreach (var details in response.ProductDetailsList ?? [])
                {
                    foreach (var mapping in group.Where(p => p.StoreProductId == details.ProductId))
                    {
                        if (mapping.Product == BillingProduct.PlusLifetime)
                        {
                            foreach (var offer in details.OneTimePurchaseOfferDetailsList ?? [])
                            {
                                if (offer.GetRentalDetails() is not null || offer.GetPreorderDetails() is not null) continue;
                                var definition = Definition(mapping, offer.OfferId); if (definition is null) continue;
                                Add(details, mapping, definition, offer.OfferToken,
                                    [new(offer.PriceAmountMicros, offer.PriceCurrencyCode!, offer.FormattedPrice!, "", 1, false)]);
                            }
                        }
                        else
                        {
                            foreach (var offer in details.GetSubscriptionOfferDetails() ?? [])
                            {
                                if (offer.BasePlanId != mapping.BasePlanId || offer.InstallmentPlanDetails is not null) continue;
                                var definition = Definition(mapping, offer.OfferId); if (definition is null) continue;
                                var phases = offer.PricingPhases?.PricingPhaseList;
                                if (phases is null || phases.Count is < 1 or > 3) continue;
                                Add(details, mapping, definition, offer.OfferToken, phases.Select(phase => new BillingPricePhase(
                                    phase.PriceAmountMicros, phase.PriceCurrencyCode!, phase.FormattedPrice!, phase.BillingPeriod!,
                                    phase.BillingCycleCount, phase.RecurrenceMode == 1)).ToArray());
                            }
                        }
                    }
                }
            }
            ct.ThrowIfCancellationRequested(); return rows;

            void Add(ProductDetails details, BillingProductMapping mapping, BillingOfferDefinition definition, string? token, BillingPricePhase[] phases)
            {
                if (rows.Count >= 40) throw new NativeBillingRefusal("StoreCatalogBound");
                var handle = Guid.NewGuid().ToString("N");
                _offers.Add(handle, (details, token, mapping.Product));
                rows.Add(new(handle, details.Title!, details.Description!, new(handle, mapping.Product, definition.Kind, phases,
                    true, definition.StartsAt, definition.EndsAt, definition.Country),
                    mapping.StoreProductId + "\0" + mapping.BasePlanId + "\0" + definition.StoreOfferId, mapping.StoreProductId));
            }
        }
        finally { _queries.Release(); }
    }

    /// <summary>Only an ordinary base offer or an exact service-configured offer can be presented.</summary>
    private static BillingOfferDefinition? Definition(BillingProductMapping mapping, string? offerId)
        => string.IsNullOrEmpty(offerId) ? new("base", BillingOfferKind.Standard)
            : mapping.Offers?.SingleOrDefault(p => p.StoreOfferId == offerId);

    /// <inheritdoc />
    public async Task<IReadOnlyList<NativeBillingPurchase>> OwnedAsync(IReadOnlyList<BillingProductMapping> catalog, CancellationToken ct)
    {
        var client = await ConnectedAsync(ct); await _queries.WaitAsync(ct);
        try
        {
            var result = new List<NativeBillingPurchase>();
            foreach (var type in new[] { "inapp", "subs" })
            {
                var response = await client.QueryPurchasesAsync(QueryPurchasesParams.NewBuilder()!.SetProductType(type)!.IncludeSuspendedSubscriptions(true)!.Build()!);
                RequireSuccess(response.Result);
                foreach (var purchase in response.Purchases ?? [])
                {
                    if (purchase.PackageName != "pro.vafadar.zanance" || purchase.Products is not { Count: 1 } products)
                        throw new NativeBillingRefusal("UnsupportedOwnedSubscription");
                    var mapping = catalog.FirstOrDefault(p => p.StoreProductId == products[0] && (p.Product == BillingProduct.PlusLifetime) == (type == "inapp"));
                    if (mapping is null) throw new NativeBillingRefusal("UnsupportedOwnedSubscription");
                    result.Add(Read(purchase, mapping.Product));
                    if (result.Count > 32) throw new NativeBillingRefusal("StoreOwnedPurchaseBound");
                }
            }
            ct.ThrowIfCancellationRequested(); return result;
        }
        finally { _queries.Release(); }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<NativeBillingPurchase>> PurchaseAsync(Guid intent, string handle, string accountBinding, CancellationToken ct,
        NativeBillingReplacement? replacement = null)
    {
        if (intent == Guid.Empty || accountBinding.Length != 64 || !accountBinding.All(char.IsAsciiHexDigit)) throw new NativeBillingRefusal("StoreAccountBindingRequired");
        var client = await ConnectedAsync(ct); await _queries.WaitAsync(ct);
        Task<IReadOnlyList<NativeBillingPurchase>> waiting;
        try
        {
            if (_payment is { Task.IsCompleted: false }) throw new NativeBillingRefusal("OriginalStorePaymentPending");
            if (!_offers.TryGetValue(handle, out var selected)) throw new NativeBillingRefusal("StoreOfferChanged");
            var details = BillingFlowParams.ProductDetailsParams.NewBuilder()!.SetProductDetails(selected.Details)!;
            if (!string.IsNullOrEmpty(selected.Token)) details.SetOfferToken(selected.Token);
            var parametersBuilder = BillingFlowParams.NewBuilder()!.SetObfuscatedAccountId(accountBinding)!;
            if (replacement is not null)
            {
                // SDK 9.1 splits the retained purchase token from the product-level replacement terms.
                var mode = replacement.Timing switch
                {
                    BillingReplacementTiming.KeepBillingDate => BillingFlowParams.ProductDetailsParams.SubscriptionProductReplacementParams.ReplacementMode.WithoutProration,
                    BillingReplacementTiming.TimeCredit => BillingFlowParams.ProductDetailsParams.SubscriptionProductReplacementParams.ReplacementMode.WithTimeProration,
                    BillingReplacementTiming.Deferred => BillingFlowParams.ProductDetailsParams.SubscriptionProductReplacementParams.ReplacementMode.Deferred,
                    _ => throw new NativeBillingRefusal("StoreReplacementUnsupported"),
                };
                details.SetSubscriptionProductReplacementParams(BillingFlowParams.ProductDetailsParams.SubscriptionProductReplacementParams.NewBuilder()!
                    .SetOldProductId(replacement.OriginalStoreProductId)!.SetReplacementMode(mode)!.Build()!);
                parametersBuilder.SetSubscriptionUpdateParams(BillingFlowParams.SubscriptionUpdateParams.NewBuilder()!
                    .SetOldPurchaseToken(replacement.OriginalReference)!.Build()!);
            }
            var parameters = parametersBuilder.SetProductDetailsParamsList([details.Build()!])!.Build()!;
            _paymentIntent = intent; _paymentProduct = selected.Product; _paymentStoreProduct = selected.Details.ProductId;
            _deferredOriginalStoreProduct = replacement?.Timing == BillingReplacementTiming.Deferred ? replacement.OriginalStoreProductId : null;
            _payment = new(TaskCreationOptions.RunContinuationsAsynchronously); waiting = _payment.Task;
            var launched = await MainThread.InvokeOnMainThreadAsync(() =>
            {
                var activity = Platform.CurrentActivity ?? throw new NativeBillingRefusal("StoreActivityUnavailable");
                return client.LaunchBillingFlow(activity, parameters);
            });
            // Only the synchronous SDK launch result establishes that no purchase screen was launched.
            // Later callback/transport failures remain uncertain and must retain their original intent.
            if ((int)launched.ResponseCode != 0) _payment.TrySetException(new NativeBillingRefusal((int)launched.ResponseCode switch
                { 1 => "StoreCanceled", 7 => "StoreRestoreRequired", _ => "StoreNotLaunched" }));
        }
        catch { _payment?.TrySetException(new NativeBillingRefusal("StoreLaunchRefused")); throw; }
        finally { _queries.Release(); }
        // A retired observer does not cancel an actual payment or clear its original native task.
        return await waiting.WaitAsync(ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<NativeBillingPurchase>?> ContinuePaymentAsync(Guid intent, CancellationToken ct)
    {
        var original = _payment;
        if (_paymentIntent != intent || original is null) return null;
        return await original.Task.WaitAsync(ct);
    }

    /// <inheritdoc />
    public Task FinishAsync(NativeBillingPurchase purchase, BillingCommandResult verified, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (!verified.MayFinish || verified.SignedReceipt is null || verified.State is null or BillingPurchaseState.Pending)
            throw new NativeBillingRefusal("VerifiedStoreCompletionRequired");
        // Never consume the non-consumable or acknowledge before committed server verification.
        // Google's idempotent acknowledgement is durably continued by the service, including after this process ends.
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void OnPurchasesUpdated(BillingResult billingResult, IList<NativePurchase>? purchases)
    {
        if ((int)billingResult.ResponseCode == 0 && purchases is not null)
        {
            if (_paymentProduct is { } product && _paymentStoreProduct is { } storeProduct)
            {
                try
                {
                    var matching = purchases.Where(p => p.PackageName == "pro.vafadar.zanance" && p.Products is { Count: 1 } ids &&
                        (ids[0] == storeProduct || ids[0] == _deferredOriginalStoreProduct))
                        .Select(p => Read(p, product)).ToArray();
                    if (matching.Length != 0) _payment?.TrySetResult(matching);
                }
                catch (NativeBillingRefusal) { _payment?.TrySetException(new NativeBillingRefusal("StorePurchaseRefused")); }
            }
            PurchasesChanged?.Invoke(this, EventArgs.Empty);
        }
        else _payment?.TrySetException(new NativeBillingRefusal((int)billingResult.ResponseCode == 1 ? "StoreCanceled" : "StorePurchaseRefused"));
    }
    /// <summary>A native callback supplies a reference and pending hint only, never verified capabilities.</summary>
    private static NativeBillingPurchase Read(NativePurchase purchase, BillingProduct hint)
    {
        if (purchase.PurchaseToken is not { Length: > 0 and <= 8192 } token || token.Any(char.IsControl) ||
            (int)purchase.PurchaseState is not (1 or 2)) throw new NativeBillingRefusal("StorePurchaseRefused");
        return new(token, hint, (int)purchase.PurchaseState == 2, purchase.Products?.SingleOrDefault());
    }
    /// <summary>Raw SDK diagnostic messages never enter logs or translated UI.</summary>
    private static void RequireSuccess(BillingResult? result)
    { if (result is null || (int)result.ResponseCode != 0) throw new NativeBillingRefusal("StoreUnavailable"); }

    private sealed class CountryListener(TaskCompletionSource<string> completion) : Java.Lang.Object, IBillingConfigResponseListener
    {
        public void OnBillingConfigResponse(BillingResult billingResult, BillingConfig? billingConfig)
        {
            if ((int)billingResult.ResponseCode == 0 && billingConfig?.CountryCode is { Length: 2 } country && country.All(char.IsAsciiLetterUpper))
                completion.TrySetResult(country);
            else completion.TrySetException(new NativeBillingRefusal("StoreCountryUnavailable"));
        }
    }
}
#endif
