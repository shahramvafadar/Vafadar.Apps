using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Testing;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data.Tests.Commerce;

/// <summary>AT-123: current-period definitions, stored update identity and atomic budget/limit replacements.</summary>
[Trait("AT", "AT-123")]
public sealed class BudgetWritePolicyTests : IDisposable
{
    private readonly TemporaryDirectory _directory = new();
    private readonly List<ServiceProvider> _providers = [];
    private readonly FixedTime _time = new();
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static readonly Guid ScopeId = Guid.Parse("11111111-2222-3333-4444-555555555555");
    private static readonly Guid AccountA = Guid.Parse("66666666-2222-3333-4444-555555555555");
    private static readonly Guid AccountB = Guid.Parse("77777777-2222-3333-4444-555555555555");

    private static CapabilityContext Context(ProductPlan plan, bool shared = false, bool member = true, bool host = true) =>
        new(plan, new(shared ? EntitlementScopeKind.SharedSpace : EntitlementScopeKind.PersonalProfile, ScopeId),
            shared ? new(ScopeId, member, host) : null);

    private ServiceProvider Provider(string path, ICommercialWriteAccessSource? source = null)
    {
        var services = new ServiceCollection().AddSingleton<TimeProvider>(_time);
        if (source is not null) services.AddSingleton(source);
        var provider = services.AddZananceData(path).BuildServiceProvider();
        provider.MigrateLocalDatabase<ZananceDbContext>(); _providers.Add(provider);
        return provider;
    }

    private Budget Current(string currency = "EUR", PeriodCalendar calendar = PeriodCalendar.Gregorian, int startDay = 1)
    {
        var today = DateOnly.FromDateTime(_time.GetLocalNow().DateTime);
        var (year, month) = PeriodMath.MonthOf(today, calendar, startDay);
        return new() { Year = year, Month = month, Calendar = calendar, CurrencyCode = currency, TotalLimit = 12345,
            AccountIds = [AccountA, AccountB], CategoryLimits = [new() { CategoryId = Guid.NewGuid(), Limit = 2345 }] };
    }

    private static async Task SeedAsync(ServiceProvider provider, params Budget[] budgets)
    {
        await using var db = await provider.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        db.Budgets.AddRange(budgets); await db.SaveChangesAsync(Ct);
    }

