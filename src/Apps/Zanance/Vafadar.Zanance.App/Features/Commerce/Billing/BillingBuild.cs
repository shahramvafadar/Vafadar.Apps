namespace Vafadar.Zanance.App.Features.Commerce.Billing;

/// <summary>Explicit mobile billing edition, independent of personal sync, cloud backup and commercial quota activation.</summary>
public static class BillingBuild
{
    /// <summary>Only a native adapter compiled and qualified for this platform can expose purchase effects.</summary>
    public static bool IsAvailable =>
#if BILLING && ANDROID
        true;
#else
        false;
#endif
    /// <summary>Explicit sandbox builds cannot create production cached rights.</summary>
    public static Core.Commerce.BillingEnvironment Environment =>
#if DEBUG || BILLING_SANDBOX
        Core.Commerce.BillingEnvironment.Sandbox;
#else
        Core.Commerce.BillingEnvironment.Production;
#endif
}
