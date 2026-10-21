using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.App.Features.Rates;
using Vafadar.Zanance.Data.Commerce;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-436: actual unit publication must follow original durable settings acceptance.</summary>
[Trait("AT", "AT-436")]
[Collection("Entry validation units")]
public sealed class DisplayUnitPublicationTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    [Fact]
    public async Task Failed_original_unit_save_does_not_change_the_units_used_by_open_money_inputs()
    {
        var units = DisplayUnits.All;
        try
        {
            DisplayUnits.Set([]); using var f = new FlowFixture(); await f.Store.GetSettingsAsync(Ct);
            await using (var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct))
                await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER RefuseUnits BEFORE UPDATE ON Settings BEGIN SELECT RAISE(ABORT, 'owned unit refusal'); END", Ct);
            await Assert.ThrowsAnyAsync<Exception>(() => DisplayUnitPublication.SaveAsync(f.Store, [new("EUR", "Hundred EUR", 2)], ct: Ct));
            Assert.Empty(DisplayUnits.All); Assert.True(string.IsNullOrEmpty((await f.Store.GetSettingsAsync(Ct)).DisplayUnits));
        }
        finally { DisplayUnits.Set(units); }
    }

    [Fact]
    public async Task Accepted_unit_notification_failure_retains_durable_result_and_applies_only_that_result()
    {
        var units = DisplayUnits.All;
        try
        {
            DisplayUnits.Set([]); using var f = new FlowFixture(); await f.Store.GetSettingsAsync(Ct);
            f.Store.Changed += (_, _) => throw new IOException("Owned accepted unit observer failure");
            var accepted = await DisplayUnitPublication.SaveAsync(f.Store, [new("EUR", "Hundred EUR", 2)], ct: Ct);
            Assert.True(accepted.NotificationFailed); Assert.True(accepted.Published);
            Assert.Equal("Hundred EUR", Assert.Single(DisplayUnits.All).Name);
            Assert.Equal("EUR|Hundred EUR|2", (await f.Store.GetSettingsAsync(Ct)).DisplayUnits);
        }
        finally { DisplayUnits.Set(units); }
    }

    [Fact]
    public async Task Retired_owner_cannot_write_or_publish_original_units()
    {
        var units = DisplayUnits.All;
        try
        {
            DisplayUnits.Set([]); using var f = new FlowFixture(); var original = await f.Store.ReadDisplayUnitsAsync(Ct);
            var before = System.Text.Json.JsonSerializer.Serialize(await f.Store.GetSettingsAsync(Ct));
            await Assert.ThrowsAsync<InvalidOperationException>(() => DisplayUnitPublication.SaveAsync(f.Store,
                [new("EUR", "Hundred EUR", 2)], original, () => false, Ct));
            Assert.Empty(DisplayUnits.All); Assert.Equal(before, System.Text.Json.JsonSerializer.Serialize(await f.Store.GetSettingsAsync(Ct)));
        }
        finally { DisplayUnits.Set(units); }
    }

    [Fact]
    public async Task Unit_writer_preserves_unrelated_newer_settings_and_refuses_newer_unit_definitions()
    {
        var units = DisplayUnits.All;
        try
        {
            DisplayUnits.Set([]); using var f = new FlowFixture(); var original = await f.Store.ReadDisplayUnitsAsync(Ct);
            await f.Store.UpdateSettingsAsync(row => row.MonthStartDay = 17, Ct);
            var saved = await DisplayUnitPublication.SaveAsync(f.Store, [new("EUR", "Hundred EUR", 2)], original, ct: Ct);
            Assert.True(saved.Published); Assert.Equal(17, (await f.Store.GetSettingsAsync(Ct)).MonthStartDay);
            await Assert.ThrowsAsync<InvalidOperationException>(() => DisplayUnitPublication.SaveAsync(f.Store,
                [new("EUR", "Thousand EUR", 3)], original, ct: Ct));
            Assert.Equal("Hundred EUR", Assert.Single(DisplayUnits.All).Name);
        }
        finally { DisplayUnits.Set(units); }
    }

    [Fact]
    public async Task Original_unit_review_refuses_another_actual_file()
    {
        using var first = new FlowFixture(); var original = await first.Store.ReadDisplayUnitsAsync(Ct);
        using var other = new FlowFixture(); var before = System.Text.Json.JsonSerializer.Serialize(await other.Store.GetSettingsAsync(Ct));
        await Assert.ThrowsAsync<InvalidOperationException>(() => other.Store.SaveDisplayUnitsAsync(original, "EUR|Hundred EUR|2", () => true, Ct));
        Assert.Equal(before, System.Text.Json.JsonSerializer.Serialize(await other.Store.GetSettingsAsync(Ct)));
    }

    [Fact]
    public async Task Unit_writer_rolls_back_the_changed_property_when_owner_retires_after_SaveChanges()
    {
        using var f = new FlowFixture(); var original = await f.Store.ReadDisplayUnitsAsync(Ct);
        var before = System.Text.Json.JsonSerializer.Serialize(await f.Store.GetSettingsAsync(Ct)); var checks = 0;
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Store.SaveDisplayUnitsAsync(original, "EUR|Hundred EUR|2", () => ++checks < 4, Ct));
        Assert.Equal(4, checks); Assert.Equal(before, System.Text.Json.JsonSerializer.Serialize(await f.Store.GetSettingsAsync(Ct)));
    }

    [Fact]
    public async Task Post_commit_retired_owner_keeps_original_saved_units_without_publishing_into_another_profile()
    {
        var units = DisplayUnits.All;
        try
        {
            DisplayUnits.Set([]); using var f = new FlowFixture(); await f.Store.GetSettingsAsync(Ct); var owns = true;
            f.Store.Changed += (_, _) => { owns = false; DisplayUnits.Set([new("USD", "Other profile unit", 1)]); };
            var result = await DisplayUnitPublication.SaveAsync(f.Store, [new("EUR", "Hundred EUR", 2)], ownsOriginal: () => owns, ct: Ct);
            Assert.False(result.Published); Assert.Equal("USD", Assert.Single(DisplayUnits.All).CurrencyCode);
            Assert.Equal("EUR|Hundred EUR|2", (await f.Store.GetSettingsAsync(Ct)).DisplayUnits);
        }
        finally { DisplayUnits.Set(units); }
    }

    private static DisplayUnitsViewModel Editor(FlowFixture f, ZananceStore? store = null) => new(store ?? f.Store, f.Translator, f.Localization, f.Platform);

    [Fact]
    public async Task Actual_unit_editor_requires_publication_and_consumes_accepted_input_before_failed_notification()
    {
        var units = DisplayUnits.All;
        try
        {
            DisplayUnits.Set([]); using var f = new FlowFixture(); var vm = Editor(f);
            vm.CurrencyCode = "EUR"; vm.Name = "Hundred EUR"; vm.FactorIndex = 1;
            await vm.AddCommand.ExecuteAsync(null); Assert.Empty(DisplayUnits.All);
            await vm.LoadAsync(); Assert.True(vm.CanEdit);
            f.Store.Changed += (_, _) => throw new IOException("Owned accepted unit callback failure");
            await vm.AddCommand.ExecuteAsync(null);
            Assert.Empty(vm.Name); Assert.Equal(f.Translator["Unit_SavedFollowUpFailed"], vm.Error);
            Assert.Contains("Hundred EUR", Assert.Single(vm.Units).Text, StringComparison.Ordinal);
            var saved = (await f.Store.GetSettingsAsync(Ct)).DisplayUnits;
            await vm.AddCommand.ExecuteAsync(null); Assert.Equal(saved, (await f.Store.GetSettingsAsync(Ct)).DisplayUnits);
            vm.Deactivate(); Assert.False(vm.CanEdit);
        }
        finally { DisplayUnits.Set(units); }
    }

    [Fact]
    public async Task Actual_remove_consent_cannot_borrow_a_departed_unit_page()
    {
        var units = DisplayUnits.All;
        try
        {
            using var f = new FlowFixture(); await DisplayUnitPublication.SaveAsync(f.Store, [new("EUR", "Hundred EUR", 2)], ct: Ct);
            var vm = Editor(f); await vm.LoadAsync(); var row = Assert.Single(vm.Units);
            f.Platform.PendingConfirmation = () => { vm.Deactivate(); return Task.FromResult(true); };
            await vm.RemoveCommand.ExecuteAsync(row);
            Assert.Equal("EUR|Hundred EUR|2", (await f.Store.GetSettingsAsync(Ct)).DisplayUnits);
            Assert.Equal("Hundred EUR", Assert.Single(DisplayUnits.All).Name); Assert.Single(f.Platform.ConfirmationDialogs);
        }
        finally { DisplayUnits.Set(units); }
    }

    private sealed class UnitWriterAccess : ICommercialWriteAccessSource
    {
        internal Action? Entered;
        public CommercialWriteAccess Capture(string path)
        { var action = Entered; Entered = null; action?.Invoke(); return CommercialWriteAccess.Inactive; }
    }

    [Fact]
    public async Task Actual_unit_editor_rejects_edit_and_return_under_the_original_writer_without_publishing()
    {
        var units = DisplayUnits.All;
        try
        {
            DisplayUnits.Set([]); using var f = new FlowFixture(); var access = new UnitWriterAccess();
            var store = new ZananceStore(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>(), f.Time, access);
            var vm = Editor(f, store); await vm.LoadAsync(); vm.CurrencyCode = "EUR"; vm.Name = "Hundred EUR"; vm.FactorIndex = 1;
            access.Entered = () => { vm.Name = "Other name"; vm.Name = "Hundred EUR"; };
            await vm.AddCommand.ExecuteAsync(null);
            Assert.Null(access.Entered); Assert.Empty(DisplayUnits.All); Assert.False(vm.CanEdit); Assert.Equal("Hundred EUR", vm.Name);
            Assert.Equal(f.Translator["Unit_SaveFailed"], vm.Error);
            Assert.True(string.IsNullOrEmpty((await f.Store.GetSettingsAsync(Ct)).DisplayUnits));
        }
        finally { DisplayUnits.Set(units); }
    }

    [Fact]
    public async Task Actual_unit_editor_keeps_post_commit_typing_and_explicit_readback_applies_accepted_units_without_resave()
    {
        var units = DisplayUnits.All;
        try
        {
            DisplayUnits.Set([]); using var f = new FlowFixture(); var vm = Editor(f); await vm.LoadAsync();
            vm.CurrencyCode = "EUR"; vm.Name = "Hundred EUR"; vm.FactorIndex = 1;
            f.Store.Changed += (_, _) => vm.Name = "Next explicit unit";
            await vm.AddCommand.ExecuteAsync(null);
            Assert.Equal("Next explicit unit", vm.Name); Assert.False(vm.CanEdit); Assert.Empty(DisplayUnits.All);
            Assert.Equal(f.Translator["Unit_SavedFollowUpFailed"], vm.Error);
            var accepted = System.Text.Json.JsonSerializer.Serialize(await f.Store.GetSettingsAsync(Ct));
            await vm.LoadAsync();
            Assert.True(vm.CanEdit); Assert.Null(vm.Error); Assert.Equal("Next explicit unit", vm.Name);
            Assert.Equal("Hundred EUR", Assert.Single(DisplayUnits.All).Name);
            Assert.Equal(accepted, System.Text.Json.JsonSerializer.Serialize(await f.Store.GetSettingsAsync(Ct)));
        }
        finally { DisplayUnits.Set(units); }
    }

    [Fact]
    public async Task Ambient_profile_unit_change_under_the_original_writer_cannot_be_overwritten_by_accepted_input()
    {
        var units = DisplayUnits.All;
        try
        {
            DisplayUnits.Set([]); using var f = new FlowFixture(); var access = new UnitWriterAccess();
            var store = new ZananceStore(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>(), f.Time, access);
            var review = await store.ReadDisplayUnitsAsync(Ct);
            access.Entered = () => DisplayUnits.Set([new("USD", "Other profile unit", 1)]);
            await Assert.ThrowsAsync<InvalidOperationException>(() => DisplayUnitPublication.SaveAsync(store,
                [new("EUR", "Hundred EUR", 2)], review, ct: Ct));
            Assert.Equal("USD", Assert.Single(DisplayUnits.All).CurrencyCode);
            Assert.True(string.IsNullOrEmpty((await f.Store.GetSettingsAsync(Ct)).DisplayUnits));
        }
        finally { DisplayUnits.Set(units); }
    }
}
