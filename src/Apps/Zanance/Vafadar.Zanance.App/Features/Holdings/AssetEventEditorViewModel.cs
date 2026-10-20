using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Localization;
using Vafadar.Maui.Mvvm;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;
using Vafadar.Zanance.App.Interaction;

namespace Vafadar.Zanance.App.Features.Holdings;

/// <summary>An account that pays for a purchase or receives a sale.</summary>
public sealed record HoldingAccount(Guid Id, string Name, string CurrencyCode)
{
    public override string ToString() => $"{Name} ({CurrencyCode})";
}

/// <summary>A place where a holding is kept.</summary>
public sealed record LocationChoice(Guid Id, string Name)
{
    public override string ToString() => Name;
}

/// <summary>
/// Records or edits one change of a holding (ZEX-S0403 to S0406, design §7.4): a purchase or sale with its money entry
/// and an optional fee (an expense in Fees), a move between locations, an opening holding or a gift (no money moves), a
/// removal and a correction with a reason. A price can be typed per gram or unit or as a total; when the account's
/// currency differs from the price currency, the value in the price currency is asked for separately (cost basis).
/// Nothing is saved when the history would become negative on any date: "On 3 Oct only 20.000 g were held at Home safe."
/// </summary>
public sealed partial class AssetEventEditorViewModel : ViewModelBase, Presentation.IUnsavedChanges
{
    private static readonly AssetEventKind[] AdvancedKinds =
    [
        AssetEventKind.Purchase, AssetEventKind.Sale, AssetEventKind.LocationTransfer, AssetEventKind.Opening,
        AssetEventKind.GiftReceived, AssetEventKind.Outflow, AssetEventKind.Correction,
    ];

    private readonly HoldingStore _holdings;
    private readonly ZananceStore _store;
    private readonly HoldingText _text;
    private readonly Translator _translator;
    private readonly ILocalizationService _localization;
    private readonly TimeProvider _time;
    private readonly IAppInteraction _interaction;
    private readonly HoldingEventEditorSession _session;
    private bool _loading = true;
    private AssetType? _type;
    private AssetEvent? _existing;
    private List<LedgerEntry> _groupEntries = [];
    private List<AssetEvent> _otherEvents = [];
    private AssetEventKind[] _kinds = AdvancedKinds;
    private IReadOnlyList<QuantityUnit> _units = [QuantityUnit.Gram];

    public AssetEventEditorViewModel(HoldingStore holdings, ZananceStore store, HoldingText text, Translator translator, ILocalizationService localization, TimeProvider time, IAppInteraction interaction)
    {
        _holdings = holdings;
        _store = store;
        _text = text;
        _translator = translator;
        _localization = localization;
        _time = time;
        _interaction = interaction;
        _session = new(holdings);
        Title = translator["AssetEvent_NewTitle"];
        Date = DateOnly.FromDateTime(time.GetLocalNow().DateTime);
        DirectionNames = [translator["AssetEvent_Increase"], translator["AssetEvent_Decrease"]];
    }

    [ObservableProperty]
    public partial string Title { get; set; }

