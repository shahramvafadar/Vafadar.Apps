using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Testing;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data.Tests.Commerce;

/// <summary>AT-143: durable scoped choices, current review, actual writer bindings and unchanged financial history.</summary>
[Trait("AT", "AT-143")]
public sealed class ResourceChoiceStoreTests : IDisposable
{
    private readonly TemporaryDirectory _directory = new();
    private readonly List<ServiceProvider> _providers = [];
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static readonly Guid ScopeId = Guid.Parse("11111111-2222-3333-4444-555555555555");
    private static readonly DateOnly Day = new(2026, 10, 12);

    private ServiceProvider Provider(string path, AccessSource source)
    {
        var services = new ServiceCollection().AddSingleton<TimeProvider>(TimeProvider.System)
            .AddSingleton<ICommercialWriteAccessSource>(source).AddZananceData(path);
        var registration = services.Single(item => item.ServiceType == typeof(IDbContextFactory<ZananceDbContext>));
        services.Remove(registration);
        services.AddSingleton<IDbContextFactory<ZananceDbContext>>(p => new TriggerConnections(
            (IDbContextFactory<ZananceDbContext>)ActivatorUtilities.CreateInstance(p, registration.ImplementationType!), source));
        var provider = services.BuildServiceProvider();
        _providers.Add(provider);
        provider.MigrateLocalDatabase<ZananceDbContext>();
        return provider;
    }

    private async Task<Fixture> CreateAsync()
    {
        var path = _directory.Combine(Guid.NewGuid() + ".db");
        var source = new AccessSource();
        var provider = Provider(path, source);
        var accounts = Enumerable.Range(0, 5).Select(index => new Account
        { Name = "Fictitious account " + index, CurrencyCode = "EUR", OpeningDate = Day, IsArchived = index == 4 }).ToArray();
        var store = provider.GetRequiredService<ZananceStore>();
        foreach (var account in accounts) await store.SaveAccountAsync(account, Ct);
        var plans = Enumerable.Range(0, 8).Select(index => new Schedule
        {
            Name = "Fictitious plan " + index, AccountId = accounts[0].Id, Amount = 1200,
            State = index == 6 ? ScheduleState.Paused : index == 7 ? ScheduleState.Ended : ScheduleState.Active,
            PausedFrom = index == 6 ? Day : null,
            Rule = new() { Start = Day, Frequency = Frequency.Monthly },
        }).ToArray();
        await provider.GetRequiredService<PlanStore>().SaveSchedulesAsync(plans, Ct);
        return new(path, source, provider, accounts, plans);
    }

    private sealed record Fixture(string Path, AccessSource Source, ServiceProvider Provider, Account[] Accounts, Schedule[] Plans)
    {
        public ResourceChoiceStore Choices => Provider.GetRequiredService<ResourceChoiceStore>();
        public ZananceStore Store => Provider.GetRequiredService<ZananceStore>();
        public PlanStore PlanStore => Provider.GetRequiredService<PlanStore>();
        public void Enable(ProductPlan plan = ProductPlan.Free, bool shared = false, bool member = true, bool host = true, Guid? scope = null)
        {
            var id = scope ?? ScopeId;
            Source.Current = new(Path, new(plan, new(shared ? EntitlementScopeKind.SharedSpace : EntitlementScopeKind.PersonalProfile, id),
                shared ? new(id, member, host) : null));
        }
        public Guid Id(QuotaKind kind, int index) => kind == QuotaKind.FinancialAccounts ? Accounts[index].Id : Plans[index].Id;
        public LedgerEntry Entry(int account) => new() { Kind = EntryKind.Expense, AccountId = Accounts[account].Id, Amount = 100, Date = Day };
    }

