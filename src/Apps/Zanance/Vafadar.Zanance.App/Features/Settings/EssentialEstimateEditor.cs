using Vafadar.Localization;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Features.Settings;

/// <summary>Actual original estimate draft and atomic Save, independent of native controls and device configuration.</summary>
internal sealed class EssentialEstimateEditor(ZananceStore store, Translator translator, ILocalizationService localization)
{
    private readonly EstimateDraftState _publication = new();
    private readonly MoneyDraftInput _money = new();
    private EssentialEstimateReview? _review;
    private string? _profile;
    private long _revision;
    private bool _saving;
    private bool _invalid;
    /// <summary>Gets the exact original raw input, never an ambient-format conversion.</summary>
    internal string Text => _money.Text;
    /// <summary>Gets the original estimate's explicit period.</summary>
    internal int PeriodIndex { get; private set; }
    /// <summary>Gets its retained currency independently of a newly selected default.</summary>
    internal string CurrencyCode { get; private set; } = "EUR";
    /// <summary>Gets whether this exact unchanged draft has already been accepted.</summary>
    internal bool HasAccepted { get; private set; }
    /// <summary>Gets content-free saved/follow-up feedback, separate from field validation.</summary>
    internal string? Result { get; private set; }
    /// <summary>Checks the profile that actually published this original draft, before any new Save captures ownership.</summary>
    internal bool IsForProfile(string profile) => _review is not null && _profile == profile;
    /// <summary>Gets a field-local request for explicit new units or valid positive input.</summary>
    internal string? Error => _money.NeedsReview(CurrencyCode)
        ? translator.Format("Plan_InputUnitChanged", MoneyText.UnitName(CurrencyCode)) : _invalid ? translator["Amount_Invalid"] : null;
    /// <summary>Gets the original visible unit label of nonempty input.</summary>
    internal string UnitName => _money.UnitName(CurrencyCode);
    /// <summary>Explains the exact retained original unit factor.</summary>
    internal string? UnitNote => DisplayUnitNote.ForInput(translator, _money, CurrencyCode);
    /// <summary>Explains the retained original numeric format after a display-only change.</summary>
    internal string? FormatNote => _money.OriginalExample(localization.CurrentCulture) is { } example
        ? translator.Format("Plan_OriginalInputFormat", example) : null;

    /// <summary>Publishes authoritative saved input or retains the exact original same-profile unsaved draft.</summary>
    internal void Publish(string profile, EssentialEstimateReview review)
    {
        var settings = review.Settings; var currency = settings.EssentialEstimateCurrency ?? settings.DefaultCurrencyCode;
        var incoming = new EstimateInput(settings.EssentialEstimate is { } amount
            ? MoneyText.ForInput(amount, currency, localization.CurrentCulture) : "", (int)settings.EssentialEstimatePeriod, currency);
        var input = _publication.Publish(profile, review.SettingsId, incoming, new(Text, PeriodIndex, CurrencyCode),
            retainMeaning: _money.NeedsReview(currency));
        _review = review; _profile = profile; CurrencyCode = input.CurrencyCode; PeriodIndex = input.PeriodIndex;
        if (!_publication.Retained) { _money.Initialize(input.Text, input.CurrencyCode, localization.CurrentCulture); _invalid = false; }
        _revision++; HasAccepted = false; Result = null;
    }
    /// <summary>Tracks actual typing; partial edits cannot renew a stale unit/currency.</summary>
    internal void ChangeText(string value)
    {
        if (Text == value) return;
        _money.ChangeText(value, CurrencyCode, localization.CurrentCulture); Edited();
    }
    /// <summary>Tracks explicit period changes, including change-and-return while a writer waits.</summary>
    internal void ChangePeriod(int value)
    {
        if (PeriodIndex == value) return; PeriodIndex = value; Edited();
    }
    /// <summary>Adopts only the user's explicit suggestion from authoritative minor units in the current unit.</summary>
    internal void UseSuggestion(long amount)
    {
        _money.Format(amount, CurrencyCode, localization.CurrentCulture); PeriodIndex = (int)EstimatePeriod.Day; Edited();
    }
    private void Edited() { _revision++; _invalid = false; HasAccepted = false; Result = null; }

    /// <summary>Captures submitted input before awaits, then rechecks its original native owner inside the writer.</summary>
    internal async Task SaveAsync(Func<bool> ownsOriginal, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(ownsOriginal);
        if (_saving || HasAccepted || _review is not { } original || _profile is not { } profile || !ownsOriginal()) return;
        if (_money.NeedsReview(CurrencyCode)) return;
        long? amount = null;
        if (!string.IsNullOrWhiteSpace(Text))
        {
            if (!_money.TryParse(out var parsed) || parsed <= 0) { _invalid = true; return; }
            amount = parsed;
        }
        var submitted = new EstimateInput(Text, PeriodIndex, CurrencyCode); var revision = _revision;
        _saving = true; Result = null;
        try
        {
            var notificationFailed = false; EssentialEstimateReview next;
            try
            {
                next = await store.SaveEssentialEstimateAsync(original, amount, (EstimatePeriod)Math.Clamp(submitted.PeriodIndex, 0, 2),
                    submitted.CurrencyCode, () => revision == _revision && ReferenceEquals(original, _review)
                        && ownsOriginal() && !_money.NeedsReview(submitted.CurrencyCode), ct);
            }
            catch (CommittedWriteNotificationException<EssentialEstimateReview> accepted)
            { next = accepted.Result; notificationFailed = true; }
            if (ReferenceEquals(_review, original) && _profile == profile)
            {
                _review = next; _publication.AcceptSave(profile, original.SettingsId, submitted);
                if (revision == _revision && ownsOriginal())
                {
                    HasAccepted = true;
                    Result = translator[notificationFailed ? "Settings_EssentialSavedFollowUpFailed"
                        : amount is null ? "Settings_EssentialCleared" : "Settings_EssentialSaved"];
                }
            }
        }
        finally { _saving = false; }
    }
}
