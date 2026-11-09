using System.Text.Json;
using Vafadar.Zanance.Core.Analysis;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data.Analysis;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.Data.Tests;

/// <summary>Portable original-client temporal observations; the private write refuses before durability or HTTP.</summary>
internal static class AiSubmissionSnapshotProbes
{
    /// <summary>Returns the literal entry payload and the one offered to the refusing journal after a controlled mutation.</summary>
    internal static async Task<SubmissionSnapshotObservation> ObserveAsync(int phase, CancellationToken ct)
    {
        if (phase is < 0 or > 3) throw new ArgumentOutOfRangeException(nameof(phase));
        var current = new List<PeriodTotals> { new("EUR", 500, 10, 200, 30), new("USD", 40, 0, 10, 2) };
        var previous = new List<PeriodTotals> { new("EUR", 400, 5, 180, 20), new("USD", 30, 0, 8, 1) };
        var comparison = phase == 3;
        var payload = Payload(current, comparison ? previous : null);
        var expected = Encode(payload);
        var journal = new RefusingJournal(phase != 2);
        var credentials = new NoCredentials();
        var changed = false;
        void Mutate()
        {
            var target = comparison ? previous : current;
            target[0] = new("EUR", 900, 15, 300, 50);
            changed = true;
        }
        using var transport = Transport(credentials);
        var rechecks = 0;
        var client = new AiClient(transport, journal, "fictitious-submission-owner", Scope(), () =>
        {
            if (phase == 2 && !changed) Mutate();
        });
        Task<AiRequestResult>? first = null;
        Task<AiRequestResult> submitted;
        if (phase == 1)
        {
            // The first refused operation owns the same semaphore; it creates no durable record.
            first = client.SubmitAsync(Offer(false), Payload([new("EUR", 80, 0, 0, 0), new("USD", 0, 0, 0, 0)], null), () => { rechecks++; return Task.CompletedTask; }, ct);
            await journal.Entered.Task.WaitAsync(TimeSpan.FromSeconds(15), ct);
            submitted = client.SubmitAsync(Offer(comparison), payload, () => { rechecks++; return Task.CompletedTask; }, ct);
            Mutate();
        }
        else
        {
            submitted = client.SubmitAsync(Offer(comparison), payload, () => { rechecks++; return Task.CompletedTask; }, ct);
            if (phase != 2)
            {
                await journal.Entered.Task.WaitAsync(TimeSpan.FromSeconds(15), ct);
                Mutate();
            }
        }
        journal.Release.TrySetResult();
        if (first is not null) await ExpectPrivateRefusalAsync(first);
        await ExpectPrivateRefusalAsync(submitted);
        var captured = journal.Records[^1];
        return new(expected, Encode(captured.Submission.Payload), Encode(payload), changed, journal.Reads, journal.Records.Count,
            journal.DurableWrites, credentials.Calls, rechecks, captured.Submission.RequestId != Guid.Empty);
    }

    /// <summary>A cancelled queued caller cannot create an intent or prevent its original owner from releasing the gate.</summary>
    internal static async Task<SubmissionCancellationObservation> ObserveCancellationAsync(CancellationToken ct)
    {
        var journal = new RefusingJournal(true);
        var credentials = new NoCredentials();
        using var transport = Transport(credentials);
        var rechecks = 0;
        var client = new AiClient(transport, journal, "fictitious-cancelled-owner", Scope(), () => { });
        var payload = Payload([new("EUR", 50, 0, 0, 0), new("USD", 0, 0, 0, 0)], null);
        var first = client.SubmitAsync(Offer(false), payload, () => { rechecks++; return Task.CompletedTask; }, ct);
        await journal.Entered.Task.WaitAsync(TimeSpan.FromSeconds(15), ct);
        using var cancelled = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var next = client.SubmitAsync(Offer(false), payload, () => { rechecks++; return Task.CompletedTask; }, cancelled.Token);
        cancelled.Cancel();
        var refused = false;
        try { await next; }
        catch (OperationCanceledException error) when (error.CancellationToken == cancelled.Token) { refused = true; }
        finally { journal.Release.TrySetResult(); }
        await ExpectPrivateRefusalAsync(first);
        return new(refused, journal.Reads, journal.Records.Count, journal.DurableWrites, credentials.Calls, rechecks);
    }

