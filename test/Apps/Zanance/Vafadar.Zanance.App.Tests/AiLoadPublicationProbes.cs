using System.Collections.Specialized;
using System.ComponentModel;
using Microsoft.EntityFrameworkCore;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Analysis;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Analysis;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Original account-load publication with real file factories and no provider or financial operation.</summary>
internal static class AiLoadPublicationProbes
{
    /// <summary>Prepares only the source load required for retained selection or an actual file transition.</summary>
    internal static async Task<AiLoadPublicationPrepared> CreateAsync(int phase,
        IDbContextFactory<ZananceDbContext> first, IDbContextFactory<ZananceDbContext> second,
        ICommercialWriteAccessSource access, Translator text, ILocalizationService locale, TimeProvider time, IAppInteraction port)
    {
        var source = new AiLoadSwitchFactory(first); var runtime = new AiReviewRuntimeProbe();
        var model = new AiAnalysisViewModel(new AiSnapshotStore(source, access), text, locale, new DateFormatter(locale), time, port, runtime);
        if (phase >= 7)
        {
            await model.LoadAsync();
            if (!model.Loaded || model.Accounts.Count != 2) throw new InvalidDataException("Two original metadata accounts required.");
            model.Accounts[0].Selected = true;
            model.From = new(2026, 8, 5); model.To = new(2026, 9, 6); model.ConfirmedOnly = false; model.ComparePrevious = true;
            if (phase >= 8) source.Current = second;
        }
        source.Calls = runtime.Captures = runtime.Validations = 0;
        return new(model, source, runtime);
    }

    /// <summary>Exercises one original synchronous publication boundary, removing all handlers afterward.</summary>
    internal static async Task<AiLoadPublicationObservation> RunAsync(AiLoadPublicationPrepared prepared, int phase)
    {
        var model = prepared.Model; var fired = false;
        NotifyCollectionChangedEventHandler collection = (_, e) =>
        {
            var target = phase is 0 or 2 or 4 ? NotifyCollectionChangedAction.Reset : NotifyCollectionChangedAction.Add;
            if (phase >= 6 || fired || e.Action != target) return;
            fired = true;
            if (phase is 0 or 1) model.RetireRoot();
            else if (phase is 2 or 3) model.From = new(2026, 7, 7);
            else throw new IOException("D963 original collection publication refused.");
        };
        PropertyChangedEventHandler property = (_, e) =>
        {
            if (fired) return;
            if (phase == 6 && e.PropertyName == nameof(model.Loaded) && model.Loaded)
            { fired = true; model.RetireRoot(); }
            if (phase == 9 && e.PropertyName == nameof(model.From))
            { fired = true; model.To = new(2026, 12, 12); }
            if (phase == 10 && e.PropertyName == nameof(model.ConfirmedOnly) && model.ConfirmedOnly)
            { fired = true; model.RetireRoot(); }
        };
        model.Accounts.CollectionChanged += collection; model.PropertyChanged += property;
        try { await model.LoadAsync(); }
        finally { model.Accounts.CollectionChanged -= collection; model.PropertyChanged -= property; }
        return new(fired, model.Loaded, model.HasPendingOperation, model.CanLeave, model.CanShare,
            model.Accounts.Count, model.Accounts.Select(x => x.Selected).ToArray(), model.From, model.To,
            model.ConfirmedOnly, model.ComparePrevious, model.Error, prepared.Source.Calls,
            prepared.Runtime.Captures, prepared.Runtime.Validations, prepared.Runtime.Effects);
    }
}

/// <summary>One original model and original source/access counters; no snapshot reader substitution.</summary>
internal sealed record AiLoadPublicationPrepared(AiAnalysisViewModel Model, AiLoadSwitchFactory Source, AiReviewRuntimeProbe Runtime);
/// <summary>Terminal complete load state, newer draft and explicit source/access counts.</summary>
internal sealed record AiLoadPublicationObservation(bool Fired, bool Loaded, bool Pending, bool CanLeave, bool CanShare,
    int Accounts, bool[] Selected, DateOnly From, DateOnly To, bool ConfirmedOnly, bool ComparePrevious, string? Error,
    int SourceCalls, int Captures, int Validations, int Effects);
/// <summary>Switches only between caller-owned original real SQLite factories to exercise a genuine different-file path.</summary>
internal sealed class AiLoadSwitchFactory(IDbContextFactory<ZananceDbContext> first) : IDbContextFactory<ZananceDbContext>
{
    internal IDbContextFactory<ZananceDbContext> Current = first;
    internal int Calls;
    public ZananceDbContext CreateDbContext() { Calls++; return Current.CreateDbContext(); }
    public Task<ZananceDbContext> CreateDbContextAsync(CancellationToken ct = default)
    { Calls++; return Current.CreateDbContextAsync(ct); }
}
