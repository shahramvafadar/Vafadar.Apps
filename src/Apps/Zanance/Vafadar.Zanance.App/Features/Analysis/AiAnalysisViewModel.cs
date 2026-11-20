using System.Collections.ObjectModel;
using PropertyChangingEventArgs = System.ComponentModel.PropertyChangingEventArgs;
using PropertyChangedEventArgs = System.ComponentModel.PropertyChangedEventArgs;
using System.Net;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Maui.Mvvm;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.Core.Analysis;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Data.Analysis;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.App.Features.Analysis;

/// <summary>Local-only explicit source choice; no name or account id enters an outgoing analysis packet.</summary>
public sealed partial class AiAccountChoice(AiAccount account, Action changed) : ObservableObject
{
    /// <summary>Original local id, never a service wallet identity.</summary>
    public Guid Id => account.Id;
    /// <summary>Original local disclosure caption.</summary>
    public string Caption => account.Name + " · " + account.Currency;
    /// <summary>Explicit choice, off by default.</summary>
    [ObservableProperty] public partial bool Selected { get; set; }
    partial void OnSelectedChanged(bool value) => changed();
}

/// <summary>Complete local review/manual handoff and optional credited boundary; model text never mutates financial data.</summary>
public sealed partial class AiAnalysisViewModel(AiSnapshotStore store, Translator text, ILocalizationService local,
    IDateFormatter dates, TimeProvider time, IAppInteraction interaction, IAiRuntime runtime) : ViewModelBase
{
    private AiAccountScope? scope;
    private AiPeriodHandoff? initialPeriod, detailedPeriod;
    private AiQuestion? question;

    /// <summary>Original question text, retained locally and never included in disclosure.</summary>
    [ObservableProperty] public partial string QuestionText { get; set; } = string.Empty;

    /// <summary>One supported purpose or a bounded scope clarification.</summary>
    [ObservableProperty] public partial string? QuestionStatus { get; private set; }

    /// <summary>A recognized forecast always uses the existing projection's all-recorded basis.</summary>
    public bool CanEditQuestionConfirmation => question?.Intent != AiQuestionIntent.PlanShortfall;

    /// <summary>The selected operation controls comparison, independently of the ordinary Analysis toggle.</summary>
    public bool CanEditQuestionComparison => question is null;

    partial void OnQuestionTextChanged(string value)
    {
        var hadQuestion = question is not null;
        question = null;
        QuestionStatus = null;
        if (hadQuestion) NotifyQuestionScope();
        Invalidate();
    }

    private void NotifyQuestionScope()
    {
        OnPropertyChanged(nameof(CanEditQuestionConfirmation));
        OnPropertyChanged(nameof(CanEditQuestionComparison));
    }

    /// <summary>Applies the finite local interpretation and discloses scope separately through Review.</summary>
    [RelayCommand]
    private async Task ApplyQuestionAsync()
    {
        if (!OwnsPresentation || !Loaded || !CanEditScope || HasPendingOperation || scope is not { } original) return;
        var originalText = QuestionText;
        var version = BeginInvalidation();
        question = null;
        readingSource = operationActive = true;
        try
        {
            ClearInvalidatedPreview(version); Validate(version);
            QuestionStatus = null; Validate(version);
            NotifyQuestionScope(); Validate(version);
            var interpretation = AiQuestionParser.Interpret(originalText);
            if (interpretation is null) { QuestionStatus = text["AiQuestion_Clarify"]; return; }

            var chosenCurrency = interpretation.Currency;
            var today = DateOnly.FromDateTime(time.GetLocalNow().Date);
            StartOperation(version);
            var range = interpretation.NextMonth
                ? await store.UpcomingMonthAsync(original.File, today,
                    Presentation.Calendars.ToPeriod(local.CurrentCalendar), visible?.Token ?? CancellationToken.None)
                : ((DateOnly From, DateOnly To)?)null;
            Validate(version);
            var compare = interpretation.Intent == AiQuestionIntent.SpendingChange;
            if (ComparePrevious != compare) { version++; ComparePrevious = compare; Validate(version); }
            if (!compare && ConfirmedOnly) { version++; ConfirmedOnly = false; Validate(version); }
            if (range is { } dates)
            {
                if (From != dates.From) { version++; From = dates.From; Validate(version); }
                if (To != dates.To) { version++; To = dates.To; Validate(version); }
            }
            question = interpretation;
            NotifyQuestionScope(); Validate(version);
            var hint = text[compare ? "AiQuestion_Spending" : "AiQuestion_Plans"];
            QuestionStatus = chosenCurrency is null ? hint : text.Format("AiQuestion_Currency", hint, chosenCurrency);
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        { if (OwnsPresentation && version == epoch) QuestionStatus = text["AiQuestion_Scope"]; }
        finally { readingSource = operationActive = false; IsBusy = false; Refresh(); }
    }

    /// <summary>Only freely selected analysis has editable dates, account choices and review filters.</summary>
    public bool CanEditScope => detailedPeriod is null;

    /// <summary>Offers an original editable draft only after the local funding facts have been reviewed.</summary>
    public bool CanOpenFundingDraft => OwnsPresentation && review is not null && detailedPeriod?.OpenDraftAsync is not null && !HasPendingOperation;
    private AiReview? review;
    private AiOffer? offer;
    private IAiRemote? remote;
    private AiAvailability? availability;
    private AiClientRecord? retained;
    private int epoch;
    private long frameEpoch;
    private string? errorKey;
    private bool active;
    private long choiceEpoch;
    private bool operationActive, readingSource, rootRetired;
    private bool metadataReady, stagingMetadata;
    private CancellationTokenSource? visible;
    private Func<bool>? ownsRoot;
    private CancellationToken featureSession;
    private CancellationTokenRegistration featureRetirement;
    private bool OwnsPresentation => active && !featureSession.IsCancellationRequested && !rootRetired && (ownsRoot?.Invoke() ?? true);
    /// <summary>Only the actual original native root may own this analysis page.</summary>
    internal void BindRootOwner(Func<bool> owner) => ownsRoot = owner;
    /// <summary>Captures the source route before its first load, without reviewing or sending anything.</summary>
    internal void SetInitialPeriod(AiPeriodHandoff period)
    {
        if (!Loaded && !rootRetired) initialPeriod = period;
    }
    /// <summary>Root replacement permanently retires the old disclosure and private connection.</summary>
    internal void RetireRoot() { rootRetired = true; Retire(); }
    /// <summary>Cancellable reads permit Back. Native disclosure and submission keep their owner.</summary>
    public bool CanLeave => !operationActive || readingSource || featureSession.IsCancellationRequested;
    /// <summary>Actual appearance does not silently retry a visible failed source.</summary>
    internal Task ActivateAsync() => OwnsPresentation || rootRetired ? Task.CompletedTask : LoadAsync();
    /// <summary>Actual command ownership cannot be released by mutable shared busy presentation.</summary>
    public bool HasPendingOperation => operationActive || IsBusy;
    /// <summary>Actual available accounts; none is silently selected.</summary>
    public ObservableCollection<AiAccountChoice> Accounts { get; } = [];
    /// <summary>Explicit first stored Gregorian date, displayed through the existing calendar controls.</summary>
    [ObservableProperty] public partial DateOnly From { get; set; } = FirstDay(time);
    /// <summary>Explicit last stored Gregorian date; never an unset date-field draft.</summary>
    [ObservableProperty] public partial DateOnly To { get; set; } = DateOnly.FromDateTime(time.GetLocalNow().Date);
    /// <summary>Default minimal confirmed-data scope; excluded unreviewed count is disclosed.</summary>
    [ObservableProperty] public partial bool ConfirmedOnly { get; set; } = true;
    /// <summary>Optional equally long explicitly disclosed previous interval.</summary>
    [ObservableProperty] public partial bool ComparePrevious { get; set; }
    /// <summary>Only enables input after the complete actual account read.</summary>
    [ObservableProperty] public partial bool Loaded { get; private set; }
    /// <summary>All account/date validation appears beside the relevant input.</summary>
    [ObservableProperty] public partial string? AccountsError { get; private set; }
    /// <summary>Original date-range error.</summary>
    [ObservableProperty] public partial string? FromError { get; private set; }
    /// <summary>Original date-range error.</summary>
    [ObservableProperty] public partial string? ToError { get; private set; }
    /// <summary>Current content-free operation feedback.</summary>
    [ObservableProperty] public partial string? Error { get; private set; }
    /// <summary>Readable exact selected aggregate summary, without source identities.</summary>
    [ObservableProperty] public partial string? Summary { get; private set; }
    /// <summary>Complete original outgoing JSON; numbers are exact ISO-currency minor units, not culture-parsed values.</summary>
    [ObservableProperty] public partial string? Disclosure { get; private set; }
    /// <summary>Actual service/provider/model/max-unit terms, never invented price or purchased entitlement.</summary>
    [ObservableProperty] public partial string? ProviderText { get; private set; }
    /// <summary>Purchased/gifted available/reserved quantities remain visibly distinct.</summary>
    [ObservableProperty] public partial string? BalanceText { get; private set; }
    /// <summary>Original approved historical packet for an existing pending/completed result.</summary>
    [ObservableProperty] public partial string? ResultSnapshot { get; private set; }
    /// <summary>Untrusted read-only plain text only, never markup or executable suggestions.</summary>
    [ObservableProperty] public partial string? ResultText { get; private set; }
    /// <summary>Exact retained request state/charged quantity.</summary>
    [ObservableProperty] public partial string? ResultStatus { get; private set; }
    /// <summary>Manual chooser handoff observation only; no receipt from its eventual recipient is inferred.</summary>
    [ObservableProperty] public partial string? ShareStatus { get; private set; }
    /// <summary>Gets whether the exact original review can be deliberately shared.</summary>
    public bool CanShare => OwnsPresentation && !HasPendingOperation && review is not null;
    /// <summary>Gets whether this edition can explicitly inspect the configured provider without financial disclosure.</summary>
    public bool NetworkEnabled => runtime.NetworkAvailable;
    /// <summary>Explicit metadata check is separate from local preparation.</summary>
    public bool CanCheckProvider => OwnsPresentation && Loaded && !HasPendingOperation && runtime.NetworkAvailable;
    /// <summary>Gets whether metadata-only original terms can be requested.</summary>
    public bool CanQuote => OwnsPresentation && !HasPendingOperation && review is not null && metadataReady && remote is not null &&
        availability is { } status && ((review.Selection.ComparePrevious || review.Selection.Forecast is not null || review.Selection.Question?.Intent == AiQuestionIntent.PlanShortfall) ? status.ComparisonConfigured ?? status.Configured : status.Configured) && !Pending;
    /// <summary>Gets whether current literal terms and actual available quantities permit a request attempt; the service rechecks authority.</summary>
    public bool CanSend => CanQuote && offer is not null && availability is not null &&
        checked(availability.Balance.PurchasedAvailable + availability.Balance.GiftedAvailable) >= offer.MaximumUnits;
    /// <summary>Gets whether an existing private original request needs explicit continuation or closure.</summary>
    public bool CanContinue => OwnsPresentation && !HasPendingOperation && metadataReady && remote is not null && Pending;
    private bool Pending => retained is not null && (retained.Result is null || retained.Result.State == AiRequestState.Pending);
    partial void OnFromChanged(DateOnly value) => Invalidate();
    partial void OnToChanged(DateOnly value) => Invalidate();
    partial void OnConfirmedOnlyChanged(bool value) => Invalidate();
    partial void OnComparePreviousChanged(bool value) => Invalidate();
    private int Invalidate()
    {
        var original = BeginInvalidation(); ClearInvalidatedPreview(original); return original;
    }
    private int BeginInvalidation()
    {
        var original = Interlocked.Increment(ref epoch); review = null; offer = null; return original;
    }
    private void ClearInvalidatedPreview(int original)
    {
        // D-965: a notification may complete a newer local review before this older clear resumes.
        Summary = null; if (original != Volatile.Read(ref epoch)) return;
        Disclosure = null; if (original != Volatile.Read(ref epoch)) return;
        ShareStatus = null; if (original != Volatile.Read(ref epoch)) return;
        Refresh();
    }

    // D-958: these notifications can synchronously retire the page or change its draft.
    // Every caller enters its original cleanup before publishing and validates before an effect.
    private void StartOperation(int original)
    {
        IsBusy = true; Validate(original);
        SetError(null); Validate(original);
        Refresh(); Validate(original);
    }
    /// <summary>Stops pending native/network publication into a departed profile/form while preserving private continuation.</summary>
    public void Retire()
    {
        // A cancellation callback can retire its own registration.
        featureRetirement.Unregister();
        featureRetirement = default;
        // Detach before any callback/disposal can reenter retirement or adopt a newer connection.
        var detached = remote; remote = null; metadataReady = false;
        local.Changed -= OnLanguageChanged; frameEpoch++; active = false;
        visible?.Cancel(); visible?.Dispose(); visible = null;
        try { Loaded = false; choiceEpoch++; Invalidate(); }
        finally { detached?.Dispose(); Refresh(); }
    }
    private void RetireDisabledFeature(CancellationToken original)
    {
        if (!active || featureSession != original) return;
        availability = null;
        retained = null;
        // Clear all private display values before any binding callback can expose a partial retirement.
        stagingMetadata = true;
        try
        {
            Summary = Disclosure = ShareStatus = ResultSnapshot = ResultText = ResultStatus = BalanceText = null;
            ProviderText = text["Ai_NoProvider"];
        }
        finally { stagingMetadata = false; }

        try { Retire(); }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            // A presentation callback must not reject a device-wide privacy change.
        }
        foreach (var field in new[]
        {
            nameof(Summary), nameof(Disclosure), nameof(ShareStatus), nameof(ResultSnapshot),
            nameof(ResultText), nameof(ResultStatus), nameof(BalanceText), nameof(ProviderText),
        })
        {
            try { OnPropertyChanged(field); }
            catch (Exception error) when (error is not OutOfMemoryException)
            {
                // Keep withdrawing the remaining display fields even if one binding fails.
            }
        }
    }

    /// <summary>Loads actual account choices only, with no HTTP, automatic selection, settings write or model run.</summary>
    [RelayCommand] public async Task LoadAsync()
    {
        var feature = runtime.FeatureSession;
        if (feature.IsCancellationRequested || HasPendingOperation || rootRetired || ownsRoot?.Invoke() == false) return;
        if (featureSession.IsCancellationRequested) { Retire(); }
        featureSession = feature;
        local.Changed -= OnLanguageChanged; local.Changed += OnLanguageChanged;
        frameEpoch++; active = true; visible ??= CancellationTokenSource.CreateLinkedTokenSource(feature); var ct = visible.Token; var original = ++epoch;
        var selected = Accounts.Where(a => a.Selected).Select(a => a.Id).ToHashSet(); var oldFile = scope?.File;
        readingSource = true; operationActive = true; metadataReady = false;
        try
        {
            featureRetirement.Unregister();
            featureRetirement = feature.Register(() => RetireDisabledFeature(feature));
            Validate(original);
            StartOperation(original); Loaded = false; Validate(original);
            remote?.Dispose(); remote = null; availability = null; retained = null; offer = null; review = null;
            Summary = Disclosure = ResultText = ResultStatus = ResultSnapshot = ShareStatus = BalanceText = null; ProviderText = text["Ai_NoProvider"];
            Validate(original);
            var current = await store.AccountsAsync(ct); Validate(original); runtime.Capture(current.File).Validate(); Validate(original);
            var incoming = initialPeriod ?? detailedPeriod;
            if (incoming is not null)
            {
                if (incoming.File != current.File || incoming.Accounts.Any(id => current.Accounts.All(account => account.Id != id)))
                    throw new InvalidOperationException("The original report scope is no longer available.");
                selected = incoming.Accounts.ToHashSet();
                Reset(From, incoming.From, value => From = value);
                Reset(To, incoming.To, value => To = value);
                Reset(ConfirmedOnly, incoming.ConfirmedOnly, value => ConfirmedOnly = value);
                Reset(ComparePrevious, incoming.ComparePrevious, value => ComparePrevious = value);
            }
            else if (oldFile is not null && oldFile != current.File)
            {
                // D-963: only our own default changes advance the expected revision. A nested draft edit
                // or root retirement must stop before the next default or account publication.
                Reset(From, FirstDay(time), value => From = value);
                Reset(To, DateOnly.FromDateTime(time.GetLocalNow().Date), value => To = value);
                Reset(ConfirmedOnly, true, value => ConfirmedOnly = value);
                Reset(ComparePrevious, false, value => ComparePrevious = value);
                AccountsError = null; Validate(original); FromError = null; Validate(original); ToError = null; Validate(original);
            }
            var originalChoices = choiceEpoch + 1;
            // Build restored choices before their callbacks become live; restoring a selection is not a new user edit.
            var replacement = current.Accounts.Select(account => new AiAccountChoice(account,
                () => { if (OwnsPresentation && originalChoices == choiceEpoch) Invalidate(); })
                { Selected = (incoming is not null || current.File == oldFile) && selected.Contains(account.Id) }).ToArray();
            Validate(original); scope = current; choiceEpoch = originalChoices;
            Accounts.Clear(); Validate(original);
            foreach (var account in replacement) { Accounts.Add(account); Validate(original); }
            detailedPeriod = incoming?.IsDetailed == true ? incoming : null;
            OnPropertyChanged(nameof(CanEditScope)); OnPropertyChanged(nameof(CanOpenFundingDraft)); Validate(original);
            Loaded = true; Validate(original);
            if (ReferenceEquals(initialPeriod, incoming)) initialPeriod = null;

            void Reset<T>(T before, T value, Action<T> publish)
            {
                Validate(original);
                if (EqualityComparer<T>.Default.Equals(before, value)) return;
                original = unchecked(original + 1);
                publish(value); Validate(original);
            }
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            if (OwnsPresentation && original == epoch) { Loaded = false; Validate(original); SetError("Ai_ScopeError"); }
        }
        finally { operationActive = false; readingSource = false; IsBusy = false; Refresh(); }
    }
    /// <summary>Prepares an exact bounded aggregate review from the original selected file, without any provider request.</summary>
    [RelayCommand] public async Task ReviewAsync()
    {
        if (!OwnsPresentation || !Loaded || HasPendingOperation || scope is null) return;
        var original = epoch;
        if (detailedPeriod is null && !string.IsNullOrWhiteSpace(QuestionText) && question is null)
        { QuestionStatus = text["AiQuestion_Clarify"]; return; }
        var applied = detailedPeriod is null ? question : null;
        var today = DateOnly.FromDateTime(time.GetLocalNow().Date);
        var selected = Accounts.Where(account => account.Selected).Select(account => account.Id).ToArray();
        if (applied is not null)
        {
            var currencies = scope.Accounts.Where(account => selected.Contains(account.Id)).Select(account => account.Currency)
                .Distinct(StringComparer.Ordinal).Take(2).ToArray();
            if (currencies.Length != 1 || applied.Currency is { } currency && currencies[0] != currency ||
                applied.Intent == AiQuestionIntent.PlanShortfall && (From < today || To.DayNumber - today.DayNumber > 90))
            {
                Invalidate();
                QuestionStatus = text["AiQuestion_Scope"];
                return;
            }
        }
        var selection = new AiSelection(scope.File, selected, From, To, ConfirmedOnly, ComparePrevious,
            local.CurrentLanguage.CultureName.Split('-')[0], detailedPeriod?.CompareFrom, detailedPeriod?.CompareTo, detailedPeriod?.Report, detailedPeriod?.Funding, detailedPeriod?.Forecast, applied,
            applied is null ? null : today, applied is null ? null : Presentation.Calendars.ToPeriod(local.CurrentCalendar));
        readingSource = true; operationActive = true;
        try
        {
            StartOperation(original);
            AccountsError = selection.AccountIds.Count > 0 ? null : text["Ai_ChooseAccounts"]; Validate(original);
            FromError = selection.From == DateOnly.MinValue ? text["Ai_InvalidRange"] : null; Validate(original);
            ToError = selection.To < selection.From || selection.To.DayNumber - selection.From.DayNumber > 365
                || selection.ComparePrevious && selection.CompareFrom is null && selection.From.DayNumber <= selection.To.DayNumber - selection.From.DayNumber + 1 ? text["Ai_InvalidRange"] : null;
            Validate(original);
            original = Invalidate(); Validate(original);
            if (AccountsError is not null || FromError is not null || ToError is not null) return;
            var access = runtime.Capture(selection.File); access.Validate(); Validate(original);
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(access.Cancellation, visible?.Token ?? CancellationToken.None);
            if (detailedPeriod is { } detail)
            {
                if (selection.From != detail.From || selection.To != detail.To || selection.ComparePrevious != detail.ComparePrevious ||
                    selection.ConfirmedOnly != detail.ConfirmedOnly || !selection.AccountIds.ToHashSet().SetEquals(detail.Accounts) ||
                    detail.ValidateReportAsync is null)
                    throw new InvalidOperationException("The displayed report scope changed.");
                await detail.ValidateReportAsync(); Validate(original);
            }

            var prepared = await store.ReviewAsync(selection, linked.Token); Validate(original); access.Validate(); Validate(original);
            if (detailedPeriod?.ValidateReportAsync is { } validateReport) { await validateReport(); Validate(original); }
            var payload = prepared.Payload; var summary = Period(payload.Current);
            if (payload.Previous is not null) summary += Environment.NewLine + Period(payload.Previous);
            if (applied is not null) summary = QuestionSummary(prepared, payload, summary);
            else if (payload.Forecast is { } forecast && detailedPeriod is { } projection)
                summary = ForecastSummary(payload.Current, forecast, projection.ReportLabels);
            else if (payload.Funding is { } funding)
            {
                var label = detailedPeriod?.ReportLabels.FirstOrDefault();
                summary = (label is null ? string.Empty : label + Environment.NewLine) + FundingSummary(funding);
            }
            else if (detailedPeriod is { } report) summary += Environment.NewLine + ReportSummary(report, payload);
            var disclosure = JsonSerializer.Serialize(payload, AiJson.Default.AiPayload); Validate(original);
            // D-962: native/binding notifications may fail, edit the draft or retire the original page.
            // Sharing authority is committed only after the complete current preview was published.
            Summary = summary; Validate(original);
            Disclosure = disclosure; Validate(original);
            review = prepared; OnPropertyChanged(nameof(CanOpenFundingDraft));
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            if (OwnsPresentation && original == epoch)
            {
                // A failed preview is not accepted work; preserve a newer draft or a retired owner's state.
                var failed = Invalidate();
                if (OwnsPresentation && failed == epoch) SetError("Ai_ScopeError");
            }
        }
        finally { operationActive = false; readingSource = false; IsBusy = false; Refresh(); }
    }
    /// <summary>Deliberately offers the exact local summary/prompt to another app; this never invokes a model or consumes credit.</summary>
    [RelayCommand] public async Task ShareAsync()
    {
        if (!CanShare || review is not { } original) return;
        var version = epoch; var offered = false; var disclosure = NativeDisclosure(); operationActive = true;
        try
        {
            StartOperation(version); ValidateDisclosure(disclosure); ValidateReview(version, original);
            if (!await interaction.ConfirmAsync(text["Ai_Title"], text["Ai_ManualConfirm"], text["Ai_ConfirmShareAction"], text["Ai_ConfirmCancelAction"])) return;
            ValidateDisclosure(disclosure); ValidateReview(version, original);
            if (!await runtime.ConfirmDisclosureAsync()) return;
            ValidateDisclosure(disclosure); ValidateReview(version, original); await CheckOriginalAsync(original);
            ValidateDisclosure(disclosure); ValidateReview(version, original); runtime.Capture(original.Selection.File).Validate();
            // D-975: access validation is an external boundary; a returned callback cannot authorize a retired chooser.
            ValidateDisclosure(disclosure); ValidateReview(version, original);
            offered = true; await runtime.ShareAsync(text["Ai_ManualPrompt"] + Environment.NewLine + JsonSerializer.Serialize(original.Payload, AiJson.Default.AiPayload));
            if (OwnsPresentation && version == epoch) ShareStatus = text["Ai_Offered"];
        }
        catch (Exception error) when (error is not OutOfMemoryException) { if (OwnsPresentation && version == epoch) { Invalidate(); SetError(offered ? "Ai_ShareUnconfirmed" : "Ai_RefreshRequired"); } }
        finally { operationActive = false; readingSource = false; IsBusy = false; Refresh(); }
    }
    /// <summary>Reads only identity/provider/wallet metadata; no selected financial payload or automatic sign-in is sent.</summary>
    [RelayCommand] public async Task CheckProviderAsync()
    {
        if (!CanCheckProvider || scope is null || visible is not { } owner) return;
        var cancellation = owner.Token;
        var version = epoch; var originalFile = scope.File; readingSource = operationActive = true; metadataReady = false; IAiRemote? opened = null;
        try
        {
            StartOperation(version); offer = null;
            // Cancel metadata reads if their page retires, even when the profile and AI preference stay active.
            opened = await runtime.OpenAsync(originalFile, cancellation); Validate(version);
            var status = await opened.AvailabilityAsync(cancellation); Validate(version);
            var saved = await opened.ReadAsync(cancellation); Validate(version);
            // Stage one coherent metadata snapshot before the first binding callback.
            var configured = status.Configured || status.ComparisonConfigured == true;
            var destination = configured || saved is not null ? opened.Destination : null; Validate(version);
            var provider = configured ? destination : text["Ai_NoProvider"];
            var balance = BalanceCaption(status.Balance);
            var snapshot = saved is null ? null : destination + Environment.NewLine + JsonSerializer.Serialize(saved.Submission, AiJson.Default.AiSubmission);
            var result = saved?.Result?.Text; var state = saved is null ? null : RecordStatus(saved); Validate(version);
            var detached = remote; remote = null;
            if (!ReferenceEquals(detached, opened)) detached?.Dispose();
            Validate(version);
            PublishMetadata(version, provider, balance, snapshot, result, state, () =>
            {
                remote = opened; opened = null; availability = status; retained = saved;
            });
            // A partial/obsolete notification sequence never grants Continue/Quote/Send authority.
            metadataReady = true;
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            if (OwnsPresentation && version == epoch)
            {
                var failed = remote; remote = null; availability = null; metadataReady = false;
                failed?.Dispose();
                if (OwnsPresentation && version == epoch) SetError(FailureKey(error));
            }
        }
        finally
        {
            try { opened?.Dispose(); }
            finally { operationActive = false; readingSource = false; IsBusy = false; Refresh(); }
        }
    }
    // D-966: changing observers see the old complete values; changed observers see the new
    // complete values. Revalidate after each callback, before any later publication/authority.
    private void PublishMetadata(int version, string? provider, string? balance, string? snapshot, string? result, string? state, Action adopt)
    {
        var fields = new (string Name, string? Before, string? After)[]
        {
            (nameof(ProviderText), ProviderText, provider), (nameof(BalanceText), BalanceText, balance),
            (nameof(ResultSnapshot), ResultSnapshot, snapshot), (nameof(ResultText), ResultText, result),
            (nameof(ResultStatus), ResultStatus, state),
        };
        var changed = fields.Where(field => field.Before != field.After).Select(field => field.Name).ToArray();
        foreach (var name in changed) { base.OnPropertyChanging(new PropertyChangingEventArgs(name)); Validate(version); }
        stagingMetadata = true;
        try { ProviderText = provider; BalanceText = balance; ResultSnapshot = snapshot; ResultText = result; ResultStatus = state; }
        finally { stagingMetadata = false; }
        adopt();
        foreach (var name in changed) { base.OnPropertyChanged(new PropertyChangedEventArgs(name)); Validate(version); }
        Validate(version);
    }
    /// <summary>Metadata and accepted-result assignment publish only after their complete snapshot has been installed.</summary>
    protected override void OnPropertyChanging(PropertyChangingEventArgs e) { if (!stagingMetadata) base.OnPropertyChanging(e); }
    /// <summary>Ordinary input notifications remain immediate; coherent metadata/result stages publish explicitly.</summary>
    protected override void OnPropertyChanged(PropertyChangedEventArgs e) { if (!stagingMetadata) base.OnPropertyChanged(e); }

    /// <summary>Fetches explicit metadata-only original terms; the native Send confirmation still has not happened.</summary>
    [RelayCommand] public async Task QuoteAsync()
    {
        if (!CanQuote || remote is null || review is null || visible is not { } owner) return;
        var cancellation = owner.Token;
        var version = epoch; var original = review; var connection = remote; readingSource = operationActive = true;
        try
        {
            // D-979: beginning a replacement quote revokes earlier terms before any busy/binding callback.
            offer = null; StartOperation(version); ValidateReview(version, original);
            var terms = await connection.OfferAsync(original.Payload.Purpose, cancellation); ValidateReview(version, original);
            var caption = text.Format("Ai_Offer", connection.Destination, terms.Provider, terms.Model, N(terms.MaximumUnits), terms.ExpiresAt.ToString("u", local.CurrentCulture));
            ValidateReview(version, original);
            PublishMetadata(version, caption, BalanceText, ResultSnapshot, ResultText, ResultStatus, () => { });
            ValidateReview(version, original);
            // D-978: fallible caption notifications cannot grant sending authority for unpublished terms.
            offer = terms;
            if (!CanAfford(terms)) SetError("Ai_Insufficient");
        }
        catch (Exception error) when (error is not OutOfMemoryException) { if (OwnsPresentation && version == epoch) SetError(FailureKey(error)); }
        finally { operationActive = false; readingSource = false; IsBusy = false; Refresh(); }
    }
    /// <summary>Sends only after native literal recipient/cost consent, private intent persistence and source revalidation.</summary>
    [RelayCommand] public async Task SendAsync()
    {
        if (!CanSend || review is not { } original || offer is not { } terms || remote is not { } connection) return;
        var version = epoch; var presentation = frameEpoch; var returned = false; var enteredSubmission = false;
        var disclosure = NativeDisclosure(); operationActive = true;
        try
        {
            StartOperation(version); ValidateDisclosure(disclosure); ValidateReview(version, original);
            // D-984: the native confirmation needs a short action at large text; its complete disclosure stays explicit.
            if (!await interaction.ConfirmAsync(text["Ai_Title"], text.Format("Ai_SendConfirm", connection.Destination, terms.Provider, terms.Model, N(terms.MaximumUnits)), text["Ai_ConfirmSendAction"], text["Ai_ConfirmCancelAction"])) return;
            ValidateDisclosure(disclosure); ValidateReview(version, original); if (!await runtime.ConfirmDisclosureAsync()) return; ValidateDisclosure(disclosure); ValidateReview(version, original);
            enteredSubmission = true;
            var result = await connection.SubmitAsync(terms, original.Payload, async () => { ValidateDisclosure(disclosure); ValidateReview(version, original); await CheckOriginalAsync(original); ValidateDisclosure(disclosure); ValidateReview(version, original); }, CancellationToken.None);
            ValidateFrame(presentation);
            // Preserve only the returned original result before fallible private-record follow-through.
            returned = true; retained = new(1, new(result.RequestId, terms, original.Payload), result); ShowRecord(retained, true); offer = null;
            var accepted = retained!; var saved = await connection.ReadAsync(CancellationToken.None); ValidateFrame(presentation);
            retained = ValidateRecordFollowup(accepted, saved); ShowRecord(retained, true);
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            if (OwnsPresentation && presentation == frameEpoch)
            {
                // A failed startup/consent has not entered the original submission client.
                // It must not create a speculative private-record read or erase a newer draft's feedback.
                if (!enteredSubmission)
                { if (version == epoch) { SetError(FailureKey(error)); offer = null; } return; }
                SetError(FailureKey(error)); offer = null;
                if (returned) return; // Known original result needs no speculative second read or primary replay.
                try
                {
                    var saved = await connection.ReadAsync(CancellationToken.None);
                    if (OwnsPresentation && presentation == frameEpoch) { retained = saved; ShowRecord(retained); if (version == epoch) Invalidate(); }
                }
                catch (Exception lost) when (lost is not OutOfMemoryException) { if (OwnsPresentation && presentation == frameEpoch) { remote?.Dispose(); remote = null; if (version == epoch) Invalidate(); } }
            }
        }
        finally { operationActive = false; readingSource = false; IsBusy = false; Refresh(); }
    }
    /// <summary>Explicitly continues only the retained original consent; absence may replay that identical packet, not a new request.</summary>
    [RelayCommand] public Task ContinueAsync() => ContinueCoreAsync(false);
    /// <summary>Explicitly closes an unaccepted id; a dispatched request stays pending until actual provider resolution.</summary>
    [RelayCommand] public Task CloseAsync() => ContinueCoreAsync(true);
    private async Task ContinueCoreAsync(bool close)
    {
        if (!CanContinue || remote is not { } connection || retained is not { } record) return;
        var version = epoch; var presentation = frameEpoch; var disclosure = NativeDisclosure(); operationActive = true;
        try
        {
            StartOperation(version); ValidateDisclosure(disclosure);
            // D-984: keep the full retained-request warning and use a concise native action.
            var key = close ? "Ai_ConfirmCloseAction" : "Ai_ConfirmContinueAction";
            var message = close ? "Ai_CloseConfirm" : record.DispatchPermitted == true ? "Ai_ContinueConfirm" : "Ai_StatusOnlyConfirm";
            if (!await interaction.ConfirmAsync(text["Ai_Title"], text[message], text[key], text["Ai_ConfirmCancelAction"])) return;
            ValidateDisclosure(disclosure); Validate(version); if (!close && !await runtime.ConfirmDisclosureAsync()) return; ValidateDisclosure(disclosure); Validate(version);
            var result = close ? await connection.CloseAsync(record, CancellationToken.None) : await connection.ContinueAsync(record, CancellationToken.None);
            ValidateFrame(presentation); retained = record with { Result = result }; ShowRecord(retained, true);
            var accepted = retained!; var saved = await connection.ReadAsync(CancellationToken.None); ValidateFrame(presentation);
            retained = ValidateRecordFollowup(accepted, saved); ShowRecord(retained, true);
        }
        catch (Exception error) when (error is not OutOfMemoryException) { if (OwnsPresentation && version == epoch) SetError(FailureKey(error)); }
        finally { operationActive = false; readingSource = false; IsBusy = false; Refresh(); }
    }
    /// <summary>Opens only the existing optional user-owned identity flow, without taking credentials or starting a financial account.</summary>
    [RelayCommand] private Task IdentityAsync() => OwnsPresentation && Loaded && !HasPendingOperation ? interaction.NavigateAsync(AppRoutes.BillingIdentityRoute) : Task.CompletedTask;
    // D-665: native acceptance belongs to the actual shown language/captions, not a translated continuation.
    private string NativeDisclosure() => string.Join("\n", new[] { local.CurrentCulture.Name, text["Ai_Title"],
        text["Ai_ManualConfirm"], text["Ai_ManualShare"], text["Ai_ConfirmShareAction"], text["Ai_ManualPrompt"], text["Ai_SendConfirm"],
        text["Ai_Send"], text["Ai_ConfirmSendAction"], text["Ai_CloseConfirm"], text["Ai_ContinueConfirm"], text["Ai_StatusOnlyConfirm"], text["Ai_Close"], text["Ai_Continue"],
        text["Ai_ConfirmCloseAction"], text["Ai_ConfirmContinueAction"], text["Ai_ConfirmCancelAction"], text["Common_Cancel"] });
    private void ValidateDisclosure(string original)
    {
        if (!string.Equals(original, NativeDisclosure(), StringComparison.Ordinal))
            throw new InvalidOperationException("Original analysis native disclosure changed.");
    }
    private bool CanAfford(AiOffer terms) => availability is not null && checked(availability.Balance.PurchasedAvailable + availability.Balance.GiftedAvailable) >= terms.MaximumUnits;
    private void ShowRecord(AiClientRecord? record, bool applyBalance = false)
    {
        var frame = frameEpoch;
        // D-969: resolve fallible destination/formatting before clearing any known outcome.
        var destination = record is null ? null : remote?.Destination; ValidateFrame(frame);
        var snapshot = record is null ? null : destination + Environment.NewLine + JsonSerializer.Serialize(record.Submission, AiJson.Default.AiSubmission);
        var result = record?.Result?.Text;
        var state = record is null ? null : RecordStatus(record);
        var balance = applyBalance && record?.Result is not null ? BalanceCaption(record.Result.Balance) : BalanceText;
        var fields = new (string Name, string? Before, string? After)[]
        {
            (nameof(ResultSnapshot), ResultSnapshot, snapshot), (nameof(ResultText), ResultText, result),
            (nameof(ResultStatus), ResultStatus, state), (nameof(BalanceText), BalanceText, balance),
        };
        var changed = fields.Where(field => field.Before != field.After).Select(field => field.Name).ToArray();
        foreach (var name in changed) { base.OnPropertyChanging(new PropertyChangingEventArgs(name)); ValidateFrame(frame); }
        // A changing observer may switch language. Reformat the same immutable result in that
        // current culture, without invalidating an independently newer editable date draft.
        state = record is null ? null : RecordStatus(record);
        balance = applyBalance && record?.Result is not null ? BalanceCaption(record.Result.Balance) : BalanceText;
        ValidateFrame(frame); stagingMetadata = true;
        try { ResultSnapshot = snapshot; ResultText = result; ResultStatus = state; BalanceText = balance; }
        finally { stagingMetadata = false; }
        if (applyBalance && record?.Result is not null && availability is not null)
            availability = availability with { Balance = record.Result.Balance };
        // Changed observers see the complete accepted record and matching quantities. A retired
        // callback cannot continue into another notification or private-record read.
        foreach (var name in changed) { base.OnPropertyChanged(new PropertyChangedEventArgs(name)); ValidateFrame(frame); }
        ValidateFrame(frame);
    }
    // D-969: an already returned outcome belongs to its literal original request. A missing,
    // foreign or regressed private read cannot erase that known receipt or permit another send.
    private static AiClientRecord ValidateRecordFollowup(AiClientRecord original, AiClientRecord? saved)
    {
        if (saved is null || saved.Version != original.Version || saved.Result is null ||
            JsonSerializer.Serialize(saved.Submission, AiJson.Default.AiSubmission) != JsonSerializer.Serialize(original.Submission, AiJson.Default.AiSubmission) ||
            original.Result is { State: not AiRequestState.Pending } && saved.Result != original.Result)
            throw new InvalidDataException("Original analysis follow-up differs from its returned outcome.");
        // The same pending request may genuinely advance while its status is being persisted.
        return saved;
    }
    private string RecordStatus(AiClientRecord record) => record.Result?.State switch
        {
            AiRequestState.Completed => text.Format("Ai_ResultUnits", N(record.Result.ChargedUnits)),
            null when record.DispatchPermitted == false => text["Ai_LocalReviewPending"],
            null => text["Ai_OutcomeUnknown"],
            AiRequestState.Refunded => text["Ai_Refunded"], AiRequestState.NotAccepted => text["Ai_NotAccepted"], _ => text["Ai_Pending"],
        };
    private void ShowBalance(AiBalance balance) => BalanceText = BalanceCaption(balance);
    private string BalanceCaption(AiBalance balance) => text.Format("Ai_Balance", N(balance.PurchasedAvailable), N(balance.GiftedAvailable), N(balance.PurchasedReserved), N(balance.GiftedReserved));
    /// <summary>Opens the original editor after checking the same reviewed funding source.</summary>
    [RelayCommand] public async Task OpenFundingDraftAsync()
    {
        if (!CanOpenFundingDraft || operationActive || review is not { } original ||
            detailedPeriod?.OpenDraftAsync is not { } open) return;
        var version = epoch;
        readingSource = operationActive = true;
        try
        {
            StartOperation(version); ValidateReview(version, original);
            await CheckOriginalAsync(original); ValidateReview(version, original);
            await open();
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        { if (OwnsPresentation && version == epoch) SetError(FailureKey(error)); }
        finally { readingSource = operationActive = false; IsBusy = false; Refresh(); }
    }

    private string QuestionSummary(AiReview prepared, AiPayload payload, string spendingSummary)
    {
        if (payload.Forecast is { } forecast)
            return text["AiQuestion_ForecastSource"] + Environment.NewLine +
                ForecastSummary(payload.Current, forecast, prepared.QuestionLabels);
        var lines = payload.Report?.Categories?.Select((item, index) => text.Format("AiQuestion_CategoryRow",
            N(index + 1), string.IsNullOrWhiteSpace(prepared.QuestionLabels[index]) ? text["Category_Uncategorized"] : prepared.QuestionLabels[index],
            MoneyText.Format(item.Current, payload.Current.Totals[0].CurrencyCode, local.CurrentCulture),
            MoneyText.Format(item.Previous, payload.Current.Totals[0].CurrencyCode, local.CurrentCulture)));
        return text["AiQuestion_ReportSource"] + Environment.NewLine + spendingSummary + Environment.NewLine +
            (lines is null ? text["Report_NoChanges"] : string.Join(Environment.NewLine, lines));
    }

    private string ForecastSummary(AiPeriod period, AiForecastFacts forecast, IReadOnlyList<string> localLabels)
    {
        var preview = new List<string>
        {
            text.Format("Forecast_Basis", Day(period.From), Day(period.To)),
            text.Format("Ai_ForecastAssumptions", N(AiProtocol.MaximumReportRows)),
        };
        if (forecast.UnknownBeforeWindow > 0) preview.Add(text.Format("AiQuestion_EarlierUnknown", N(forecast.UnknownBeforeWindow)));
        Add("Ai_ForecastBaseline", forecast.Baseline, 0);
        if (forecast.Scenario is { } scenario) Add("Ai_ForecastScenario", scenario, forecast.Baseline.Events.Count);
        return string.Join(Environment.NewLine, preview);

        void Add(string heading, AiForecastPath path, int offset)
        {
            preview.Add(text[heading]);
            preview.Add(text.Format("Ai_ForecastBalances", Money(path.Opening), Money(path.Closing),
                Money(path.Minimum), Day(path.MinimumOn), N(path.UnknownCount)));
            preview.Add(path.FirstShortfallOn is { } first && path.FirstShortfallBalance is { } balance
                ? text.Format("Ai_ForecastFirstShortfall", Day(first), Money(balance)) : text["Ai_ForecastNoShortfall"]);
            for (var index = 0; index < path.Events.Count; index++)
            {
                var item = path.Events[index];
                var label = localLabels[offset + index];
                if (string.IsNullOrWhiteSpace(label)) label = text["Forecast_RecordedEntry"];
                var assumption = item.Excluded ? text["Forecast_LeftOut"]
                    : item.Moved && item.AssumedAmount ? text["Forecast_DateAndAmountAssumed"]
                    : item.Moved ? text["Forecast_DateAssumed"]
                    : item.AssumedAmount ? text["Forecast_AmountAssumed"]
                    : item.Source == Core.Forecasts.ForecastSource.OverduePlan ? text["Forecast_OverdueAssumed"] : null;
                var amount = item.Effect is { } value ? MoneyText.Format(value, forecast.Currency, local.CurrentCulture,
                    showPlus: true, approximate: item.Estimated) : text["Plan_AmountUnknown"];
                preview.Add($"[{index + 1}] {label} · {Day(item.On)} · {amount}" +
                    (assumption is null ? "" : " · " + assumption));
            }
        }
        string Day(DateOnly day) => dates.Format(day, DateFormatStyle.Long);
        string Money(long value) => MoneyText.Format(value, forecast.Currency, local.CurrentCulture);
    }

    private string FundingSummary(AiFundingFacts facts)
    {
        var summary = facts.Kind switch
        {
            AiFundingKind.BudgetLimit => text.Format("Ai_BudgetLimitFacts", Money(facts.Baseline), Money(facts.Observed),
                Money(facts.Difference), Money(facts.Carry!.Value), Money(facts.Existing!.Value)),
            AiFundingKind.BudgetSuggestion => text.Format("Ai_BudgetSuggestionFacts", Money(facts.Observed), Money(facts.Baseline),
                N(facts.PeriodCount!.Value), Optional(facts.Existing), Money(facts.Difference)),
            _ => text.Format("Ai_GoalFundingFacts", Money(facts.Baseline), Money(facts.Observed), Money(facts.Difference),
                Money(facts.Unfunded!.Value), N(facts.PeriodCount!.Value), Optional(facts.PlannedContribution)),
        };
        if (facts.Alternative() is { } alternative)
            summary += Environment.NewLine + text.Format(facts.Kind == AiFundingKind.Goal ? "Ai_FundingPerDate" : "Ai_FundingDraftValue", Money(alternative));
        return summary;
        string Money(long amount) => MoneyText.Format(amount, facts.Currency, local.CurrentCulture);
        string Optional(long? amount) => amount is { } value ? Money(value) : text["Report_NotAvailable"];
    }

    private async Task CheckOriginalAsync(AiReview original)
    {
        if (original.Selection.Question?.Intent == AiQuestionIntent.PlanShortfall &&
            (original.Selection.AsOf != DateOnly.FromDateTime(time.GetLocalNow().Date) ||
                original.Selection.QuestionCalendar != Presentation.Calendars.ToPeriod(local.CurrentCalendar)))
            throw new InvalidOperationException("The original question scope expired.");
        await store.CheckAsync(original);
        if (detailedPeriod?.ValidateReportAsync is { } validateReport) await validateReport();
    }

    private string ReportSummary(AiPeriodHandoff localReport, AiPayload payload)
    {
        var rows = payload.Report!;
        var amounts = rows.Categories is { } changes
            ? changes.Select(row => text.Format("Report_ChangeDetail", Formatted(row.Current, payload.Current.Totals[0].CurrencyCode),
                Formatted(row.Previous, payload.Current.Totals[0].CurrencyCode)))
            : rows.Plans!.Select(row => text.Format("Ai_PlanFact", Formatted(row.Planned, row.Currency), Formatted(row.Actual, row.Currency),
                row.Variance is { } variance ? Formatted(variance, row.Currency) : text["Report_NotAvailable"],
                N(row.Settled), N(row.Open), N(row.Unknown)));
        return text.Format("Ai_ReportReferences", N(AiProtocol.MaximumReportRows)) + Environment.NewLine
            + string.Join(Environment.NewLine, amounts.Select((amount, index) =>
                $"[{index + 1}] {localReport.ReportLabels[index]} · {amount}"));

        string Formatted(long amount, string currency) => MoneyText.Format(amount, currency, local.CurrentCulture);
    }

    private string Period(AiPeriod period) => text.Format("Ai_Summary", dates.Format(period.From, DateFormatStyle.Long), dates.Format(period.To, DateFormatStyle.Long), N(period.AccountCount), N(period.UnreviewedCount)) +
        Environment.NewLine + string.Join(Environment.NewLine, period.Totals.Select(t => text.Format("Ai_CurrencySummary", t.CurrencyCode,
            MoneyText.Format(t.NetIncome, t.CurrencyCode, local.CurrentCulture), MoneyText.Format(t.NetExpense, t.CurrencyCode, local.CurrentCulture), MoneyText.Format(checked(t.NetIncome - t.NetExpense), t.CurrencyCode, local.CurrentCulture))));
    private static string FailureKey(Exception error) => error is PersonalSyncHttpException { Status: HttpStatusCode.Unauthorized } || error.Message == "AiIdentityRequired" ? "Ai_IdentityRequired" :
        error is AiSourceReviewRequiredException ? "Ai_SourceReviewRequired" :
        error is PersonalSyncHttpException { Status: HttpStatusCode.NotFound or HttpStatusCode.ServiceUnavailable } ? "Ai_NoProvider" :
        error is PersonalSyncHttpException { Status: HttpStatusCode.TooManyRequests } ? "Ai_WaitStatus" : "Ai_Failed";
    private void SetError(string? key) { errorKey = key; Error = key is null ? null : text[key]; }
    // A changed visible disclosure needs fresh review; translated feedback must not trigger a source/provider read.
    private void OnLanguageChanged(object? sender, EventArgs args)
    {
        if (!OwnsPresentation) return;
        var frame = frameEpoch; var language = local.CurrentLanguage.CultureName; var culture = local.CurrentCulture.Name;
        var key = errorKey; var connection = remote; var status = availability; var saved = retained;
        var original = BeginInvalidation();
        bool Current() => OwnsPresentation && frame == frameEpoch && original == Volatile.Read(ref epoch)
            && language == local.CurrentLanguage.CultureName && culture == local.CurrentCulture.Name && key == errorKey
            && ReferenceEquals(connection, remote) && ReferenceEquals(status, availability) && ReferenceEquals(saved, retained);
        try
        {
            // D-965: even a retained destination getter can retire this owner before publication.
            var provider = status?.Configured == true ? connection?.Destination : text["Ai_NoProvider"];
            if (!Current()) return;
            SetError(key); if (!Current()) return;
            if (AccountsError is not null) { AccountsError = text["Ai_ChooseAccounts"]; if (!Current()) return; }
            if (FromError is not null) { FromError = text["Ai_InvalidRange"]; if (!Current()) return; }
            if (ToError is not null) { ToError = text["Ai_InvalidRange"]; if (!Current()) return; }
            ProviderText = provider; if (!Current()) return;
            if (status is not null) { ShowBalance(status.Balance); if (!Current()) return; }
            // Historical source JSON and untrusted result text are immutable, not translated or cleared/replayed.
            if (saved is not null) ResultStatus = RecordStatus(saved);
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            // A failed binding must not fault the global language event or resume this obsolete refresh.
            // Sharing authority was removed before any fallible publication.
        }
        finally
        {
            if (OwnsPresentation && frame == frameEpoch && original == Volatile.Read(ref epoch))
            {
                try { ClearInvalidatedPreview(original); }
                catch (Exception error) when (error is not OutOfMemoryException) { /* A failed preview notification cannot restore sharing authority. */ }
            }
        }
    }

    private string N(long value) => NativeDigits.Apply(value.ToString(local.CurrentCulture))!;
    // An accepted historical result is bound to its page/file frame, independently of a newer editable date draft.
    private void ValidateFrame(long original)
    {
        if (!OwnsPresentation || original != frameEpoch) throw new InvalidOperationException("Original analysis presentation retired.");
    }
    private void Validate(int version) { if (!OwnsPresentation || version != Volatile.Read(ref epoch)) throw new InvalidOperationException("Original analysis form retired."); }
    private void ValidateReview(int version, AiReview original)
    {
        Validate(version); RequireReview();
        original.EnsureCurrent();
        // D-975: cached-rights callbacks may retire this page or edit its reviewed draft before returning.
        Validate(version); RequireReview();
        void RequireReview()
        {
            if (!ReferenceEquals(review, original) || original.Selection.Language != local.CurrentLanguage.CultureName.Split('-')[0])
                throw new InvalidOperationException("Original analysis disclosure changed.");
        }
    }
    private void Refresh() { if (detailedPeriod?.Funding is not null) OnPropertyChanged(nameof(CanOpenFundingDraft)); OnPropertyChanged(nameof(HasPendingOperation)); OnPropertyChanged(nameof(CanLeave)); OnPropertyChanged(nameof(CanShare)); OnPropertyChanged(nameof(CanCheckProvider)); OnPropertyChanged(nameof(CanQuote)); OnPropertyChanged(nameof(CanSend)); OnPropertyChanged(nameof(CanContinue)); }
    private static DateOnly FirstDay(TimeProvider clock) { var date = DateOnly.FromDateTime(clock.GetLocalNow().Date); return new(date.Year, date.Month, 1); }
}
