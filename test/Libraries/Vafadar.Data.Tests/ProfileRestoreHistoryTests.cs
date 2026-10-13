using Microsoft.Data.Sqlite;
using Vafadar.Testing;

namespace Vafadar.Data.Tests;

/// <summary>Tests durable reference reservation/ancestry without interpreting metadata as native acceptance.</summary>
public sealed class ProfileRestoreHistoryTests : IDisposable
{
    private readonly TemporaryDirectory directory = new();
    private readonly DatabaseKeyBinding initial = new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private string PathOfHistory => directory.Combine("restore-history.sqlite");
    private SqliteProfileRestoreHistory History() => new(PathOfHistory, initial, () => { });
    public void Dispose() => directory.Dispose();

    [Fact, Trait("AT", "AT-207")]
    public async Task Reservation_survives_reopen_and_repeating_it_keeps_the_same_target()
    {
        await History().CreateNewAsync(Ct); var state = new DatabaseGenerationCheckpoint(initial.ProfileId, initial.GenerationId, Guid.NewGuid());
        await History().ReserveAsync(state, Ct); await History().ReserveAsync(state, Ct);
        var rows = await History().ReadAsync(Ct); Assert.Equal(2, rows.Count); Assert.False(rows[^1].Selected);
        Assert.Equal(initial.GenerationId, rows[^1].OriginalGeneration); Assert.Equal(state.CandidateGeneration, rows[^1].CandidateGeneration);
        await History().RequireAsync(state, Ct); Assert.Equal(rows, await History().ReadAsync(Ct));
    }

    [Fact, Trait("AT", "AT-207")]
    public async Task Another_restore_cannot_replace_unresolved_intent_or_reinterpret_its_parent()
    {
        await History().CreateNewAsync(Ct); var state = new DatabaseGenerationCheckpoint(initial.ProfileId, initial.GenerationId, Guid.NewGuid());
        await History().ReserveAsync(state, Ct);
        await Assert.ThrowsAsync<InvalidOperationException>(() => History().ReserveAsync(new(initial.ProfileId, state.CandidateGeneration, Guid.NewGuid()), Ct));
        await Assert.ThrowsAsync<InvalidOperationException>(() => History().RequireAsync(new(initial.ProfileId, initial.GenerationId, Guid.NewGuid()), Ct));
        Assert.Equal(state.CandidateGeneration, (await History().ReadAsync(Ct))[^1].CandidateGeneration);
    }

    [Fact, Trait("AT", "AT-207")]
    public async Task Foreign_profile_or_stale_original_fails_without_reserving_a_target()
    {
        await History().CreateNewAsync(Ct);
        await Assert.ThrowsAsync<InvalidOperationException>(() => History().ReserveAsync(new(Guid.NewGuid(), initial.GenerationId, Guid.NewGuid()), Ct));
        await Assert.ThrowsAsync<InvalidOperationException>(() => History().ReserveAsync(new(initial.ProfileId, Guid.NewGuid(), Guid.NewGuid()), Ct));
        Assert.Single(await History().ReadAsync(Ct));
    }

    [Fact, Trait("AT", "AT-207")]
    public async Task Missing_history_is_not_recreated_and_interrupted_bootstrap_is_not_overwritten()
    {
        await Assert.ThrowsAsync<IOException>(() => History().ReadAsync(Ct)); Assert.False(File.Exists(PathOfHistory));
        await File.WriteAllBytesAsync(PathOfHistory, [], Ct);
        await Assert.ThrowsAsync<IOException>(() => History().CreateNewAsync(Ct)); Assert.Equal(0, new FileInfo(PathOfHistory).Length);
    }

    [Fact, Trait("AT", "AT-207")]
    public async Task Broken_ancestry_is_rejected_without_changing_the_stored_file()
    {
        await History().CreateNewAsync(Ct); await History().ReserveAsync(new(initial.ProfileId, initial.GenerationId, Guid.NewGuid()), Ct);
        await using (var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = PathOfHistory, Pooling = false }.ToString()))
        {
            await connection.OpenAsync(Ct); using var command = connection.CreateCommand();
            command.CommandText = "UPDATE restore_history SET original=$original WHERE sequence=1";
            command.Parameters.AddWithValue("$original", Guid.NewGuid().ToString("N")); await command.ExecuteNonQueryAsync(Ct);
        }
        var before = await File.ReadAllBytesAsync(PathOfHistory, Ct);
        await Assert.ThrowsAsync<IOException>(() => History().ReadAsync(Ct)); Assert.Equal(before, await File.ReadAllBytesAsync(PathOfHistory, Ct));
    }
}
