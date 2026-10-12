namespace Vafadar.Data.Tests;

public sealed class DatabaseGenerationCheckpointTests
{
    [Fact, Trait("AT", "AT-187")]
    public void Only_durable_selector_commit_changes_the_authoritative_generation()
    {
        var profile = Guid.NewGuid();
        var original = Guid.NewGuid();
        var candidate = Guid.NewGuid();
        var start = new DatabaseGenerationCheckpoint(profile, original, candidate);
        var keyed = start.Advance(profile, candidate, DatabaseGenerationPhase.KeyPrepared);
        var verified = keyed.Advance(profile, candidate, DatabaseGenerationPhase.CandidateVerified);
        Assert.Equal(original, start.SelectedGeneration);
        Assert.Equal(original, verified.SelectedGeneration);
        var selected = verified.Advance(profile, candidate, DatabaseGenerationPhase.Selected);
        Assert.Equal(candidate, selected.SelectedGeneration);
        Assert.Equal(candidate, selected.Advance(profile, candidate, DatabaseGenerationPhase.Completed).SelectedGeneration);
        Assert.Equal(DatabaseGenerationPhase.Original, start.Phase);
    }

    [Theory]
    [InlineData(DatabaseGenerationPhase.CandidateVerified)]
    [InlineData(DatabaseGenerationPhase.Selected)]
    [InlineData(DatabaseGenerationPhase.Completed)]
    [InlineData(DatabaseGenerationPhase.Original)]
    public void Skipping_or_repeating_checkpoints_is_refused(DatabaseGenerationPhase next)
    {
        var state = new DatabaseGenerationCheckpoint(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        Assert.Throws<InvalidOperationException>(() => state.Advance(state.ProfileId, state.CandidateGeneration, next));
    }

    [Fact]
    public void Another_profile_or_candidate_cannot_take_over_a_transition()
    {
        var state = new DatabaseGenerationCheckpoint(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        Assert.Throws<InvalidOperationException>(() => state.Advance(Guid.NewGuid(), state.CandidateGeneration, DatabaseGenerationPhase.KeyPrepared));
        Assert.Throws<InvalidOperationException>(() => state.Advance(state.ProfileId, Guid.NewGuid(), DatabaseGenerationPhase.KeyPrepared));
    }

    [Fact]
    public void Empty_or_identical_generations_are_refused()
    {
        var id = Guid.NewGuid();
        Assert.Throws<ArgumentException>(() => new DatabaseGenerationCheckpoint(Guid.Empty, id, Guid.NewGuid()));
        Assert.Throws<ArgumentException>(() => new DatabaseGenerationCheckpoint(id, id, id));
    }
    [Fact]
    public void Completed_transition_cannot_advance_again()
    {
        var state = new DatabaseGenerationCheckpoint(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        foreach (var phase in new[] { DatabaseGenerationPhase.KeyPrepared, DatabaseGenerationPhase.CandidateVerified,
                     DatabaseGenerationPhase.Selected, DatabaseGenerationPhase.Completed })
            state = state.Advance(state.ProfileId, state.CandidateGeneration, phase);
        Assert.Throws<InvalidOperationException>(() => state.Advance(state.ProfileId, state.CandidateGeneration,
            (DatabaseGenerationPhase)5));
        Assert.Throws<InvalidOperationException>(() => state.Advance(state.ProfileId, state.CandidateGeneration,
            DatabaseGenerationPhase.Original));
    }
}
