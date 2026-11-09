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

/// <summary>Portable original-model review preparation; no provider, authorization answer or ledger write is supplied.</summary>
internal static class AiReviewPublicationProbes
{
    /// <summary>Loads one caller-owned metadata account and selects it without preparing a successful review.</summary>
    internal static async Task<AiReviewPublicationPrepared> CreateAsync(IDbContextFactory<ZananceDbContext> original,
        ICommercialWriteAccessSource access, Translator text, ILocalizationService locale, TimeProvider time, IAppInteraction interaction)
    {
        var source = new AiReviewSourceProbe(original);
        var runtime = new AiReviewRuntimeProbe();
        var model = new AiAnalysisViewModel(new AiSnapshotStore(source, access), text, locale, new DateFormatter(locale), time, interaction, runtime);
        await model.LoadAsync();
        if (!model.Loaded || model.Accounts.Count != 1) throw new InvalidDataException("One original metadata account is required.");
        model.Accounts[0].Selected = true;
        source.Calls = 0; runtime.Captures = runtime.Validations = 0;
        return new(model, source, runtime, model.From);
    }

    /// <summary>Exercises one late review boundary; its handler is removed before any explicit Retry.</summary>
    internal static async Task<AiReviewPublicationObservation> RunAsync(AiReviewPublicationPrepared prepared, int phase)
    {
        if (phase is < 0 or > 5) throw new ArgumentOutOfRangeException(nameof(phase));
        var model = prepared.Model;
        var fired = false;
        if (phase == 5) model.ComparePrevious = true;
        PropertyChangedEventHandler handler = (_, args) =>
        {
            var property = phase == 3 ? nameof(AiAnalysisViewModel.Disclosure) : nameof(AiAnalysisViewModel.Summary);
            var value = phase == 3 ? model.Disclosure : model.Summary;
            if (phase >= 4 || args.PropertyName != property || value is null || fired) return;
            fired = true;
            if (phase is 0 or 3) throw new IOException("D962 original review publication refused.");
            if (phase == 1) model.From = prepared.OriginalFrom.AddDays(1);
            else model.RetireRoot();
        };
        if (phase == 4) prepared.Runtime.AfterValidation = count =>
        {
            if (count != 2 || fired) return;
            fired = true; model.RetireRoot();
        };
        model.PropertyChanged += handler;
        try { await model.ReviewAsync(); }
        finally { model.PropertyChanged -= handler; prepared.Runtime.AfterValidation = null; }
        return Observe(prepared, fired);
    }

    /// <summary>Continues only the previously failed model, without replaying its successful metadata preparation.</summary>
    internal static async Task<AiReviewPublicationObservation> RetryAsync(AiReviewPublicationPrepared prepared)
    { await prepared.Model.ReviewAsync(); return Observe(prepared, false); }

    private static AiReviewPublicationObservation Observe(AiReviewPublicationPrepared prepared, bool fired)
    {
        var model = prepared.Model;
        return new(fired, model.CanShare, model.Disclosure, model.Summary, model.Error, model.Loaded,
            model.HasPendingOperation, model.CanLeave, prepared.OriginalFrom, model.From, prepared.Source.Calls,
            prepared.Runtime.Captures, prepared.Runtime.Validations, prepared.Runtime.Effects);
    }
}

/// <summary>One retained original model and its counted source/access ports, suitable for a same-input explicit Retry.</summary>
internal sealed record AiReviewPublicationPrepared(AiAnalysisViewModel Model, AiReviewSourceProbe Source,
    AiReviewRuntimeProbe Runtime, DateOnly OriginalFrom);

/// <summary>Complete preview eligibility/visible state and actual port counters after a single original review attempt.</summary>
internal sealed record AiReviewPublicationObservation(bool Fired, bool CanShare, string? Disclosure, string? Summary,
    string? Error, bool Loaded, bool Pending, bool CanLeave, DateOnly OriginalFrom, DateOnly CurrentFrom,
    int SourceCalls, int Captures, int Validations, int NativeOrProviderEffects);

/// <summary>Counts the original real SQLite context factory; it does not replace the source reader.</summary>
internal sealed class AiReviewSourceProbe(IDbContextFactory<ZananceDbContext> original) : IDbContextFactory<ZananceDbContext>
{
    internal int Calls;
    public ZananceDbContext CreateDbContext() { Interlocked.Increment(ref Calls); return original.CreateDbContext(); }
    public Task<ZananceDbContext> CreateDbContextAsync(CancellationToken ct = default)
    { Interlocked.Increment(ref Calls); return original.CreateDbContextAsync(ct); }
}

/// <summary>Counted local access with no native authentication, credential or HTTP implementation.</summary>
internal sealed class AiReviewRuntimeProbe : IAiRuntime
{
    internal int Captures, Validations, Effects;
    internal Action<int>? AfterValidation;
    public bool NetworkAvailable => true;
    public AiUiAccess Capture(string originalFile)
    {
        Captures++;
        return new(() => { Validations++; AfterValidation?.Invoke(Validations); }, CancellationToken.None);
    }
    public Task<bool> ConfirmDisclosureAsync()
    { Effects++; return Task.FromException<bool>(new InvalidOperationException("No authorization request belongs to this review observation.")); }
    public Task ShareAsync(string exactText)
    { Effects++; return Task.FromException(new InvalidOperationException("No share belongs to this review observation.")); }
    public Task<IAiRemote> OpenAsync(string originalFile, CancellationToken ct)
    { Effects++; return Task.FromException<IAiRemote>(new InvalidOperationException("No identity/provider belongs to this review observation.")); }
}
