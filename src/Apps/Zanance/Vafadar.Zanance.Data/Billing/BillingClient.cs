using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Vafadar.Cryptography;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.Data.Billing;

/// <summary>Original protected device/principal requests and signed cache, independent of financial files and commercial activation.</summary>
public sealed class BillingClient(IBillingServiceTransport transport, IBillingPrivateStorage storage,
    BillingIssuer distribution, BillingEnvironment environment)
{
    private const string RegistrationPurpose = "zanance.billing.registration.v1";
    private const string CommandPurpose = "zanance.billing.command.v1";
    private const string SnapshotPurpose = "zanance.billing.snapshot.v1";
    private const string TimePurpose = "zanance.billing.time.v1";
    private const string ReceiptPurpose = "zanance.billing.receipt.v1";
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, SemaphoreSlim> Gates = new(StringComparer.Ordinal);
    // Native contexts are recreated between foreground calls. Serialize their original slots across instances too.
    private readonly SemaphoreSlim gate = Gates.GetOrAdd(transport.PeerBinding + "\0" + distribution + "\0" + environment, static _ => new(1));

    /// <summary>Reads only this actual optional identity's original private cache; missing state does not create keys or imply a refund.</summary>
    public async Task<BillingClientView> ReadAsync(IBillingNativeAccess access, bool allowOutage, CancellationToken ct = default)
    {
        await gate.WaitAsync(ct);
        try
        {
            var current = await OpenAsync(access, false, ct);
            return View(current.State, access.ReadMonotonic(), allowOutage);
        }
        finally { gate.Release(); }
    }

    /// <summary>Obtains one genuinely fresh complete current observation; an old operation response cannot enter the cache.</summary>
    public async Task<BillingClientView> ObserveAsync(IBillingNativeAccess access, CancellationToken ct = default)
    {
        await gate.WaitAsync(ct);
        try
        {
            var current = await OpenAsync(access, true, ct);
            var state = await RegisteredAsync(current.State!, current.User, access, ct);
            if (state.DeviceRetired) throw Refusal();
            var before = access.ReadMonotonic();
            var proof = await TimeAsync(state, current.User, access, ct);
            using var key = Key(state);
            var command = new BillingCommand(state.Customer, Guid.NewGuid(), BillingOperation.Observe, state.Revision,
                state.IdentityHash, null, null, proof.ServerTime.AddMinutes(5));
            var signed = Sign(command, key, CommandPurpose, BillingJson.Default.BillingCommand);
            var reply = await SendCommandAsync(state, current.User, signed, access, ct);
            await RequireScopeAsync(current.User, access, ct);
            if (reply.OperationId != command.OperationId || reply.MayFinish || reply.State is not null || reply.SignedReceipt is not null)
                throw Refusal();
            var snapshot = ReadSnapshot(reply.SignedSnapshot, state);
            BillingSnapshotPolicy.RequireCurrent(snapshot, new(state.Customer, command.OperationId, key.Public.Fingerprint,
                distribution, environment, state.IdentityHash, state.Revision, Max(state.MinimumServerTime, proof.ServerTime)));
            var after = access.ReadMonotonic();
            if (before.IsValid && (!after.IsValid || before.BootIdentity != after.BootIdentity || after.ElapsedMilliseconds < before.ElapsedMilliseconds))
                throw Refusal();
            state = state with { Cache = reply.SignedSnapshot, CacheRequest = command.OperationId,
                Anchor = new(snapshot.ServerTime, before), Revision = snapshot.Revision, MinimumServerTime = snapshot.ServerTime,
                DeviceRetired = snapshot.Retired };
            await SaveAsync(state, current.User, access, ct);
            return View(state, after, false);
        }
        finally { gate.Release(); }
    }

    /// <summary>Retains one original verification request before HTTP. Pending payment never triggers another native purchase.</summary>
    public Task<BillingCommandResult> VerifyAsync(IBillingNativeAccess access, string reference, BillingProduct hint,
        bool restore, CancellationToken ct = default)
    {
        if (!Enum.IsDefined(hint) || reference.Length is < 1 or > 8192 || reference.Any(char.IsControl)) throw Refusal();
        return StartAsync(access, restore ? BillingOperation.RestorePurchase : BillingOperation.VerifyPurchase, reference, hint, ct);
    }
    /// <summary>Explicitly rechecks the service's already owned finite purchase list, not a new native payment.</summary>
    public Task<BillingCommandResult> RefreshAsync(IBillingNativeAccess access, CancellationToken ct = default)
        => StartAsync(access, BillingOperation.Refresh, null, null, ct);

    /// <summary>Continues only the original literal request. A lost response never changes its operation id, consent or keys.</summary>
    public async Task<BillingCommandResult?> ContinueAsync(IBillingNativeAccess access, CancellationToken ct = default)
    {
        await gate.WaitAsync(ct);
        try
        {
            var current = await OpenAsync(access, false, ct);
            if (current.State?.PendingCommand is null) return null;
            return await ExecuteOriginalAsync(current.State, current.User, access, ct);
        }
        finally { gate.Release(); }
    }

    /// <summary>Explicitly establishes a separate restore-only principal after local loss, preserving every original key/request slot.</summary>
    public async Task ReenrollForStoreRestoreAsync(IBillingNativeAccess access, CancellationToken ct = default)
    {
        await gate.WaitAsync(ct);
        try
        {
            try
            {
                var original = await OpenAsync(access, false, ct);
                if (original.State?.DeviceRetired != true) return; // Healthy or genuinely fresh state is never replaced.
            }
            catch (BillingPrivateStateUnavailableException) { }
            var user = await transport.CurrentUserAsync(ct); access.Validate();
            var hash = user is null ? null : BillingAccountBinding.Identity(user);
            BillingPrivateRoute route;
            try { route = await ReadRouteAsync(hash, ct); }
            catch (BillingPrivateStateUnavailableException) { route = new(Guid.Empty); }
            if (route.Candidate is null)
            {
                route = route with { Candidate = Guid.NewGuid() };
                await WriteRouteAsync(hash, route, user, access, ct);
            }
            var generation = route.Candidate!.Value;
            BillingClientState? candidate = null;
            try
            {
                var encoded = await storage.ReadAsync(StateSlot(hash, generation), ct);
                if (encoded is not null) candidate = ReadPrivateState(encoded, hash, generation);
            }
            catch (BillingPrivateStateUnavailableException)
            {
                // A candidate cannot perform HTTP before route publication. Explicit recovery may use a new
                // namespace if that unaccepted candidate itself was lost; its original slot remains untouched.
                route = route with { Candidate = Guid.NewGuid() }; generation = route.Candidate.Value;
                await WriteRouteAsync(hash, route, user, access, ct);
            }
            candidate ??= NewState(hash, generation, restoreOnly: true);
            if (!candidate.RequiresStoreRestore || candidate.Authority is not null || candidate.PendingCommand is not null || candidate.Cache is not null)
                throw Refusal();
            await RequireScopeAsync(user, access, ct);
            await WriteStateAsync(candidate, ct);
            // A cut here resumes the same saved candidate/key; ordinary reads cannot skip unfinished routing.
            if (await ReadRouteAsync(hash, ct) != route) throw Refusal();
            await WriteRouteAsync(hash, new(generation), user, access, ct);
        }
        finally { gate.Release(); }
    }

    /// <summary>Releases restore-only purchase entry only after the explicit native owned query and fresh verified current state.</summary>
    public async Task CompleteStoreRestoreAsync(IBillingNativeAccess access, bool nativePurchasePending, CancellationToken ct = default)
    {
        await gate.WaitAsync(ct);
        try
        {
            var current = await OpenAsync(access, false, ct); var state = current.State;
            if (state is null || !state.RequiresStoreRestore) return;
            if (nativePurchasePending || state.DeviceRetired || state.PendingCommand is not null || state.NativePayment is not null || state.Cache is null || state.Anchor is null)
                return;
            var snapshot = ReadSnapshot(state.Cache, state); var now = state.Anchor.Read(access.ReadMonotonic());
            if (now is null || now < snapshot.ServerTime || now - snapshot.ServerTime > TimeSpan.FromMinutes(2) ||
                snapshot.Grants.Any(grant => grant.Facts.State == BillingPurchaseState.Pending)) return;
            await SaveAsync(state with { RequiresStoreRestore = false }, current.User, access, ct);
        }
        finally { gate.Release(); }
    }

    /// <summary>Records the original payment intent before launching the native store, under a genuinely fresh verified preflight.</summary>
    public async Task<Guid> BeginNativeAsync(IBillingNativeAccess access, BillingProduct product, CancellationToken ct = default,
        IReadOnlyList<string>? ownedReferences = null, string? replacementId = null)
    {
        if (!Enum.IsDefined(product) || ownedReferences?.Count > 32 || replacementId is not null &&
            (replacementId.Length != 64 || !replacementId.All(char.IsAsciiHexDigit) || BillingProducts.Period(product) == BillingPeriod.None)) throw Refusal();
        var previous = ownedReferences?.Select(BillingAccountBinding.ReceiptHash).Distinct(StringComparer.Ordinal).ToArray() ?? [];
        await gate.WaitAsync(ct);
        try
        {
            var current = await OpenAsync(access, false, ct); var state = current.State ?? throw Refusal();
            if (state.NativePayment is not null || state.PendingCommand is not null || state.DeviceRetired || state.RequiresStoreRestore || state.Cache is null || state.Anchor is null)
                throw new InvalidOperationException("Original billing work requires continuation.");
            var snapshot = ReadSnapshot(state.Cache, state); var now = state.Anchor.Read(access.ReadMonotonic());
            if (now is null || now < snapshot.ServerTime || now - snapshot.ServerTime > TimeSpan.FromMinutes(2) ||
                BillingProducts.RequiresIdentity(product, distribution) && current.User is null)
                throw Refusal();
            if (BillingProducts.Plan(product) == ProductPlan.Plus && snapshot.Grants.Any(p => p.Facts.Product == BillingProduct.PlusLifetime && p.Facts.HasAccessAt(now.Value)))
                throw new InvalidOperationException("The verified Lifetime base already includes Plus.");
            var subscriptions = snapshot.Grants.Where(grant => BillingProducts.Period(grant.Facts.Product) != BillingPeriod.None && grant.Facts.HasAccessAt(now.Value)).ToArray();
            if (BillingProducts.Period(product) != BillingPeriod.None && (replacementId is null && subscriptions.Length != 0 ||
                replacementId is not null && !subscriptions.Any(grant => grant.PurchaseId == replacementId && grant.Issuer == distribution &&
                    grant.Facts.State == BillingPurchaseState.Active && grant.Facts.ScheduledChange is null)))
                throw new InvalidOperationException("Review and replace the actual existing subscription instead of starting a parallel subscription.");
            var intent = Guid.NewGuid(); state = state with { NativePayment = intent, LastOperation = null, LastPurchaseState = null,
                NativeProduct = product, NativePriorReceipts = previous, NativeReceipt = null, NativeReplacementId = replacementId };
            await SaveAsync(state, current.User, access, ct); return intent;
        }
        finally { gate.Release(); }
    }

    /// <summary>Tests the original native target before an explicit owned-purchase restore; older unrelated receipts cannot finish it.</summary>
    public async Task<bool> MatchesNativeAsync(IBillingNativeAccess access, string reference, BillingProduct product, CancellationToken ct = default)
    {
        await gate.WaitAsync(ct);
        try
        {
            var current = await OpenAsync(access, false, ct);
            return current.State?.NativePayment is null || MatchesNative(current.State, reference, product);
        }
        finally { gate.Release(); }
    }

    private static bool MatchesNative(BillingClientState state, string reference, BillingProduct product)
    {
        var hash = BillingAccountBinding.ReceiptHash(reference);
        // Older protected formats without an original target remain unresolved rather than guessing their payment.
        return state.NativeProduct == product && state.NativePriorReceipts is not null &&
            !state.NativePriorReceipts.Contains(hash, StringComparer.Ordinal) &&
            (state.NativeReceipt is null || state.NativeReceipt == hash);
    }

    /// <summary>Closes only a canceled or synchronously refused native launch before any verification or uncertain provider receipt.</summary>
    public async Task CancelNativeAsync(IBillingNativeAccess access, Guid intent, CancellationToken ct = default)
    {
        await gate.WaitAsync(ct);
        try
        {
            var current = await OpenAsync(access, false, ct); var state = current.State ?? throw Refusal();
            if (state.NativePayment != intent || state.PendingCommand is not null || state.LastOperation is not null) throw Refusal();
            await SaveAsync(state with { NativePayment = null, NativeProduct = null, NativePriorReceipts = null, NativeReceipt = null, NativeReplacementId = null }, current.User, access, ct);
        }
        finally { gate.Release(); }
    }

    /// <summary>Returns only the previously persisted signed completion for this exact native intent; it does not query the store again.</summary>
    public async Task<BillingCommandResult?> LastCompletionAsync(IBillingNativeAccess access, Guid intent, CancellationToken ct = default)
    {
        await gate.WaitAsync(ct);
        try
        {
            var current = await OpenAsync(access, false, ct); var state = current.State ?? throw Refusal();
            if (state.NativePayment != intent || state.PendingCommand is not null || state.LastOperation is null) return null;
            var reply = StrictJsonPayload.Deserialize(Encoding.UTF8.GetBytes(state.LastOperation), BillingJson.Default.BillingCommandResult, 262144);
            if (reply.SignedReceipt is null) throw Refusal();
            var receipt = StandardJoseSignatures.VerifyJson(reply.SignedReceipt, Public(state.Authority ?? throw Refusal()), ReceiptPurpose,
                8192, BillingJson.Default.BillingOperationReceipt);
            if (receipt.CustomerId != state.Customer || receipt.OperationId != reply.OperationId || receipt.NativePaymentId != intent ||
                receipt.DeviceFingerprint != Public(state.PublicIdentity).Fingerprint || receipt.Distribution != distribution || receipt.Environment != environment ||
                receipt.IdentityHash != state.IdentityHash || receipt.MayFinish != reply.MayFinish || receipt.State != reply.State ||
                receipt.MayFinish && receipt.CompletedProduct != state.NativeProduct) throw Refusal();
            return reply;
        }
        finally { gate.Release(); }
    }

    /// <summary>Closes the exact intent only after the native adapter has finished its service-verified original completion.</summary>
    public async Task FinishNativeAsync(IBillingNativeAccess access, Guid intent, CancellationToken ct = default)
    {
        await gate.WaitAsync(ct);
        try
        {
            var current = await OpenAsync(access, false, ct); var state = current.State ?? throw Refusal();
            if (state.NativePayment != intent || state.PendingCommand is not null || state.LastOperation is null) throw Refusal();
            var receipt = StrictJsonPayload.Deserialize(Encoding.UTF8.GetBytes(state.LastOperation), BillingJson.Default.BillingCommandResult, 262144);
            if (!receipt.MayFinish || receipt.SignedReceipt is null) throw Refusal();
            var facts = StandardJoseSignatures.VerifyJson(receipt.SignedReceipt, Public(state.Authority ?? throw Refusal()), ReceiptPurpose,
                8192, BillingJson.Default.BillingOperationReceipt);
            if (!facts.MayFinish || facts.CustomerId != state.Customer || facts.OperationId != receipt.OperationId || facts.IdentityHash != state.IdentityHash ||
                facts.Environment != environment || facts.Distribution != distribution || facts.NativePaymentId != intent || facts.CompletedProduct != state.NativeProduct) throw Refusal();
            await SaveAsync(state with { NativePayment = null, NativeProduct = null, NativePriorReceipts = null, NativeReceipt = null, NativeReplacementId = null }, current.User, access, ct);
        }
        finally { gate.Release(); }
    }

    private async Task<BillingCommandResult> StartAsync(IBillingNativeAccess access, BillingOperation operation,
        string? reference, BillingProduct? hint, CancellationToken ct)
    {
        await gate.WaitAsync(ct);
        try
        {
            var current = await OpenAsync(access, true, ct);
            var state = await RegisteredAsync(current.State!, current.User, access, ct);
            if (state.DeviceRetired || state.PendingCommand is not null) throw new InvalidOperationException("Original billing work requires continuation.");
            if (state.NativePayment is not null && operation == BillingOperation.Refresh)
                throw new InvalidOperationException("Continue the original payment before starting another verification operation.");
            if (state.NativePayment is not null && operation != BillingOperation.Refresh)
            {
                if (reference is null || hint is null || !MatchesNative(state, reference, hint.Value)) throw Refusal();
                state = state with { NativeReceipt = BillingAccountBinding.ReceiptHash(reference) };
            }
            var proof = await TimeAsync(state, current.User, access, ct); using var key = Key(state);
            var command = new BillingCommand(state.Customer, Guid.NewGuid(), operation, state.Revision, state.IdentityHash,
                hint, reference, proof.ServerTime.AddMinutes(5), operation == BillingOperation.Refresh ? null : state.NativePayment,
                operation == BillingOperation.Refresh ? null : state.NativeReplacementId);
            state = state with { PendingCommand = Sign(command, key, CommandPurpose, BillingJson.Default.BillingCommand) };
            await SaveAsync(state, current.User, access, ct);
            return await ExecuteOriginalAsync(state, current.User, access, ct);
        }
        finally { gate.Release(); }
    }

    private async Task<BillingCommandResult> ExecuteOriginalAsync(BillingClientState state, string? user, IBillingNativeAccess access, CancellationToken ct)
    {
        var original = state.PendingCommand ?? throw Refusal(); using var key = Key(state);
        var command = StandardJoseSignatures.VerifyJson(original, key.Public, CommandPurpose, 32768, BillingJson.Default.BillingCommand);
        await RequireScopeAsync(user, access, ct);
        var reply = await SendCommandAsync(state, user, original, access, ct);
        await RequireScopeAsync(user, access, ct);
        var snapshot = ReadSnapshot(reply.SignedSnapshot, state);
        if (snapshot.Kind != BillingSnapshotKind.Operation || snapshot.RequestId != command.OperationId || reply.OperationId != command.OperationId ||
            reply.SignedReceipt is null) throw Refusal();
        var receipt = StandardJoseSignatures.VerifyJson(reply.SignedReceipt, Public(state.Authority ?? throw Refusal()), ReceiptPurpose,
            8192, BillingJson.Default.BillingOperationReceipt);
        if (receipt.CustomerId != state.Customer || receipt.OperationId != command.OperationId || receipt.CommandHash != BillingAccountBinding.CommandHash(original) ||
            receipt.DeviceFingerprint != key.Public.Fingerprint || receipt.Distribution != distribution || receipt.Environment != environment ||
            receipt.IdentityHash != state.IdentityHash || receipt.NativePaymentId != command.NativePaymentId || receipt.MayFinish != reply.MayFinish || receipt.State != reply.State || receipt.ServerTime != snapshot.ServerTime ||
            receipt.CompletedProduct != (command.NativePaymentId is not null && reply.MayFinish ? command.ProductHint : null) ||
            reply.MayFinish && reply.State is null or BillingPurchaseState.Pending)
            throw Refusal();
        // Original replies can be stale after a refund. Only completion is retained here, never paid-cache state or its clock anchor.
        state = state with { PendingCommand = null, LastOperation = JsonSerializer.Serialize(reply, BillingJson.Default.BillingCommandResult),
            LastPurchaseState = reply.State, DeviceRetired = state.DeviceRetired || snapshot.Retired };
        await SaveAsync(state, user, access, ct); return reply;
    }

    private async Task<BillingCommandResult> SendCommandAsync(BillingClientState state, string? user, string signed,
        IBillingNativeAccess access, CancellationToken ct)
    {
        try { return await transport.CommandAsync(user, state.Customer, new(signed), ct); }
        catch (PersonalSyncHttpException error) when (error.Status == System.Net.HttpStatusCode.Gone)
        {
            // The fixed authenticated peer explicitly retired this device. Preserve every original request/key,
            // but never turn a later network outage into authority to reuse its old positive paid snapshot.
            await SaveAsync(state with { DeviceRetired = true }, user, access, ct);
            throw;
        }
    }

    private async Task<BillingTimeProof> TimeAsync(BillingClientState state, string? user, IBillingNativeAccess access, CancellationToken ct)
    {
        var nonce = Guid.NewGuid(); await RequireScopeAsync(user, access, ct);
        var reply = await transport.TimeAsync(nonce, ct); await RequireScopeAsync(user, access, ct);
        if (reply.Authority != state.Authority) throw Refusal();
        var proof = StandardJoseSignatures.VerifyJson(reply.SignedTime, Public(reply.Authority), TimePurpose, 4096, BillingJson.Default.BillingTimeProof);
        if (proof.RequestId != nonce || proof.ServerTime < state.MinimumServerTime || proof.Deadline <= proof.ServerTime ||
            proof.Deadline > proof.ServerTime.AddMinutes(2)) throw Refusal();
        return proof;
    }

    private async Task<BillingClientState> RegisteredAsync(BillingClientState state, string? user, IBillingNativeAccess access, CancellationToken ct)
    {
        if (state.Authority is not null) return state;
        await RequireScopeAsync(user, access, ct);
        var result = await transport.RegisterAsync(user, new(state.PublicIdentity, state.Registration), ct);
        await RequireScopeAsync(user, access, ct);
        var authority = Public(result.Authority);
        var snapshot = StandardJoseSignatures.VerifyJson(result.SignedSnapshot, authority, SnapshotPurpose, 131072, BillingJson.Default.BillingSnapshot);
        using var key = Key(state);
        if (result.CustomerId != state.Customer || result.AccountToken != snapshot.AccountToken || snapshot.AccountToken == Guid.Empty ||
            snapshot.Kind != BillingSnapshotKind.Registration || snapshot.Version != 1 || snapshot.RequestId != state.Customer ||
            snapshot.CustomerId != state.Customer || snapshot.DeviceFingerprint != key.Public.Fingerprint || snapshot.Distribution != distribution ||
            snapshot.Environment != environment || snapshot.IdentityHash != state.IdentityHash || snapshot.Grants.Count != 0)
            throw Refusal();
        state = state with { Authority = result.Authority };
        await SaveAsync(state, user, access, ct); return state;
    }

    private async Task<(BillingClientState? State, string? User)> OpenAsync(IBillingNativeAccess access, bool create, CancellationToken ct)
    {
        access.Validate();
        if (distribution is not (BillingIssuer.GooglePlay or BillingIssuer.AppStore) || !Enum.IsDefined(environment)) throw Refusal();
        var user = await transport.CurrentUserAsync(ct); access.Validate();
        var hash = user is null ? null : BillingAccountBinding.Identity(user);
        var route = await ReadRouteAsync(hash, ct);
        if (route.Candidate is not null) throw new BillingPrivateStateUnavailableException();
        var encoded = await storage.ReadAsync(StateSlot(hash, route.Current), ct); access.Validate();
        BillingClientState? state;
        if (encoded is null)
        {
            if (route.Current != Guid.Empty) throw new BillingPrivateStateUnavailableException();
            if (!create) return (null, user);
            state = NewState(hash, route.Current, restoreOnly: route.Current != Guid.Empty);
            await SaveAsync(state, user, access, ct);
        }
        else
        {
            state = ReadPrivateState(encoded, hash, route.Current);
        }
        await RequireScopeAsync(user, access, ct); return (state, user);
    }

    private BillingClientView View(BillingClientState? state, BillingMonotonicInstant now, bool outage)
    {
        if (state?.Cache is null || state.Anchor is null || state.DeviceRetired)
            return new(null, new(ProductPlan.Free, true, false), state?.PendingCommand is not null, state?.LastPurchaseState, state?.NativePayment is not null, state?.NativePayment, state?.NativeProduct, state?.RequiresStoreRestore == true, state?.NativeReplacementId);
        var snapshot = ReadSnapshot(state.Cache, state); var key = Public(state.PublicIdentity);
        BillingSnapshotPolicy.RequireCurrent(snapshot, new(state.Customer, state.CacheRequest, key.Fingerprint, distribution,
            environment, state.IdentityHash, state.Revision, state.MinimumServerTime));
        if (snapshot.ServerTime != state.Anchor.ServerTime) throw Refusal();
        var decision = BillingCachePolicy.Evaluate(snapshot.Grants, distribution, environment, state.Anchor, now, outage);
        return new(snapshot, decision, state.PendingCommand is not null, state.LastPurchaseState, state.NativePayment is not null, state.NativePayment, state.NativeProduct, state.RequiresStoreRestore, state.NativeReplacementId);
    }

    private BillingSnapshot ReadSnapshot(string signed, BillingClientState state)
    {
        var snapshot = StandardJoseSignatures.VerifyJson(signed, Public(state.Authority ?? throw Refusal()), SnapshotPurpose, 131072, BillingJson.Default.BillingSnapshot);
        if (snapshot.CustomerId != state.Customer || snapshot.Distribution != distribution || snapshot.Environment != environment ||
            snapshot.IdentityHash != state.IdentityHash || snapshot.DeviceFingerprint != Public(state.PublicIdentity).Fingerprint) throw Refusal();
        return snapshot;
    }
    private async Task SaveAsync(BillingClientState state, string? user, IBillingNativeAccess access, CancellationToken ct)
    {
        await RequireScopeAsync(user, access, ct);
        var route = await ReadRouteAsync(state.IdentityHash, ct);
        if (route.Candidate is not null || route.Current != state.PrivateGeneration) throw Refusal();
        await WriteStateAsync(state, ct); await RequireScopeAsync(user, access, ct);
    }
    private async Task WriteStateAsync(BillingClientState state, CancellationToken ct)
    {
        var encoded = JsonSerializer.Serialize(state, BillingClientJson.Default.BillingClientState);
        if (Encoding.UTF8.GetByteCount(encoded) > 524288) throw Refusal();
        await storage.WriteAsync(StateSlot(state.IdentityHash, state.PrivateGeneration), encoded, ct);
    }
    private BillingClientState NewState(string? hash, Guid generation, bool restoreOnly)
    {
        using var key = SigningRecipientIdentity.Create(); var bytes = key.ExportForProtectedStorage();
        try
        {
            var customer = Guid.NewGuid(); var publicJson = JsonSerializer.Serialize(key.Public, BillingClientJson.Default.PublicSigningRecipientIdentity);
            var registration = new BillingRegistration(customer, distribution, environment, publicJson, hash);
            return new(1, transport.PeerBinding, distribution, environment, hash, customer, Convert.ToBase64String(bytes), publicJson,
                Sign(registration, key, RegistrationPurpose, BillingJson.Default.BillingRegistration), PrivateGeneration: generation, RequiresStoreRestore: restoreOnly);
        }
        finally { CryptographicOperations.ZeroMemory(bytes); }
    }
    private BillingClientState ReadPrivateState(string encoded, string? hash, Guid generation)
    {
        try
        {
            var state = StrictJsonPayload.Deserialize(Encoding.UTF8.GetBytes(encoded), BillingClientJson.Default.BillingClientState, 524288);
            if (state.Version != 1 || state.Peer != transport.PeerBinding || state.Distribution != distribution || state.Environment != environment ||
                state.IdentityHash != hash || state.Customer == Guid.Empty || state.PrivateGeneration != generation ||
                state.PrivateIdentity is not { Length: > 0 and <= 16384 } || state.PublicIdentity is not { Length: > 0 and <= 16384 } ||
                state.Registration is not { Length: > 0 and <= 32768 } || state.PendingCommand?.Length > 65536 || state.Cache?.Length > 196608 ||
                state.LastOperation?.Length > 262144 || state.Revision < 0 || state.NativePriorReceipts?.Length > 32 ||
                state.NativeReplacementId is { } replacement && (state.NativePayment is null || replacement.Length != 64 || !replacement.All(char.IsAsciiHexDigit)))
                throw Refusal();
            using var key = Key(state);
            var registration = StandardJoseSignatures.VerifyJson(state.Registration, key.Public, RegistrationPurpose, 32768, BillingJson.Default.BillingRegistration);
            if (registration.Id != state.Customer || registration.PublicIdentity != state.PublicIdentity || registration.IdentityHash != hash ||
                registration.Distribution != distribution || registration.Environment != environment || Public(state.PublicIdentity).Fingerprint != key.Public.Fingerprint)
                throw Refusal();
            return state;
        }
        catch (Exception error) when (error is InvalidDataException or JsonException or FormatException or CryptographicException or ArgumentException)
        { throw new BillingPrivateStateUnavailableException(); }
    }
    private async Task<BillingPrivateRoute> ReadRouteAsync(string? hash, CancellationToken ct)
    {
        var encoded = await storage.ReadAsync(RouteSlot(hash), ct);
        if (encoded is null) return new(Guid.Empty);
        try
        {
            var route = StrictJsonPayload.Deserialize(Encoding.UTF8.GetBytes(encoded), BillingClientJson.Default.BillingPrivateRoute, 1024);
            if (route.Candidate == Guid.Empty || route.Candidate == route.Current) throw Refusal();
            return route;
        }
        catch (Exception error) when (error is InvalidDataException or JsonException or ArgumentException)
        { throw new BillingPrivateStateUnavailableException(); }
    }
    private async Task WriteRouteAsync(string? hash, BillingPrivateRoute route, string? user, IBillingNativeAccess access, CancellationToken ct)
    {
        await RequireScopeAsync(user, access, ct);
        await storage.WriteAsync(RouteSlot(hash), JsonSerializer.Serialize(route, BillingClientJson.Default.BillingPrivateRoute), ct);
        await RequireScopeAsync(user, access, ct);
    }
    private string RouteSlot(string? hash) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes("zanance.billing.route.v1\0" + Slot(hash))));
    private string StateSlot(string? hash, Guid generation) => generation == Guid.Empty ? Slot(hash) :
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes("zanance.billing.generation.v1\0" + Slot(hash) + "\0" + generation.ToString("N"))));
    private async Task RequireScopeAsync(string? user, IBillingNativeAccess access, CancellationToken ct)
    { access.Validate(); if (await transport.CurrentUserAsync(ct) != user) throw Refusal(); access.Validate(); }
    private string Slot(string? user) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(
        "zanance.billing.private.v1\0" + transport.PeerBinding + "\0" + distribution + "\0" + environment + "\0" + (user ?? "anonymous"))));
    private static SigningRecipientIdentity Key(BillingClientState state)
    {
        var bytes = Convert.FromBase64String(state.PrivateIdentity);
        try { return SigningRecipientIdentity.ImportFromProtectedStorage(bytes); }
        finally { CryptographicOperations.ZeroMemory(bytes); }
    }
    private static PublicSigningRecipientIdentity Public(string encoded)
    {
        var result = StrictJsonPayload.Deserialize(Encoding.UTF8.GetBytes(encoded), BillingClientJson.Default.PublicSigningRecipientIdentity, 16384);
        using var signing = result.OpenSigningKey(); using var wrapping = result.OpenWrappingKey(); return result;
    }
    private static string Sign<T>(T body, SigningRecipientIdentity key, string purpose, System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> json)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(body, json);
        try { return StandardJoseSignatures.Sign(bytes, key, purpose); } finally { CryptographicOperations.ZeroMemory(bytes); }
    }
    private static DateTimeOffset Max(DateTimeOffset left, DateTimeOffset right) => left > right ? left : right;
    private static InvalidDataException Refusal() => new("Original billing authority or scope was refused.");
}
