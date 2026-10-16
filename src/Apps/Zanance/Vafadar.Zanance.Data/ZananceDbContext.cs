using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Vafadar.Data;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Core.Rates;
using Vafadar.Zanance.Core.Settings;

namespace Vafadar.Zanance.Data;

/// <summary>
/// The Zanance app's on-device database.
/// </summary>
/// <remarks>
/// Every schema change needs a migration:
/// <c>dotnet ef migrations add &lt;Name&gt; --project src/Apps/Zanance/Vafadar.Zanance.Data</c>.
/// Migrations must keep existing user data (AT-60).
/// </remarks>
public sealed partial class ZananceDbContext : LocalDbContext
{
    /// <summary>Configures the original context and closed-connection write function without opening storage or reading data during construction.</summary>
    public ZananceDbContext(DbContextOptions<ZananceDbContext> options, TimeProvider? time = null) : base(options)
    {
        WorkClock = time ?? TimeProvider.System;
        if (Database.GetDbConnection() is SqliteConnection sqlite)
            sqlite.CreateFunction<string, string, string, long>("zanance_household_write", DemandHouseholdFinancialWrite);
    }
    /// <summary>Actual injected operation clock; commercial/test flags never replace household expiry authority.</summary>
    internal TimeProvider WorkClock { get; }
    /// <summary>Gets profile-local explicit financial resource choices, independent of verified paid facts.</summary>
    internal DbSet<Commerce.StoredResourceSelection> ResourceSelections => Set<Commerce.StoredResourceSelection>();

    /// <summary>Gets the accounts.</summary>
    public DbSet<Account> Accounts => Set<Account>();

    /// <summary>Gets the categories.</summary>
    public DbSet<Category> Categories => Set<Category>();

    /// <summary>Gets the ledger entries.</summary>
    public DbSet<LedgerEntry> Entries => Set<LedgerEntry>();

    /// <summary>Gets durable Undo records for imports that explicitly replace aggregate amounts.</summary>
    public DbSet<Importing.ImportLinkBatch> ImportLinks => Set<Importing.ImportLinkBatch>();

    /// <summary>Gets the plans.</summary>
    public DbSet<Schedule> Schedules => Set<Schedule>();

    /// <summary>Gets the stored states of plan occurrences.</summary>
    public DbSet<OccurrenceState> OccurrenceStates => Set<OccurrenceState>();

    /// <summary>Gets the budgets.</summary>
    public DbSet<Budget> Budgets => Set<Budget>();

    /// <summary>Gets the manual exchange rates.</summary>
    public DbSet<ExchangeRate> ExchangeRates => Set<ExchangeRate>();

    /// <summary>Gets the saved transaction list filters (REP-08).</summary>
    public DbSet<SavedFilter> SavedFilters => Set<SavedFilter>();

    /// <summary>Gets the settings (one row).</summary>
    public DbSet<ZananceSettings> Settings => Set<ZananceSettings>();

    /// <summary>Gets the quick entry templates (TX-04).</summary>
    public DbSet<EntryTemplate> Templates => Set<EntryTemplate>();

    /// <summary>Gets the savings goals (F2-GOAL-01).</summary>
    public DbSet<Goal> Goals => Set<Goal>();

    /// <summary>Gets the money earmarked for goals (F2-GOAL-02).</summary>
    public DbSet<GoalAllocation> GoalAllocations => Set<GoalAllocation>();

    /// <summary>Gets the contribution plans of goals (ZEX-GO08).</summary>
    public DbSet<ContributionPlan> ContributionPlans => Set<ContributionPlan>();

    /// <summary>Gets the asset types of quantity holdings (ZEX-AS02).</summary>
    public DbSet<Core.Holdings.AssetType> AssetTypes => Set<Core.Holdings.AssetType>();

    /// <summary>Gets the places where holdings are kept.</summary>
    public DbSet<Core.Holdings.AssetLocation> AssetLocations => Set<Core.Holdings.AssetLocation>();

    /// <summary>Gets the changes of holdings.</summary>
    public DbSet<Core.Holdings.AssetEvent> AssetEvents => Set<Core.Holdings.AssetEvent>();

    /// <summary>Gets the dated prices of asset types.</summary>
    public DbSet<Core.Holdings.AssetValuation> AssetValuations => Set<Core.Holdings.AssetValuation>();

    /// <summary>Gets the saved forecasts (ZEX-S0803); read-only after saving.</summary>
    public DbSet<Core.Forecasts.ForecastSnapshot> ForecastSnapshots => Set<Core.Forecasts.ForecastSnapshot>();

    /// <summary>Gets the local categorization rules (F2-TX-04).</summary>
    public DbSet<CategoryRule> CategoryRules => Set<CategoryRule>();

    /// <summary>Gets the receipt photos and documents of entries (F2-TX-04).</summary>
    public DbSet<EntryAttachment> Attachments => Set<EntryAttachment>();

    /// <summary>Gets encrypted household financial contacts; none is an authentication or membership grant.</summary>
    public DbSet<Core.Sharing.HouseholdContact> HouseholdContacts => Set<Core.Sharing.HouseholdContact>();
    /// <summary>Gets actual shared costs/refunds, counted once independently of their allocations or linked cash entry.</summary>
    public DbSet<Core.Sharing.HouseholdCost> HouseholdCosts => Set<Core.Sharing.HouseholdCost>();
    /// <summary>Gets exact minor-unit shares of one shared cost.</summary>
    public DbSet<Core.Sharing.HouseholdCostAllocation> HouseholdAllocations => Set<Core.Sharing.HouseholdCostAllocation>();
    /// <summary>Gets explicit actual private repayments; saving one never manufactures a ledger transfer.</summary>
    public DbSet<Core.Sharing.HouseholdSettlement> HouseholdSettlements => Set<Core.Sharing.HouseholdSettlement>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ZananceDbContext).Assembly);
    }
}
