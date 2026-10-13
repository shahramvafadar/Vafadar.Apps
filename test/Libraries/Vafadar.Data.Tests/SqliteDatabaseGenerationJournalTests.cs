using Microsoft.Data.Sqlite;

namespace Vafadar.Data.Tests;

/// <summary>Actual isolated SQLite journal behavior, not duplicated checkpoint persistence logic.</summary>
public sealed class SqliteDatabaseGenerationJournalTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "vafadar-generation-journal-" + Guid.NewGuid().ToString("N"));
    private readonly DatabaseGenerationCheckpoint initial = new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private string PathOfJournal => Path.Combine(root, "generation.sqlite");

    /// <summary>Creates only the independently owned directory.</summary>
    public SqliteDatabaseGenerationJournalTests() => Directory.CreateDirectory(root);

    private SqliteDatabaseGenerationJournal Store(DatabaseGenerationCheckpoint? identity = null) => new(PathOfJournal, identity ?? initial);
    private static DatabaseGenerationCheckpoint Next(DatabaseGenerationCheckpoint state) => state.Advance(state.ProfileId, state.CandidateGeneration, state.Phase + 1);

    [Fact, Trait("AT", "AT-216")]
    public async Task Interrupted_initial_journal_publication_preserves_captured_state_on_resume()
    {
        await Assert.ThrowsAsync<IOException>(() => Store().CreateNewAsync(new InterruptedPublication(), Ct));
        Assert.False(File.Exists(PathOfJournal));
        Assert.Equal(initial, await new SqliteDatabaseGenerationJournal(PathOfJournal + ".creating", initial).ReadAsync(Ct));
        await Store().CreateNewAsync(Ct); Assert.Equal(initial, await Store().ReadAsync(Ct));
        Assert.False(File.Exists(PathOfJournal + ".creating"));
    }

    [Fact, Trait("AT", "AT-216")]
    public async Task Advanced_staged_journal_cannot_be_published_as_a_fresh_original()
    {
        await Assert.ThrowsAsync<IOException>(() => Store().CreateNewAsync(new InterruptedPublication(), Ct));
        var staged = new SqliteDatabaseGenerationJournal(PathOfJournal + ".creating", initial);
        await staged.CompareAndCommitAsync(initial, Next(initial), Ct);
        await Assert.ThrowsAsync<InvalidOperationException>(() => Store().CreateNewAsync(Ct));
        Assert.Equal(Next(initial), await staged.ReadAsync(Ct)); Assert.False(File.Exists(PathOfJournal));
    }

    private sealed class InterruptedPublication : IDatabaseGenerationFileDurability
    {
        public long AvailableBytes => long.MaxValue;
        public Task SynchronizeFileAsync(string path, CancellationToken ct) => throw new IOException("Fictitious pre-publication interruption.");
        public Task SynchronizeDirectoryAsync(CancellationToken ct) => throw new IOException("Unexpected publication.");
    }

    [Fact, Trait("AT", "AT-194")]
    public async Task Every_committed_phase_reopens_with_its_exact_original_identity()
    {
        await Store().CreateNewAsync(Ct);
        var expected = initial;
        while (true)
        {
            var actual = await Store().ReadAsync(Ct);
            Assert.Equal(expected, actual);
            if (expected.Phase == DatabaseGenerationPhase.Completed) break;
            var next = Next(expected);
            await Store().CompareAndCommitAsync(expected, next, Ct);
            expected = next;
        }
    }

    [Fact, Trait("AT", "AT-194")]
    public async Task Stale_writer_cannot_replace_an_already_committed_checkpoint()
    {
        await Store().CreateNewAsync(Ct);
        await Store().CompareAndCommitAsync(initial, Next(initial), Ct);
        await Assert.ThrowsAsync<InvalidOperationException>(() => Store().CompareAndCommitAsync(initial, Next(initial), Ct));
        Assert.Equal(Next(initial), await Store().ReadAsync(Ct));
    }

    [Fact]
    public async Task Missing_journal_is_not_recreated_by_read_or_commit()
    {
        await Assert.ThrowsAsync<IOException>(() => Store().ReadAsync(Ct));
        await Assert.ThrowsAsync<IOException>(() => Store().CompareAndCommitAsync(initial, Next(initial), Ct));
        Assert.False(File.Exists(PathOfJournal));
    }

    [Fact]
    public async Task Explicit_creation_does_not_replace_an_existing_selected_journal()
    {
        await Store().CreateNewAsync(Ct);
        var state = initial;
        for (var i = 0; i < 3; i++) { var next = Next(state); await Store().CompareAndCommitAsync(state, next, Ct); state = next; }
        await Assert.ThrowsAsync<IOException>(() => Store().CreateNewAsync(Ct));
        Assert.True((await Store().ReadAsync(Ct)).CandidateSelected);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task Foreign_profile_or_generation_cannot_read_or_advance(int changed)
    {
        await Store().CreateNewAsync(Ct);
        var foreign = new DatabaseGenerationCheckpoint(changed == 0 ? Guid.NewGuid() : initial.ProfileId,
            changed == 1 ? Guid.NewGuid() : initial.OriginalGeneration, changed == 2 ? Guid.NewGuid() : initial.CandidateGeneration);
        await Assert.ThrowsAsync<IOException>(() => Store(foreign).ReadAsync(Ct));
        await Assert.ThrowsAsync<IOException>(() => Store(foreign).CompareAndCommitAsync(foreign, Next(foreign), Ct));
        Assert.Equal(initial, await Store().ReadAsync(Ct));
    }

    [Theory]
    [InlineData("PRAGMA user_version=2")]
    [InlineData("CREATE TABLE unrelated(value TEXT)")]
    [InlineData("DELETE FROM generation_state")]
    [InlineData("PRAGMA ignore_check_constraints=ON; UPDATE generation_state SET phase=99")]
    public async Task Damaged_or_unknown_format_is_not_interpreted_as_original_state(string mutation)
    {
        await Store().CreateNewAsync(Ct);
        await using (var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = PathOfJournal, Pooling = false }.ToString()))
        {
            await connection.OpenAsync(Ct);
            using var command = connection.CreateCommand(); command.CommandText = mutation;
            await command.ExecuteNonQueryAsync(Ct);
        }
        await Assert.ThrowsAsync<IOException>(() => Store().ReadAsync(Ct));
    }

    [Fact]
    public async Task Cancelled_commit_retains_last_selected_checkpoint()
    {
        await Store().CreateNewAsync(Ct);
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Store().CompareAndCommitAsync(initial, Next(initial), cancelled.Token));
        Assert.Equal(initial, await Store().ReadAsync(Ct));
    }

    [Fact]
    public async Task Skipped_commit_is_refused_without_writing()
    {
        await Store().CreateNewAsync(Ct);
        await Assert.ThrowsAsync<InvalidOperationException>(() => Store().CompareAndCommitAsync(initial, Next(Next(initial)), Ct));
        Assert.Equal(initial, await Store().ReadAsync(Ct));
    }

    /// <summary>Removes only this fixture's owned files; journal connections never use global pools.</summary>
    public void Dispose() => Directory.Delete(root, recursive: true);
}
