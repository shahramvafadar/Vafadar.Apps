using Vafadar.Zanance.Core.Commerce;

namespace Vafadar.Zanance.Core.Tests.Commerce;

/// <summary>AT-133: explicit downgrade choices retain history, state, identities and exact capability boundaries.</summary>
[Trait("AT", "AT-133")]
public sealed class ResourceSelectionTests
{
    private static readonly Guid ProfileId = Guid.Parse("11111111-2222-3333-4444-555555555555");
    private static readonly QuotaScope Profile = new(QuotaScopeKind.PersonalProfile, ProfileId);
    private static CapabilityContext Context(ProductPlan plan = ProductPlan.Free) => new(plan, new(EntitlementScopeKind.PersonalProfile, ProfileId));
    private static QuotaItem Item(QuotaKind kind, QuotaItemState state = QuotaItemState.Active, QuotaScope? scope = null) =>
        new(kind, scope ?? Profile, Guid.NewGuid(), state);

    [Theory]
    [InlineData(QuotaKind.FinancialAccounts, 4)]
    [InlineData(QuotaKind.Goals, 2)]
    [InlineData(QuotaKind.RecurringPlans, 6)]
    [InlineData(QuotaKind.QuickTemplates, 4)]
    [InlineData(QuotaKind.SavedFilters, 2)]
    public void An_over_quota_downgrade_never_guesses_the_first_or_last_active_items(QuotaKind kind, int count)
    {
        var items = Enumerable.Range(0, count).Select(_ => Item(kind)).ToArray();
        var result = ResourceSelectionPolicy.Resolve(kind, Profile, Context(), items.Reverse());
        Assert.True(result.RequiresSelection); Assert.Equal(0, result.SelectedCount);
        Assert.Equal(items.Select(item => item.Id).Order(), result.Items.Select(item => item.Id));
        Assert.All(result.Items, item => Assert.Equal(QuotaItemState.ReadOnly, item.State));
        Assert.All(items, item => Assert.Equal(QuotaItemState.Active, item.State));
    }

    [Theory]
    [InlineData(QuotaItemState.Active)]
    [InlineData(QuotaItemState.Paused)]
    public void Paused_goals_keep_their_slot_and_history_is_not_activated_or_archived(QuotaItemState state)
    {
        var chosen = Item(QuotaKind.Goals, state); var other = Item(QuotaKind.Goals);
        var completed = Item(QuotaKind.Goals, QuotaItemState.Ended); var archived = Item(QuotaKind.Goals, QuotaItemState.Archived);
        var choice = new ResourceSelection(QuotaKind.Goals, Profile, [chosen.Id]);
        var result = ResourceSelectionPolicy.Resolve(QuotaKind.Goals, Profile, Context(), [other, completed, chosen, archived], choice);
        Assert.False(result.RequiresSelection); Assert.Equal(1, result.SelectedCount); Assert.True(result.IsSelected(chosen.Id));
        Assert.Equal(state, result.Items.Single(item => item.Id == chosen.Id).State);
        Assert.Equal(QuotaItemState.ReadOnly, result.Items.Single(item => item.Id == other.Id).State);
        Assert.Equal(QuotaItemState.Ended, result.Items.Single(item => item.Id == completed.Id).State);
        Assert.Equal(QuotaItemState.Archived, result.Items.Single(item => item.Id == archived.Id).State);
    }

    [Fact]
    public void An_explicit_empty_or_smaller_choice_is_preserved_even_when_more_slots_are_available()
    {
        var items = Enumerable.Range(0, 3).Select(_ => Item(QuotaKind.FinancialAccounts)).ToArray();
        foreach (var ids in new[] { Array.Empty<Guid>(), new[] { items[2].Id } })
        {
            var result = ResourceSelectionPolicy.Resolve(QuotaKind.FinancialAccounts, Profile, Context(), items,
                new(QuotaKind.FinancialAccounts, Profile, ids));
            Assert.False(result.RequiresSelection); Assert.Equal(ids.Length, result.SelectedCount);
            Assert.Equal(ids.Order(), result.Items.Where(item => result.IsSelected(item.Id)).Select(item => item.Id));
        }
    }

    [Fact]
    public void Without_a_choice_all_original_usable_items_fit_their_available_capacity()
    {
        var active = Item(QuotaKind.RecurringPlans); var paused = Item(QuotaKind.RecurringPlans, QuotaItemState.Paused);
        var ended = Item(QuotaKind.RecurringPlans, QuotaItemState.Ended);
        var result = ResourceSelectionPolicy.Resolve(QuotaKind.RecurringPlans, Profile, Context(), [active, paused, ended]);
        Assert.False(result.RequiresSelection); Assert.Equal(2, result.SelectedCount);
        Assert.Equal(QuotaItemState.Paused, result.Items.Single(item => item.Id == paused.Id).State);
        Assert.False(result.IsSelected(ended.Id));
    }

