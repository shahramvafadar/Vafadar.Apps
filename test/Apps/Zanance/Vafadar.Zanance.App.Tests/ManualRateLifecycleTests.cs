using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Core.Rates;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-437: actual rate source, partial input, native consent and committed readback boundaries.</summary>
[Trait("AT", "AT-437")]
public sealed class ManualRateLifecycleTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static ExchangeRate Rate(decimal value = 1.25m) => new() { Date = new(2026, 10, 9), FromCurrencyCode = "USD", ToCurrencyCode = "EUR", Rate = value };
    [Fact]
    public async Task Display_reload_and_partial_edit_retain_the_original_plain_ratio()
    {
        using var f = new FlowFixture(); var model = await ManualRateContextTests.CreateAsync(f);
        f.Localization.SetFormattingCulture("de-DE"); model.RateText = "1,25";
        f.Localization.SetFormattingCulture("en-US"); await model.LoadAsync(); model.RateText = "1,250";
        Assert.Contains("1,25", model.RateFormatNote); await model.AddCommand.ExecuteAsync(null);
        Assert.Equal(1.25m, Assert.Single(await f.Store.GetRatesAsync(Ct)).Rate); Assert.Equal("", model.RateText);
    }
    [Fact]
    public async Task A_changed_currency_pair_requires_complete_explicit_reentry()
    {
        using var f = new FlowFixture(); var model = await ManualRateContextTests.CreateAsync(f);
        model.RateText = "1.25"; model.ToCurrency = "GBP"; await model.AddCommand.ExecuteAsync(null);
        Assert.Empty(await f.Store.GetRatesAsync(Ct)); Assert.Equal("1.25", model.RateText); Assert.NotNull(model.RateContextError);
        model.RateText = ""; model.RateText = "1.50"; await model.AddCommand.ExecuteAsync(null);
        var saved = Assert.Single(await f.Store.GetRatesAsync(Ct)); Assert.Equal("GBP", saved.ToCurrencyCode); Assert.Equal(1.50m, saved.Rate);
    }
    [Theory]
    [InlineData("pair")]
    [InlineData("date")]
    [InlineData("estimate")]
    public async Task A_changed_and_returned_rate_choice_refuses_the_pending_original_write(string field)
    {
        using var f = new FlowFixture(); var model = await ManualRateContextTests.CreateAsync(f); model.RateText = "1.25";
        f.Time.BeforeRead = () =>
        {
            f.Time.BeforeRead = null;
            if (field == "pair") { model.ToCurrency = "GBP"; model.ToCurrency = "EUR"; }
            if (field == "date") { var date = model.Date; model.Date = date.AddDays(1); model.Date = date; }
            if (field == "estimate") { model.IsEstimate = true; model.IsEstimate = false; }
        };
        await model.AddCommand.ExecuteAsync(null); f.Time.BeforeRead = null;
        Assert.Empty(await f.Store.GetRatesAsync(Ct)); Assert.Equal("1.25", model.RateText); Assert.NotNull(model.Error);
    }
    [Fact]
    public async Task The_original_same_date_pair_replaces_its_rate_identity_without_converting_cash()
    {
        using var f = new FlowFixture(); var source = Rate(); await f.Store.SaveRateAsync(source, Ct);
        var model = await ManualRateContextTests.CreateAsync(f); model.RateText = "1.50"; await model.AddCommand.ExecuteAsync(null);
        var saved = Assert.Single(await f.Store.GetRatesAsync(Ct)); Assert.Equal(source.Id, saved.Id); Assert.Equal(1.50m, saved.Rate);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }
    [Fact]
    public async Task Original_delete_consent_cannot_follow_a_changed_and_returned_draft()
    {
        using var f = new FlowFixture(); await f.Store.SaveRateAsync(Rate(), Ct);
        var model = await ManualRateContextTests.CreateAsync(f); var row = Assert.Single(model.Rates);
        f.Platform.PendingConfirmation = () => { model.RateText = "2.50"; model.RateText = ""; return Task.FromResult(true); };
        await model.DeleteCommand.ExecuteAsync(row); Assert.Single(await f.Store.GetRatesAsync(Ct));
        Assert.Single(f.Platform.ConfirmationDialogs); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }
    [Fact]
    public async Task A_newer_stored_rate_survives_old_native_delete_consent()
    {
        using var f = new FlowFixture(); var source = Rate(); await f.Store.SaveRateAsync(source, Ct);
        var model = await ManualRateContextTests.CreateAsync(f); var row = Assert.Single(model.Rates);
        f.Platform.PendingConfirmation = async () => { source.Rate = 2.50m; await f.Store.SaveRateAsync(source, Ct); return true; };
        await model.DeleteCommand.ExecuteAsync(row);
        var saved = Assert.Single(await f.Store.GetRatesAsync(Ct)); Assert.Equal(source.Id, saved.Id); Assert.Equal(2.50m, saved.Rate);
        Assert.NotNull(model.Error); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }
    [Fact]
    public async Task Mutating_a_returned_rate_row_cannot_renew_its_original_private_snapshot()
    {
        using var f = new FlowFixture(); await f.Store.GetSettingsAsync(Ct); var source = Rate(); await f.Store.SaveRateAsync(source, Ct);
        var review = await f.Store.ReadManualRatesAsync(Ct); Assert.Single(review.Rates).Rate = 2.50m;
        source.Rate = 2.50m; await f.Store.SaveRateAsync(source, Ct);
        await Assert.ThrowsAsync<ManualRateReviewChangedException>(() => f.Store.SaveManualRateAsync(review, Rate(1.50m), () => true, Ct));
        Assert.Equal(2.50m, Assert.Single(await f.Store.GetRatesAsync(Ct)).Rate);
    }
    [Fact]
    public async Task A_refused_sql_insert_keeps_the_same_original_rate_draft_for_explicit_retry()
    {
        using var f = new FlowFixture(); var model = await ManualRateContextTests.CreateAsync(f); model.RateText = "1.25";
        var factory = f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        await using (var db = await factory.CreateDbContextAsync(Ct))
            await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER RefuseRate BEFORE INSERT ON ExchangeRates BEGIN SELECT RAISE(ABORT, 'Fictitious rate write refusal'); END;", Ct);
        await model.AddCommand.ExecuteAsync(null); Assert.Empty(await f.Store.GetRatesAsync(Ct)); Assert.Equal("1.25", model.RateText);
        await using (var db = await factory.CreateDbContextAsync(Ct)) await db.Database.ExecuteSqlRawAsync("DROP TRIGGER RefuseRate;", Ct);
        await model.AddCommand.ExecuteAsync(null); Assert.Equal(1.25m, Assert.Single(await f.Store.GetRatesAsync(Ct)).Rate);
    }
    [Fact]
    public async Task Known_rate_acceptance_survives_a_failed_following_list_read_without_rearming()
    {
        using var f = new FlowFixture(); var model = await ManualRateContextTests.CreateAsync(f); model.RateText = "1.25";
        void BreakReadback(object? sender, EventArgs args) => f.Time.BeforeRead = () => throw new IOException("Fictitious rate readback refusal");
        f.Store.Changed += BreakReadback; await model.AddCommand.ExecuteAsync(null); f.Store.Changed -= BreakReadback; f.Time.BeforeRead = null;
        Assert.Equal("", model.RateText); Assert.NotNull(model.Error); Assert.False(model.CanAdd);
        Assert.Single(await f.Store.GetRatesAsync(Ct)); await model.AddCommand.ExecuteAsync(null); Assert.Single(await f.Store.GetRatesAsync(Ct));
    }
    [Fact]
    public async Task Known_delete_notification_failure_cannot_delete_another_rate_on_retry()
    {
        using var f = new FlowFixture(); await f.Store.SaveRateAsync(Rate(), Ct);
        var model = await ManualRateContextTests.CreateAsync(f); var row = Assert.Single(model.Rates); f.Platform.Confirmations.Enqueue(true);
        void Fail(object? sender, EventArgs args) => throw new IOException("Fictitious accepted delete notification");
        f.Store.Changed += Fail; await model.DeleteCommand.ExecuteAsync(row); f.Store.Changed -= Fail;
        Assert.Empty(await f.Store.GetRatesAsync(Ct)); Assert.NotNull(model.Error); await model.DeleteCommand.ExecuteAsync(row);
        Assert.Empty(await f.Store.GetRatesAsync(Ct)); Assert.Single(f.Platform.ConfirmationDialogs);
    }
    [Fact]
    public async Task Monetary_display_units_do_not_multiply_a_plain_manual_ratio()
    {
        var original = DisplayUnits.All.ToArray();
        try
        {
            using var f = new FlowFixture(); var model = await ManualRateContextTests.CreateAsync(f); model.RateText = "1.25";
            DisplayUnits.Set([new("USD", "Fictitious hundred dollars", 2), new("EUR", "Fictitious thousand euro", 3)]);
            await model.AddCommand.ExecuteAsync(null); Assert.Equal(1.25m, Assert.Single(await f.Store.GetRatesAsync(Ct)).Rate);
            Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
        }
        finally { DisplayUnits.Set(original); }
    }
}