    private static async Task<string> SnapshotAsync(ServiceProvider provider)
    {
        await using var db = await provider.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        await db.Database.OpenConnectionAsync(Ct); var connection = db.Database.GetDbConnection(); var tables = new List<string>();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name";
            using var reader = await command.ExecuteReaderAsync(Ct);
            while (await reader.ReadAsync(Ct)) tables.Add(reader.GetString(0));
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

    public void Dispose()
    {
        foreach (var provider in _providers) provider.Dispose();
        SqliteConnection.ClearAllPools(); _directory.Dispose();
    }

    [Fact]
    public async Task Inactive_registration_saves_two_current_definitions_without_paid_context()
    {
        var store = Provider(_directory.Combine("inactive.db")).GetRequiredService<ZananceStore>();
        await store.SaveBudgetAsync(Current(), Ct); await store.SaveBudgetAsync(Current("USD"), Ct);
        Assert.Equal(2, (await store.GetBudgetsAsync(Ct)).Count);
        Assert.Empty(await store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task New_second_current_definition_is_rejected_before_audit_limits_or_changed_events()
    {
        var path = _directory.Combine("free.db"); var provider = Provider(path, new AccessSource(path, Context(ProductPlan.Free)));
        var store = provider.GetRequiredService<ZananceStore>(); await store.SaveBudgetAsync(Current(), Ct);
        var before = await SnapshotAsync(provider); var events = 0; store.Changed += (_, _) => events++;
        var target = Current("USD");
        var rejected = await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => store.SaveBudgetAsync(target, Ct));
        Assert.Equal(QuotaKind.BudgetDefinitions, rejected.Quota); Assert.Equal(1, rejected.Current);
        Assert.Equal(1, rejected.Maximum); Assert.Equal(default, target.CreatedAt);
        Assert.Equal(before, await SnapshotAsync(provider)); Assert.Equal(0, events);
    }

    [Theory]
    [InlineData(PeriodCalendar.Persian)]
    [InlineData(PeriodCalendar.Hijri)]
    public async Task Canonical_accounts_currency_and_current_calendar_rows_share_one_definition(PeriodCalendar calendar)
    {
        var path = _directory.Combine("canonical.db"); var provider = Provider(path, new AccessSource(path, Context(ProductPlan.Free)));
        var store = provider.GetRequiredService<ZananceStore>(); await store.SaveBudgetAsync(Current(), Ct);
        var same = Current("eur", calendar); same.AccountIds = [AccountB, AccountA, AccountA];
        await store.SaveBudgetAsync(same, Ct);
        Assert.Equal(2, (await store.GetBudgetsAsync(Ct)).Count);
        var rejected = await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => store.SaveBudgetAsync(Current("GBP"), Ct));
        Assert.Equal(1, rejected.Current);
    }

    [Fact]
    public async Task Past_definitions_and_future_copies_do_not_consume_current_period_capacity()
    {
        var path = _directory.Combine("history.db"); var provider = Provider(path, new AccessSource(path, Context(ProductPlan.Free)));
        var pastA = Current(); (pastA.Year, pastA.Month) = PeriodMath.Previous(pastA.Year, pastA.Month);
        var pastB = Current("USD"); (pastB.Year, pastB.Month) = PeriodMath.Previous(pastB.Year, pastB.Month);
        await SeedAsync(provider, pastA, pastB);
        var store = provider.GetRequiredService<ZananceStore>(); await store.SaveBudgetAsync(Current(), Ct);
        var next = Current(); (next.Year, next.Month) = PeriodMath.Next(next.Year, next.Month);
        await store.SaveBudgetAsync(next, Ct);
        Assert.Equal(4, (await store.GetBudgetsAsync(Ct)).Count);
        var rejected = await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => store.SaveBudgetAsync(Current("GBP"), Ct));
        Assert.Equal(1, rejected.Current);
    }

