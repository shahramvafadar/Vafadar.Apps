using Microsoft.EntityFrameworkCore;
using Vafadar.Data;
using Vafadar.Finance.Core.Accounts;
using Vafadar.Finance.Core.Budgets;
using Vafadar.Finance.Core.Categories;
using Vafadar.Finance.Core.Goals;
using Vafadar.Finance.Core.Ledger;
using Vafadar.Finance.Core.Plans;
using Vafadar.Finance.Core.Rates;
using Vafadar.Finance.Core.Settings;

namespace Vafadar.Finance.Data;

/// <summary>
/// The Finance app's on-device database.
/// </summary>
/// <remarks>
/// Every schema change needs a migration:
/// <c>dotnet ef migrations add &lt;Name&gt; --project src/Apps/Finance/Vafadar.Finance.Data</c>.
/// Migrations must keep existing user data (AT-60).
/// </remarks>
public sealed class FinanceDbContext(DbContextOptions<FinanceDbContext> options) : LocalDbContext(options)
{
    /// <summary>Gets the accounts.</summary>
    public DbSet<Account> Accounts => Set<Account>();

    /// <summary>Gets the categories.</summary>
    public DbSet<Category> Categories => Set<Category>();

    /// <summary>Gets the ledger entries.</summary>
    public DbSet<LedgerEntry> Entries => Set<LedgerEntry>();

    /// <summary>Gets the plans.</summary>
    public DbSet<Schedule> Schedules => Set<Schedule>();

    /// <summary>Gets the stored states of plan occurrences.</summary>
    public DbSet<OccurrenceState> OccurrenceStates => Set<OccurrenceState>();

    /// <summary>Gets the budgets.</summary>
    public DbSet<Budget> Budgets => Set<Budget>();

    /// <summary>Gets the manual exchange rates.</summary>
    public DbSet<ExchangeRate> ExchangeRates => Set<ExchangeRate>();

    /// <summary>Gets the settings (one row).</summary>
    public DbSet<FinanceSettings> Settings => Set<FinanceSettings>();

    /// <summary>Gets the quick entry templates (TX-04).</summary>
    public DbSet<EntryTemplate> Templates => Set<EntryTemplate>();

    /// <summary>Gets the savings goals (F2-GOAL-01).</summary>
    public DbSet<Goal> Goals => Set<Goal>();

    /// <summary>Gets the money earmarked for goals (F2-GOAL-02).</summary>
    public DbSet<GoalAllocation> GoalAllocations => Set<GoalAllocation>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FinanceDbContext).Assembly);
    }
}
