#if DEBUG && WINDOWS
using Vafadar.Zanance.App.Features.Commerce.Billing;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Data.Billing;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    private static ProductBillingPresentation OwnedBillingReviewState()
    {
        var now = new DateTimeOffset(2026, 10, 20, 0, 0, 0, TimeSpan.Zero);
        var grants = new[]
        {
            new BillingCachedGrant(BillingIssuer.GooglePlay, BillingEnvironment.Sandbox,
                new(BillingProduct.PlusMonthly, BillingPurchaseState.Active, now.AddMonths(-1), now.AddDays(7), null, false, false, true, "fictional-v1",
                    ScheduledChange: new(BillingProduct.ProYearly, now.AddDays(7), "fictional-v2")), false, now),
            new BillingCachedGrant(BillingIssuer.GooglePlay, BillingEnvironment.Sandbox,
                new(BillingProduct.ProYearly, BillingPurchaseState.Grace, now.AddYears(-1), now.AddDays(-1), now.AddDays(3), true, false, true, "fictional-v1"), true, now),
            new BillingCachedGrant(BillingIssuer.AppStore, BillingEnvironment.Sandbox,
                new(BillingProduct.PlusLifetime, BillingPurchaseState.Active, now.AddYears(-1), null, null, false, false, true, "fictional-v1"), true, now)
        };
        foreach (var grant in grants) grant.Facts.Validate();
        var view = new BillingClientView(new(1, BillingSnapshotKind.Current, Guid.NewGuid(), Guid.NewGuid(), new string('a', 64),
            BillingIssuer.GooglePlay, BillingEnvironment.Sandbox, null, Guid.NewGuid(), 0, now, grants, false),
            new(ProductPlan.Free, true, true), false, null, false);
        return new(ProductBillingStatus.VerificationUnavailable, view, null, [], true, false);
    }
 }
#endif