    [Theory]
    [InlineData(ProductPlan.Plus)]
    [InlineData(ProductPlan.Pro)]
    public void An_upgrade_restores_original_usable_items_without_rewriting_a_previous_limited_choice(ProductPlan plan)
    {
        var items = Enumerable.Range(0, 5).Select(_ => Item(QuotaKind.Goals)).Append(Item(QuotaKind.Goals, QuotaItemState.Archived)).ToArray();
        var choice = new ResourceSelection(QuotaKind.Goals, Profile, [Guid.NewGuid()]);
        var result = ResourceSelectionPolicy.Resolve(QuotaKind.Goals, Profile, Context(plan), items, choice);
        Assert.Null(result.Maximum); Assert.False(result.RequiresSelection); Assert.Equal(5, result.SelectedCount);
        Assert.Equal(QuotaItemState.Archived, result.Items.Single(item => item.Id == items[^1].Id).State);
        Assert.Single(choice.SelectedIds);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Stale_archived_ended_or_unknown_choices_need_review_without_silently_choosing_replacements(int stale)
    {
        var active = Item(QuotaKind.Goals);
        var old = Item(QuotaKind.Goals, stale == 0 ? QuotaItemState.Archived : QuotaItemState.Ended);
        var selected = stale == 2 ? Guid.NewGuid() : old.Id;
        var result = ResourceSelectionPolicy.Resolve(QuotaKind.Goals, Profile, Context(), [active, old], new(QuotaKind.Goals, Profile, [selected]));
        Assert.True(result.RequiresSelection); Assert.False(result.IsSelected(active.Id));
        Assert.Equal(old.State, result.Items.Single(item => item.Id == old.Id).State);
    }

    [Fact]
    public void A_too_large_previous_choice_needs_review_instead_of_automatic_truncation()
    {
        var items = Enumerable.Range(0, 4).Select(_ => Item(QuotaKind.FinancialAccounts)).ToArray();
        var result = ResourceSelectionPolicy.Resolve(QuotaKind.FinancialAccounts, Profile, Context(), items,
            new(QuotaKind.FinancialAccounts, Profile, items.Select(item => item.Id)));
        Assert.True(result.RequiresSelection); Assert.Equal(0, result.SelectedCount); Assert.Equal(4, result.Items.Count);
    }

    [Fact]
    public void Choices_are_immutable_canonical_values_in_cached_access_snapshots()
    {
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid() }; var expected = ids.ToArray();
        var original = new ResourceSelection(QuotaKind.FinancialAccounts, Profile, ids);
        ids[0] = Guid.NewGuid();
        var equivalent = new ResourceSelection(QuotaKind.FinancialAccounts, Profile, [expected[1], expected[0], expected[0]]);
        Assert.Equal(original, equivalent); Assert.Equal(original.GetHashCode(), equivalent.GetHashCode());
        Assert.Equal(expected.Order(), original.SelectedIds);
        Assert.Throws<NotSupportedException>(() => ((IList<Guid>)original.SelectedIds)[0] = Guid.NewGuid());
        Assert.Equal(expected.Order(), original.SelectedIds);
    }

    [Fact]
    public void Conflicting_duplicate_or_already_derived_states_are_rejected()
    {
        var item = Item(QuotaKind.Goals);
        Assert.Throws<ArgumentException>(() => ResourceSelectionPolicy.Resolve(QuotaKind.Goals, Profile, Context(),
            [item, new(QuotaKind.Goals, Profile, item.Id, QuotaItemState.Archived)]));
        Assert.Throws<ArgumentException>(() => ResourceSelectionPolicy.Resolve(QuotaKind.Goals, Profile, Context(),
            [new(QuotaKind.Goals, Profile, item.Id, QuotaItemState.ReadOnly)]));
        Assert.Equal(1, ResourceSelectionPolicy.Resolve(QuotaKind.Goals, Profile, Context(), [item, item]).SelectedCount);
    }

    [Fact]
    public void Foreign_profile_resources_and_other_kinds_do_not_consume_or_receive_selected_slots()
    {
        var own = Item(QuotaKind.FinancialAccounts);
        var foreign = Item(QuotaKind.FinancialAccounts, scope: new(QuotaScopeKind.PersonalProfile, Guid.NewGuid()));
        var otherKind = Item(QuotaKind.Goals);
        var result = ResourceSelectionPolicy.Resolve(QuotaKind.FinancialAccounts, Profile, Context(), [own, foreign, otherKind]);
        Assert.Equal(own.Id, Assert.Single(result.Items).Id); Assert.True(result.IsSelected(own.Id));
        Assert.False(result.IsSelected(foreign.Id)); Assert.False(result.IsSelected(otherKind.Id));
    }

