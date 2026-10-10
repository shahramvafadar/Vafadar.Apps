using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Plans;

namespace Vafadar.Zanance.Core.Commerce;

/// <summary>The identity domain in which a quota is counted.</summary>
public enum QuotaScopeKind
{
    /// <summary>Personal financial resources in one profile.</summary>
    PersonalProfile,
    /// <summary>Local profiles on one device.</summary>
    Device,
    /// <summary>Hosted spaces of one online identity.</summary>
    OnlineIdentity,
    /// <summary>Financial resources or member seats in one shared space.</summary>
    SharedSpace,
}

/// <summary>Explicit quota boundary; an account, device, online identity and space never substitute for each other.</summary>
public sealed record QuotaScope
{
    /// <summary>Creates a typed, non-empty counting boundary.</summary>
    public QuotaScope(QuotaScopeKind kind, Guid id)
    {
        if (!Enum.IsDefined(kind)) throw new ArgumentOutOfRangeException(nameof(kind));
        if (id == Guid.Empty) throw new ArgumentException("A quota scope requires an identity.", nameof(id));
        Kind = kind;
        Id = id;
    }
    /// <summary>Gets the counting domain.</summary>
    public QuotaScopeKind Kind { get; }
    /// <summary>Gets the exact counting boundary.</summary>
    public Guid Id { get; }
}

/// <summary>Availability for new work, not whether historical data is accessible.</summary>
public enum QuotaItemState
{
    /// <summary>Usable for new work.</summary>
    Active,
    /// <summary>Temporarily paused, still consumes its slot (approved OD-03).</summary>
    Paused,
    /// <summary>Archived historical data.</summary>
    Archived,
    /// <summary>Completed goals or ended plans.</summary>
    Ended,
    /// <summary>Retained data not selected for new work after a downgrade/import.</summary>
    ReadOnly,
}

/// <summary>Immutable scoped resource projection; future services determine read-only selection, never this model.</summary>
public sealed record QuotaItem
{
    /// <summary>Creates a resource with a compatible counting boundary.</summary>
    public QuotaItem(QuotaKind kind, QuotaScope scope, Guid id, QuotaItemState state)
    {
        ArgumentNullException.ThrowIfNull(scope);
        if (!Enum.IsDefined(state)) throw new ArgumentOutOfRangeException(nameof(state));
        QuotaUsage.ValidateScope(kind, scope);
        if (kind is QuotaKind.BudgetDefinitions or QuotaKind.SharedMembers)
            throw new ArgumentException("Budgets and member seats require their dedicated counting projections.", nameof(kind));
        if (id == Guid.Empty) throw new ArgumentException("A quota item requires an identity.", nameof(id));
        if (state == QuotaItemState.Paused && kind is not (QuotaKind.Goals or QuotaKind.RecurringPlans))
            throw new ArgumentException("Only goals and plans have a quota-preserving pause.", nameof(state));
        Kind = kind;
        Scope = scope;
        Id = id;
        State = state;
    }
    /// <summary>Gets the resource kind.</summary>
    public QuotaKind Kind { get; }
    /// <summary>Gets the counting boundary.</summary>
    public QuotaScope Scope { get; }
    /// <summary>Gets the stable resource identity.</summary>
    public Guid Id { get; }
    /// <summary>Gets availability for new work.</summary>
    public QuotaItemState State { get; }
    /// <summary>Projects the actual account archive state without changing the entity.</summary>
    public static QuotaItem From(QuotaScope scope, Account account)
    {
        ArgumentNullException.ThrowIfNull(account);
        return new(QuotaKind.FinancialAccounts, scope, account.Id, account.IsArchived ? QuotaItemState.Archived : QuotaItemState.Active);
    }
    /// <summary>Projects actual goal state; a paused goal still counts.</summary>
    public static QuotaItem From(QuotaScope scope, Goal goal)
    {
        ArgumentNullException.ThrowIfNull(goal);
        return new(QuotaKind.Goals, scope, goal.Id, goal.State switch
        {
            GoalState.Active => QuotaItemState.Active,
            GoalState.Paused => QuotaItemState.Paused,
            GoalState.Completed => QuotaItemState.Ended,
            GoalState.Archived => QuotaItemState.Archived,
            _ => throw new ArgumentOutOfRangeException(nameof(goal)),
        });
    }
    /// <summary>Projects actual schedule state; prior ended revisions do not consume a slot.</summary>
    public static QuotaItem From(QuotaScope scope, Schedule plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        return new(QuotaKind.RecurringPlans, scope, plan.Id, plan.State switch
        {
            ScheduleState.Active => QuotaItemState.Active,
            ScheduleState.Paused => QuotaItemState.Paused,
            ScheduleState.Ended => QuotaItemState.Ended,
            _ => throw new ArgumentOutOfRangeException(nameof(plan)),
        });
    }
}

