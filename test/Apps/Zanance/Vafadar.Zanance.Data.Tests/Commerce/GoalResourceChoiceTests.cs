using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Testing;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data.Tests.Commerce;

/// <summary>AT-145: actual scoped goal choice persistence and atomic goal/earmark writes without financial loss.</summary>
[Trait("AT", "AT-145")]
public sealed class GoalResourceChoiceTests : IDisposable
{
    private readonly TemporaryDirectory _directory = new();
    private readonly List<ServiceProvider> _providers = [];
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static readonly Guid ScopeId = Guid.Parse("11111111-2222-3333-4444-555555555555");
    private static readonly DateOnly Day = new(2026, 10, 12);
    private static readonly QuotaScope Scope = new(QuotaScopeKind.PersonalProfile, ScopeId);

    private ServiceProvider Provider(string path, AccessSource source)
    {
        var services = new ServiceCollection().AddSingleton<TimeProvider>(TimeProvider.System)
            .AddSingleton<ICommercialWriteAccessSource>(source).AddZananceData(path);
        var registration = services.Single(item => item.ServiceType == typeof(IDbContextFactory<ZananceDbContext>));
        services.Remove(registration);
        services.AddSingleton<IDbContextFactory<ZananceDbContext>>(p => new TriggerConnections(
            (IDbContextFactory<ZananceDbContext>)ActivatorUtilities.CreateInstance(p, registration.ImplementationType!), source));
        var provider = services.BuildServiceProvider(); _providers.Add(provider);
        provider.MigrateLocalDatabase<ZananceDbContext>(); return provider;
    }

    private async Task<Fixture> CreateAsync()
    {
        var path = _directory.Combine(Guid.NewGuid() + ".db"); var source = new AccessSource();
        var provider = Provider(path, source); var store = provider.GetRequiredService<ZananceStore>();
        var goals = provider.GetRequiredService<GoalStore>();
        var accounts = Enumerable.Range(0, 5).Select(index => new Account
        { Name = "Fictitious account " + index, CurrencyCode = "EUR", OpeningDate = Day }).ToArray();
        foreach (var account in accounts) await store.SaveAccountAsync(account, Ct);
        var items = Enumerable.Range(0, 5).Select(index => new Goal
        {
            Name = "Fictitious goal " + index, Type = GoalType.AccountBalance, AccountId = accounts[index].Id,
            CurrencyCode = "EUR", TargetAmount = 10000, Note = "Complete original note",
            State = index == 2 ? GoalState.Paused : index == 3 ? GoalState.Completed : index == 4 ? GoalState.Archived : GoalState.Active,
        }).ToArray();
        foreach (var goal in items) await goals.SaveGoalAsync(goal, Ct);
        return new(path, provider, source, accounts, items);
    }

    private sealed record Fixture(string Path, ServiceProvider Provider, AccessSource Source, Account[] Accounts, Goal[] Items)
    {
        public ResourceChoiceStore Choices => Provider.GetRequiredService<ResourceChoiceStore>();
        public GoalStore Goals => Provider.GetRequiredService<GoalStore>();
        public ZananceStore Store => Provider.GetRequiredService<ZananceStore>();
        public void Enable(ProductPlan product = ProductPlan.Free, Guid? scope = null) => Source.Current = new(Path,
            new(product, new(EntitlementScopeKind.PersonalProfile, scope ?? ScopeId)));
        public Task<ResourceChoiceSnapshot> Read() => Choices.ReadAsync(QuotaKind.Goals, Ct);
        public async Task Choose(params Guid[] ids) => await Choices.SaveAsync(await Read(), ids, Ct);
        public Goal New() => new() { Name = "Explicit new goal", Type = GoalType.AccountBalance,
            AccountId = Accounts[3].Id, CurrencyCode = "EUR", TargetAmount = 5000 };
    }

    private static async Task SqlAsync(Fixture f, string sql)
    {
        await using var db = await f.Provider.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        await db.Database.ExecuteSqlRawAsync(sql, Ct);
    }

