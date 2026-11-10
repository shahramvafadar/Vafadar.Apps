using System.ComponentModel;
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

/// <summary>Exercises only new quote binding failures with declared non-native metadata.</summary>
internal static class AiQuoteAuthorityProbes
{
    /// <summary>Prepares one current original review without a primary request or consent.</summary>
    internal static async Task<AiQuotePrepared> CreateAsync(IDbContextFactory<ZananceDbContext> factory,
        ICommercialWriteAccessSource rights, Translator text, ILocalizationService locale, TimeProvider time, IAppInteraction interaction)
    {
        var runtime = new QuoteRuntime(time);
        var model = new AiAnalysisViewModel(new AiSnapshotStore(factory, rights), text, locale, new DateFormatter(locale), time, interaction, runtime);
        await model.LoadAsync(); if (!model.Loaded || model.Accounts.Count != 1) throw new InvalidDataException();
        model.Accounts[0].Selected = true; await model.ReviewAsync(); await model.CheckProviderAsync();
        if (!model.CanQuote || model.CanSend) throw new InvalidDataException();
        return new(model, runtime);
    }
    /// <summary>Failed changing/changed notifications cannot leave fresh terms usable; the control never sends.</summary>
    internal static async Task<AiQuoteObservation> RunAsync(AiQuotePrepared p, int phase)
    {
        if (phase is < 0 or > 2) throw new ArgumentOutOfRangeException(nameof(phase));
        bool fired = false;
        void Refuse(string? name)
        {
            if (fired || name != nameof(p.Model.ProviderText)) return;
            fired = true; throw new IOException("D978 actual quote caption binding refused.");
        }
        PropertyChangingEventHandler changing = (_, e) => { if (phase == 0) Refuse(e.PropertyName); };
        PropertyChangedEventHandler changed = (_, e) => { if (phase == 1) Refuse(e.PropertyName); };
        p.Model.PropertyChanging += changing; p.Model.PropertyChanged += changed;
        try { await p.Model.QuoteAsync(); }
        finally { p.Model.PropertyChanging -= changing; p.Model.PropertyChanged -= changed; }
        return new(fired, p.Model.CanSend, p.Model.CanQuote, p.Model.HasPendingOperation, p.Model.CanLeave, p.Model.Error, p.Runtime.Offers, p.Runtime.Primary);
    }
}
/// <summary>Only the existing model and counted synthetic metadata port.</summary>
internal sealed record AiQuotePrepared(AiAnalysisViewModel Model, QuoteRuntime Runtime);
/// <summary>Original authority and effect counters after one new quote condition.</summary>
internal sealed record AiQuoteObservation(bool Fired, bool CanSend, bool CanQuote, bool Pending, bool CanLeave, string? Error, int Offers, int Primary);
/// <summary>Declares affordable terms without any remote journal, authentication, submission or financial effect.</summary>
internal sealed class QuoteRuntime(TimeProvider time) : IAiRuntime, IAiRemote
{
    internal int Offers, Primary;
    public bool NetworkAvailable => true;
    public string Destination => "https://localhost/declared-quote-only";
    public AiUiAccess Capture(string file) => new(() => { }, CancellationToken.None);
    public Task<IAiRemote> OpenAsync(string file, CancellationToken ct) => Task.FromResult<IAiRemote>(this);
    public Task<AiAvailability> AvailabilityAsync(CancellationToken ct) => Task.FromResult(new AiAvailability(true, "declared", "input-only", new(20, 0, 0, 0)));
    public Task<AiClientRecord?> ReadAsync(CancellationToken ct) => Task.FromResult<AiClientRecord?>(null);
    public Task<AiOffer> OfferAsync(AiPurpose purpose, CancellationToken ct) { Offers++; return Task.FromResult(new AiOffer(Guid.NewGuid(), "declared", "input-only", purpose, 7, time.GetUtcNow().AddDays(1))); }
    public Task<bool> ConfirmDisclosureAsync() { Primary++; throw new InvalidOperationException("No genuine authentication."); }
    public Task ShareAsync(string text) { Primary++; throw new InvalidOperationException("No recipient."); }
    public Task<AiRequestResult> SubmitAsync(AiOffer offer, AiPayload payload, Func<Task> check, CancellationToken ct) { Primary++; throw new InvalidOperationException("No submission."); }
    public Task<AiRequestResult> ContinueAsync(CancellationToken ct) { Primary++; throw new InvalidOperationException("No continuation."); }
    public Task<AiRequestResult> CloseAsync(CancellationToken ct) { Primary++; throw new InvalidOperationException("No close."); }
    public void Dispose() { }
}
