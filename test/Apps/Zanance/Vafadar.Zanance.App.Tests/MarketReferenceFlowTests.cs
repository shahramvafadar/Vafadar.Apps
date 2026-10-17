using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Rates;
using Vafadar.Zanance.Core.Rates;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Rates;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Actual application source/SQLite with only native/network effects supplied through explicit ports.</summary>
[Trait("AT", "AT-299")]
public sealed class MarketReferenceFlowTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private sealed class Remote : IMarketRemote
    {
        internal MarketCatalog? Catalog { get; set; }
        internal Action? DuringAvailability;
        internal bool Disposed;
        internal int Fetches;
        public string Destination => "https://localhost/fictitious-market";
        public Task<MarketAvailability> AvailabilityAsync(CancellationToken ct)
        { DuringAvailability?.Invoke(); return Task.FromResult(new MarketAvailability(Catalog)); }
        public Task<MarketQuote> FetchAsync(MarketCatalog catalog, MarketRequest request, CancellationToken ct)
        { Fetches++; throw new InvalidOperationException("This fixture never fetches a real source"); }
        public void Dispose() => Disposed = true;
    }
    private sealed class Runtime(string file, MarketQuote quote) : IMarketRuntime
    {
        internal bool Deny, Confirm, EmptyCache, Network = true;
        internal int Opens, Confirms, AccountOpens;
        internal string? Terms;
        internal Action? DuringConfirmation;
        internal readonly Remote Remote = new();
        public bool NetworkAvailable => Network;
        public Task OpenAccountAsync() { AccountOpens++; return Task.CompletedTask; }
        public MarketUiAccess Capture() => new(file, () => { if (Deny) throw new OperationCanceledException("Fixture covered"); }, CancellationToken.None);
        public Task<IReadOnlyList<MarketQuote>> ReadAsync(MarketUiAccess access, CancellationToken ct) => Task.FromResult<IReadOnlyList<MarketQuote>>(EmptyCache ? [] : [quote]);
        public Task<IMarketRemote> OpenAsync(MarketUiAccess access, CancellationToken ct) { Opens++; return Task.FromResult<IMarketRemote>(Remote); }
        public Task<bool> ConfirmAsync(string title, string originalTerms, string accept, string cancel)
        { Confirms++; Terms = originalTerms; DuringConfirmation?.Invoke(); return Task.FromResult(Confirm); }
    }
    private static (MarketReferenceViewModel Model, Runtime Runtime) Create(FlowFixture f)
    {
        var instant = f.Time.GetUtcNow(); var date = DateOnly.FromDateTime(instant.Date);
        var symbol = new MarketSymbol("EURUSD", "Fictitious EUR/USD", "EUR", "USD", MarketUnit.Currency);
        var catalog = new MarketCatalog(1, "fictitious-native-port", "fixture-v1", "Fictitious original source",
            "https://source.example.invalid/reference", "https://source.example.invalid/licence", "Fictitious local qualification only",
            "Europe/Berlin", 60, 3600, true, new(2020, 1, 1), true, [symbol]);
        var quote = new MarketQuote(1, new(catalog.Provider, catalog.Revision, symbol.Id, date), catalog, symbol,
            new("fictitious-quote", date, instant.AddHours(-1), 1.23456789m), instant, instant.AddMinutes(1), instant.AddHours(1));
        var runtime = new Runtime(f.Directory.Combine("zanance.db"), quote);
        var model = new MarketReferenceViewModel(f.Services.GetRequiredService<MarketReferenceStore>(), f.Services.GetRequiredService<HoldingStore>(),
            runtime, f.Translator, f.Localization, new DateFormatter(f.Localization), f.Time);
        return (model, runtime);
    }
    [Theory]
    [InlineData("en")][InlineData("fa")][InlineData("de")][InlineData("es")][InlineData("fr")][InlineData("it")]
    public async Task Appearance_and_native_cancel_do_not_fetch_or_write_and_explicit_confirmation_saves_exact_original_value(string language)
    {
        using var f = new FlowFixture(); f.Localization.SetLanguage(f.Localization.SupportedLanguages.Single(l => l.CultureName == language));
        var (model, runtime) = Create(f); await model.LoadAsync();
        Assert.True(model.Loaded); Assert.Null(model.Selected); Assert.Equal(0, runtime.Opens); Assert.Equal(0, runtime.Remote.Fetches);
        Assert.Empty(await f.Store.GetRatesAsync(Ct)); model.Selected = Assert.Single(model.Cached);
        await model.ReviewAsync(); Assert.NotNull(model.ReviewText); Assert.DoesNotContain("Market_", model.ReviewText); Assert.DoesNotContain("{0}", model.ReviewText);
        Assert.Contains("https://source.example.invalid/licence", model.ReviewText); Assert.Contains("Fictitious original source", model.ReviewText);
        runtime.Confirm = false; await model.ApplyAsync(); Assert.Empty(await f.Store.GetRatesAsync(Ct)); Assert.True(model.CanApply);
        var original = model.ReviewText; runtime.Confirm = true; await model.ApplyAsync();
        Assert.Equal(original, runtime.Terms); var saved = Assert.Single(await f.Store.GetRatesAsync(Ct));
        Assert.Equal(1.23456789m, saved.Rate); Assert.True(saved.IsEstimate); Assert.Equal(model.Selected.Quote.Request.OnOrBefore, saved.Date);
        Assert.NotNull(model.Outcome); Assert.NotEqual("Market_Applied", model.Outcome); Assert.Equal(0, runtime.Opens); Assert.Equal(0, runtime.Remote.Fetches);
        Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct)); model.Retire();
    }
    [Fact]
    public async Task A_cover_before_native_confirmation_never_opens_the_dialog_or_writes_a_value()
    {
        using var f = new FlowFixture(); var (model, runtime) = Create(f); await model.LoadAsync();
        model.Selected = Assert.Single(model.Cached); await model.ReviewAsync(); runtime.Deny = true; runtime.Confirm = true;
        await model.ApplyAsync(); Assert.Equal(0, runtime.Confirms); Assert.Empty(await f.Store.GetRatesAsync(Ct)); Assert.False(model.CanApply);
    }
    [Theory]
    [InlineData("en")][InlineData("fa")][InlineData("de")][InlineData("es")][InlineData("fr")][InlineData("it")]
    public async Task Saved_source_survives_manual_change_and_expired_cache_as_read_only_history_with_original_identifiers(string language)
    {
        using var f = new FlowFixture(); f.Localization.SetLanguage(f.Localization.SupportedLanguages.Single(l => l.CultureName == language));
        var (model, runtime) = Create(f); await model.LoadAsync(); model.Selected = Assert.Single(model.Cached);
        await model.ReviewAsync(); runtime.Confirm = true; await model.ApplyAsync();
        var original = Assert.Single(model.History); Assert.Contains("fixture-v1", original.Details); Assert.Contains("fictitious-quote", original.Details);
        var row = Assert.Single(await f.Store.GetRatesAsync(Ct)); row.Rate = 9.9m; await f.Store.SaveRateAsync(row, Ct);
        f.Time.Now = f.Time.Now.AddHours(2); runtime.EmptyCache = true; await model.LoadAsync(); Assert.Empty(model.Cached);
        model.SavedSource = Assert.Single(model.History); Assert.Equal(original.Id, model.SavedSource.Id);
        Assert.NotEqual(original.Details, model.SavedSource.Details);
        Assert.Contains("fictitious-quote", model.SavedSource.Details); Assert.DoesNotContain("Market_", model.SavedSource.Details);
        Assert.Null(model.Selected); Assert.False(model.CanApply); Assert.False(model.CanReview); Assert.False(model.HasMoreHistory);
        Assert.Equal(0, runtime.Opens); Assert.Equal(0, runtime.Remote.Fetches); Assert.Equal(1, runtime.Confirms);
        Assert.Equal(9.9m, Assert.Single(await f.Store.GetRatesAsync(Ct)).Rate);
    }
    [Fact]
    public async Task A_picker_change_during_native_confirmation_cannot_replace_the_original_review()
    {
        using var f = new FlowFixture(); var (model, runtime) = Create(f); await model.LoadAsync();
        model.Selected = Assert.Single(model.Cached); await model.ReviewAsync(); runtime.Confirm = true;
        runtime.DuringConfirmation = () => model.ExactItemAcknowledged = true;
        await model.ApplyAsync(); Assert.Equal(1, runtime.Confirms); Assert.Empty(await f.Store.GetRatesAsync(Ct)); Assert.False(model.CanApply);
    }
    [Fact]
    public async Task Retired_metadata_completion_disposes_its_connection_without_publishing_to_another_page()
    {
        using var f = new FlowFixture(); var (model, runtime) = Create(f); await model.LoadAsync();
        runtime.Remote.DuringAvailability = model.Retire; await model.CheckAsync();
        Assert.True(runtime.Remote.Disposed); Assert.False(model.Loaded); Assert.Null(model.Symbol); Assert.Empty(model.Symbols); Assert.Empty(await f.Store.GetRatesAsync(Ct));
    }
    [Fact]
    public async Task An_unconfigured_explicit_source_check_never_fetches_a_symbol_or_erases_the_original_cache()
    {
        using var f = new FlowFixture(); var (model, runtime) = Create(f); await model.LoadAsync();
        await model.CheckAsync(); Assert.Equal(1, runtime.Opens); Assert.Equal(0, runtime.Remote.Fetches); Assert.Single(model.Cached);
        Assert.Equal(f.Translator["Market_Unconfigured"], model.Availability); Assert.False(model.CanFetch); Assert.Null(model.Error);
    }
    [Fact]
    public async Task Failed_explicit_fetch_runs_once_and_keeps_the_original_cached_reference_available_for_separate_review()
    {
        using var f = new FlowFixture(); var (model, runtime) = Create(f); await model.LoadAsync();
        var cached = Assert.Single(model.Cached); runtime.Remote.Catalog = cached.Quote.Catalog; await model.CheckAsync();
        Assert.Null(model.Symbol); Assert.False(model.CanFetch); model.Symbol = Assert.Single(model.Symbols);
        await model.FetchAsync(); Assert.Equal(1, runtime.Remote.Fetches); Assert.Equal(cached, Assert.Single(model.Cached));
        Assert.Equal(f.Translator["Market_NetworkFailure"], model.Error); Assert.Empty(await f.Store.GetRatesAsync(Ct));
        model.Selected = cached; await model.ReviewAsync(); Assert.NotNull(model.ReviewText); Assert.True(model.CanApply); Assert.Equal(1, runtime.Remote.Fetches);
    }
    [Fact]
    public async Task Persian_original_source_keeps_exact_fractional_price_currency_packet_and_complete_licence_times()
    {
        using var f = new FlowFixture(); f.Localization.SetLanguage(f.Localization.SupportedLanguages.Single(l => l.CultureName == "fa"));
        var (model, _) = Create(f); await model.LoadAsync(); model.Selected = Assert.Single(model.Cached);
        var quote = model.Selected.Quote;
        Assert.Contains(MoneyText.ReferencePrice(quote.Observation.Value, "USD", f.Localization.CurrentCulture), model.SourceText);
        Assert.Contains(quote.Catalog.LicenceUrl, model.SourceText); Assert.Contains(quote.RetainUntil.ToString("u", f.Localization.CurrentCulture), model.SourceText);
        Assert.DoesNotContain("{", model.SourceText); Assert.Empty(await f.Store.GetRatesAsync(Ct));
    }
    [Fact]
    public async Task Account_navigation_is_explicit_and_never_opens_a_provider_or_submits_financial_data()
    {
        using var f = new FlowFixture(); var (model, runtime) = Create(f); await model.LoadAsync();
        Assert.Equal(0, runtime.AccountOpens); await model.AccountCommand.ExecuteAsync(null);
        Assert.Equal(1, runtime.AccountOpens); Assert.Equal(0, runtime.Opens); Assert.Equal(0, runtime.Remote.Fetches);
        Assert.Empty(await f.Store.GetRatesAsync(Ct));
    }
}
