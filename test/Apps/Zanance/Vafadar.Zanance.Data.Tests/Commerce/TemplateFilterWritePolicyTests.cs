using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Testing;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data.Tests.Commerce;

/// <summary>AT-121: actual template/filter writes, replacement semantics and independent SQLite capacity checks.</summary>
[Trait("AT", "AT-121")]
public sealed class TemplateFilterWritePolicyTests : IDisposable
{
    private readonly TemporaryDirectory _directory = new();
    private readonly List<ServiceProvider> _providers = [];
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static readonly Guid ProfileId = Guid.Parse("11111111-2222-3333-4444-555555555555");
    private static readonly Guid AccountId = Guid.Parse("66666666-2222-3333-4444-555555555555");

    private static EntryTemplate Template(string name) => new()
    {
        Name = name, Kind = EntryKind.Expense, AccountId = AccountId, CategoryId = Guid.NewGuid(), Amount = 12345,
        Title = "Original complete title", Payee = "Original payee", Icon = "Cart", SortOrder = 17,
    };

    private static SavedFilter Filter(string name) => new()
    {
        Name = name, Period = 3, From = new(2026, 7, 1), To = new(2026, 7, 20), Kind = 1, AccountId = AccountId,
        CategoryIds = [Guid.NewGuid(), Guid.NewGuid()], CategoryName = "Original category scope", InTotalsOnly = true,
        UnreviewedOnly = true, Search = "#original complete query", SortOrder = 17,
    };

    private static CapabilityContext Context(ProductPlan plan, bool shared = false, bool member = true, bool host = true) =>
        new(plan, new(shared ? EntitlementScopeKind.SharedSpace : EntitlementScopeKind.PersonalProfile, ProfileId),
            shared ? new(ProfileId, member, host) : null);

    private ServiceProvider Provider(string path, ICommercialWriteAccessSource? source = null)
    {
        var services = new ServiceCollection();
        if (source is not null) services.AddSingleton(source);
        var provider = services.AddZananceData(path).BuildServiceProvider();
        provider.MigrateLocalDatabase<ZananceDbContext>();
        _providers.Add(provider);
        return provider;
    }

    private static async Task SeedAsync(ServiceProvider provider, bool filter, int count)
    {
        await using var db = await provider.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        for (var i = 0; i < count; i++)
        {
            if (filter) db.SavedFilters.Add(Filter("Original " + i));
            else db.Templates.Add(Template("Original " + i));
        }
        await db.SaveChangesAsync(Ct);
    }

    private static int Limit(bool filter) => filter ? 1 : 3;
    private static Task SaveAsync(ZananceStore store, bool filter, string name) =>
        filter ? store.SaveSavedFilterAsync(Filter(name), Ct) : store.SaveTemplateAsync(Template(name), Ct);
    private static async Task<int> CountAsync(ZananceStore store, bool filter) =>
        filter ? (await store.GetSavedFiltersAsync(Ct)).Count : (await store.GetTemplatesAsync(Ct)).Count;
    private static async Task<string> RowsAsync(ZananceStore store, bool filter) => filter
        ? JsonSerializer.Serialize((await store.GetSavedFiltersAsync(Ct)).OrderBy(row => row.Id))
        : JsonSerializer.Serialize((await store.GetTemplatesAsync(Ct)).OrderBy(row => row.Id));