/// <summary>Budget definition identity: financial scope, canonical account set, currency and period kind, never period date.</summary>
public sealed record BudgetDefinitionKey
{
    /// <summary>Creates the definition key; historical copies and account ordering do not create new definitions.</summary>
    public BudgetDefinitionKey(QuotaScope scope, string currencyCode, BudgetPeriod period, IEnumerable<Guid> accountIds)
    {
        ArgumentNullException.ThrowIfNull(scope);
        QuotaUsage.ValidateScope(QuotaKind.BudgetDefinitions, scope);
        ArgumentException.ThrowIfNullOrWhiteSpace(currencyCode);
        if (currencyCode.Length != 3 || currencyCode.Any(c => !char.IsAsciiLetter(c)))
            throw new ArgumentException("An ISO currency code has three Latin letters.", nameof(currencyCode));
        if (!Enum.IsDefined(period)) throw new ArgumentOutOfRangeException(nameof(period));
        ArgumentNullException.ThrowIfNull(accountIds);
        var ids = accountIds.Distinct().Order().ToArray();
        if (ids.Contains(Guid.Empty)) throw new ArgumentException("Account identities cannot be empty.", nameof(accountIds));
        Scope = scope;
        CurrencyCode = currencyCode.ToUpperInvariant();
        Period = period;
        AccountSet = string.Join(";", ids.Select(id => id.ToString("N")));
    }
    /// <summary>Gets the financial scope.</summary>
    public QuotaScope Scope { get; }
    /// <summary>Gets the normalized currency code.</summary>
    public string CurrencyCode { get; }
    /// <summary>Gets the period kind.</summary>
    public BudgetPeriod Period { get; }
    /// <summary>Gets a canonical set key; empty means all accounts under the existing budget scope rule.</summary>
    public string AccountSet { get; }
    /// <summary>Projects a real period row without counting its date, row id, limits or historical rollover as identity.</summary>
    public static BudgetDefinitionKey From(QuotaScope scope, Budget budget)
    {
        ArgumentNullException.ThrowIfNull(budget);
        return new(scope, budget.CurrencyCode, budget.Period, budget.AccountIds);
    }
}

/// <summary>A budget period's definition and explicit active-definition selection, supplied by the future service.</summary>
/// <param name="Definition">The complete definition identity.</param>
/// <param name="Active">Whether this definition is selected for new work.</param>
public sealed record BudgetUsage(BudgetDefinitionKey Definition, bool Active);

/// <summary>Membership seat state; invitations do not themselves authorize financial access.</summary>
public enum SharedSeatState
{
    /// <summary>An accepted owner/member, counted once.</summary>
    Active,
    /// <summary>An invitation reserving a seat until its supplied expiry.</summary>
    Pending,
    /// <summary>A departed member or withdrawn invitation.</summary>
    Left,
}

/// <summary>Scoped member/invitation facts with an explicit exclusive pending expiry.</summary>
public sealed record SharedSeat
{
    /// <summary>Creates a seat; a pending invitation requires an expiry supplied by the invitation service.</summary>
    public SharedSeat(QuotaScope scope, Guid identityId, SharedSeatState state, DateTimeOffset? expiresAt = null)
    {
        ArgumentNullException.ThrowIfNull(scope);
        QuotaUsage.ValidateScope(QuotaKind.SharedMembers, scope);
        if (identityId == Guid.Empty) throw new ArgumentException("A seat requires an identity.", nameof(identityId));
        if (!Enum.IsDefined(state)) throw new ArgumentOutOfRangeException(nameof(state));
        if ((state == SharedSeatState.Pending) != (expiresAt is not null))
            throw new ArgumentException("Only pending seats have an explicit expiry.", nameof(expiresAt));
        Scope = scope;
        IdentityId = identityId;
        State = state;
        ExpiresAt = expiresAt?.ToUniversalTime();
    }
    /// <summary>Gets the exact space.</summary>
    public QuotaScope Scope { get; }
    /// <summary>Gets the identity, independent of its devices.</summary>
    public Guid IdentityId { get; }
    /// <summary>Gets the seat state.</summary>
    public SharedSeatState State { get; }
    /// <summary>Gets the pending expiry, never an inferred grant of access.</summary>
    public DateTimeOffset? ExpiresAt { get; }
}

