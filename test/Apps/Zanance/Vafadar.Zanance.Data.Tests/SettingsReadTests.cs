using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Testing;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data.Tests;

/// <summary>Cold settings reads retain complete original values, row ownership and command cancellation.</summary>
[Trait("AT", "AT-363")]
public sealed class SettingsReadTests : IDisposable
{
    private readonly TemporaryDirectory directory = new();
    private readonly ServiceProvider services;
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private string Path => directory.Combine("settings.db");
    private IDbContextFactory<ZananceDbContext> Factory => services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
    private ZananceStore Store => services.GetRequiredService<ZananceStore>();

    /// <summary>Uses the original registration, model, migrations, writer and isolated SQLite file.</summary>
    public SettingsReadTests()
    {
        services = new ServiceCollection().AddZananceData(Path).BuildServiceProvider();
        services.MigrateLocalDatabase<ZananceDbContext>();
    }

    /// <summary>Every persisted property is compared with an independent original EF materialization.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Complete_settings_match_original_ef_including_nulls_and_exact_identity(bool populated)
    {
        await using (var db = await Factory.CreateDbContextAsync(Ct))
        {
            var account = new Account { Name = "Fictitious settings account", CurrencyCode = "EUR", OpeningDate = new(2026, 10, 19) };
            db.Accounts.Add(account);
            db.Settings.Add(new ZananceSettings
            {
                AppLockEnabled = populated, BudgetCalendar = (PeriodCalendar)2, DefaultAccountId = populated ? account.Id : null,
                DefaultCurrencyCode = "USD", ReportCurrencyCode = "CHF", DisplayUnits = populated ? "XAU|Gold|3" : null,
                EssentialEstimate = populated ? long.MinValue + 2 : null, EssentialEstimateCurrency = populated ? "EUR" : null,
                EssentialEstimatePeriod = (EstimatePeriod)2, FortnightStart = populated ? new DateOnly(2000, 2, 29) : null,
                HomeBudgetCurrencyCode = populated ? "CAD" : null, HomeLayout = populated ? "balance,accounts" : null,
                Mode = ExperienceMode.Advanced, MonthStartDay = 28, NotificationsShowDetails = populated, OnboardingCompleted = populated,
                RateFreshnessDays = 90, ReminderDaysBefore = 11, ReminderTime = new TimeOnly(23, 59, 58).Add(TimeSpan.FromTicks(1234)),
                ReviewProgress = populated ? "2026-10:unreviewed,plans\nquoted\"" : null,
                ReviewReminderEnabled = populated, ValuationCurrencyEnabled = !populated
            });
            await db.SaveChangesAsync(Ct);
        }
        await using var oracle = await Factory.CreateDbContextAsync(Ct);
        var expected = await oracle.Settings.AsNoTracking().OrderBy(s => s.CreatedAt).FirstAsync(Ct);
        var actual = await Store.GetSettingsAsync(Ct);
        foreach (var property in typeof(ZananceSettings).GetProperties())
            Assert.Equal(property.GetValue(expected), property.GetValue(actual));
        Assert.Equal(expected.Id, Store.GetSettings().Id);
        Assert.Single(await oracle.Settings.ToListAsync(Ct));
    }

    /// <summary>Imported multiple-row input retains original earliest-created selection rather than row insertion order.</summary>
    [Fact]
    public async Task Earliest_original_row_is_selected_without_rewriting_any_row()
    {
        var later = new ZananceSettings { ReportCurrencyCode = "USD" };
        var earlier = new ZananceSettings { ReportCurrencyCode = "CHF" };
        await using (var db = await Factory.CreateDbContextAsync(Ct))
        {
            db.Settings.AddRange(later, earlier); await db.SaveChangesAsync(Ct);
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Settings SET CreatedAt = {10L} WHERE Id = {earlier.Id}", Ct);
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Settings SET CreatedAt = {20L} WHERE Id = {later.Id}", Ct);
        }
        Assert.Equal(earlier.Id, (await Store.GetSettingsAsync(Ct)).Id);
        Assert.Equal(earlier.Id, Store.GetSettings().Id);
        await using var verify = await Factory.CreateDbContextAsync(Ct);
        Assert.Equal(2, await verify.Settings.CountAsync(Ct));
        Assert.Equal(10L, (await verify.Settings.FindAsync([earlier.Id], Ct))!.CreatedAt.UtcTicks);
    }

