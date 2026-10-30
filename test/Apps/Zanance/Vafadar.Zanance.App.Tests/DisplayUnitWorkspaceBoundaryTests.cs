using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Features.Rates;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>D-650: actual original unit read lifetime, global publication source and current native row identity.</summary>
[Collection("Entry validation units")]
public sealed class DisplayUnitWorkspaceBoundaryTests
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
    private static DisplayUnitsViewModel Model(FlowFixture f, ZananceStore? store = null) => new(store ?? f.Store, f.Translator, f.Localization, f.Platform);
    /// <summary>Departure must cancel actual source acquisition without publishing into another open monetary context.</summary>
    [Fact]
    public async Task Original_unit_departure_cancels_pending_actual_source_acquisition()
    {
        var units = DisplayUnits.All;
        try
        {
            DisplayUnits.Set([]); using var f = new FlowFixture(); await f.Store.GetSettingsAsync(Ct);
            var factory = new GateFactory(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>());
            var store = new ZananceStore(factory, f.Time, f.Services.GetRequiredService<ICommercialWriteAccessSource>()); var model = Model(f, store);
            var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); var held = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); CancellationToken actual = default;
            factory.Before = async ct => { actual = ct; entered.TrySetResult(); await held.Task.WaitAsync(ct); };
            var pending = model.LoadAsync(); await entered.Task.WaitAsync(TimeSpan.FromSeconds(10), Ct);
            try { model.Deactivate(); Assert.True(actual.IsCancellationRequested); }
            finally { held.TrySetResult(); try { await pending; } catch (OperationCanceledException) { } }
            Assert.False(model.CanEdit); Assert.Empty(DisplayUnits.All); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
        }
        finally { DisplayUnits.Set(units); }
    }
    /// <summary>An old explicit read cannot reactivate commands belonging to a closed original unit form.</summary>
    [Fact]
    public async Task Closed_original_unit_load_cannot_restore_command_authority()
    {
        var units = DisplayUnits.All;
        try
        {
            DisplayUnits.Set([]); using var f = new FlowFixture(); var model = Model(f); await model.LoadAsync(); Assert.True(model.CanEdit);
            model.Deactivate(); await model.LoadAsync(); Assert.False(model.CanEdit);
            Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
        }
        finally { DisplayUnits.Set(units); }
    }
    /// <summary>The original unit editor cannot borrow definitions subsequently published by another actual profile file.</summary>
    [Fact]
    public async Task Foreign_actual_profile_publication_before_Add_cannot_replace_original_definitions()
    {
        var units = DisplayUnits.All;
        try
        {
            DisplayUnits.Set([]); using var first = new FlowFixture(); using var later = new FlowFixture();
            await DisplayUnitPublication.SaveAsync(first.Store, [new("EUR", "Original Euro", 1)], ct: Ct);
            var model = Model(first); await model.LoadAsync(); var before = System.Text.Json.JsonSerializer.Serialize(await first.Store.GetSettingsAsync(Ct));
            await DisplayUnitPublication.SaveAsync(later.Store, [new("USD", "Other file dollar", 2)], ct: Ct);
            model.CurrencyCode = "GBP"; model.Name = "Explicit pounds"; model.FactorIndex = 1; await model.AddCommand.ExecuteAsync(null);
            Assert.Equal(before, System.Text.Json.JsonSerializer.Serialize(await first.Store.GetSettingsAsync(Ct)));
            Assert.Equal("USD", Assert.Single(DisplayUnits.All).CurrencyCode); Assert.Empty(await first.Store.GetEntriesAsync(cancellationToken: Ct));
        }
        finally { DisplayUnits.Set(units); }
    }
    /// <summary>An old row cannot gain new native Remove consent merely because a later rendered record compares equal.</summary>
    [Fact]
    public async Task Original_unit_row_instance_cannot_gain_remove_consent_after_identical_republication()
    {
        var units = DisplayUnits.All;
        try
        {
            DisplayUnits.Set([]); using var f = new FlowFixture(); await DisplayUnitPublication.SaveAsync(f.Store, [new("EUR", "Original Euro", 1)], ct: Ct);
            var model = Model(f); await model.LoadAsync(); var old = Assert.Single(model.Units); await model.LoadAsync();
            Assert.Equal(old, Assert.Single(model.Units)); Assert.NotSame(old, Assert.Single(model.Units));
            await model.RemoveCommand.ExecuteAsync(old); Assert.Empty(f.Platform.ConfirmationDialogs);
            Assert.Equal("Original Euro", Assert.Single(DisplayUnits.All).Name); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
        }
        finally { DisplayUnits.Set(units); }
    }
    /// <summary>A declined native root cannot initialize an original settings row or publish global units.</summary>
    [Fact]
    public async Task Missing_original_unit_root_refuses_before_settings_initialization()
    {
        var units = DisplayUnits.All;
        try
        {
            DisplayUnits.Set([]); using var f = new FlowFixture(); var model = Model(f); model.BindRootOwner(() => false);
            await model.LoadAsync(); Assert.False(model.CanEdit);
            await using var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
            Assert.Empty(await db.Settings.ToListAsync(Ct)); Assert.Empty(DisplayUnits.All);
        }
        finally { DisplayUnits.Set(units); }
    }
    /// <summary>A retained original form refuses a different real file before creating its settings or applying its unit definitions.</summary>
    [Fact]
    public async Task Original_unit_page_refuses_another_file_before_initialization()
    {
        var units = DisplayUnits.All;
        try
        {
            DisplayUnits.Set([]); using var first = new FlowFixture(); using var later = new FlowFixture();
            var factory = new GateFactory(first.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>());
            var store = new ZananceStore(factory, first.Time, first.Services.GetRequiredService<ICommercialWriteAccessSource>()); var model = Model(first, store);
            await model.LoadAsync(); model.Name = "  Raw crates  "; factory.Current = later.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
            await Assert.ThrowsAsync<InvalidOperationException>(model.LoadAsync); Assert.True(model.Loading.HasFailed); Assert.Equal("  Raw crates  ", model.Name);
            await using var db = await factory.Current.CreateDbContextAsync(Ct); Assert.Empty(await db.Settings.ToListAsync(Ct)); Assert.Empty(DisplayUnits.All);
        }
        finally { DisplayUnits.Set(units); }
    }
    /// <summary>Actual source failure preserves raw name/currency/exponent and current captions through explicit read-only Retry.</summary>
    [Fact]
    public async Task Original_unit_failure_retry_keeps_raw_name_and_exact_exponent()
    {
        var units = DisplayUnits.All;
        try
        {
            DisplayUnits.Set([]); using var f = new FlowFixture(); var factory = new GateFactory(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>());
            var model = Model(f, new ZananceStore(factory, f.Time, f.Services.GetRequiredService<ICommercialWriteAccessSource>()));
            model.SetPresentationActive(true); await model.LoadAsync(); model.CurrencyCode = "GBP"; model.FactorIndex = 2; model.Name = "  Raw crates  ";
            factory.Before = _ => throw new IOException("D650 original unit source refusal"); await Assert.ThrowsAsync<IOException>(model.LoadAsync);
            Assert.True(model.Loading.HasFailed); Assert.False(model.CanEdit); Assert.True(model.CanReload);
            f.Localization.SetFormattingCulture("de-DE"); Assert.Equal("1.000", model.FactorNames[2]);
            factory.Before = null; await model.ReloadCommand.ExecuteAsync(null);
            Assert.True(model.Loading.IsReady); Assert.Equal("  Raw crates  ", model.Name); Assert.Equal("GBP", model.CurrencyCode); Assert.Equal(2, model.FactorIndex);
            Assert.True(model.CanAdd); Assert.Null(model.Error); Assert.True(string.IsNullOrEmpty((await f.Store.GetSettingsAsync(Ct)).DisplayUnits));
            model.Deactivate();
        }
        finally { DisplayUnits.Set(units); }
    }
    /// <summary>Changed actual definitions cannot silently renew a retained name; clearing and explicit reentry admit only the current source.</summary>
    [Fact]
    public async Task Original_unit_draft_requires_explicit_reentry_after_definitions_change()
    {
        var units = DisplayUnits.All;
        try
        {
            DisplayUnits.Set([]); using var f = new FlowFixture(); await DisplayUnitPublication.SaveAsync(f.Store, [new("EUR", "Original Euro", 1)], ct: Ct);
            var model = Model(f); await model.LoadAsync(); model.CurrencyCode = "GBP"; model.FactorIndex = 1; model.Name = "Retained crates";
            await f.Store.UpdateSettingsAsync(row => row.DisplayUnits = "USD|New source dollar|1", Ct); await model.LoadAsync();
            Assert.Equal("Retained crates", model.Name); Assert.NotNull(model.DraftContextError); Assert.False(model.CanAdd);
            var before = System.Text.Json.JsonSerializer.Serialize(await f.Store.GetSettingsAsync(Ct)); await model.AddCommand.ExecuteAsync(null);
            Assert.Equal(before, System.Text.Json.JsonSerializer.Serialize(await f.Store.GetSettingsAsync(Ct)));
            model.Name = ""; model.Name = "New crates"; await model.AddCommand.ExecuteAsync(null);
            Assert.Empty(model.Name); Assert.Null(model.DraftContextError); Assert.Contains(DisplayUnits.All, unit => unit.CurrencyCode == "USD");
            Assert.Contains(DisplayUnits.All, unit => unit.CurrencyCode == "GBP" && unit.Name == "New crates"); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
        }
        finally { DisplayUnits.Set(units); }
    }
    /// <summary>A known own commit retires its submitted name on actual return without another write or synthetic receipt.</summary>
    [Fact]
    public async Task Known_unit_commit_at_departure_is_read_back_without_rearming_submitted_name()
    {
        var units = DisplayUnits.All;
        try
        {
            DisplayUnits.Set([]); using var f = new FlowFixture(); var model = Model(f); await model.LoadAsync();
            model.CurrencyCode = "EUR"; model.FactorIndex = 1; model.Name = "Accepted crates";
            void Leave(object? sender, EventArgs args) => model.Deactivate(); f.Store.Changed += Leave;
            await model.AddCommand.ExecuteAsync(null); f.Store.Changed -= Leave;
            Assert.Equal("EUR|Accepted crates|2", (await f.Store.GetSettingsAsync(Ct)).DisplayUnits); Assert.False(model.CanEdit); Assert.Empty(DisplayUnits.All);
            var accepted = System.Text.Json.JsonSerializer.Serialize(await f.Store.GetSettingsAsync(Ct));
            model.SetPresentationActive(true); await model.LoadAsync(); Assert.True(model.CanEdit); Assert.Empty(model.Name); Assert.False(model.CanAdd);
            Assert.Equal("Accepted crates", Assert.Single(DisplayUnits.All).Name); Assert.Equal(accepted, System.Text.Json.JsonSerializer.Serialize(await f.Store.GetSettingsAsync(Ct)));
            model.Deactivate();
        }
        finally { DisplayUnits.Set(units); }
    }
    /// <summary>Another real profile's publication during native Remove consent cannot be borrowed by the original selective writer.</summary>
    [Fact]
    public async Task Foreign_publication_during_original_remove_consent_preserves_both_files()
    {
        var units = DisplayUnits.All;
        try
        {
            DisplayUnits.Set([]); using var first = new FlowFixture(); using var later = new FlowFixture();
            await DisplayUnitPublication.SaveAsync(first.Store, [new("EUR", "Original Euro", 1)], ct: Ct); var model = Model(first); await model.LoadAsync(); var row = Assert.Single(model.Units);
            var before = System.Text.Json.JsonSerializer.Serialize(await first.Store.GetSettingsAsync(Ct));
            first.Platform.PendingConfirmation = async () => { await DisplayUnitPublication.SaveAsync(later.Store, [new("USD", "Other file dollar", 2)], ct: Ct); return true; };
            await model.RemoveCommand.ExecuteAsync(row);
            Assert.Equal(before, System.Text.Json.JsonSerializer.Serialize(await first.Store.GetSettingsAsync(Ct))); Assert.Equal("USD", Assert.Single(DisplayUnits.All).CurrencyCode);
            Assert.Equal("USD|Other file dollar|2", (await later.Store.GetSettingsAsync(Ct)).DisplayUnits); Assert.True(model.Loading.HasFailed);
        }
        finally { DisplayUnits.Set(units); }
    }
    /// <summary>Only complete current original units/default hints may be published after asynchronous source capture.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Changed_original_unit_or_default_input_is_not_published(bool definitions)
    {
        var units = DisplayUnits.All;
        try
        {
            DisplayUnits.Set([]); using var f = new FlowFixture(); await f.Store.GetSettingsAsync(Ct); var original = f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>(); var factory = new GateFactory(original); var calls = 0;
            factory.Before = async _ => { if (++calls != 2) return; await using var db = await original.CreateDbContextAsync(Ct); await db.Database.ExecuteSqlRawAsync(definitions ? "UPDATE Settings SET DisplayUnits='EUR|Changed Euro|2'" : "UPDATE Settings SET DefaultCurrencyCode='GBP'", Ct); };
            var model = Model(f, new ZananceStore(factory, f.Time, f.Services.GetRequiredService<ICommercialWriteAccessSource>()));
            await Assert.ThrowsAsync<InvalidOperationException>(model.LoadAsync); Assert.True(model.Loading.HasFailed); Assert.False(model.CanEdit); Assert.Empty(DisplayUnits.All);
        }
        finally { DisplayUnits.Set(units); }
    }
    /// <summary>A cancelled old source cannot cover or replace a freshly resumed original unit publication.</summary>
    [Fact]
    public async Task Late_original_unit_read_cannot_cover_the_ready_resumed_page()
    {
        var units = DisplayUnits.All;
        try
        {
            DisplayUnits.Set([]); using var f = new FlowFixture(); await f.Store.GetSettingsAsync(Ct); var factory = new GateFactory(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>());
            var model = Model(f, new ZananceStore(factory, f.Time, f.Services.GetRequiredService<ICommercialWriteAccessSource>())); var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); var held = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            factory.Before = async ct => { entered.TrySetResult(); await held.Task.WaitAsync(ct); }; var old = model.LoadAsync(); await entered.Task.WaitAsync(TimeSpan.FromSeconds(10), Ct);
            model.Deactivate(); factory.Before = null; model.SetPresentationActive(true); await model.LoadAsync(); held.TrySetResult(); await old;
            Assert.True(model.CanEdit); Assert.True(model.Loading.IsReady); Assert.Null(model.Error); model.Deactivate();
        }
        finally { DisplayUnits.Set(units); }
    }
    private sealed class AcceptedSourceReadFailure(IDbContextFactory<ZananceDbContext> factory) : ICommercialWriteAccessSource
    {
        internal bool RefuseAfterCommit;
        public CommercialWriteAccess Capture(string path)
        {
            if (RefuseAfterCommit)
            {
                using var db = factory.CreateDbContext();
                if (db.Settings.AsNoTracking().Select(row => row.DisplayUnits).SingleOrDefault() == "EUR|Accepted crates|1")
                    throw new IOException("D650 actual accepted source follow-through refusal");
            }
            return CommercialWriteAccess.Inactive;
        }
    }
    /// <summary>A source read that fails only after the actual SQLite commit cannot erase its original known receipt or publish into retired authority.</summary>
    [Fact]
    public async Task Actual_post_commit_source_failure_keeps_receipt_without_fresh_access_capture()
    {
        var units = DisplayUnits.All;
        try
        {
            DisplayUnits.Set([]); using var f = new FlowFixture(); await f.Store.GetSettingsAsync(Ct);
            var access = new AcceptedSourceReadFailure(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>());
            var store = new ZananceStore(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>(), f.Time, access); var original = await store.ReadDisplayUnitsAsync(Ct); access.RefuseAfterCommit = true;
            var saved = await DisplayUnitPublication.SaveAsync(store, [new("EUR", "Accepted crates", 1)], original, ct: Ct);
            Assert.True(saved.NotificationFailed); Assert.False(saved.Published); Assert.NotNull(saved.Review);
            Assert.Equal("EUR|Accepted crates|1", (await f.Store.GetSettingsAsync(Ct)).DisplayUnits); Assert.Empty(DisplayUnits.All);
            Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
        }
        finally { DisplayUnits.Set(units); }
    }
}