/// <summary>Pure, scope-bound counting without writing, enforcing limits or hiding retained data.</summary>
public static class QuotaUsage
{
    /// <summary>Validates the counting domain; reject financial accounts mistaken for profiles or online identities.</summary>
    public static void ValidateScope(QuotaKind kind, QuotaScope scope)
    {
        ArgumentNullException.ThrowIfNull(scope);
        if (!Enum.IsDefined(kind)) throw new ArgumentOutOfRangeException(nameof(kind));
        var valid = kind switch
        {
            QuotaKind.LocalProfiles => scope.Kind == QuotaScopeKind.Device,
            QuotaKind.HostedSpaces => scope.Kind == QuotaScopeKind.OnlineIdentity,
            QuotaKind.SharedMembers => scope.Kind == QuotaScopeKind.SharedSpace,
            _ => scope.Kind is QuotaScopeKind.PersonalProfile or QuotaScopeKind.SharedSpace,
        };
        if (!valid) throw new ArgumentException("The resource and counting domain do not match.", nameof(scope));
    }

    /// <summary>Counts distinct usable resources in exactly this boundary; conflicting duplicate facts are rejected.</summary>
    public static int CountItems(QuotaKind kind, QuotaScope scope, IEnumerable<QuotaItem> items)
    {
        ValidateScope(kind, scope);
        ArgumentNullException.ThrowIfNull(items);
        if (kind is QuotaKind.BudgetDefinitions or QuotaKind.SharedMembers)
            throw new ArgumentException("Use the dedicated definition/seat counter.", nameof(kind));
        var states = new Dictionary<Guid, QuotaItemState>();
        foreach (var item in items)
        {
            ArgumentNullException.ThrowIfNull(item);
            if (item.Kind != kind || item.Scope != scope) continue;
            if (states.TryGetValue(item.Id, out var state) && state != item.State)
                throw new ArgumentException("Conflicting state for the same scoped resource.", nameof(items));
            states[item.Id] = item.State;
        }
        return states.Values.Count(state => state is QuotaItemState.Active or QuotaItemState.Paused);
    }

    /// <summary>Counts each active budget definition once across copied months and historical rows.</summary>
    public static int CountBudgets(QuotaScope scope, IEnumerable<BudgetUsage> periods)
    {
        ValidateScope(QuotaKind.BudgetDefinitions, scope);
        ArgumentNullException.ThrowIfNull(periods);
        var definitions = new HashSet<BudgetDefinitionKey>();
        foreach (var period in periods)
        {
            ArgumentNullException.ThrowIfNull(period);
            ArgumentNullException.ThrowIfNull(period.Definition);
            if (period.Active && period.Definition.Scope == scope) definitions.Add(period.Definition);
        }
        return definitions.Count;
    }

    /// <summary>Counts owner/members and unexpired invitations once; leaving or expiry frees a seat immediately.</summary>
    public static int CountMembers(QuotaScope scope, IEnumerable<SharedSeat> seats, DateTimeOffset instant)
    {
        ValidateScope(QuotaKind.SharedMembers, scope);
        ArgumentNullException.ThrowIfNull(seats);
        var states = new Dictionary<Guid, SharedSeat>();
        foreach (var seat in seats)
        {
            ArgumentNullException.ThrowIfNull(seat);
            if (seat.Scope != scope) continue;
            if (states.TryGetValue(seat.IdentityId, out var prior) && prior != seat)
                throw new ArgumentException("Conflicting state for the same scoped identity.", nameof(seats));
            states[seat.IdentityId] = seat;
        }
        return states.Values.Count(seat => seat.State == SharedSeatState.Active
            || (seat.State == SharedSeatState.Pending && instant < seat.ExpiresAt));
    }
}
