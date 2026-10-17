using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Data.Billing;

namespace Vafadar.Zanance.App.Features.Commerce;

/// <summary>Complete wrapping purchase history text; contains no receipt, identity or purchase identifier.</summary>
public sealed record BillingOwnedPurchaseRow(string Title, string Details);

public sealed partial class ProductPlansViewModel
{
    /// <summary>Projects only original verified snapshots; current access continues to come from the unchanged cache policy.</summary>
    private IReadOnlyList<BillingOwnedPurchaseRow> OwnedPurchases()
    {
        var rows = new List<BillingOwnedPurchaseRow>();
        Add(_billingPresentation?.Local, "Billing_Owned_Device");
        Add(_billingPresentation?.Identity, "Billing_Owned_Identity");
        return rows;

        void Add(BillingClientView? view, string scope)
        {
            if (view?.Snapshot is not { } snapshot) return;
            // Device and identity histories can have different verification horizons. Never merge their
            // state/date fields or turn a historical active observation into a fresh paid-access claim.
            foreach (var grant in snapshot.Grants)
            {
                var facts = grant.Facts;
                var lines = new List<string> { _translator[scope], _translator["Billing_Owned_State" + facts.State],
                    _translator.Format("Billing_Owned_Verified", Date(grant.VerifiedAt)) };
                if (view.Decision.NeedsVerification || view.Decision.UsingOutageAllowance)
                    lines.Add(_translator["Billing_Owned_Refresh"]);
                if (BillingProducts.Period(facts.Product) == BillingPeriod.None)
                    lines.Add(_translator["Billing_Owned_Lifetime"]);
                else
                {
                    if (facts.PaidUntil is { } paid) lines.Add(_translator.Format("Billing_Owned_PaidThrough", Date(paid)));
                    if (facts.State == BillingPurchaseState.Grace && facts.GraceUntil is { } grace)
                        lines.Add(_translator.Format("Billing_Owned_GraceThrough", Date(grace)));
                    lines.Add(_translator[facts.AutoRenews ? "Billing_Owned_RenewOn" : "Billing_Owned_RenewOff"]);
                }
                if (facts.ScheduledChange is { } change)
                    lines.Add(_translator.Format("Billing_Owned_Change", Product(change.Product), Date(change.EffectiveAt)));
                var issuer = grant.Issuer switch { BillingIssuer.GooglePlay => "Google Play", BillingIssuer.AppStore => "App Store", BillingIssuer.Web => "Zanance web", _ => throw new InvalidDataException("Unsupported verified purchase issuer.") };
                rows.Add(new(Product(facts.Product) + " · " + issuer, string.Join(Environment.NewLine, lines)));
            }
        }
        string Date(DateTimeOffset value) => _billingDates?.Format(value) ?? throw new InvalidOperationException("Billing date formatter required.");
        string Product(BillingProduct product) => BillingProducts.Plan(product) + " · " + _translator[BillingProducts.Period(product) switch
        { BillingPeriod.Month => "Billing_PeriodMonth", BillingPeriod.Year => "Billing_PeriodYear", _ => "Billing_PeriodOnce" }];
    }
}
