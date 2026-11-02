using System.Collections;
using System.Reflection;
using Android.BillingClient.Api;
using Vafadar.Zanance.App.Features.Commerce.Billing;
using Vafadar.Zanance.App.Platforms.Android;
using Vafadar.Zanance.Core.Commerce;

namespace Vafadar.Zanance.BillingAdapter.Tests;

/// <summary>The original Play adapter preserves its SDK-owned task when another observer or launch is refused.</summary>
public sealed class GooglePlayPurchaseOwnershipTests
{
    /// <summary>Rejecting another purchase cannot complete the original SDK task or replace its intent.</summary>
    [Fact]
    public async Task Pending_second_purchase_keeps_the_original_sdk_task_and_outcome()
    {
        using var f = new Fixture(); var originalId = Guid.NewGuid();
        var original = f.Purchase(originalId, TestContext.Current.CancellationToken); var native = f.NativeTask!;
        var refusal = await Assert.ThrowsAsync<NativeBillingRefusal>(() => f.Purchase(Guid.NewGuid(), TestContext.Current.CancellationToken));
        Assert.Equal("OriginalStorePaymentPending", refusal.Message);
        Assert.Same(native, f.NativeTask); Assert.False(native.IsCompleted); Assert.Equal(originalId, f.NativeIntent);
        Assert.Equal(1, f.Sdk.NativeLaunchAttempts);
        f.CancelSdk(); Assert.Equal("StoreCanceled", (await Assert.ThrowsAsync<NativeBillingRefusal>(() => original)).Message);
    }

