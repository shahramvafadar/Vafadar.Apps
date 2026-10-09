using System.Globalization;
using System.Text.Json;
using Vafadar.Zanance.App.Features.Settings;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Core.Settings;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Actual estimate draft publication against stored preferences, including units and save boundaries.</summary>
public sealed class EstimateDraftTests
{
    [Fact, Trait("AT", "AT-91")]
    public async Task First_publication_uses_saved_values_instead_of_constructor_defaults()
    {
        using var f = new FlowFixture(); var ct = TestContext.Current.CancellationToken;
        await f.Store.UpdateSettingsAsync(s => { s.EssentialEstimate = 1234; s.EssentialEstimatePeriod = EstimatePeriod.Week; }, ct);
        var settings = await f.Store.GetSettingsAsync(ct); var incoming = Input(settings); var state = new EstimateDraftState();
        Assert.Equal(incoming, state.Publish("main", settings.Id, incoming, new("", 0, "USD")));
    }

    [Theory, InlineData("17."), InlineData(""), InlineData("invalid"), InlineData("۱۷٫۲۵"), Trait("AT", "AT-91")]
    public async Task Reload_keeps_exact_unsaved_input_and_period_without_writing_preferences(string text)
    {
        using var f = new FlowFixture(); var ct = TestContext.Current.CancellationToken;
        await f.Store.UpdateSettingsAsync(s => s.EssentialEstimate = 1200, ct);
        var settings = await f.Store.GetSettingsAsync(ct); var incoming = Input(settings); var state = new EstimateDraftState();
        state.Publish("main", settings.Id, incoming, new("", 0, "EUR"));
        var draft = new EstimateInput(text, 2, "EUR");
        var before = JsonSerializer.Serialize(settings);
        var reloaded = await f.Store.GetSettingsAsync(ct);
        Assert.Equal(draft, state.Publish("main", reloaded.Id, Input(reloaded), draft));
        Assert.Equal(before, JsonSerializer.Serialize(await f.Store.GetSettingsAsync(ct)));
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: ct));
    }

    [Fact, Trait("AT", "AT-91")]
    public async Task Changing_only_the_period_remains_unsaved_after_reload()
    {
        using var f = new FlowFixture(); var ct = TestContext.Current.CancellationToken; var settings = await f.Store.GetSettingsAsync(ct); var incoming = Input(settings);
        var state = new EstimateDraftState(); state.Publish("main", settings.Id, incoming, incoming);
        var draft = incoming with { PeriodIndex = 2 };
        Assert.Equal(draft, state.Publish("main", settings.Id, incoming, draft));
        Assert.Equal(EstimatePeriod.Day, (await f.Store.GetSettingsAsync(ct)).EssentialEstimatePeriod);
    }

    [Fact, Trait("AT", "AT-91")]
    public async Task A_changed_default_currency_does_not_reinterpret_the_unsaved_amount()
    {
        using var f = new FlowFixture(); var ct = TestContext.Current.CancellationToken; var settings = await f.Store.GetSettingsAsync(ct); var initial = Input(settings);
        var state = new EstimateDraftState(); state.Publish("main", settings.Id, initial, initial);
        var draft = new EstimateInput("17.25", 2, "EUR");
        await f.Store.UpdateSettingsAsync(s => s.DefaultCurrencyCode = "CHF", ct);
        var reloaded = await f.Store.GetSettingsAsync(ct);
        Assert.Equal("CHF", Input(reloaded).CurrencyCode);
        Assert.Equal(draft, state.Publish("main", reloaded.Id, Input(reloaded), draft));
        Assert.Null(reloaded.EssentialEstimate);
    }

    [Fact, Trait("AT", "AT-91")]
    public void A_clean_field_refreshes_after_external_saved_changes()
    {
        var id = Guid.NewGuid(); var initial = new EstimateInput("12.00", 0, "EUR"); var updated = new EstimateInput("30.00", 1, "CHF");
        var state = new EstimateDraftState(); state.Publish("main", id, initial, initial);
        Assert.Equal(updated, state.Publish("main", id, updated, initial));
    }

    [Theory, InlineData(true), InlineData(false), Trait("AT", "AT-91")]
    public void A_draft_cannot_cross_a_profile_or_replaced_settings_row(bool changeProfile)
    {
        var id = Guid.NewGuid(); var initial = new EstimateInput("12.00", 0, "EUR"); var updated = new EstimateInput("30.00", 1, "CHF");
        var state = new EstimateDraftState(); state.Publish("main", id, initial, initial);
        Assert.Equal(updated, state.Publish(changeProfile ? "restored-copy" : "main", changeProfile ? id : Guid.NewGuid(),
            updated, new("17.", 2, "EUR")));
    }

    [Fact, Trait("AT", "AT-91")]
    public void A_successful_save_accepts_only_the_submitted_input_and_keeps_later_typing_dirty()
    {
        var id = Guid.NewGuid(); var initial = new EstimateInput("12.00", 0, "EUR"); var submitted = new EstimateInput("17.25", 2, "EUR");
        var typedLater = submitted with { Text = "18." }; var state = new EstimateDraftState();
        state.Publish("main", id, initial, initial); state.AcceptSave("main", id, submitted);
        Assert.Equal(typedLater, state.Publish("main", id, submitted, typedLater));
    }

    [Fact, Trait("AT", "AT-91")]
    public void Successfully_saved_clean_input_can_refresh_instead_of_remaining_a_permanent_draft()
    {
        var id = Guid.NewGuid(); var initial = new EstimateInput("12.00", 0, "EUR"); var submitted = new EstimateInput("17.25", 2, "EUR");
        var changed = submitted with { Text = "30.00" }; var state = new EstimateDraftState();
        state.Publish("main", id, initial, initial); state.AcceptSave("main", id, submitted);
        Assert.Equal(changed, state.Publish("main", id, changed, submitted));
    }

    [Fact, Trait("AT", "AT-91")]
    public void A_late_save_from_the_previous_profile_cannot_replace_the_current_baseline()
    {
        var id = Guid.NewGuid(); var initial = new EstimateInput("12.00", 0, "EUR"); var other = new EstimateInput("30.00", 1, "CHF");
        var state = new EstimateDraftState(); state.Publish("main", id, initial, initial); state.Publish("copy", id, other, initial);
        state.AcceptSave("main", id, initial);
        var draft = other with { Text = "31." };
        Assert.Equal(draft, state.Publish("copy", id, other, draft));
    }

    private static EstimateInput Input(ZananceSettings settings)
    {
        var currency = settings.EssentialEstimateCurrency ?? settings.DefaultCurrencyCode;
        return new(settings.EssentialEstimate is { } amount ? MoneyText.ForInput(amount, currency, CultureInfo.GetCultureInfo("en-US")) : "",
            (int)settings.EssentialEstimatePeriod, currency);
    }
}
