using System.ComponentModel;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Analysis;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.Core.Analysis;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Analysis;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Original translation/invalidation probes with real sources and declared historical input, never a primary request.</summary>
internal static class AiLanguagePublicationProbes
{
    /// <summary>Prepares only the actual source/error or metadata needed by one new publication condition.</summary>
    internal static async Task<AiLanguagePrepared> CreateAsync(int phase, IDbContextFactory<ZananceDbContext> original,
        ICommercialWriteAccessSource access, Translator text, ILocalizationService locale, TimeProvider clock, IAppInteraction port)
    {
        var source = new AiLanguageSource(original) { Refuse = phase == 2 }; var runtime = new AiLanguageRuntime(clock);
        runtime.Remote.Configured = phase != 3;
        var model = new AiAnalysisViewModel(new AiSnapshotStore(source, access), text, locale, new DateFormatter(locale), clock, port, runtime);
        await model.LoadAsync();
        if (phase == 2) { if (model.Error is null || model.Loaded) throw new InvalidDataException("Original source-error input required."); }
        else
        {
            if (!model.Loaded || model.Accounts.Count != 1) throw new InvalidDataException("One actual metadata account required.");
            if (phase >= 11) { model.Accounts[0].Selected = true; await model.ReviewAsync(); if (!model.CanShare) throw new InvalidDataException(); }
            else
            {
                // This is an unbound model; no DateField receives an unset date.
                model.From = DateOnly.MinValue; model.To = new(2026, 10, 9); await model.ReviewAsync();
                if (model.AccountsError is null || model.FromError is null || model.ToError is null) throw new InvalidDataException();
                if (phase is 3 or 4 or 5 or 7 or 9)
                { await model.CheckProviderAsync(); if (model.ResultSnapshot is null || model.ResultText is null) throw new InvalidDataException("Declared historical metadata input required."); }
            }
        }
        source.Calls = 0; runtime.Reset();
        return new(model, source, runtime, text, locale, model.From, model.To, model.ResultSnapshot, model.ResultText);
    }

    /// <summary>Runs a single language notification boundary or a controlled worker-scheduler nested local preview.</summary>
    internal static async Task<AiLanguageObservation> RunAsync(AiLanguagePrepared prepared, int phase)
    {
        var model = prepared.Model; var fired = false; var retired = false; var late = 0; var frozen = 0; string? exceptionType = null;
        var publications = new[] { nameof(model.Error), nameof(model.AccountsError), nameof(model.FromError), nameof(model.ToError),
            nameof(model.ProviderText), nameof(model.BalanceText), nameof(model.ResultSnapshot), nameof(model.ResultText), nameof(model.ResultStatus) };
        PropertyChangedEventHandler handler = (_, e) =>
        {
            if (e.PropertyName is nameof(model.ResultSnapshot) or nameof(model.ResultText)) frozen++;
            if (retired && publications.Contains(e.PropertyName)) late++;
            if (fired) return;
            var target = phase switch { 0 or 6 or 10 => nameof(model.AccountsError), 1 => nameof(model.ToError), 2 => nameof(model.Error),
                3 => nameof(model.ProviderText), 4 => nameof(model.BalanceText), 5 => nameof(model.ResultSnapshot), 11 or 12 => nameof(model.Summary), _ => "none" };
            if (e.PropertyName != target || phase >= 11 && model.Summary is not null) return;
            fired = true;
            if (phase == 6) prepared.Locale.SetLanguage(prepared.Locale.SupportedLanguages.Single(x => x.CultureName == "it"));
            else if (phase == 10) throw new IOException("D965 original notification refused.");
            else if (phase >= 11) model.ReviewAsync().GetAwaiter().GetResult();
            else { model.RetireRoot(); retired = true; }
        };
        if (phase == 7) prepared.Runtime.Remote.BeforeDestination = () => { if (fired) return; fired = true; model.RetireRoot(); retired = true; };
        model.PropertyChanged += handler;
        try
        {
            void Invoke()
            {
                try
                {
                    if (phase == 11) model.From = prepared.OriginalFrom.AddDays(1);
                    else prepared.Locale.SetLanguage(prepared.Locale.SupportedLanguages.Single(x => x.CultureName == "de"));
                }
                catch (Exception error) when (error is not OutOfMemoryException) { exceptionType = error.GetType().Name; }
            }
            if (phase >= 11) await Task.Run(Invoke); else Invoke();
        }
        finally { model.PropertyChanged -= handler; prepared.Runtime.Remote.BeforeDestination = null; }
        return new(fired, late, frozen, exceptionType, model.Loaded, model.HasPendingOperation, model.CanLeave, model.CanShare,
            model.Summary, model.Disclosure, model.Error, model.AccountsError, model.FromError, model.ToError, model.ProviderText,
            model.BalanceText, model.ResultSnapshot, model.ResultText, model.ResultStatus, model.From, model.To,
            prepared.Locale.CurrentLanguage.CultureName, prepared.Source.Calls, prepared.Runtime.Captures, prepared.Runtime.Validations,
            prepared.Runtime.Effects, model.ResultSnapshot == prepared.OriginalSnapshot && model.ResultText == prepared.OriginalText);
    }
}
/// <summary>One original current model with its actual source and declared read-only metadata ports.</summary>
internal sealed record AiLanguagePrepared(AiAnalysisViewModel Model, AiLanguageSource Source, AiLanguageRuntime Runtime,
    Translator Text, ILocalizationService Locale, DateOnly OriginalFrom, DateOnly OriginalTo, string? OriginalSnapshot, string? OriginalText);
