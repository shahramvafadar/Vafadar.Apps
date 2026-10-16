#if BILLING_RECOVERY_REVIEW
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Cryptography;
using Vafadar.Localization;
using Vafadar.Maui.Localization;
using Vafadar.Zanance.App.Features.Commerce;
using Vafadar.Zanance.App.Features.Commerce.Billing;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Data.Billing;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App;

/// <summary>Three original processes qualify actual protected routing/candidate retention; the empty proof peer never grants or charges.</summary>
internal static class AndroidBillingRecoveryReview
{
    /// <summary>Runs only the next retained stage in the exact independently compiled installation.</summary>
    internal static async Task RunAsync(App app, IServiceProvider services, string run, bool resume)
    {
        var stage = "identity";
        using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(3)); var ct = deadline.Token;
        string? root = null;
        try
        {
            Require(AppInfo.Current.PackageName == "pro.vafadar.zanance.billingrecoveryreview", "Independent native billing identity required.");
            Require(run.Length == 32 && run.All(char.IsAsciiHexDigit), "Public fixture id required.");
            root = Path.Combine(FileSystem.AppDataDirectory, "billing-recovery", run); Directory.CreateDirectory(root);
            var marker = Path.Combine(root, "fixture.marker");
            if (File.Exists(marker)) Require(await File.ReadAllTextAsync(marker, ct) == "Fictitious native billing recovery v1", "Original fixture required.");
            else { Require(!resume, "Original continuation marker required."); await File.WriteAllTextAsync(marker, "Fictitious native billing recovery v1", ct); }
            var native = new SecureBillingStorage(); var proofSlot = Digest("native-proof-authority:" + run);
            var encoded = await native.ReadAsync(proofSlot, ct);
            ProofState state;
            if (encoded is null)
            {
                Require(!resume, "Original protected proof authority required.");
                using var signer = SigningRecipientIdentity.Create(); var bytes = signer.ExportForProtectedStorage();
                try { state = new(Convert.ToBase64String(bytes), Guid.NewGuid(), [], 0); }
                finally { CryptographicOperations.ZeroMemory(bytes); }
                await native.WriteAsync(proofSlot, JsonSerializer.Serialize(state, BillingNativeJson.Default.ProofState), ct);
            }
            else state = StrictJsonPayload.Deserialize(Encoding.UTF8.GetBytes(encoded), BillingNativeJson.Default.ProofState, 262144);
            Require(state.Stage < 3 && resume == (state.Stage > 0), "Completed stages must not replay; use explicit original continuation.");
            using var peer = new EmptyProofPeer(native, proofSlot, state, run);
            var storage = new FaultStorage(native) { Unavailable = state.OriginalSlot };
            var access = new MauiBillingAccess(() => Require(AppInfo.Current.PackageName == AndroidSyncReview.Package, "Original native caller required."));
            var client = new BillingClient(peer, storage, BillingIssuer.GooglePlay, BillingEnvironment.Sandbox);
            if (state.Stage == 0)
            {
                stage = "original-empty-principal";
                var original = await client.ObserveAsync(access, ct);
                Require(original.Snapshot is { Grants.Count: 0 } && storage.FirstStateSlot is not null, "Original empty native principal required.");
                var originalValue = await native.ReadAsync(storage.FirstStateSlot!, ct) ?? throw new IOException();
                storage.Unavailable = storage.FirstStateSlot;
                await ExpectLossAsync(() => client.ReadAsync(access, true, ct));
                stage = "cut-after-native-candidate-write"; storage.FailAfterCandidateWrite = true;
                try { await client.ReenrollForStoreRestoreAsync(access, ct); throw new IOException("Expected native persistence cut missing."); }
                catch (IOException) when (storage.FaultFired) { }
                Require(storage.CandidateCustomer is not null && storage.CandidateFingerprint is not null, "Saved native candidate required.");
                await peer.SaveAsync(peer.State with { Stage = 1, OriginalSlot = storage.FirstStateSlot,
                    OriginalDigest = Digest(originalValue), OriginalCustomer = original.Snapshot!.CustomerId,
                    CandidateCustomer = storage.CandidateCustomer, CandidateFingerprint = storage.CandidateFingerprint }, ct);
                await PublishAsync("candidate-saved-before-publication");
                // Render the actual new recovery form with explicit no-store effects; the host invokes genuine Cancel only.
                await MainThread.InvokeOnMainThreadAsync(() => app.ShowSyncReview(new ProductPlansPage(
                    new ProductPlansViewModel(services.GetRequiredService<CommercialAccessStore>(), services.GetRequiredService<Translator>(),
                        services.GetRequiredService<IAppInteraction>(), new RecoveryScreen()), services.GetRequiredService<ILocalizationService>())
                    .WithFlowDirection(services.GetRequiredService<ILocalizationService>())));
            }
            else if (state.Stage == 1)
            {
                stage = "cold-original-candidate";
                await ExpectLossAsync(() => client.ReadAsync(access, true, ct));
                await client.ReenrollForStoreRestoreAsync(access, ct);
                var recovered = await client.ObserveAsync(access, ct);
                Require(recovered.RequiresStoreRestore && recovered.Snapshot is { } recoveredSnapshot && recoveredSnapshot.CustomerId == state.CandidateCustomer &&
                    recoveredSnapshot.DeviceFingerprint == state.CandidateFingerprint && recoveredSnapshot.Grants.Count == 0,
                    "Cold continuation must use the same saved native identity, without a grant.");
                await client.CompleteStoreRestoreAsync(access, true, ct);
                Require((await client.ReadAsync(access, false, ct)).RequiresStoreRestore, "Pending native work cannot release restore-only state.");
                try { await client.BeginNativeAsync(access, BillingProduct.PlusMonthly, ct); throw new IOException("Restore-only payment was allowed."); }
                catch (InvalidOperationException) { }
                // Empty fixture peer/native-owned list: no real store operation or paid capability is asserted.
                await client.CompleteStoreRestoreAsync(access, false, ct);
                Require(!(await client.ReadAsync(access, false, ct)).RequiresStoreRestore, "Explicit empty restoration did not complete.");
                await PreserveOriginalAsync(); await peer.SaveAsync(peer.State with { Stage = 2 }, ct);
                await PublishAsync("same-candidate-published");
            }
            else
            {
                stage = "cold-published-routing";
                var cold = await client.ReadAsync(access, false, ct);
                Require(!cold.RequiresStoreRestore && cold.Snapshot is { } coldSnapshot && coldSnapshot.CustomerId == state.CandidateCustomer &&
                    coldSnapshot.DeviceFingerprint == state.CandidateFingerprint && coldSnapshot.Grants.Count == 0,
                    "Original native protected routing did not survive process replacement.");
                await PreserveOriginalAsync(); await peer.SaveAsync(peer.State with { Stage = 3 }, ct); await PublishAsync("complete");
            }
            async Task PreserveOriginalAsync()
            {
                var original = await native.ReadAsync(peer.State.OriginalSlot!, ct) ?? throw new IOException("Original retained private slot missing.");
                Require(Digest(original) == peer.State.OriginalDigest, "Original private bytes changed.");
            }
            async Task PublishAsync(string result)
            {
                var report = new NativeRecoveryReport(run, result, Environment.ProcessId, true, true, false, false, null);
                await File.WriteAllTextAsync(Path.Combine(root, "public-proof.json"), JsonSerializer.Serialize(report, BillingNativeJson.Default.NativeRecoveryReport), ct);
                Android.Util.Log.Info("ZananceBillingRecovery", JsonSerializer.Serialize(report, BillingNativeJson.Default.NativeRecoveryReport));
            }
        }
        catch (Exception error)
        {
            // Report type/stage only, never platform exception text, a protected record, key or signed receipt.
            var report = new NativeRecoveryReport(run, stage, Environment.ProcessId, false, false, false, false, error.GetType().Name);
            if (root is not null) await File.WriteAllTextAsync(Path.Combine(root, "public-failure.json"), JsonSerializer.Serialize(report, BillingNativeJson.Default.NativeRecoveryReport));
            Android.Util.Log.Error("ZananceBillingRecovery", JsonSerializer.Serialize(report, BillingNativeJson.Default.NativeRecoveryReport));
        }
    }
    private static async Task ExpectLossAsync(Func<Task<BillingClientView>> read)
    { try { await read(); throw new IOException("Original private loss was bypassed."); } catch (BillingPrivateStateUnavailableException) { } }
    private static void Require(bool valid, string message) { if (!valid) throw new IOException(message); }
    private static string Digest(string value) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private sealed class FaultStorage(SecureBillingStorage actual) : IBillingPrivateStorage
    {
        internal string? FirstStateSlot;
        internal string? Unavailable;
        internal bool FailAfterCandidateWrite;
        internal bool FaultFired;
        internal Guid? CandidateCustomer;
        internal string? CandidateFingerprint;
        public Task<string?> ReadAsync(string scope, CancellationToken ct) => scope == Unavailable
            ? Task.FromException<string?>(new BillingPrivateStateUnavailableException()) : actual.ReadAsync(scope, ct);
        public async Task WriteAsync(string scope, string value, CancellationToken ct)
        {
            Require(scope != Unavailable, "Original unavailable private state must never be overwritten.");
            using var json = JsonDocument.Parse(value); var state = json.RootElement.TryGetProperty("PrivateIdentity", out _);
            if (state) FirstStateSlot ??= scope;
            await actual.WriteAsync(scope, value, ct);
            if (!state || json.RootElement.GetProperty("PrivateGeneration").GetGuid() == Guid.Empty) return;
            CandidateCustomer = json.RootElement.GetProperty("Customer").GetGuid();
            CandidateFingerprint = Public(json.RootElement.GetProperty("PublicIdentity").GetString()!).Fingerprint;
            if (FailAfterCandidateWrite) { FailAfterCandidateWrite = false; FaultFired = true; throw new IOException("Fictitious cut after confirmed native write."); }
        }
    }

    /// <summary>An explicitly fictional empty authority port; no purchase verification or entitlement algorithm is implemented here.</summary>
    private sealed class EmptyProofPeer : IBillingServiceTransport, IDisposable
    {
        private readonly SecureBillingStorage storage;
        private readonly string scope;
        private readonly SigningRecipientIdentity signer;
        internal ProofState State { get; private set; }
        public string PeerBinding { get; }
        internal EmptyProofPeer(SecureBillingStorage storage, string scope, ProofState state, string run)
        {
            this.storage = storage; this.scope = scope; State = state; PeerBinding = "fictional-empty-native-billing:" + run;
            var bytes = Convert.FromBase64String(state.Secret);
            try { signer = SigningRecipientIdentity.ImportFromProtectedStorage(bytes); } finally { CryptographicOperations.ZeroMemory(bytes); }
        }
        internal async Task SaveAsync(ProofState state, CancellationToken ct)
        { await storage.WriteAsync(scope, JsonSerializer.Serialize(state, BillingNativeJson.Default.ProofState), ct); State = state; }
        public Task<string?> CurrentUserAsync(CancellationToken ct) => Task.FromResult<string?>(null);
        public Task<BillingTimeResult> TimeAsync(Guid nonce, CancellationToken ct)
        {
            var now = TimeProvider.System.GetUtcNow(); return Task.FromResult(new BillingTimeResult(Authority(),
                Signed(new BillingTimeProof(nonce, now, now.AddMinutes(2)), "zanance.billing.time.v1", BillingJson.Default.BillingTimeProof)));
        }
        public async Task<BillingRegistrationResult> RegisterAsync(string? user, BillingRegistrationEnvelope input, CancellationToken ct)
        {
            var device = Public(input.PublicIdentity);
            var registration = StandardJoseSignatures.VerifyJson(input.Signed, device, "zanance.billing.registration.v1", 32768, BillingJson.Default.BillingRegistration);
            Require(registration.IdentityHash is null && registration.PublicIdentity == input.PublicIdentity, "Fictitious empty registration mismatch.");
            if (State.Devices.TryGetValue(registration.Id, out var existing)) Require(existing == input.PublicIdentity, "Original public registration changed.");
            else { State.Devices.Add(registration.Id, input.PublicIdentity); await SaveAsync(State, ct); }
            var snapshot = Snapshot(registration.Id, registration.Id, device.Fingerprint, BillingSnapshotKind.Registration);
            return new(registration.Id, State.Account, Authority(), Signed(snapshot, "zanance.billing.snapshot.v1", BillingJson.Default.BillingSnapshot), []);
        }
        public Task<BillingCommandResult> CommandAsync(string? user, Guid customer, BillingSignedRequest input, CancellationToken ct)
        {
            var device = Public(State.Devices[customer]);
            var command = StandardJoseSignatures.VerifyJson(input.Signed, device, "zanance.billing.command.v1", 32768, BillingJson.Default.BillingCommand);
            Require(command.CustomerId == customer && command.Operation == BillingOperation.Observe && command.IdentityHash is null, "No purchase, payment or verification belongs to the empty native port.");
            var snapshot = Snapshot(customer, command.OperationId, device.Fingerprint, BillingSnapshotKind.Current);
            return Task.FromResult(new BillingCommandResult(command.OperationId, Signed(snapshot, "zanance.billing.snapshot.v1", BillingJson.Default.BillingSnapshot), false, null));
        }
        private BillingSnapshot Snapshot(Guid customer, Guid nonce, string fingerprint, BillingSnapshotKind kind)
            => new(1, kind, customer, nonce, fingerprint, BillingIssuer.GooglePlay, BillingEnvironment.Sandbox, null, State.Account, 0, TimeProvider.System.GetUtcNow(), [], false);
        private string Authority() => JsonSerializer.Serialize(signer.Public, BillingNativeJson.Default.PublicSigningRecipientIdentity);
        private string Signed<T>(T value, string purpose, System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> json)
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(value, json);
            try { return StandardJoseSignatures.Sign(bytes, signer, purpose); } finally { CryptographicOperations.ZeroMemory(bytes); }
        }
        public void Dispose() => signer.Dispose();
    }
    private static PublicSigningRecipientIdentity Public(string value)
        => StrictJsonPayload.Deserialize(Encoding.UTF8.GetBytes(value), BillingNativeJson.Default.PublicSigningRecipientIdentity, 16384);

    private sealed class RecoveryScreen : IProductBillingEnvironment
    {
        public bool Available => true;
        public Task<ProductBillingPresentation> LoadAsync(CancellationToken ct)
            => Task.FromResult(new ProductBillingPresentation(ProductBillingStatus.RecoveryRequired, null, null, [], true, false));
        public Task<ProductBillingPresentation> PurchaseAsync(NativeBillingOffer offer, CancellationToken ct) => throw new IOException("Native payment forbidden in recovery review.");
        public Task<ProductBillingPresentation> RestoreAsync(CancellationToken ct) => throw new IOException("Only native confirmation Cancel belongs to this review.");
        public Task<ProductBillingPresentation> ContinueAsync(CancellationToken ct) => throw new IOException("No purchase continuation belongs to this review.");
        public Task ManageAsync(CancellationToken ct) => throw new IOException("External store navigation forbidden in recovery review.");
    }
}

/// <summary>Independent native fixture secrets stay inside actual SecureStorage and never enter its public reports.</summary>
internal sealed record ProofState(string Secret, Guid Account, Dictionary<Guid, string> Devices, int Stage,
    string? OriginalSlot = null, string? OriginalDigest = null, Guid? OriginalCustomer = null, Guid? CandidateCustomer = null, string? CandidateFingerprint = null)
{
    /// <inheritdoc />
    public override string ToString() => "Protected fictional native proof state";
}
/// <summary>Content-free native stage evidence; never a store, service, hardware-key or purchase acceptance claim.</summary>
internal sealed record NativeRecoveryReport(string Run, string Stage, int Process, bool NativeProtectedStorage, bool OriginalPrivateRetained,
    bool ActualStoreAcceptance, bool ActualPayment, string? Error);

[JsonSerializable(typeof(ProofState))]
[JsonSerializable(typeof(NativeRecoveryReport))]
[JsonSerializable(typeof(PublicSigningRecipientIdentity))]
internal partial class BillingNativeJson : JsonSerializerContext;
#endif