    [Theory]
    [InlineData(QuotaKind.FinancialAccounts)]
    [InlineData(QuotaKind.RecurringPlans)]
    public async Task Inactive_loading_never_saves_or_invents_a_bounded_choice(QuotaKind kind)
    {
        var f = await CreateAsync(); var before = await SnapshotAsync(f.Provider);
        var reviewed = await f.Choices.ReadAsync(kind, Ct);
        Assert.False(reviewed.CanChoose); Assert.Null(reviewed.Maximum); Assert.False(reviewed.RequiresSelection);
        Assert.Contains(f.Id(kind, 0), reviewed.SelectedIds);
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Choices.SaveAsync(reviewed, [f.Id(kind, 0)], Ct));
        Assert.Equal(before, await SnapshotAsync(f.Provider));
    }

    [Theory]
    [InlineData(QuotaKind.FinancialAccounts, 3)]
    [InlineData(QuotaKind.RecurringPlans, 5)]
    public async Task An_explicit_choice_survives_a_new_provider_without_touching_financial_rows(QuotaKind kind, int maximum)
    {
        var f = await CreateAsync(); f.Enable(); var before = await SnapshotAsync(f.Provider, choices: false);
        var reviewed = await f.Choices.ReadAsync(kind, Ct); var events = 0;
        f.Choices.Changed += (_, _) => events++;
        Assert.True(reviewed.RequiresSelection); Assert.Equal(maximum, reviewed.Maximum); Assert.Empty(reviewed.SelectedIds);
        Assert.True(await f.Choices.SaveAsync(reviewed, [f.Id(kind, 1), f.Id(kind, 0), f.Id(kind, 1)], Ct));
        var other = Provider(f.Path, f.Source).GetRequiredService<ResourceChoiceStore>();
        var read = await other.ReadAsync(kind, Ct);
        Assert.Equal(new[] { f.Id(kind, 0), f.Id(kind, 1) }.Order(), read.SelectedIds);
        Assert.False(read.RequiresSelection); Assert.Equal(1, events);
        Assert.Equal(before, await SnapshotAsync(f.Provider, choices: false));
        var stored = await SnapshotAsync(f.Provider);
        Assert.False(await other.SaveAsync(read, read.SelectedIds.Reverse(), Ct));
        Assert.Equal(stored, await SnapshotAsync(f.Provider));
    }

    [Theory]
    [InlineData(QuotaKind.FinancialAccounts)]
    [InlineData(QuotaKind.RecurringPlans)]
    public async Task An_explicit_empty_choice_is_not_an_absent_choice(QuotaKind kind)
    {
        var f = await CreateAsync(); f.Enable();
        Assert.True(await f.Choices.SaveAsync(await f.Choices.ReadAsync(kind, Ct), [], Ct));
        var read = await f.Choices.ReadAsync(kind, Ct);
        Assert.False(read.RequiresSelection); Assert.Empty(read.SelectedIds);
        Assert.False(await f.Choices.SaveAsync(read, [], Ct));
    }

    [Theory]
    [InlineData(QuotaKind.FinancialAccounts, "excess")]
    [InlineData(QuotaKind.RecurringPlans, "excess")]
    [InlineData(QuotaKind.FinancialAccounts, "missing")]
    [InlineData(QuotaKind.RecurringPlans, "missing")]
    [InlineData(QuotaKind.FinancialAccounts, "historical")]
    [InlineData(QuotaKind.RecurringPlans, "historical")]
    public async Task Excessive_foreign_or_historical_ids_are_rejected_without_any_row_change(QuotaKind kind, string change)
    {
        var f = await CreateAsync(); f.Enable(); var before = await SnapshotAsync(f.Provider); var events = 0;
        f.Choices.Changed += (_, _) => events++;
        var ids = change == "missing" ? new[] { Guid.NewGuid() } : change == "historical"
            ? new[] { f.Id(kind, kind == QuotaKind.FinancialAccounts ? 4 : 7) }
            : Enumerable.Range(0, kind == QuotaKind.FinancialAccounts ? 4 : 6).Select(index => f.Id(kind, index)).ToArray();
        var read = await f.Choices.ReadAsync(kind, Ct);
        await Assert.ThrowsAsync<ArgumentException>(() => f.Choices.SaveAsync(read, ids, Ct));
        Assert.Equal(before, await SnapshotAsync(f.Provider)); Assert.Equal(0, events);
    }

    [Theory]
    [InlineData(QuotaKind.FinancialAccounts, "choice")]
    [InlineData(QuotaKind.RecurringPlans, "choice")]
    [InlineData(QuotaKind.FinancialAccounts, "original")]
    [InlineData(QuotaKind.RecurringPlans, "original")]
    [InlineData(QuotaKind.FinancialAccounts, "rights")]
    [InlineData(QuotaKind.RecurringPlans, "rights")]
    [InlineData(QuotaKind.FinancialAccounts, "scope")]
    [InlineData(QuotaKind.RecurringPlans, "scope")]
    public async Task A_retired_review_never_overwrites_current_choices(QuotaKind kind, string change)
    {
        var f = await CreateAsync(); f.Enable(); var read = await f.Choices.ReadAsync(kind, Ct);
        if (change == "choice")
            await f.Choices.SaveAsync(await f.Choices.ReadAsync(kind, Ct), [f.Id(kind, 1)], Ct);
        else if (change == "rights") f.Enable(ProductPlan.Plus);
        else if (change == "scope") f.Enable(scope: Guid.NewGuid());
        else
        {
            if (kind == QuotaKind.FinancialAccounts) { f.Accounts[0].Name += " changed"; await f.Store.SaveAccountAsync(f.Accounts[0], Ct); }
            else { f.Plans[0].State = ScheduleState.Paused; await f.PlanStore.SaveScheduleAsync(f.Plans[0], Ct); }
        }
        var before = await SnapshotAsync(f.Provider);
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Choices.SaveAsync(read, [f.Id(kind, 0)], Ct));
        Assert.Equal(before, await SnapshotAsync(f.Provider));
    }

    [Fact]
    public async Task A_profile_switch_rejects_the_draft_and_writes_neither_file()
    {
        var f = await CreateAsync(); f.Enable(); var read = await f.Choices.ReadAsync(QuotaKind.FinancialAccounts, Ct);
        var path = _directory.Combine("other.db"); var source = new AccessSource(); var other = Provider(path, source);
        var before = await SnapshotAsync(f.Provider); var second = await SnapshotAsync(other);
        f.Provider.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>().MoveTo(path);
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Choices.SaveAsync(read, [f.Accounts[0].Id], Ct));
        f.Provider.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>().MoveTo(f.Path);
        Assert.Equal(before, await SnapshotAsync(f.Provider)); Assert.Equal(second, await SnapshotAsync(other));
    }

    [Fact]
    public async Task Returning_to_the_same_choice_still_retires_an_older_revision()
    {
        var f = await CreateAsync(); f.Enable(); var kind = QuotaKind.FinancialAccounts;
        await f.Choices.SaveAsync(await f.Choices.ReadAsync(kind, Ct), [f.Accounts[0].Id], Ct);
        var old = await f.Choices.ReadAsync(kind, Ct);
        await f.Choices.SaveAsync(await f.Choices.ReadAsync(kind, Ct), [f.Accounts[1].Id], Ct);
        await f.Choices.SaveAsync(await f.Choices.ReadAsync(kind, Ct), [f.Accounts[0].Id], Ct);
        var before = await SnapshotAsync(f.Provider);
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Choices.SaveAsync(old, [f.Accounts[2].Id], Ct));
        Assert.Equal(before, await SnapshotAsync(f.Provider));
    }

    [Theory]
    [InlineData("sql")]
    [InlineData("access")]
    public async Task Failure_after_choice_SQL_rolls_back_without_publishing_Changed(string failure)
    {
        var f = await CreateAsync(); f.Enable(); var read = await f.Choices.ReadAsync(QuotaKind.FinancialAccounts, Ct);
        await using (var db = await f.Provider.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct))
        {
            await db.Database.ExecuteSqlRawAsync(failure == "sql"
                ? "CREATE TRIGGER FailChoice AFTER INSERT ON ResourceSelections BEGIN SELECT RAISE(ABORT, 'Owned failure'); END;"
                : "CREATE TRIGGER RetireChoice AFTER INSERT ON ResourceSelections BEGIN SELECT RetireChoiceAccess(); END;", Ct);
        }
        var before = await SnapshotAsync(f.Provider); var events = 0; f.Choices.Changed += (_, _) => events++;
        if (failure == "access") f.Source.AfterSql = () => f.Source.Current = CommercialWriteAccess.Inactive;
        await Assert.ThrowsAnyAsync<Exception>(() => f.Choices.SaveAsync(read, [f.Accounts[0].Id], Ct));
        Assert.Equal(before, await SnapshotAsync(f.Provider)); Assert.Equal(0, events);
    }

    [Fact]
    public async Task Two_independent_reviewed_writers_cannot_overwrite_each_other()
    {
        var f = await CreateAsync(); f.Enable(); var other = Provider(f.Path, f.Source).GetRequiredService<ResourceChoiceStore>();
        var first = await f.Choices.ReadAsync(QuotaKind.FinancialAccounts, Ct); var second = await other.ReadAsync(QuotaKind.FinancialAccounts, Ct);
        var changed = 0; f.Choices.Changed += (_, _) => Interlocked.Increment(ref changed); other.Changed += (_, _) => Interlocked.Increment(ref changed);
        var results = await Task.WhenAll(TrySave(f.Choices, first, f.Accounts[0].Id), TrySave(other, second, f.Accounts[1].Id));
        Assert.Equal(1, results.Count(result => result)); Assert.Equal(1, changed);
        Assert.Single((await f.Choices.ReadAsync(QuotaKind.FinancialAccounts, Ct)).SelectedIds);
    }

    private static async Task<bool> TrySave(ResourceChoiceStore store, ResourceChoiceSnapshot reviewed, Guid id)
    {
        try { return await store.SaveAsync(reviewed, [id], Ct); }
        catch (InvalidOperationException) { return false; }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Paid_upgrades_ignore_but_do_not_rewrite_the_retained_choice(bool inactive)
    {
        var f = await CreateAsync(); f.Enable(); var kind = QuotaKind.FinancialAccounts;
        await f.Choices.SaveAsync(await f.Choices.ReadAsync(kind, Ct), [f.Accounts[0].Id], Ct);
        var before = await SnapshotAsync(f.Provider);
        if (inactive) f.Source.Current = CommercialWriteAccess.Inactive; else f.Enable(ProductPlan.Plus);
        var read = await f.Choices.ReadAsync(kind, Ct);
        Assert.False(read.CanChoose); Assert.Equal(4, read.SelectedIds.Count); Assert.Equal(before, await SnapshotAsync(f.Provider));
        f.Enable(); Assert.Equal(new[] { f.Accounts[0].Id }, (await f.Choices.ReadAsync(kind, Ct)).SelectedIds);
    }

    [Fact]
    public async Task Scoped_choice_cannot_grant_access_to_a_foreign_or_revoked_shared_membership()
    {
        var f = await CreateAsync(); f.Enable();
        await f.Choices.SaveAsync(await f.Choices.ReadAsync(QuotaKind.FinancialAccounts, Ct), [f.Accounts[0].Id], Ct);
        f.Enable(shared: true); Assert.Equal(4, (await f.Choices.ReadAsync(QuotaKind.FinancialAccounts, Ct)).SelectedIds.Count);
        f.Enable(shared: true, host: false); Assert.Empty((await f.Choices.ReadAsync(QuotaKind.FinancialAccounts, Ct)).SelectedIds);
        f.Enable(shared: true, member: false);
        await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => f.Choices.ReadAsync(QuotaKind.FinancialAccounts, Ct));
        f.Enable(scope: Guid.NewGuid()); Assert.True((await f.Choices.ReadAsync(QuotaKind.FinancialAccounts, Ct)).RequiresSelection);
    }

    [Fact]
    public async Task Stored_account_choices_control_real_money_and_preserve_retained_corrections()
    {
        var f = await CreateAsync(); var original = f.Entry(3); Assert.True((await f.Store.SaveEntryAsync(original, Ct)).Succeeded);
        f.Enable();
        await f.Choices.SaveAsync(await f.Choices.ReadAsync(QuotaKind.FinancialAccounts, Ct), [f.Accounts[0].Id], Ct);
        Assert.True((await f.Store.SaveEntryAsync(f.Entry(0), Ct)).Succeeded);
        var before = await SnapshotAsync(f.Provider);
        await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => f.Store.SaveEntryAsync(f.Entry(3), Ct));
        Assert.Equal(before, await SnapshotAsync(f.Provider));
        original.Note = "Actual retained correction";
        Assert.True((await f.Store.SaveEntryAsync(original, Ct)).Succeeded);
        Assert.Equal(original.Note, (await f.Store.GetEntryAsync(original.Id, Ct))!.Note);
    }

    [Fact]
    public async Task Stored_plan_choice_controls_current_work_and_retires_prepared_reminders()
    {
        var f = await CreateAsync(); f.Enable(); var kind = QuotaKind.RecurringPlans;
        var old = await f.PlanStore.GetWorkSnapshotAsync(Ct);
        await f.Choices.SaveAsync(await f.Choices.ReadAsync(kind, Ct), [f.Plans[0].Id, f.Plans[6].Id], Ct);
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.PlanStore.ValidateWorkSnapshotAsync(old, Ct));
        var read = await f.PlanStore.GetWorkSnapshotAsync(Ct);
        Assert.True(read.Work.CanGenerate(f.Plans[0].Id)); Assert.False(read.Work.CanGenerate(f.Plans[1].Id));
        Assert.Equal(8, read.Schedules.Count); Assert.Equal(2, (await f.Choices.ReadAsync(kind, Ct)).SelectedIds.Count);
        await f.PlanStore.ValidateWorkSnapshotAsync(read, Ct);
        await f.Choices.SaveAsync(await f.Choices.ReadAsync(kind, Ct), [f.Plans[1].Id], Ct);
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.PlanStore.ValidateWorkSnapshotAsync(read, Ct));
    }

    [Fact]
    public async Task Explicit_new_account_joins_a_remaining_chosen_slot_atomically_and_archive_releases_it()
    {
        var f = await CreateAsync(); f.Enable(); var kind = QuotaKind.FinancialAccounts;
        await f.Choices.SaveAsync(await f.Choices.ReadAsync(kind, Ct), [f.Accounts[0].Id], Ct);
        var account = new Account { Name = "Explicit new", CurrencyCode = "EUR", OpeningDate = Day };
        Assert.True(await f.Store.SaveAccountAsync(account, Ct));
        Assert.Contains(account.Id, (await f.Choices.ReadAsync(kind, Ct)).SelectedIds);
        Assert.True((await f.Store.SaveEntryAsync(new() { Kind = EntryKind.Expense, AccountId = account.Id, Amount = 100, Date = Day }, Ct)).Succeeded);
        account.IsArchived = true; Assert.True(await f.Store.SaveAccountAsync(account, Ct));
        Assert.DoesNotContain(account.Id, (await f.Choices.ReadAsync(kind, Ct)).SelectedIds);
        account.IsArchived = false; Assert.True(await f.Store.SaveAccountAsync(account, Ct));
        Assert.Contains(account.Id, (await f.Choices.ReadAsync(kind, Ct)).SelectedIds);
    }

    [Fact]
    public async Task A_full_chosen_account_capacity_rejects_creation_without_row_changes()
    {
        var f = await CreateAsync(); f.Enable();
        await f.Choices.SaveAsync(await f.Choices.ReadAsync(QuotaKind.FinancialAccounts, Ct), f.Accounts.Take(3).Select(account => account.Id), Ct);
        var before = await SnapshotAsync(f.Provider);
        await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => f.Store.SaveAccountAsync(
            new() { Name = "Rejected", CurrencyCode = "EUR", OpeningDate = Day }, Ct));
        Assert.Equal(before, await SnapshotAsync(f.Provider));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task An_explicit_split_transfers_only_the_checked_predecessors_selected_slot(bool selected)
    {
        var f = await CreateAsync(); f.Enable(); var kind = QuotaKind.RecurringPlans;
        await f.Choices.SaveAsync(await f.Choices.ReadAsync(kind, Ct), [f.Plans[selected ? 0 : 1].Id], Ct);
        var previous = (await f.PlanStore.GetScheduleAsync(f.Plans[0].Id, Ct))!;
        var next = PlanActions.SplitFrom(previous, Day.AddMonths(1));
        Assert.True(await f.PlanStore.SaveSplitAsync(previous, next, Ct));
        var read = await f.Choices.ReadAsync(kind, Ct);
        Assert.Equal(selected, read.SelectedIds.Contains(next.Id)); Assert.DoesNotContain(previous.Id, read.SelectedIds);
        Assert.Single(read.SelectedIds); Assert.False(read.RequiresSelection);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task New_plan_batches_use_the_chosen_slots_and_endings_free_the_same_choice()
    {
        var f = await CreateAsync(); f.Enable(); var kind = QuotaKind.RecurringPlans;
        await f.Choices.SaveAsync(await f.Choices.ReadAsync(kind, Ct), [f.Plans[0].Id, f.Plans[6].Id], Ct);
        var plan = new Schedule { Name = "Explicit new plan", AccountId = f.Accounts[0].Id, Rule = new() { Start = Day } };
        await f.PlanStore.SaveScheduleAsync(plan, Ct);
        Assert.Contains(plan.Id, (await f.Choices.ReadAsync(kind, Ct)).SelectedIds);
        plan.State = ScheduleState.Ended; await f.PlanStore.SaveScheduleAsync(plan, Ct);
        Assert.DoesNotContain(plan.Id, (await f.Choices.ReadAsync(kind, Ct)).SelectedIds);
        Assert.False((await f.Choices.ReadAsync(kind, Ct)).RequiresSelection);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task A_full_chosen_plan_capacity_rejects_the_entire_new_batch()
    {
        var f = await CreateAsync(); f.Enable(); var kind = QuotaKind.RecurringPlans;
        await f.Choices.SaveAsync(await f.Choices.ReadAsync(kind, Ct), f.Plans.Take(5).Select(plan => plan.Id), Ct);
        var before = await SnapshotAsync(f.Provider);
        var additions = Enumerable.Range(0, 2).Select(index => new Schedule
        { Name = "Rejected plan " + index, AccountId = f.Accounts[0].Id, Rule = new() { Start = Day } });
        await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => f.PlanStore.SaveSchedulesAsync(additions, Ct));
        Assert.Equal(before, await SnapshotAsync(f.Provider));
    }

    [Fact]
    public async Task A_stored_choice_overrides_only_the_cached_choice_and_never_paid_rights()
    {
        var f = await CreateAsync(); f.Enable(); var kind = QuotaKind.FinancialAccounts;
        f.Source.Current = new(f.Path, f.Source.Current.Context!, accountSelection: new(kind,
            new(QuotaScopeKind.PersonalProfile, ScopeId), [f.Accounts[1].Id]));
        await f.Choices.SaveAsync(await f.Choices.ReadAsync(kind, Ct), [f.Accounts[0].Id], Ct);
        Assert.True((await f.Store.SaveEntryAsync(f.Entry(0), Ct)).Succeeded);
        await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => f.Store.SaveEntryAsync(f.Entry(1), Ct));
        Assert.Equal(ProductPlan.Free, f.Source.Current.Context!.PersonalPlan);
        Assert.Equal(new[] { f.Accounts[1].Id }, f.Source.Current.AccountSelection!.SelectedIds);
    }

    [Fact]
    public async Task An_account_SQL_failure_rolls_back_its_automatic_choice_join()
    {
        var f = await CreateAsync(); f.Enable(); var kind = QuotaKind.FinancialAccounts;
        await f.Choices.SaveAsync(await f.Choices.ReadAsync(kind, Ct), [f.Accounts[0].Id], Ct);
        await using (var db = await f.Provider.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct))
            await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER FailAccount AFTER INSERT ON Accounts BEGIN SELECT RAISE(ABORT, 'Owned failure'); END;", Ct);
        var before = await SnapshotAsync(f.Provider);
        await Assert.ThrowsAsync<DbUpdateException>(() => f.Store.SaveAccountAsync(
            new() { Name = "Rejected new account", CurrencyCode = "EUR", OpeningDate = Day }, Ct));
        Assert.Equal(before, await SnapshotAsync(f.Provider));
    }

    [Theory]
    [InlineData(QuotaKind.FinancialAccounts, false)]
    [InlineData(QuotaKind.FinancialAccounts, true)]
    [InlineData(QuotaKind.RecurringPlans, false)]
    [InlineData(QuotaKind.RecurringPlans, true)]
    public async Task Malformed_stored_choices_block_new_work_but_allow_history_corrections_and_explicit_repair(QuotaKind kind, bool invalidRevision)
    {
        var f = await CreateAsync(); var money = f.Entry(0); Assert.True((await f.Store.SaveEntryAsync(money, Ct)).Succeeded);
        f.Enable(); await f.Choices.SaveAsync(await f.Choices.ReadAsync(kind, Ct), [f.Id(kind, 0)], Ct);
        await using (var db = await f.Provider.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct))
        {
            if (invalidRevision)
                await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE ResourceSelections SET Revision={Guid.Empty}", Ct);
            else await db.Database.ExecuteSqlRawAsync("UPDATE ResourceSelections SET IdentitySet='malformed-owned-choice'", Ct);
        }
        money.Note = "Retained history correction";
        Assert.True((await f.Store.SaveEntryAsync(money, Ct)).Succeeded);
        var read = await f.Choices.ReadAsync(kind, Ct);
        Assert.True(read.RequiresSelection); Assert.Empty(read.SelectedIds);
        if (kind == QuotaKind.FinancialAccounts)
            await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => f.Store.SaveEntryAsync(f.Entry(0), Ct));
        else
        {
            var work = await f.PlanStore.GetWorkSnapshotAsync(Ct);
            Assert.Equal(8, work.Schedules.Count); Assert.False(work.Work.CanGenerate(f.Plans[0].Id));
        }
        Assert.True(await f.Choices.SaveAsync(read, [f.Id(kind, 1)], Ct));
        Assert.False((await f.Choices.ReadAsync(kind, Ct)).RequiresSelection);
        Assert.Equal(money.Note, (await f.Store.GetEntryAsync(money.Id, Ct))!.Note);
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
