using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Testing;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data.Tests.Commerce;

/// <summary>AT-120: real SQLite service-side account quotas, inactive deployment and independent-writer contention.</summary>
[Trait("AT", "AT-120")]
public sealed class AccountWritePolicyTests : IDisposable
{
    private readonly TemporaryDirectory _directory = new();
    private readonly List<ServiceProvider> _providers = [];
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static Account Account(string name, bool archived = false) => new()
    {
        Name = name, CurrencyCode = "EUR", OpeningDate = new(2026, 10, 10), OpeningBalance = 12345,
        Counterparty = "Original counterparty", IsArchived = archived,
    };

    private static CapabilityContext Context(ProductPlan plan, bool shared = false, bool activeHost = true) =>
        new(plan, new(shared ? EntitlementScopeKind.SharedSpace : EntitlementScopeKind.PersonalProfile,
            Guid.Parse("11111111-2222-3333-4444-555555555555")),
            shared ? new(Guid.Parse("11111111-2222-3333-4444-555555555555"), true, activeHost) : null);

    private ServiceProvider Provider(string path, ICommercialWriteAccessSource? source = null)
    {
        var services = new ServiceCollection();
        if (source is not null) services.AddSingleton(source);
        var provider = services.AddZananceData(path).BuildServiceProvider();
        provider.MigrateLocalDatabase<ZananceDbContext>();
        _providers.Add(provider);
        return provider;
    }

    private static async Task SeedAsync(ServiceProvider provider, params Account[] accounts)
    {
        await using var db = await provider.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        db.Accounts.AddRange(accounts);
        await db.SaveChangesAsync(Ct);
    }

    private static async Task<string> AccountRowsAsync(ZananceStore store) =>
        JsonSerializer.Serialize((await store.GetAccountsAsync(cancellationToken: Ct)).OrderBy(account => account.Id));

    public void Dispose()
    {
        foreach (var provider in _providers) provider.Dispose();
        SqliteTestPools.Clear(_directory);
        _directory.Dispose();
    }

    [Fact]
    public async Task Current_registration_keeps_test_builds_unrestricted_without_creating_a_paid_right()
    {
        var path = _directory.Combine("inactive.db");
        var provider = Provider(path);
        Assert.False(provider.GetRequiredService<ICommercialWriteAccessSource>().Capture(path).Enforced);
        Assert.Null(provider.GetRequiredService<ICommercialWriteAccessSource>().Capture(path).Context);
        var store = provider.GetRequiredService<ZananceStore>();
        for (var i = 0; i < 10; i++) Assert.True(await store.SaveAccountAsync(Account("Unrestricted " + i), Ct));
        Assert.Equal(10, (await store.GetAccountsAsync(cancellationToken: Ct)).Count);
    }

