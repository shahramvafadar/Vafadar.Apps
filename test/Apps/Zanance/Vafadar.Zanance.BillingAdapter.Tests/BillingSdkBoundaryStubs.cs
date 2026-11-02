// These are inert SDK boundaries for the linked original adapter, never a store or entitlement implementation.
namespace Java.Lang
{
    /// <summary>Provides only the native listener's disposable base identity.</summary>
    internal class Object : IDisposable
    {
        /// <summary>Releases no native resource because this host boundary creates none.</summary>
        public void Dispose() { }
    }
}
namespace Android.App
{
    /// <summary>Provides an inert context if an unexpected SDK constructor is reached.</summary>
    internal static class Application
    {
        /// <summary>Gets a context marker with no account, platform or network capability.</summary>
        public static object Context { get; } = new();
    }
}
namespace Vafadar.Zanance.App.Platforms.Android
{
    /// <summary>Provides only the controlled native launch target.</summary>
    internal static class Platform
    {
        /// <summary>Gets or sets the activity marker used by the simulated SDK boundary.</summary>
        internal static object? CurrentActivity { get; set; } = new();
    }
    /// <summary>Executes the original native-dispatch delegate without any UI, process or SDK effect.</summary>
    internal static class MainThread
    {
        /// <summary>Returns the controlled SDK launch result or its actual exception.</summary>
        internal static Task<T> InvokeOnMainThreadAsync<T>(Func<T> action) => Task.FromResult(action());
    }
}
namespace Android.BillingClient.Api
{
    /// <summary>Models only SDK BillingResult fields needed to compile the original adapter.</summary>
    internal sealed class BillingResult
    {
        /// <summary>Gets the controlled ResponseCode field; no store facts are obtained.</summary>
        public int ResponseCode { get; init; }
    }
    /// <summary>Models only SDK BillingConfig fields needed to compile the original adapter.</summary>
    internal sealed class BillingConfig
    {
        /// <summary>Gets the controlled CountryCode field; no store facts are obtained.</summary>
        public string? CountryCode { get; init; }
    }
    /// <summary>Receives only explicit simulated purchase callbacks.</summary>
    internal interface IPurchasesUpdatedListener { void OnPurchasesUpdated(BillingResult result, IList<Purchase>? purchases); }
    /// <summary>Receives only explicit controlled connection callbacks.</summary>
    internal interface IBillingClientStateListener { void OnBillingSetupFinished(BillingResult result); void OnBillingServiceDisconnected(); }
    /// <summary>Retains the shape of the unused native-country callback.</summary>
    internal interface IBillingConfigResponseListener { void OnBillingConfigResponse(BillingResult result, BillingConfig? config); }
    /// <summary>Models only SDK Purchase fields needed to compile the original adapter.</summary>
    internal sealed class Purchase
    {
        /// <summary>Gets the controlled PurchaseToken field; no store facts are obtained.</summary>
        public string? PurchaseToken { get; init; }
        /// <summary>Gets the controlled PurchaseState field; no store facts are obtained.</summary>
        public int PurchaseState { get; init; }
        /// <summary>Gets the controlled PackageName field; no store facts are obtained.</summary>
        public string? PackageName { get; init; }
        /// <summary>Gets the controlled Products field; no store facts are obtained.</summary>
        public IList<string>? Products { get; init; }
    }
    /// <summary>Provides only a fixture-owned product handle, not a purchasable real offer.</summary>
    internal sealed class ProductDetails
    {
        /// <summary>Gets the deliberately fictitious source product identity.</summary>
        public string ProductId { get; init; } = "controlled_product";
        /// <summary>Gets inert title metadata, never shown by these tests.</summary>
        public string? Title { get; init; }
        /// <summary>Gets inert description metadata, never shown by these tests.</summary>
        public string? Description { get; init; }
        /// <summary>Gets the unused SDK offer collection shape.</summary>
        public IList<OneTimeOffer>? OneTimePurchaseOfferDetailsList { get; init; }
        /// <summary>Returns no configured subscription offer.</summary>
        public IList<SubscriptionOffer>? GetSubscriptionOfferDetails() => null;
    }
    /// <summary>Models only SDK OneTimeOffer fields needed to compile the original adapter.</summary>
    internal sealed class OneTimeOffer
    {
        /// <summary>Gets the controlled OfferId field; no store facts are obtained.</summary>
        public string? OfferId { get; init; }
        /// <summary>Gets the controlled OfferToken field; no store facts are obtained.</summary>
        public string? OfferToken { get; init; }
        /// <summary>Gets the controlled PriceAmountMicros field; no store facts are obtained.</summary>
        public long PriceAmountMicros { get; init; }
        /// <summary>Gets the controlled PriceCurrencyCode field; no store facts are obtained.</summary>
        public string? PriceCurrencyCode { get; init; }
        /// <summary>Gets the controlled FormattedPrice field; no store facts are obtained.</summary>
        public string? FormattedPrice { get; init; }
        /// <summary>Returns no unused rental terms.</summary>
        public object? GetRentalDetails() => null;
        /// <summary>Returns no unused preorder terms.</summary>
        public object? GetPreorderDetails() => null;
    }
    /// <summary>Models only SDK SubscriptionOffer fields needed to compile the original adapter.</summary>
    internal sealed class SubscriptionOffer
    {
        /// <summary>Gets the controlled BasePlanId field; no store facts are obtained.</summary>
        public string? BasePlanId { get; init; }
        /// <summary>Gets the controlled InstallmentPlanDetails field; no store facts are obtained.</summary>
        public object? InstallmentPlanDetails { get; init; }
        /// <summary>Gets the controlled OfferId field; no store facts are obtained.</summary>
        public string? OfferId { get; init; }
        /// <summary>Gets the controlled OfferToken field; no store facts are obtained.</summary>
        public string? OfferToken { get; init; }
        /// <summary>Gets the controlled PricingPhases field; no store facts are obtained.</summary>
        public PricingPhases? PricingPhases { get; init; }
    }
    /// <summary>Models only SDK PricingPhases fields needed to compile the original adapter.</summary>
    internal sealed class PricingPhases
    {
        /// <summary>Gets the controlled PricingPhaseList field; no store facts are obtained.</summary>
        public IList<PricingPhase>? PricingPhaseList { get; init; }
    }
    /// <summary>Models only SDK PricingPhase fields needed to compile the original adapter.</summary>
    internal sealed class PricingPhase
    {
        /// <summary>Gets the controlled PriceAmountMicros field; no store facts are obtained.</summary>
        public long PriceAmountMicros { get; init; }
        /// <summary>Gets the controlled PriceCurrencyCode field; no store facts are obtained.</summary>
        public string? PriceCurrencyCode { get; init; }
        /// <summary>Gets the controlled FormattedPrice field; no store facts are obtained.</summary>
        public string? FormattedPrice { get; init; }
        /// <summary>Gets the controlled BillingPeriod field; no store facts are obtained.</summary>
        public string? BillingPeriod { get; init; }
        /// <summary>Gets the controlled BillingCycleCount field; no store facts are obtained.</summary>
        public int BillingCycleCount { get; init; }
        /// <summary>Gets the controlled RecurrenceMode field; no store facts are obtained.</summary>
        public int RecurrenceMode { get; init; }
    }
    /// <summary>Models only SDK ProductDetailsResponse fields needed to compile the original adapter.</summary>
    internal sealed class ProductDetailsResponse
    {
        /// <summary>Gets the controlled Result field; no store facts are obtained.</summary>
        public BillingResult? Result { get; init; }
        /// <summary>Gets the controlled ProductDetailsList field; no store facts are obtained.</summary>
        public IList<ProductDetails>? ProductDetailsList { get; init; }
    }
    /// <summary>Models only SDK PurchasesResponse fields needed to compile the original adapter.</summary>
    internal sealed class PurchasesResponse
    {
        /// <summary>Gets the controlled Result field; no store facts are obtained.</summary>
        public BillingResult? Result { get; init; }
        /// <summary>Gets the controlled Purchases field; no store facts are obtained.</summary>
        public IList<Purchase>? Purchases { get; init; }
    }
    /// <summary>Retains the unused SDK GetBillingConfigParams builder shape.</summary>
    internal sealed class GetBillingConfigParams
    {
        /// <summary>Creates only inert builder data.</summary>
        public static SdkBuilder<GetBillingConfigParams> NewBuilder() => new();
    }
    /// <summary>Retains the unused SDK PendingPurchasesParams builder shape.</summary>
    internal sealed class PendingPurchasesParams
    {
        /// <summary>Creates only inert builder data.</summary>
        public static SdkBuilder<PendingPurchasesParams> NewBuilder() => new();
    }
    /// <summary>Retains the unused SDK QueryPurchasesParams builder shape.</summary>
    internal sealed class QueryPurchasesParams
    {
        /// <summary>Creates only inert builder data.</summary>
        public static SdkBuilder<QueryPurchasesParams> NewBuilder() => new();
    }
    /// <summary>Retains the unused native catalog query shape.</summary>
    internal sealed class QueryProductDetailsParams
    {
        /// <summary>Creates only inert query metadata.</summary>
        public static SdkBuilder<QueryProductDetailsParams> NewBuilder() => new();
        /// <summary>Retains the original catalog product builder shape.</summary>
        internal sealed class Product
        {
            /// <summary>Creates only inert product metadata.</summary>
            public static SdkBuilder<Product> NewBuilder() => new();
        }
    }
    /// <summary>Retains original launch-parameter construction without storing account bindings or references.</summary>
    internal sealed class BillingFlowParams
    {
        /// <summary>Creates only an inert launch description.</summary>
        public static SdkBuilder<BillingFlowParams> NewBuilder() => new();
        /// <summary>Retains the native product-parameter shape.</summary>
        internal sealed class ProductDetailsParams
        {
            /// <summary>Creates inert product parameters.</summary>
            public static SdkBuilder<ProductDetailsParams> NewBuilder() => new();
            /// <summary>Retains existing replacement-term type names without changing any commercial policy.</summary>
            internal sealed class SubscriptionProductReplacementParams
            {
                /// <summary>Matches only the three existing replacement alternatives.</summary>
                internal enum ReplacementMode { WithoutProration, WithTimeProration, Deferred }
                /// <summary>Creates unused replacement metadata.</summary>
                public static SdkBuilder<SubscriptionProductReplacementParams> NewBuilder() => new();
            }
        }
        /// <summary>Retains the unused original purchase-reference parameter shape.</summary>
        internal sealed class SubscriptionUpdateParams
        {
            /// <summary>Creates inert metadata without retaining an original reference.</summary>
            public static SdkBuilder<SubscriptionUpdateParams> NewBuilder() => new();
        }
    }
    /// <summary>Compiles the unchanged SDK builder calls; all financial/catalog logic remains in the real original adapter.</summary>
    internal sealed class SdkBuilder<T> where T : new()
    {
        /// <summary>Accepts the original SetListener call without retaining private input or performing an SDK effect.</summary>
        public SdkBuilder<T> SetListener(IPurchasesUpdatedListener listener) => this;
        /// <summary>Accepts the original EnablePendingPurchases call without retaining private input or performing an SDK effect.</summary>
        public SdkBuilder<T> EnablePendingPurchases(PendingPurchasesParams value) => this;
        /// <summary>Accepts the original EnableAutoServiceReconnection call without retaining private input or performing an SDK effect.</summary>
        public SdkBuilder<T> EnableAutoServiceReconnection() => this;
        /// <summary>Accepts the original EnableOneTimeProducts call without retaining private input or performing an SDK effect.</summary>
        public SdkBuilder<T> EnableOneTimeProducts() => this;
        /// <summary>Accepts the original SetProductId call without retaining private input or performing an SDK effect.</summary>
        public SdkBuilder<T> SetProductId(string id) => this;
        /// <summary>Accepts the original SetProductType call without retaining private input or performing an SDK effect.</summary>
        public SdkBuilder<T> SetProductType(string type) => this;
        /// <summary>Accepts the original SetProductList call without retaining private input or performing an SDK effect.</summary>
        public SdkBuilder<T> SetProductList(IList<QueryProductDetailsParams.Product> products) => this;
        /// <summary>Accepts the original IncludeSuspendedSubscriptions call without retaining private input or performing an SDK effect.</summary>
        public SdkBuilder<T> IncludeSuspendedSubscriptions(bool value) => this;
        /// <summary>Accepts the original SetProductDetails call without retaining private input or performing an SDK effect.</summary>
        public SdkBuilder<T> SetProductDetails(ProductDetails details) => this;
        /// <summary>Accepts the original SetOfferToken call without retaining private input or performing an SDK effect.</summary>
        public SdkBuilder<T> SetOfferToken(string token) => this;
        /// <summary>Accepts the original SetObfuscatedAccountId call without retaining private input or performing an SDK effect.</summary>
        public SdkBuilder<T> SetObfuscatedAccountId(string binding) => this;
        /// <summary>Accepts the original SetSubscriptionUpdateParams call without retaining private input or performing an SDK effect.</summary>
        public SdkBuilder<T> SetSubscriptionUpdateParams(BillingFlowParams.SubscriptionUpdateParams value) => this;
        /// <summary>Accepts the original SetProductDetailsParamsList call without retaining private input or performing an SDK effect.</summary>
        public SdkBuilder<T> SetProductDetailsParamsList(IList<BillingFlowParams.ProductDetailsParams> products) => this;
        /// <summary>Accepts the original SetSubscriptionProductReplacementParams call without retaining private input or performing an SDK effect.</summary>
        public SdkBuilder<T> SetSubscriptionProductReplacementParams(BillingFlowParams.ProductDetailsParams.SubscriptionProductReplacementParams value) => this;
        /// <summary>Accepts the original SetOldProductId call without retaining private input or performing an SDK effect.</summary>
        public SdkBuilder<T> SetOldProductId(string id) => this;
        /// <summary>Accepts the original SetReplacementMode call without retaining private input or performing an SDK effect.</summary>
        public SdkBuilder<T> SetReplacementMode(BillingFlowParams.ProductDetailsParams.SubscriptionProductReplacementParams.ReplacementMode mode) => this;
        /// <summary>Accepts the original SetOldPurchaseToken call without retaining private input or performing an SDK effect.</summary>
        public SdkBuilder<T> SetOldPurchaseToken(string token) => this;
        /// <summary>Builds only inert parameter metadata.</summary>
        public T Build() => new();
    }
    /// <summary>Controls connection/launch boundaries while the linked original store owns every task and refusal.</summary>
    internal sealed class BillingClient
    {
        private bool ready = true;
        private int connections, launches, readyReads;
        /// <summary>Gets or sets controlled connection readiness.</summary>
        public bool IsReady { get { Interlocked.Increment(ref readyReads); return Volatile.Read(ref ready); } set => Volatile.Write(ref ready, value); }
        /// <summary>Gets how often the original adapter inspected readiness.</summary>
        public int ReadyReads => Volatile.Read(ref readyReads);
        /// <summary>Gets the number of simulated connection starts.</summary>
        public int Connections => Volatile.Read(ref connections);
        /// <summary>Gets the number of simulated native launch attempts.</summary>
        public int NativeLaunchAttempts => Volatile.Read(ref launches);
        /// <summary>Gets or sets immediate completion of a controlled connection.</summary>
        public bool CompleteConnectionImmediately { get; set; }
        /// <summary>Gets or sets whether the launch boundary throws.</summary>
        public bool ThrowOnLaunch { get; set; }
        /// <summary>Gets or sets the explicit synchronous launch result.</summary>
        public int LaunchResponseCode { get; set; }
        /// <summary>Refuses accidental client construction; fixtures inject only this inert boundary.</summary>
        public static SdkBuilder<BillingClient> NewBuilder(object context) => throw new InvalidOperationException("Only the injected SDK boundary is admitted.");
        /// <summary>Holds or explicitly completes the original adapter's connection callback.</summary>
        public void StartConnection(IBillingClientStateListener listener)
        {
            Interlocked.Increment(ref connections);
            if (CompleteConnectionImmediately) { IsReady = true; listener.OnBillingSetupFinished(new BillingResult { ResponseCode = 0 }); }
        }
        /// <summary>Refuses all unrelated native country queries.</summary>
        public void GetBillingConfigAsync(GetBillingConfigParams value, IBillingConfigResponseListener listener) => throw new InvalidOperationException("No country query is in this cohort.");
        /// <summary>Refuses all unrelated native catalog queries.</summary>
        public Task<ProductDetailsResponse> QueryProductDetailsAsync(QueryProductDetailsParams value) => throw new InvalidOperationException("No catalog query is in this cohort.");
        /// <summary>Refuses all unrelated native owned-purchase queries.</summary>
        public Task<PurchasesResponse> QueryPurchasesAsync(QueryPurchasesParams value) => throw new InvalidOperationException("No owned query is in this cohort.");
        /// <summary>Records only a simulated launch and returns or throws its controlled SDK boundary result.</summary>
        public BillingResult LaunchBillingFlow(object activity, BillingFlowParams parameters)
        {
            Interlocked.Increment(ref launches);
            if (ThrowOnLaunch) throw new InvalidOperationException("Controlled native launch failure");
            return new BillingResult { ResponseCode = LaunchResponseCode };
        }
    }
}
