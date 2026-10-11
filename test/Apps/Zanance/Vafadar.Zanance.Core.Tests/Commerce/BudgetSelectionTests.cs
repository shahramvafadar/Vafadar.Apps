using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Commerce;

namespace Vafadar.Zanance.Core.Tests.Commerce;

/// <summary>AT-150: canonical budget choices and independent current new-work rights retain original periods.</summary>
[Trait("AT", "AT-150")]
public sealed class BudgetSelectionTests
{
    private static readonly Guid ProfileId = Guid.Parse("11111111-2222-3333-4444-555555555555");
    private static readonly QuotaScope Scope = new(QuotaScopeKind.PersonalProfile, ProfileId);
    private static readonly DateOnly Today = new(2026, 10, 12);
    private static CapabilityContext Context(ProductPlan plan = ProductPlan.Free) => new(plan, new(EntitlementScopeKind.PersonalProfile, ProfileId));
    private static Budget Budget(string currency = "EUR") => new() { CurrencyCode = currency, Year = 2026, Month = 10, TotalLimit = 12345 };

    [Fact]
    public void Canonical_definition_choice_ignores_calendar_copied_month_id_and_account_order()
    {
        var account = Guid.NewGuid(); var original = Budget(); original.AccountIds = [account, account];
        var copy = Budget("eur"); copy.AccountIds = [account]; copy.Month = 11; copy.Calendar = PeriodCalendar.Persian;
        var choice = new BudgetSelection(Scope, [BudgetDefinitionKey.From(Scope, original), BudgetDefinitionKey.From(Scope, copy)]);
        Assert.Single(choice.Definitions); Assert.Equal(choice, BudgetSelection.Parse(Scope, choice.Value));
        original.AccountIds.Clear(); Assert.Equal(account.ToString("N"), Assert.Single(choice.Definitions).AccountSet);
    }

    [Theory]
    [InlineData("eur|0|")]
    [InlineData("EUR|00|")]
    [InlineData("EUR|0|\nEUR|0|")]
    [InlineData("USD|0|\nEUR|0|")]
    [InlineData("EUR|999|")]
    public void Noncanonical_or_unknown_values_never_supply_an_implicit_choice(string value) =>
        Assert.ThrowsAny<ArgumentException>(() => BudgetSelection.Parse(Scope, value));

    [Fact]
    public void Missing_over_capacity_choice_never_selects_a_first_definition_or_mutates_original_limits()
    {
        var a = Budget(); var b = Budget("USD");
        var availability = BudgetSelectionPolicy.Resolve(Context(), Scope, [a, b], Today, 1);
        var work = BudgetWorkPolicy.Resolve(Context(), Scope, [a, b], Today, 1);
        Assert.True(availability.RequiresSelection); Assert.Empty(availability.SelectedDefinitions);
        Assert.False(work.CanUse(a.Id)); Assert.False(work.CanUse(b.Id)); Assert.True(work.CanCorrect);
        Assert.Equal(12345, a.TotalLimit); Assert.Equal(12345, b.TotalLimit);
    }

    [Theory]
    [InlineData(BudgetMethod.Envelopes, BudgetRollover.None, BudgetPeriod.Month)]
    [InlineData(BudgetMethod.Flex, BudgetRollover.None, BudgetPeriod.Month)]
    [InlineData(BudgetMethod.Limits, BudgetRollover.Surplus, BudgetPeriod.Month)]
    [InlineData(BudgetMethod.Limits, BudgetRollover.None, BudgetPeriod.Week)]
    public void A_selected_definition_never_grants_paid_methods_rollover_or_periods(BudgetMethod method, BudgetRollover rollover, BudgetPeriod period)
    {
        var original = Budget(); original.Method = method; original.Rollover = rollover; original.Period = period; original.PeriodStart = Today;
        var choice = new BudgetSelection(Scope, [BudgetDefinitionKey.From(Scope, original)]);
        var work = BudgetWorkPolicy.Resolve(Context(), Scope, [original], Today, 1, choice);
        Assert.False(work.CanUse(original.Id)); Assert.True(work.CanCorrect);
        Assert.True(BudgetWorkPolicy.Resolve(Context(ProductPlan.Plus), Scope, [original], Today, 1, choice).CanUse(original.Id));
    }

    [Fact]
    public void Shared_definition_selection_is_not_a_personal_membership_or_host_grant()
    {
        var scope = new QuotaScope(QuotaScopeKind.SharedSpace, ProfileId); var original = Budget();
        var choice = new BudgetSelection(scope, [BudgetDefinitionKey.From(scope, original)]);
        foreach (var (member, host) in new[] { (false, true), (true, false) })
        {
            var context = new CapabilityContext(ProductPlan.Pro, new(EntitlementScopeKind.SharedSpace, ProfileId), new(ProfileId, member, host));
            var work = BudgetWorkPolicy.Resolve(context, scope, [original], Today, 1, choice);
            Assert.False(work.CanUse(original.Id)); Assert.Equal(member, work.CanCorrect);
        }
        Assert.Throws<ArgumentException>(() => BudgetSelectionPolicy.Resolve(Context(), scope, [original], Today, 1, choice));
    }

    [Fact]
    public void A_future_copy_becomes_current_without_gaining_another_definition_or_automatic_choice()
    {
        var a = Budget(); var aNext = Budget(); aNext.Month = 11;
        var bNext = Budget("USD"); bNext.Month = 11;
        var choice = new BudgetSelection(Scope, [BudgetDefinitionKey.From(Scope, a)]);
        var nextDay = new DateOnly(2026, 11, 12);
        var work = BudgetWorkPolicy.Resolve(Context(), Scope, [a, aNext, bNext], nextDay, 1, choice);
        Assert.False(work.CanUse(a.Id)); Assert.True(work.CanUse(aNext.Id)); Assert.False(work.CanUse(bNext.Id));
        Assert.True(BudgetSelectionPolicy.Resolve(Context(), Scope, [a, aNext, bNext], nextDay, 1).RequiresSelection);
    }

    [Fact]
    public void Upgrade_ignores_old_limited_choices_and_explicit_empty_selection_retains_creation_capacity()
    {
        var a = Budget(); var b = Budget("USD"); var empty = new BudgetSelection(Scope, []);
        var free = BudgetWorkPolicy.Resolve(Context(), Scope, [a, b], Today, 1, empty);
        Assert.False(free.CanUse(a.Id)); Assert.True(free.CanCreate(BudgetMethod.Limits, BudgetRollover.None, BudgetPeriod.Month));
        var plus = BudgetWorkPolicy.Resolve(Context(ProductPlan.Plus), Scope, [a, b], Today, 1, empty);
        Assert.True(plus.CanUse(a.Id)); Assert.True(plus.CanUse(b.Id));
    }

    [Fact]
    public void Oversized_encoding_is_rejected_before_creating_an_unreadable_scoped_choice()
    {
        var budget = Budget(); budget.AccountIds = Enumerable.Range(0, 257).Select(_ => Guid.NewGuid()).ToList();
        Assert.Throws<ArgumentException>(() => new BudgetSelection(Scope, [BudgetDefinitionKey.From(Scope, budget)]));
        Assert.Throws<ArgumentException>(() => BudgetSelection.Parse(Scope, new string('x', 8448)));
    }
}
