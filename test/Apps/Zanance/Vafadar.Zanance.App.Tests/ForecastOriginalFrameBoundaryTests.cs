using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Forecast;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Forecasts;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>D-646: actual original forecast prompt, row, source and accepted writer boundaries.</summary>
public sealed class ForecastOriginalFrameBoundaryTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static readonly DateOnly Day = new(2026, 10, 9);
    private sealed class Port(FlowPlatform original) : IForecastInteraction
    {
        internal TaskCompletionSource<string?>? Prompt;
        internal int Choices, PromptCalls;
        internal Func<Task<string?>>? OnPrompt;
        internal string? Choice;
        public Task<string?> PromptAsync(string title, string message, string accept, string cancel, string initialValue, int maxLength, bool numeric = false) { PromptCalls++; return OnPrompt?.Invoke() ?? Prompt?.Task ?? Task.FromResult<string?>(null); }
        public Task<string?> ChooseAsync(string title, string cancel, params string[] actions) { Choices++; return Task.FromResult(Choice); }
        public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel) => original.ConfirmAsync(title, message, accept, cancel);
        public Task NavigateAsync(string route, IDictionary<string, object>? query = null) => original.NavigateAsync(route, query);
    }
    private static ForecastViewModel Model(FlowFixture f, Port port) => new(f.Store, f.Translator, new DateFormatter(f.Localization), f.Localization, f.Time, f.Services.GetRequiredService<ForecastWorkStore>(), port);
    private static async Task SeedAsync(FlowFixture f) => await f.Store.SaveAccountAsync(new Account { Name = "D646 original account", CurrencyCode = "EUR", OpeningDate = Day.AddMonths(-1), OpeningBalance = 0 }, Ct);
    /// <summary>A native name returned after another complete projection must not save that replacement projection.</summary>
    [Fact]
    public async Task Pending_original_name_cannot_save_a_reloaded_projection()
    {
        using var f = new FlowFixture(); await SeedAsync(f); var port = new Port(f.Platform); var vm = Model(f, port); await vm.LoadAsync(); Assert.True(vm.CanSave);
        port.Prompt = new(TaskCreationOptions.RunContinuationsAsynchronously); var saving = vm.SaveSnapshotCommand.ExecuteAsync(null);
        await vm.LoadAsync(); port.Prompt.SetResult("D646 late old name"); await saving;
        Assert.Empty(await f.Store.GetForecastSnapshotsAsync(Ct)); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }
    /// <summary>A retained saved row absent from the actual current publication cannot navigate.</summary>
    [Fact]
    public async Task Unpublished_saved_row_cannot_open_another_snapshot()
    {
        using var f = new FlowFixture(); var port = new Port(f.Platform); var vm = Model(f, port); await vm.LoadAsync();
        await vm.OpenSnapshotCommand.ExecuteAsync(new SnapshotRow(Guid.NewGuid(), "D646 retired row", "old")); Assert.Empty(f.Platform.Routes);
    }
    /// <summary>A retained forecast item absent from the actual current publication cannot open native actions.</summary>
    [Fact]
    public async Task Unpublished_plan_row_cannot_open_native_actions()
    {
        using var f = new FlowFixture(); var port = new Port(f.Platform) { Choice = f.Translator["Forecast_OpenOccurrence"] }; var vm = Model(f, port); await vm.LoadAsync();
        var row = new ForecastRow("old", "D646 retired item", "0 EUR", Microsoft.Maui.Graphics.Colors.Transparent, null, Guid.NewGuid(), Day, Day, false, false);
        await vm.OpenItemCommand.ExecuteAsync(row); Assert.Equal(0, port.Choices); Assert.Empty(f.Platform.Routes);
    }
    /// <summary>The actual saved-path writer cannot accept financial/source facts changed since the original review.</summary>
    [Fact]
    public async Task Actual_saved_path_writer_refuses_changed_original_settings()
    {
        using var f = new FlowFixture(); await SeedAsync(f); var reader = f.Services.GetRequiredService<ForecastWorkStore>(); var review = await reader.ReadAsync(Ct);
        var forecast = Assert.Single(review.Compute(1, new(), PeriodCalendar.Gregorian, Day));
        var saved = ForecastSnapshot.From("D646 stale source", forecast, review.Accounts.Select(a => a.Id), Day, null, "0.1.0");
        var settings = await f.Store.GetSettingsAsync(Ct); settings.MonthStartDay = 17; await f.Store.SaveSettingsAsync(settings, Ct);
        await Assert.ThrowsAnyAsync<InvalidOperationException>(() => f.Store.SaveForecastSnapshotsAsync([saved], review, Ct)); Assert.Empty(await f.Store.GetForecastSnapshotsAsync(Ct));
    }
    /// <summary>Actual acceptance is known before a Changed subscriber observes it, independently of follow-through.</summary>
    [Fact]
    public async Task Original_workspace_receipt_precedes_changed_notification()
    {
        using var f = new FlowFixture(); await SeedAsync(f); var vm = new ForecastWorkspace(f.Services.GetRequiredService<ForecastWorkStore>(), f.Store, f.Translator); await vm.LoadAsync(1, PeriodCalendar.Gregorian, Day);
        bool? observed = null; f.Store.Changed += (_, _) => { observed = vm.Result == f.Translator["Forecast_Saved"]; throw new IOException("D646 actual post-commit notification refusal."); };
        await vm.SaveAsync("D646 accepted path", null, "0.1.0"); Assert.True(observed); Assert.Single(await f.Store.GetForecastSnapshotsAsync(Ct)); Assert.False(vm.CanSave);
    }
    /// <summary>A pending original native name cannot survive page or root departure.</summary>
    [Theory]
    [InlineData("page")][InlineData("root")]
    public async Task Pending_original_name_cannot_outlive_native_ownership(string departure)
    {
        using var f = new FlowFixture(); await SeedAsync(f); var port = new Port(f.Platform); var vm = Model(f, port); await vm.LoadAsync();
        port.Prompt = new(TaskCreationOptions.RunContinuationsAsynchronously); var saving = vm.SaveSnapshotCommand.ExecuteAsync(null);
        if (departure == "page") vm.SetActive(false); else vm.BindRootOwner(() => false);
        port.Prompt.SetResult("D646 retired original name"); await saving;
        Assert.False(vm.CanSave); Assert.Empty(await f.Store.GetForecastSnapshotsAsync(Ct));
    }
    /// <summary>A previously published saved row is no longer a command owner after the same identity is reread.</summary>
    [Fact]
    public async Task Retained_original_saved_row_cannot_navigate_after_republication()
    {
        using var f = new FlowFixture(); await SeedAsync(f);
        var forecast = new CurrencyForecast("EUR", 0, 0, 0, Day, [new(Day, 0)], []);
        await f.Store.SaveForecastSnapshotAsync(ForecastSnapshot.From("D646 saved row", forecast, [], Day, null, "0.1.0"), Ct);
        var vm = Model(f, new(f.Platform)); await vm.LoadAsync(); var old = Assert.Single(vm.Snapshots); await vm.LoadAsync();
        Assert.Equal(old.Id, Assert.Single(vm.Snapshots).Id); await vm.OpenSnapshotCommand.ExecuteAsync(old); Assert.Empty(f.Platform.Routes);
    }
    /// <summary>Source changes made by actual SQLite insertion must roll back the original snapshot batch.</summary>
    [Fact]
    public async Task Actual_SQL_insert_cannot_change_protected_original_settings()
    {
        using var f = new FlowFixture(); await SeedAsync(f); var settings = await f.Store.GetSettingsAsync(Ct); settings.MonthStartDay = 17; await f.Store.SaveSettingsAsync(settings, Ct);
        var review = await f.Services.GetRequiredService<ForecastWorkStore>().ReadAsync(Ct);
        var forecast = Assert.Single(review.Compute(1, new(), PeriodCalendar.Gregorian, Day));
        await using (var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct))
        {
            Assert.Equal("Settings", db.Model.FindEntityType(typeof(Vafadar.Zanance.Core.Settings.ZananceSettings))!.GetTableName());
            await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER D646ChangedSource AFTER INSERT ON ForecastSnapshots BEGIN UPDATE Settings SET MonthStartDay=19; END;", Ct);
        }
        var saved = ForecastSnapshot.From("D646 rejected trigger", forecast, review.Accounts.Select(a => a.Id), Day, null, "0.1.0");
        await Assert.ThrowsAnyAsync<InvalidOperationException>(() => f.Store.SaveForecastSnapshotsAsync([saved], review, Ct));
        Assert.Empty(await f.Store.GetForecastSnapshotsAsync(Ct)); Assert.Equal(17, (await f.Store.GetSettingsAsync(Ct)).MonthStartDay);
    }
    private sealed class SqlRetirementFactory(IDbContextFactory<ZananceDbContext> original, Action retire) : IDbContextFactory<ZananceDbContext>
    {
        public ZananceDbContext CreateDbContext()
        {
            var db = original.CreateDbContext(); ((Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection()).CreateFunction("RetireD646SaveOwner", () => { retire(); return 1; }); return db;
        }
        public Task<ZananceDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => Task.FromResult(CreateDbContext());
    }
    /// <summary>Actual post-insert native owner retirement rolls the batch back without emitting an accepted receipt.</summary>
    [Fact]
    public async Task Actual_SQL_insert_owner_retirement_cannot_commit_or_publish_a_receipt()
    {
        using var f = new FlowFixture(); await SeedAsync(f); var review = await f.Services.GetRequiredService<ForecastWorkStore>().ReadAsync(Ct);
        var owner = true; var callbacks = 0; var accepted = false;
        var factory = new SqlRetirementFactory(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>(), () => { owner = false; callbacks++; });
        await using (var db = await factory.CreateDbContextAsync(Ct)) await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER D646RetiredOwner AFTER INSERT ON ForecastSnapshots BEGIN SELECT RetireD646SaveOwner(); END;", Ct);
        var writer = new ZananceStore(factory, f.Time, f.Services.GetRequiredService<ICommercialWriteAccessSource>());
        var forecast = Assert.Single(review.Compute(1, new(), PeriodCalendar.Gregorian, Day)); var saved = ForecastSnapshot.From("D646 retired writer", forecast, review.Accounts.Select(a => a.Id), Day, null, "0.1.0");
        await Assert.ThrowsAnyAsync<InvalidOperationException>(() => writer.SaveForecastSnapshotsAsync([saved], review, Ct, () => owner, _ => accepted = true));
        Assert.Equal(1, callbacks); Assert.False(accepted); Assert.Empty(await f.Store.GetForecastSnapshotsAsync(Ct));
    }
    private sealed class GateFactory(IDbContextFactory<ZananceDbContext> original) : IDbContextFactory<ZananceDbContext>
    {
        internal IDbContextFactory<ZananceDbContext> Current = original;
        internal bool Fail;
        internal Func<CancellationToken, Task>? Before;
        public ZananceDbContext CreateDbContext() => Current.CreateDbContext();
        public async Task<ZananceDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        { if (Before is { } before) await before(cancellationToken); if (Fail) { Fail = false; throw new IOException("D646 original read refusal."); } return await Current.CreateDbContextAsync(cancellationToken); }
    }
    private static ForecastViewModel GatedModel(FlowFixture f, GateFactory factory) => new(f.Store, f.Translator, new DateFormatter(f.Localization), f.Localization, f.Time,
        new ForecastWorkStore(factory, f.Services.GetRequiredService<ICommercialWriteAccessSource>()), new Port(f.Platform));
    /// <summary>A page cannot follow an actual replacement reader file even when its facts are otherwise valid.</summary>
    [Fact]
    public async Task Original_forecast_cannot_follow_a_replacement_actual_reader_file()
    {
        using var f = new FlowFixture(); using var other = new FlowFixture(); await SeedAsync(f); await SeedAsync(other);
        var factory = new GateFactory(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>()); var vm = GatedModel(f, factory); await vm.LoadAsync();
        factory.Current = other.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>(); await vm.LoadAsync();
        Assert.True(vm.Loading.HasFailed); Assert.False(vm.CanSave); Assert.False(vm.CanInteract); Assert.True(vm.CanReload);
    }
    /// <summary>Explicit Retry preserves the raw name and scenario without recording another plan or ledger entry.</summary>
    [Fact]
    public async Task Original_failed_read_keeps_raw_name_and_scenario_until_explicit_retry()
    {
        using var f = new FlowFixture(); await SeedAsync(f); var factory = new GateFactory(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>()); var vm = GatedModel(f, factory); await vm.LoadAsync();
        vm.Workspace.SnapshotName = "  D646 raw name  "; vm.Workspace.EditScenario(s => s.SetAmount(Guid.NewGuid(), Day, 12345)); factory.Fail = true; await vm.LoadAsync();
        Assert.True(vm.Loading.HasFailed); Assert.False(vm.CanSave); Assert.True(vm.CanReload); Assert.False(vm.Workspace.Scenario.IsEmpty);
        await vm.RefreshAccessCommand.ExecuteAsync(null); Assert.True(vm.Loading.IsReady); Assert.Equal("  D646 raw name  ", vm.Workspace.SnapshotName); Assert.False(vm.Workspace.Scenario.IsEmpty);
        Assert.Empty(await f.Store.GetForecastSnapshotsAsync(Ct)); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }
    /// <summary>Retiring a pending source does not let its late cancellation cover the resumed original page.</summary>
    [Fact]
    public async Task Original_pending_read_cancels_without_covering_resumed_publication()
    {
        using var f = new FlowFixture(); await SeedAsync(f); var factory = new GateFactory(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>()); var vm = GatedModel(f, factory); await vm.LoadAsync();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); var held = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        factory.Before = async ct => { entered.TrySetResult(); await held.Task.WaitAsync(ct); }; var old = vm.LoadAsync(); await entered.Task.WaitAsync(Ct);
        vm.SetActive(false); factory.Before = null; vm.SetActive(true); await vm.LoadAsync(); held.TrySetResult(); await old;
        Assert.True(vm.Loading.IsReady); Assert.True(vm.CanSave); Assert.Single(vm.Cards); Assert.Empty(await f.Store.GetForecastSnapshotsAsync(Ct));
    }

    /// <summary>Retained original horizon choices translate with the same unchanged computed scope and raw name.</summary>
    [Theory]
    [InlineData("fa")][InlineData("de")][InlineData("en")]
    public async Task Original_horizon_choices_follow_display_without_replacing_the_draft(string language)
    {
        using var f = new FlowFixture(); await SeedAsync(f); var vm = Model(f, new(f.Platform)); await vm.LoadAsync(); vm.Workspace.SnapshotName = "  D646 raw  ";
        f.Localization.SetLanguage(f.Localization.SupportedLanguages.Single(item => item.CultureName == language)); await vm.LoadAsync();
        Assert.Equal(new[] { f.Translator["Forecast_EndOfMonth"], f.Translator["Forecast_30Days"], f.Translator["Forecast_90Days"] }, vm.HorizonNames);
        Assert.Equal("  D646 raw  ", vm.Workspace.SnapshotName); Assert.Single(vm.Cards); Assert.Empty(await f.Store.GetForecastSnapshotsAsync(Ct));
    }

    /// <summary>An invalid response cannot open another amount prompt after the actual original page departs.</summary>
    [Fact]
    public async Task Original_amount_retry_cannot_open_a_new_prompt_after_departure()
    {
        using var f = new FlowFixture(); await SeedAsync(f); var account = Assert.Single(await f.Store.GetAccountsAsync(cancellationToken: Ct));
        await f.Services.GetRequiredService<PlanStore>().SaveScheduleAsync(new Vafadar.Zanance.Core.Plans.Schedule
        { Name = "D646 original prompt plan", AccountId = account.Id, Amount = 1000, Rule = new() { Start = Day.AddDays(1) }, ReminderEnabled = false, AutoPost = false }, Ct);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); var pending = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var port = new Port(f.Platform) { Choice = f.Translator["Forecast_AssumeAmount"] }; port.OnPrompt = () => { entered.TrySetResult(); return port.PromptCalls == 1 ? pending.Task : Task.FromResult<string?>(null); };
        var vm = Model(f, port); await vm.LoadAsync(); var row = Assert.Single(Assert.Single(vm.Cards).Rows); var action = vm.OpenItemCommand.ExecuteAsync(row);
        await entered.Task.WaitAsync(Ct); vm.SetActive(false); pending.SetResult("invalid"); await action;
        Assert.Equal(1, port.PromptCalls); Assert.True(vm.Workspace.Scenario.IsEmpty); Assert.Empty(await f.Store.GetForecastSnapshotsAsync(Ct));
    }

    /// <summary>Known accepted feedback follows display changes without losing its exact accepted identity or later name.</summary>
    [Fact]
    public async Task Original_accepted_receipt_translates_without_discarding_later_raw_name()
    {
        using var f = new FlowFixture(); await SeedAsync(f); f.Localization.SetLanguage(f.Localization.SupportedLanguages.Single(item => item.CultureName == "de"));
        var vm = new ForecastWorkspace(f.Services.GetRequiredService<ForecastWorkStore>(), f.Store, f.Translator); await vm.LoadAsync(1, PeriodCalendar.Gregorian, Day);
        f.Store.Changed += (_, _) => { vm.SnapshotName = "  D646 later raw  "; throw new IOException("D646 accepted notification refusal."); };
        await vm.SaveAsync("D646 original language receipt", null, "0.1.0"); var id = Assert.Single(vm.AcceptedSnapshotIds);
        f.Localization.SetLanguage(f.Localization.SupportedLanguages.Single(item => item.CultureName == "en")); await vm.LoadAsync(1, PeriodCalendar.Gregorian, Day);
        Assert.Equal(f.Translator["Forecast_Saved"], vm.Result); Assert.Equal(f.Translator["Forecast_SavedFollowUpFailed"], vm.Error);
        Assert.Equal("  D646 later raw  ", vm.SnapshotName); Assert.Equal(id, Assert.Single(vm.AcceptedSnapshotIds)); Assert.Single(await f.Store.GetForecastSnapshotsAsync(Ct)); Assert.False(vm.CanSave);
    }

    /// <summary>A live forecast raw name or scenario prevents a tab-root display rebuild until departure.</summary>
    [Theory]
    [InlineData("raw")][InlineData("scenario")]
    public async Task Original_forecast_draft_requires_in_place_display_refresh(string draft)
    {
        using var f = new FlowFixture(); await SeedAsync(f); var vm = Model(f, new(f.Platform)); await vm.LoadAsync(); Assert.False(vm.HasRetainedWork);
        if (draft == "raw") vm.Workspace.SnapshotName = "  "; else vm.Workspace.EditScenario(s => s.SetAmount(Guid.NewGuid(), Day, 12345));
        Assert.True(vm.HasRetainedWork); Assert.Empty(await f.Store.GetForecastSnapshotsAsync(Ct)); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

}
