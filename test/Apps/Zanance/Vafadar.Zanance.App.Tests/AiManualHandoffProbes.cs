using Microsoft.EntityFrameworkCore;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Analysis;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Analysis;
using Vafadar.Zanance.Data.Commerce;
namespace Vafadar.Zanance.App.Tests;

/// <summary>Original manual disclosure must survive its final access callbacks before any chooser handoff.</summary>
internal static class AiManualHandoffProbes
{
    /// <summary>Prepares one actual zero-ledger review; all consent/recipient ports remain declared synthetic controls.</summary>
    internal static async Task<AiManualPrepared> CreateAsync(IDbContextFactory<ZananceDbContext> factory,
        ICommercialWriteAccessSource rights, Translator text, ILocalizationService locale, TimeProvider time)
    {
        var access = new ManualAccess(rights); var runtime = new ManualRuntime(); var port = new ManualPort();
        var model = new AiAnalysisViewModel(new AiSnapshotStore(factory, access), text, locale, new DateFormatter(locale), time, port, runtime);
        await model.LoadAsync(); if (model.Accounts.Count != 1 || !model.Loaded) throw new InvalidDataException();
        model.Accounts[0].Selected = true; await model.ReviewAsync(); if (!model.CanShare) throw new InvalidDataException();
        return new(model, access, runtime, port, locale, model.From);
    }
    /// <summary>Changes one original owner at capture, access validation or review-rights validation; no native recipient exists.</summary>
    internal static async Task<AiManualObservation> RunAsync(AiManualPrepared p, int phase)
    {
        if (phase is < 0 or > 9) throw new ArgumentOutOfRangeException(nameof(phase));
        bool fired = false;
        void Mutate()
        {
            if (fired) return; fired = true;
            if (phase % 3 == 0) p.Model.RetireRoot();
            else if (phase % 3 == 1) p.Model.From = p.OriginalFrom.AddDays(1);
            else p.Locale.SetLanguage(p.Locale.SupportedLanguages.Single(x => x.CultureName == "de"));
        }
        p.Runtime.Captures = p.Runtime.Validations = 0;
        if (phase < 3) p.Runtime.DuringCapture = Mutate;
        else if (phase < 6) p.Runtime.DuringValidation = Mutate;
        else if (phase < 9) p.Access.DuringCapture = Mutate;
        try { await p.Model.ShareAsync(); }
        finally { p.Runtime.DuringCapture = p.Runtime.DuringValidation = null; p.Access.DuringCapture = null; }
        return new(fired, p.Port.Confirmations, p.Runtime.Authorizations, p.Runtime.Shares, p.Model.HasPendingOperation,
            p.Model.CanLeave, p.Model.From, p.Locale.CurrentLanguage.CultureName, p.Runtime.Opens);
    }
}
/// <summary>The existing original model and exact native/source callbacks; no duplicate review is needed after a failed run.</summary>
internal sealed record AiManualPrepared(AiAnalysisViewModel Model, ManualAccess Access, ManualRuntime Runtime, ManualPort Port,
    ILocalizationService Locale, DateOnly OriginalFrom);
/// <summary>Only original effect counters and retained draft state, without sharing any payload or credential.</summary>
internal sealed record AiManualObservation(bool Fired, int Confirmations, int Authorizations, int Shares, bool Pending,
    bool CanLeave, DateOnly From, string Language, int Opens);
/// <summary>Captures actual cached rights and invokes a bounded reentrant original-owner callback.</summary>
internal sealed class ManualAccess(ICommercialWriteAccessSource original) : ICommercialWriteAccessSource
{
    internal Action? DuringCapture;
    public CommercialWriteAccess Capture(string file) { var value = original.Capture(file); DuringCapture?.Invoke(); return value; }
}
/// <summary>Synthetic native consent only; recipient attempts are counted and always refuse before platform handoff.</summary>
internal sealed class ManualRuntime : IAiRuntime
{
    internal Action? DuringCapture, DuringValidation;
    internal int Captures, Validations, Authorizations, Shares, Opens;
    public bool NetworkAvailable => false;
    public AiUiAccess Capture(string file) { Captures++; DuringCapture?.Invoke(); return new(() => { Validations++; DuringValidation?.Invoke(); }, CancellationToken.None); }
    public Task<bool> ConfirmDisclosureAsync() { Authorizations++; return Task.FromResult(true); }
    public Task ShareAsync(string text) { Shares++; throw new IOException("D975 recipient port refuses before native handoff"); }
    public Task<IAiRemote> OpenAsync(string file, CancellationToken ct) { Opens++; throw new InvalidOperationException(); }
}
/// <summary>Declared non-native confirmation; all unrelated navigation and dialogs refuse.</summary>
internal sealed class ManualPort : IAppInteraction
{
    internal int Confirmations;
    public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel) { Confirmations++; return Task.FromResult(true); }
    public Task AlertAsync(string title, string message, string cancel) => throw new InvalidOperationException();
    public Task<string?> PromptAsync(string title, string message, string accept, string cancel, string? initialValue = null, int maxLength = -1) => throw new InvalidOperationException();
    public Task<string?> ChooseAsync(string title, string cancel, params string[] choices) => throw new InvalidOperationException();
    public Task NavigateAsync(string route, IDictionary<string, object>? parameters = null) => throw new InvalidOperationException();
    public Task ShowFailureAsync(Exception exception) => throw new InvalidOperationException();
    public Task RestoreOnboardingAsync() => throw new InvalidOperationException();
    public void CompleteOnboarding() => throw new InvalidOperationException();
    public void ShowCurrentProfile() => throw new InvalidOperationException();
}
