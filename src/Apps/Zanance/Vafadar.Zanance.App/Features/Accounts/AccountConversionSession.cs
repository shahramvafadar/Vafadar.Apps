using CommunityToolkit.Mvvm.ComponentModel;
using Vafadar.Localization;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Features.Accounts;

/// <summary>The actual original account and new holding identity of one accepted conversion, independent of its view.</summary>
public sealed record AccountConversionOutcome(Guid AccountId, Guid TypeId);

/// <summary>Owns the original conversion preview/confirmation, one accepted writer and guarded native follow-through.</summary>
public sealed partial class AccountConversionSession(HoldingStore holdings, Translator translator,
    ILocalizationService localization, TimeProvider time, IAppInteraction interaction) : ObservableObject
{
    private long _revision;
    private Guid _account;
    private bool _active;
    private CancellationTokenSource _lifetime = new();
    private AccountConversionReview? _acceptedReview;
    private string? _errorKey;
    private Exception? _feedbackException;
    private bool _visibleAccepted;

    /// <summary>Gets whether the original preview, consent, writer or result navigation is outstanding.</summary>
    [ObservableProperty] public partial bool IsBusy { get; private set; }
    /// <summary>Gets current-owner feedback without turning an accepted conversion into a rejected financial command.</summary>
    [ObservableProperty] public partial string? Error { get; private set; }
    /// <summary>Gets the current owner's accepted result, retained when notification or navigation fails.</summary>
    [ObservableProperty] public partial string? Result { get; private set; }
    /// <summary>Gets the last actual accepted identities even if that page retired before publication.</summary>
    public AccountConversionOutcome? AcceptedOutcome { get; private set; }

    /// <summary>Retires old consent/read authority without discarding a known commit or another original form's draft.</summary>
    public void SetOwner(Guid accountId, bool active)
    {
        if (_account == accountId && _active == active) return;
        Interlocked.Increment(ref _revision);
        if (_account != accountId)
        { Error = Result = null; _errorKey = null; _feedbackException = null; _visibleAccepted = false; }
        _account = accountId; _active = active;
        var old = _lifetime; _lifetime = new(); old.Cancel(); old.Dispose();
    }

    private DateOnly Today => DateOnly.FromDateTime(time.GetLocalNow().DateTime);
    private string DisplayIdentity() => $"{localization.CurrentLanguage.CultureName}|{localization.CurrentCulture.Name}|{localization.CurrentCalendar}|{localization.CurrentDigits}|{localization.FormattingCultureName}";

    /// <summary>Retranslates only existing current-owner feedback; it never reads, confirms or repeats a conversion.</summary>
    public void RefreshFeedback()
    {
        if (!_active) return;
        if (_visibleAccepted) Result = translator["Account_Converted"];
        Error = _errorKey is { } key ? translator[key] : _feedbackException is { } exception
            ? CommercialFailureMessage.OrFallback(exception, translator, "Account_ConversionReviewAgain") : null;
    }

    private void ShowError(string key) { _errorKey = key; _feedbackException = null; Error = translator[key]; }
    private void ShowFailure(Exception exception)
    { _errorKey = null; _feedbackException = exception; Error = CommercialFailureMessage.OrFallback(exception, translator, "Account_ConversionReviewAgain"); }

    /// <summary>Uses the original recorded balance/name, explicit native consent and actual-file writer; never resubmits a receipt.</summary>
    public async Task<Guid?> ConvertAsync(Guid accountId, Func<bool> ownerCurrent)
    {
        ArgumentNullException.ThrowIfNull(ownerCurrent);
        if (IsBusy || !_active || _account != accountId || accountId == Guid.Empty || !ownerCurrent()) return null;
        var revision = Volatile.Read(ref _revision); var token = _lifetime.Token;
        var day = Today; var display = DisplayIdentity(); var culture = localization.CurrentCulture;
        bool OwnerCurrent() => !token.IsCancellationRequested && revision == Volatile.Read(ref _revision)
            && _active && _account == accountId && day == Today && display == DisplayIdentity();
        bool Current() => OwnerCurrent() && ownerCurrent();
        IsBusy = true;
        try
        {
            if (AcceptedOutcome is { } prior && prior.AccountId == accountId && _acceptedReview is { } accepted)
            {
                // A retained result may be opened elsewhere, but the accepted conversion itself is never repeated.
                await holdings.ValidateAccountConversionFileAsync(accepted, token);
                return Current() ? prior.TypeId : null;
            }
            Error = null; _errorKey = null; _feedbackException = null;
            var review = await holdings.ReadAccountConversionAsync(accountId, day, token);
            if (!Current()) return null;
            if (review is null) { ShowError("Account_ConversionReviewAgain"); return null; }
            var value = review.Balance > 0 ? MoneyText.Format(review.Balance, review.Inputs.Account.CurrencyCode, culture)
                : translator["Holding_ValueUnknown"];
            var location = translator["Holding_DefaultLocation"];
            await holdings.ValidateAccountConversionAsync(review, token);
            if (!Current()) return null;
            if (!await interaction.ConfirmAsync(translator["Account_ConvertToHolding"],
                    translator.Format("Account_ConvertPreview", review.Inputs.Account.Name, value),
                    translator["Account_ConvertConfirm"], translator["Common_Cancel"]) || !Current()) return null;

            AssetType type; var notificationFailed = false;
            try { type = await holdings.ConvertAccountAsync(review, location, Current, token); }
            catch (CommittedWriteNotificationException<AssetType> completed)
            { type = completed.Result; notificationFailed = true; }
            // Capture acceptance before any UI/property subscriber or native route can fail or retire this owner.
            _acceptedReview = review; AcceptedOutcome = new(accountId, type.Id);
            // After commit, an own archive/refresh may make the old eligibility predicate false.
            // Publish the receipt only to its still-active native owner, without requiring old financial eligibility.
            if (OwnerCurrent())
            {
                _visibleAccepted = true; Result = translator["Account_Converted"];
                if (notificationFailed) ShowError("Account_ConvertedFollowUpFailed");
                try { await interaction.NavigateAsync($"../{AppRoutes.HoldingDetailRoute}", new Dictionary<string, object> { ["id"] = type.Id }); }
                catch (Exception exception) when (exception is not OutOfMemoryException)
                { if (OwnerCurrent()) ShowError("Account_ConvertedFollowUpFailed"); }
            }
            return type.Id;
        }
        catch (OperationCanceledException) when (!Current()) { return null; }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            if (OwnerCurrent())
            {
                if (_acceptedReview is not null && AcceptedOutcome?.AccountId == accountId) ShowError("Account_ConvertedFollowUpFailed");
                else ShowFailure(exception);
            }
            return AcceptedOutcome?.AccountId == accountId ? AcceptedOutcome.TypeId : null;
        }
        finally { IsBusy = false; }
    }
}