    public void Dispose()
    {
        foreach (var provider in _providers) provider.Dispose();
        SqliteConnection.ClearAllPools();
        _directory.Dispose();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Inactive_current_registration_still_allows_more_than_the_final_Free_quota(bool filter)
    {
        var store = Provider(_directory.Combine("inactive.db")).GetRequiredService<ZananceStore>();
        for (var i = 0; i <= Limit(filter); i++) await SaveAsync(store, filter, "Unrestricted " + i);
        Assert.Equal(Limit(filter) + 1, await CountAsync(store, filter));
        Assert.Empty(await store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task New_resource_at_Free_capacity_is_rejected_before_sort_assignment_audit_or_stored_metadata_changes(bool filter)
    {
        var path = _directory.Combine("free.db");
        var provider = Provider(path, new AccessSource(path, Context(ProductPlan.Free)));
        await SeedAsync(provider, filter, Limit(filter));
        var store = provider.GetRequiredService<ZananceStore>();
        var before = await RowsAsync(store, filter);
        var events = 0;
        store.Changed += (_, _) => events++;
        var template = Template("New");
        var savedFilter = Filter("New");
        var rejected = await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => filter
            ? store.SaveSavedFilterAsync(savedFilter, Ct) : store.SaveTemplateAsync(template, Ct));
        Assert.Equal(filter ? QuotaKind.SavedFilters : QuotaKind.QuickTemplates, rejected.Quota);
        Assert.Equal(filter ? CommercialFeature.SavedFilters : CommercialFeature.QuickTemplates, rejected.Feature);
        Assert.Equal(Limit(filter), rejected.Current);
        Assert.Equal(Limit(filter), rejected.Maximum);
        Assert.Equal(before, await RowsAsync(store, filter));
        Assert.Equal(17, filter ? savedFilter.SortOrder : template.SortOrder);
        Assert.Equal(default, filter ? savedFilter.CreatedAt : template.CreatedAt);
        Assert.Equal(0, events);
        Assert.Empty(await store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Existing_id_edit_above_quota_reuses_its_slot_and_preserves_creation_and_complete_other_metadata(bool filter)
    {
        var path = _directory.Combine("edit.db");
        var provider = Provider(path, new AccessSource(path, Context(ProductPlan.Free)));
        await SeedAsync(provider, filter, Limit(filter) + 2);
        var store = provider.GetRequiredService<ZananceStore>();
        if (filter)
        {
            var rows = await store.GetSavedFiltersAsync(Ct);
            var target = rows[0];
            var created = target.CreatedAt;
            var other = JsonSerializer.Serialize(rows.Skip(1));
            target.Search = "#corrected query";
            await store.SaveSavedFilterAsync(target, Ct);
            var after = await store.GetSavedFiltersAsync(Ct);
            Assert.Equal(other, JsonSerializer.Serialize(after.Where(row => row.Id != target.Id)));
            var actual = Assert.Single(after, row => row.Id == target.Id);
            Assert.Equal(created, actual.CreatedAt);
            Assert.Equal(target.CategoryIds, actual.CategoryIds);
            Assert.Equal(target.From, actual.From);
            Assert.Equal(target.To, actual.To);
            Assert.Equal("#corrected query", actual.Search);
        }
        else
        {
            var rows = await store.GetTemplatesAsync(Ct);
            var target = rows[0];
            var created = target.CreatedAt;
            var other = JsonSerializer.Serialize(rows.Skip(1));
            target.Title = "Corrected title";
            await store.SaveTemplateAsync(target, Ct);
            var after = await store.GetTemplatesAsync(Ct);
            Assert.Equal(other, JsonSerializer.Serialize(after.Where(row => row.Id != target.Id)));
            var actual = Assert.Single(after, row => row.Id == target.Id);
            Assert.Equal(created, actual.CreatedAt);
            Assert.Equal(target.Payee, actual.Payee);
            Assert.Equal(target.AccountId, actual.AccountId);
            Assert.Equal(target.Amount, actual.Amount);
            Assert.Equal("Corrected title", actual.Title);
        }
        Assert.Equal(Limit(filter) + 2, await CountAsync(store, filter));
        Assert.Empty(await store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Deleting_one_resource_frees_capacity_without_changing_ledger_entries(bool filter)
    {
        var path = _directory.Combine("delete.db");
        var provider = Provider(path, new AccessSource(path, Context(ProductPlan.Free)));
        await SeedAsync(provider, filter, Limit(filter));
        var store = provider.GetRequiredService<ZananceStore>();
        if (filter) await store.DeleteSavedFilterAsync((await store.GetSavedFiltersAsync(Ct))[0].Id, Ct);
        else await store.DeleteTemplateAsync((await store.GetTemplatesAsync(Ct))[0].Id, Ct);
        await SaveAsync(store, filter, "Replacement capacity");
        Assert.Equal(Limit(filter), await CountAsync(store, filter));
        Assert.Empty(await store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Theory]
    [InlineData(false, ProductPlan.Plus, false)]
    [InlineData(true, ProductPlan.Plus, false)]
    [InlineData(false, ProductPlan.Pro, false)]
    [InlineData(true, ProductPlan.Pro, false)]
    [InlineData(false, ProductPlan.Free, true)]
    [InlineData(true, ProductPlan.Free, true)]
    public async Task Paid_personal_and_exact_active_shared_contexts_have_unlimited_capacity(bool filter, ProductPlan plan, bool shared)
    {
        var path = _directory.Combine("unlimited.db");
        var provider = Provider(path, new AccessSource(path, Context(plan, shared)));
        var store = provider.GetRequiredService<ZananceStore>();
        for (var i = 0; i <= Limit(filter); i++) await SaveAsync(store, filter, "Allowed " + i);
        Assert.Equal(Limit(filter) + 1, await CountAsync(store, filter));
    }

    [Fact]
    public async Task Confirmed_same_name_replacement_at_Free_limit_keeps_one_slot_and_stores_the_complete_new_query()
    {
        var path = _directory.Combine("replace.db");
        var provider = Provider(path, new AccessSource(path, Context(ProductPlan.Free)));
        await SeedAsync(provider, true, 1);
        var store = provider.GetRequiredService<ZananceStore>();
        var original = Assert.Single(await store.GetSavedFiltersAsync(Ct));
        var replacement = Filter(" original 0 ");
        replacement.Search = "#replacement query";
        replacement.InTotalsOnly = false;
        await store.SaveSavedFilterAsync(replacement, Ct);
        var actual = Assert.Single(await store.GetSavedFiltersAsync(Ct));
        Assert.NotEqual(original.Id, actual.Id);
        Assert.Equal(replacement.Id, actual.Id);
        Assert.Equal("original 0", actual.Name);
        Assert.Equal(original.SortOrder, actual.SortOrder);
        Assert.Equal(JsonSerializer.Serialize(replacement), JsonSerializer.Serialize(actual));
        Assert.Empty(await store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task Same_id_and_confirmed_name_collision_can_reduce_retained_over_quota_filters_without_consuming_another_slot()
    {
        var path = _directory.Combine("collision.db");
        var provider = Provider(path, new AccessSource(path, Context(ProductPlan.Free)));
        await SeedAsync(provider, true, 3);
        var store = provider.GetRequiredService<ZananceStore>();
        var original = await store.GetSavedFiltersAsync(Ct);
        var target = original[0];
        var retained = JsonSerializer.Serialize(original[2]);
        var created = target.CreatedAt;
        var order = target.SortOrder;
        target.Name = original[1].Name;
        target.Search = "#explicit replacement";
        await store.SaveSavedFilterAsync(target, Ct);
        var actual = await store.GetSavedFiltersAsync(Ct);
        Assert.Equal(2, actual.Count);
        Assert.DoesNotContain(actual, row => row.Id == original[1].Id);
        Assert.Equal(retained, JsonSerializer.Serialize(Assert.Single(actual, row => row.Id == original[2].Id)));
        var edited = Assert.Single(actual, row => row.Id == target.Id);
        Assert.Equal(created, edited.CreatedAt);
        Assert.Equal(order, edited.SortOrder);
        Assert.Equal("#explicit replacement", edited.Search);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Actual_database_failure_rolls_back_new_template_or_removed_filter_and_allows_retry(bool filter)
    {
        var path = _directory.Combine("failure.db");
        var provider = Provider(path, new AccessSource(path, Context(ProductPlan.Free)));
        await SeedAsync(provider, filter, filter ? 1 : 2);
        var store = provider.GetRequiredService<ZananceStore>();
        var before = await RowsAsync(store, filter);
        var events = 0;
        store.Changed += (_, _) => events++;
        await using var db = await provider.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        // The isolated test database rejects INSERT after the store prepares its actual changes/removal.
        var trigger = filter
            ? "CREATE TRIGGER reject_resource BEFORE INSERT ON SavedFilters BEGIN SELECT RAISE(ABORT, 'fixture failure'); END;"
            : "CREATE TRIGGER reject_resource BEFORE INSERT ON Templates BEGIN SELECT RAISE(ABORT, 'fixture failure'); END;";
        await db.Database.ExecuteSqlRawAsync(trigger, Ct);
        await Assert.ThrowsAsync<DbUpdateException>(() => SaveAsync(store, filter, filter ? "original 0" : "New"));
        Assert.Equal(before, await RowsAsync(store, filter));
        Assert.Equal(0, events);
        await db.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_resource;", Ct);
        await SaveAsync(store, filter, filter ? "original 0" : "New");
        Assert.Equal(filter ? 1 : 3, await CountAsync(store, filter));
        Assert.Equal(1, events);
        Assert.Empty(await store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Expired_shared_host_keeps_existing_corrections_but_rejects_new_resources(bool filter)
    {
        var path = _directory.Combine("expired.db");
        var provider = Provider(path, new AccessSource(path, Context(ProductPlan.Pro, true, host: false)));
        await SeedAsync(provider, filter, 1);
        var store = provider.GetRequiredService<ZananceStore>();
        var rejected = await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => SaveAsync(store, filter, "New"));
        Assert.Equal(FeaturePermission.RequiresPro, rejected.Permission);
        if (filter)
        {
            var original = Assert.Single(await store.GetSavedFiltersAsync(Ct));
            original.Search = "#correction";
            await store.SaveSavedFilterAsync(original, Ct);
        }
        else
        {
            var original = Assert.Single(await store.GetTemplatesAsync(Ct));
            original.Title = "Correction";
            await store.SaveTemplateAsync(original, Ct);
        }
        Assert.Equal(1, await CountAsync(store, filter));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Personal_Pro_never_substitutes_for_missing_shared_membership_on_existing_edits(bool filter)
    {
        var path = _directory.Combine("membership.db");
        var provider = Provider(path, new AccessSource(path, Context(ProductPlan.Pro, true, member: false)));
        await SeedAsync(provider, filter, 1);
        var store = provider.GetRequiredService<ZananceStore>();
        var before = await RowsAsync(store, filter);
        var rejected = await Assert.ThrowsAsync<CommercialWriteRejectedException>(async () =>
        {
            if (filter) await store.SaveSavedFilterAsync(Assert.Single(await store.GetSavedFiltersAsync(Ct)), Ct);
            else await store.SaveTemplateAsync(Assert.Single(await store.GetTemplatesAsync(Ct)), Ct);
        });
        Assert.Equal(FeaturePermission.RequiresMembership, rejected.Permission);
        Assert.Equal(before, await RowsAsync(store, filter));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Two_independent_providers_competing_for_the_last_slot_commit_once_with_correct_sort_order(bool filter)
    {
        var path = _directory.Combine("contention.db");
        using var barrier = new Barrier(2);
        var source = new AccessSource(path, Context(ProductPlan.Free), barrier);
        var firstProvider = Provider(path, source);
        var secondProvider = Provider(path, source);
        await SeedAsync(firstProvider, filter, Limit(filter) - 1);
        var first = firstProvider.GetRequiredService<ZananceStore>();
        var second = secondProvider.GetRequiredService<ZananceStore>();
        var committed = 0;
        first.Changed += (_, _) => Interlocked.Increment(ref committed);
        second.Changed += (_, _) => Interlocked.Increment(ref committed);
        async Task<Exception?> Attempt(ZananceStore store, string name)
        {
            try { await SaveAsync(store, filter, name); return null; }
            catch (Exception ex) { return ex; }
        }
        var results = await Task.WhenAll(Task.Run(() => Attempt(first, "Contender A"), Ct), Task.Run(() => Attempt(second, "Contender B"), Ct));
        Assert.Single(results, result => result is null);
        var rejected = Assert.IsType<CommercialWriteRejectedException>(Assert.Single(results, result => result is not null));
        Assert.Equal(Limit(filter), rejected.Current);
        Assert.Equal(1, committed);
        Assert.Equal(Limit(filter), await CountAsync(first, filter));
        Assert.Equal(2, source.SynchronizedCaptures);
        if (filter) Assert.Equal(0, Assert.Single(await first.GetSavedFiltersAsync(Ct)).SortOrder);
        else Assert.Equal(18, (await first.GetTemplatesAsync(Ct)).Max(row => row.SortOrder));
    }

    /// <summary>Fictitious resolved facts only; the source performs no quota/count algorithm or purchase verification.</summary>
    private sealed class AccessSource(string path, CapabilityContext context, Barrier? barrier = null) : ICommercialWriteAccessSource
    {
        private readonly CommercialWriteAccess _access = new(path, context);
        private int _captures;
        /// <summary>Gets the number of initial contenders observed before writer acquisition.</summary>
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
}
