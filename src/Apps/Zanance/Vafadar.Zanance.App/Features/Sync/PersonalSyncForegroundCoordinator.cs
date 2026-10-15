using System.Net;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.App.Features.Sync;

/// <summary>Runs one original current-profile round with coalescing, lifetime checks and bounded retry delays.</summary>
public sealed class PersonalSyncForegroundCoordinator(PersonalSyncStore store, PersonalSyncCoordinator coordinator,
    IPersonalSyncEnvironment environment, TimeProvider time)
{
    private sealed record Retry(DateTimeOffset Due, int Failures);
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly object state = new();
    private readonly Dictionary<string, Retry> retries = new(StringComparer.Ordinal);
    private long revision;

    /// <summary>Raised after actual local commit/failure feedback; the native owner dispatches visible refreshes.</summary>
    public event EventHandler? Changed;

    /// <summary>Allows a fresh attempt only after an explicit connection/session/review action.</summary>
    public void Reset()
    {
        lock (state) { revision++; retries.Clear(); }
    }

    /// <summary>Checks a public interest hint first; that hint can never authorize network or financial access.</summary>
    public async Task RunAsync(bool interested, CancellationToken ct = default, PersonalSyncAccess? capturedAccess = null)
    {
        if (!interested || !environment.Available || !await gate.WaitAsync(0, ct)) return;
        PersonalSyncReview? connection = null; PersonalSyncAccess? access = null; CancellationTokenSource? linked = null;
        var attemptRevision = 0L; var failures = 0; var scope = "original";
        try
        {
            access = capturedAccess ?? environment.Capture(); access.Validate();
            linked = CancellationTokenSource.CreateLinkedTokenSource(ct, access.Cancellation);
            var token = linked.Token;
            scope = access.Scope ?? "original";
            lock (state)
            {
                attemptRevision = revision;
                if (retries.TryGetValue(scope, out var retry))
                { if (retry.Due > time.GetUtcNow()) return; failures = retry.Failures; }
            }
            connection = await store.ReviewAsync(token); access.Validate();
            var status = await store.StatusAsync(token); access.Validate();
            if (!status.Connected || connection.IdentityId is null || connection.PeerKey is null || connection.RemoteProfile is null)
            { SetDue(scope, attemptRevision, TimeSpan.FromSeconds(30), 0); return; }
            if (status.Failure is "sign-in-required" or "connection-review-required")
            { SetDue(scope, attemptRevision, TimeSpan.MaxValue, 0); return; }
            var configuration = environment.ReadConnection();
            var peer = new LocalSyncPeer(configuration.Address, configuration.Fingerprint);
            // A remembered endpoint for a different profile must not receive this profile's automatic request.
            if (peer.BindingKey(connection.IdentityId) != connection.PeerKey)
            {
                await store.ReportFailureAsync(connection, PersonalSyncFailure.ConnectionReviewRequired, token); access.Validate();
                SetDue(scope, attemptRevision, TimeSpan.MaxValue, 0); Changed?.Invoke(this, EventArgs.Empty); return;
            }
            if ((await store.ConflictsAsync(token)).Count != 0)
            { SetDue(scope, attemptRevision, TimeSpan.MaxValue, 0); return; }
            access.Validate(); using var transport = environment.Open(peer, access.Validate);
            var outcome = await coordinator.SynchronizeAsync(transport, token); access.Validate();
            if (outcome.Applied) await environment.PublishAppliedAsync(token);
            access.Validate(); SetDue(scope, attemptRevision, TimeSpan.FromSeconds(30), 0); Changed?.Invoke(this, EventArgs.Empty);
        }
        catch (OperationCanceledException) { }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            // Responses and exceptions may contain private information; retain only these fixed status codes.
            if (access is null || linked is null) return;
            var unauthorized = error is PersonalSyncHttpException { Status: HttpStatusCode.Unauthorized };
            var reconnect = error is PersonalSyncHttpException { Status: HttpStatusCode.Forbidden };
            failures = Math.Min(failures + 1, 5);
            SetDue(scope, attemptRevision, unauthorized || reconnect ? TimeSpan.MaxValue : TimeSpan.FromSeconds(Math.Min(300, 15 * (1 << failures))), failures);
            if (connection is null) return;
            try
            {
                access.Validate();
                await store.ReportFailureAsync(connection, unauthorized ? PersonalSyncFailure.SignInRequired : reconnect
                    ? PersonalSyncFailure.ConnectionReviewRequired : PersonalSyncFailure.ServiceUnavailable, linked.Token);
                access.Validate(); Changed?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception retired) when (retired is not OutOfMemoryException) { /* A retired binding must keep its new owner's state. */ }
        }
        finally { linked?.Dispose(); gate.Release(); }
    }

    private void SetDue(string scope, long attemptRevision, TimeSpan delay, int failures)
    {
        lock (state)
        {
            if (attemptRevision != revision) return;
            // No private history is retained here; the durable profile binding remains the only authority.
            if (retries.Count >= 100 && !retries.ContainsKey(scope)) retries.Clear();
            retries[scope] = new(delay == TimeSpan.MaxValue ? DateTimeOffset.MaxValue : time.GetUtcNow().Add(delay), failures);
        }
    }
}
