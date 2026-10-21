using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Features.Settings;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-436: actual essential estimate editor and original selective SQLite writer, with no native-control replicas.</summary>
[Trait("AT", "AT-436")]
[Collection("Entry validation units")]
public sealed class EssentialMoneyContextTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static async Task<EssentialEstimateEditor> OpenAsync(FlowFixture f, ZananceStore? store = null)
    {
        f.Localization.SetFormattingCulture("en-US");
        await f.Store.UpdateSettingsAsync(row => { row.EssentialEstimate = 1200; row.EssentialEstimatePeriod = EstimatePeriod.Week;
            row.EssentialEstimateCurrency = "EUR"; }, Ct);
        store ??= f.Store; var editor = new EssentialEstimateEditor(store, f.Translator, f.Localization);
        editor.Publish("original", await store.ReadEssentialEstimateAsync(Ct)); return editor;
    }
    private static async Task<string> SettingsAsync(FlowFixture f) => JsonSerializer.Serialize(await f.Store.GetSettingsAsync(Ct));

    [Fact]
    public async Task Unit_change_partial_edit_and_reload_require_complete_estimate_reentry()
    {
        var units = DisplayUnits.All;
        try
        {
            DisplayUnits.Set([]); using var f = new FlowFixture(); var editor = await OpenAsync(f);
            editor.ChangeText("12.34"); DisplayUnits.Set([new("EUR", "Hundred EUR", 2)]); editor.ChangeText("12.35");
            editor.Publish("original", await f.Store.ReadEssentialEstimateAsync(Ct));
            Assert.Equal("12.35", editor.Text); Assert.Equal("EUR", editor.UnitName); Assert.NotNull(editor.Error);
            var before = await SettingsAsync(f); await editor.SaveAsync(() => true, Ct); Assert.Equal(before, await SettingsAsync(f));
            editor.ChangeText(""); editor.ChangeText("12.35"); Assert.Equal("Hundred EUR", editor.UnitName);
            await editor.SaveAsync(() => true, Ct); Assert.Equal(123500, (await f.Store.GetSettingsAsync(Ct)).EssentialEstimate);
            Assert.True(editor.HasAccepted); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
        }
        finally { DisplayUnits.Set(units); }
    }

    [Fact]
    public async Task Formatting_reload_preserves_the_estimate_parser_and_raw_text()
    {
        using var f = new FlowFixture(); var editor = await OpenAsync(f); f.Localization.SetFormattingCulture("de-DE");
        editor.ChangeText("12,34"); f.Localization.SetFormattingCulture("en-US");
        editor.Publish("original", await f.Store.ReadEssentialEstimateAsync(Ct));
        Assert.Equal("12,34", editor.Text); Assert.NotNull(editor.FormatNote);
        await editor.SaveAsync(() => true, Ct); Assert.Equal(1234, (await f.Store.GetSettingsAsync(Ct)).EssentialEstimate);
    }

    [Theory, InlineData("not money"), InlineData("-1"), InlineData("0"), InlineData("9223372036854775808")]
    public async Task Nonblank_invalid_estimates_preserve_the_original_saved_amount(string raw)
    {
        using var f = new FlowFixture(); var editor = await OpenAsync(f); editor.ChangeText(raw);
        var before = await SettingsAsync(f); await editor.SaveAsync(() => true, Ct);
        Assert.Equal(before, await SettingsAsync(f)); Assert.Equal(raw, editor.Text); Assert.NotNull(editor.Error); Assert.False(editor.HasAccepted);
    }

    [Fact]
    public async Task Explicit_blank_clears_only_the_estimate_and_saves_its_selected_period()
    {
        using var f = new FlowFixture(); var editor = await OpenAsync(f); editor.ChangeText(""); editor.ChangePeriod(2);
        await editor.SaveAsync(() => true, Ct); var saved = await f.Store.GetSettingsAsync(Ct);
        Assert.Null(saved.EssentialEstimate); Assert.Equal(EstimatePeriod.Month, saved.EssentialEstimatePeriod);
        Assert.Equal("EUR", saved.EssentialEstimateCurrency); Assert.True(editor.HasAccepted);
    }

    [Fact]
    public async Task Estimate_save_preserves_unrelated_preferences_changed_since_publication()
    {
        using var f = new FlowFixture(); var editor = await OpenAsync(f); editor.ChangeText("12.34");
        await f.Store.UpdateSettingsAsync(row => { row.MonthStartDay = 19; row.ReportCurrencyCode = "USD"; }, Ct);
        await editor.SaveAsync(() => true, Ct); var saved = await f.Store.GetSettingsAsync(Ct);
        Assert.Equal(1234, saved.EssentialEstimate); Assert.Equal(19, saved.MonthStartDay); Assert.Equal("USD", saved.ReportCurrencyCode);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task A_newer_saved_estimate_requires_refresh_before_original_replacement()
    {
        using var f = new FlowFixture(); var editor = await OpenAsync(f); editor.ChangeText("12.34");
        await f.Store.UpdateSettingsAsync(row => row.EssentialEstimate = 5678, Ct); var before = await SettingsAsync(f);
        await Assert.ThrowsAsync<InvalidOperationException>(() => editor.SaveAsync(() => true, Ct));
        Assert.Equal(before, await SettingsAsync(f)); Assert.Equal("12.34", editor.Text); Assert.False(editor.HasAccepted);
    }

    [Theory, InlineData(false), InlineData(true)]
    public async Task Estimate_publication_never_carries_a_draft_into_another_profile_or_restored_settings_row(bool changedProfile)
    {
        using var f = new FlowFixture(); var editor = await OpenAsync(f); editor.ChangeText("17.");
        Assert.True(editor.IsForProfile("original")); Assert.False(editor.IsForProfile("different"));
        if (changedProfile)
            editor.Publish("different", await f.Store.ReadEssentialEstimateAsync(Ct));
        else
        {
            using var restored = new FlowFixture();
            await restored.Store.UpdateSettingsAsync(row => { row.EssentialEstimate = 3400; row.EssentialEstimateCurrency = "CHF"; }, Ct);
            editor.Publish("original", await restored.Store.ReadEssentialEstimateAsync(Ct));
        }
        Assert.Equal(changedProfile ? "12.00" : "34.00", editor.Text);
        Assert.Equal(changedProfile ? "EUR" : "CHF", editor.CurrencyCode);
        Assert.Equal(changedProfile, editor.IsForProfile("different")); Assert.False(editor.HasAccepted);
    }

    [Fact]
    public async Task Estimate_writer_rolls_back_saved_fields_when_ownership_retires_before_commit()
    {
        using var f = new FlowFixture(); _ = await OpenAsync(f); var original = await f.Store.ReadEssentialEstimateAsync(Ct);
        var before = await SettingsAsync(f); var checks = 0;
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Store.SaveEssentialEstimateAsync(original, 1234, EstimatePeriod.Day,
            "EUR", () => ++checks < 4, Ct));
        Assert.Equal(4, checks); Assert.Equal(before, await SettingsAsync(f));
    }

    [Fact]
    public async Task Explicit_suggestion_adopts_current_unit_without_reinterpreting_the_old_raw_estimate()
    {
        var units = DisplayUnits.All;
        try
        {
            DisplayUnits.Set([]); using var f = new FlowFixture(); var editor = await OpenAsync(f); editor.ChangeText("17.");
            DisplayUnits.Set([new("EUR", "Hundred EUR", 2)]); Assert.NotNull(editor.Error);
            editor.UseSuggestion(123400); Assert.Equal("12.34", editor.Text); Assert.Equal("Hundred EUR", editor.UnitName);
            Assert.Null(editor.Error); Assert.Equal((int)EstimatePeriod.Day, editor.PeriodIndex);
            await editor.SaveAsync(() => true, Ct); Assert.Equal(123400, (await f.Store.GetSettingsAsync(Ct)).EssentialEstimate);
        }
        finally { DisplayUnits.Set(units); }
    }

    [Fact]
    public async Task The_estimate_writer_refuses_another_actual_profile_file()
    {
        using var f = new FlowFixture(); _ = await OpenAsync(f); var original = await f.Store.ReadEssentialEstimateAsync(Ct);
        using var other = new FlowFixture(); var before = await SettingsAsync(other);
        await Assert.ThrowsAsync<InvalidOperationException>(() => other.Store.SaveEssentialEstimateAsync(original, 1234, EstimatePeriod.Day, "EUR", () => true, Ct));
        Assert.Equal(before, await SettingsAsync(other));
    }

    private sealed class WriterAccess : ICommercialWriteAccessSource
    {
        internal Action? Entered;
        public CommercialWriteAccess Capture(string databasePath)
        { var action = Entered; Entered = null; action?.Invoke(); return CommercialWriteAccess.Inactive; }
    }

    [Theory, InlineData("raw"), InlineData("period"), InlineData("unit"), InlineData("owner")]
    public async Task The_original_estimate_writer_refuses_changed_and_returned_input_or_retired_ownership(string change)
    {
        var units = DisplayUnits.All;
        try
        {
            DisplayUnits.Set([]); using var f = new FlowFixture(); var access = new WriterAccess();
            var store = new ZananceStore(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>(), f.Time, access);
            var editor = await OpenAsync(f, store); editor.ChangeText("12.34"); var owns = true; var before = await SettingsAsync(f);
            access.Entered = () =>
            {
                if (change == "raw") { editor.ChangeText("99"); editor.ChangeText("12.34"); }
                else if (change == "period") { editor.ChangePeriod(2); editor.ChangePeriod(1); }
                else if (change == "unit") DisplayUnits.Set([new("EUR", "Hundred EUR", 2)]);
                else owns = false;
            };
            await Assert.ThrowsAsync<InvalidOperationException>(() => editor.SaveAsync(() => owns, Ct));
            Assert.Null(access.Entered); Assert.Equal(before, await SettingsAsync(f)); Assert.Equal("12.34", editor.Text);
        }
        finally { DisplayUnits.Set(units); }
    }

    [Fact]
    public async Task Accepted_estimate_notification_failure_is_final_and_a_later_explicit_edit_uses_the_new_review()
    {
        using var f = new FlowFixture(); var editor = await OpenAsync(f); editor.ChangeText("12.34"); var notifications = 0;
        f.Store.Changed += (_, _) => { notifications++; throw new InvalidOperationException("Original observer failure"); };
        await editor.SaveAsync(() => true, Ct); Assert.True(editor.HasAccepted); Assert.NotNull(editor.Result);
        var accepted = await SettingsAsync(f); await editor.SaveAsync(() => true, Ct);
        Assert.Equal(1, notifications); Assert.Equal(accepted, await SettingsAsync(f));
        editor.ChangeText("14.34"); await editor.SaveAsync(() => true, Ct);
        Assert.Equal(2, notifications); Assert.True(editor.HasAccepted); Assert.Equal(1434, (await f.Store.GetSettingsAsync(Ct)).EssentialEstimate);
    }

    [Fact]
    public async Task Actual_typing_after_accepted_save_stays_dirty_through_notification_failure_and_readback()
    {
        using var f = new FlowFixture(); var editor = await OpenAsync(f); editor.ChangeText("12.34");
        f.Store.Changed += (_, _) => { editor.ChangeText("18."); throw new InvalidOperationException("Original observer failure"); };
        await editor.SaveAsync(() => true, Ct); Assert.False(editor.HasAccepted); Assert.Null(editor.Result);
        editor.Publish("original", await f.Store.ReadEssentialEstimateAsync(Ct));
        Assert.Equal("18.", editor.Text); Assert.Equal(1234, (await f.Store.GetSettingsAsync(Ct)).EssentialEstimate);
    }
}
