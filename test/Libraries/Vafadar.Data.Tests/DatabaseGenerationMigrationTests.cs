namespace Vafadar.Data.Tests;

public sealed class DatabaseGenerationMigrationTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static DatabaseGenerationCheckpoint Start() => new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

    [Fact, Trait("AT", "AT-188")]
    public async Task Cleanup_occurs_only_after_readback_and_keyed_selected_reopen()
    {
        var session = new Session(Start());
        Assert.Equal(session.State.CandidateGeneration, await DatabaseGenerationMigration.ResumeAsync(session.State, session, Ct));
        Assert.Equal(DatabaseGenerationPhase.Completed, session.State.Phase);
        Assert.Equal(new[] { "key", "persist:KeyPrepared", "verify", "persist:CandidateVerified", "reopen:CandidateVerified",
            "persist:Selected", "reopen:Selected", "cleanup", "persist:Completed", "reopen:Completed" }, session.Effects);
    }

    [Theory, Trait("AT", "AT-188")]
    [InlineData("key")]
    [InlineData("verify")]
    [InlineData("persist:KeyPrepared")]
    [InlineData("persist:CandidateVerified")]
    [InlineData("persist:Selected")]
    [InlineData("reopen:Selected")]
    [InlineData("cleanup")]
    [InlineData("persist:Completed")]
    public async Task Interrupted_effect_can_resume_from_actual_last_persisted_state(string fault)
    {
        var initial = Start();
        var session = new Session(initial) { FaultAfter = fault };
        await Assert.ThrowsAsync<IOException>(() => DatabaseGenerationMigration.ResumeAsync(initial, session, Ct));
        Assert.Equal(initial.CandidateGeneration, await DatabaseGenerationMigration.ResumeAsync(initial, session, Ct));
        Assert.Equal(DatabaseGenerationPhase.Completed, session.State.Phase);
    }

    [Fact]
    public async Task Missing_selected_key_stops_without_cleanup_or_plaintext_fallback()
    {
        var initial = Start();
        var selected = At(initial, DatabaseGenerationPhase.Selected);
        var session = new Session(selected) { KeyMissing = true };
        await Assert.ThrowsAsync<InvalidOperationException>(() => DatabaseGenerationMigration.ResumeAsync(initial, session, Ct));
        Assert.Equal(selected, session.State);
        Assert.Equal(new[] { "reopen:Selected" }, session.Effects);
    }

    [Fact]
    public async Task Returned_persist_without_actual_readback_stops_before_selection_or_cleanup()
    {
        var session = new Session(Start()) { IgnorePersist = true };
        await Assert.ThrowsAsync<InvalidOperationException>(() => DatabaseGenerationMigration.ResumeAsync(session.State, session, Ct));
        Assert.Equal(DatabaseGenerationPhase.Original, session.State.Phase);
        Assert.DoesNotContain("cleanup", session.Effects);
    }

    [Fact]
    public async Task Retired_context_after_key_await_does_not_persist_or_create_candidate()
    {
        var session = new Session(Start()) { RetireAfter = "key" };
        await Assert.ThrowsAsync<InvalidOperationException>(() => DatabaseGenerationMigration.ResumeAsync(session.State, session, Ct));
        Assert.Equal(new[] { "key" }, session.Effects);
        Assert.Equal(DatabaseGenerationPhase.Original, session.State.Phase);
    }

    [Fact]
    public async Task Other_generation_or_regressed_checkpoint_is_refused_before_effects()
    {
        var captured = Start();
        var foreign = new Session(new(captured.ProfileId, captured.OriginalGeneration, Guid.NewGuid()));
        await Assert.ThrowsAsync<InvalidOperationException>(() => DatabaseGenerationMigration.ResumeAsync(captured, foreign, Ct));
        var regressed = new Session(captured);
        await Assert.ThrowsAsync<InvalidOperationException>(() => DatabaseGenerationMigration.ResumeAsync(
            At(captured, DatabaseGenerationPhase.Selected), regressed, Ct));
        Assert.Empty(foreign.Effects);
        Assert.Empty(regressed.Effects);
    }

    [Fact]
    public async Task Completed_restart_only_reopens_candidate_and_does_not_repeat_cleanup()
    {
        var session = new Session(At(Start(), DatabaseGenerationPhase.Completed));
        await DatabaseGenerationMigration.ResumeAsync(session.State, session, Ct);
        Assert.Equal(new[] { "reopen:Completed" }, session.Effects);
    }

    [Fact]
    public async Task Canceled_operation_cannot_begin_key_or_generation_effects()
    {
        var session = new Session(Start());
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => DatabaseGenerationMigration.ResumeAsync(session.State, session, canceled.Token));
        Assert.Empty(session.Effects);
    }

    private static DatabaseGenerationCheckpoint At(DatabaseGenerationCheckpoint state, DatabaseGenerationPhase target)
    {
        while (state.Phase < target) state = state.Advance(state.ProfileId, state.CandidateGeneration, state.Phase + 1);
        return state;
    }

    // Faultable native-effect port, not a filesystem/key/power-loss simulation. Actual adapter proof remains separate.
    private sealed class Session(DatabaseGenerationCheckpoint initial) : IDatabaseGenerationMigrationSession
    {
        public DatabaseGenerationCheckpoint State { get; private set; } = initial;
        public List<string> Effects { get; } = [];
        public string? FaultAfter { get; set; }
        public string? RetireAfter { get; init; }
        public bool KeyMissing { get; init; }
        public bool IgnorePersist { get; init; }
        private bool _retired;
        public void ValidateContext() { if (_retired) throw new InvalidOperationException("Context retired."); }
        public Task<DatabaseGenerationCheckpoint> ReadCheckpointAsync(CancellationToken ct)
        { ct.ThrowIfCancellationRequested(); return Task.FromResult(State); }
        private async Task Effect(string name)
        {
            Effects.Add(name);
            // Force a genuine await boundary so retirement checks cannot pass only by synchronous execution.
            await Task.Yield();
            if (RetireAfter == name) _retired = true;
            if (FaultAfter == name) { FaultAfter = null; throw new IOException("Interrupted native effect."); }
        }
        public Task PrepareKeyAsync(DatabaseGenerationCheckpoint state, CancellationToken ct) => Effect("key");
        public Task CreateAndVerifyCandidateAsync(DatabaseGenerationCheckpoint state, CancellationToken ct) => Effect("verify");
        public async Task ReopenCandidateAsync(DatabaseGenerationCheckpoint state, CancellationToken ct)
        { await Effect("reopen:" + state.Phase); if (KeyMissing) throw new InvalidOperationException("Key missing."); }
        public async Task PersistCheckpointAsync(DatabaseGenerationCheckpoint expected, DatabaseGenerationCheckpoint next, CancellationToken ct)
        {
            Assert.Equal(expected, State);
            if (!IgnorePersist) State = next;
            await Effect("persist:" + next.Phase);
        }
        public Task CleanupOriginalAsync(DatabaseGenerationCheckpoint state, CancellationToken ct)
        { Assert.True(State.CandidateSelected); Assert.Equal(DatabaseGenerationPhase.Selected, state.Phase); return Effect("cleanup"); }
    }
}
