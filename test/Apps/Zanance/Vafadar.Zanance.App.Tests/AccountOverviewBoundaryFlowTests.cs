using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Rates;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Account overview valuation, quantity separation and original-file/access/cancellation boundaries.</summary>
public sealed class AccountOverviewBoundaryFlowTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static async Task PrepareValuationAsync(FlowFixture f, ExperienceMode mode, bool enabled)
    {
        await AccountOverviewFlowTests.SeedAsync(f);
        await using var db = await AccountOverviewFlowTests.Factory(f).CreateDbContextAsync(Ct);
        db.Accounts.Add(new Account { Name = "Dollar source", Type = AccountType.Cash, CurrencyCode = "USD",
            OpeningBalance = 20000, OpeningDate = new(2026, 10, 1), IncludeInTotals = true });
        db.Settings.Add(new ZananceSettings { Mode = mode, ReportCurrencyCode = "EUR", ValuationCurrencyEnabled = enabled });
        db.ExchangeRates.Add(new ExchangeRate { FromCurrencyCode = "USD", ToCurrencyCode = "EUR", Date = new(2026, 10, 8), Rate = 0.5m });
        await db.SaveChangesAsync(Ct);
    }

    [Theory]
    [InlineData(ExperienceMode.Advanced, true, true)]
    [InlineData(ExperienceMode.Simple, true, false)]
    [InlineData(ExperienceMode.Advanced, false, false)]
    public async Task Valuation_reads_rates_only_for_the_original_enabled_multicurrency_surface(ExperienceMode mode, bool enabled, bool expected)
    {
        using var f = new FlowFixture(); await PrepareValuationAsync(f, mode, enabled);
        var source = await f.Store.ReadAccountOverviewAsync(Ct);
        Assert.Equal(expected ? 1 : 0, source.Inputs.Rates.Count);
        var vm = AccountOverviewFlowTests.Create(f); await vm.LoadAsync(); Assert.Equal(2, vm.Totals.Count);
        if (expected) Assert.Contains("190.00", vm.ConvertedText, StringComparison.Ordinal);
        else Assert.Null(vm.ConvertedText);
        Assert.Single(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task Missing_valuation_rate_stays_incomplete_while_native_totals_are_available()
    {
        using var f = new FlowFixture(); await PrepareValuationAsync(f, ExperienceMode.Advanced, true);
        await using (var db = await AccountOverviewFlowTests.Factory(f).CreateDbContextAsync(Ct))
            await db.Database.ExecuteSqlRawAsync("DELETE FROM ExchangeRates", Ct);
        var vm = AccountOverviewFlowTests.Create(f); await vm.LoadAsync();
        Assert.True(vm.Loading.IsReady); Assert.Equal(2, vm.Totals.Count);
        Assert.Contains("USD", vm.ConvertedText, StringComparison.Ordinal);
        Assert.DoesNotContain("190.00", vm.ConvertedText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Original_holding_quantity_is_separate_from_cash_and_ignores_archived_types()
    {
        using var f = new FlowFixture(); await AccountOverviewFlowTests.SeedAsync(f);
        var holdings = f.Services.GetRequiredService<HoldingStore>();
        var type = new AssetType { Name = "Original counted pieces", Kind = AssetKind.CountableItem,
            Dimension = AssetDimension.Count, PriceCurrencyCode = "USD" };
        await holdings.SaveTypeAsync(type, Ct); var place = await holdings.EnsureDefaultLocationAsync("Original place", Ct);
        Assert.True((await holdings.SaveEventAsync(new AssetEvent { AssetTypeId = type.Id, LocationId = place.Id,
            Kind = AssetEventKind.Opening, Date = new(2026, 10, 1), Quantity = 3000, BasisAmount = 50000 }, [], Ct)).Succeeded);
        var vm = AccountOverviewFlowTests.Create(f); await vm.LoadAsync();
        Assert.Contains(type.Name, vm.HoldingsText, StringComparison.Ordinal);
        Assert.Contains("90.00", Assert.Single(vm.Totals).Text, StringComparison.Ordinal);
        await using (var db = await AccountOverviewFlowTests.Factory(f).CreateDbContextAsync(Ct))
            await db.Database.ExecuteSqlRawAsync("UPDATE AssetTypes SET IsArchived=1", Ct);
        await vm.LoadAsync(); Assert.Null(vm.HoldingsText); Assert.Single(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Original_rate_or_holding_revision_change_refuses_old_source(bool holding)
    {
        using var f = new FlowFixture(); await PrepareValuationAsync(f, ExperienceMode.Advanced, true);
        var holdings = f.Services.GetRequiredService<HoldingStore>();
        await holdings.SaveTypeAsync(new AssetType { Name = "Original asset", Dimension = AssetDimension.Count, PriceCurrencyCode = "EUR" }, Ct);
        var source = await f.Store.ReadAccountOverviewAsync(Ct);
        await using (var db = await AccountOverviewFlowTests.Factory(f).CreateDbContextAsync(Ct))
        {
            if (holding) await db.Database.ExecuteSqlRawAsync("UPDATE AssetTypes SET Name='Changed asset'", Ct);
            else await db.Database.ExecuteSqlRawAsync("UPDATE ExchangeRates SET Rate='0.6'", Ct);
        }
        await Assert.ThrowsAsync<AccountOverviewSupersededException>(() => f.Store.ValidateAccountOverviewAsync(source, Ct));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Complete_settings_and_rate_input_mutation_is_refused(bool rate)
    {
        using var f = new FlowFixture(); await PrepareValuationAsync(f, ExperienceMode.Advanced, true);
        var source = await f.Store.ReadAccountOverviewAsync(Ct);
        if (rate) source.Inputs.Rates[0].Rate = 0.9m;
        else source.Inputs.Settings!.MonthStartDay = 15;
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Store.ValidateAccountOverviewAsync(source, Ct));
    }

    [Fact]
    public async Task Cancelled_original_read_does_not_initialize_settings_or_return_a_partial_packet()
    {
        using var f = new FlowFixture(); using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(Ct);
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => f.Store.ReadAccountOverviewAsync(cancellation.Token));
        await using var db = await AccountOverviewFlowTests.Factory(f).CreateDbContextAsync(Ct);
        Assert.Empty(await db.Settings.ToListAsync(Ct)); Assert.Empty(await db.Accounts.ToListAsync(Ct));
    }

    [Fact]
    public async Task Cached_access_change_has_precedence_over_a_persisted_source_retry()
    {
        using var f = new FlowFixture(); await AccountOverviewFlowTests.SeedAsync(f); var access = new MutableAccess();
        var store = new ZananceStore(AccountOverviewFlowTests.Factory(f), f.Time, access); var source = await store.ReadAccountOverviewAsync(Ct);
        await using (var db = await AccountOverviewFlowTests.Factory(f).CreateDbContextAsync(Ct))
            await db.Database.ExecuteSqlRawAsync("UPDATE Entries SET Amount=1100", Ct);
        access.Current = new(source.DatabasePath, new CapabilityContext(ProductPlan.Free, new(EntitlementScopeKind.PersonalProfile, Guid.NewGuid())));
        var error = await Record.ExceptionAsync(() => store.ValidateAccountOverviewAsync(source, Ct));
        Assert.IsType<InvalidOperationException>(error); Assert.False(error is AccountOverviewSupersededException);
    }

    [Fact]
    public async Task Retained_command_cannot_move_its_account_identity_to_another_file()
    {
        using var f = new FlowFixture(); using var other = new FlowFixture(); await AccountOverviewFlowTests.SeedAsync(f);
        var switched = false; var factory = new SelectedFactory(() => switched ? AccountOverviewFlowTests.Factory(other) : AccountOverviewFlowTests.Factory(f));
        var vm = AccountOverviewFlowTests.Create(f, new ZananceStore(factory, f.Time, f.Services.GetRequiredService<ICommercialWriteAccessSource>()));
        await vm.LoadAsync(); var row = Assert.Single(Assert.Single(vm.Groups).Items); switched = true;
        await vm.OpenAsync(row); Assert.Empty(f.Platform.Routes); Assert.Single(f.Platform.Failures);
        Assert.True(vm.Loading.HasFailed); Assert.False(vm.Loading.IsReady);
    }

    [Fact]
    public async Task Original_add_debt_and_holdings_routes_keep_their_original_parameters_without_a_financial_write()
    {
        using var f = new FlowFixture(); await AccountOverviewFlowTests.SeedAsync(f); var vm = AccountOverviewFlowTests.Create(f);
        await vm.LoadAsync(); await vm.AddCommand.ExecuteAsync(null); await vm.AddDebtCommand.ExecuteAsync(null); await vm.OpenHoldingsCommand.ExecuteAsync(null);
        Assert.Equal(["account", "account", "holdings"], f.Platform.Routes.Select(route => route.Route));
        Assert.Null(f.Platform.Routes[0].Parameters); Assert.Equal(true, f.Platform.Routes[1].Parameters!["debt"]);
        Assert.Null(f.Platform.Routes[2].Parameters); Assert.Single(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    /// <summary>Changes only the original factory selection, never fabricating an EF context or application window.</summary>
    private sealed class SelectedFactory(Func<IDbContextFactory<ZananceDbContext>> select) : IDbContextFactory<ZananceDbContext>
    {
        public ZananceDbContext CreateDbContext() => select().CreateDbContext();
        public Task<ZananceDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => select().CreateDbContextAsync(cancellationToken);
    }

    /// <summary>Supplies explicit changing cached authority without network/provider acceptance.</summary>
    private sealed class MutableAccess : ICommercialWriteAccessSource
    {
        internal CommercialWriteAccess Current = CommercialWriteAccess.Inactive;
        public CommercialWriteAccess Capture(string databasePath) => Current;
    }
}
