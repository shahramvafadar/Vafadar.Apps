using System.Text.Json;
using Vafadar.Zanance.Core.Commerce;
using static Vafadar.Zanance.Api.Billing.BillingProviderFields;
using static Vafadar.Zanance.Api.Billing.AppleBillingFields;

namespace Vafadar.Zanance.Api.Billing;

/// <summary>Fresh App Store Server API queries plus authenticated transaction/renewal data, never a client assertion of payment.</summary>
internal sealed class ApplePurchaseProvider(BillingProviderHttp http, BillingCatalog catalog, IAppleBillingCredentials credentials,
    AppleBillingSignatures signatures, TimeProvider time) : IBillingProvider
{
    /// <inheritdoc />
    public BillingIssuer Issuer => BillingIssuer.AppStore;
    /// <inheritdoc />
    public async Task<BillingVerifiedPurchase> VerifyAsync(string reference, BillingProduct hint, BillingEnvironment environment, CancellationToken ct)
    {
        try
        {
            // Numeric transaction ids are discoverable identifiers, not evidence of actual store possession.
            // Public verification/restoration requires the signed StoreKit transaction before any credential or HTTP effect.
            if (reference.Length > 16384) throw new BillingRefusal(422, "AppleSignedDataRefused");
            using var proof = signatures.Verify(reference);
            CheckTransaction(proof.RootElement, environment);
            var transaction = Transaction(Text(proof.RootElement, "transactionId", 64));
            var original = Transaction(Text(proof.RootElement, "originalTransactionId", 64));
            var account = Text(proof.RootElement, "appAccountToken", 64);
            var verified = await VerifyCoreAsync(transaction, hint, environment, ct);
            if (verified.Canonical != original || verified.AccountBinding != account)
                throw new BillingRefusal(422, "OriginalStoreChainDiffers");
            return verified with { Reference = reference };
        }
        catch (Exception error) when (error is KeyNotFoundException or InvalidOperationException or ArgumentException or FormatException)
        { throw new BillingRefusal(422, "StoreResponseRefused"); }
    }

    /// <inheritdoc />
    public async Task<BillingVerifiedPurchase> VerifyExistingAsync(string reference, BillingProduct hint, BillingEnvironment environment, CancellationToken ct)
    {
        try { return await VerifyCoreAsync(reference, hint, environment, ct); }
        catch (Exception error) when (error is KeyNotFoundException or InvalidOperationException or ArgumentException or FormatException)
        { throw new BillingRefusal(422, "StoreResponseRefused"); }
    }

    private async Task<BillingVerifiedPurchase> VerifyCoreAsync(string reference, BillingProduct hint, BillingEnvironment environment, CancellationToken ct)
    {
        if (!Enum.IsDefined(environment) || !Enum.IsDefined(hint)) throw new BillingRefusal(422, "StorePurchaseRefused");
        Transaction(reference); var bearer = await credentials.AccessAsync(ct);
        using var response = await http.SendAsync(Endpoint(environment, "/inApps/v1/transactions/" + reference), bearer, false, ct);
        using var initial = signatures.Verify(Text(response.RootElement, "signedTransactionInfo", 65536));
        CheckTransaction(initial.RootElement, environment);
        var original = Transaction(Text(initial.RootElement, "originalTransactionId", 64));
        var mapping = catalog.Resolve(Issuer, environment, Text(initial.RootElement, "productId"), null);
        if (mapping.Product == BillingProduct.PlusLifetime)
        {
            if (hint != BillingProduct.PlusLifetime || Text(initial.RootElement, "type") != "Non-Consumable")
                throw new BillingRefusal(422, "UnsupportedStorePurchase");
            var facts = Facts(initial.RootElement, mapping, null, null);
            return Result(initial.RootElement, reference, original, environment, mapping, facts);
        }
        if (hint == BillingProduct.PlusLifetime) throw new BillingRefusal(422, "UnsupportedStorePurchase");
        using var status = await http.SendAsync(Endpoint(environment, "/inApps/v1/subscriptions/" + reference), bearer, false, ct);
        if (Text(status.RootElement, "bundleId") != "pro.vafadar.zanance" ||
            Text(status.RootElement, "environment") != EnvironmentName(environment)) throw new BillingRefusal(422, "StoreApplicationDiffers");
        var groups = status.RootElement.GetProperty("data");
        if (groups.ValueKind != JsonValueKind.Array || groups.GetArrayLength() > 20) throw new BillingRefusal(422, "StoreResponseRefused");
        var matches = new List<JsonElement>(); var total = 0;
        foreach (var group in groups.EnumerateArray())
        {
            var rows = group.GetProperty("lastTransactions");
            if (rows.ValueKind != JsonValueKind.Array || checked(total += rows.GetArrayLength()) > 100)
                throw new BillingRefusal(422, "StoreResponseRefused");
            foreach (var row in rows.EnumerateArray())
                if (Text(row, "originalTransactionId", 64) == original) matches.Add(row);
        }
        if (matches.Count != 1) throw new BillingRefusal(422, "OriginalStoreChainDiffers");
        var current = matches[0];
        using var transaction = signatures.Verify(Text(current, "signedTransactionInfo", 65536));
        using var renewal = signatures.Verify(Text(current, "signedRenewalInfo", 65536));
        CheckTransaction(transaction.RootElement, environment);
        if (Text(transaction.RootElement, "originalTransactionId", 64) != original ||
            Text(renewal.RootElement, "originalTransactionId", 64) != original ||
            Text(renewal.RootElement, "environment") != EnvironmentName(environment) ||
            Text(transaction.RootElement, "type") != "Auto-Renewable Subscription")
            throw new BillingRefusal(422, "OriginalStoreChainDiffers");
        mapping = catalog.Resolve(Issuer, environment, Text(transaction.RootElement, "productId"), null);
        if (mapping.Product == BillingProduct.PlusLifetime) throw new BillingRefusal(422, "OriginalStoreChainDiffers");
        var normalized = Facts(transaction.RootElement, mapping, current, renewal.RootElement);
        return Result(transaction.RootElement, reference, original, environment, mapping, normalized);
    }

    /// <inheritdoc />
    public Task AcknowledgeAsync(BillingVerifiedPurchase purchase, CancellationToken ct) => Task.CompletedTask;

    /// <summary>Only a verified transaction's app-account UUID determines the existing owner.</summary>
    private BillingVerifiedPurchase Result(JsonElement transaction, string reference, string original, BillingEnvironment environment,
        BillingProductMapping mapping, BillingPurchaseFacts facts)
    {
        var binding = Text(transaction, "appAccountToken", 64);
        if (!Guid.TryParseExact(binding, "D", out var account) || account == Guid.Empty) throw new BillingRefusal(422, "StoreAccountBindingRequired");
        return new(Issuer, environment, reference, account.ToString("D"), facts, null, original, mapping.StoreProductId);
    }

    /// <summary>Bundle/environment, ordinary quantity and original purchase ownership are independent of its paid state.</summary>
    private void CheckTransaction(JsonElement row, BillingEnvironment environment)
    {
        if (Text(row, "bundleId") != "pro.vafadar.zanance" || Text(row, "environment") != EnvironmentName(environment) ||
            Text(row, "inAppOwnershipType") != "PURCHASED" || !row.GetProperty("quantity").TryGetInt32(out var count) || count != 1)
            throw new BillingRefusal(422, "StoreApplicationDiffers");
        Transaction(Text(row, "transactionId", 64));
        if (Milliseconds(row, "purchaseDate") > time.GetUtcNow().AddMinutes(1)) throw new BillingRefusal(422, "StoreTimeRefused");
    }

    /// <summary>Maps signed provider validity; renewal cancellation alone does not discard paid access.</summary>
    private static BillingPurchaseFacts Facts(JsonElement transaction, BillingProductMapping mapping, JsonElement? status, JsonElement? renewal)
    {
        var revoked = transaction.TryGetProperty("revocationDate", out _);
        if (revoked) Milliseconds(transaction, "revocationDate");
        var state = revoked ? BillingPurchaseState.Revoked : status is null ? BillingPurchaseState.Active :
            status.Value.GetProperty("status").GetInt32() switch
            {
                1 => BillingPurchaseState.Active, 2 => BillingPurchaseState.Expired, 3 => BillingPurchaseState.Hold,
                4 => BillingPurchaseState.Grace, 5 => BillingPurchaseState.Revoked, _ => throw new BillingRefusal(422, "StoreStateRefused"),
            };
        DateTimeOffset? end = mapping.Product == BillingProduct.PlusLifetime ? null : Milliseconds(transaction, "expiresDate");
        DateTimeOffset? grace = state == BillingPurchaseState.Grace && renewal is { } r ? Milliseconds(r, "gracePeriodExpiresDate") : null;
        var renews = false;
        if (renewal is { } renewalRow)
        {
            renews = renewalRow.GetProperty("autoRenewStatus").GetInt32() switch
            { 0 => false, 1 => true, _ => throw new BillingRefusal(422, "StoreStateRefused") };
        }
        var discount = OptionalText(transaction, "offerDiscountType");
        if (discount is not (null or "FREE_TRIAL" or "PAY_AS_YOU_GO" or "PAY_UP_FRONT"))
            throw new BillingRefusal(422, "UnsupportedStorePhase");
        var facts = new BillingPurchaseFacts(mapping.Product, state, Milliseconds(transaction, "purchaseDate"), end, grace,
            renews, discount == "FREE_TRIAL", true, mapping.TermsVersion,
            discount is not null || !transaction.TryGetProperty("offerType", out _));
        facts.Validate(); return facts;
    }
    /// <summary>Fixed official API hosts; environment is never an arbitrary client URL.</summary>
    private static Uri Endpoint(BillingEnvironment environment, string path) => new("https://" +
        (environment == BillingEnvironment.Sandbox ? "api.storekit-sandbox.apple.com" : "api.storekit.apple.com") + path);
    /// <summary>Exact provider environment spelling.</summary>
    internal static string EnvironmentName(BillingEnvironment environment) => environment == BillingEnvironment.Sandbox ? "Sandbox" : "Production";
}
