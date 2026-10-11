using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Testing;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Performance;

namespace Vafadar.Zanance.Data.Tests;

/// <summary>The benchmark must preserve real migration/ledger rules and refuse existing data.</summary>
public sealed class PerformanceFixtureTests : IDisposable
{
    private readonly TemporaryDirectory _directory = new();
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() { SqliteTestPools.Clear(_directory); _directory.Dispose(); }

    [Fact, Trait("AT", "AT-82")]
    public void Tenfold_scales_entries_accounts_and_schedules_together()
    {
        Assert.Equal(new(10_000, 20, 100), Workload.Reference);
        Assert.Equal(new(100_000, 200, 1_000), Workload.Tenfold);
    }

    [Fact, Trait("AT", "AT-82")]
    public void Existing_directory_and_its_data_are_never_replaced()
    {
        var file = _directory.Combine("sentinel.txt"); File.WriteAllText(file, "keep");
        Assert.Throws<IOException>(() => FixtureDirectory.Create(Path.GetDirectoryName(file)!));
        Assert.Equal("keep", File.ReadAllText(file));
    }

    [Fact, Trait("AT", "AT-82")]
    public void Existing_file_is_never_used_as_a_fixture_directory()
    {
        var file = _directory.Combine("existing"); File.WriteAllText(file, "keep");
        Assert.Throws<IOException>(() => FixtureDirectory.Create(file)); Assert.Equal("keep", File.ReadAllText(file));
    }

    [Theory, Trait("AT", "AT-82")]
    [InlineData(9, 2, 1), InlineData(10, 1, 1), InlineData(10, 2, 0)]
    public async Task Invalid_workload_creates_no_files(int entries, int accounts, int schedules)
    {
        var path = _directory.Combine("invalid");
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => PerformanceFixture.CreateAsync(path, new(entries, accounts, schedules), Ct));
        Assert.False(Directory.Exists(path));
    }

    [Fact, Trait("AT", "AT-82")]
    public async Task Migration_preserves_every_legacy_row_and_plans_never_post_or_remind()
    {
        var folder = _directory.Combine("proof");
        var migration = await PerformanceFixture.CreateAsync(folder, new(100, 4, 6), Ct);
        Assert.Equal(migration.BeforeFingerprint, migration.AfterFingerprint);
        Assert.True(File.Exists(Path.Combine(folder, "legacy.db")));
        await using var services = new ServiceCollection().AddZananceData(Path.Combine(folder, "fixture.db")).BuildServiceProvider();
        var store = services.GetRequiredService<ZananceStore>();
        var entries = await store.GetEntriesAsync(cancellationToken: Ct);
        var accounts = await store.GetAccountsAsync(cancellationToken: Ct);
        var plans = await services.GetRequiredService<PlanStore>().GetSchedulesAsync(Ct);
        Assert.Equal(100, entries.Count); Assert.Equal(4, accounts.Count); Assert.Equal(6, plans.Count);
        Assert.All(plans, p => { Assert.Equal(ScheduleState.Active, p.State); Assert.False(p.AutoPost); Assert.False(p.ReminderEnabled); });
        Assert.Equal(10, entries.Count(e => e.Kind == EntryKind.Transfer));
        Assert.All(entries.Where(e => e.Kind == EntryKind.Transfer), e => { Assert.NotEqual(e.AccountId, e.ToAccountId); Assert.Equal(e.Amount, e.ToAmount); });
        Assert.All(entries, e => { Assert.Null(e.ScheduleId); Assert.Null(e.CategoryId); Assert.Null(e.RefundOfId); Assert.True(e.Amount > 0); });
        Assert.All(entries.Where(e => e.Kind == EntryKind.Transfer), e => Assert.Equal(0, EntrySearch.SignedResult(e)));
        var settings = await store.GetSettingsAsync(Ct);
        Assert.Equal(PerformanceFixture.Id(4, 0), settings.Id);
        Assert.True(settings.OnboardingCompleted); Assert.False(settings.AppLockEnabled); Assert.False(settings.ReviewReminderEnabled);
        Assert.Equal(accounts.First(a => a.Id == PerformanceFixture.Id(1, 0)).Id, settings.DefaultAccountId);
        await using var db = await services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        Assert.Empty(await db.Database.GetPendingMigrationsAsync(Ct));
    }

    [Fact, Trait("AT", "AT-82")]
    public async Task Independently_generated_fixtures_have_identical_legacy_rows()
    {
        var first = await PerformanceFixture.CreateAsync(_directory.Combine("first"), new(100, 4, 6), Ct);
        var second = await PerformanceFixture.CreateAsync(_directory.Combine("second"), new(100, 4, 6), Ct);
        Assert.Equal(first.BeforeFingerprint, second.BeforeFingerprint);
    }

    [Fact, Trait("AT", "AT-82")]
    public async Task Semicolon_in_a_fixture_path_is_a_literal_directory_name()
    {
        var folder = _directory.Combine("literal;fixture");
        var proof = await PerformanceFixture.CreateAsync(folder, new(10, 2, 1), Ct);
        Assert.Equal(proof.BeforeFingerprint, proof.AfterFingerprint);
        Assert.True(File.Exists(Path.Combine(folder, "fixture.db")));
    }

    [Fact, Trait("AT", "AT-82")]
    public async Task A_queued_entry_read_keeps_its_original_profile_after_the_location_moves()
    {
        var first = _directory.Combine("old-profile"); var second = _directory.Combine("new-profile");
        await PerformanceFixture.CreateAsync(first, new(100, 4, 6), Ct);
        await PerformanceFixture.CreateAsync(second, new(10, 2, 1), Ct);
        await using var services = new ServiceCollection().AddZananceData(Path.Combine(first, "fixture.db")).BuildServiceProvider();
        var store = services.GetRequiredService<ZananceStore>();
        var read = store.GetEntriesAsync(cancellationToken: Ct);
        services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>().MoveTo(Path.Combine(second, "fixture.db"));
        Assert.Equal(100, (await read).Count);
        Assert.Equal(10, (await store.GetEntriesAsync(cancellationToken: Ct)).Count);
    }

    [Fact, Trait("AT", "AT-82")]
    public async Task Cancelled_background_read_preserves_all_fixture_rows()
    {
        var folder = _directory.Combine("cancelled-read");
        await PerformanceFixture.CreateAsync(folder, new(100, 4, 6), Ct);
        var path = Path.Combine(folder, "fixture.db"); var before = await PerformanceFixture.FingerprintAsync(path, Ct);
        await using var services = new ServiceCollection().AddZananceData(path).BuildServiceProvider();
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => services.GetRequiredService<ZananceStore>().GetEntriesAsync(cancellationToken: cancelled.Token));
        Assert.Equal(before, await PerformanceFixture.FingerprintAsync(path, Ct));
    }
}