    private static AiPayload Payload(IReadOnlyList<PeriodTotals> current, IReadOnlyList<PeriodTotals>? previous) =>
        new(1, previous is null ? AiPurpose.PeriodExplanation : AiPurpose.PeriodComparison, "en",
            new(new(2026, 10, 1), new(2026, 10, 10), 2, true, current, 0),
            previous is null ? null : new(new(2026, 9, 21), new(2026, 9, 30), 2, true, previous, 0));

    private static AiOffer Offer(bool comparison) => new(Guid.NewGuid(), "fictitious-provider", "fictitious-model",
        comparison ? AiPurpose.PeriodComparison : AiPurpose.PeriodExplanation, 8, DateTimeOffset.UnixEpoch.AddYears(100));

    private static string Encode(AiPayload payload) => JsonSerializer.Serialize(payload, AiJson.Default.AiPayload);

    // The absolute label participates only in scope hashing: no directory, database, key or temporary file is created.
    private static string Scope() => Path.GetFullPath(Path.Combine("artifacts", "analysis-submission-snapshot", "fictitious-" + Guid.NewGuid().ToString("N") + ".sqlite"));
    private static PersonalSyncTransport Transport(NoCredentials credentials) =>
        new(new LocalSyncPeer("https://localhost:55555", new string('f', 64)), credentials);

    private static async Task ExpectPrivateRefusalAsync(Task<AiRequestResult> operation)
    {
        try { await operation; }
        catch (PrivateStop) { return; }
        throw new InvalidOperationException("The private journal must refuse before transport or source revalidation.");
    }

    private sealed class PrivateStop : IOException { }

    private sealed class RefusingJournal(bool holdFirstRead) : IAiPrivateStorage
    {
        internal readonly TaskCompletionSource Entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal readonly TaskCompletionSource Release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal readonly List<AiClientRecord> Records = [];
        internal int Reads;
        internal int DurableWrites => 0;
        public async Task<string?> ReadAsync(string scope, CancellationToken ct)
        {
            if (Interlocked.Increment(ref Reads) == 1 && holdFirstRead)
            {
                Entered.TrySetResult();
                await Release.Task.WaitAsync(ct);
            }
            return null;
        }
        public Task WriteAsync(string scope, string value, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Records.Add(JsonSerializer.Deserialize(value, AiClientJson.Default.AiClientRecord) ?? throw new InvalidDataException());
            throw new PrivateStop();
        }
    }

    private sealed class NoCredentials : IPersonalSyncTokenStore
    {
        internal int Calls;
        public Task<PersonalSyncSession?> ReadAsync(string peer, CancellationToken ct)
        { Interlocked.Increment(ref Calls); throw new InvalidOperationException("Credential access is outside this observation."); }
        public Task WriteAsync(string peer, PersonalSyncSession session, CancellationToken ct)
        { Interlocked.Increment(ref Calls); throw new InvalidOperationException("Credential mutation is outside this observation."); }
        public Task RemoveAsync(string peer, CancellationToken ct)
        { Interlocked.Increment(ref Calls); throw new InvalidOperationException("Credential mutation is outside this observation."); }
    }
}

/// <summary>Complete aggregate equality and actual original-port counters, with no credential or financial-source values.</summary>
internal sealed record SubmissionSnapshotObservation(string ExpectedPayload, string JournalPayload, string MutatedCallerPayload,
    bool Changed, int PrivateReads, int PrivateWriteAttempts, int DurableWrites, int CredentialCalls, int SourceRechecks, bool RequestIdAssigned);

/// <summary>Exact original cancellation and private-port counters; no successful provider operation occurs.</summary>
internal sealed record SubmissionCancellationObservation(bool OriginalCancellation, int PrivateReads, int PrivateWriteAttempts,
    int DurableWrites, int CredentialCalls, int SourceRechecks);