    [Fact]
    public void Capability_and_selection_boundaries_must_match_even_for_unlimited_plans()
    {
        var space = new QuotaScope(QuotaScopeKind.SharedSpace, ProfileId);
        Assert.Throws<ArgumentException>(() => ResourceSelectionPolicy.Resolve(QuotaKind.FinancialAccounts, space, Context(ProductPlan.Pro), []));
        Assert.Throws<ArgumentException>(() => ResourceSelectionPolicy.Resolve(QuotaKind.FinancialAccounts, Profile, Context(ProductPlan.Pro), [],
            new(QuotaKind.FinancialAccounts, space, [])));
        Assert.Throws<ArgumentException>(() => ResourceSelectionPolicy.Resolve(QuotaKind.FinancialAccounts, Profile, Context(), [],
            new(QuotaKind.Goals, Profile, [])));
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void Personal_Pro_or_explicit_ids_never_substitute_for_current_shared_membership_and_host_rights(bool member, bool host)
    {
        var scope = new QuotaScope(QuotaScopeKind.SharedSpace, ProfileId);
        var context = new CapabilityContext(ProductPlan.Pro, new(EntitlementScopeKind.SharedSpace, ProfileId), new(ProfileId, member, host));
        var item = Item(QuotaKind.FinancialAccounts, scope: scope);
        var result = ResourceSelectionPolicy.Resolve(QuotaKind.FinancialAccounts, scope, context, [item], new(QuotaKind.FinancialAccounts, scope, [item.Id]));
        Assert.Equal(0, result.Maximum); Assert.False(result.IsSelected(item.Id)); Assert.False(result.RequiresSelection);
        Assert.Equal(QuotaItemState.ReadOnly, Assert.Single(result.Items).State);
    }

    [Fact]
    public void Accepted_guests_get_unlimited_local_items_only_inside_their_exact_active_space()
    {
        var scope = new QuotaScope(QuotaScopeKind.SharedSpace, ProfileId);
        var context = new CapabilityContext(ProductPlan.Free, new(EntitlementScopeKind.SharedSpace, ProfileId), new(ProfileId, true, true));
        var items = Enumerable.Range(0, 8).Select(_ => Item(QuotaKind.FinancialAccounts, scope: scope)).ToArray();
        Assert.Equal(8, ResourceSelectionPolicy.Resolve(QuotaKind.FinancialAccounts, scope, context, items).SelectedCount);
        Assert.Throws<ArgumentException>(() => ResourceSelectionPolicy.Resolve(QuotaKind.LocalProfiles,
            new(QuotaScopeKind.Device, Guid.NewGuid()), context, []));
    }

    [Fact]
    public void Personal_profile_selection_counts_in_the_device_boundary_not_the_financial_profile()
    {
        var device = new QuotaScope(QuotaScopeKind.Device, Guid.NewGuid());
        var items = Enumerable.Range(0, 3).Select(_ => Item(QuotaKind.LocalProfiles, scope: device)).ToArray();
        var result = ResourceSelectionPolicy.Resolve(QuotaKind.LocalProfiles, device, Context(), items,
            new(QuotaKind.LocalProfiles, device, [items[1].Id]));
        Assert.Equal(1, result.SelectedCount); Assert.True(result.IsSelected(items[1].Id));
        Assert.Throws<ArgumentException>(() => new ResourceSelection(QuotaKind.LocalProfiles, Profile, []));
    }

    [Fact]
    public void A_choice_is_not_a_paid_feature_grant_and_never_selects_budget_rows_or_member_seats()
    {
        var item = Item(QuotaKind.FinancialAccounts);
        Assert.True(ResourceSelectionPolicy.Resolve(QuotaKind.FinancialAccounts, Profile, Context(), [item]).IsSelected(item.Id));
        Assert.Equal(FeaturePermission.RequiresPlus, PlanPolicy.Check(CommercialFeature.ReceiptReading, Context()));
        Assert.Throws<ArgumentException>(() => new ResourceSelection(QuotaKind.BudgetDefinitions, Profile, []));
        Assert.Throws<ArgumentException>(() => new ResourceSelection(QuotaKind.SharedMembers, new(QuotaScopeKind.SharedSpace, ProfileId), []));
        Assert.Throws<ArgumentException>(() => new ResourceSelection(QuotaKind.HostedSpaces, new(QuotaScopeKind.OnlineIdentity, ProfileId), []));
        Assert.Throws<ArgumentException>(() => new ResourceSelection(QuotaKind.FinancialAccounts, Profile, [Guid.Empty]));
    }
}