/// <summary>Exact terminal publication, immutable record and source/effect observations.</summary>
internal sealed record AiLanguageObservation(bool Fired, int LatePublications, int FrozenNotifications, string? ExceptionType,
    bool Loaded, bool Pending, bool CanLeave, bool CanShare, string? Summary, string? Disclosure, string? Error, string? AccountsError,
    string? FromError, string? ToError, string? ProviderText, string? BalanceText, string? Snapshot, string? ResultText, string? ResultStatus,
    DateOnly From, DateOnly To, string Language, int SourceCalls, int Captures, int Validations, int Effects, bool HistoricalValuesExact);
/// <summary>Counts real SQLite factory entry; one declared refusal precedes opening its source.</summary>
internal sealed class AiLanguageSource(IDbContextFactory<ZananceDbContext> original) : IDbContextFactory<ZananceDbContext>
{
    internal bool Refuse; internal int Calls;
    public ZananceDbContext CreateDbContext() { Calls++; if (Refuse) throw new IOException("D965 source unavailable"); return original.CreateDbContext(); }
    public Task<ZananceDbContext> CreateDbContextAsync(CancellationToken ct = default)
    { Calls++; return Refuse ? Task.FromException<ZananceDbContext>(new IOException("D965 source unavailable")) : original.CreateDbContextAsync(ct); }
}
/// <summary>Counted local scope and historical-input metadata; all primary/native/credential effects refuse.</summary>
internal sealed class AiLanguageRuntime(TimeProvider clock) : IAiRuntime
{
    internal readonly AiLanguageRemote Remote = new(clock);
    internal int Captures, Validations, Effects;
    public bool NetworkAvailable => true;
    internal void Reset() { Captures = Validations = Effects = 0; Remote.Calls = 0; }
    public AiUiAccess Capture(string originalFile) { Captures++; return new(() => Validations++, CancellationToken.None); }
    public Task<bool> ConfirmDisclosureAsync() { Effects++; return Task.FromException<bool>(new InvalidOperationException("No real authentication")); }
    public Task ShareAsync(string exactText) { Effects++; return Task.FromException(new InvalidOperationException("No recipient or share")); }
    public Task<IAiRemote> OpenAsync(string originalFile, CancellationToken ct) { Effects++; return Task.FromResult<IAiRemote>(Remote); }
}
/// <summary>A preexisting fictitious DTO is test input, never a synthesized successful primary execution.</summary>
internal sealed class AiLanguageRemote : IAiRemote
{
    internal bool Configured = true; internal int Calls; internal Action? BeforeDestination;
    private readonly AiClientRecord record;
    internal static AiBalance Balance => new(17, 29, 2, 4);
    internal AiLanguageRemote(TimeProvider clock)
    {
        var payload = new AiPayload(1, AiPurpose.PeriodExplanation, "en", new(new(2026, 10, 1), new(2026, 10, 9), 1, true, [new("EUR", 0, 0, 0, 0)], 0), null);
        var offer = new AiOffer(Guid.NewGuid(), "declared-history", "input-only", payload.Purpose, 7, clock.GetUtcNow().AddDays(1));
        var id = Guid.NewGuid(); record = new(1, new(id, offer, payload), new(id, AiProtocol.Digest(payload), AiRequestState.Completed, 3, "D965 declared preexisting untrusted plain text", Balance));
    }
    public string Destination { get { BeforeDestination?.Invoke(); return "https://localhost/declared-history-input"; } }
    public Task<AiAvailability> AvailabilityAsync(CancellationToken ct) { Calls++; return Task.FromResult(new AiAvailability(Configured, "declared-history", "input-only", Balance)); }
    public Task<AiClientRecord?> ReadAsync(CancellationToken ct) { Calls++; return Task.FromResult<AiClientRecord?>(record); }
    public Task<AiOffer> OfferAsync(AiPurpose purpose, CancellationToken ct) { Calls++; return Task.FromException<AiOffer>(new InvalidOperationException("No quote")); }
    public Task<AiRequestResult> SubmitAsync(AiOffer offer, AiPayload payload, Func<Task> recheck, CancellationToken ct) { Calls++; return Task.FromException<AiRequestResult>(new InvalidOperationException("No primary request")); }
    public Task<AiRequestResult> ContinueAsync(CancellationToken ct) { Calls++; return Task.FromException<AiRequestResult>(new InvalidOperationException("No continuation")); }
    public Task<AiRequestResult> CloseAsync(CancellationToken ct) { Calls++; return Task.FromException<AiRequestResult>(new InvalidOperationException("No closure")); }
    public void Dispose() { }
}
