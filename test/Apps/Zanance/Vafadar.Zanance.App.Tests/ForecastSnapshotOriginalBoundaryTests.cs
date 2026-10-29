using Microsoft.Data.Sqlite;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Forecast;
using Vafadar.Zanance.Core.Forecasts;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>D-645: original saved-comparison identity, source and actual delete-consent boundaries.</summary>
public sealed class ForecastSnapshotOriginalBoundaryTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static readonly DateOnly Day = new(2026, 10, 9);
    private sealed class Port(FlowPlatform original) : IForecastInteraction
    {
        public Task<string?> PromptAsync(string title, string message, string accept, string cancel, string initialValue, int maxLength, bool numeric = false) => throw new NotSupportedException("No prompt in original saved-comparison checks.");
        public Task<string?> ChooseAsync(string title, string cancel, params string[] actions) => throw new NotSupportedException("No menu in original saved-comparison checks.");
        public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel) => original.ConfirmAsync(title, message, accept, cancel);
        public Task NavigateAsync(string route, IDictionary<string, object>? query = null) => original.NavigateAsync(route, query);
    }
    private static SnapshotViewModel Model(FlowFixture f) => new(f.Store, f.Translator, new DateFormatter(f.Localization),
        f.Localization, f.Time, f.Services.GetRequiredService<ForecastWorkStore>(), new Port(f.Platform));
    private static async Task<ForecastSnapshot> SavedAsync(FlowFixture f, string name)
    {
        var forecast = new CurrencyForecast("EUR", 0, 0, 0, Day, [new(Day, 0), new(Day.AddDays(1), 0)], []);
        var snapshot = ForecastSnapshot.From(name, forecast, [], Day, null, "0.1.0");
        await f.Store.SaveForecastSnapshotAsync(snapshot, Ct); return snapshot;
    }
    private static async Task OpenAsync(SnapshotViewModel vm, Guid id)
    { vm.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = id }); await vm.LoadAsync(); Assert.True(vm.Loading.IsReady); }
    /// <summary>A native consent for one original saved forecast cannot delete a replacement route identity.</summary>
    [Fact]
    public async Task Pending_original_delete_cannot_target_a_replacement_query()
    {
        using var f = new FlowFixture(); var first = await SavedAsync(f, "D645 original A"); var second = await SavedAsync(f, "D645 original B");
        var vm = Model(f); await OpenAsync(vm, first.Id);
        var pending = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously); f.Platform.PendingConfirmation = () => pending.Task;
        var deleting = vm.DeleteCommand.ExecuteAsync(null);
        vm.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = second.Id }); pending.SetResult(true); await deleting;
        Assert.Equal(2, (await f.Store.GetForecastSnapshotsAsync(Ct)).Count); Assert.Empty(f.Platform.Routes);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }
    /// <summary>The actual writer refuses a new identity absent from the original reviewed saved-data packet.</summary>
    [Fact]
    public async Task Reviewed_delete_cannot_remove_an_identity_absent_from_original_source()
    {
        using var f = new FlowFixture(); await SavedAsync(f, "D645 reviewed original");
        var review = await f.Services.GetRequiredService<ForecastWorkStore>().ReadAsync(Ct); var other = await SavedAsync(f, "D645 later identity");
        var before = JsonSerializer.Serialize(await f.Store.GetForecastSnapshotsAsync(Ct));
        var failure = await Record.ExceptionAsync(() => f.Store.DeleteReviewedForecastSnapshotAsync(other.Id, review, Ct));
        Assert.NotNull(failure); Assert.Equal(before, JsonSerializer.Serialize(await f.Store.GetForecastSnapshotsAsync(Ct)));
    }
    /// <summary>Changed original saved content requires new consent, rather than deleting the replacement row.</summary>
    [Fact]
    public async Task Pending_original_delete_refuses_changed_saved_content()
    {
        using var f = new FlowFixture(); var saved = await SavedAsync(f, "D645 original saved name"); var vm = Model(f); await OpenAsync(vm, saved.Id);
        var pending = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously); f.Platform.PendingConfirmation = () => pending.Task;
        var deleting = vm.DeleteCommand.ExecuteAsync(null);
        await using (var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct))
            await db.ForecastSnapshots.Where(item => item.Id == saved.Id).ExecuteUpdateAsync(setters => setters.SetProperty(item => item.Name, "D645 changed original saved content"), Ct);
        pending.SetResult(true); await deleting;
        Assert.Equal("D645 changed original saved content", Assert.Single(await f.Store.GetForecastSnapshotsAsync(Ct)).Name);
        Assert.Empty(f.Platform.Routes); Assert.False(vm.CanDelete); Assert.True(vm.Loading.HasFailed); Assert.True(vm.CanReload);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }
    /// <summary>Original detached rows/settings are immutable publication authority, not writable caller input.</summary>
    [Theory]
    [InlineData("snapshot")] [InlineData("settings")]
    public async Task Mutated_original_read_packet_cannot_remain_current(string changed)
    {
        using var f = new FlowFixture(); await SavedAsync(f, "D645 original immutable packet");
        var review = await f.Services.GetRequiredService<ForecastWorkStore>().ReadAsync(Ct);
        var before = JsonSerializer.Serialize(await f.Store.GetForecastSnapshotsAsync(Ct));
        if (changed == "snapshot") Assert.Single(review.Snapshots).Name = "D645 detached mutation"; else review.Settings.MonthStartDay = 2;
        Assert.Throws<InvalidOperationException>(review.EnsureCurrent);
        Assert.Equal(before, JsonSerializer.Serialize(await f.Store.GetForecastSnapshotsAsync(Ct)));
    }
    /// <summary>A missing original identity clears the former native title and retained rows under the same cover.</summary>
    [Fact]
    public async Task Missing_original_identity_cannot_keep_a_former_comparison_header()
    {
        using var f = new FlowFixture(); var saved = await SavedAsync(f, "D645 former comparison"); var vm = Model(f); await OpenAsync(vm, saved.Id);
        Assert.NotEmpty(vm.SavedRows); await OpenAsync(vm, Guid.NewGuid());
        Assert.True(vm.NotFound); Assert.Null(vm.Name); Assert.Empty(vm.SavedRows); Assert.Empty(vm.Points); Assert.Empty(vm.Rows); Assert.Empty(vm.Parts);
        Assert.False(vm.CanDelete); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    private sealed class SqlRetirementFactory(IDbContextFactory<ZananceDbContext> original, Action retire) : IDbContextFactory<ZananceDbContext>
    {
        public ZananceDbContext CreateDbContext()
        {
            var db = original.CreateDbContext();
            ((SqliteConnection)db.Database.GetDbConnection()).CreateFunction("RetireD645DeleteOwner", () => { retire(); return 1; });
            return db;
        }
        public Task<ZananceDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => Task.FromResult(CreateDbContext());
    }
    /// <summary>Real SQLite execution retirement must roll back the reviewed delete even in inactive commercial builds.</summary>
    [Fact]
    public async Task Actual_SQL_delete_retirement_rolls_back_without_an_accepted_receipt()
    {
        using var f = new FlowFixture(); var saved = await SavedAsync(f, "D645 original SQL-time retirement");
        var review = await f.Services.GetRequiredService<ForecastWorkStore>().ReadAsync(Ct);
        var owner = true; var callbacks = 0; var accepted = false;
        var factory = new SqlRetirementFactory(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>(), () => { owner = false; callbacks++; });
        var writer = new ZananceStore(factory, f.Time, f.Services.GetRequiredService<ICommercialWriteAccessSource>());
        await using (var db = await factory.CreateDbContextAsync(Ct))
            await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER RetireD645OriginalOwner AFTER DELETE ON ForecastSnapshots BEGIN SELECT RetireD645DeleteOwner(); END;", Ct);
        var failure = await Record.ExceptionAsync(() => writer.DeleteReviewedForecastSnapshotAsync(saved.Id, review, Ct, () => owner, () => accepted = true));
        Assert.NotNull(failure); Assert.Equal(1, callbacks); Assert.False(accepted);
        Assert.Equal(saved.Id, Assert.Single(await f.Store.GetForecastSnapshotsAsync(Ct)).Id);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }

    /// <summary>A native delete answer cannot outlive actual page, root or display departure.</summary>
    [Theory]
    [InlineData("departure")] [InlineData("root")] [InlineData("display")]
    public async Task Pending_original_delete_cannot_outlive_its_actual_frame(string changed)
    {
        using var f = new FlowFixture(); var saved = await SavedAsync(f, "D645 original current frame"); var vm = Model(f);
        var root = true; vm.BindRootOwner(() => root); vm.SetActive(true); await OpenAsync(vm, saved.Id);
        var pending = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously); f.Platform.PendingConfirmation = () => pending.Task;
        var deleting = vm.DeleteCommand.ExecuteAsync(null); Assert.False(vm.CanDelete); Assert.False(vm.CanReload);
        if (changed == "departure") vm.SetActive(false);
        if (changed == "root") root = false;
        if (changed == "display") f.Localization.SetLanguage(f.Localization.SupportedLanguages.Single(item => item.CultureName == "de"));
        pending.SetResult(true); await deleting;
        if (changed == "display") await vm.LoadAsync();
        Assert.Equal(saved.Id, Assert.Single(await f.Store.GetForecastSnapshotsAsync(Ct)).Id); Assert.Empty(f.Platform.Routes);
        vm.SetActive(false); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }
    /// <summary>The actual accepted delete receipt precedes failing notifications and cannot replay the command.</summary>
    [Fact]
    public async Task Known_original_deletion_survives_notification_failure_without_replay()
    {
        using var f = new FlowFixture(); var saved = await SavedAsync(f, "D645 original accepted deletion"); var vm = Model(f); await OpenAsync(vm, saved.Id);
        Guid? observed = null;
        f.Store.Changed += (_, _) => { observed = vm.DeletedSnapshotId; throw new IOException("D645 original delete notification refusal"); };
        f.Platform.Confirmations.Enqueue(true); await vm.DeleteCommand.ExecuteAsync(null);
        Assert.Equal(saved.Id, observed); Assert.Equal(saved.Id, vm.DeletedSnapshotId); Assert.False(vm.CanDelete);
        Assert.Equal(f.Translator["Forecast_DeletedFollowUpFailed"], vm.AccessError); Assert.Empty(await f.Store.GetForecastSnapshotsAsync(Ct));
        await vm.DeleteCommand.ExecuteAsync(null); Assert.Single(f.Platform.ConfirmationDialogs); Assert.Empty(f.Platform.Routes);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }
    private sealed class GateFactory(IDbContextFactory<ZananceDbContext> original) : IDbContextFactory<ZananceDbContext>
    {
        internal IDbContextFactory<ZananceDbContext> Current = original;
        internal Func<CancellationToken, Task>? Before;
        public ZananceDbContext CreateDbContext() => Current.CreateDbContext();
        public async Task<ZananceDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        { if (Before is { } before) await before(cancellationToken); return await Current.CreateDbContextAsync(cancellationToken); }
    }
    private static SnapshotViewModel Model(FlowFixture f, GateFactory factory) => new(f.Store, f.Translator, new DateFormatter(f.Localization),
        f.Localization, f.Time, new ForecastWorkStore(factory, f.Services.GetRequiredService<ICommercialWriteAccessSource>()), new Port(f.Platform));
    /// <summary>The original source cannot silently follow a newly opened profile during an explicit read.</summary>
    [Fact]
    public async Task Original_saved_comparison_refuses_a_foreign_actual_reader_file()
    {
        using var f = new FlowFixture(); using var other = new FlowFixture(); var saved = await SavedAsync(f, "D645 original bound file");
        await SavedAsync(other, "D645 other actual file"); var factory = new GateFactory(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>());
        var vm = Model(f, factory); await OpenAsync(vm, saved.Id);
        factory.Current = other.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>(); await vm.LoadAsync();
        Assert.True(vm.Loading.HasFailed); Assert.False(vm.Loading.IsReady); Assert.False(vm.CanDelete); Assert.NotNull(vm.LoadError);
        Assert.Equal(saved.Id, Assert.Single(await f.Store.GetForecastSnapshotsAsync(Ct)).Id); Assert.Single(await other.Store.GetForecastSnapshotsAsync(Ct));
    }
    /// <summary>Cancellation of the real old source acquisition cannot replace a newly completed publication.</summary>
    [Fact]
    public async Task Original_pending_source_cancels_without_covering_the_new_frame()
    {
        using var f = new FlowFixture(); var saved = await SavedAsync(f, "D645 original read cancellation");
        var factory = new GateFactory(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>()); var vm = Model(f, factory);
        vm.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = saved.Id });
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); var held = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        factory.Before = async token => { entered.TrySetResult(); await held.Task.WaitAsync(token); };
        var original = vm.LoadAsync(); await entered.Task.WaitAsync(Ct); Assert.False(vm.CanDelete); Assert.False(vm.CanReload);
        vm.SetActive(false); factory.Before = null; vm.SetActive(true); await vm.LoadAsync(); await original;
        Assert.True(vm.Loading.IsReady); Assert.Equal(saved.Name, vm.Name); Assert.True(vm.CanDelete); vm.SetActive(false);
        Assert.Equal(saved.Id, Assert.Single(await f.Store.GetForecastSnapshotsAsync(Ct)).Id);
    }

    /// <summary>An absent requested identity is a complete missing state rather than an indefinite loading cover.</summary>
    [Fact]
    public async Task Missing_query_identity_finishes_the_original_missing_state()
    {
        using var f = new FlowFixture(); var vm = Model(f); await vm.LoadAsync();
        Assert.True(vm.Loading.IsReady); Assert.True(vm.NotFound); Assert.False(vm.CanDelete); Assert.Null(vm.Name);
        Assert.Empty(vm.SavedRows); Assert.Empty(await f.Store.GetForecastSnapshotsAsync(Ct));
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
    }
}
