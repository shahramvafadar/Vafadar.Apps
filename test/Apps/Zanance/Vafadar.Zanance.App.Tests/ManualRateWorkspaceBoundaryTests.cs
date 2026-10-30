using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Rates;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>D-649: original real manual-rate source acquisition and departed reference navigation.</summary>
public sealed class ManualRateWorkspaceBoundaryTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private sealed class GateFactory(IDbContextFactory<ZananceDbContext> original) : IDbContextFactory<ZananceDbContext>
    {
        internal Func<CancellationToken, Task>? Before;
        internal IDbContextFactory<ZananceDbContext> Current = original;
        public ZananceDbContext CreateDbContext() => Current.CreateDbContext();
        public async Task<ZananceDbContext> CreateDbContextAsync(CancellationToken ct = default)
        { if (Before is { } before) await before(ct); return await Current.CreateDbContextAsync(ct); }
    }
    private static RatesViewModel Model(FlowFixture f, IDbContextFactory<ZananceDbContext> factory) =>
        new(new ZananceStore(factory, f.Time, f.Services.GetRequiredService<ICommercialWriteAccessSource>()),
            f.Translator, new DateFormatter(f.Localization), f.Localization, f.Time, f.Platform);
    /// <summary>Actual page departure must cancel a held original source acquisition without another load or write.</summary>
    [Fact]
    public async Task Original_rate_departure_cancels_pending_actual_source_acquisition()
    {
        using var f = new FlowFixture(); await f.Store.GetSettingsAsync(Ct);
        var factory = new GateFactory(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>()); var model = Model(f, factory);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); var held = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken actual = default; factory.Before = async ct => { actual = ct; entered.TrySetResult(); await held.Task.WaitAsync(ct); };
        var pending = model.LoadAsync(); await entered.Task.WaitAsync(TimeSpan.FromSeconds(10), Ct);
        try { model.SetPresentationActive(false); Assert.True(actual.IsCancellationRequested); }
        finally { held.TrySetResult(); try { await pending; } catch (OperationCanceledException) { } }
        Assert.False(model.CanEdit); Assert.Empty(await f.Store.GetRatesAsync(Ct)); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }
    /// <summary>An actual departed rate form cannot reopen reference navigation from an old native command.</summary>
    [Fact]
    public async Task Departed_original_rate_reference_command_cannot_navigate()
    {
        using var f = new FlowFixture(); var model = await ManualRateContextTests.CreateAsync(f); model.SetPresentationActive(false);
        await model.OpenReferencesCommand.ExecuteAsync(null); Assert.Empty(f.Platform.Routes);
        Assert.Empty(await f.Store.GetRatesAsync(Ct)); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }
    /// <summary>The ordinary first settings acquisition cannot silently select another actual profile for the rate page.</summary>
    [Fact]
    public async Task Original_rate_publication_cannot_follow_another_file_after_initial_settings_read()
    {
        using var first = new FlowFixture(); using var later = new FlowFixture();
        await first.Store.GetSettingsAsync(Ct); await later.Store.GetSettingsAsync(Ct);
        await first.Store.SaveAccountAsync(new Account { Name = "D649 original", CurrencyCode = "USD", OpeningBalance = 0, OpeningDate = new(2026, 10, 1) }, Ct);
        await later.Store.SaveAccountAsync(new Account { Name = "D649 later", CurrencyCode = "GBP", OpeningBalance = 0, OpeningDate = new(2026, 10, 1) }, Ct);
        var factory = new GateFactory(first.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>()); var count = 0;
        factory.Before = _ => { if (++count > 1) factory.Current = later.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>(); return Task.CompletedTask; };
        var model = Model(first, factory); var error = await Record.ExceptionAsync(model.LoadAsync);
        if (error is not null) Assert.IsAssignableFrom<InvalidOperationException>(error);
        else Assert.Equal("USD", model.FromCurrency);
        Assert.Empty(await first.Store.GetRatesAsync(Ct)); Assert.Empty(await later.Store.GetRatesAsync(Ct));
    }
    /// <summary>A replaced native root cannot reopen original reference navigation or write the old raw rate.</summary>
    [Fact]
    public async Task Retired_actual_root_refuses_reference_and_rate_commands()
    {
        using var f = new FlowFixture(); var model = await ManualRateContextTests.CreateAsync(f); model.RateText = "1.25";
        model.RetireRoot(); await model.OpenReferencesCommand.ExecuteAsync(null); await model.AddCommand.ExecuteAsync(null);
        Assert.False(model.CanEdit); Assert.False(model.CanReload); Assert.Empty(f.Platform.Routes);
        Assert.Empty(await f.Store.GetRatesAsync(Ct)); Assert.Equal("1.25", model.RateText);
    }
    /// <summary>A declined original root is refused before even an initial settings row can be created.</summary>
    [Fact]
    public async Task Missing_original_root_does_not_initialize_first_settings()
    {
        using var f = new FlowFixture(); var original = f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>(); var factory = new GateFactory(original);
        var calls = 0; factory.Before = _ => { calls++; return Task.CompletedTask; }; var model = Model(f, factory); model.BindRootOwner(() => false);
        await model.LoadAsync(); Assert.Equal(0, calls); Assert.False(model.CanEdit);
        await using var db = await original.CreateDbContextAsync(Ct); Assert.Empty(await db.Settings.ToListAsync(Ct));
    }
    /// <summary>A retained page never initializes a different actual profile while refreshing its original raw rate.</summary>
    [Fact]
    public async Task Existing_original_page_refuses_another_actual_file_before_initialization()
    {
        using var f = new FlowFixture(); using var later = new FlowFixture(); await f.Store.GetSettingsAsync(Ct);
        var factory = new GateFactory(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>()); var model = Model(f, factory);
        await model.LoadAsync(); model.RateText = "1.2500";
        factory.Current = later.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        await Assert.ThrowsAsync<InvalidOperationException>(model.LoadAsync); Assert.True(model.Loading.HasFailed); Assert.Equal("1.2500", model.RateText); Assert.False(model.CanEdit);
        await using var db = await factory.Current.CreateDbContextAsync(Ct); Assert.Empty(await db.Settings.ToListAsync(Ct));
        Assert.Empty(await f.Store.GetRatesAsync(Ct));
    }
    /// <summary>Publication validates every original rate and the actual report/account currency hints after asynchronous source work.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task Changed_original_rate_or_currency_hints_are_not_published(int source)
    {
        using var f = new FlowFixture(); await f.Store.GetSettingsAsync(Ct);
        await f.Store.SaveAccountAsync(new Account { Name = "D649 original", CurrencyCode = "USD", OpeningBalance = 0, OpeningDate = new(2026, 10, 1) }, Ct);
        await f.Store.SaveRateAsync(new Core.Rates.ExchangeRate { FromCurrencyCode = "USD", ToCurrencyCode = "EUR", Date = new(2026, 10, 9), Rate = 1.25m }, Ct);
        var original = f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>(); var factory = new GateFactory(original); var count = 0;
        factory.Before = async _ =>
        {
            if (++count != 3) return;
            await using var db = await original.CreateDbContextAsync(Ct);
            await db.Database.ExecuteSqlRawAsync(source == 0 ? "UPDATE Settings SET ReportCurrencyCode='GBP'" : source == 1 ? "UPDATE Accounts SET CurrencyCode='GBP'" : "UPDATE ExchangeRates SET Rate='2.5'", Ct);
        };
        var model = Model(f, factory); await Assert.ThrowsAsync<ManualRateReviewChangedException>(model.LoadAsync);
        Assert.False(model.CanEdit); Assert.True(model.Loading.HasFailed); Assert.Empty(model.Rates); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }
    /// <summary>Read failure keeps the original rate form covered and preserves the raw decimal through an explicit read-only retry.</summary>
    [Fact]
    public async Task Actual_read_failure_has_failed_cover_and_retry_retains_original_raw()
    {
        using var f = new FlowFixture(); var original = f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>(); var factory = new GateFactory(original);
        var model = Model(f, factory); await model.LoadAsync(); model.RateText = "1.2500";
        factory.Before = _ => throw new IOException("D649 original source read refusal"); await Assert.ThrowsAsync<IOException>(model.LoadAsync);
        Assert.True(model.Loading.HasFailed); Assert.False(model.Loading.IsReady); Assert.True(model.CanReload); Assert.False(model.CanEdit);
        factory.Before = null; await model.ReloadCommand.ExecuteAsync(null);
        Assert.True(model.Loading.IsReady); Assert.Equal("1.2500", model.RateText); Assert.True(model.CanAdd); Assert.Empty(await f.Store.GetRatesAsync(Ct));
    }
    /// <summary>Changed actual source prevents reference navigation and requires explicit source refresh without another stored rate.</summary>
    [Fact]
    public async Task Reference_navigation_refuses_changed_original_rate_source()
    {
        using var f = new FlowFixture(); var model = await ManualRateContextTests.CreateAsync(f); model.RateText = "1.2500";
        var settings = await f.Store.GetSettingsAsync(Ct); settings.ReportCurrencyCode = "GBP"; await f.Store.SaveSettingsAsync(settings, Ct);
        await model.OpenReferencesCommand.ExecuteAsync(null); Assert.Empty(f.Platform.Routes); Assert.True(model.Loading.HasFailed);
        Assert.Equal("1.2500", model.RateText); Assert.Empty(await f.Store.GetRatesAsync(Ct));
    }
    /// <summary>A late cancelled original reader cannot cover or overwrite a ready resumed publication.</summary>
    [Fact]
    public async Task Old_cancelled_read_does_not_cover_the_resumed_original_page()
    {
        using var f = new FlowFixture(); await f.Store.GetSettingsAsync(Ct); var factory = new GateFactory(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>());
        var model = Model(f, factory); var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); var held = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        factory.Before = async ct => { entered.TrySetResult(); await held.Task.WaitAsync(ct); }; var old = model.LoadAsync(); await entered.Task.WaitAsync(TimeSpan.FromSeconds(10), Ct);
        model.SetPresentationActive(false); factory.Before = null; model.SetPresentationActive(true); await model.LoadAsync(); held.TrySetResult(); await old;
        Assert.True(model.Loading.IsReady); Assert.True(model.CanEdit); Assert.Empty(await f.Store.GetRatesAsync(Ct));
        model.SetPresentationActive(false);
    }
    /// <summary>A replaced original row cannot borrow a later publication merely because its visible record values compare equal.</summary>
    [Fact]
    public async Task Original_row_instance_cannot_gain_fresh_delete_consent_after_identical_republication()
    {
        using var f = new FlowFixture(); await f.Store.SaveRateAsync(new Core.Rates.ExchangeRate { FromCurrencyCode = "USD", ToCurrencyCode = "EUR", Date = new(2026, 10, 9), Rate = 1.25m }, Ct);
        var model = await ManualRateContextTests.CreateAsync(f); var original = Assert.Single(model.Rates);
        await model.LoadAsync(); Assert.Equal(original, Assert.Single(model.Rates)); Assert.NotSame(original, Assert.Single(model.Rates));
        await model.DeleteCommand.ExecuteAsync(original); Assert.Empty(f.Platform.ConfirmationDialogs);
        Assert.Single(await f.Store.GetRatesAsync(Ct)); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }
}