    /// <summary>A returned preference is a detached snapshot, never a process-wide cache or implicit write.</summary>
    [Fact]
    public async Task Unsaved_changes_cannot_change_a_later_read_and_saved_changes_are_fresh()
    {
        var initial = await Store.GetSettingsAsync(Ct);
        initial.ReportCurrencyCode = "USD"; initial.AppLockEnabled = true;
        var unsaved = await Store.GetSettingsAsync(Ct);
        Assert.Equal("EUR", unsaved.ReportCurrencyCode); Assert.False(unsaved.AppLockEnabled);
        await Store.SaveSettingsAsync(initial, Ct);
        var saved = await Store.GetSettingsAsync(Ct);
        Assert.Equal(initial.Id, saved.Id); Assert.Equal("USD", saved.ReportCurrencyCode); Assert.True(saved.AppLockEnabled);
        Assert.NotSame(initial, saved);
    }

    /// <summary>Already-cancelled first use cannot create a preference row or publish defaults.</summary>
    [Fact]
    public async Task Cancelled_first_read_does_not_initialize_settings()
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(Ct); cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Store.GetSettingsAsync(cancellation.Token));
        await using var db = await Factory.CreateDbContextAsync(Ct);
        Assert.False(await db.Settings.AnyAsync(Ct));
    }

    /// <summary>Original EF command interception can cancel after native execution; no partial settings are returned.</summary>
    [Fact]
    public async Task Cancellation_at_reader_completion_releases_the_connection_and_preserves_the_saved_row()
    {
        var original = await Store.GetSettingsAsync(Ct);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(Ct);
        var interceptor = new CancelSettingsReader(cancellation);
        var options = new DbContextOptionsBuilder<ZananceDbContext>().UseSqlite("Data Source=" + Path)
            .AddInterceptors(interceptor).Options;
        var store = new ZananceStore(new DirectFactory(options), TimeProvider.System, services.GetRequiredService<ICommercialWriteAccessSource>());
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.GetSettingsAsync(cancellation.Token));
        Assert.True(interceptor.Observed);
        Assert.Equal(original.Id, (await Store.GetSettingsAsync(Ct)).Id);
    }

    /// <summary>A borrowed context must read the caller's uncommitted snapshot, without committing or discarding it.</summary>
    [Fact]
    public async Task Joined_transaction_retains_uncommitted_settings_and_original_rollback_ownership()
    {
        var original = await Store.GetSettingsAsync(Ct);
        await using var owner = await Factory.CreateDbContextAsync(Ct);
        await using var transaction = await owner.Database.BeginTransactionAsync(Ct);
        await owner.Database.ExecuteSqlInterpolatedAsync($"UPDATE Settings SET ReportCurrencyCode = {"CHF"} WHERE Id = {original.Id}", Ct);
        var options = new DbContextOptionsBuilder<ZananceDbContext>().UseSqlite(owner.Database.GetDbConnection()).Options;
        var store = new ZananceStore(new BorrowedFactory(options, transaction.GetDbTransaction()), TimeProvider.System,
            services.GetRequiredService<ICommercialWriteAccessSource>());
        Assert.Equal("CHF", (await store.GetSettingsAsync(Ct)).ReportCurrencyCode);
        Assert.NotNull(owner.Database.CurrentTransaction);
        await transaction.RollbackAsync(Ct);
        Assert.Equal(original.ReportCurrencyCode, (await Store.GetSettingsAsync(Ct)).ReportCurrencyCode);
    }

    /// <summary>A future query filter must never be silently bypassed by the complete typed projection.</summary>
    [Fact]
    public async Task A_changed_model_filter_is_refused_before_issuing_a_settings_command()
    {
        await Store.GetSettingsAsync(Ct);
        var observed = new CountSettingsCommands();
        using var modelContext = new FilteredSettingsContext(new DbContextOptionsBuilder<FilteredSettingsContext>().UseSqlite("Data Source=" + Path).Options);
        var options = new DbContextOptionsBuilder<ZananceDbContext>().UseSqlite("Data Source=" + Path)
            .UseModel(modelContext.Model).AddInterceptors(observed).Options;
        var store = new ZananceStore(new DirectFactory(options), TimeProvider.System, services.GetRequiredService<ICommercialWriteAccessSource>());
        var failure = await Assert.ThrowsAsync<InvalidOperationException>(() => store.GetSettingsAsync(Ct));
        Assert.Contains("must be refreshed", failure.Message, StringComparison.Ordinal);
        Assert.Equal(0, observed.Count);
    }

    /// <summary>Builds independent future filter metadata using provider conventions, not a copy of the reader's contract.</summary>
    private sealed class FilteredSettingsContext(DbContextOptions<FilteredSettingsContext> options) : DbContext(options)
    {
        /// <inheritdoc />
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<ZananceSettings>(); entity.ToTable("Settings");
            entity.Property(s => s.CreatedAt).HasConversion(new Vafadar.Data.Conventions.UtcTicksDateTimeOffsetConverter());
            entity.Property(s => s.UpdatedAt).HasConversion(new Vafadar.Data.Conventions.UtcTicksDateTimeOffsetConverter());
            entity.HasQueryFilter(s => !s.AppLockEnabled);
        }
    }

    /// <summary>Counts only actual settings commands; metadata refusals must precede this boundary.</summary>
    private sealed class CountSettingsCommands : DbCommandInterceptor
    {
        /// <summary>Gets the number of commands observed after metadata validation.</summary>
        internal int Count { get; private set; }
        /// <inheritdoc />
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData,
            InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("FROM \"Settings\"", StringComparison.Ordinal)) Count++;
            return ValueTask.FromResult(result);
        }
    }

    /// <summary>Original command readers remain interceptable; bypass SQL connections would fail this assertion.</summary>
    private sealed class CancelSettingsReader(CancellationTokenSource cancellation) : DbCommandInterceptor
    {
        /// <summary>Gets whether cancellation reached the actual reader boundary.</summary>
        internal bool Observed { get; private set; }
        /// <inheritdoc />
        public override ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData,
            DbDataReader result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("FROM \"Settings\"", StringComparison.Ordinal))
            { Observed = true; cancellation.Cancel(); }
            return ValueTask.FromResult(result);
        }
    }

    /// <summary>Creates original contexts with only the independently observed command interceptor added.</summary>
    private sealed class DirectFactory(DbContextOptions<ZananceDbContext> options) : IDbContextFactory<ZananceDbContext>
    {
        /// <inheritdoc />
        public ZananceDbContext CreateDbContext() => new(options);
    }

    /// <summary>Joins only the independently owned transaction; disposing this context never owns its commit.</summary>
    private sealed class BorrowedFactory(DbContextOptions<ZananceDbContext> options, DbTransaction transaction) : IDbContextFactory<ZananceDbContext>
    {
        /// <inheritdoc />
        public ZananceDbContext CreateDbContext()
        {
            var context = new ZananceDbContext(options); context.Database.UseTransaction(transaction); return context;
        }
    }

    /// <summary>Releases only this fixture's provider, pools and directory.</summary>
    public void Dispose() { services.Dispose(); SqliteTestPools.Clear(directory); directory.Dispose(); }
}