    [Fact]
    public async Task Enabled_Free_rejects_fourth_account_before_writing_or_raising_Changed_with_a_structured_reason()
    {
        var path = _directory.Combine("free.db");
        var source = new AccessSource(path, Context(ProductPlan.Free));
        var provider = Provider(path, source);
        var store = provider.GetRequiredService<ZananceStore>();
        for (var i = 0; i < 3; i++) Assert.True(await store.SaveAccountAsync(Account("Account " + i), Ct));
        var before = await AccountRowsAsync(store);
        var events = 0;
        store.Changed += (_, _) => events++;
        var draft = Account("Fourth");
        var rejected = await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => store.SaveAccountAsync(draft, Ct));
        Assert.Equal(CommercialFeature.FinancialAccounts, rejected.Feature);
        Assert.Equal(QuotaKind.FinancialAccounts, rejected.Quota);
        Assert.Equal(FeaturePermission.Allowed, rejected.Permission);
        Assert.Equal(3, rejected.Maximum);
        Assert.Equal(3, rejected.Current);
        Assert.Equal(1, rejected.Requested);
        Assert.Equal(before, await AccountRowsAsync(store));
        Assert.Equal(0, events);
        Assert.Equal(default, draft.CreatedAt);
        Assert.Empty(await store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task Existing_over_quota_accounts_remain_correctable_and_archivable_without_automatic_selection_or_deletion()
    {
        var path = _directory.Combine("retained.db");
        var provider = Provider(path, new AccessSource(path, Context(ProductPlan.Free)));
        var seed = Enumerable.Range(0, 10).Select(i => Account("Retained " + i)).ToArray();
        await SeedAsync(provider, seed);
        var store = provider.GetRequiredService<ZananceStore>();
        var correction = (await store.GetAccountsAsync(cancellationToken: Ct))[0];
        correction.Name = "Corrected original";
        correction.Counterparty = "Corrected counterparty";
        Assert.True(await store.SaveAccountAsync(correction, Ct));
        correction.IsArchived = true;
        Assert.True(await store.SaveAccountAsync(correction, Ct));
        var before = await AccountRowsAsync(store);
        correction.IsArchived = false;
        await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => store.SaveAccountAsync(correction, Ct));
        await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => store.SaveAccountAsync(Account("New"), Ct));
        Assert.Equal(before, await AccountRowsAsync(store));
        var rows = await store.GetAccountsAsync(cancellationToken: Ct);
        Assert.Equal(10, rows.Count);
        Assert.Single(rows, account => account.IsArchived);
        Assert.Contains(rows, account => account.Name == "Corrected original" && account.Counterparty == "Corrected counterparty");
    }

    [Fact]
    public async Task Archived_history_uses_no_slot_but_unarchive_competes_for_the_same_active_quota()
    {
        var path = _directory.Combine("archive.db");
        var provider = Provider(path, new AccessSource(path, Context(ProductPlan.Free)));
        var store = provider.GetRequiredService<ZananceStore>();
        var active = Enumerable.Range(0, 3).Select(i => Account("Active " + i)).ToArray();
        foreach (var account in active) Assert.True(await store.SaveAccountAsync(account, Ct));
        var history = Account("Archived history", true);
        Assert.True(await store.SaveAccountAsync(history, Ct));
        history.IsArchived = false;
        await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => store.SaveAccountAsync(history, Ct));
        active[0].IsArchived = true;
        Assert.True(await store.SaveAccountAsync(active[0], Ct));
        Assert.True(await store.SaveAccountAsync(history, Ct));
        Assert.Equal(3, (await store.GetAccountsAsync(cancellationToken: Ct)).Count(account => !account.IsArchived));
        Assert.Equal(4, (await store.GetAccountsAsync(cancellationToken: Ct)).Count);
    }

    [Theory]
    [InlineData(ProductPlan.Plus, false)]
    [InlineData(ProductPlan.Pro, false)]
    [InlineData(ProductPlan.Free, true)]
    public async Task Paid_personal_and_exact_active_shared_contexts_have_unlimited_account_capacity(ProductPlan plan, bool shared)
    {
        var path = _directory.Combine("unlimited.db");
        var provider = Provider(path, new AccessSource(path, Context(plan, shared)));
        var store = provider.GetRequiredService<ZananceStore>();
        for (var i = 0; i < 5; i++) Assert.True(await store.SaveAccountAsync(Account("Account " + i), Ct));
        Assert.Equal(5, (await store.GetAccountsAsync(cancellationToken: Ct)).Count);
    }

    [Fact]
    public async Task Expired_shared_host_rejects_new_work_but_does_not_block_existing_account_correction()
    {
        var path = _directory.Combine("host-expired.db");
        var provider = Provider(path, new AccessSource(path, Context(ProductPlan.Pro, true, false)));
        await SeedAsync(provider, Account("Original"));
        var store = provider.GetRequiredService<ZananceStore>();
        var rejected = await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => store.SaveAccountAsync(Account("New"), Ct));
        Assert.Equal(FeaturePermission.RequiresPro, rejected.Permission);
        Assert.Null(rejected.Quota);
        var original = Assert.Single(await store.GetAccountsAsync(cancellationToken: Ct));
        original.Name = "Correction";
        Assert.True(await store.SaveAccountAsync(original, Ct));
        Assert.Equal("Correction", Assert.Single(await store.GetAccountsAsync(cancellationToken: Ct)).Name);
    }

    [Fact]
    public async Task Missing_shared_membership_cannot_be_bypassed_by_editing_existing_or_creating_archived_accounts()
    {
        var path = _directory.Combine("missing-membership.db");
        var space = Guid.NewGuid();
        var context = new CapabilityContext(ProductPlan.Pro, new(EntitlementScopeKind.SharedSpace, space), new(space, false, true));
        var provider = Provider(path, new AccessSource(path, context));
        await SeedAsync(provider, Account("Original"));
        var store = provider.GetRequiredService<ZananceStore>();
        var before = await AccountRowsAsync(store);
        var correction = Assert.Single(await store.GetAccountsAsync(cancellationToken: Ct));
        correction.Name = "Unauthorized correction";
        var rejected = await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => store.SaveAccountAsync(correction, Ct));
        Assert.Equal(FeaturePermission.RequiresMembership, rejected.Permission);
        rejected = await Assert.ThrowsAsync<CommercialWriteRejectedException>(() => store.SaveAccountAsync(Account("Archived", true), Ct));
        Assert.Equal(FeaturePermission.RequiresMembership, rejected.Permission);
        Assert.Equal(before, await AccountRowsAsync(store));
    }

    [Fact]
    public async Task Failed_database_write_rolls_back_the_writer_and_leaves_the_last_slot_available()
    {
        var path = _directory.Combine("failed-write.db");
        var provider = Provider(path, new AccessSource(path, Context(ProductPlan.Free)));
        await SeedAsync(provider, Account("First"), Account("Second"));
        var store = provider.GetRequiredService<ZananceStore>();
        var before = await AccountRowsAsync(store);
        var events = 0;
        store.Changed += (_, _) => events++;
        var invalid = Account("Invalid");
        invalid.Name = null!; // Exercise the actual required SQLite column after the quota check, not a fake store.
        await Assert.ThrowsAsync<DbUpdateException>(() => store.SaveAccountAsync(invalid, Ct));
        Assert.Equal(before, await AccountRowsAsync(store));
        Assert.Equal(0, events);
        Assert.True(await store.SaveAccountAsync(Account("Valid third"), Ct));
        Assert.Equal(1, events);
        Assert.Equal(3, (await store.GetAccountsAsync(cancellationToken: Ct)).Count);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Another_profile_or_case_variant_snapshot_never_authorizes_the_exact_opened_database(bool caseVariant)
    {
        var path = _directory.Combine("opened.db");
        var source = new AccessSource(caseVariant ? path.ToUpperInvariant() : _directory.Combine("other.db"), Context(ProductPlan.Pro));
        var store = Provider(path, source).GetRequiredService<ZananceStore>();
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.SaveAccountAsync(Account("Wrong boundary"), Ct));
        Assert.Empty(await store.GetAccountsAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task Rights_changed_while_acquiring_the_writer_are_rejected_before_the_account_is_written()
    {
        var path = _directory.Combine("changed.db");
        var source = new AccessSource(path, Context(ProductPlan.Pro))
        {
            ChangedAccess = new(path, Context(ProductPlan.Free)), ChangeAfterCapture = 1,
        };
        var store = Provider(path, source).GetRequiredService<ZananceStore>();
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.SaveAccountAsync(Account("Retired rights"), Ct));
        Assert.Empty(await store.GetAccountsAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task Two_independent_providers_competing_for_last_slot_commit_one_account_and_reject_the_other_by_quota()
    {
        var path = _directory.Combine("contention.db");
        using var barrier = new Barrier(2);
        var source = new AccessSource(path, Context(ProductPlan.Free)) { InitialCaptureBarrier = barrier };
        var firstProvider = Provider(path, source);
        var secondProvider = Provider(path, source);
        await SeedAsync(firstProvider, Account("First"), Account("Second"));
        var first = firstProvider.GetRequiredService<ZananceStore>();
        var second = secondProvider.GetRequiredService<ZananceStore>();
        var committed = 0;
        first.Changed += (_, _) => Interlocked.Increment(ref committed);
        second.Changed += (_, _) => Interlocked.Increment(ref committed);
        async Task<Exception?> Attempt(ZananceStore store, string name)
        {
            try { Assert.True(await store.SaveAccountAsync(Account(name), Ct)); return null; }
            catch (Exception ex) { return ex; }
        }
        var results = await Task.WhenAll(Task.Run(() => Attempt(first, "Contender A"), Ct), Task.Run(() => Attempt(second, "Contender B"), Ct));
        Assert.Single(results, result => result is null);
        var rejected = Assert.IsType<CommercialWriteRejectedException>(Assert.Single(results, result => result is not null));
        Assert.Equal(3, rejected.Current);
        Assert.Equal(QuotaKind.FinancialAccounts, rejected.Quota);
        Assert.Equal(1, committed);
        Assert.Equal(3, (await first.GetAccountsAsync(cancellationToken: Ct)).Count);
        Assert.Equal(2, source.SynchronizedCaptures);
        Assert.All(source.RequestedPaths, requested => Assert.Equal(Path.GetFullPath(path), requested));
    }

    [Fact]
    public async Task Currency_lock_still_returns_false_and_rolls_back_without_mutating_the_existing_account()
    {
        var path = _directory.Combine("currency.db");
        var provider = Provider(path, new AccessSource(path, Context(ProductPlan.Free)));
        var store = provider.GetRequiredService<ZananceStore>();
        var account = Account("Main");
        Assert.True(await store.SaveAccountAsync(account, Ct));
        Assert.True((await store.SaveEntryAsync(new LedgerEntry
        {
            AccountId = account.Id, Kind = EntryKind.Expense, Amount = 100, Date = new(2026, 10, 10),
        }, Ct)).Succeeded);
        var before = await AccountRowsAsync(store);
        account.CurrencyCode = "USD";
        Assert.False(await store.SaveAccountAsync(account, Ct));
        Assert.Equal(before, await AccountRowsAsync(store));
        Assert.Single(await store.GetEntriesAsync(cancellationToken: Ct));
    }

    /// <summary>Fictitious already-resolved facts; no store verification or customer/test entitlement is simulated.</summary>
    private sealed class AccessSource(string path, CapabilityContext context) : ICommercialWriteAccessSource
    {
        private readonly CommercialWriteAccess _access = new(path, context);
        private int _captures;
        /// <summary>Gets or sets the facts returned after the requested transition.</summary>
        public CommercialWriteAccess? ChangedAccess { get; init; }
        /// <summary>Gets or sets the number of initial stable captures.</summary>
        public int ChangeAfterCapture { get; init; } = int.MaxValue;
        /// <summary>Gets or sets a barrier making both contenders capture before either obtains the database writer.</summary>
        public Barrier? InitialCaptureBarrier { get; init; }
        /// <summary>Gets paths requested by actual context-bound writes.</summary>
        public ConcurrentBag<string> RequestedPaths { get; } = [];
        /// <summary>Gets the two initial contenders observed at the barrier.</summary>
        public int SynchronizedCaptures;
        /// <inheritdoc />
        public CommercialWriteAccess Capture(string databasePath)
        {
            RequestedPaths.Add(databasePath);
            var capture = Interlocked.Increment(ref _captures);
            if (InitialCaptureBarrier is { } barrier && capture <= 2)
            {
                Interlocked.Increment(ref SynchronizedCaptures);
                if (!barrier.SignalAndWait(TimeSpan.FromSeconds(15), Ct)) throw new TimeoutException("The two writer contenders did not rendezvous.");
            }
            return capture > ChangeAfterCapture ? ChangedAccess! : _access;
        }
    }
}
