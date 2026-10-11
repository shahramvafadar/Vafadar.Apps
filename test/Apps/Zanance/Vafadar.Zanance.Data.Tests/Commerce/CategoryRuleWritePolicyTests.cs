using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Testing;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data.Tests.Commerce;

/// <summary>AT-127: exact-file categorization rule creation, retained corrections and serialized replacements.</summary>
[Trait("AT", "AT-127")]
public sealed class CategoryRuleWritePolicyTests : IDisposable
{
    private readonly TemporaryDirectory _directory = new();
    private readonly List<ServiceProvider> _providers = [];
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static readonly Guid Scope = Guid.Parse("11111111-2222-3333-4444-555555555555");
    private static CapabilityContext Context(ProductPlan plan, bool shared = false, bool member = true, bool host = true) =>
        new(plan, new(shared ? EntitlementScopeKind.SharedSpace : EntitlementScopeKind.PersonalProfile, Scope),
            shared ? new(Scope, member, host) : null);

    private ServiceProvider Provider(string path, AccessSource source, bool trigger = false)
    {
        var services = new ServiceCollection().AddSingleton<TimeProvider>(new FixedTime())
            .AddSingleton<ICommercialWriteAccessSource>(source).AddZananceData(path);
        if (trigger)
        {
            var original = services.Single(d => d.ServiceType == typeof(IDbContextFactory<ZananceDbContext>));
            services.Remove(original);
            services.AddSingleton<IDbContextFactory<ZananceDbContext>>(p => new TriggerConnections(
                (IDbContextFactory<ZananceDbContext>)ActivatorUtilities.CreateInstance(p, original.ImplementationType!), source));
        }
        var provider = services.BuildServiceProvider(); provider.MigrateLocalDatabase<ZananceDbContext>();
        _providers.Add(provider); return provider;
    }

    private async Task<Fixture> FixtureAsync(bool trigger = false)
    {
        var path = _directory.Combine(Guid.NewGuid() + ".db"); var source = new AccessSource(path);
        var provider = Provider(path, source, trigger); var store = provider.GetRequiredService<ZananceStore>();
        var category = new Category { Kind = CategoryKind.Expense, Name = "Original expense" };
        var second = new Category { Kind = CategoryKind.Expense, Name = "Other expense" };
        await store.SaveCategoryAsync(category, Ct); await store.SaveCategoryAsync(second, Ct);
        return new(path, source, provider, store, category, second);
    }

    private sealed record Fixture(string Path, AccessSource Source, ServiceProvider Provider, ZananceStore Store,
        Category Category, Category Second)
    {
        public CategoryRule Rule(string match = "Original match", CategoryKind kind = CategoryKind.Expense) =>
            new() { Match = match, CategoryId = Category.Id, Kind = kind };
        public void Enable(ProductPlan plan = ProductPlan.Free, bool shared = false, bool member = true, bool host = true) =>
            Source.Current = new(Path, Context(plan, shared, member, host));
    }