    [ObservableProperty]
    public partial string? TypeName { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<string> KindNames { get; set; } = [];

    [ObservableProperty]
    public partial int KindIndex { get; set; }

    /// <summary>Gets a value indicating whether the kind can be chosen (new events in Advanced).</summary>
    [ObservableProperty]
    public partial bool CanChooseKind { get; set; }

    [ObservableProperty]
    public partial string? KindHint { get; set; }

    [ObservableProperty]
    public partial DateOnly Date { get; set; }

    [ObservableProperty]
    public partial string QuantityText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial IReadOnlyList<string> UnitNames { get; set; } = [];

    [ObservableProperty]
    public partial int UnitIndex { get; set; }

    [ObservableProperty]
    public partial bool HasUnitChoice { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<LocationChoice> Locations { get; set; } = [];

    [ObservableProperty]
    public partial LocationChoice? Location { get; set; }

    [ObservableProperty]
    public partial LocationChoice? ToLocation { get; set; }

    [ObservableProperty]
    public partial string? LocationLabel { get; set; }

    [ObservableProperty]
    public partial bool IsTransfer { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<HoldingAccount> Accounts { get; set; } = [];

    [ObservableProperty]
    public partial HoldingAccount? Account { get; set; }

    /// <summary>Gets a value indicating whether money moves (purchase or sale).</summary>
    [ObservableProperty]
    public partial bool HasMoney { get; set; }

    [ObservableProperty]
    public partial string? AmountLabel { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<string> PriceModeNames { get; set; } = [];

    /// <summary>Gets or sets 0 = the total, 1 = a price per gram or unit.</summary>
    [ObservableProperty]
    public partial int PriceModeIndex { get; set; }

    [ObservableProperty]
    public partial string AmountText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string FeeText { get; set; } = string.Empty;

    /// <summary>Gets a value indicating whether the account's currency differs from the price currency.</summary>
    [ObservableProperty]
    public partial bool HasSecondAmount { get; set; }

    [ObservableProperty]
    public partial string? SecondAmountLabel { get; set; }

    [ObservableProperty]
    public partial string SecondAmountText { get; set; } = string.Empty;

    /// <summary>Gets a value indicating whether a value at the time can be given (opening holding or gift).</summary>
    [ObservableProperty]
    public partial bool HasBasis { get; set; }

    [ObservableProperty]
    public partial string? BasisLabel { get; set; }

    [ObservableProperty]
    public partial string BasisText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsCorrection { get; set; }

    public IReadOnlyList<string> DirectionNames { get; }

    [ObservableProperty]
    public partial int DirectionIndex { get; set; }

    [ObservableProperty]
    public partial bool HasReason { get; set; }

    [ObservableProperty]
    public partial string? ReasonLabel { get; set; }

    [ObservableProperty]
    public partial string ReasonText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Note { get; set; } = string.Empty;

    /// <summary>Gets what saving will change, e.g. "After saving: 30.000 g at Home safe · Cash −1,020.00 EUR".</summary>
    [ObservableProperty]
    public partial string? EffectText { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(QuantityError), nameof(FormError))]
    public partial string? Error { get; set; }

    /// <summary>Gets the bounded quantity feedback beside its original input without duplicating the form error.</summary>
    public string? QuantityError => Error == _translator["AssetEvent_QuantityTooLarge"] ? Error : null;

    /// <summary>Gets other original validation feedback on its existing form surface.</summary>
    public string? FormError => QuantityError is null ? Error : null;

    /// <summary>Gets source/refund currency feedback beside the original account choice.</summary>
    [ObservableProperty]
    public partial string? AccountError { get; set; }

    /// <summary>Gets the retained-refund amount feedback beside the original fee input.</summary>
    [ObservableProperty]
    public partial string? FeeError { get; set; }

    private AssetEventKind Kind => _kinds[Math.Clamp(KindIndex, 0, _kinds.Length - 1)];

    private QuantityUnit Unit => _units[Math.Clamp(UnitIndex, 0, _units.Count - 1)];

    /// <summary>Publishes a coherent original file/type/event and choices before enabling the editor.</summary>
    public async Task ApplyQueryAsync(IDictionary<string, object> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        // Native navigation may redeliver an empty query after the original parameters were consumed.
        // Keep that original loaded review instead of starting a second load with an empty type.
        if (!(query.TryGetValue("id", out var originalId) && originalId is Guid)
            && !(query.TryGetValue("type", out var originalType) && originalType is Guid)) return;
        var culture = _localization.CurrentCulture;
        _loading = true; PublishState();
        var eventId = query.TryGetValue("id", out var idValue) && idValue is Guid id ? id : (Guid?)null;
        var typeId = query.TryGetValue("type", out var typeValue) && typeValue is Guid t ? t : Guid.Empty;
        var requested = query.TryGetValue("kind", out var kindValue) && Enum.TryParse<AssetEventKind>(kindValue as string, out var k) ? k : (AssetEventKind?)null;
        query.Clear();
        // Preserve initial settings/default-location initialization, then capture all displayed sources together.
        await Task.Run(async () =>
        {
            await _store.GetSettingsAsync();
            if (!(await _holdings.GetLocationsAsync()).Any(l => !l.IsArchived))
                await _holdings.EnsureDefaultLocationAsync(_translator["Holding_DefaultLocation"]);
        });
        var review = await Task.Run(() => _session.LoadAsync(typeId, eventId));
        if (review is null) return;
        var advanced = review.Inputs.Settings.Advanced;
        var events = review.Inputs.Events;
        _existing = eventId is { } editedId ? HoldingEventReadReview.CopyEvent(events.Single(e => e.Id == editedId)) : null;
        _type = review.Inputs.Type;
        _groupEntries = review.Inputs.Entries.Select(e => e.Copy()).ToList();
        if (_type is not { } type)
        {
            Error = _translator["Holding_NotFound"];
            return;
        }

        TypeName = type.Name;
        _otherEvents = [.. events.Where(e => e.AssetTypeId == type.Id && e.Id != _existing?.Id)];
        _units = Quantities.UnitsOf(type.Dimension);
        UnitNames = [.. _units.Select(u => u == QuantityUnit.Piece ? _text.CountName(type) : _translator[u == QuantityUnit.Kilogram ? "Unit_Kilogram" : "Unit_Gram"])];
        HasUnitChoice = _units.Count > 1;
        PriceModeNames = [_translator["AssetEvent_PriceTotal"], _text.PerUnit(type)];

        PublishLocations(review.Inputs.Locations, null);
        var accounts = review.Inputs.Accounts.Where(a => !a.IsArchived);
        var defaultAccount = review.Inputs.Settings.DefaultAccountId;
        Accounts = [.. accounts.Where(a => !a.Type.IsOutsideCash()).OrderBy(a => a.SortOrder).ThenBy(a => a.Name).Select(a => new HoldingAccount(a.Id, a.Name, a.CurrencyCode))];
        // The default account first when it pays in the price currency, then any account in that currency (ZEX-S0202).
        Account = Accounts.FirstOrDefault(a => a.Id == defaultAccount && string.Equals(a.CurrencyCode, type.PriceCurrencyCode, StringComparison.OrdinalIgnoreCase))
            ?? Accounts.FirstOrDefault(a => string.Equals(a.CurrencyCode, type.PriceCurrencyCode, StringComparison.OrdinalIgnoreCase))
            ?? Accounts.FirstOrDefault();

        // Simple records only corrections; an existing event keeps its kind (a purchase stays a purchase).
        _kinds = _existing is { } edited ? [edited.Kind] : advanced ? AdvancedKinds : [AssetEventKind.Correction];
        KindNames = [.. _kinds.Select(_text.EventKind)];
        CanChooseKind = _kinds.Length > 1;
        KindIndex = Math.Max(0, Array.IndexOf(_kinds, _existing?.Kind ?? requested ?? _kinds[0]));
        OnKindIndexChanged(KindIndex);

        if (_existing is { } existing)
        {
            Title = _translator["AssetEvent_EditTitle"];
            Date = existing.Date;
            UnitIndex = 0;
            QuantityText = Quantities.ForInput(existing.Quantity, Unit, culture);
            Location = Locations.FirstOrDefault(l => l.Id == existing.LocationId) ?? Location;
            ToLocation = Locations.FirstOrDefault(l => l.Id == existing.ToLocationId);
            DirectionIndex = existing.IsIncrease ? 0 : 1;
            ReasonText = existing.Reason ?? string.Empty;
            Note = existing.Note ?? string.Empty;
            BasisText = existing.BasisAmount is { } basis && HasBasis ? MoneyText.ForInput(basis, type.PriceCurrencyCode, culture) : string.Empty;

            if (_groupEntries.FirstOrDefault(e => e.Kind is EntryKind.AssetPurchase or EntryKind.AssetSale) is { } money)
            {
                Account = Accounts.FirstOrDefault(a => a.Id == money.AccountId) ?? Account;
                AmountText = MoneyText.ForInput(money.Amount, Account?.CurrencyCode ?? type.PriceCurrencyCode, culture);
            }

            if (_groupEntries.FirstOrDefault(e => e.Kind == EntryKind.Expense) is { } fee)
            {
                FeeText = MoneyText.ForInput(fee.Amount, Account?.CurrencyCode ?? type.PriceCurrencyCode, culture);
            }

            var priced = existing.Kind == AssetEventKind.Sale ? existing.ProceedsAmount : existing.BasisAmount;
            SecondAmountText = HasSecondAmount && priced is { } second ? MoneyText.ForInput(second, type.PriceCurrencyCode, culture) : string.Empty;
        }

        UpdateEffect();
        _snapshot = Snapshot(); _loading = false; PublishState();
    }

    private void PublishLocations(IReadOnlyList<AssetLocation> sources, Guid? select)
    {
        var locations = sources.Where(l => !l.IsArchived).OrderBy(l => l.SortOrder).ThenBy(l => l.Name).ToList();
        Locations = [.. locations.Select(l => new LocationChoice(l.Id, l.Name))];
        Location = Locations.FirstOrDefault(l => l.Id == (select ?? Location?.Id)) ?? Locations[0];
        ToLocation ??= Locations.FirstOrDefault(l => l.Id != Location.Id);
    }

    partial void OnKindIndexChanged(int value)
    {
        var kind = Kind;
        HasMoney = kind is AssetEventKind.Purchase or AssetEventKind.Sale;
        IsTransfer = kind == AssetEventKind.LocationTransfer;
        HasBasis = kind is AssetEventKind.Opening or AssetEventKind.GiftReceived;
        IsCorrection = kind == AssetEventKind.Correction;
        HasReason = kind is AssetEventKind.Correction or AssetEventKind.Outflow;
        ReasonLabel = _translator[IsCorrection ? "AssetEvent_ReasonRequired" : "AssetEvent_Reason"];
        LocationLabel = _translator[IsTransfer ? "AssetEvent_From" : "AssetEvent_Location"];
        AmountLabel = _translator[kind == AssetEventKind.Sale ? "AssetEvent_Received" : "AssetEvent_Paid"];
        BasisLabel = _type is null ? null : _translator.Format("AssetEvent_BasisLabel", _type.PriceCurrencyCode);
        KindHint = _translator[$"AssetEvent_{kind}Hint"];
        UpdateSecondAmount();
        UpdateEffect();
    }

    partial void OnAccountChanged(HoldingAccount? value)
    {
        AccountError = null;
        UpdateSecondAmount();
        UpdateEffect();
    }

    partial void OnQuantityTextChanged(string value) => UpdateEffect();

    partial void OnUnitIndexChanged(int value) => UpdateEffect();

    partial void OnAmountTextChanged(string value) => UpdateEffect();

    partial void OnFeeTextChanged(string value) { FeeError = null; UpdateEffect(); }

    partial void OnPriceModeIndexChanged(int value) => UpdateEffect();

    partial void OnLocationChanged(LocationChoice? value) => UpdateEffect();

    partial void OnToLocationChanged(LocationChoice? value) => UpdateEffect();

    partial void OnDirectionIndexChanged(int value) => UpdateEffect();

    private void UpdateSecondAmount()
    {
        HasSecondAmount = HasMoney && _type is not null && Account is not null && !string.Equals(Account.CurrencyCode, _type.PriceCurrencyCode, StringComparison.OrdinalIgnoreCase);
        SecondAmountLabel = _type is null ? null : _translator.Format("AssetEvent_SecondAmount", _type.PriceCurrencyCode);
    }

    // The quantity in base units, or null when the text is not a valid quantity of this type.
    private long? ParsedQuantity(out bool outsideRange)
    {
        outsideRange = false;
        return _type is not null && Quantities.TryParse(QuantityText, Unit, _type, _localization.CurrentCulture, out var quantity, out outsideRange) ? quantity : null;
    }

    // The money that moves in the account's currency: the total or price per gram/unit × quantity.
    private long? ParsedAmount(long quantity)
    {
        if (Account is null || !MoneyText.TryParse(AmountText, Account.CurrencyCode, _localization.CurrentCulture, out var amount) || amount <= 0)
        {
            return null;
        }

        if (PriceModeIndex != 1)
        {
            return amount;
        }

        // A price per gram or unit times a very large quantity can leave the range of an amount: that is invalid input,
        // shown as such, never a crash while typing.
        try
        {
            var total = Math.Round((decimal)amount * quantity / Quantities.PerGramOrUnit, MidpointRounding.AwayFromZero);
            return total is > 0 and <= long.MaxValue ? (long)total : null;
        }
        catch (OverflowException)
        {
            return null;
        }
    }

    // D-410: only a genuinely blank/zero fee requests removal; malformed or negative input is not zero.
    private long? ParsedFee() => Account is null ? null : string.IsNullOrWhiteSpace(FeeText) ? 0
        : MoneyText.TryParse(FeeText, Account.CurrencyCode, _localization.CurrentCulture, out var fee) && fee >= 0 ? fee : null;

    private AssetEvent BuildEvent(AssetType type, long quantity)
    {
        var assetEvent = _existing is { } original ? HoldingEventReadReview.CopyEvent(original) : new AssetEvent { AssetTypeId = type.Id };
        assetEvent.Kind = Kind;
        assetEvent.Date = Date;
        assetEvent.Quantity = quantity;
        assetEvent.LocationId = Location?.Id ?? Guid.Empty;
        assetEvent.ToLocationId = IsTransfer ? ToLocation?.Id : null;
        assetEvent.IsIncrease = IsCorrection && DirectionIndex == 0;
        assetEvent.Reason = HasReason && !string.IsNullOrWhiteSpace(ReasonText) ? ReasonText.Trim() : null;
        assetEvent.Note = string.IsNullOrWhiteSpace(Note) ? null : Note.Trim();
        return assetEvent;
    }

    // Shows the holding at the chosen place and the money change before saving (design §7.4).
    private void UpdateEffect()
    {
        if (_type is not { } type || Location is null || ParsedQuantity(out _) is not { } quantity)
        {
            EffectText = null;
            return;
        }

        var probe = new AssetEvent
        {
            AssetTypeId = type.Id, Kind = Kind, Date = Date, Quantity = quantity, LocationId = Location.Id,
            ToLocationId = IsTransfer ? ToLocation?.Id : null, IsIncrease = IsCorrection && DirectionIndex == 0,
        };
        var events = _otherEvents.Append(probe).ToList();
        string At(LocationChoice place) => _translator.Format("AssetEvent_At", _text.Quantity(Math.Max(0, events.Sum(e => e.EffectAt(place.Id))), type), place.Name);
        List<string> parts;
        try
        {
            // D-412: preserve checked quantity rules; an unrepresentable draft has no guessed or wrapped preview.
            _ = HoldingsLedger.Quantity(events, type.Id, DateOnly.MaxValue);
            parts = [At(Location)];
            if (IsTransfer && ToLocation is { } to && to.Id != Location.Id) parts.Add(At(to));
        }
        catch (OverflowException) { EffectText = null; return; }

        if (HasMoney && Account is { } account && ParsedAmount(quantity) is { } amount && ParsedFee() is { } fee)
        {
            // A preview must not wrap at the Int64 boundary or invent a zero fee while input is invalid.
            var change = Kind == AssetEventKind.Purchase ? -((decimal)amount + fee) : (decimal)amount - fee;
            if (change is >= long.MinValue and <= long.MaxValue)
                parts.Add($"{account.Name} {_text.Money((long)change, account.CurrencyCode, showPlus: true)}");
        }

        EffectText = _translator.Format("AssetEvent_After", string.Join(" · ", parts));
    }

    [RelayCommand(CanExecute = nameof(CanEdit))]
    private async Task AddLocationAsync()
    {
        if (!CanEdit || _session.Review is null) return;
        var name = await _interaction.PromptAsync(_translator["AssetEvent_AddLocation"], _translator["AssetEvent_AddLocationMessage"],
            _translator["Common_Save"], _translator["Common_Cancel"], maxLength: 60);
        if (string.IsNullOrWhiteSpace(name) || !CanEdit || _session.Review is null) return;
        IsBusy = true; PublishState();
        try
        {
            var location = new AssetLocation { Name = name.Trim() };
            await _holdings.SaveLocationAsync(location);
            var review = await _session.AddLocationAsync(location.Id);
            if (review is null) return;
            PublishLocations(review.Inputs.Locations, IsTransfer ? Location?.Id : location.Id);
            if (IsTransfer) ToLocation = Locations.FirstOrDefault(l => l.Id == location.Id);
        }
        catch (HoldingEventReviewChangedException) { RetireReview(); Error = _translator["AssetEvent_SourceChanged"]; }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        { RetireReview(); Error = Presentation.CommercialFailureMessage.OrFallback(exception, _translator, "Common_SaveFailed", draftKept: true); }
        finally { IsBusy = false; PublishState(); }
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        if (_type is not { } type || !CanSave)
        {
            return;
        }

        Error = null; AccountError = null; FeeError = null;
        var culture = _localization.CurrentCulture;
        if (ParsedQuantity(out var outsideRange) is not { } quantity)
        {
            Error = _translator[outsideRange ? "AssetEvent_QuantityTooLarge" : type.Dimension == AssetDimension.Count && !type.Divisible ? "AssetEvent_WholeUnits" : "AssetEvent_QuantityInvalid"];
            return;
        }

        if (Location is null || (IsTransfer && (ToLocation is null || ToLocation.Id == Location.Id)))
        {
            Error = _translator["AssetEvent_LocationsDiffer"];
            return;
        }

        if (IsCorrection && string.IsNullOrWhiteSpace(ReasonText))
        {
            Error = _translator["AssetEvent_ReasonMissing"];
            return;
        }

        var originalDraft = Snapshot();
        var assetEvent = BuildEvent(type, quantity);
        try
        {
            // This is feedback over the original review, not write authority: the atomic writer rechecks it.
            // A refused range input must keep that original review available for explicit same-form correction.
            var proposed = _otherEvents.Append(assetEvent).ToList();
            _ = HoldingsLedger.Quantity(proposed, type.Id, DateOnly.MaxValue);
            _ = HoldingsLedger.FindConflict(proposed);
        }
        catch (OverflowException) { Error = _translator["AssetEvent_QuantityTooLarge"]; return; }
        var entries = new List<LedgerEntry>();
        long? priced = null;
        if (HasMoney)
        {
            if (Account is not { } account || ParsedAmount(quantity) is not { } amount)
            {
                Error = _translator[Account is null ? "AssetEvent_AccountMissing" : "Amount_Invalid"];
                return;
            }

            if (ParsedFee() is not { } fee)
            {
                FeeError = _translator["Amount_Invalid"];
                return;
            }

            priced = amount;
            if (HasSecondAmount)
            {
                // The value in the price currency is the cost basis or the proceeds; without it they stay unknown.
                priced = MoneyText.TryParse(SecondAmountText, type.PriceCurrencyCode, culture, out var second) && second > 0 ? second : null;
            }

            var money = _groupEntries.FirstOrDefault(e => e.Kind is EntryKind.AssetPurchase or EntryKind.AssetSale)?.Copy() ?? new LedgerEntry();
            money.Kind = Kind == AssetEventKind.Purchase ? EntryKind.AssetPurchase : EntryKind.AssetSale;
            money.AccountId = account.Id;
            money.Amount = amount;
            money.Date = Date;
            money.Title = type.Name;
            money.Note = assetEvent.Note;
            entries.Add(money);

            // A fee is spending (Fees), never part of the price (ZEX-P09).
            if (fee > 0)
            {
                var feeEntry = _groupEntries.FirstOrDefault(e => e.Kind == EntryKind.Expense)?.Copy() ?? new LedgerEntry();
                feeEntry.Kind = EntryKind.Expense;
                feeEntry.AccountId = account.Id;
                feeEntry.Amount = fee;
                feeEntry.Date = Date;
                feeEntry.CategoryId = _session.Review!.Inputs.Categories.FirstOrDefault(c => c.SystemKey == Vafadar.Zanance.Core.Categories.DefaultCategories.Fees
                    && c.Kind == Vafadar.Zanance.Core.Categories.CategoryKind.Expense)?.Id;
                feeEntry.Title = _translator.Format("AssetEvent_FeeTitle", type.Name);
                entries.Add(feeEntry);
            }
        }
        else if (HasBasis && !string.IsNullOrWhiteSpace(BasisText))
        {
            if (!MoneyText.TryParse(BasisText, type.PriceCurrencyCode, culture, out var basis) || basis <= 0)
            {
                Error = _translator["Amount_Invalid"];
                return;
            }

            priced = basis;
        }

        assetEvent.BasisAmount = Kind is AssetEventKind.Purchase or AssetEventKind.Opening or AssetEventKind.GiftReceived ? priced : null;
        assetEvent.ProceedsAmount = Kind == AssetEventKind.Sale ? priced : null;

        IsBusy = true; PublishState();
        try
        {
            // D-408: the exact raw draft and original source stay bound to the existing atomic quantity/payment/fee/price writer.
            var result = await _session.SaveAsync(assetEvent, entries, () => Snapshot() == originalDraft);
            if (result is null) return;
            if (result.Conflict is { } conflict)
            {
                var place = Locations.FirstOrDefault(l => l.Id == conflict.LocationId)?.Name ?? "?";
                Error = _translator.Format("Holding_Conflict", _text.Date(conflict.Date), _text.Quantity(conflict.Available, type), place);
                return;
            }

            if (!result.Succeeded)
            {
                // D-409: retained-refund corrections point to the actual account/fee fields without losing raw input.
                if (result.Errors.Contains(LedgerError.RefundCurrencyMismatch)) AccountError = _translator["LedgerError_RefundCurrencyMismatch"];
                if (result.Errors.Contains(LedgerError.RefundExceedsPurchase)) FeeError = _translator["LedgerError_RefundExceedsPurchase"];
                var otherErrors = result.Errors.Where(e => e is not (LedgerError.RefundCurrencyMismatch or LedgerError.RefundExceedsPurchase)).ToList();
                Error = otherErrors.Count == 0 ? null : string.Join(Environment.NewLine, otherErrors.Select(e => _translator[$"LedgerError_{e}"]));
                return;
            }

            _snapshot = Snapshot(); PublishState();
        }
        catch (CommittedWriteNotificationException<HoldingSaveResult>)
        { _snapshot = Snapshot(); Error = _translator["AssetEvent_SavedNeedsReview"]; return; }
        catch (HoldingEventReviewChangedException)
        { RetireReview(); Error = _translator["AssetEvent_SourceChanged"]; return; }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        { RetireReview(); Error = Presentation.CommercialFailureMessage.OrFallback(exception, _translator, "Common_SaveFailed", draftKept: true); return; }
        finally { IsBusy = false; PublishState(); }
        try { await _interaction.NavigateAsync(".."); }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        { Error = _translator["Editor_SavedReturnFailed"]; await _interaction.ShowFailureAsync(exception); }
    }

    /// <summary>Gets whether raw holding inputs are still editable.</summary>
    public bool CanEdit => !_loading && !IsBusy && !_session.Committed;
    /// <summary>Gets whether the original coherent financial review still authorizes this form's Save.</summary>
    public bool CanSave => CanEdit && _session.Review is not null;
    /// <summary>Gets Cancel before acceptance and Close afterwards.</summary>
    public string CloseText => _translator[_session.Committed ? "Common_Close" : "Common_Cancel"];
    private void PublishState()
    {
        OnPropertyChanged(nameof(CanEdit)); OnPropertyChanged(nameof(CanSave)); OnPropertyChanged(nameof(CloseText));
        SaveCommand.NotifyCanExecuteChanged(); AddLocationCommand.NotifyCanExecuteChanged();
    }
    /// <summary>Retires a departed source without changing its raw draft or accepted result.</summary>
    public void RetireReview() { _session.Retire(); PublishState(); }

    [RelayCommand]
    private async Task CancelAsync()
    {
        if (!_session.Committed && IsDirty && !await _interaction.ConfirmAsync(_translator["Common_DiscardTitle"],
            _translator["Common_DiscardMessage"], _translator["Common_Discard"], _translator["Common_KeepEditing"])) return;
        await _interaction.NavigateAsync("..");
    }

    // The input as it was loaded or saved; leaving with a change asks first (CR12).
    private string? _snapshot;

    /// <inheritdoc />
    public bool IsDirty => !_session.Committed && _snapshot is not null && Snapshot() != _snapshot;

    private string Snapshot() => Presentation.DraftFingerprint.Create(
        KindIndex, Date, QuantityText, UnitIndex, Location?.Id, ToLocation?.Id, Account?.Id, PriceModeIndex,
        AmountText, FeeText, SecondAmountText, BasisText, DirectionIndex, ReasonText, Note);
}
