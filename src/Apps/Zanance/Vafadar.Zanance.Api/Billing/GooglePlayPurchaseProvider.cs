using System.Text.Json;
using Vafadar.Zanance.Core.Commerce;
using static Vafadar.Zanance.Api.Billing.BillingProviderFields;

namespace Vafadar.Zanance.Api.Billing;

/// <summary>Actual Google Developer API verification, never client-local success or an order-id assertion.</summary>
internal sealed class GooglePlayPurchaseProvider(BillingProviderHttp http, BillingCatalog catalog,
    IGoogleBillingCredentials credentials, TimeProvider time) : IBillingProvider
{
    private const string Package = "pro.vafadar.zanance";
    /// <inheritdoc />
    public BillingIssuer Issuer => BillingIssuer.GooglePlay;

    /// <inheritdoc />
    public async Task<BillingVerifiedPurchase> VerifyAsync(string reference, BillingProduct hint, BillingEnvironment environment, CancellationToken ct)
    {
        if (!Enum.IsDefined(environment) || !Enum.IsDefined(hint) || string.IsNullOrEmpty(reference) || reference.Length > 8192 || reference.Any(char.IsControl))
            throw new BillingRefusal(422, "StorePurchaseRefused");
        var oneTime = hint == BillingProduct.PlusLifetime;
        var resource = oneTime ? "productsv2/tokens/" : "subscriptionsv2/tokens/";
        using var document = await http.SendAsync(Endpoint(resource + Uri.EscapeDataString(reference)), await credentials.AccessAsync(ct), false, ct);
        var row = document.RootElement;
        var test = row.TryGetProperty(oneTime ? "testPurchaseContext" : "testPurchase", out var testContext) && testContext.ValueKind == JsonValueKind.Object;
        if (test != (environment == BillingEnvironment.Sandbox)) throw new BillingRefusal(422, "StoreEnvironmentMismatch");
        try { return oneTime ? ReadOneTime(row, reference, environment) : ReadSubscription(row, reference, environment); }
        catch (Exception error) when (error is KeyNotFoundException or InvalidOperationException or ArgumentException or FormatException)
        { throw new BillingRefusal(422, "StoreResponseRefused"); }
    }

    /// <inheritdoc />
    public async Task AcknowledgeAsync(BillingVerifiedPurchase purchase, CancellationToken ct)
    {
        if (purchase.Issuer != Issuer) throw new BillingRefusal(422, "StorePurchaseRefused");
        if (purchase.Facts.Acknowledged || purchase.Facts.State is not (BillingPurchaseState.Active or BillingPurchaseState.Grace)) return;
        var mapping = catalog.Read(Issuer, purchase.Environment).FirstOrDefault(row => row.Product == purchase.Facts.Product && row.StoreProductId == purchase.StoreProductId)
            ?? throw new BillingRefusal(422, "UnsupportedStoreProduct");
        var resource = purchase.Facts.Product == BillingProduct.PlusLifetime ? "products/" : "subscriptions/";
        using var reply = await http.SendAsync(Endpoint(resource + Uri.EscapeDataString(mapping.StoreProductId) + "/tokens/" +
            Uri.EscapeDataString(purchase.Reference) + ":acknowledge"), await credentials.AccessAsync(ct), true, ct);
    }

    /// <summary>The fixed API package path is unrelated to an untrusted application id or return URL.</summary>
    private static Uri Endpoint(string resource) => new("https://androidpublisher.googleapis.com/androidpublisher/v3/applications/" + Package + "/purchases/" + resource);

    /// <summary>Accepts only the approved non-consumable, with actual provider account binding and payment state.</summary>
    private BillingVerifiedPurchase ReadOneTime(JsonElement row, string reference, BillingEnvironment environment)
    {
        var lines = row.GetProperty("productLineItem");
        if (lines.ValueKind != JsonValueKind.Array || lines.GetArrayLength() != 1) throw new BillingRefusal(422, "UnsupportedStorePurchase");
        var line = lines[0]; var mapping = catalog.Resolve(Issuer, environment, Text(line, "productId"), null);
        if (mapping.Product != BillingProduct.PlusLifetime) throw new BillingRefusal(422, "UnsupportedStorePurchase");
        var offer = Object(line, "productOfferDetails");
        if (offer.TryGetProperty("rentOfferDetails", out _) || offer.TryGetProperty("preorderOfferDetails", out _) ||
            offer.TryGetProperty("quantity", out var quantity) && (!quantity.TryGetInt32(out var count) || count != 1))
            throw new BillingRefusal(422, "UnsupportedStorePurchase");
        var state = Text(Object(row, "purchaseStateContext"), "purchaseState") switch
        {
            "PURCHASED" => BillingPurchaseState.Active, "PENDING" => BillingPurchaseState.Pending,
            "CANCELLED" => BillingPurchaseState.Revoked, _ => throw new BillingRefusal(422, "StoreStateRefused"),
        };
        var started = state == BillingPurchaseState.Active ? Instant(row, "purchaseCompletionTime")
            : row.TryGetProperty("purchaseCompletionTime", out _) ? Instant(row, "purchaseCompletionTime") : DateTimeOffset.UnixEpoch;
        if (started > time.GetUtcNow().AddMinutes(1)) throw new BillingRefusal(422, "StoreTimeRefused");
        var facts = new BillingPurchaseFacts(mapping.Product, state, started, null, null, false, false,
            Text(row, "acknowledgementState") == "ACKNOWLEDGEMENT_STATE_ACKNOWLEDGED", mapping.TermsVersion);
        facts.Validate(); return new(Issuer, environment, reference, Text(row, "obfuscatedExternalAccountId", 64), facts, null, StoreProductId: mapping.StoreProductId);
    }

    /// <summary>Cancellation preserves the paid end; hold, pause and unknown states cannot manufacture access.</summary>
    private BillingVerifiedPurchase ReadSubscription(JsonElement row, string reference, BillingEnvironment environment)
    {
        var state = Text(row, "subscriptionState") switch
        {
            "SUBSCRIPTION_STATE_PENDING" => BillingPurchaseState.Pending,
            "SUBSCRIPTION_STATE_PENDING_PURCHASE_CANCELED" => BillingPurchaseState.Revoked,
            "SUBSCRIPTION_STATE_ACTIVE" or "SUBSCRIPTION_STATE_CANCELED" => BillingPurchaseState.Active,
            "SUBSCRIPTION_STATE_IN_GRACE_PERIOD" => BillingPurchaseState.Grace,
            "SUBSCRIPTION_STATE_ON_HOLD" => BillingPurchaseState.Hold,
            "SUBSCRIPTION_STATE_PAUSED" => BillingPurchaseState.Paused,
            "SUBSCRIPTION_STATE_EXPIRED" => BillingPurchaseState.Expired,
            _ => throw new BillingRefusal(422, "StoreStateRefused"),
        };
        var all = row.GetProperty("lineItems");
        if (all.ValueKind != JsonValueKind.Array || all.GetArrayLength() is < 1 or > 4) throw new BillingRefusal(422, "UnsupportedStorePurchase");
        // A deferred replacement without an actual successful order is not a second current paid item.
        var candidates = all.EnumerateArray().Where(line => OptionalText(line, "latestSuccessfulOrderId") is not null).ToArray();
        if (state is BillingPurchaseState.Pending or BillingPurchaseState.Revoked && candidates.Length == 0 && all.GetArrayLength() == 1) candidates = [all[0]];
        if (candidates.Length != 1) throw new BillingRefusal(422, "UnsupportedStorePurchase");
        var line = candidates[0]; var details = Object(line, "offerDetails");
        var mapping = catalog.Resolve(Issuer, environment, Text(line, "productId"), Text(details, "basePlanId"));
        var start = row.TryGetProperty("startTime", out _) ? Instant(row, "startTime")
            : state is BillingPurchaseState.Pending or BillingPurchaseState.Revoked ? DateTimeOffset.UnixEpoch
                : throw new BillingRefusal(422, "StoreResponseRefused");
        var expiry = line.TryGetProperty("expiryTime", out _) ? Instant(line, "expiryTime") : (DateTimeOffset?)null;
        var renews = false;
        if (line.TryGetProperty("autoRenewingPlan", out var auto))
        {
            if (auto.ValueKind != JsonValueKind.Object) throw new BillingRefusal(422, "StoreResponseRefused");
            if (auto.TryGetProperty("installmentDetails", out _)) throw new BillingRefusal(422, "UnsupportedStorePurchase");
            // Protobuf JSON may omit a false scalar; absence does not imply renewal or a new paid interval.
            if (auto.TryGetProperty("autoRenewEnabled", out var renew))
            {
                if (renew.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) throw new BillingRefusal(422, "StoreResponseRefused");
                renews = renew.GetBoolean();
            }
        }
        else if (state is not (BillingPurchaseState.Pending or BillingPurchaseState.Revoked))
            throw new BillingRefusal(422, "UnsupportedStorePurchase");
        var trial = false; var trialKnown = false;
        if (line.TryGetProperty("offerPhase", out var phase))
        {
            if (phase.ValueKind != JsonValueKind.Object || phase.EnumerateObject().Count() != 1) throw new BillingRefusal(422, "UnsupportedStorePhase");
            trial = phase.TryGetProperty("freeTrial", out _);
            if (!trial && !phase.TryGetProperty("basePrice", out _) && !phase.TryGetProperty("introductoryPrice", out _) && !phase.TryGetProperty("prorationPeriod", out _))
                throw new BillingRefusal(422, "UnsupportedStorePhase");
            trialKnown = true;
        }
        BillingScheduledChange? scheduled = null;
        if (state == BillingPurchaseState.Active && line.TryGetProperty("deferredItemReplacement", out var replacement))
        {
            var productId = Text(replacement, "productId");
            var future = all.EnumerateArray().Where(item => OptionalText(item, "productId") == productId &&
                OptionalText(item, "latestSuccessfulOrderId") is null && !item.TryGetProperty("expiryTime", out _)).ToArray();
            if (future.Length != 1 || expiry is null || future[0].TryGetProperty("prepaidPlan", out _) ||
                future[0].TryGetProperty("autoRenewingPlan", out var futurePlan) && futurePlan.TryGetProperty("installmentDetails", out _))
                throw new BillingRefusal(422, "UnsupportedDeferredStorePurchase");
            var target = catalog.Resolve(Issuer, environment, productId, Text(Object(future[0], "offerDetails"), "basePlanId"));
            scheduled = new(target.Product, expiry.Value, target.TermsVersion);
        }
        var facts = new BillingPurchaseFacts(mapping.Product, state, start, state == BillingPurchaseState.Grace ? null : expiry,
            state == BillingPurchaseState.Grace ? expiry : null, renews, trial,
            Text(row, "acknowledgementState") == "ACKNOWLEDGEMENT_STATE_ACKNOWLEDGED", mapping.TermsVersion, trialKnown, scheduled);
        facts.Validate();
        return new(Issuer, environment, reference, Text(Object(row, "externalAccountIdentifiers"), "obfuscatedExternalAccountId", 64),
            facts, OptionalText(row, "linkedPurchaseToken", 8192), StoreProductId: mapping.StoreProductId);
    }

}