    [Fact]
    public async Task Inactive_loading_keeps_all_original_states_and_never_creates_a_choice()
    {
        var f = await CreateAsync(); var before = await SnapshotAsync(f.Provider); var read = await f.Read();
        Assert.False(read.CanChoose); Assert.Null(read.Maximum); Assert.False(read.RequiresSelection);
        Assert.Equal(f.Items.Take(3).Select(goal => goal.Id).Order(), read.SelectedIds);
        Assert.Equal(QuotaItemState.Paused, read.Items.Single(item => item.Id == f.Items[2].Id).State);
        Assert.Equal(QuotaItemState.Ended, read.Items.Single(item => item.Id == f.Items[3].Id).State);
        Assert.All(read.Items, item => { Assert.Equal(GoalType.AccountBalance, item.GoalType); Assert.Equal("EUR", item.CurrencyCode); });
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Choices.SaveAsync(read, [f.Items[0].Id], Ct));
        Assert.Equal(before, await SnapshotAsync(f.Provider));
    }

    [Fact]
    public async Task Reviewed_single_choice_survives_reopen_without_changing_any_financial_row()
    {
        var f = await CreateAsync(); f.Enable(); var before = await SnapshotAsync(f.Provider, choices: false);
        var read = await f.Read(); Assert.True(read.RequiresSelection); Assert.Empty(read.SelectedIds); Assert.Equal(1, read.Maximum);
        var events = 0; f.Choices.Changed += (_, _) => events++;
        Assert.True(await f.Choices.SaveAsync(read, [f.Items[2].Id, f.Items[2].Id], Ct));
        var reopened = Provider(f.Path, f.Source).GetRequiredService<ResourceChoiceStore>();
        var current = await reopened.ReadAsync(QuotaKind.Goals, Ct);
        Assert.Equal(new[] { f.Items[2].Id }, current.SelectedIds); Assert.False(current.RequiresSelection);
        var stored = await SnapshotAsync(f.Provider);
        Assert.False(await reopened.SaveAsync(current, current.SelectedIds, Ct));
        Assert.Equal(1, events); Assert.Equal(stored, await SnapshotAsync(f.Provider));
        Assert.Equal(before, await SnapshotAsync(f.Provider, choices: false));
        Assert.Equal(GoalState.Paused, (await f.Goals.GetGoalAsync(f.Items[2].Id, Ct))!.State);
    }

    [Theory]
    [InlineData("excess")]
    [InlineData("foreign")]
    [InlineData("completed")]
    [InlineData("archived")]
    public async Task Invalid_subsets_write_nothing(string reason)
    {
        var f = await CreateAsync(); f.Enable(); var before = await SnapshotAsync(f.Provider);
        var ids = reason == "excess" ? f.Items.Take(2).Select(goal => goal.Id).ToArray()
            : new[] { reason == "foreign" ? Guid.NewGuid() : f.Items[reason == "completed" ? 3 : 4].Id };
        var reviewed = await f.Read();
        await Assert.ThrowsAsync<ArgumentException>(() => f.Choices.SaveAsync(reviewed, ids, Ct));
        Assert.Equal(before, await SnapshotAsync(f.Provider));
    }

    [Theory]
    [InlineData("choice")]
    [InlineData("name")]
    [InlineData("type")]
    [InlineData("currency")]
    [InlineData("state")]
    [InlineData("rights")]
    [InlineData("scope")]
    public async Task A_retired_review_cannot_replace_the_current_choice(string change)
    {
        var f = await CreateAsync(); f.Enable(); var read = await f.Read();
        if (change == "choice") await f.Choose(f.Items[1].Id);
        else if (change == "rights") f.Enable(ProductPlan.Plus);
        else if (change == "scope") f.Enable(scope: Guid.NewGuid());
        else await SqlAsync(f, change switch
        {
            "name" => "UPDATE Goals SET Name='Changed original'",
            "type" => "UPDATE Goals SET Type=0",
            "currency" => "UPDATE Goals SET CurrencyCode='USD'",
            _ => "UPDATE Goals SET State=1 WHERE State=0",
        });
        var before = await SnapshotAsync(f.Provider);
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Choices.SaveAsync(read, [f.Items[0].Id], Ct));
        Assert.Equal(before, await SnapshotAsync(f.Provider));
    }

    [Fact]
    public async Task Explicit_empty_choice_joins_only_a_new_goal_and_pause_keeps_its_slot()
    {
        var f = await CreateAsync(); f.Enable(); await f.Choose();
        var goal = f.New(); await f.Goals.SaveGoalWithContributionPlanAsync(goal,
            new() { GoalId = goal.Id, Rule = new() { Start = Day, Frequency = Frequency.Monthly } }, Ct);
        Assert.Equal(new[] { goal.Id }, (await f.Read()).SelectedIds);
        goal.State = GoalState.Paused; await f.Goals.SaveGoalAsync(goal, Ct);
        Assert.Equal(new[] { goal.Id }, (await f.Read()).SelectedIds);
        var before = await SnapshotAsync(f.Provider); var next = f.New(); next.AccountId = f.Accounts[4].Id;
        await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => f.Goals.SaveGoalAsync(next, Ct));
        Assert.Equal(before, await SnapshotAsync(f.Provider)); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Theory]
    [InlineData(GoalState.Completed)]
    [InlineData(GoalState.Archived)]
    public async Task Ending_releases_only_the_owned_slot_and_reopening_joins_it(GoalState ending)
    {
        var f = await CreateAsync(); f.Enable(); await f.Choose(f.Items[0].Id);
        f.Items[0].State = ending; await f.Goals.SaveGoalAsync(f.Items[0], Ct);
        Assert.Empty((await f.Read()).SelectedIds); Assert.False((await f.Read()).RequiresSelection);
        f.Items[0].State = GoalState.Paused; await f.Goals.SaveGoalAsync(f.Items[0], Ct);
        Assert.Equal(new[] { f.Items[0].Id }, (await f.Read()).SelectedIds);
        f.Items[3].State = GoalState.Active; var before = await SnapshotAsync(f.Provider);
        await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => f.Goals.SaveGoalAsync(f.Items[3], Ct));
        Assert.Equal(before, await SnapshotAsync(f.Provider));
    }

    [Fact]
    public async Task Unselected_pause_cannot_resume_but_its_history_can_be_corrected_or_completed()
    {
        var f = await CreateAsync(); f.Enable(); await f.Choose(f.Items[0].Id);
        var goal = f.Items[2]; goal.State = GoalState.Active; var before = await SnapshotAsync(f.Provider);
        var rejected = await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => f.Goals.SaveGoalAsync(goal, Ct));
        Assert.Equal(QuotaKind.Goals, rejected.Quota); Assert.Equal(goal.Id, rejected.ResourceId); Assert.False(rejected.RequiresSelection);
        Assert.Equal(before, await SnapshotAsync(f.Provider));
        goal.State = GoalState.Paused; goal.Note = "Correct retained history"; await f.Goals.SaveGoalAsync(goal, Ct);
        goal.State = GoalState.Completed; await f.Goals.SaveGoalAsync(goal, Ct);
        Assert.Equal(new[] { f.Items[0].Id }, (await f.Read()).SelectedIds);
        Assert.Equal(goal.Note, (await f.Goals.GetGoalAsync(goal.Id, Ct))!.Note);
    }

    [Fact]
    public async Task Deleting_the_selected_goal_releases_its_slot_and_all_its_dependent_history_atomically()
    {
        var f = await CreateAsync(); await f.Goals.SaveContributionPlanAsync(f.Items[0].Id,
            new() { GoalId = f.Items[0].Id, Amount = 500, Rule = new() { Start = Day } }, Ct);
        await f.Goals.AddAllocationAsync(new() { GoalId = f.Items[0].Id, AccountId = f.Accounts[0].Id, Amount = 1000, Date = Day }, Ct);
        f.Enable(); await f.Choose(f.Items[0].Id); await f.Goals.DeleteGoalAsync(f.Items[0].Id, Ct);
        Assert.Empty((await f.Read()).SelectedIds); Assert.Equal(4, (await f.Goals.GetGoalsAsync(Ct)).Count);
        Assert.Empty(await f.Goals.GetAllocationsAsync(cancellationToken: Ct)); Assert.Empty(await f.Goals.GetContributionPlansAsync(Ct));
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
        var goal = f.New(); await f.Goals.SaveGoalAsync(goal, Ct); Assert.Equal(new[] { goal.Id }, (await f.Read()).SelectedIds);
    }

    [Theory]
    [InlineData("insert")]
    [InlineData("contribution")]
    [InlineData("delete")]
    public async Task Failure_rolls_back_goal_choice_and_every_dependent_row(string action)
    {
        var f = await CreateAsync(); f.Enable(); await f.Choose(action == "delete" ? [f.Items[0].Id] : []);
        await SqlAsync(f, action switch
        {
            "delete" => "CREATE TRIGGER FailGoal AFTER DELETE ON Goals BEGIN SELECT RAISE(ABORT, 'Owned failure'); END;",
            "contribution" => "CREATE TRIGGER FailGoal AFTER INSERT ON ContributionPlans BEGIN SELECT RAISE(ABORT, 'Owned failure'); END;",
            _ => "CREATE TRIGGER FailGoal AFTER INSERT ON Goals BEGIN SELECT RAISE(ABORT, 'Owned failure'); END;",
        });
        var before = await SnapshotAsync(f.Provider); var events = 0; f.Goals.Changed += (_, _) => events++;
        if (action == "delete") await Assert.ThrowsAsync<SqliteException>(() => f.Goals.DeleteGoalAsync(f.Items[0].Id, Ct));
        else if (action == "insert") await Assert.ThrowsAsync<DbUpdateException>(() => f.Goals.SaveGoalAsync(f.New(), Ct));
        else { var goal = f.New(); await Assert.ThrowsAsync<DbUpdateException>(() => f.Goals.SaveGoalWithContributionPlanAsync(goal,
            new() { GoalId = goal.Id, Rule = new() { Start = Day, Frequency = Frequency.Monthly } }, Ct)); }
        Assert.Equal(before, await SnapshotAsync(f.Provider)); Assert.Equal(0, events);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Malformed_choice_blocks_reopening_but_keeps_corrections_release_and_explicit_repair(bool revision)
    {
        var f = await CreateAsync(); var allocation = new GoalAllocation
        { GoalId = f.Items[0].Id, AccountId = f.Accounts[0].Id, Amount = 1000, Date = Day };
        await f.Goals.AddAllocationAsync(allocation, Ct); f.Enable(); await f.Choose(f.Items[0].Id);
        await SqlAsync(f, revision ? "UPDATE ResourceSelections SET Revision='00000000-0000-0000-0000-000000000000'"
            : "UPDATE ResourceSelections SET IdentitySet='Malformed owned choice'");
        var before = await SnapshotAsync(f.Provider);
        await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => f.Goals.SaveGoalAsync(f.New(), Ct));
        Assert.Equal(before, await SnapshotAsync(f.Provider));
        f.Items[0].Note = "Retained correction"; await f.Goals.SaveGoalAsync(f.Items[0], Ct);
        await f.Goals.AddAllocationAsync(new() { GoalId = f.Items[0].Id, AccountId = f.Accounts[0].Id, Amount = -500, Date = Day }, Ct);
        var read = await f.Read(); Assert.True(read.RequiresSelection); Assert.Empty(read.SelectedIds);
        Assert.True(await f.Choices.SaveAsync(read, [f.Items[2].Id], Ct)); Assert.False((await f.Read()).RequiresSelection);
        Assert.Equal(f.Items[0].Note, (await f.Goals.GetGoalAsync(f.Items[0].Id, Ct))!.Note);
        Assert.Equal(500, (await f.Goals.GetAllocationsAsync(cancellationToken: Ct)).Sum(item => item.Amount));
    }

    [Fact]
    public async Task Choosing_a_retained_advanced_goal_never_grants_paid_earmark_or_contribution_tools()
    {
        var f = await CreateAsync(); f.Items[0].Type = GoalType.Earmark; await f.Goals.SaveGoalAsync(f.Items[0], Ct);
        f.Enable(); await f.Choose(f.Items[0].Id); var before = await SnapshotAsync(f.Provider);
        var error = await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => f.Goals.AddAllocationAsync(
            new() { GoalId = f.Items[0].Id, AccountId = f.Accounts[0].Id, Amount = 500, Date = Day }, Ct));
        Assert.Equal(CommercialFeature.AdvancedGoals, error.Feature); Assert.Equal(FeaturePermission.RequiresPlus, error.Permission);
        await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => f.Goals.SaveContributionPlanAsync(f.Items[0].Id,
            new() { GoalId = f.Items[0].Id, Amount = 500, Rule = new() { Start = Day } }, Ct));
        Assert.Equal(before, await SnapshotAsync(f.Provider));
    }

    [Theory]
    [InlineData(ProductPlan.Plus)]
    [InlineData(ProductPlan.Pro)]
    public async Task Unlimited_upgrade_keeps_limited_choice_without_restricting_new_work(ProductPlan product)
    {
        var f = await CreateAsync(); f.Enable(); await f.Choose(f.Items[0].Id); f.Enable(product);
        var choice = await ChoiceSqlAsync(f); var read = await f.Read(); Assert.Null(read.Maximum); Assert.False(read.CanChoose);
        Assert.Equal(3, read.SelectedIds.Count);
        await f.Goals.AddAllocationAsync(new() { GoalId = f.Items[1].Id, AccountId = f.Accounts[1].Id, Amount = 500, Date = Day }, Ct);
        Assert.Equal(choice, await ChoiceSqlAsync(f)); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    private static async Task<string> ChoiceSqlAsync(Fixture f)
    {
        await using var db = await f.Provider.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        await db.Database.OpenConnectionAsync(Ct); using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT IdentitySet || Revision FROM ResourceSelections";
        return (string)(await command.ExecuteScalarAsync(Ct))!;
    }

    [Fact]
    public async Task Cached_goal_choice_requires_exact_kind_and_scope_and_stored_choice_overrides_it()
    {
        var f = await CreateAsync(); f.Enable();
        Assert.Throws<ArgumentException>(() => new CommercialWriteAccess(f.Path, f.Source.Current.Context!,
            goalSelection: new(QuotaKind.FinancialAccounts, Scope, [f.Items[0].Id])));
        Assert.Throws<ArgumentException>(() => new CommercialWriteAccess(f.Path, f.Source.Current.Context!,
            goalSelection: new(QuotaKind.Goals, new(QuotaScopeKind.PersonalProfile, Guid.NewGuid()), [f.Items[0].Id])));
        f.Source.Current = new(f.Path, f.Source.Current.Context!, goalSelection: new(QuotaKind.Goals, Scope, [f.Items[0].Id]));
        await f.Choose(f.Items[2].Id); f.Items[2].State = GoalState.Active; await f.Goals.SaveGoalAsync(f.Items[2], Ct);
        Assert.Equal(new[] { f.Items[2].Id }, (await f.Read()).SelectedIds);
        Assert.Equal(new[] { f.Items[0].Id }, f.Source.Current.GoalSelection!.SelectedIds);
    }

    [Fact]
    public async Task Independent_writers_compete_for_one_explicit_empty_goal_slot()
    {
        var f = await CreateAsync(); f.Enable(); await f.Choose();
        var other = Provider(f.Path, f.Source).GetRequiredService<GoalStore>();
        var first = f.New(); var second = f.New(); second.AccountId = f.Accounts[4].Id;
        var results = await Task.WhenAll(TrySave(f.Goals, first), TrySave(other, second));
        Assert.Equal(1, results.Count(result => result)); Assert.Single((await f.Read()).SelectedIds);
        Assert.Equal(6, (await f.Goals.GetGoalsAsync(Ct)).Count);
    }

    private static async Task<bool> TrySave(GoalStore store, Goal goal)
    {
        try { await store.SaveGoalAsync(goal, Ct); return true; }
        catch (CommercialWriteRejectedException) { return false; }
    }

    [Theory]
    [InlineData("choice")]
    [InlineData("goal")]
    public async Task Access_retirement_after_actual_SQL_rolls_back_the_entire_operation(string operation)
    {
        var f = await CreateAsync(); f.Enable(); await f.Choose();
        await SqlAsync(f, operation == "choice"
            ? "CREATE TRIGGER RetireAccess AFTER UPDATE ON ResourceSelections BEGIN SELECT RetireChoiceAccess(); END;"
            : "CREATE TRIGGER RetireAccess AFTER INSERT ON Goals BEGIN SELECT RetireChoiceAccess(); END;");
        var before = await SnapshotAsync(f.Provider); var events = 0;
        f.Goals.Changed += (_, _) => events++; f.Choices.Changed += (_, _) => events++;
        f.Source.AfterSql = () => f.Source.Current = CommercialWriteAccess.Inactive;
        if (operation == "goal") await Assert.ThrowsAsync<InvalidOperationException>(() => f.Goals.SaveGoalAsync(f.New(), Ct));
        else { var reviewed = await f.Read(); await Assert.ThrowsAsync<InvalidOperationException>(() => f.Choices.SaveAsync(reviewed, [f.Items[0].Id], Ct)); }
        Assert.Equal(before, await SnapshotAsync(f.Provider)); Assert.Equal(0, events);
    }

    [Fact]
    public async Task Switching_the_opened_file_rejects_the_review_and_preserves_both_files()
    {
        var f = await CreateAsync(); var other = await CreateAsync(); f.Enable(); var reviewed = await f.Read();
        var first = await SnapshotAsync(f.Provider); var second = await SnapshotAsync(other.Provider);
        var location = f.Provider.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>(); location.MoveTo(other.Path);
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Choices.SaveAsync(reviewed, [f.Items[0].Id], Ct));
        location.MoveTo(f.Path); Assert.Equal(first, await SnapshotAsync(f.Provider)); Assert.Equal(second, await SnapshotAsync(other.Provider));
    }

    private static async Task<string> SnapshotAsync(ServiceProvider provider, bool choices = true)
    {
        await using var db = await provider.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        await db.Database.OpenConnectionAsync(Ct); var connection = db.Database.GetDbConnection(); var tables = new List<string>();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name";
            using var reader = await command.ExecuteReaderAsync(Ct); while (await reader.ReadAsync(Ct)) tables.Add(reader.GetString(0));
        }
        Assert.Equal(25, tables.Count); var all = new SortedDictionary<string, List<string>>();
        foreach (var table in tables.Where(table => choices || table != "ResourceSelections"))
        {
            using var command = connection.CreateCommand(); command.CommandText = "SELECT * FROM \"" + table + "\"";
            using var reader = await command.ExecuteReaderAsync(Ct); var rows = new List<string>();
            while (await reader.ReadAsync(Ct))
            {
                var values = new object?[reader.FieldCount];
                for (var i = 0; i < values.Length; i++) values[i] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                rows.Add(JsonSerializer.Serialize(values));
            }
            rows.Sort(StringComparer.Ordinal); all[table] = rows;
        }
        return JsonSerializer.Serialize(all);
    }

    private sealed class AccessSource : ICommercialWriteAccessSource
    {
        internal CommercialWriteAccess Current = CommercialWriteAccess.Inactive;
        internal Action? AfterSql;
        public CommercialWriteAccess Capture(string databasePath) => Current;
    }

    /// <summary>Retires cached rights from the actual fixture-only SQLite insert without replacing application logic.</summary>
    private sealed class TriggerConnections(IDbContextFactory<ZananceDbContext> inner, AccessSource source) : IDbContextFactory<ZananceDbContext>
    {
        public ZananceDbContext CreateDbContext()
        {
            var db = inner.CreateDbContext();
            ((SqliteConnection)db.Database.GetDbConnection()).CreateFunction("RetireChoiceAccess", () => { source.AfterSql?.Invoke(); return 1; });
            return db;
        }
    }

    public void Dispose()
    {
        foreach (var provider in _providers) provider.Dispose();
        SqliteTestPools.Clear(_directory); _directory.Dispose();
    }
}
