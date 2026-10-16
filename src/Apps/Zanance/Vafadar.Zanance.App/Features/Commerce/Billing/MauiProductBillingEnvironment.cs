using System.Net;
using Vafadar.Zanance.App.Features.Sync;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Data.Billing;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.App.Features.Commerce.Billing;

/// <summary>Original main mobile store composition, with separate permanent anonymous Plus and actual identity Pro principals.</summary>
internal sealed class MauiProductBillingEnvironment(PersonalSyncLifetime lifetime, IPersonalSyncTokenStore tokens,
    IBillingPrivateStorage storage, IBillingNativeStore? native = null) : IProductBillingEnvironment, IBillingIdentityEnvironment
{
    private readonly SemaphoreSlim requests = new(1);
    /// <inheritdoc />
    public bool Available => BillingBuild.IsAvailable && native is not null;
    /// <inheritdoc />
    public Task<ProductBillingPresentation> LoadAsync(CancellationToken ct) => RunAsync(ReadCurrentAsync, ct);
    /// <inheritdoc />
    public Task<ProductBillingPresentation> PurchaseAsync(NativeBillingOffer selected, CancellationToken ct) => RunAsync(async (context, token) =>
    {
        var refreshed = await ReadCurrentAsync(context, token);
        var match = refreshed.Offers.SingleOrDefault(p => p.StoreIdentity == selected.StoreIdentity && p.Offer.Product == selected.Offer.Product &&
            p.Offer.Kind == selected.Offer.Kind && p.Offer.Country == selected.Offer.Country && p.Offer.StartsAt == selected.Offer.StartsAt &&
            p.Offer.EndsAt == selected.Offer.EndsAt && p.Offer.Phases.SequenceEqual(selected.Offer.Phases) &&
            p.Replacement == selected.Replacement && p.ExistingSubscription == selected.ExistingSubscription);
        if (match is null) return refreshed with { OfferChanged = refreshed.Status == ProductBillingStatus.Ready };
        // A deferred Pro-to-Plus change still belongs to the original Pro identity until it becomes effective.
        var useIdentity = BillingProducts.Plan(match.Offer.Product) == ProductPlan.Pro ||
            match.Replacement is { } from && BillingProducts.Plan(from.FromProduct) == ProductPlan.Pro;
        var client = context.Client(useIdentity); var fresh = useIdentity ? refreshed.Identity : refreshed.Local;
        if (fresh?.Snapshot is null || useIdentity && fresh.Snapshot.IdentityHash is null) throw new NativeBillingRefusal("PurchaseIdentityRequired");
        var previouslyOwned = context.CurrentOwned;
        NativeBillingReplacement? nativeReplacement = null;
        if (match.Replacement is { } replacement)
        {
            var original = previouslyOwned.SingleOrDefault(purchase => BillingAccountBinding.PurchaseId(native!.Distribution, BillingBuild.Environment, purchase.Reference) == replacement.PurchaseId &&
                purchase.StoreProductId == replacement.StoreProductId) ?? throw new NativeBillingRefusal("OriginalSubscriptionChanged");
            nativeReplacement = new(original.Reference, replacement.StoreProductId, replacement.Timing);
        }
        var intent = await client.BeginNativeAsync(context.Access, match.Offer.Product, token,
            previouslyOwned.Select(p => p.Reference).ToArray(), match.Replacement?.PurchaseId);
        IReadOnlyList<NativeBillingPurchase> purchases;
        try
        {
            var account = fresh.Snapshot!.AccountToken;
            purchases = await native!.PurchaseAsync(intent, match.Handle, native.Distribution == BillingIssuer.GooglePlay
                ? BillingAccountBinding.Google(account, useIdentity) : account.ToString("D"), token, nativeReplacement);
        }
        catch (NativeBillingRefusal error) when (error.Message == "StoreCanceled")
        { await client.CancelNativeAsync(context.Access, intent, token); return await ReadCurrentAsync(context, token); }
        catch (NativeBillingRefusal error) when (error.Message is "StoreNotLaunched" or "StoreRestoreRequired")
        {
            await client.CancelNativeAsync(context.Access, intent, token);
            if (error.Message == "StoreRestoreRequired") return await ReadCurrentAsync(context, token);
            throw;
        }
        foreach (var purchase in purchases)
        {
            var verified = await client.VerifyAsync(context.Access, purchase.Reference, purchase.Hint, false, token);
            if (verified.MayFinish)
            {
                await native.FinishAsync(purchase, verified, token);
                await client.FinishNativeAsync(context.Access, intent, token);
            }
        }
        return await ReadCurrentAsync(context, token);
    }, ct);

    /// <inheritdoc />
    public Task<ProductBillingPresentation> RestoreAsync(CancellationToken ct) => RunAsync((context, token) => RestoreCoreAsync(context, token, explicitRestore: true), ct);
    /// <inheritdoc />
    public Task<ProductBillingPresentation> ContinueAsync(CancellationToken ct) => RunAsync(async (context, token) =>
    {
        var continued = await ContinueNativeAsync(context.Client(false), context, token);
        if (await context.HasIdentityAsync(token)) continued |= await ContinueNativeAsync(context.Client(true), context, token);
        return continued ? await ReadCurrentAsync(context, token) : await RestoreCoreAsync(context, token, explicitRestore: false);
    }, ct);
    /// <inheritdoc />
    public Task ManageAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (!Available) throw new NativeBillingRefusal("StoreUnavailable");
        return MainThread.InvokeOnMainThreadAsync(async () =>
        {
            ct.ThrowIfCancellationRequested();
            // This fixed HTTPS Play surface cannot become a user-supplied checkout URL or trigger a purchase itself.
            await Launcher.Default.OpenAsync(new Uri("https://play.google.com/store/account/subscriptions?package=pro.vafadar.zanance"));
        });
    }

    /// <inheritdoc />
    public Task<bool> ConnectedAsync(CancellationToken ct) => IdentityWorkAsync((context, token) => context.HasIdentityAsync(token), ct);
    /// <inheritdoc />
    public async Task SignInAsync(string email, string password, CancellationToken ct)
        => await IdentityWorkAsync(async (context, token) => { await context.IdentityTransport.SignInIdentityAsync(email, password, token); return true; }, ct);
    /// <inheritdoc />
    public Task<bool> SignOutAsync(CancellationToken ct) => IdentityWorkAsync(async (context, token) =>
    {
        try { await context.IdentityTransport.SignOutIdentityAsync(token); return true; }
        catch (Exception error) when (IsOutage(error) || error is PersonalSyncHttpException { Status: HttpStatusCode.Unauthorized })
        { return false; } // The transport already retired the local credential under its shared peer gate.
    }, ct);
    /// <inheritdoc />
    public async Task RegisterAsync(string email, string password, CancellationToken ct)
        => await IdentityWorkAsync(async (context, token) => { await context.IdentityTransport.RegisterIdentityAsync(email, password, token); return true; }, ct);
    /// <inheritdoc />
    public async Task RecoverAsync(string email, CancellationToken ct)
        => await IdentityWorkAsync(async (context, token) => { await context.IdentityTransport.RecoverIdentityAsync(email, token); return true; }, ct);
    /// <inheritdoc />
    public async Task CompleteAsync(Guid challenge, string token, string? newPassword, CancellationToken ct)
        => await IdentityWorkAsync(async (context, cancellation) => { await context.IdentityTransport.CompleteIdentityAsync(challenge, token, newPassword, cancellation); return true; }, ct);
    private async Task<T> IdentityWorkAsync<T>(Func<Context, CancellationToken, Task<T>> work, CancellationToken ct)
    {
        if (!Available) throw new NativeBillingRefusal("IdentityUnavailable");
        await requests.WaitAsync(ct);
        try
        {
            var captured = lifetime.CaptureBilling(); using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, captured.Cancellation);
            using var context = new Context(tokens, storage, native!.Distribution, new MauiBillingAccess(captured.Validate));
            // Credentials remain in the original request only. Every transport request also validates captured native access.
            var result = await work(context, linked.Token); captured.Validate(); return result;
        }
        finally { requests.Release(); }
    }

    private async Task<bool> ContinueNativeAsync(BillingClient client, Context context, CancellationToken ct)
    {
        await client.ContinueAsync(context.Access, ct);
        var view = await client.ReadAsync(context.Access, false, ct);
        if (view.NativePaymentId is not { } intent) return false;
        IReadOnlyList<NativeBillingPurchase>? original;
        try { original = await native!.ContinuePaymentAsync(intent, ct); }
        catch (NativeBillingRefusal error) when (error.Message == "StoreCanceled")
        { await client.CancelNativeAsync(context.Access, intent, ct); return true; }
        catch (NativeBillingRefusal error) when (error.Message is "StoreNotLaunched" or "StoreRestoreRequired")
        { await client.CancelNativeAsync(context.Access, intent, ct); return false; }
        if (original is null) return false; // Process loss uses an explicit current owned-purchase query instead.
        foreach (var purchase in original)
        {
            if (!await client.MatchesNativeAsync(context.Access, purchase.Reference, purchase.Hint, ct)) continue;
            var verified = await client.LastCompletionAsync(context.Access, intent, ct);
            if (verified?.MayFinish != true) verified = await client.VerifyAsync(context.Access, purchase.Reference, purchase.Hint, false, ct);
            if (verified.MayFinish)
            {
                await native!.FinishAsync(purchase, verified, ct);
                await client.FinishNativeAsync(context.Access, intent, ct);
            }
        }
        return true;
    }

    private async Task<ProductBillingPresentation> RestoreCoreAsync(Context context, CancellationToken ct, bool explicitRestore)
    {
        var localClient = context.Client(false);
        if (explicitRestore) await localClient.ReenrollForStoreRestoreAsync(context.Access, ct);
        var local = await localClient.ObserveAsync(context.Access, ct);
        var hasIdentity = await context.HasIdentityAsync(ct);
        if (explicitRestore && hasIdentity) await context.Client(true).ReenrollForStoreRestoreAsync(context.Access, ct);
        var catalog = local.Snapshot?.Catalog ?? [];
        if (catalog.Count == 0) return new(ProductBillingStatus.NotConfigured, local, null, [], false, hasIdentity);
        var owned = await native!.OwnedAsync(catalog, ct);
        foreach (var originalPurchase in owned)
        {
            // Pending SDK results are not a reason to repeatedly verify or launch another charge.
            var identityView = hasIdentity ? await context.Client(true).ReadAsync(context.Access, false, ct) : null;
            var pro = BillingProducts.Plan(originalPurchase.Hint) == ProductPlan.Pro ||
                identityView is { NativePaymentId: not null, NativeReplacementId: not null } &&
                BillingOfferSelection.OriginalNativeProduct(identityView, originalPurchase, catalog) is not null;
            if (pro && !hasIdentity) continue;
            var client = context.Client(pro);
            var originalView = await client.ReadAsync(context.Access, false, ct);
            // The SDK identifies the owned SKU, not a subscription base plan. The service rechecks the exact
            // original product against fresh provider facts before issuing any native completion receipt.
            var product = originalView.NativePaymentId is not null
                ? BillingOfferSelection.OriginalNativeProduct(originalView, originalPurchase, catalog) : originalPurchase.Hint;
            if (product is null) continue; // An unrelated owned Lifetime/SKU cannot pin or complete this original subscription intent.
            var purchase = originalPurchase with { Hint = product.Value };
            if (!explicitRestore && originalView.NativePaymentId is null) continue;
            if (!await client.MatchesNativeAsync(context.Access, purchase.Reference, purchase.Hint, ct)) continue;
            var verified = originalView.NativePaymentId is { } originalIntent
                ? await client.LastCompletionAsync(context.Access, originalIntent, ct) : null;
            if (verified?.MayFinish != true)
                verified = await client.VerifyAsync(context.Access, purchase.Reference, purchase.Hint, explicitRestore, ct);
            if (verified.MayFinish)
            {
                await native.FinishAsync(purchase, verified, ct);
                var view = await client.ReadAsync(context.Access, false, ct);
                if (view.NativePaymentId is { } intent) await client.FinishNativeAsync(context.Access, intent, ct);
            }
            if (explicitRestore && !pro && hasIdentity && identityView?.NativePaymentId is null)
            {
                // Explicit Restore explains this optional association. It does not make local Plus require an account.
                await context.Client(true).VerifyAsync(context.Access, purchase.Reference, purchase.Hint, true, ct);
            }
        }
        var pending = owned.Any(purchase => purchase.Pending);
        // Only this explicit actual store query can release a new restore-only private context. No charge is launched.
        if (explicitRestore)
        {
            await localClient.ObserveAsync(context.Access, ct);
            await localClient.CompleteStoreRestoreAsync(context.Access, pending, ct);
            if (hasIdentity)
            {
                await context.Client(true).ObserveAsync(context.Access, ct);
                await context.Client(true).CompleteStoreRestoreAsync(context.Access, pending, ct);
            }
        }
        return await ReadCurrentAsync(context, ct);
    }

    private async Task<ProductBillingPresentation> ReadCurrentAsync(Context context, CancellationToken ct)
    {
        var localClient = context.Client(false); BillingClientView? local = null, identity = null;
        try
        {
            local = await localClient.ObserveAsync(context.Access, ct);
            var hasIdentity = await context.HasIdentityAsync(ct);
            if (hasIdentity) identity = await context.Client(true).ObserveAsync(context.Access, ct);
            var current = identity?.Snapshot ?? local.Snapshot; var catalog = current?.Catalog ?? [];
            var pending = local.HasPendingCommand || local.HasUnresolvedNativePayment ||
                identity is { HasPendingCommand: true } or { HasUnresolvedNativePayment: true } ||
                local.Snapshot?.Grants.Any(grant => grant.Facts.State == BillingPurchaseState.Pending) == true ||
                identity?.Snapshot?.Grants.Any(grant => grant.Facts.State == BillingPurchaseState.Pending) == true;
            if (pending) return new(ProductBillingStatus.Pending, local, identity, [], true, hasIdentity);
            if (local.RequiresStoreRestore || identity?.RequiresStoreRestore == true)
                return new(ProductBillingStatus.RecoveryRequired, local, identity, [], true, hasIdentity);
            if (catalog.Count == 0) return new(ProductBillingStatus.NotConfigured, local, identity, [], false, hasIdentity);
            var country = await native!.CountryAsync(ct); var actual = await native.CatalogAsync(catalog, ct);
            context.CurrentOwned = await native.OwnedAsync(catalog, ct);
            return BillingOfferSelection.Select(native.Distribution, BillingBuild.Environment, local, identity,
                catalog, context.CurrentOwned, actual, country, hasIdentity);
        }
        catch (BillingPrivateStateUnavailableException) when (!ct.IsCancellationRequested)
        {
            // A loss marker or corrupt private key never creates a replacement on Load. Restore has its own explicit consent.
            return new(ProductBillingStatus.RecoveryRequired, null, null, [], true, false);
        }
        catch (PersonalSyncHttpException error) when (error.Status == HttpStatusCode.Gone && !ct.IsCancellationRequested)
        { return new(ProductBillingStatus.RecoveryRequired, null, null, [], true, await context.HasIdentityAsync(ct)); }
        catch (NativeBillingRefusal error) when (error.Message == "UnsupportedOwnedSubscription" && !ct.IsCancellationRequested)
        { return new(ProductBillingStatus.ManageExisting, local, identity, [], true, await context.HasIdentityAsync(ct)); }
        catch (Exception error) when (IsOutage(error) && !ct.IsCancellationRequested)
        {
            local = await localClient.ReadAsync(context.Access, true, ct);
            var hasIdentity = await context.HasIdentityAsync(ct);
            if (hasIdentity) identity = await context.Client(true).ReadAsync(context.Access, true, ct);
            return new(ProductBillingStatus.VerificationUnavailable, local, identity, [], false, hasIdentity);
        }
    }

    private static bool IsOutage(Exception error) => error is HttpRequestException or TaskCanceledException ||
        error is PersonalSyncHttpException http && (int)http.Status >= 500;

    private async Task<ProductBillingPresentation> RunAsync(Func<Context, CancellationToken, Task<ProductBillingPresentation>> work, CancellationToken ct)
    {
        if (!Available) return new(ProductBillingStatus.Unavailable, null, null, [], false, false);
        await requests.WaitAsync(ct);
        try
        {
            var captured = lifetime.CaptureBilling(); using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, captured.Cancellation);
            using var context = new Context(tokens, storage, native!.Distribution, new MauiBillingAccess(captured.Validate));
            ProductBillingPresentation result;
            try { result = await work(context, linked.Token); }
            catch (BillingPrivateStateUnavailableException) when (!linked.IsCancellationRequested)
            { result = new(ProductBillingStatus.RecoveryRequired, null, null, [], true, false); }
            catch (PersonalSyncHttpException error) when (error.Status == HttpStatusCode.Gone && !linked.IsCancellationRequested)
            { result = new(ProductBillingStatus.RecoveryRequired, null, null, [], true, await context.HasIdentityAsync(linked.Token)); }
            catch (Exception error) when (!linked.IsCancellationRequested)
            {
                // Re-read private continuation only: never retry successful HTTP/provider work to repair its presentation.
                var local = await context.Client(false).ReadAsync(context.Access, IsOutage(error), linked.Token);
                var hasIdentity = await context.HasIdentityAsync(linked.Token);
                var identity = hasIdentity && error is not PersonalSyncHttpException { Status: HttpStatusCode.Unauthorized }
                    ? await context.Client(true).ReadAsync(context.Access, IsOutage(error), linked.Token) : null;
                var pending = local.HasPendingCommand || local.HasUnresolvedNativePayment ||
                    identity is { HasPendingCommand: true } or { HasUnresolvedNativePayment: true };
                result = new(pending ? ProductBillingStatus.Pending : ProductBillingStatus.VerificationUnavailable, local, identity, [], pending, hasIdentity);
            }
            captured.Validate(); return result;
        }
        finally { requests.Release(); }
    }

    private sealed class Context : IDisposable
    {
        private readonly BillingHttpTransport anonymousTransport;
        private readonly BillingHttpTransport identityTransport;
        private readonly BillingClient anonymous;
        private readonly BillingClient identity;
        internal MauiBillingAccess Access { get; }
        internal BillingHttpTransport IdentityTransport => identityTransport;
        /// <summary>Ephemeral native ownership from this exact preflight, never persisted or presented.</summary>
        internal IReadOnlyList<NativeBillingPurchase> CurrentOwned { get; set; } = [];
        internal Context(IPersonalSyncTokenStore tokens, IBillingPrivateStorage storage, BillingIssuer issuer, MauiBillingAccess access)
        {
            Access = access;
            // Personal-sync settings never select a paid issuer. Qualification host injection belongs to a separate exact review build.
            var peer = new LocalSyncPeer("https://api.zanance.vafadar.pro/", "");
            anonymousTransport = new(peer, tokens, access.Validate, useIdentity: false);
            identityTransport = new(peer, tokens, access.Validate);
            anonymous = new(anonymousTransport, storage, issuer, BillingBuild.Environment);
            identity = new(identityTransport, storage, issuer, BillingBuild.Environment);
        }
        internal BillingClient Client(bool useIdentity) => useIdentity ? identity : anonymous;
        internal async Task<bool> HasIdentityAsync(CancellationToken ct)
        {
            try { return await identityTransport.CurrentUserAsync(ct) is not null; }
            catch (Exception error) when (error is InvalidDataException or System.Text.Json.JsonException or System.Security.Cryptography.CryptographicException)
            { return false; } // An unreadable optional session must not force an account for anonymous Plus restoration.
#if ANDROID
            catch (Java.Security.GeneralSecurityException) { return false; }
#endif
        }
        public void Dispose() { anonymousTransport.Dispose(); identityTransport.Dispose(); }
    }
}