    [Theory]
    [InlineData(PeriodCalendar.Gregorian)]
    [InlineData(PeriodCalendar.Persian)]
    [InlineData(PeriodCalendar.Hijri)]
    public async Task Financial_month_start_and_rule_calendar_determine_actual_current_rows(PeriodCalendar calendar)
    {
        var parts = PeriodMath.MonthOf(DateOnly.FromDateTime(_time.Now.DateTime), calendar);
        var day = PeriodMath.MonthRange(parts.Year, parts.Month, calendar).First.AddDays(9);
        _time.Now = new(day.ToDateTime(new(12, 0)), TimeSpan.Zero);
        var path = _directory.Combine("financial.db"); var provider = Provider(path, new AccessSource(path, Context(ProductPlan.Free)));
        var store = provider.GetRequiredService<ZananceStore>(); await store.UpdateSettingsAsync(s => s.MonthStartDay = 25, Ct);
        await store.SaveBudgetAsync(Current("EUR", calendar, 25), Ct);
        var next = Current("USD", calendar, 25); (next.Year, next.Month) = PeriodMath.Next(next.Year, next.Month);
        await store.SaveBudgetAsync(next, Ct);
        var rejected = await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => store.SaveBudgetAsync(Current("GBP", calendar, 25), Ct));
        Assert.Equal(1, rejected.Current);
    }

    [Fact]
    public async Task Ignored_incoming_period_currency_and_calendar_cannot_bypass_a_stored_current_scope_change()
    {
        var path = _directory.Combine("identity.db"); var provider = Provider(path, new AccessSource(path, Context(ProductPlan.Free)));
        var store = provider.GetRequiredService<ZananceStore>(); var original = Current();
        await store.SaveBudgetAsync(original, Ct); await store.SaveBudgetAsync(Current("EUR", PeriodCalendar.Persian), Ct);
        var before = await SnapshotAsync(provider);
        original.Year = 1900; original.Month = 1; original.Calendar = PeriodCalendar.Hijri; original.CurrencyCode = "USD";
        original.AccountIds = [AccountA]; original.CategoryLimits.Clear();
        var rejected = await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => store.SaveBudgetAsync(original, Ct));
        Assert.Equal(QuotaKind.BudgetDefinitions, rejected.Quota);
        Assert.Equal(before, await SnapshotAsync(provider));
    }

    [Fact]
    public async Task Retained_over_quota_corrections_and_same_definition_replacement_preserve_other_complete_rows()
    {
        var path = _directory.Combine("edit.db"); var provider = Provider(path, new AccessSource(path, Context(ProductPlan.Free)));
        var first = Current(); var other = Current("USD"); await SeedAsync(provider, first, other);
        var store = provider.GetRequiredService<ZananceStore>();
        var retained = JsonSerializer.Serialize((await store.GetBudgetsAsync(Ct)).Single(b => b.Id == other.Id));
        var created = first.CreatedAt; first.TotalLimit = 6789; first.CategoryLimits[0].Limit = 4567;
        await store.SaveBudgetAsync(first, Ct);
        Assert.Equal(created, (await store.GetBudgetsAsync(Ct)).Single(b => b.Id == first.Id).CreatedAt);
        var replacement = Current(); replacement.TotalLimit = 9999; await store.ReplaceBudgetAsync(first.Id, replacement, Ct);
        var after = await store.GetBudgetsAsync(Ct); Assert.Equal(2, after.Count);
        Assert.Equal(retained, JsonSerializer.Serialize(after.Single(b => b.Id == other.Id)));
        Assert.DoesNotContain(after, b => b.Id == first.Id);
    }

    [Theory]
    [InlineData(ProductPlan.Plus, false)]
    [InlineData(ProductPlan.Pro, false)]
    [InlineData(ProductPlan.Free, true)]
    public async Task Paid_or_exact_active_shared_contexts_allow_multiple_current_definitions(ProductPlan plan, bool shared)
    {
        var path = _directory.Combine("paid.db"); var provider = Provider(path, new AccessSource(path, Context(plan, shared)));
        var store = provider.GetRequiredService<ZananceStore>();
        await store.SaveBudgetAsync(Current(), Ct); await store.SaveBudgetAsync(Current("USD"), Ct);
        Assert.Equal(2, (await store.GetBudgetsAsync(Ct)).Count);
    }

    [Theory]
    [InlineData("envelopes")]
    [InlineData("flex")]
    [InlineData("surplus")]
    [InlineData("deficit")]
    [InlineData("week")]
    [InlineData("fortnight")]
    public async Task New_paid_budget_methods_rollover_and_periods_are_rejected_before_storage(string tool)
    {
        var path = _directory.Combine("advanced.db"); var provider = Provider(path, new AccessSource(path, Context(ProductPlan.Free)));
        var target = Current();
        switch (tool)
        {
            case "envelopes": target.Method = BudgetMethod.Envelopes; break;
            case "flex": target.Method = BudgetMethod.Flex; break;
            case "surplus": target.Rollover = BudgetRollover.Surplus; break;
            case "deficit": target.Rollover = BudgetRollover.SurplusAndDeficit; break;
            case "week": target.Period = BudgetPeriod.Week; target.PeriodStart = DateOnly.FromDateTime(_time.Now.DateTime); break;
            case "fortnight": target.Period = BudgetPeriod.TwoWeeks; target.PeriodStart = DateOnly.FromDateTime(_time.Now.DateTime); break;
        }
        var before = await SnapshotAsync(provider);
        var rejected = await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => provider.GetRequiredService<ZananceStore>().SaveBudgetAsync(target, Ct));
        Assert.Equal(CommercialFeature.AdvancedBudgets, rejected.Feature); Assert.Equal(FeaturePermission.RequiresPlus, rejected.Permission);
        Assert.Equal(before, await SnapshotAsync(provider));
    }

    [Fact]
    public async Task Retained_paid_method_and_historical_metadata_corrections_remain_available_without_new_work()
    {
        var path = _directory.Combine("retained.db"); var provider = Provider(path, new AccessSource(path, Context(ProductPlan.Free)));
        var current = Current(); current.Method = BudgetMethod.Envelopes;
        var past = Current(); (past.Year, past.Month) = PeriodMath.Previous(past.Year, past.Month);
        await SeedAsync(provider, current, past); var store = provider.GetRequiredService<ZananceStore>();
        current.TotalLimit = 6789; await store.SaveBudgetAsync(current, Ct);
        past.Method = BudgetMethod.Flex; past.Rollover = BudgetRollover.Surplus; await store.SaveBudgetAsync(past, Ct);
        Assert.Equal(BudgetMethod.Flex, (await store.GetBudgetsAsync(Ct)).Single(b => b.Id == past.Id).Method);
        var before = await SnapshotAsync(provider); current.Rollover = BudgetRollover.Surplus;
        await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => store.SaveBudgetAsync(current, Ct));
        Assert.Equal(before, await SnapshotAsync(provider));
    }

    [Fact]
    public async Task Replacement_quota_rejection_happens_before_old_budget_or_category_limits_are_deleted()
    {
        var path = _directory.Combine("replace-quota.db"); var provider = Provider(path, new AccessSource(path, Context(ProductPlan.Free)));
        var current = Current(); var past = Current("GBP"); (past.Year, past.Month) = PeriodMath.Previous(past.Year, past.Month);
        await SeedAsync(provider, current, past); var store = provider.GetRequiredService<ZananceStore>();
        var before = await SnapshotAsync(provider); var events = 0; store.Changed += (_, _) => events++;
        await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => store.ReplaceBudgetAsync(past.Id, Current("USD"), Ct));
        Assert.Equal(before, await SnapshotAsync(provider)); Assert.Equal(0, events);
    }

    [Fact]
    public async Task Replacement_failure_after_old_delete_recovers_complete_budget_limits_and_allows_retry()
    {
        var path = _directory.Combine("replace-failure.db"); var provider = Provider(path, new AccessSource(path, Context(ProductPlan.Free)));
        var store = provider.GetRequiredService<ZananceStore>(); var original = Current(); await store.SaveBudgetAsync(original, Ct);
        await using var db = await provider.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_limits BEFORE INSERT ON BudgetCategoryLimits BEGIN SELECT RAISE(ABORT, 'fixture failure'); END;", Ct);
        var before = await SnapshotAsync(provider); var events = 0; store.Changed += (_, _) => events++;
        var replacement = Current(); replacement.TotalLimit = 9876;
        await Assert.ThrowsAsync<DbUpdateException>(() => store.ReplaceBudgetAsync(original.Id, replacement, Ct));
        Assert.Equal(before, await SnapshotAsync(provider)); Assert.Equal(0, events);
        await db.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_limits;", Ct);
        await store.ReplaceBudgetAsync(original.Id, replacement, Ct);
        Assert.Equal(1, events); Assert.Equal(replacement.Id, Assert.Single(await store.GetBudgetsAsync(Ct)).Id);
    }

    [Fact]
    public async Task Missing_membership_rejects_existing_corrections_even_with_personal_Pro()
    {
        var path = _directory.Combine("member.db"); var provider = Provider(path, new AccessSource(path, Context(ProductPlan.Pro, true, member: false)));
        var original = Current(); await SeedAsync(provider, original); var before = await SnapshotAsync(provider);
        var rejected = await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => provider.GetRequiredService<ZananceStore>().SaveBudgetAsync(original, Ct));
        Assert.Equal(FeaturePermission.RequiresMembership, rejected.Permission); Assert.Equal(before, await SnapshotAsync(provider));
    }

    [Fact]
    public async Task Expired_host_keeps_current_corrections_and_slot_neutral_replacement_but_blocks_new_definitions()
    {
        var path = _directory.Combine("expired.db"); var provider = Provider(path, new AccessSource(path, Context(ProductPlan.Pro, true, host: false)));
        var original = Current(); await SeedAsync(provider, original); var store = provider.GetRequiredService<ZananceStore>();
        original.TotalLimit = 5678; await store.SaveBudgetAsync(original, Ct);
        await store.ReplaceBudgetAsync(original.Id, Current(), Ct);
        var before = await SnapshotAsync(provider);
        var rejected = await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => store.SaveBudgetAsync(Current("USD"), Ct));
        Assert.Equal(FeaturePermission.RequiresPro, rejected.Permission); Assert.Equal(before, await SnapshotAsync(provider));
    }

    [Fact]
    public async Task Enabled_save_reads_default_month_start_without_creating_a_settings_row()
    {
        var path = _directory.Combine("no-settings.db"); var provider = Provider(path, new AccessSource(path, Context(ProductPlan.Free)));
        await provider.GetRequiredService<ZananceStore>().SaveBudgetAsync(Current(), Ct);
        await using var db = await provider.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        Assert.Empty(await db.Settings.ToListAsync(Ct)); Assert.Empty(await db.Entries.ToListAsync(Ct));
    }

    [Fact]
    public async Task Independent_providers_competing_for_the_only_current_definition_commit_once()
    {
        var path = _directory.Combine("contention.db"); using var barrier = new Barrier(2);
        var source = new AccessSource(path, Context(ProductPlan.Free), barrier);
        var first = Provider(path, source).GetRequiredService<ZananceStore>(); var second = Provider(path, source).GetRequiredService<ZananceStore>();
        var events = 0; first.Changed += (_, _) => Interlocked.Increment(ref events); second.Changed += (_, _) => Interlocked.Increment(ref events);
        async Task<Exception?> Attempt(ZananceStore store, string currency)
        { try { await store.SaveBudgetAsync(Current(currency), Ct); return null; } catch (Exception ex) { return ex; } }
        var results = await Task.WhenAll(Task.Run(() => Attempt(first, "EUR"), Ct), Task.Run(() => Attempt(second, "USD"), Ct));
        Assert.Single(results, result => result is null);
        Assert.IsType<CommercialWriteRejectedException>(Assert.Single(results, result => result is not null));
        Assert.Equal(2, source.SynchronizedCaptures); Assert.Equal(1, events); Assert.Single(await first.GetBudgetsAsync(Ct));
        Assert.Empty(await first.GetEntriesAsync(cancellationToken: Ct));
    }

    /// <summary>Resolved immutable financial-scope facts, never a fake budget/count implementation.</summary>
    private sealed class AccessSource(string path, CapabilityContext context, Barrier? barrier = null) : ICommercialWriteAccessSource
    {
        private readonly CommercialWriteAccess _access = new(path, context);
        private int _captures;
        /// <summary>Gets the initial contenders captured before writer acquisition.</summary>
        public int SynchronizedCaptures;
        /// <inheritdoc />
        public CommercialWriteAccess Capture(string databasePath)
        {
            Assert.Equal(Path.GetFullPath(path), databasePath);
            if (barrier is not null && Interlocked.Increment(ref _captures) <= 2)
            {
                Interlocked.Increment(ref SynchronizedCaptures);
                if (!barrier.SignalAndWait(TimeSpan.FromSeconds(15), Ct)) throw new TimeoutException("The contenders did not rendezvous.");
            }
            return _access;
        }
    }

    /// <summary>Explicit device-local instant for financial-period boundaries, not a replacement budget algorithm.</summary>
    private sealed class FixedTime : TimeProvider
    {
        public DateTimeOffset Now = new(2026, 10, 10, 12, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }
}
