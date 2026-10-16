using Vafadar.Zanance.Core.Commerce;

namespace Vafadar.Zanance.Api.Billing;

/// <summary>Content-free refusal, with no provider tokens, private response or nested HTTP exception.</summary>
internal sealed class BillingRefusal(int status, string code) : Exception(code)
{
    /// <summary>Bounded public status.</summary>
    internal int Status { get; } = status;
}

/// <summary>One current provider query result; never constructed from an unverified client callback.</summary>
internal sealed record BillingVerifiedPurchase(BillingIssuer Issuer, BillingEnvironment Environment,
    string Reference, string AccountBinding, BillingPurchaseFacts Facts, string? ReplacedReference, string? CanonicalReference = null, string? StoreProductId = null)
{
    /// <summary>Stable provider chain id; Apple renewal transaction ids are aliases, not separate subscriptions.</summary>
    internal string Canonical => CanonicalReference ?? Reference;
    /// <summary>No raw provider reference in diagnostics.</summary>
    public override string ToString() => "Verified provider purchase";
}

/// <summary>Maintained provider verification and acknowledgement, separate from persistence and native UI.</summary>
internal interface IBillingProvider
{
    /// <summary>The one explicit supported issuer.</summary>
    BillingIssuer Issuer { get; }
    /// <summary>Queries the actual provider with configured credentials and checks its original application/environment.</summary>
    Task<BillingVerifiedPurchase> VerifyAsync(string reference, BillingProduct hint, BillingEnvironment environment, CancellationToken ct);
    /// <summary>Rechecks a canonical reference already authorized by durable service ownership, never public restore input.</summary>
    Task<BillingVerifiedPurchase> VerifyExistingAsync(string reference, BillingProduct hint, BillingEnvironment environment, CancellationToken ct)
        => VerifyAsync(reference, hint, environment, ct);
    /// <summary>Acknowledges only an already persisted verified paid grant; retry remains idempotent.</summary>
    Task AcknowledgeAsync(BillingVerifiedPurchase purchase, CancellationToken ct);
}

/// <summary>Configured products are not store availability, prices or a commercial activation switch.</summary>
internal sealed class BillingCatalog
{
    private readonly Dictionary<(BillingIssuer, BillingEnvironment), BillingProductMapping[]> _mappings = [];

    /// <summary>Reads only public bounded product identifiers; no account credential is read here.</summary>
    internal BillingCatalog(IConfiguration configuration)
    {
        foreach (var issuer in new[] { BillingIssuer.GooglePlay, BillingIssuer.AppStore })
        foreach (var environment in Enum.GetValues<BillingEnvironment>())
        {
            var rows = new List<BillingProductMapping>();
            foreach (var product in Enum.GetValues<BillingProduct>())
            {
                var section = configuration.GetSection($"Billing:Catalog:{issuer}:{environment}:{product}");
                Add(section, true);
                var legacy = section.GetSection("Legacy").GetChildren().ToArray();
                if (legacy.Length > 16) throw new InvalidOperationException("Billing legacy catalog bound exceeded.");
                foreach (var historical in legacy) Add(historical, false);

                void Add(IConfigurationSection entry, bool available)
                {
                    var id = entry["ProductId"]; if (string.IsNullOrEmpty(id)) return;
                    var plan = entry["BasePlanId"]; var terms = entry["TermsVersion"];
                    if (!Identifier(id) || plan is not null && !Identifier(plan) || string.IsNullOrWhiteSpace(terms) || terms.Length > 64 || terms.Any(char.IsControl) ||
                        issuer == BillingIssuer.GooglePlay && product != BillingProduct.PlusLifetime && string.IsNullOrEmpty(plan) ||
                        issuer == BillingIssuer.AppStore && plan is not null || product == BillingProduct.PlusLifetime && plan is not null)
                        throw new InvalidOperationException("Invalid configured billing catalog.");
                    var offers = new List<BillingOfferDefinition>();
                    if (available)
                    {
                        foreach (var offer in entry.GetSection("Offers").GetChildren())
                        {
                            var offerId = offer["StoreOfferId"]; var country = offer["Country"];
                            if (offers.Count == 8 || offerId is null || !Identifier(offerId) ||
                                !Enum.TryParse<BillingOfferKind>(offer["Kind"], out var kind) || !Enum.IsDefined(kind) || kind == BillingOfferKind.Standard ||
                                kind == BillingOfferKind.ProTrial && BillingProducts.Plan(product) != ProductPlan.Pro ||
                                kind == BillingOfferKind.LifetimeAnnual && product != BillingProduct.ProYearly)
                                throw new InvalidOperationException("Invalid configured billing offer.");
                            DateTimeOffset? start = Date(offer["StartsAt"]), end = Date(offer["EndsAt"]);
                            if (kind == BillingOfferKind.Campaign && (country is not { Length: 2 } || !country.All(char.IsAsciiLetterUpper) ||
                                start is null || end is null || start >= end) || kind != BillingOfferKind.Campaign &&
                                (country is not null || start is not null || end is not null))
                                throw new InvalidOperationException("Invalid configured billing offer window.");
                            offers.Add(new(offerId, kind, country, start, end));
                        }
                        if (offers.Select(o => o.StoreOfferId).Distinct(StringComparer.Ordinal).Count() != offers.Count)
                            throw new InvalidOperationException("Ambiguous configured billing offer.");
                    }
                    rows.Add(new(product, id, plan, terms, available, offers));
                }
            }
            if (rows.GroupBy(row => row.StoreProductId, StringComparer.Ordinal).Any(group =>
                    group.Select(row => BillingProducts.Plan(row.Product)).Distinct().Count() > 1 ||
                    group.Select(row => row.Product == BillingProduct.PlusLifetime).Distinct().Count() > 1) ||
                rows.Select(row => (row.StoreProductId, row.BasePlanId)).Distinct().Count() != rows.Count)
                throw new InvalidOperationException("Ambiguous configured billing product.");
            var mappings = rows.ToArray();
            if (System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(mappings, BillingJson.Default.BillingProductMappingArray).Length > 24576)
                throw new InvalidOperationException("Configured billing catalog exceeds its protocol bound.");
            _mappings[(issuer, environment)] = mappings;
        }
    }

    /// <summary>Returns current and explicit restore-only legacy mappings, without guessing catalog defaults.</summary>
    internal IReadOnlyList<BillingProductMapping> Read(BillingIssuer issuer, BillingEnvironment environment) =>
        _mappings.TryGetValue((issuer, environment), out var rows) ? rows : [];

    /// <summary>Requires exact provider product/base-plan facts; a client hint cannot select capabilities.</summary>
    internal BillingProductMapping Resolve(BillingIssuer issuer, BillingEnvironment environment, string product, string? plan)
        => Read(issuer, environment).SingleOrDefault(row => row.StoreProductId == product && row.BasePlanId == plan)
            ?? throw new BillingRefusal(422, "UnsupportedStoreProduct");

    /// <summary>Strict configured UTC offer boundaries, independent of a client's wall clock or selected country.</summary>
    private static DateTimeOffset? Date(string? text)
    {
        if (text is null) return null;
        return DateTimeOffset.TryParseExact(text, "yyyy-MM-dd'T'HH:mm:ss'Z'", System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, out var result)
            ? result : throw new InvalidOperationException("Invalid configured billing offer time.");
    }

    /// <summary>Only ordinary store identifiers; no URLs, paths or protocol selection.</summary>
    private static bool Identifier(string value) => value.Length is > 0 and <= 200 &&
        value.All(character => char.IsAsciiLetterOrDigit(character) || character is '.' or '_' or '-');
}
