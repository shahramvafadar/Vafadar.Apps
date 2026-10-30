using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Testing;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data.Tests.Commerce;

/// <summary>AT-150: actual SQLite definition choices, retained corrections and atomic writer/period boundaries.</summary>
[Trait("AT", "AT-150")]
public sealed class BudgetChoiceStoreTests : IDisposable
{
    private readonly TemporaryDirectory _directory = new();
    private readonly List<ServiceProvider> _providers = [];
    private readonly FixedTime _time = new();
    private static readonly Guid ScopeId = Guid.Parse("11111111-2222-3333-4444-555555555555");
    private static readonly QuotaScope Scope = new(QuotaScopeKind.PersonalProfile, ScopeId);
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Matching_original_budget_definition_choice_is_known_unchanged_without_another_SQL_write_or_event()
    {
        var f = await CreateAsync(Current()); f.Enable(); await f.Choices.SaveAsync(await f.Choices.ReadAsync(Ct), [], Ct);
        var original = await f.Choices.ReadAsync(Ct); var before = await SnapshotAsync(f);
        var receipt = new SelectionWriteReceipt(); var events = 0; f.Choices.Changed += (_, _) => events++;
        Assert.False(await f.Choices.SaveAsync(original, [], Ct, receipt));
        Assert.True(receipt.Accepted); Assert.False(receipt.Changed); Assert.Equal(0, events); Assert.Equal(before, await SnapshotAsync(f));
    }

