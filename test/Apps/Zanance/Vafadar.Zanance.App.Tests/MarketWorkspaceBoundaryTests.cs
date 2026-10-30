using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Rates;
using Vafadar.Zanance.Core.Rates;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Rates;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Original reference workspace boundaries against real local SQLite, with only native/cache effects supplied.</summary>
public sealed class MarketWorkspaceBoundaryTests
{
    private sealed class Runtime(string file) : IMarketRuntime
    {
        internal string File = file;
        internal bool Fail;
        internal bool Confirm;
        internal Action? DuringConfirmation;
        internal IReadOnlyList<MarketQuote> Cache = [];
        internal int Reads;
        internal CancellationToken ReadToken;
        internal TaskCompletionSource? Held;
        internal readonly TaskCompletionSource Entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool NetworkAvailable => false;
        public Task OpenAccountAsync() => throw new InvalidOperationException("No account navigation is required.");
        public MarketUiAccess Capture() => new(File, () => { }, CancellationToken.None);
        public async Task<IReadOnlyList<MarketQuote>> ReadAsync(MarketUiAccess access, CancellationToken ct)
        { Reads++; ReadToken = ct; Entered.TrySetResult(); if (Held is not null) await Held.Task.WaitAsync(ct); if (Fail) throw new IOException("Actual cache port failure"); return Cache; }
        public Task<IMarketRemote> OpenAsync(MarketUiAccess access, CancellationToken ct) => throw new InvalidOperationException("No provider dispatch is required.");
        public Task<bool> ConfirmAsync(string title, string originalTerms, string accept, string cancel)
        { DuringConfirmation?.Invoke(); return Task.FromResult(Confirm); }
    }
    private static MarketReferenceViewModel Create(FlowFixture f, Runtime runtime) => new(
        f.Services.GetRequiredService<MarketReferenceStore>(), runtime,
        f.Translator, f.Localization, new DateFormatter(f.Localization), f.Time);
    private static MarketQuote Quote(FlowFixture f)
    {
        var now = f.Time.Now; var date = DateOnly.FromDateTime(now.Date);
        var symbol = new MarketSymbol("EURUSD", "Fictitious EUR/USD", "EUR", "USD", MarketUnit.Currency);
        var catalog = new MarketCatalog(1, "fictitious-boundary", "one", "Fictitious source", "https://source.example.invalid/reference",
            "https://source.example.invalid/licence", "Local fictitious qualification", "Europe/Berlin", 60, 3600, true, new(2020, 1, 1), true, [symbol]);
        return new(1, new(catalog.Provider, catalog.Revision, symbol.Id, date), catalog, symbol,
            new("original", date, now.AddMinutes(-1), 1.25m), now, now.AddMinutes(1), now.AddHours(1));
    }

