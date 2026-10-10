using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Core.Settings;

namespace Vafadar.Zanance.Core.Tests.Commerce;

/// <summary>AT-119: final OD-03 commercial policy, with no app enforcement or store/provider simulation.</summary>
[Trait("AT", "AT-119")]
public sealed class PlanPolicyTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 10, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid ProfileId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid SpaceId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly QuotaScope Profile = new(QuotaScopeKind.PersonalProfile, ProfileId);
    private static readonly QuotaScope Space = new(QuotaScopeKind.SharedSpace, SpaceId);

    // These acceptance sets follow the approved matrix, not the policy's dispatch implementation.
    private static readonly CommercialFeature[] FreeFeatures =
    [
        CommercialFeature.RegionalDisplay, CommercialFeature.Security, CommercialFeature.Appearance,
        CommercialFeature.Transactions, CommercialFeature.History, CommercialFeature.Corrections,
        CommercialFeature.Warnings, CommercialFeature.BackupRestore, CommercialFeature.DeleteData,
        CommercialFeature.BasicExport, CommercialFeature.CloudBackup, CommercialFeature.FinancialAccounts,
        CommercialFeature.BasicBudgets, CommercialFeature.BasicGoals, CommercialFeature.BasicPlans,
        CommercialFeature.PlanReminders, CommercialFeature.BasicReports, CommercialFeature.BasicForecast,
        CommercialFeature.FinancialMonthStart, CommercialFeature.QuickTemplates, CommercialFeature.EntryDocumentation,
        CommercialFeature.BulkCorrections, CommercialFeature.SavedFilters, CommercialFeature.HomeCustomization,
        CommercialFeature.DisplayUnits, CommercialFeature.TransfersRefundsReimbursements, CommercialFeature.AggregatedEntries,
        CommercialFeature.CsvImport, CommercialFeature.QuickAddWidget, CommercialFeature.ViewHoldings,
    ];
    private static readonly CommercialFeature[] PlusFeatures =
    [
        CommercialFeature.AdvancedBudgets, CommercialFeature.AdvancedGoals, CommercialFeature.AdvancedPlans,
        CommercialFeature.AdvancedReports, CommercialFeature.AdvancedPdf, CommercialFeature.AdvancedForecast,
        CommercialFeature.ManageHoldings, CommercialFeature.LoanAnalysis, CommercialFeature.SplitTransactions,
        CommercialFeature.CategorizationRules, CommercialFeature.ReceiptReading, CommercialFeature.ContributionReviewReminders,
    ];

    /// <summary>Every valid product/duration, both display modes and both financial contexts.</summary>
    public static TheoryData<ProductPlan, PurchaseKind?, BillingPeriod, ExperienceMode, bool> Contexts()
    {
        var result = new TheoryData<ProductPlan, PurchaseKind?, BillingPeriod, ExperienceMode, bool>();
        (ProductPlan Plan, PurchaseKind? Kind, BillingPeriod Period)[] products =
        [
            (ProductPlan.Free, null, BillingPeriod.None),
            (ProductPlan.Plus, PurchaseKind.Subscription, BillingPeriod.Month),
            (ProductPlan.Plus, PurchaseKind.Subscription, BillingPeriod.Year),
            (ProductPlan.Plus, PurchaseKind.Lifetime, BillingPeriod.None),
            (ProductPlan.Pro, PurchaseKind.Subscription, BillingPeriod.Month),
            (ProductPlan.Pro, PurchaseKind.Subscription, BillingPeriod.Year),
        ];
        foreach (var product in products)
        foreach (var mode in Enum.GetValues<ExperienceMode>())
        foreach (var shared in new[] { false, true }) result.Add(product.Plan, product.Kind, product.Period, mode, shared);
        return result;
    }

    private static CapabilityContext Context(ProductPlan plan, bool shared = false, SharedSpaceAccess? access = null) =>
        new(plan, new(shared ? EntitlementScopeKind.SharedSpace : EntitlementScopeKind.PersonalProfile, shared ? SpaceId : ProfileId),
            shared ? access ?? new(SpaceId, true, true) : access);

    private static Entitlement Grant(ProductPlan plan, PurchaseKind kind = PurchaseKind.Subscription,
        BillingPeriod period = BillingPeriod.Month, DateTimeOffset? end = null, bool revoked = false) =>
        new(plan, kind, period, EntitlementSource.GooglePlay, Now.AddDays(-1),
            kind == PurchaseKind.Lifetime ? null : end ?? Now.AddDays(30), "terms-1", revoked);

    [Theory]
    [MemberData(nameof(Contexts))]
    public void Approved_matrix_is_identical_for_purchase_duration_and_display_mode_in_its_exact_scope(
        ProductPlan plan, PurchaseKind? kind, BillingPeriod period, ExperienceMode mode, bool shared)
    {
        var resolved = EntitlementResolver.Resolve(kind is null ? [] : [Grant(plan, kind.Value, period)], Now);
        Assert.Equal(plan, resolved);
        var context = Context(resolved, shared);
        var checkedFeatures = new HashSet<CommercialFeature>();
        foreach (var feature in FreeFeatures)
        {
            Assert.Equal(FeaturePermission.Allowed, PlanPolicy.Check(feature, context));
            Assert.True(checkedFeatures.Add(feature));
        }
        foreach (var feature in PlusFeatures)
        {
            Assert.Equal(shared || plan != ProductPlan.Free ? FeaturePermission.Allowed : FeaturePermission.RequiresPlus,
                PlanPolicy.Check(feature, context));
            Assert.True(checkedFeatures.Add(feature));
        }
        foreach (var feature in new[] { CommercialFeature.PersonalSync, CommercialFeature.HostSharedSpace })
        {
            Assert.Equal(shared ? FeaturePermission.OutsideScope : plan == ProductPlan.Pro ? FeaturePermission.Allowed : FeaturePermission.RequiresPro,
                PlanPolicy.Check(feature, context));
            Assert.True(checkedFeatures.Add(feature));
        }
        foreach (var feature in new[] { CommercialFeature.JoinSharedSpace, CommercialFeature.SharedSpaceSync })
        {
            Assert.Equal(shared ? FeaturePermission.Allowed : FeaturePermission.OutsideScope, PlanPolicy.Check(feature, context));
            Assert.True(checkedFeatures.Add(feature));
        }
        foreach (var feature in new[] { CommercialFeature.AiCredit, CommercialFeature.TaxAddOn, CommercialFeature.BankConnection })
        {
            Assert.Equal(FeaturePermission.RequiresAddOn, PlanPolicy.Check(feature, context));
            Assert.True(checkedFeatures.Add(feature));
        }
        Assert.Equal(shared ? FeaturePermission.OutsideScope : FeaturePermission.Allowed,
            PlanPolicy.Check(CommercialFeature.LocalProfiles, context));
        Assert.True(checkedFeatures.Add(CommercialFeature.LocalProfiles));
        Assert.Equal(Enum.GetValues<CommercialFeature>().Order(), checkedFeatures.Order());
        // Presentation still keeps existing data accessible in both modes. It supplies no commercial grant.
        foreach (var feature in Enum.GetValues<Feature>()) Assert.True(FeaturePolicy.ShowsExisting(new ZananceSettings { Mode = mode }, feature, hasData: true));
    }

    [Fact]
    public void Lifetime_survives_Pro_expiry_and_subscription_cadence_does_not_change_Plus_capabilities()
    {
        var lifetime = Grant(ProductPlan.Plus, PurchaseKind.Lifetime, BillingPeriod.None);
        var pro = Grant(ProductPlan.Pro, end: Now.AddHours(1));
        Assert.Equal(ProductPlan.Pro, EntitlementResolver.Resolve([lifetime, pro], Now));
        Assert.Equal(ProductPlan.Plus, EntitlementResolver.Resolve([pro, lifetime], Now.AddHours(1)));
        Assert.Equal(ProductPlan.Plus, EntitlementResolver.Resolve([lifetime], Now.AddYears(100)));
        Assert.Equal("terms-1", lifetime.TermsVersion);
        Assert.Null(lifetime.ValidUntil);
    }

    [Fact]
    public void Validity_is_half_open_with_no_invented_grace_and_revocation_applies_only_to_that_grant()
    {
        var subscription = Grant(ProductPlan.Pro, end: Now.AddHours(1));
        Assert.False(subscription.IsValidAt(subscription.ValidFrom.AddTicks(-1)));
        Assert.True(subscription.IsValidAt(subscription.ValidFrom));
        Assert.True(subscription.IsValidAt(Now.ToOffset(TimeSpan.FromHours(5))));
        Assert.False(subscription.IsValidAt(Now.AddHours(1)));
        Assert.Equal(ProductPlan.Free, EntitlementResolver.Resolve([subscription], Now.AddHours(1)));
        var revoked = Grant(ProductPlan.Pro, revoked: true);
        Assert.Equal(ProductPlan.Plus, EntitlementResolver.Resolve([revoked, Grant(ProductPlan.Plus)], Now));
        Assert.Equal(ProductPlan.Free, EntitlementResolver.Resolve([], Now));
    }

    [Theory]
    [InlineData(ProductPlan.Free, PurchaseKind.Subscription, BillingPeriod.Month)]
    [InlineData(ProductPlan.Free, PurchaseKind.Lifetime, BillingPeriod.None)]
    [InlineData(ProductPlan.Pro, PurchaseKind.Lifetime, BillingPeriod.None)]
    [InlineData(ProductPlan.Plus, PurchaseKind.Lifetime, BillingPeriod.Year)]
    [InlineData(ProductPlan.Plus, PurchaseKind.Subscription, BillingPeriod.None)]
    [InlineData((ProductPlan)99, PurchaseKind.Subscription, BillingPeriod.Month)]
    [InlineData(ProductPlan.Plus, (PurchaseKind)99, BillingPeriod.Month)]
    [InlineData(ProductPlan.Plus, PurchaseKind.Subscription, (BillingPeriod)99)]
    public void Impossible_or_unknown_products_never_become_entitlements(ProductPlan plan, PurchaseKind kind, BillingPeriod period) =>
        Assert.ThrowsAny<ArgumentException>(() => Grant(plan, kind, period));

    [Fact]
    public void Missing_terms_source_or_validity_are_rejected()
    {
        Assert.Throws<ArgumentException>(() => new Entitlement(ProductPlan.Plus, PurchaseKind.Subscription, BillingPeriod.Month,
            EntitlementSource.GooglePlay, Now, null, "terms"));
        Assert.Throws<ArgumentException>(() => new Entitlement(ProductPlan.Plus, PurchaseKind.Subscription, BillingPeriod.Month,
            EntitlementSource.GooglePlay, Now, Now, "terms"));
        Assert.Throws<ArgumentException>(() => new Entitlement(ProductPlan.Plus, PurchaseKind.Lifetime, BillingPeriod.None,
            EntitlementSource.GooglePlay, Now, Now.AddDays(1), "terms"));
        Assert.ThrowsAny<ArgumentException>(() => new Entitlement(ProductPlan.Plus, PurchaseKind.Lifetime, BillingPeriod.None,
            EntitlementSource.GooglePlay, Now, null, " "));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Entitlement(ProductPlan.Plus, PurchaseKind.Lifetime, BillingPeriod.None,
            (EntitlementSource)99, Now, null, "terms"));
    }

    [Theory]
    [InlineData(ProductPlan.Free)]
    [InlineData(ProductPlan.Plus)]
    [InlineData(ProductPlan.Pro)]
    public void Personal_purchase_never_substitutes_for_matching_accepted_membership_and_active_host(ProductPlan plan)
    {
        SharedSpaceAccess?[] invalid = [null, new(Guid.NewGuid(), true, true), new(SpaceId, false, true)];
        foreach (var access in invalid)
        {
            var context = new CapabilityContext(plan, new(EntitlementScopeKind.SharedSpace, SpaceId), access);
            foreach (var feature in Enum.GetValues<CommercialFeature>())
                Assert.Equal(FeaturePermission.RequiresMembership, PlanPolicy.Check(feature, context));
            foreach (var kind in Enum.GetValues<QuotaKind>()) Assert.Equal(0, QuotaPolicy.Get(kind, context).Maximum);
        }
    }

    [Theory]
    [InlineData(ProductPlan.Free)]
    [InlineData(ProductPlan.Plus)]
    [InlineData(ProductPlan.Pro)]
    public void Expired_host_keeps_existing_data_eligible_without_personal_Pro_restarting_shared_work(ProductPlan personalPlan)
    {
        var context = Context(personalPlan, true, new(SpaceId, true, false));
        Assert.True(context.HasSharedMembership);
        Assert.False(context.HasSharedAccess);
        CommercialFeature[] retained = [CommercialFeature.RegionalDisplay, CommercialFeature.Security, CommercialFeature.Appearance,
            CommercialFeature.History, CommercialFeature.Corrections, CommercialFeature.Warnings, CommercialFeature.BackupRestore,
            CommercialFeature.DeleteData, CommercialFeature.BasicExport, CommercialFeature.CloudBackup,
            CommercialFeature.BasicReports, CommercialFeature.ViewHoldings];
        foreach (var feature in retained) Assert.Equal(FeaturePermission.Allowed, PlanPolicy.Check(feature, context));
        foreach (var feature in new[] { CommercialFeature.Transactions, CommercialFeature.AdvancedPlans, CommercialFeature.SharedSpaceSync })
            Assert.Equal(FeaturePermission.RequiresPro, PlanPolicy.Check(feature, context));
        foreach (var kind in Enum.GetValues<QuotaKind>()) Assert.Equal(0, QuotaPolicy.Get(kind, context).Maximum);
    }

    [Fact]
    public void Guest_Plus_never_spreads_to_a_personal_profile_even_if_the_membership_facts_are_supplied()
    {
        var grant = new SharedSpaceAccess(SpaceId, true, true);
        Assert.Equal(FeaturePermission.Allowed, PlanPolicy.Check(CommercialFeature.AdvancedGoals, Context(ProductPlan.Free, true, grant)));
        var personal = Context(ProductPlan.Free, access: grant);
        Assert.False(personal.HasSharedAccess);
        Assert.Equal(FeaturePermission.RequiresPlus, PlanPolicy.Check(CommercialFeature.AdvancedGoals, personal));
        Assert.Equal(FeaturePermission.RequiresPro, PlanPolicy.Check(CommercialFeature.PersonalSync, personal));
        Assert.Equal(3, QuotaPolicy.Get(QuotaKind.FinancialAccounts, personal).Maximum);
    }

    [Theory]
    [MemberData(nameof(Contexts))]
    public void Quotas_keep_device_identity_and_space_separate_in_every_valid_product_context(
        ProductPlan plan, PurchaseKind? kind, BillingPeriod period, ExperienceMode mode, bool shared)
    {
        _ = mode; // The actual policy has no presentation input.
        var context = Context(EntitlementResolver.Resolve(kind is null ? [] : [Grant(plan, kind.Value, period)], Now), shared);
        (QuotaKind Kind, int Free)[] local =
        [(QuotaKind.FinancialAccounts, 3), (QuotaKind.BudgetDefinitions, 1), (QuotaKind.Goals, 1),
            (QuotaKind.RecurringPlans, 5), (QuotaKind.QuickTemplates, 3), (QuotaKind.SavedFilters, 1)];
        foreach (var entry in local)
            Assert.Equal(shared || plan != ProductPlan.Free ? (int?)null : entry.Free, QuotaPolicy.Get(entry.Kind, context).Maximum);
        Assert.Equal(shared ? 0 : plan == ProductPlan.Free ? 1 : (int?)null, QuotaPolicy.Get(QuotaKind.LocalProfiles, context).Maximum);
        Assert.Equal(!shared && plan == ProductPlan.Pro ? 1 : 0, QuotaPolicy.Get(QuotaKind.HostedSpaces, context).Maximum);
        Assert.Equal(shared ? 6 : 0, QuotaPolicy.Get(QuotaKind.SharedMembers, context).Maximum);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(6)]
    public void Quota_boundary_batch_overflow_and_already_excess_data_do_not_mutate_usage(int limit)
    {
        var quota = new Quota(limit);
        Assert.True(quota.CanAdd(limit - 1));
        Assert.False(quota.CanAdd(limit));
        Assert.False(quota.CanAdd(limit - 1, 2));
        Assert.False(quota.CanAdd(int.MaxValue, int.MaxValue));
        Assert.True(quota.CanAdd(int.MaxValue, 0));
        Assert.True(new Quota(null).CanAdd(int.MaxValue, int.MaxValue));
        Assert.False(new Quota(0).CanAdd(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => quota.CanAdd(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => quota.CanAdd(0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Quota(-1));
    }

    [Fact]
    public void Actual_account_goal_and_plan_states_count_pause_but_not_archive_completion_or_ended_revisions()
    {
        var accounts = new[] { new Account { Name = "Main", CurrencyCode = "EUR" }, new Account { Name = "Archive", CurrencyCode = "EUR", IsArchived = true } };
        var goals = Enum.GetValues<GoalState>().Select(state => new Goal { Name = state.ToString(), CurrencyCode = "EUR", State = state }).ToArray();
        var plans = Enum.GetValues<ScheduleState>().Select(state => new Schedule { Name = state.ToString(), State = state }).ToArray();
        var items = accounts.Select(account => QuotaItem.From(Profile, account))
            .Concat(goals.Select(goal => QuotaItem.From(Profile, goal))).Concat(plans.Select(plan => QuotaItem.From(Profile, plan))).ToArray();
        Assert.Equal(1, QuotaUsage.CountItems(QuotaKind.FinancialAccounts, Profile, items));
        Assert.Equal(2, QuotaUsage.CountItems(QuotaKind.Goals, Profile, items));
        Assert.Equal(2, QuotaUsage.CountItems(QuotaKind.RecurringPlans, Profile, items));
        Assert.Equal(Enum.GetValues<GoalState>(), goals.Select(goal => goal.State));
        Assert.Equal(Enum.GetValues<ScheduleState>(), plans.Select(plan => plan.State));
        Assert.True(accounts[1].IsArchived);
    }

    [Fact]
    public void Shared_and_other_profile_accounts_never_consume_personal_slots_and_duplicate_reads_count_once()
    {
        var accountId = Guid.NewGuid();
        var local = new QuotaItem(QuotaKind.FinancialAccounts, Profile, accountId, QuotaItemState.Active);
        QuotaItem[] rows = [local, local, new(QuotaKind.FinancialAccounts, Space, accountId, QuotaItemState.Active),
            new(QuotaKind.FinancialAccounts, new(QuotaScopeKind.PersonalProfile, Guid.NewGuid()), Guid.NewGuid(), QuotaItemState.Active),
            new(QuotaKind.FinancialAccounts, Profile, Guid.NewGuid(), QuotaItemState.ReadOnly)];
        Assert.Equal(1, QuotaUsage.CountItems(QuotaKind.FinancialAccounts, Profile, rows));
        Assert.Equal(1, QuotaUsage.CountItems(QuotaKind.FinancialAccounts, Space, rows));
        Assert.Throws<ArgumentException>(() => QuotaUsage.CountItems(QuotaKind.FinancialAccounts, Profile,
            [local, new(QuotaKind.FinancialAccounts, Profile, accountId, QuotaItemState.Archived)]));
    }

    [Fact]
    public void Budget_copies_across_months_count_one_definition_with_canonical_accounts_and_independent_currency_period_scope()
    {
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        var october = new Budget { CurrencyCode = "EUR", Period = BudgetPeriod.Month, PeriodStart = new(2026, 10, 1), AccountIds = [first, second] };
        var november = new Budget { CurrencyCode = "eur", Period = BudgetPeriod.Month, PeriodStart = new(2026, 11, 1), AccountIds = [second, first, first] };
        var a = BudgetDefinitionKey.From(Profile, october);
        var b = BudgetDefinitionKey.From(Profile, november);
        Assert.Equal(a, b);
        Assert.Equal(1, QuotaUsage.CountBudgets(Profile, [new(a, true), new(b, true), new(a, false)]));
        Assert.Equal(0, QuotaUsage.CountBudgets(Profile, [new(a, false)]));
        BudgetUsage[] distinct = [new(a, true), new(new(Profile, "USD", BudgetPeriod.Month, [first, second]), true),
            new(new(Profile, "EUR", BudgetPeriod.Week, [first, second]), true), new(new(Profile, "EUR", BudgetPeriod.Month, []), true),
            new(new(Space, "EUR", BudgetPeriod.Month, [first, second]), true)];
        Assert.Equal(4, QuotaUsage.CountBudgets(Profile, distinct));
        Assert.Equal(1, QuotaUsage.CountBudgets(Space, distinct));
        Assert.Equal(new DateOnly(2026, 11, 1), november.PeriodStart);
        Assert.Equal([second, first, first], november.AccountIds);
    }

    [Fact]
    public void Device_profiles_and_hosted_spaces_use_their_own_domains_even_when_ids_coincide()
    {
        var device = new QuotaScope(QuotaScopeKind.Device, ProfileId);
        var identity = new QuotaScope(QuotaScopeKind.OnlineIdentity, ProfileId);
        QuotaItem[] rows = [new(QuotaKind.LocalProfiles, device, Guid.NewGuid(), QuotaItemState.Active),
            new(QuotaKind.HostedSpaces, identity, Guid.NewGuid(), QuotaItemState.Active)];
        Assert.Equal(1, QuotaUsage.CountItems(QuotaKind.LocalProfiles, device, rows));
        Assert.Equal(1, QuotaUsage.CountItems(QuotaKind.HostedSpaces, identity, rows));
        Assert.Throws<ArgumentException>(() => new QuotaItem(QuotaKind.LocalProfiles, Profile, Guid.NewGuid(), QuotaItemState.Active));
        Assert.Throws<ArgumentException>(() => new QuotaItem(QuotaKind.HostedSpaces, Space, Guid.NewGuid(), QuotaItemState.Active));
    }

    [Fact]
    public void Six_member_seats_include_owner_and_pending_invites_until_expiry_while_left_and_other_spaces_do_not_count()
    {
        var owner = new SharedSeat(Space, Guid.NewGuid(), SharedSeatState.Active);
        var expiry = Now.AddDays(7);
        var pending = Enumerable.Range(0, 5).Select(_ => new SharedSeat(Space, Guid.NewGuid(), SharedSeatState.Pending, expiry)).ToArray();
        SharedSeat[] rows = [owner, owner, .. pending, new(Space, Guid.NewGuid(), SharedSeatState.Left),
            new(new(QuotaScopeKind.SharedSpace, Guid.NewGuid()), Guid.NewGuid(), SharedSeatState.Active)];
        Assert.Equal(6, QuotaUsage.CountMembers(Space, rows, Now));
        Assert.False(QuotaPolicy.Get(QuotaKind.SharedMembers, Context(ProductPlan.Free, true)).CanAdd(6));
        Assert.Equal(6, QuotaUsage.CountMembers(Space, rows, expiry.AddTicks(-1)));
        Assert.Equal(1, QuotaUsage.CountMembers(Space, rows, expiry));
        Assert.Throws<ArgumentException>(() => QuotaUsage.CountMembers(Space,
            [owner, new(Space, owner.IdentityId, SharedSeatState.Left)], Now));
        Assert.Throws<ArgumentException>(() => new SharedSeat(Space, Guid.NewGuid(), SharedSeatState.Pending));
        Assert.Throws<ArgumentException>(() => new SharedSeat(Space, Guid.NewGuid(), SharedSeatState.Active, expiry));
    }

    [Fact]
    public void Invalid_enum_identity_currency_and_projection_inputs_fail_explicitly()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PlanPolicy.Check((CommercialFeature)99, Context(ProductPlan.Free)));
        Assert.Throws<ArgumentOutOfRangeException>(() => QuotaPolicy.Get((QuotaKind)99, Context(ProductPlan.Free)));
        Assert.Throws<ArgumentOutOfRangeException>(() => Context((ProductPlan)99));
        Assert.Throws<ArgumentException>(() => new EntitlementScope(EntitlementScopeKind.PersonalProfile, Guid.Empty));
        Assert.Throws<ArgumentException>(() => new QuotaScope(QuotaScopeKind.Device, Guid.Empty));
        Assert.Throws<ArgumentException>(() => new SharedSpaceAccess(Guid.Empty, true, true));
        Assert.Throws<ArgumentException>(() => new QuotaItem(QuotaKind.FinancialAccounts, Profile, Guid.NewGuid(), QuotaItemState.Paused));
        Assert.Throws<ArgumentException>(() => new BudgetDefinitionKey(Profile, "EU", BudgetPeriod.Month, []));
        Assert.Throws<ArgumentException>(() => new BudgetDefinitionKey(Profile, "EUR", BudgetPeriod.Month, [Guid.Empty]));
        Assert.Throws<ArgumentOutOfRangeException>(() => new BudgetDefinitionKey(Profile, "EUR", (BudgetPeriod)99, []));
        Assert.Throws<ArgumentOutOfRangeException>(() => QuotaItem.From(Profile, new Goal { Name = "Invalid", CurrencyCode = "EUR", State = (GoalState)99 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => QuotaItem.From(Profile, new Schedule { Name = "Invalid", State = (ScheduleState)99 }));
    }
}