    [Fact]
    public async Task Free_new_rule_is_rejected_before_draft_trim_audit_or_stored_changes()
    {
        var f = await FixtureAsync(); f.Enable(); var before = await SnapshotAsync(f.Provider);
        var rule = f.Rule("  New match  "); var events = 0; f.Store.Changed += (_, _) => events++;
        var error = await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => f.Store.SaveCategoryRuleAsync(rule, Ct));
        Assert.Equal(CommercialFeature.CategorizationRules, error.Feature); Assert.Equal(FeaturePermission.RequiresPlus, error.Permission);
        Assert.Equal("  New match  ", rule.Match); Assert.Equal(default, rule.CreatedAt); Assert.Equal(default, rule.UpdatedAt);
        Assert.Equal(0, events); Assert.Equal(before, await SnapshotAsync(f.Provider));
    }

    [Fact]
    public async Task Inactive_registration_keeps_rules_unrestricted_without_ledger_or_settings_changes()
    {
        var f = await FixtureAsync(); var before = await SnapshotAsync(f.Provider, omitRules: true);
        for (var i = 0; i < 8; i++) await f.Store.SaveCategoryRuleAsync(f.Rule("  Match " + i + "  "), Ct);
        Assert.Equal(8, (await f.Store.GetCategoryRulesAsync(Ct)).Count);
        Assert.Equal(before, await SnapshotAsync(f.Provider, omitRules: true));
    }

    [Theory]
    [InlineData(ProductPlan.Plus, false)]
    [InlineData(ProductPlan.Pro, false)]
    [InlineData(ProductPlan.Free, true)]
    public async Task Paid_personal_or_exact_shared_guest_rights_allow_new_rules(ProductPlan plan, bool shared)
    {
        var f = await FixtureAsync(); f.Enable(plan, shared); var before = await SnapshotAsync(f.Provider, omitRules: true);
        var rule = f.Rule("  Paid match  "); var events = 0; f.Store.Changed += (_, _) => events++;
        await f.Store.SaveCategoryRuleAsync(rule, Ct);
        var saved = Assert.Single(await f.Store.GetCategoryRulesAsync(Ct)); Assert.Equal(rule.Id, saved.Id);
        Assert.Equal("Paid match", saved.Match); Assert.Equal(f.Category.Id, saved.CategoryId); Assert.Equal(1, events);
        Assert.Equal(before, await SnapshotAsync(f.Provider, omitRules: true));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Retained_pattern_correction_and_deletion_remain_available_after_downgrade(bool expiredHost)
    {
        var f = await FixtureAsync(); var rule = f.Rule(); await f.Store.SaveCategoryRuleAsync(rule, Ct);
        var before = await SnapshotAsync(f.Provider, omitRules: true); var created = rule.CreatedAt;
        f.Enable(shared: expiredHost, host: !expiredHost); rule.CategoryId = f.Second.Id; rule.Match = "  ORIGINAL MATCH  ";
        rule.CreatedAt = default;
        await f.Store.SaveCategoryRuleAsync(rule, Ct);
        var saved = Assert.Single(await f.Store.GetCategoryRulesAsync(Ct)); Assert.Equal(rule.Id, saved.Id);
        Assert.Equal(created, saved.CreatedAt); Assert.Equal(f.Second.Id, saved.CategoryId); Assert.Equal("ORIGINAL MATCH", saved.Match);
        await f.Store.DeleteCategoryRuleAsync(rule.Id, Ct); Assert.Empty(await f.Store.GetCategoryRulesAsync(Ct));
        Assert.Equal(before, await SnapshotAsync(f.Provider, omitRules: true));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Same_pattern_new_id_replacement_is_a_retained_correction_in_the_existing_UI_path(bool expiredHost)
    {
        var f = await FixtureAsync(); var original = f.Rule(); await f.Store.SaveCategoryRuleAsync(original, Ct);
        f.Enable(shared: expiredHost, host: !expiredHost); var correction = f.Rule("  ORIGINAL MATCH  "); correction.CategoryId = f.Second.Id;
        await f.Store.SaveCategoryRuleAsync(correction, Ct);
        var saved = Assert.Single(await f.Store.GetCategoryRulesAsync(Ct)); Assert.Equal(correction.Id, saved.Id);
        Assert.NotEqual(original.Id, saved.Id); Assert.Equal(f.Second.Id, saved.CategoryId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Existing_id_cannot_add_new_pattern_or_kind_after_downgrade(bool changeKind)
    {
        var f = await FixtureAsync(); var rule = f.Rule(); await f.Store.SaveCategoryRuleAsync(rule, Ct);
        f.Enable(); var before = await SnapshotAsync(f.Provider); var events = 0; f.Store.Changed += (_, _) => events++;
        if (changeKind) rule.Kind = CategoryKind.Income; else rule.Match = "  Newly configured pattern  ";
        var error = await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => f.Store.SaveCategoryRuleAsync(rule, Ct));
        Assert.Equal(CommercialFeature.CategorizationRules, error.Feature); Assert.Equal(before, await SnapshotAsync(f.Provider));
        Assert.Equal(changeKind ? "Original match" : "  Newly configured pattern  ", rule.Match); Assert.Equal(0, events);
    }

    [Fact]
    public async Task Same_text_in_another_kind_is_new_automation_rather_than_an_existing_replacement()
    {
        var f = await FixtureAsync(); await f.Store.SaveCategoryRuleAsync(f.Rule(), Ct); f.Enable();
        var before = await SnapshotAsync(f.Provider);
        await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => f.Store.SaveCategoryRuleAsync(f.Rule(kind: CategoryKind.Income), Ct));
        Assert.Equal(before, await SnapshotAsync(f.Provider));
    }

    [Theory]
    [InlineData("create")]
    [InlineData("replace")]
    [InlineData("edit")]
    [InlineData("delete")]
    public async Task Personal_Pro_never_replaces_membership_for_rules(string operation)
    {
        var f = await FixtureAsync(); var rule = f.Rule(); await f.Store.SaveCategoryRuleAsync(rule, Ct);
        f.Enable(ProductPlan.Pro, shared: true, member: false); var before = await SnapshotAsync(f.Provider); var events = 0;
        f.Store.Changed += (_, _) => events++;
        var error = await Assert.ThrowsAsync<CommercialWriteRejectedException>(async () =>
        {
            if (operation == "delete") await f.Store.DeleteCategoryRuleAsync(rule.Id, Ct);
            else if (operation == "create") await f.Store.SaveCategoryRuleAsync(f.Rule("Brand new"), Ct);
            else if (operation == "replace") await f.Store.SaveCategoryRuleAsync(f.Rule(), Ct);
            else { rule.CategoryId = f.Second.Id; await f.Store.SaveCategoryRuleAsync(rule, Ct); }
        });
        Assert.Equal(FeaturePermission.RequiresMembership, error.Permission); Assert.Equal(0, events);
        Assert.Equal(before, await SnapshotAsync(f.Provider));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Mismatched_file_facts_reject_rule_save_or_delete_before_mutation(bool delete)
    {
        var f = await FixtureAsync(); var rule = f.Rule(); await f.Store.SaveCategoryRuleAsync(rule, Ct);
        var before = await SnapshotAsync(f.Provider); f.Source.Current = new(_directory.Combine("wrong.db"), Context(ProductPlan.Pro));
        var events = 0; f.Store.Changed += (_, _) => events++;
        await Assert.ThrowsAsync<InvalidOperationException>(() => delete ? f.Store.DeleteCategoryRuleAsync(rule.Id, Ct)
            : f.Store.SaveCategoryRuleAsync(f.Rule("New wrong file"), Ct));
        Assert.Equal(0, events); Assert.Equal(before, await SnapshotAsync(f.Provider));
    }

    [Theory]
    [InlineData("create")]
    [InlineData("replace")]
    [InlineData("edit")]
    [InlineData("delete")]
    public async Task SQL_trigger_retired_access_rolls_back_all_rule_operations_without_Changed(string operation)
    {
        var f = await FixtureAsync(trigger: true); var rule = f.Rule(); await f.Store.SaveCategoryRuleAsync(rule, Ct);
        f.Enable(ProductPlan.Plus); var before = await SnapshotAsync(f.Provider); var events = 0; f.Store.Changed += (_, _) => events++;
        var verb = operation == "edit" ? "UPDATE" : operation is "delete" or "replace" ? "DELETE" : "INSERT";
        await SqlAsync(f, "CREATE TRIGGER RetireRule AFTER " + verb + " ON CategoryRules BEGIN SELECT RetireRuleAccess(); END;");
        f.Source.Retire = true;
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            if (operation == "delete") await f.Store.DeleteCategoryRuleAsync(rule.Id, Ct);
            else if (operation == "replace") await f.Store.SaveCategoryRuleAsync(f.Rule(), Ct);
            else if (operation == "create") await f.Store.SaveCategoryRuleAsync(f.Rule("New paid match"), Ct);
            else { rule.CategoryId = f.Second.Id; await f.Store.SaveCategoryRuleAsync(rule, Ct); }
        });
        Assert.Equal(1, f.Source.Retired); Assert.Equal(0, events); Assert.Equal(before, await SnapshotAsync(f.Provider));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Failed_SQL_replacement_restores_original_rule_in_inactive_and_enabled_operations(bool enabled)
    {
        var f = await FixtureAsync(); var rule = f.Rule(); await f.Store.SaveCategoryRuleAsync(rule, Ct);
        if (enabled) f.Enable(); var before = await SnapshotAsync(f.Provider); var events = 0; f.Store.Changed += (_, _) => events++;
        await SqlAsync(f, "CREATE TRIGGER RejectNewRule BEFORE INSERT ON CategoryRules BEGIN SELECT RAISE(ABORT,'Owned rule SQL failure'); END;");
        await Assert.ThrowsAsync<DbUpdateException>(() => f.Store.SaveCategoryRuleAsync(f.Rule(), Ct));
        Assert.Equal(0, events); Assert.Equal(before, await SnapshotAsync(f.Provider));
        await SqlAsync(f, "DROP TRIGGER RejectNewRule"); await f.Store.SaveCategoryRuleAsync(f.Rule(), Ct);
        Assert.Single(await f.Store.GetCategoryRulesAsync(Ct)); Assert.Equal(1, events);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Independent_same_pattern_writers_leave_one_rule_in_inactive_and_enabled_operations(bool enabled)
    {
        var f = await FixtureAsync(); var second = Provider(f.Path, f.Source).GetRequiredService<ZananceStore>();
        if (enabled) f.Enable(ProductPlan.Plus); var before = await SnapshotAsync(f.Provider, omitRules: true);
        using var barrier = new Barrier(2); f.Source.Rendezvous = barrier; f.Source.Captures = 0;
        var first = f.Rule(" Concurrent match "); var other = f.Rule("CONCURRENT MATCH"); other.CategoryId = f.Second.Id;
        var events = 0; f.Store.Changed += (_, _) => Interlocked.Increment(ref events); second.Changed += (_, _) => Interlocked.Increment(ref events);
        await Task.WhenAll(Task.Run(() => f.Store.SaveCategoryRuleAsync(first, Ct), Ct), Task.Run(() => second.SaveCategoryRuleAsync(other, Ct), Ct));
        var saved = Assert.Single(await f.Store.GetCategoryRulesAsync(Ct)); Assert.Contains(saved.Id, new[] { first.Id, other.Id });
        Assert.Equal(2, events); Assert.Equal(before, await SnapshotAsync(f.Provider, omitRules: true));
    }

    [Fact]
    public async Task Access_capture_before_writer_keeps_the_initial_database_after_profile_provider_moves()
    {
        var f = await FixtureAsync(); var otherPath = _directory.Combine("untouched-profile.db");
        var other = Provider(otherPath, new AccessSource(otherPath)); var before = await SnapshotAsync(other);
        f.Enable(ProductPlan.Plus); f.Source.OnCapture = () => f.Provider.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>().MoveTo(otherPath);
        await f.Store.SaveCategoryRuleAsync(f.Rule(), Ct); Assert.Equal(before, await SnapshotAsync(other));
        f.Source.OnCapture = null;
        f.Provider.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>().MoveTo(f.Path);
        Assert.Single(await f.Store.GetCategoryRulesAsync(Ct)); Assert.All(f.Source.Paths, path => Assert.Equal(f.Path, path));
    }

    [Fact]
    public async Task Invalid_short_rule_is_rejected_before_writer_and_keeps_original_draft()
    {
        var f = await FixtureAsync(); var rule = f.Rule(" x "); var before = await SnapshotAsync(f.Provider);
        await Assert.ThrowsAsync<ArgumentException>(() => f.Store.SaveCategoryRuleAsync(rule, Ct));
        Assert.Empty(f.Source.Paths); Assert.Equal(" x ", rule.Match); Assert.Equal(before, await SnapshotAsync(f.Provider));
    }

    private static async Task SqlAsync(Fixture f, string sql)
    {
        await using var db = await f.Provider.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        await db.Database.ExecuteSqlRawAsync(sql, Ct);
    }

    private static async Task<string> SnapshotAsync(ServiceProvider provider, bool omitRules = false)
    {
        await using var db = await provider.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        await db.Database.OpenConnectionAsync(Ct); var connection = db.Database.GetDbConnection(); var tables = new List<string>();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name";
            using var reader = await command.ExecuteReaderAsync(Ct); while (await reader.ReadAsync(Ct)) tables.Add(reader.GetString(0));
        }
        Assert.Equal(24, tables.Count); var all = new Dictionary<string, List<string>>();
        foreach (var table in tables)
        {
            if (omitRules && table == "CategoryRules") continue;
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

    /// <summary>Synchronous fixture facts; a barrier coordinates independent providers before the actual writer.</summary>
    private sealed class AccessSource(string path) : ICommercialWriteAccessSource
    {
        public CommercialWriteAccess Current = CommercialWriteAccess.Inactive;
        public Barrier? Rendezvous = null;
        public int Captures;
        public bool Retire;
        public int Retired;
        public readonly List<string> Paths = [];
        public Action? OnCapture;
        public CommercialWriteAccess Capture(string databasePath)
        {
            Assert.Equal(System.IO.Path.GetFullPath(path), databasePath); lock (Paths) Paths.Add(databasePath);
            OnCapture?.Invoke();
            if (Rendezvous is { } barrier && Interlocked.Increment(ref Captures) <= 2
                && !barrier.SignalAndWait(TimeSpan.FromSeconds(15), Ct)) throw new TimeoutException("Rule writers did not rendezvous.");
            return Current;
        }
    }

    /// <summary>A real native connection callback changes only cached fixture rights after SQL.</summary>
    private sealed class TriggerConnections(IDbContextFactory<ZananceDbContext> inner, AccessSource source) : IDbContextFactory<ZananceDbContext>
    {
        public ZananceDbContext CreateDbContext()
        {
            var db = inner.CreateDbContext();
            ((SqliteConnection)db.Database.GetDbConnection()).CreateFunction("RetireRuleAccess", () =>
            {
                if (source.Retire)
                {
                    source.Current = new(source.Current.DatabasePath!, Context(ProductPlan.Free)); source.Retire = false; source.Retired++;
                }
                return 1;
            });
            return db;
        }
    }

    /// <summary>Stable audit time allows complete column comparison after rollback and unchanged saves.</summary>
    private sealed class FixedTime : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 10, 10, 12, 0, 0, TimeSpan.Zero);
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }

    public void Dispose()
    {
        foreach (var provider in _providers) provider.Dispose(); SqliteTestPools.Clear(_directory); _directory.Dispose();
    }
}
