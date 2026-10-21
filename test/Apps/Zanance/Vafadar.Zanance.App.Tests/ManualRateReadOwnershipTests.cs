using Vafadar.Zanance.Core.Rates;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-437: explicit rate read retry, native lifetime and original private file ownership.</summary>
[Trait("AT", "AT-437")]
public sealed class ManualRateReadOwnershipTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    [Fact]
    public async Task Explicit_read_retry_restores_the_original_decimal_draft_without_a_save()
    {
        using var f = new FlowFixture(); var model = await ManualRateContextTests.CreateAsync(f);
        f.Localization.SetFormattingCulture("de-DE"); model.RateText = "1,25"; f.Localization.SetFormattingCulture("en-US");
        f.Time.BeforeRead = () => throw new IOException("Fictitious rate publication read refusal");
        await Assert.ThrowsAsync<IOException>(() => model.LoadAsync()); f.Time.BeforeRead = null;
        Assert.False(model.CanEdit); Assert.True(model.CanReload); Assert.Equal("1,25", model.RateText);
        await model.ReloadCommand.ExecuteAsync(null);
        Assert.True(model.CanEdit); Assert.Equal("1,25", model.RateText); Assert.Contains("1,25", model.RateFormatNote);
        Assert.Empty(await f.Store.GetRatesAsync(Ct)); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }
    [Fact]
    public async Task Retired_native_rate_owner_rolls_back_the_original_pending_write()
    {
        using var f = new FlowFixture(); var model = await ManualRateContextTests.CreateAsync(f); model.RateText = "1.25";
        f.Time.BeforeRead = () => { f.Time.BeforeRead = null; model.SetPresentationActive(false); };
        await model.AddCommand.ExecuteAsync(null); f.Time.BeforeRead = null;
        Assert.Empty(await f.Store.GetRatesAsync(Ct)); Assert.Equal("1.25", model.RateText); Assert.False(model.CanEdit);
        Assert.False(model.CanReload); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }
    [Fact]
    public async Task An_original_manual_rate_snapshot_cannot_write_another_real_financial_file()
    {
        using var first = new FlowFixture(); using var second = new FlowFixture();
        await first.Store.GetSettingsAsync(Ct); await second.Store.GetSettingsAsync(Ct);
        var original = await first.Store.ReadManualRatesAsync(Ct);
        await Assert.ThrowsAsync<ManualRateReviewChangedException>(() => second.Store.SaveManualRateAsync(original,
            new ExchangeRate { Date = new(2026, 10, 9), FromCurrencyCode = "USD", ToCurrencyCode = "EUR", Rate = 1.25m }, () => true, Ct));
        Assert.Empty(await first.Store.GetRatesAsync(Ct)); Assert.Empty(await second.Store.GetRatesAsync(Ct));
        Assert.Empty(await first.Store.GetEntriesAsync(cancellationToken: Ct)); Assert.Empty(await second.Store.GetEntriesAsync(cancellationToken: Ct));
    }
    [Fact]
    public async Task Refresh_after_known_acceptance_is_read_only_and_never_rearms_the_submitted_value()
    {
        using var f = new FlowFixture(); var model = await ManualRateContextTests.CreateAsync(f); model.RateText = "1.25";
        void FailReadback(object? sender, EventArgs args) => f.Time.BeforeRead = () => throw new IOException("Fictitious accepted rate readback refusal");
        f.Store.Changed += FailReadback; await model.AddCommand.ExecuteAsync(null); f.Store.Changed -= FailReadback; f.Time.BeforeRead = null;
        var accepted = Assert.Single(await f.Store.GetRatesAsync(Ct)); Assert.Equal("", model.RateText); Assert.True(model.CanReload);
        await model.ReloadCommand.ExecuteAsync(null); Assert.Equal(accepted.Id, Assert.Single(await f.Store.GetRatesAsync(Ct)).Id);
        Assert.Equal("", model.RateText); Assert.False(model.CanAdd); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }
    [Fact]
    public async Task An_old_rendered_rate_row_cannot_request_delete_after_its_display_metadata_changes()
    {
        using var f = new FlowFixture(); await f.Store.SaveRateAsync(new ExchangeRate { Date = new(2026, 10, 9),
            FromCurrencyCode = "USD", ToCurrencyCode = "EUR", Rate = 1.25m }, Ct);
        var model = await ManualRateContextTests.CreateAsync(f); var original = Assert.Single(model.Rates);
        model.SetPresentationActive(true); f.Localization.SetFormattingCulture("de-DE"); model.SetPresentationActive(false); model.SetPresentationActive(true);
        Assert.NotEqual(original.Text, Assert.Single(model.Rates).Text); f.Platform.Confirmations.Enqueue(true);
        await model.DeleteCommand.ExecuteAsync(original); Assert.Empty(f.Platform.ConfirmationDialogs); Assert.Single(await f.Store.GetRatesAsync(Ct));
        model.SetPresentationActive(false);
    }
    [Fact]
    public async Task A_same_currency_pair_cannot_bypass_the_new_writer_with_different_letter_case()
    {
        using var f = new FlowFixture(); await f.Store.GetSettingsAsync(Ct); var original = await f.Store.ReadManualRatesAsync(Ct);
        await Assert.ThrowsAsync<ArgumentException>(() => f.Store.SaveManualRateAsync(original,
            new ExchangeRate { Date = new(2026, 10, 9), FromCurrencyCode = "USD", ToCurrencyCode = "usd", Rate = 1m }, () => true, Ct));
        Assert.Empty(await f.Store.GetRatesAsync(Ct)); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }
    [Fact]
    public async Task Rate_departure_during_delete_confirmation_preserves_the_same_original_source()
    {
        using var f = new FlowFixture(); await f.Store.SaveRateAsync(new ExchangeRate { Date = new(2026, 10, 9),
            FromCurrencyCode = "USD", ToCurrencyCode = "EUR", Rate = 1.25m }, Ct);
        var model = await ManualRateContextTests.CreateAsync(f); var original = Assert.Single(model.Rates);
        f.Platform.PendingConfirmation = () => { model.SetPresentationActive(false); return Task.FromResult(true); };
        await model.DeleteCommand.ExecuteAsync(original); Assert.Single(await f.Store.GetRatesAsync(Ct)); Assert.False(model.CanEdit);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }
}