    /// <summary>Two callers admitted before connection completion still leave only one SDK task and one losing refusal.</summary>
    [Fact]
    public async Task Connection_race_refusal_cannot_fault_the_winning_native_owner()
    {
        using var f = new Fixture(ready: false); var firstId = Guid.NewGuid(); var secondId = Guid.NewGuid();
        var first = f.Purchase(firstId, TestContext.Current.CancellationToken);
        var second = f.Purchase(secondId, TestContext.Current.CancellationToken);
        Assert.Null(f.NativeTask); Assert.False(first.IsCompleted); Assert.False(second.IsCompleted); Assert.Equal(1, f.Sdk.Connections);
        f.Sdk.IsReady = true; f.Store.OnBillingSetupFinished(new BillingResult { ResponseCode = 0 });
        var refused = await Task.WhenAny(first, second).WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken);
        var winner = ReferenceEquals(refused, first) ? second : first;
        var winnerId = ReferenceEquals(refused, first) ? secondId : firstId;
        Assert.Equal("OriginalStorePaymentPending", (await Assert.ThrowsAsync<NativeBillingRefusal>(() => refused)).Message);
        Assert.False(f.NativeTask!.IsCompleted); Assert.Equal(winnerId, f.NativeIntent); Assert.Equal(1, f.Sdk.NativeLaunchAttempts);
        var resumed = f.Store.ContinuePaymentAsync(winnerId, TestContext.Current.CancellationToken); Assert.False(resumed.IsCompleted);
        f.CancelSdk(); Assert.Equal("StoreCanceled", (await Assert.ThrowsAsync<NativeBillingRefusal>(() => winner)).Message);
        Assert.Equal("StoreCanceled", (await Assert.ThrowsAsync<NativeBillingRefusal>(() => resumed)).Message);
    }

    /// <summary>Observer cancellation is not SDK cancellation; another request cannot break the retained continuation.</summary>
    [Fact]
    public async Task Retired_observer_and_later_refusal_keep_the_original_task_resumable()
    {
        using var f = new Fixture(); using var observer = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var id = Guid.NewGuid(); var original = f.Purchase(id, observer.Token); var native = f.NativeTask!;
        observer.Cancel(); await Assert.ThrowsAnyAsync<OperationCanceledException>(() => original);
        Assert.False(native.IsCompleted);
        Assert.Equal("OriginalStorePaymentPending", (await Assert.ThrowsAsync<NativeBillingRefusal>(() => f.Purchase(Guid.NewGuid(), TestContext.Current.CancellationToken))).Message);
        Assert.False(native.IsCompleted); Assert.Same(native, f.NativeTask);
        var resumed = f.Store.ContinuePaymentAsync(id, TestContext.Current.CancellationToken); Assert.False(resumed.IsCompleted);
        f.CancelSdk(); Assert.Equal("StoreCanceled", (await Assert.ThrowsAsync<NativeBillingRefusal>(() => resumed)).Message);
    }

    /// <summary>A known outstanding native task is refused before another readiness or connection effect.</summary>
    [Fact]
    public async Task Pending_second_purchase_does_not_reconnect_the_sdk()
    {
        using var f = new Fixture(); var first = f.Purchase(Guid.NewGuid(), TestContext.Current.CancellationToken);
        var native = f.NativeTask!; var reads = f.Sdk.ReadyReads; f.Sdk.IsReady = false; f.Sdk.CompleteConnectionImmediately = true;
        Assert.Equal("OriginalStorePaymentPending", (await Assert.ThrowsAsync<NativeBillingRefusal>(() => f.Purchase(Guid.NewGuid(), TestContext.Current.CancellationToken))).Message);
        Assert.Equal(0, f.Sdk.Connections); Assert.Equal(reads, f.Sdk.ReadyReads); Assert.Equal(1, f.Sdk.NativeLaunchAttempts); Assert.False(native.IsCompleted);
        f.CancelSdk(); await Assert.ThrowsAsync<NativeBillingRefusal>(() => first);
    }

    /// <summary>A failure after this call creates its task still completes that exact task for later observation.</summary>
    [Fact]
    public async Task Own_launch_exception_completes_only_the_newly_created_native_task()
    {
        using var f = new Fixture(); f.Sdk.ThrowOnLaunch = true; var id = Guid.NewGuid();
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Purchase(id, TestContext.Current.CancellationToken));
        Assert.True(f.NativeTask!.IsFaulted); Assert.Equal(id, f.NativeIntent); Assert.Equal(1, f.Sdk.NativeLaunchAttempts);
        Assert.Equal("StoreLaunchRefused", (await Assert.ThrowsAsync<NativeBillingRefusal>(() => f.Store.ContinuePaymentAsync(id, TestContext.Current.CancellationToken))).Message);
    }

    /// <summary>The existing synchronous not-launched cancellation remains StoreCanceled rather than a fabricated launch failure.</summary>
    [Fact]
    public async Task Synchronous_sdk_cancel_result_keeps_its_original_refusal_code()
    {
        using var f = new Fixture(); f.Sdk.LaunchResponseCode = 1; var id = Guid.NewGuid();
        Assert.Equal("StoreCanceled", (await Assert.ThrowsAsync<NativeBillingRefusal>(() => f.Purchase(id, TestContext.Current.CancellationToken))).Message);
        Assert.True(f.NativeTask!.IsFaulted); Assert.Equal(1, f.Sdk.NativeLaunchAttempts);
        Assert.Equal("StoreCanceled", (await Assert.ThrowsAsync<NativeBillingRefusal>(() => f.Store.ContinuePaymentAsync(id, TestContext.Current.CancellationToken))).Message);
    }

    /// <summary>Arranges only inert SDK readiness and one prior local offer handle; no real store, reference or database is involved.</summary>
    private sealed class Fixture : IDisposable
    {
        private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;
        private readonly List<Task> observations = [];
        /// <summary>Gets the actual original adapter under the controlled SDK boundary.</summary>
        internal GooglePlayBillingStore Store { get; } = new();
        /// <summary>Gets the inert SDK boundary; no real client or connection is created.</summary>
        internal BillingClient Sdk { get; } = new();
        /// <summary>Gets the actual retained task through scoped test-only inspection.</summary>
        internal Task<IReadOnlyList<NativeBillingPurchase>>? NativeTask =>
            ((TaskCompletionSource<IReadOnlyList<NativeBillingPurchase>>?)typeof(GooglePlayBillingStore).GetField("_payment", Fields)!.GetValue(Store))?.Task;
        /// <summary>Gets the original intent paired with the actual retained task.</summary>
        internal Guid? NativeIntent => (Guid?)typeof(GooglePlayBillingStore).GetField("_paymentIntent", Fields)!.GetValue(Store);
        /// <summary>Arranges local readiness and one prior offer handle without a store effect.</summary>
        internal Fixture(bool ready = true)
        {
            Sdk.IsReady = ready; typeof(GooglePlayBillingStore).GetField("_client", Fields)!.SetValue(Store, Sdk);
            var offers = (IDictionary)typeof(GooglePlayBillingStore).GetField("_offers", Fields)!.GetValue(Store)!;
            offers.Add("controlled-handle", (new ProductDetails(), (string?)null, BillingProduct.PlusLifetime));
        }
        /// <summary>Calls the original purchase method with a synthetic binding and records its observer.</summary>
        internal Task<IReadOnlyList<NativeBillingPurchase>> Purchase(Guid intent, CancellationToken ct)
        {
            var result = Store.PurchaseAsync(intent, "controlled-handle", new string('0', 64), ct); observations.Add(result); return result;
        }
        /// <summary>Delivers only a controlled cancellation response to the original adapter callback.</summary>
        internal void CancelSdk() => Store.OnPurchasesUpdated(new BillingResult { ResponseCode = 1 }, null);
        /// <summary>Closes only this fixture's controlled pending observations and disposable adapter.</summary>
        public void Dispose()
        {
            // Close only controlled tasks, including an uncompleted simulated connection; this is not a store callback claim.
            Store.OnBillingSetupFinished(new BillingResult { ResponseCode = 2 }); CancelSdk();
            foreach (var observation in observations) _ = ObserveShutdownAsync(observation);
            Store.Dispose();
        }
        private static async Task ObserveShutdownAsync(Task observation)
        {
            try { await observation; }
            catch (Exception error) when (error is not OutOfMemoryException) { }
        }
    }
}