    [Fact]
    public async Task Actual_budget_choice_receipt_is_retained_before_fallible_notification_and_is_one_request_only()
    {
        var f = await CreateAsync(Current()); f.Enable(); var original = await f.Choices.ReadAsync(Ct);
        var before = await SnapshotAsync(f, false); var receipt = new SelectionWriteReceipt();
        f.Choices.Changed += (_, _) => throw new IOException("Fictitious notification failure after actual budget choice commit.");
        await Assert.ThrowsAsync<IOException>(() => f.Choices.SaveAsync(original, [], Ct, receipt));
        Assert.True(receipt.Accepted); Assert.True(receipt.Changed); var committed = await SnapshotAsync(f);
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Choices.SaveAsync(original, [], Ct, receipt));
        Assert.Equal(committed, await SnapshotAsync(f)); Assert.Equal(before, await SnapshotAsync(f, false));
    }

    [Fact]
    public async Task Native_budget_owner_retired_by_actual_selection_SQL_cannot_commit_or_claim_an_accepted_receipt()
    {
        var f = await CreateAsync(Current()); f.Enable(); var original = await f.Choices.ReadAsync(Ct);
        await SqlAsync(f, "CREATE TRIGGER RetireNativeBudgetChoice AFTER INSERT ON ResourceSelections BEGIN SELECT RetireBudgetNativeOwner(); END;");
        var before = await SnapshotAsync(f); var current = true; var receipt = new SelectionWriteReceipt();
        f.Source.AfterSql = () => current = false;
        await Assert.ThrowsAsync<OperationCanceledException>(() => f.Choices.SaveAsync(original, [], Ct, receipt,
            () => { if (!current) throw new OperationCanceledException("Fictitious original budget owner retired after SQL."); }));
        Assert.False(receipt.Accepted); Assert.False(receipt.Changed); Assert.Equal(before, await SnapshotAsync(f));
    }

    [Fact]
    public async Task Original_public_budget_choice_read_runs_off_its_caller_inside_one_joined_read_transaction()
    {
        var f = await CreateAsync(Current()); f.Enable(); f.Source.ProbeRead = true;
        var caller = await Task.Factory.StartNew(() =>
        {
            var thread = Environment.CurrentManagedThreadId; _ = f.Choices.ReadAsync(Ct).GetAwaiter().GetResult(); return thread;
        }, Ct, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        f.Source.ProbeRead = false;
        Assert.NotEqual(caller, f.Source.ReadThread); Assert.True(f.Source.ReadTransaction);
    }
    private static Budget Current(string currency = "EUR") => new() { CurrencyCode = currency, Year = 2026, Month = 10,
        TotalLimit = 12345, AccountIds = [Guid.Parse("11111111-1111-1111-1111-111111111111")],
        CategoryLimits = [new() { CategoryId = Guid.Parse("22222222-2222-2222-2222-222222222222"), Limit = 2345 }] };
    private ServiceProvider Provider(string path, AccessSource source)
    {
        var services = new ServiceCollection().AddSingleton<TimeProvider>(_time)
            .AddSingleton<ICommercialWriteAccessSource>(source).AddZananceData(path);
        var registration = services.Single(item => item.ServiceType == typeof(IDbContextFactory<ZananceDbContext>));
        services.Remove(registration);
        services.AddSingleton<IDbContextFactory<ZananceDbContext>>(p => new ObservedBudgetContexts(
            (IDbContextFactory<ZananceDbContext>)ActivatorUtilities.CreateInstance(p, registration.ImplementationType!), source));
        var provider = services.BuildServiceProvider();
        _providers.Add(provider); provider.MigrateLocalDatabase<ZananceDbContext>(); return provider;
    }
    private async Task<Fixture> CreateAsync(params Budget[] budgets)
    {
        var path = _directory.Combine(Guid.NewGuid() + ".db"); var source = new AccessSource(); var provider = Provider(path, source);
        await using var db = await provider.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        db.Budgets.AddRange(budgets); await db.SaveChangesAsync(Ct);
        return new(path, provider, source);
    }
    private sealed record Fixture(string Path, ServiceProvider Provider, AccessSource Source)
    {
        public BudgetChoiceStore Choices => Provider.GetRequiredService<BudgetChoiceStore>();
        public ZananceStore Store => Provider.GetRequiredService<ZananceStore>();
        public void Enable(ProductPlan product = ProductPlan.Free, Guid? scopeId = null, BudgetSelection? choice = null) => Source.Current =
            new(Path, new(product, new(EntitlementScopeKind.PersonalProfile, scopeId ?? ScopeId)), budgetSelection: choice);
        public async Task Choose(params Budget[] budgets) => await Choices.SaveAsync(await Choices.ReadAsync(Ct), budgets.Select(b => BudgetDefinitionKey.From(Scope, b)), Ct);
    }
    private static async Task SqlAsync(Fixture f, string sql)
    {
        await using var db = await f.Provider.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        await db.Database.ExecuteSqlRawAsync(sql, Ct);
    }
    private static async Task<string> SnapshotAsync(Fixture f, bool includeChoices = true)
    {
        await using var db = await f.Provider.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        await db.Database.OpenConnectionAsync(Ct); var connection = db.Database.GetDbConnection(); var tables = new List<string>();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name";
            using var reader = await command.ExecuteReaderAsync(Ct);
            while (await reader.ReadAsync(Ct)) if (includeChoices || reader.GetString(0) != "ResourceSelections") tables.Add(reader.GetString(0));
        }
        var result = new Dictionary<string, List<string>>();
        foreach (var table in tables)
        {
            using var command = connection.CreateCommand(); command.CommandText = "SELECT * FROM \"" + table + "\"";
            using var reader = await command.ExecuteReaderAsync(Ct); var rows = new List<string>();
            while (await reader.ReadAsync(Ct))
            {
                var values = new object?[reader.FieldCount];
                for (var i = 0; i < values.Length; i++) values[i] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                rows.Add(JsonSerializer.Serialize(values));
            }
            rows.Sort(StringComparer.Ordinal); result[table] = rows;
        }
        return JsonSerializer.Serialize(result);
    }

    [Fact]
    public async Task Inactive_read_keeps_complete_original_periods_without_settings_or_choice_writes()
    {
        var first = Current(); var second = Current("USD"); var f = await CreateAsync(first, second);
        var before = await SnapshotAsync(f); var read = await f.Choices.ReadAsync(Ct);
        Assert.Equal(2, read.Items.Count); Assert.False(read.CanChoose); Assert.False(read.RequiresSelection);
        Assert.True(read.Work.CanUse(first.Id)); Assert.True(read.Work.CanUse(second.Id));
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Choices.SaveAsync(read, [], Ct));
        Assert.Equal(before, await SnapshotAsync(f));
    }

    [Fact]
    public async Task Over_capacity_originals_require_explicit_choice_and_durable_save_changes_only_choice_rows()
    {
        var first = Current(); var second = Current("USD"); var f = await CreateAsync(first, second); f.Enable();
        var before = await SnapshotAsync(f, false); var read = await f.Choices.ReadAsync(Ct);
        Assert.True(read.RequiresSelection); Assert.False(read.Work.CanUse(first.Id)); Assert.True(read.Work.CanCorrect);
        var events = 0; f.Choices.Changed += (_, _) => events++; await f.Choose(second);
        Assert.Equal(1, events); Assert.Equal(before, await SnapshotAsync(f, false));
        var independent = Provider(f.Path, f.Source).GetRequiredService<BudgetChoiceStore>(); var reopened = await independent.ReadAsync(Ct);
        Assert.False(reopened.RequiresSelection); Assert.False(reopened.Work.CanUse(first.Id)); Assert.True(reopened.Work.CanUse(second.Id));
        Assert.False(await independent.SaveAsync(reopened, reopened.SelectedDefinitions, Ct));
        Assert.Equal(1, events); Assert.Equal(before, await SnapshotAsync(f, false));
    }

    [Fact]
    public async Task Explicit_empty_choice_allows_one_new_definition_above_retained_original_count()
    {
        var first = Current(); var second = Current("USD"); var f = await CreateAsync(first, second); f.Enable();
        await f.Choose(); var added = Current("GBP"); await f.Store.SaveBudgetAsync(added, Ct);
        var read = await f.Choices.ReadAsync(Ct); Assert.True(read.Work.CanUse(added.Id)); Assert.False(read.Work.CanUse(first.Id));
        Assert.Equal(BudgetDefinitionKey.From(Scope, added), Assert.Single(read.SelectedDefinitions));
        var before = await SnapshotAsync(f);
        await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => f.Store.SaveBudgetAsync(Current("CHF"), Ct));
        Assert.Equal(before, await SnapshotAsync(f));
    }

    [Fact]
    public async Task Unselected_paid_budget_corrections_stay_available_but_new_paid_copies_remain_blocked()
    {
        var paid = Current(); paid.Method = BudgetMethod.Flex; var other = Current("USD"); var f = await CreateAsync(paid, other); f.Enable();
        await f.Choose(other); paid.TotalLimit = 45678; await f.Store.SaveBudgetAsync(paid, Ct);
        Assert.False((await f.Choices.ReadAsync(Ct)).Work.CanUse(paid.Id));
        Assert.Equal(45678, (await f.Store.GetBudgetsAsync(Ct)).Single(b => b.Id == paid.Id).TotalLimit);
        var next = Current(); next.Month = 11; next.Method = BudgetMethod.Flex; var before = await SnapshotAsync(f);
        await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => f.Store.SaveBudgetAsync(next, Ct));
        Assert.Equal(before, await SnapshotAsync(f));
    }

    [Fact]
    public async Task Changing_selected_account_scope_transfers_the_only_slot_without_rewriting_other_periods()
    {
        var original = Current(); var other = Current("USD"); var f = await CreateAsync(original, other); f.Enable(); await f.Choose(original);
        original.AccountIds = []; await f.Store.SaveBudgetAsync(original, Ct);
        Assert.Equal(BudgetDefinitionKey.From(Scope, original), Assert.Single((await f.Choices.ReadAsync(Ct)).SelectedDefinitions));
        Assert.Equal(other.TotalLimit, (await f.Store.GetBudgetsAsync(Ct)).Single(b => b.Id == other.Id).TotalLimit);
    }

    [Fact]
    public async Task A_second_current_row_of_the_old_definition_prevents_silent_slot_transfer()
    {
        var original = Current(); var same = Current(); same.Calendar = PeriodCalendar.Persian;
        (same.Year, same.Month) = PeriodMath.MonthOf(new(2026, 10, 12), same.Calendar);
        var f = await CreateAsync(original, same); f.Enable(); await f.Choose(original); original.AccountIds = [];
        var before = await SnapshotAsync(f);
        await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => f.Store.SaveBudgetAsync(original, Ct));
        Assert.Equal(before, await SnapshotAsync(f));
    }

    [Fact]
    public async Task A_failed_limit_write_rolls_back_its_new_definition_choice_and_all_original_rows()
    {
        var original = Current(); var f = await CreateAsync(original); f.Enable(); await f.Choose();
        await SqlAsync(f, "CREATE TRIGGER reject_budget_limit BEFORE INSERT ON BudgetCategoryLimits BEGIN SELECT RAISE(ABORT,'owned fixture'); END;");
        var before = await SnapshotAsync(f); var events = 0; f.Store.Changed += (_, _) => events++;
        await Assert.ThrowsAsync<DbUpdateException>(() => f.Store.SaveBudgetAsync(Current("USD"), Ct));
        Assert.Equal(before, await SnapshotAsync(f)); Assert.Equal(0, events);
    }

    [Fact]
    public async Task Deleting_the_last_current_definition_releases_only_its_choice_and_retains_financial_rows()
    {
        var current = Current(); var past = Current(); past.Month = 9; var other = Current("USD");
        var f = await CreateAsync(current, past, other); f.Enable(); await f.Choose(current);
        await f.Store.DeleteBudgetAsync(past.Id, Ct);
        Assert.Single((await f.Choices.ReadAsync(Ct)).SelectedDefinitions);
        await f.Store.DeleteBudgetAsync(current.Id, Ct);
        Assert.Empty((await f.Choices.ReadAsync(Ct)).SelectedDefinitions); Assert.Single(await f.Store.GetBudgetsAsync(Ct));
        await f.Store.SaveBudgetAsync(Current("CHF"), Ct); Assert.Equal(2, (await f.Store.GetBudgetsAsync(Ct)).Count);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task Copied_definition_stays_selected_when_the_financial_month_advances_and_reviewed_save_expires()
    {
        var current = Current(); var next = Current(); next.Month = 11; var other = Current("USD"); other.Month = 11;
        var f = await CreateAsync(current, next, other); f.Enable(); await f.Choose(current); var reviewed = await f.Choices.ReadAsync(Ct);
        _time.Now = new(2026, 11, 12, 12, 0, 0, TimeSpan.Zero); var before = await SnapshotAsync(f);
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Choices.SaveAsync(reviewed, reviewed.SelectedDefinitions, Ct));
        Assert.Equal(before, await SnapshotAsync(f)); var read = await f.Choices.ReadAsync(Ct);
        Assert.True(read.Work.CanUse(next.Id)); Assert.False(read.Work.CanUse(current.Id)); Assert.False(read.Work.CanUse(other.Id));
    }

    [Fact]
    public async Task Equal_audit_time_financial_edits_and_choice_ABA_reject_reviewed_overwrites()
    {
        var a = Current(); var b = Current("USD"); var f = await CreateAsync(a, b); f.Enable(); await f.Choose(a);
        var stale = await f.Choices.ReadAsync(Ct); await f.Choose(b); await f.Choose(a); var before = await SnapshotAsync(f);
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Choices.SaveAsync(stale, [], Ct)); Assert.Equal(before, await SnapshotAsync(f));
        stale = await f.Choices.ReadAsync(Ct); await SqlAsync(f, "UPDATE Budgets SET TotalLimit=45678;"); before = await SnapshotAsync(f);
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Choices.SaveAsync(stale, [], Ct)); Assert.Equal(before, await SnapshotAsync(f));
    }

    [Fact]
    public async Task Malformed_choices_block_new_work_keep_corrections_and_require_explicit_repair()
    {
        var original = Current(); var f = await CreateAsync(original); f.Enable(); await f.Choose(original);
        await SqlAsync(f, "UPDATE ResourceSelections SET IdentitySet='EUR|999|';"); var read = await f.Choices.ReadAsync(Ct);
        Assert.True(read.RequiresSelection); Assert.False(read.Work.CanUse(original.Id));
        original.TotalLimit = 98765; await f.Store.SaveBudgetAsync(original, Ct); var before = await SnapshotAsync(f);
        await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => f.Store.SaveBudgetAsync(Current("USD"), Ct));
        Assert.Equal(before, await SnapshotAsync(f)); await f.Choose(original);
        Assert.True((await f.Choices.ReadAsync(Ct)).Work.CanUse(original.Id));
    }

    [Fact]
    public async Task Foreign_file_or_retired_paid_scope_never_accepts_an_old_review()
    {
        var original = Current(); var f = await CreateAsync(original); f.Enable(); var read = await f.Choices.ReadAsync(Ct);
        var foreign = await CreateAsync(Current()); foreign.Enable(); var before = await SnapshotAsync(f); var foreignBefore = await SnapshotAsync(foreign);
        await Assert.ThrowsAsync<InvalidOperationException>(() => foreign.Choices.SaveAsync(read, [], Ct));
        f.Enable(scopeId: Guid.NewGuid()); await Assert.ThrowsAsync<InvalidOperationException>(() => f.Choices.SaveAsync(read, [], Ct));
        Assert.Equal(before, await SnapshotAsync(f)); Assert.Equal(foreignBefore, await SnapshotAsync(foreign));
    }

    [Fact]
    public async Task Cached_choice_is_scoped_but_a_persisted_choice_overrides_it_without_granting_paid_tools()
    {
        var a = Current(); var b = Current("USD"); var f = await CreateAsync(a, b);
        f.Enable(choice: new(Scope, [BudgetDefinitionKey.From(Scope, b)])); Assert.True((await f.Choices.ReadAsync(Ct)).Work.CanUse(b.Id));
        await f.Choose(a); var read = await f.Choices.ReadAsync(Ct); Assert.True(read.Work.CanUse(a.Id)); Assert.False(read.Work.CanUse(b.Id));
        Assert.Throws<ArgumentException>(() => new CommercialWriteAccess(f.Path,
            new(ProductPlan.Free, new(EntitlementScopeKind.PersonalProfile, Guid.NewGuid())), budgetSelection: new(Scope, [])));
    }

    [Fact]
    public async Task Independent_reviewed_choices_compete_for_one_revision_without_financial_writes()
    {
        var a = Current(); var b = Current("USD"); var f = await CreateAsync(a, b); f.Enable();
        var second = Provider(f.Path, f.Source).GetRequiredService<BudgetChoiceStore>(); var firstRead = await f.Choices.ReadAsync(Ct); var secondRead = await second.ReadAsync(Ct);
        var before = await SnapshotAsync(f, false);
        async Task<Exception?> Save(BudgetChoiceStore store, BudgetChoiceSnapshot read, Budget budget)
        { try { await store.SaveAsync(read, [BudgetDefinitionKey.From(Scope, budget)], Ct); return null; } catch (Exception e) { return e; } }
        var results = await Task.WhenAll(Task.Run(() => Save(f.Choices, firstRead, a), Ct), Task.Run(() => Save(second, secondRead, b), Ct));
        Assert.Single(results, r => r is null); Assert.IsType<InvalidOperationException>(Assert.Single(results, r => r is not null));
        Assert.Equal(before, await SnapshotAsync(f, false)); Assert.Single((await f.Choices.ReadAsync(Ct)).SelectedDefinitions);
    }

    [Fact]
    public async Task Independent_creations_share_the_only_explicitly_empty_definition_slot()
    {
        var f = await CreateAsync(Current(), Current("USD")); f.Enable(); await f.Choose();
        var second = Provider(f.Path, f.Source).GetRequiredService<ZananceStore>(); using var barrier = new Barrier(2);
        f.Source.Barrier = barrier; f.Source.Captures = 0;
        async Task<Exception?> Save(ZananceStore store, string currency)
        { try { await store.SaveBudgetAsync(Current(currency), Ct); return null; } catch (Exception e) { return e; } }
        var results = await Task.WhenAll(Task.Run(() => Save(f.Store, "GBP"), Ct), Task.Run(() => Save(second, "CHF"), Ct));
        f.Source.Barrier = null;
        Assert.Single(results, r => r is null); Assert.IsType<CommercialWriteRejectedException>(Assert.Single(results, r => r is not null));
        Assert.Equal(3, (await f.Store.GetBudgetsAsync(Ct)).Count); Assert.Single((await f.Choices.ReadAsync(Ct)).SelectedDefinitions);
    }

    [Fact]
    public async Task Changed_financial_month_start_expires_the_review_without_saving_a_choice()
    {
        var f = await CreateAsync(Current()); f.Enable(); var read = await f.Choices.ReadAsync(Ct);
        await f.Store.UpdateSettingsAsync(settings => settings.MonthStartDay = 25, Ct); var before = await SnapshotAsync(f);
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Choices.SaveAsync(read, [], Ct));
        Assert.Equal(before, await SnapshotAsync(f));
        var newRead = await f.Choices.ReadAsync(Ct); Assert.Equal(25, newRead.MonthStartDay);
        Assert.Empty(newRead.SelectedDefinitions); Assert.False(newRead.Work.CanUse(read.Items[0].Id));
    }

    [Fact]
    public async Task A_failed_choice_SQL_write_preserves_the_review_and_complete_financial_data_for_retry()
    {
        var f = await CreateAsync(Current(), Current("USD")); f.Enable(); var read = await f.Choices.ReadAsync(Ct);
        await SqlAsync(f, "CREATE TRIGGER reject_budget_choice BEFORE INSERT ON ResourceSelections BEGIN SELECT RAISE(ABORT,'owned choice fixture'); END;");
        var before = await SnapshotAsync(f); var events = 0; f.Choices.Changed += (_, _) => events++;
        await Assert.ThrowsAsync<DbUpdateException>(() => f.Choices.SaveAsync(read, [], Ct));
        Assert.Equal(before, await SnapshotAsync(f)); Assert.Equal(0, events);
        await SqlAsync(f, "DROP TRIGGER reject_budget_choice;"); Assert.True(await f.Choices.SaveAsync(read, [], Ct));
        Assert.Equal(1, events); Assert.Empty((await f.Choices.ReadAsync(Ct)).SelectedDefinitions);
    }

    [Fact]
    public async Task Changing_a_choice_value_without_its_revision_still_rejects_a_reviewed_overwrite()
    {
        var original = Current(); var f = await CreateAsync(original); f.Enable(); await f.Choose(original);
        var read = await f.Choices.ReadAsync(Ct); await SqlAsync(f, "UPDATE ResourceSelections SET IdentitySet='';");
        var before = await SnapshotAsync(f);
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Choices.SaveAsync(read, [], Ct));
        Assert.Equal(before, await SnapshotAsync(f));
    }

    [Fact]
    public async Task Personal_Pro_never_allows_budget_deletion_without_exact_shared_membership()
    {
        var original = Current(); var f = await CreateAsync(original);
        f.Source.Current = new(f.Path, new(ProductPlan.Pro, new(EntitlementScopeKind.SharedSpace, ScopeId), new(ScopeId, false, true)));
        var before = await SnapshotAsync(f);
        var rejected = await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => f.Store.DeleteBudgetAsync(original.Id, Ct));
        Assert.Equal(FeaturePermission.RequiresMembership, rejected.Permission); Assert.Equal(before, await SnapshotAsync(f));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var provider in _providers) provider.Dispose();
        SqliteTestPools.Clear(_directory); _directory.Dispose();
    }
    private sealed class AccessSource : ICommercialWriteAccessSource
    {
        public CommercialWriteAccess Current = CommercialWriteAccess.Inactive;
        public Barrier? Barrier;
        public int Captures;
        internal bool ProbeRead, ReadTransaction;
        internal int ReadThread;
        internal Action? AfterSql;
        internal WeakReference<ZananceDbContext>? ReadContext;
        public CommercialWriteAccess Capture(string databasePath)
        {
            if (ProbeRead && ReadContext?.TryGetTarget(out var db) == true)
            { ReadThread = Environment.CurrentManagedThreadId; ReadTransaction = db.Database.CurrentTransaction is not null; }
            if (Barrier is not null && Interlocked.Increment(ref Captures) <= 2
                && !Barrier.SignalAndWait(TimeSpan.FromSeconds(15), Ct))
                throw new TimeoutException("The independent owned writers did not rendezvous.");
            return Current;
        }
    }
    /// <summary>Observes the actual fixture context/SQLite writer without substituting choice reads or commits.</summary>
    private sealed class ObservedBudgetContexts(IDbContextFactory<ZananceDbContext> inner, AccessSource source) : IDbContextFactory<ZananceDbContext>
    {
        public ZananceDbContext CreateDbContext()
        {
            var db = inner.CreateDbContext(); source.ReadContext = new(db);
            ((SqliteConnection)db.Database.GetDbConnection()).CreateFunction("RetireBudgetNativeOwner", () => { source.AfterSql?.Invoke(); return 1; });
            return db;
        }
    }
    private sealed class FixedTime : TimeProvider
    {
        public DateTimeOffset Now = new(2026, 10, 12, 12, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }
}