    [Fact]
    public async Task Actual_page_departure_cancels_the_held_original_cache_read()
    {
        using var f = new FlowFixture(); var runtime = new Runtime(f.Directory.Combine("zanance.db")) { Held = new() };
        var model = Create(f, runtime); var load = model.LoadAsync(); await runtime.Entered.Task.WaitAsync(TestContext.Current.CancellationToken);
        model.Retire(); var cancelled = runtime.ReadToken.IsCancellationRequested; runtime.Held.SetResult(); await load;
        Assert.True(cancelled); Assert.False(model.Loaded); Assert.Empty(await f.Store.GetRatesAsync(TestContext.Current.CancellationToken));
    }
    [Fact]
    public async Task A_public_old_load_cannot_reactivate_a_departed_original_page()
    {
        using var f = new FlowFixture(); var runtime = new Runtime(f.Directory.Combine("zanance.db")); var model = Create(f, runtime);
        await model.LoadAsync(); model.Retire(); await model.LoadAsync();
        Assert.False(model.Loaded); Assert.Equal(1, runtime.Reads); Assert.False(model.CanReview);
    }
    [Fact]
    public async Task Captured_private_cache_file_cannot_publish_another_actual_SQLite_file()
    {
        using var f = new FlowFixture(); var runtime = new Runtime(f.Directory.Combine("other-profile.db")); var model = Create(f, runtime);
        await model.LoadAsync(); Assert.False(model.Loaded); Assert.NotNull(model.Error); Assert.Empty(model.Assets); Assert.Empty(model.History);
        Assert.Empty(await f.Store.GetRatesAsync(TestContext.Current.CancellationToken));
    }
    [Fact]
    public async Task A_terminal_original_root_cannot_be_activated_by_a_later_native_appearance()
    {
        using var f = new FlowFixture(); var runtime = new Runtime(f.Directory.Combine("zanance.db")); var model = Create(f, runtime);
        await model.LoadAsync(); model.RetireRoot(); model.SetPresentationActive(true); await model.LoadAsync();
        Assert.Equal(1, runtime.Reads); Assert.False(model.Loaded); Assert.False(model.CanApply);
    }
    [Fact]
    public async Task Actual_original_page_return_gets_a_new_cancellable_frame_without_reactivating_old_work()
    {
        using var f = new FlowFixture(); var runtime = new Runtime(f.Directory.Combine("zanance.db")); var model = Create(f, runtime);
        await model.LoadAsync(); var original = runtime.ReadToken; model.Retire(); Assert.True(original.IsCancellationRequested);
        model.SetPresentationActive(true); await model.LoadAsync(); Assert.True(model.Loaded); Assert.False(runtime.ReadToken.IsCancellationRequested);
        Assert.NotEqual(original, runtime.ReadToken); Assert.Equal(2, runtime.Reads);
    }
    [Fact]
    public async Task Actual_root_retirement_during_a_held_read_cannot_publish_its_late_results()
    {
        using var f = new FlowFixture(); var runtime = new Runtime(f.Directory.Combine("zanance.db")) { Held = new() };
        var current = true; var model = Create(f, runtime); model.BindRootOwner(() => current);
        var load = model.LoadAsync(); await runtime.Entered.Task.WaitAsync(TestContext.Current.CancellationToken);
        current = false; runtime.Held.SetResult(); await load; Assert.False(model.Loaded); Assert.Empty(model.Assets); Assert.Empty(model.Cached);
    }
    [Fact]
    public async Task Original_page_cannot_adopt_a_replacement_profile_on_explicit_reload()
    {
        using var f = new FlowFixture(); var runtime = new Runtime(f.Directory.Combine("zanance.db")); var model = Create(f, runtime);
        await model.LoadAsync(); runtime.File = f.Directory.Combine("replacement.db"); await model.LoadAsync();
        Assert.False(model.Loaded); Assert.NotNull(model.Error); Assert.Equal(1, runtime.Reads); Assert.False(System.IO.File.Exists(runtime.File));
    }
    [Fact]
    public async Task Explicit_read_only_retry_keeps_the_selected_request_date_and_does_not_write_a_rate()
    {
        using var f = new FlowFixture(); var runtime = new Runtime(f.Directory.Combine("zanance.db")) { Fail = true }; var model = Create(f, runtime);
        model.Date = new(2026, 10, 15); await model.LoadAsync(); Assert.NotNull(model.Error); Assert.False(model.Loaded);
        runtime.Fail = false; await model.LoadAsync(); Assert.True(model.Loaded); Assert.Null(model.Error); Assert.Equal(new DateOnly(2026, 10, 15), model.Date);
        Assert.Empty(await f.Store.GetRatesAsync(TestContext.Current.CancellationToken)); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: TestContext.Current.CancellationToken));
    }
    [Fact]
    public async Task Another_actual_file_cannot_supply_the_next_saved_history_page()
    {
        using var f = new FlowFixture(); var store = f.Services.GetRequiredService<MarketReferenceStore>();
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.HistoryAsync(ct: TestContext.Current.CancellationToken, expectedFile: f.Directory.Combine("foreign.db")));
        Assert.Empty(await store.HistoryAsync(ct: TestContext.Current.CancellationToken, expectedFile: f.Directory.Combine("zanance.db")));
    }
    [Fact]
    public async Task A_value_equal_copy_outside_the_actual_published_cache_has_no_review_authority()
    {
        using var f = new FlowFixture(); var runtime = new Runtime(f.Directory.Combine("zanance.db")) { Cache = [Quote(f)] };
        var model = Create(f, runtime); await model.LoadAsync(); var original = Assert.Single(model.Cached);
        model.Selected = original with { }; Assert.False(model.CanReview); await model.ReviewAsync(); Assert.False(model.CanApply);
        Assert.Empty(await f.Store.GetRatesAsync(TestContext.Current.CancellationToken));
    }
    [Fact]
    public async Task Native_confirmation_cannot_transfer_a_review_to_a_retired_original_root()
    {
        using var f = new FlowFixture(); var runtime = new Runtime(f.Directory.Combine("zanance.db")) { Cache = [Quote(f)], Confirm = true };
        var model = Create(f, runtime); await model.LoadAsync(); model.Selected = Assert.Single(model.Cached); await model.ReviewAsync(); Assert.True(model.CanApply);
        runtime.DuringConfirmation = model.RetireRoot; await model.ApplyAsync();
        Assert.False(model.CanApply); Assert.Empty(await f.Store.GetRatesAsync(TestContext.Current.CancellationToken)); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: TestContext.Current.CancellationToken));
    }
}
