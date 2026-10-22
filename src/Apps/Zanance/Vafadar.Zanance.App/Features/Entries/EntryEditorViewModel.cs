using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentIcons.Common;
using Vafadar.Localization;
using Vafadar.Maui.Mvvm;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Core.Receipts;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Features.Entries;

/// <summary>A quick template above a new entry, with the icon and colour of its category (TX-04).</summary>
public sealed record TemplateChip(EntryTemplate Template, Symbol Icon, Color IconColor)
{
    /// <summary>Gets separate reviewed activity availability; retained identities remain visible.</summary>
    public bool CanApply { get; init; } = true;
}

/// <summary>A bounded receipt choice; choosing it fills the form, never saves the entry.</summary>
public sealed record ReceiptChoice(ReceiptAmountCandidate Candidate, string Label);

/// <summary>A category tile in the entry editor.</summary>
public sealed partial class CategoryChoice(Guid id, string name, Symbol icon, Color color) : ObservableObject
{
    public Guid Id { get; } = id;

    public string Name { get; } = name;

    public Symbol Icon { get; } = icon;

    public Color Color { get; } = color;

    public Color Background { get; } = color.WithAlpha(0.12f);

    /// <summary>
    /// Gets the chip outline: the category colour when selected. A property instead of a trigger setter with a binding,
    /// which stays applied after the trigger turns off, so a category chosen before kept looking selected.
    /// </summary>
    public Color Outline => IsSelected ? Color : Presentation.Palette.DividerColor;

    /// <summary>Gets the chip background: the light category colour when selected.</summary>
    public Color Fill => IsSelected ? Background : Presentation.Palette.CardBackground;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Outline), nameof(Fill))]
    public partial bool IsSelected { get; set; }
}

/// <summary>
/// Creates and edits entries (UI-03): expense, income, transfer (with fee and destination amount), refund, foreign
/// amount. The entry object keeps its id across saves, so repeated taps on Save never create duplicates (AT-03).
/// </summary>
public sealed partial class EntryEditorViewModel : ViewModelBase, IQueryAttributable, Presentation.IThemeAware
{
    private static readonly EntryKind[] ChipKinds = [EntryKind.Expense, EntryKind.Income, EntryKind.Transfer];

    private readonly ZananceStore _store;
    private readonly Commerce.QuickResourceActions _quick;
    /// <summary>Gets original template application state and visible feedback without saving the entry draft.</summary>
    public Commerce.QuickResourceActions QuickActions => _quick;
    private readonly Translator _translator;
    private readonly ILocalizationService _localization;
    private readonly TimeProvider _time;
    private LedgerEntry _entry = new();
    private LedgerEntry? _fee;
    private LedgerEntry? _destinationFee;
    // What the feature policy shows for new entries: creating an aggregated entry and a fee in the destination currency.
    private bool _canAggregate;
    private bool _showsFee;
    private LedgerEntry? _refundOf;
    private CategoryLookup _categories;
    private Dictionary<Guid, Account> _accounts = [];
    private string _snapshot = string.Empty;
    private bool _loading;
    private bool _isNew;
    private int _startDay = 1;
    private Presentation.PendingAttachment? _pendingAttachment;
    private EntryEditorSaveSession? _saveSession;
    private long _saveRevision;
    private ReceiptSuggestion? _receipt;
    private string? _receiptAppliedText;
    private string? _receiptAppliedCurrency;
    private long _receiptDisplayFactor = 1;
    private readonly Presentation.UndoService _undo;
    private readonly AssetEntryConfirmation _assetConfirmation;
    private readonly Data.Commerce.ReceiptWorkStore _receiptReader;

    public EntryEditorViewModel(ZananceStore store, Translator translator, ILocalizationService localization, TimeProvider time, Presentation.UndoService undo, AssetEntryConfirmation assetConfirmation, Interaction.IAppInteraction interaction, Data.Commerce.ReceiptWorkStore receiptReader)
    {
        _assetConfirmation = assetConfirmation;
        _receiptReader = receiptReader;
        _quick = new(store, translator, interaction);
        _undo = undo;
        _store = store;
        _translator = translator;
        _localization = localization;
        _time = time;
        _categories = new CategoryLookup([], translator);
        ObserveLoading(Loading);
        KindNames = [.. ChipKinds.Select(k => translator[$"EntryKind_{k}"])];
        Title = translator["Entry_NewTitle"];
        AmountText = string.Empty;
        EntryTitle = string.Empty;
        Payee = string.Empty;
        Note = string.Empty;
        ToAmountText = string.Empty;
        FeeText = string.Empty;
        ForeignAmountText = string.Empty;
        ForeignCurrency = Currencies.Euro.Code;
        CurrencyCode = Currencies.Euro.Code;
        ToCurrencyCode = Currencies.Euro.Code;
        Accounts = [];
        ToAccounts = [];
        Date = Today;
        ReimbursementDue = Today.AddDays(30);
    }

    public IReadOnlyList<string> KindNames { get; }

    public IReadOnlyList<string> CurrencyCodes { get; } = [.. Currencies.All.Select(c => c.Code)];

    public ObservableCollection<CategoryChoice> Categories { get; } = [];

    [ObservableProperty]
    public partial string Title { get; set; }

    [ObservableProperty]
    public partial int KindIndex { get; set; }

    [ObservableProperty]
    public partial bool CanChangeKind { get; set; }

    [ObservableProperty]
    public partial string? FixedKindText { get; set; }

    [ObservableProperty]
    public partial string AmountText { get; set; }

    [ObservableProperty]
    public partial string CurrencyCode { get; set; }

    [ObservableProperty]
    public partial string EntryTitle { get; set; }

    [ObservableProperty]
    public partial DateOnly Date { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<AccountChoice> Accounts { get; set; }

    [ObservableProperty]
    public partial AccountChoice? Account { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<AccountChoice> ToAccounts { get; set; }

    [ObservableProperty]
    public partial AccountChoice? ToAccount { get; set; }

    [ObservableProperty]
    public partial string ToCurrencyCode { get; set; }

    [ObservableProperty]
    public partial string ToAmountText { get; set; }

    [ObservableProperty]
    public partial string? ToAmountLabel { get; set; }

    [ObservableProperty]
    public partial string FeeText { get; set; }

    /// <summary>Gets or sets a fee charged at the destination, in its currency (Advanced, ZEX-S0204).</summary>
    [ObservableProperty]
    public partial string DestinationFeeText { get; set; } = string.Empty;

    /// <summary>Gets a value indicating whether the destination fee field is shown (Advanced transfers).</summary>
    [ObservableProperty]
    public partial bool ShowDestinationFee { get; set; }

    [ObservableProperty]
    public partial bool ShowDetails { get; set; }

    [ObservableProperty]
    public partial string Payee { get; set; }

    [ObservableProperty]
    public partial string Note { get; set; }

    [ObservableProperty]
    public partial bool ForeignEnabled { get; set; }

    [ObservableProperty]
    public partial string ForeignCurrency { get; set; }

    [ObservableProperty]
    public partial string ForeignAmountText { get; set; }

    [ObservableProperty]
    public partial string? RefundInfo { get; set; }

    /// <summary>Gets or sets an icon for this entry only; <see langword="null"/> uses the category icon (VIS-03).</summary>
    [ObservableProperty]
    public partial string? IconKey { get; set; }

    [ObservableProperty]
    public partial string? AmountError { get; set; }

    /// <summary>Gets or sets the source account problem beside its picker.</summary>
    [ObservableProperty]
    public partial string? AccountError { get; set; }

    /// <summary>Gets or sets the destination account problem beside its picker.</summary>
    [ObservableProperty]
    public partial string? ToAccountError { get; set; }

    /// <summary>Gets or sets the destination amount problem beside its input.</summary>
    [ObservableProperty]
    public partial string? ToAmountError { get; set; }

    /// <summary>Gets or sets the source fee problem beside its input.</summary>
    [ObservableProperty]
    public partial string? FeeError { get; set; }

    /// <summary>Gets or sets the destination fee problem beside its input.</summary>
    [ObservableProperty]
    public partial string? DestinationFeeError { get; set; }

    /// <summary>Gets or sets the original currency problem beside its selector.</summary>
    [ObservableProperty]
    public partial string? ForeignCurrencyError { get; set; }

    /// <summary>Gets or sets the original amount problem beside its input.</summary>
    [ObservableProperty]
    public partial string? ForeignAmountError { get; set; }

    /// <summary>Gets or sets the reimbursable amount problem beside its input.</summary>
    [ObservableProperty]
    public partial string? ReimbursableError { get; set; }

    /// <summary>Requests that the visible form reveal the first problem after publishing all field feedback.</summary>
    public event EventHandler? ValidationFailed;

    [ObservableProperty]
    public partial string? SaveError { get; set; }

    [ObservableProperty]
    public partial bool IsTransfer { get; set; }

    /// <summary>Gets a value indicating whether the entry can be marked as aggregated (Advanced, income or expense, ZEX-S0611).</summary>
    [ObservableProperty]
    public partial bool CanAggregate { get; set; }

    partial void OnIsTransferChanged(bool value) => CanAggregate = (_canAggregate || IsAggregated) && !value;

    partial void OnIsAggregatedChanged(bool value)
    {
        // A new aggregate covers the financial month of its date by default – in the user's calendar and from the month's
        // start day, like the periods everywhere else; the user can change the range.
        if (value && !_loading && AggregatedFrom == AggregatedTo)
        {
            var calendar = Presentation.Calendars.ToPeriod(_localization.CurrentCalendar);
            var (year, month) = PeriodMath.MonthOf(Date, calendar, _startDay);
            (AggregatedFrom, AggregatedTo) = PeriodMath.MonthRange(year, month, calendar, _startDay);
        }
    }

    // Reimbursable part of an expense (F2-TX-03).
    [ObservableProperty]
    public partial bool IsExpense { get; set; }

    [ObservableProperty]
    public partial bool ReimbursableEnabled { get; set; }

    [ObservableProperty]
    public partial string ReimbursableText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ReimbursedBy { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether the entry sums up several purchases or receipts of a range (ZEX-P21, Advanced).</summary>
    [ObservableProperty]
    public partial bool IsAggregated { get; set; }

    [ObservableProperty]
    public partial DateOnly AggregatedFrom { get; set; }

    [ObservableProperty]
    public partial DateOnly AggregatedTo { get; set; }

    /// <summary>Gets or sets a value indicating whether the reimbursement is expected by a day (ZEX-K12); optional.</summary>
    [ObservableProperty]
    public partial bool HasReimbursementDue { get; set; }

    [ObservableProperty]
    public partial DateOnly ReimbursementDue { get; set; }

    // Tags (F2-TX-04): typed comma-separated, with the most used tags one tap away.
    [ObservableProperty]
    public partial string TagsText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial IReadOnlyList<TagSuggestion> TagSuggestions { get; set; } = [];

    private IReadOnlyList<string> _tagsInUse = [];

    partial void OnTagsTextChanged(string value) => UpdateTagSuggestions();

    private void UpdateTagSuggestions()
    {
        var current = EntryTags.Parse(TagsText);
        TagSuggestions = [.. _tagsInUse.Where(t => !current.Contains(t, StringComparer.CurrentCultureIgnoreCase)).Take(8)
            .Select(tag => new TagSuggestion(tag))];
    }

    private void AddTag(string tag) => TagsText = EntryTags.Format(EntryTags.Normalize([.. EntryTags.Parse(TagsText), tag]));

    [ObservableProperty]
    public partial bool ShowCategories { get; set; }

    [ObservableProperty]
    public partial bool ShowAccountPicker { get; set; }

    [ObservableProperty]
    public partial bool ShowToAmount { get; set; }

    [ObservableProperty]
    public partial bool HasNoAccounts { get; set; }

    /// <summary>
    /// Gets or sets the notice under the amount after the account changed to another currency (ZEX-P04): the digits are
    /// kept and relabelled, never converted.
    /// </summary>
    [ObservableProperty]
    public partial string? CurrencyNotice { get; set; }

    /// <summary>Gets or sets the notice that a template or context account could not be used (archived, ZEX-S0103).</summary>
    [ObservableProperty]
    public partial string? AccountNotice { get; set; }

    /// <summary>Gets a value indicating whether no account is chosen yet: the field asks for one and Save is disabled.</summary>
    public bool NeedsAccount => Account is null && !HasNoAccounts;

    /// <summary>Gets a value indicating whether Save can be used now.</summary>
    public bool CanSave => IsFormEditable && Account is not null && _saveSession is { Accepted: false } && MoneyContextsReady;

    /// <summary>Gets whether this new supported transaction can open a detached repeating-plan draft.</summary>
    public bool ShowRepeat => _isNew && Kind is EntryKind.Expense or EntryKind.Income or EntryKind.Transfer;

    /// <summary>Gets the effect of saving, e.g. "−25.00 EUR from Main" (ZEX-S0103, S0204); nothing changes before Save.</summary>
    [ObservableProperty]
    public partial string? EffectText { get; set; }

    /// <summary>Gets the rate implied by the two amounts of a transfer between currencies (ZEX-S0204).</summary>
    [ObservableProperty]
    public partial string? ImpliedRateText { get; set; }

    private AccountChoice? _previousAccount;
    private string? _previousAmountText;

    protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
    {
        var publication = _publicationGuard; publication?.Invoke();
        base.OnPropertyChanged(e); publication?.Invoke();
        if (e.PropertyName is nameof(IsBusy) or nameof(Account) or nameof(HasNoAccounts))
        {
            base.OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs(nameof(CanSave)));
            base.OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs(nameof(CanRepeat)));
            base.OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs(nameof(NeedsAccount)));
            base.OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs(nameof(IsFormEditable)));
            base.OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs(nameof(CanReloadForm)));
        }

        if (!_loading && e.PropertyName is nameof(ForeignCurrency) or nameof(ForeignEnabled) or nameof(ReimbursableEnabled) or nameof(ShowToAmount) or nameof(ShowDestinationFee) or nameof(KindIndex)) RefreshMoneyInputs();
        if (e.PropertyName is nameof(ForeignCurrency) or nameof(ForeignEnabled) or nameof(ReimbursableEnabled) or nameof(ForeignAmountText) or nameof(ReimbursableText) or nameof(Account) or nameof(ToAccount) or nameof(AmountText) or nameof(ToAmountText) or nameof(FeeText) or nameof(DestinationFeeText) or nameof(ShowDestinationFee) or nameof(KindIndex))
        {
            UpdateEffect();
        }
    }

    // Undoes the last account change: the previous account and the amount as typed before (ZEX-P04).
    private void UndoAccountChange()
    {
        if (_previousAccount is null)
        {
            return;
        }

        var (account, amount) = (_previousAccount, _previousAmountText);
        _loading = true;
        try
        {
            Account = account;
            AmountText = amount ?? AmountText;
            if (_previousAmountMeaning is { } meaning) _amountMoney.Restore(meaning);
        }
        finally
        {
            _loading = false;
        }

        UpdateCurrencies();
        CurrencyNotice = null;
        _previousAccount = null;
    }

    // "−25.00 EUR from Main", "+3,000.00 EUR to Main" or for a transfer "−102.00 EUR from Main · +110.00 USD to Dollar".
    private void UpdateEffect()
    {
        EffectText = null;
        ImpliedRateText = null;
        if (!MoneyContextsReady || Account is null || !_accounts.TryGetValue(Account.Id, out var account)
            || !_amountMoney.TryParse(out var amount) || amount <= 0)
        {
            return;
        }

        var culture = _localization.CurrentCulture;
        var outgoing = Kind is EntryKind.Expense or EntryKind.IncomeReversal or EntryKind.Transfer;
        if (Kind == EntryKind.Transfer && !string.IsNullOrWhiteSpace(FeeText)
            && _feeMoney.TryParse(out var fee) && fee > 0)
        {
            amount = checked(amount + fee);
        }

        var parts = new List<string>
        {
            _translator.Format(outgoing ? "Entry_EffectFrom" : "Entry_EffectTo", MoneyText.Format(outgoing ? -amount : amount, account.CurrencyCode, culture, showPlus: !outgoing), account.Name),
        };

        if (Kind == EntryKind.Transfer && ToAccount is not null && _accounts.TryGetValue(ToAccount.Id, out var target))
        {
            var sameCurrency = string.Equals(target.CurrencyCode, account.CurrencyCode, StringComparison.OrdinalIgnoreCase);
            long received = 0;
            var known = sameCurrency
                ? _amountMoney.TryParse(out received)
                : _destinationMoney.TryParse(out received) && received > 0;
            if (known)
            {
                if (ShowDestinationFee && _destinationFeeMoney.TryParse(out var destinationFee) && destinationFee > 0)
                {
                    received -= destinationFee;
                }

                parts.Add(_translator.Format("Entry_EffectTo", MoneyText.Format(received, target.CurrencyCode, culture, showPlus: true), target.Name));
                if (!sameCurrency && _amountMoney.TryParse(out var sent) && sent > 0)
                {
                    var rate = MoneyText.ToDecimal(received, Currencies.TryGet(target.CurrencyCode, out var to) ? to : Currencies.Euro)
                        / MoneyText.ToDecimal(sent, Currencies.TryGet(account.CurrencyCode, out var from) ? from : Currencies.Euro);
                    ImpliedRateText = _translator.Format("Entry_ImpliedRate", account.CurrencyCode, rate.ToString("0.####", culture), target.CurrencyCode);
                }
            }
        }

        EffectText = string.Join(" · ", parts);
    }

    /// <summary>Gets the kind being edited.</summary>
    public EntryKind Kind => _refundOf is not null ? EntryKind.Refund : CanChangeKind ? ChipKinds[Math.Clamp(KindIndex, 0, ChipKinds.Length - 1)] : _entry.Kind;

    /// <summary>Gets a value indicating whether the user changed something.</summary>
    public bool IsDirty => _hasPublishedForm && Snapshot() != _snapshot;

    private DateOnly Today => DateOnly.FromDateTime(_time.GetLocalNow().DateTime);

    /// <summary>Query: <c>id</c> (edit), <c>duplicate</c> (copy of an entry), <c>refundOf</c> (refund), <c>kind</c> (new).</summary>
    public async void ApplyQueryAttributes(IDictionary<string, object> query) => await Presentation.Failures.GuardAsync(() => ApplyQueryAsync(query));

    /// <summary>Gets the notice shown while values read from a receipt wait for review (D-31).</summary>
    [ObservableProperty]
    public partial string? ReceiptNote { get; set; }

    /// <summary>Gets or sets the receipt's relevant source rows, kept only while reviewing.</summary>
    [ObservableProperty]
    public partial string? ReceiptSource { get; set; }

    /// <summary>Gets or sets the source script's direction; Latin source keeps its original decimal punctuation.</summary>
    [ObservableProperty]
    public partial FlowDirection ReceiptSourceDirection { get; set; }

    /// <summary>Gets or sets the warning for an explicit unit that the selected account cannot accept.</summary>
    [ObservableProperty]
    public partial string? ReceiptUnitNotice { get; set; }

    /// <summary>Gets the complete numeric choices; damaged fragments are never selectable.</summary>
    public ObservableCollection<ReceiptChoice> ReceiptChoices { get; } = [];

    // Values read from a receipt fill the form for review; they count as unsaved changes and nothing is saved until Save.
    private void ApplyReceipt(IDictionary<string, object> query)
    {
        if (!query.TryGetValue("receipt", out var result) || result is not ReceiptSuggestion receipt)
        {
            return;
        }

        _receipt = receipt;
        var applied = new List<string>();
        var culture = _localization.CurrentCulture;
        ReceiptSource = receipt.AmountSource is { } source ? source[..Math.Min(400, source.Length)] : null;
        ReceiptSourceDirection = ReceiptSource?.Any(c => c is >= '؀' and <= 'ۿ') == true
            ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        ReceiptChoices.Clear();
        var publication = _publicationGuard;
        foreach (var candidate in receipt.Candidates)
        {
            publication?.Invoke();
            // Isolate the number and printed unit together in RTL; this is the receipt unit, not a display-unit guess.
            var text = "\u2066\u200E" + candidate.Amount.ToString("0.############################", culture) + " "
                + (candidate.UnitLabel ?? _translator["Receipt_UnspecifiedUnit"]) + "\u200E\u2069";
            ReceiptChoices.Add(new(candidate, _translator.Format("Receipt_UseAmount", Vafadar.Localization.Formatting.NativeDigits.Apply(text) ?? text)));
            publication?.Invoke();
        }

        if (receipt.AmountStatus == ReceiptAmountStatus.Found && receipt.Candidates.Count == 1
            && receipt.Candidates[0].TryMinor(Account?.CurrencyCode, out var minor))
        {
            SetReceiptAmount(minor);
            applied.Add(_translator["Entry_Amount"]);
        }

        if (receipt.Date is { } day)
        {
            Date = day;
            applied.Add(_translator["Entry_Date"]);
        }

        if (receipt.Merchant is { } payeeText && string.IsNullOrWhiteSpace(Payee))
        {
            Payee = payeeText;
            ShowDetails = true;
            applied.Add(_translator["Entry_Payee"]);
        }

        var notes = new List<string>
        {
            _translator[receipt.AmountStatus switch
            {
                ReceiptAmountStatus.Found => "Receipt_TotalFound",
                ReceiptAmountStatus.Review => "Receipt_TotalReview",
                _ => "Receipt_TotalMissing",
            }],
        };
        if (applied.Count > 0)
        {
            notes.Add(_translator.Format("Receipt_Review", string.Join(_translator["Reminder_ListSeparator"], applied)));
        }

        // A receipt read on Home becomes the entry's attachment when it is saved (D-37).
        if (_isNew && query.TryGetValue("receiptFile", out var file) && file is Presentation.PendingAttachment pending)
        {
            _pendingAttachment = pending with { Data = pending.Data.ToArray() };
            notes.Add(_translator["Receipt_WillAttach"]);
        }

        ReceiptNote = notes.Count == 0 ? null : string.Join(" ", notes);
        UpdateReceiptUnitNotice();
    }

    // A manual choice is still only an unsaved form edit, and explicit currency conflicts need manual amount entry.
    private void UseReceiptAmount(ReceiptChoice choice)
    {
        if (choice.Candidate.TryMinor(Account?.CurrencyCode, out var minor))
        {
            SetReceiptAmount(minor);
            ReceiptUnitNotice = null;
        }
        else
        {
            ReceiptUnitNotice = _translator["Receipt_UnitReview"];
        }
    }

    private void SetReceiptAmount(long minor)
    {
        // TryMinor verified the account before this method was called.
        var currency = Account!.CurrencyCode;
        AmountText = _amountMoney.Format(minor, currency, _localization.CurrentCulture);
        RefreshMoneyInputs();
        _receiptAppliedText = AmountText;
        _receiptAppliedCurrency = currency;
        _receiptDisplayFactor = DisplayUnits.TryGet(currency, out var unit) ? unit.Factor : 1;
    }

    private void UpdateReceiptUnitNotice()
    {
        ReceiptUnitNotice = _receipt is { Candidates.Count: > 0 }
            && _receipt.Candidates.Any(c => !c.TryMinor(Account?.CurrencyCode, out _))
            ? _translator["Receipt_UnitReview"] : null;
    }

    [ObservableProperty]
    public partial IReadOnlyList<EntryTemplate> Templates { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<TemplateChip> TemplateChips { get; set; } = [];

    [ObservableProperty]
    public partial bool HasTemplates { get; set; }

    // Fills the form from a template; the date stays today and nothing is saved until the user taps Save (TX-04).
    private async Task ApplyTemplateAsync(EntryTemplate template)
    {
        if (IsBusy) return;
        var epoch = _formEpoch; var owner = _loadSession;
        bool Current() => epoch == _formEpoch && !_formDeparted && ReferenceEquals(owner, _loadSession);
        IsBusy = true;
        try
        {
            await _quick.ApplyTemplateAsync(template.Id, reviewed =>
            {
                if (!Current()) return;
                void Guard() { if (!Current()) throw new OperationCanceledException("The original template form retired."); }
                _publicationGuard = Guard;
                try { ApplyReviewedTemplate(reviewed); Guard(); }
                finally { _publicationGuard = null; }
            });
        }
        finally { if (Current()) IsBusy = false; }
    }

    private void ApplyReviewedTemplate(EntryTemplate template)
    {
        var entry = template.CreateEntry(Date == default ? Today : Date);
        AccountNotice = null;
        if (Accounts.All(a => a.Id != entry.AccountId))
        {
            // The template's account is archived or gone: its amount would mean something else in another account, so
            // the amount and the account stay empty and the user is told (ZEX-S0103).
            entry.AccountId = Guid.Empty;
            entry.Amount = 0;
            entry.ToAmount = null;
            AccountNotice = _translator["Entry_TemplateAccountArchived"];
        }

        if (entry.ToAccountId is { } to && Accounts.All(a => a.Id != to))
        {
            entry.ToAccountId = null;
        }

        _loading = true;
        try
        {
            _entry = entry;
            LoadFrom(entry);
            CanChangeKind = true;
        }
        finally
        {
            SynchronizeMoneyInputs(); _loading = false;
        }

        UpdateKindState();
    }

    private static Guid? Get(IDictionary<string, object> query, string key) =>
        query.TryGetValue(key, out var value) && (value is Guid guid || Guid.TryParse(value?.ToString(), out guid)) ? guid : null;

    private void PublishReferenceData(EntryEditorReadSnapshot source)
    {
        var accounts = source.Inputs.Resources.Accounts;
        _accounts = accounts.ToDictionary(a => a.Id);
        _categories = new CategoryLookup(source.Inputs.Resources.Categories, _translator);
        Accounts = [.. accounts.Where(a => !a.IsArchived).Select(a => new AccountChoice(a.Id, a.Name, a.CurrencyCode))];
        ToAccounts = Accounts; HasNoAccounts = Accounts.Count == 0;
        _tagsInUse = source.Inputs.Tags; _rules = source.Inputs.Rules; UpdateTagSuggestions();
    }

    private void LoadFrom(LedgerEntry entry)
    {
        var culture = _localization.CurrentCulture;
        CanChangeKind = entry.Kind is EntryKind.Expense or EntryKind.Income or EntryKind.Transfer && _refundOf is null;
        KindIndex = Math.Max(0, Array.IndexOf(ChipKinds, entry.Kind));
        FixedKindText = CanChangeKind ? null : _translator[$"EntryKind_{entry.Kind}"];

        // An archived account of an existing entry stays selectable for that entry.
        Accounts = EnsureChoice(Accounts, entry.AccountId);
        Account = Accounts.FirstOrDefault(a => a.Id == entry.AccountId);
        CurrencyNotice = null;
        _previousAccount = null;
        if (entry.ToAccountId is { } to)
        {
            ToAccounts = EnsureChoice(ToAccounts, to);
            ToAccount = ToAccounts.FirstOrDefault(a => a.Id == to);
        }
        else
        {
            ToAccount = Accounts.FirstOrDefault(a => a.Id != entry.AccountId);
        }

        var currency = CurrencyOf(entry.AccountId);
        AmountText = entry.Amount > 0 ? _amountMoney.Format(entry.Amount, currency, culture) : string.Empty;
        ToAmountText = entry.ToAmount is { } toAmount && ToAccount is { } toAccount ? _destinationMoney.Format(toAmount, toAccount.CurrencyCode, culture) : string.Empty;
        if (_fee is not null)
        {
            FeeText = _feeMoney.Format(_fee.Amount, currency, culture);
        }

        if (_destinationFee is not null && ToAccount is not null)
        {
            DestinationFeeText = _destinationFeeMoney.Format(_destinationFee.Amount, ToAccount.CurrencyCode, culture);
        }

        EntryTitle = entry.Title ?? string.Empty;
        Date = entry.Date;
        Payee = entry.Payee ?? string.Empty;
        Note = entry.Note ?? string.Empty;
        ForeignEnabled = entry.OriginalAmount is not null;
        if (entry.OriginalAmount is { } original && entry.OriginalCurrencyCode is { } originalCurrency)
        {
            ForeignCurrency = originalCurrency;
            ForeignAmountText = _foreignMoney.Format(original, originalCurrency, culture);
        }

        IconKey = entry.Icon;
        ReimbursableEnabled = entry.ReimbursableAmount is > 0;
        ReimbursableText = entry.ReimbursableAmount is { } reimbursable ? _reimbursableMoney.Format(reimbursable, currency, culture) : string.Empty;
        ReimbursedBy = entry.ReimbursedBy ?? string.Empty;
        HasReimbursementDue = entry.ReimbursementDueDate is not null;
        IsAggregated = entry.IsAggregated;
        AggregatedFrom = entry.AggregatedFrom ?? entry.Date;
        AggregatedTo = entry.AggregatedTo ?? entry.Date;
        ReimbursementDue = entry.ReimbursementDueDate ?? ReimbursementDue;
        TagsText = EntryTags.Format(entry.Tags);
        ShowDetails = !string.IsNullOrEmpty(entry.Payee) || !string.IsNullOrEmpty(entry.Note) || ForeignEnabled || entry.Icon is not null || ReimbursableEnabled || entry.Tags.Count > 0 || entry.IsAggregated;
        BuildCategories(entry.CategoryId);
        // D-392: retained aggregate fields must remain editable in Simple mode. Publish only after
        // their original range is loaded; keep the capability while an unsaved toggle is changed.
        CanAggregate = (_canAggregate || entry.IsAggregated) && !IsTransfer;
    }

    private IReadOnlyList<AccountChoice> EnsureChoice(IReadOnlyList<AccountChoice> choices, Guid id) =>
        choices.Any(a => a.Id == id) || !_accounts.TryGetValue(id, out var account)
            ? choices
            : [.. choices, new AccountChoice(account.Id, account.Name, account.CurrencyCode)];

    private string CurrencyOf(Guid accountId) => _accounts.TryGetValue(accountId, out var account) ? account.CurrencyCode : Currencies.Euro.Code;

    partial void OnKindIndexChanged(int value)
    {
        if (!_loading)
        {
            UpdateKindState();
            BuildCategories(null);
        }
    }

    partial void OnAccountChanged(AccountChoice? oldValue, AccountChoice? newValue)
    {
        // Another currency keeps the typed digits and says so, with Undo (ZEX-P04); a fee in the old currency is cleared.
        if (!_loading && oldValue is not null && newValue is not null && !string.Equals(oldValue.CurrencyCode, newValue.CurrencyCode, StringComparison.OrdinalIgnoreCase))
        {
            _previousAccount = oldValue;
            _previousAmountText = AmountText; _previousAmountMeaning = _amountMoney.Capture();
            if (!string.IsNullOrWhiteSpace(AmountText) || !string.IsNullOrWhiteSpace(FeeText))
            {
                CurrencyNotice = _translator.Format("Entry_CurrencyChanged", MoneyText.UnitName(newValue.CurrencyCode), MoneyText.UnitName(oldValue.CurrencyCode));
            }

            FeeText = string.Empty;
        }
        else if (!_loading && newValue is not null && oldValue is null)
        {
            AccountNotice = null;
        }

        UpdateCurrencies();
        if (!_loading && _receipt is not null)
        {
            UpdateReceiptUnitNotice();
            // Selecting the first account may resolve a clear ISO total; never overwrite a user-entered amount.
            if (oldValue is null && string.IsNullOrWhiteSpace(AmountText)
                && _receipt.AmountStatus == ReceiptAmountStatus.Found && _receipt.Candidates.Count == 1
                && _receipt.Candidates[0].TryMinor(newValue?.CurrencyCode, out var receiptMinor))
            {
                SetReceiptAmount(receiptMinor);
            }
        }
    }

    partial void OnToAccountChanged(AccountChoice? oldValue, AccountChoice? newValue)
    {
        // The amount received is in the destination's currency: a new currency clears it instead of relabelling it.
        if (!_loading && oldValue is not null && newValue is not null && !string.Equals(oldValue.CurrencyCode, newValue.CurrencyCode, StringComparison.OrdinalIgnoreCase))
        {
            ToAmountText = string.Empty;
            DestinationFeeText = string.Empty;
        }

        UpdateCurrencies();
    }

    private void UpdateKindState()
    {
        OnPropertyChanged(nameof(ShowRepeat));
        IsTransfer = Kind == EntryKind.Transfer;
        IsExpense = Kind == EntryKind.Expense;
        ShowCategories = Kind is EntryKind.Expense or EntryKind.Income or EntryKind.Refund or EntryKind.IncomeReversal;
        if (_refundOf is not null && _accounts.TryGetValue(_refundOf.AccountId, out var purchaseAccount))
        {
            // A refund is compared with the purchase amount, so it goes to an account in the same currency.
            Accounts = [.. Accounts.Where(a => a.CurrencyCode == purchaseAccount.CurrencyCode)];
        }

        // The account is always visible (ZEX-S0103), read-only when there is only one.
        ShowAccountPicker = true;

        // A new entry says what it records ("New expense"), so the kind chosen on Home is confirmed at a glance.
        if (_isNew && CanChangeKind)
        {
            Title = Kind switch
            {
                EntryKind.Expense => _translator["Entry_NewExpense"],
                EntryKind.Income => _translator["Entry_NewIncome"],
                EntryKind.Transfer => _translator["Entry_NewTransfer"],
                _ => _translator["Entry_NewTitle"],
            };
        }

        if (_loading)
        {
            return;
        }

        if (Categories.Count == 0 && ShowCategories)
        {
            BuildCategories(_entry.CategoryId);
        }

        UpdateCurrencies();
    }

    private void UpdateCurrencies()
    {
        CurrencyCode = Account?.CurrencyCode ?? Currencies.Euro.Code;
        ToCurrencyCode = ToAccount?.CurrencyCode ?? CurrencyCode;
        ShowToAmount = IsTransfer && Account is not null && ToAccount is not null && Account.CurrencyCode != ToAccount.CurrencyCode;
        // D-101: Simple must expose an existing fee so an unrelated edit never silently removes it.
        ShowDestinationFee = IsTransfer && (_showsFee || _destinationFee is not null || !string.IsNullOrWhiteSpace(DestinationFeeText)) && ToAccount is not null;
        ToAmountLabel = _translator.Format("Entry_ToAmount", ToCurrencyCode);
        RefreshMoneyInputs();
    }

    // Only the chips are drawn again, with the chosen category; the form keeps everything typed.
    Task Presentation.IThemeAware.RefreshThemeAsync()
    {
        BuildCategories(Categories.FirstOrDefault(c => c.IsSelected)?.Id);
        TemplateChips = [.. TemplateChips.Select(chip => chip.Template.Kind == EntryKind.Transfer
            ? chip with { IconColor = Presentation.Palette.TransferText }
            : chip with { IconColor = _categories.Color(chip.Template.CategoryId) })];
        RefreshMoneyInputs(); UpdateEffect();
        return Task.CompletedTask;
    }

    private void BuildCategories(Guid? selectedId)
    {
        Categories.Clear();
        if (!(Kind is EntryKind.Expense or EntryKind.Income or EntryKind.Refund or EntryKind.IncomeReversal))
        {
            return;
        }

        var kind = Kind is EntryKind.Income or EntryKind.IncomeReversal ? CategoryKind.Income : CategoryKind.Expense;
        var candidates = _categories.All
            .Where(c => c.Kind == kind && (!c.IsArchived || c.Id == selectedId))
            .OrderBy(c => c.SystemKey == DefaultCategories.Uncategorized)
            .ThenBy(c => c.SortOrder);

        var publication = _publicationGuard;
        foreach (var category in candidates)
        {
            publication?.Invoke();
            var name = CategoryLookup.NameOf(category, _translator);
            var parent = _categories.Get(category.ParentId);
            var label = parent is null ? name : $"{CategoryLookup.NameOf(parent, _translator)} › {name}";
            Categories.Add(new CategoryChoice(category.Id, label, Icons.Parse(category.Icon, Symbol.Tag), CategoryLookup.DisplayColor(category.Color))
            {
                IsSelected = category.Id == selectedId,
            });
            publication?.Invoke();
        }
    }

    private void UpdateRefundInfo(EntryEditorReadSnapshot source)
    {
        if (_refundOf is null)
        {
            RefundInfo = null;
            return;
        }

        var refundable = EntryActions.Refundable(_refundOf, source.Inputs.Paybacks, exceptRefundId: _entry.Id);
        var presenter = new EntryPresenter(_accounts, _categories, _translator, _localization.CurrentCulture);
        RefundInfo = _translator.Format("Entry_RefundOf", presenter.Title(_refundOf)) + Environment.NewLine
            + _translator.Format("Entry_Refundable", MoneyText.Format(refundable, CurrencyOf(_refundOf.AccountId), _localization.CurrentCulture));
    }

    private void SelectCategory(CategoryChoice choice)
    {
        _categoryChosen = true;
        RuleHint = null;
        foreach (var category in Categories)
        {
            category.IsSelected = ReferenceEquals(category, choice) && !category.IsSelected;
        }
    }

    // Categorization rules (F2-TX-04): a new entry gets the category of the best matching rule until the user picks a
    // category; the suggestion is shown and can be undone.
    private IReadOnlyList<CategoryRule> _rules = [];
    private bool _categoryChosen;
    private Guid? _beforeSuggestion;

    [ObservableProperty]
    public partial string? RuleHint { get; set; }

    partial void OnPayeeChanged(string value) => SuggestCategory();

    partial void OnEntryTitleChanged(string value) => SuggestCategory();

    private void SuggestCategory()
    {
        if (_loading || _categoryChosen || !CanChangeKind || !(Kind is EntryKind.Expense or EntryKind.Income) || _categories is null)
        {
            return;
        }

        var kind = Kind == EntryKind.Income ? CategoryKind.Income : CategoryKind.Expense;
        var rule = CategoryRules.Suggest(_rules, _categories.All.ToDictionary(c => c.Id), kind, Payee, EntryTitle);
        var current = Categories.FirstOrDefault(c => c.IsSelected)?.Id;
        if (rule is null)
        {
            if (RuleHint is not null)
            {
                SelectById(_beforeSuggestion);
                RuleHint = null;
            }

            return;
        }

        if (RuleHint is null)
        {
            _beforeSuggestion = current;
        }

        SelectById(rule.CategoryId);
        RuleHint = _translator.Format("Entry_RuleSuggested", rule.Match);
    }

    private void SelectById(Guid? id)
    {
        foreach (var category in Categories)
        {
            category.IsSelected = category.Id == id;
        }
    }

    private void UndoSuggestion()
    {
        SelectById(_beforeSuggestion);
        RuleHint = null;
        _categoryChosen = true;
    }

    private void ToggleDetails() => ShowDetails = !ShowDetails;

    // Finds overlaps of the entries to save with aggregated entries and asks: link and replace, keep both or cancel.
    // Returns the aggregated entries as they were before a replacement (for Undo), an empty list without a change, or
    // A choice belongs to this exact detached proposal and owner; cancellation changes no draft or ledger row.
    private async Task<IReadOnlyList<ImportAggregateChoice>?> ResolveOverlapsAsync(EntryAggregateReview review, Func<bool> current)
    {
        var choices = new List<ImportAggregateChoice>(); var linkedDetails = new HashSet<Guid>();
        var replace = _translator["Aggregate_Replace"]; var keep = _translator["Aggregate_KeepBoth"];
        foreach (var overlap in review.Overlaps)
        {
            if (!current()) return null;
            var currency = _accounts.TryGetValue(overlap.Aggregate.AccountId, out var account) ? account.CurrencyCode : Currencies.Euro.Code;
            var message = _translator.Format("Aggregate_Overlap", overlap.Aggregate.Title ?? _categories.Name(overlap.Aggregate.CategoryId),
                MoneyText.Format(overlap.Aggregate.Amount, currency, _localization.CurrentCulture),
                MoneyText.Format(overlap.DetailedTotal, currency, _localization.CurrentCulture),
                MoneyText.Format(overlap.Remainder, currency, _localization.CurrentCulture));
            var canLink = review.CanLink(overlap.Aggregate.Id) && !overlap.Detailed.Any(e => linkedDetails.Contains(e.Id));
            bool link;
            if (!canLink)
            {
                message += "\n\n" + _translator["Aggregate_LinkUnavailable"] + "\n\n" + _translator["Import_AggregateKept"];
                // The explanation belongs in the wrapping body; a native confirmation footer needs a compact caption.
                var keepBoth = await Shell.Current.DisplayAlertAsync(_translator["Aggregate_Review"], message, _translator["Import_KeepBothShort"], _translator["Common_Cancel"]);
                if (!current() || !keepBoth) return null;
                link = false;
            }
            else
            {
                // Native action-sheet titles trim long financial text on Windows. Review the complete
                // original amounts in a wrapping native message before offering the three distinct outcomes.
                var reviewed = await Shell.Current.DisplayAlertAsync(_translator["Aggregate_Review"], message,
                    _translator["Common_Next"], _translator["Common_Cancel"]);
                if (!current() || !reviewed) return null;
                var answer = await Shell.Current.DisplayActionSheetAsync(_translator["Aggregate_Review"], _translator["Common_Cancel"], null, replace, keep);
                if (!current() || answer != replace && answer != keep) return null;
                link = answer == replace;
            }
            choices.Add(new(overlap, link));
            if (link) linkedDetails.UnionWith(overlap.Detailed.Select(e => e.Id));
        }
        return choices;
    }

    /// <summary>Opens the plan editor with detached supported values; neither navigation nor validation saves money.</summary>
    private async Task RepeatAsync()
    {
        if (!ShowRepeat || !CanRepeat || Account is null) { return; }
        var handoffRaw = Snapshot(); var handoffMoney = MoneyRevision;
        SaveError = null;
        if (!CheckReceiptDisplayUnit()) { return; }
        var validation = EntryDraftValidation.Validate(WithMoneyContexts(new EntryValidationInput
        {
            Kind = Kind, AccountId = Account.Id, CurrencyCode = CurrencyCode, AmountText = AmountText,
            DestinationId = ToAccount?.Id, DestinationCurrencyCode = ToAccount?.CurrencyCode ?? CurrencyCode,
            DestinationAmountRequired = ShowToAmount, DestinationAmountText = ToAmountText,
        }), _localization.CurrentCulture);
        string? Text(string? key) => key is null ? null : _translator[key];
        AccountError = Text(validation.AccountErrorKey);
        AmountError = Text(validation.AmountErrorKey);
        ToAccountError = Text(validation.DestinationErrorKey);
        ToAmountError = Text(validation.DestinationAmountErrorKey);
        if (validation.HasErrors || validation.Amount is null)
        {
            ValidationFailed?.Invoke(this, EventArgs.Empty);
            return;
        }

        if (handoffRaw != Snapshot() || handoffMoney != MoneyRevision || !RepeatMoneyReady) return;
        var draft = new EntryPlanDraft
        {
            Kind = Kind, AccountId = Account.Id, CurrencyCode = Account.CurrencyCode, Amount = validation.Amount.Value,
            Date = Date, Name = string.IsNullOrWhiteSpace(EntryTitle)
                ? Kind == EntryKind.Transfer ? _translator["EntryKind_Transfer"]
                    : _categories.Name(Categories.FirstOrDefault(category => category.IsSelected)?.Id) : EntryTitle.Trim(),
            CategoryId = Categories.FirstOrDefault(category => category.IsSelected)?.Id, Note = Note,
            ToAccountId = ToAccount?.Id, ToCurrencyCode = ToAccount?.CurrencyCode, ToAmount = validation.DestinationAmount,
        };
        var epoch = _formEpoch; var owner = _loadSession; var raw = Snapshot();
        bool Current() => epoch == _formEpoch && !_formDeparted && ReferenceEquals(owner, _loadSession);
        IsBusy = true;
        try
        {
            if (owner is null) return; await owner.ValidateAsync();
            if (!Current() || raw != Snapshot() || handoffMoney != MoneyRevision || !RepeatMoneyReady) return;
            // The original editor stays on the navigation stack with its complete unsaved draft and attachments.
            await Presentation.Failures.GuardAsync(() => Shell.Current.GoToAsync(AppShell.PlanEditorRoute,
                  new Dictionary<string, object> { ["entryDraft"] = draft,
                      ["entryOrigin"] = new EntryPlanOrigin(owner.Review ?? throw new InvalidOperationException("The original entry form retired."),
                          () => ReferenceEquals(owner, _loadSession)) }));
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            if (Current()) { owner?.Retire(); _quick.Retire(); Loading = new(); await FailFormReadAsync(Loading, error); }
        }
        finally { if (Current()) IsBusy = false; }
    }

    /// <summary>Prevents a receipt amount from being reinterpreted after a display-unit change in another form.</summary>
    private bool CheckReceiptDisplayUnit()
    {
        if (Account is null) { return true; }
        var displayFactor = DisplayUnits.TryGet(Account.CurrencyCode, out var receiptUnit) ? receiptUnit.Factor : 1;
        if (_receiptAppliedText != AmountText || _receiptAppliedCurrency != Account.CurrencyCode || displayFactor == _receiptDisplayFactor)
        { return true; }
        ReceiptUnitNotice = _translator["Receipt_UnitChanged"];
        SaveError = ReceiptUnitNotice;
        return false;
    }

    private async Task SaveAsync()
    {
        if (IsBusy || _saveSession is not { Accepted: false } session)
        {
            return;
        }
        RefreshMoneyInputs();
        if (!MoneyContextsReady) { ValidationFailed?.Invoke(this, EventArgs.Empty); return; }
        var moneyRevision = MoneyRevision;
        var revision = _saveRevision; var originalEntry = _entry; var rawDraft = Snapshot();
        var loadOwner = _loadSession;

        SaveError = null;
        AmountError = AccountError = ToAccountError = ToAmountError = FeeError = DestinationFeeError
            = ForeignCurrencyError = ForeignAmountError = ReimbursableError = null;
        if (Account is null)
        {
            AccountError = _translator[HasNoAccounts ? "Entry_NoAccounts" : "Entry_ChooseAccount"];
            ValidationFailed?.Invoke(this, EventArgs.Empty);
            return;
        }

        if (!CheckReceiptDisplayUnit()) { return; }

        // Keep the draft untouched while the valued-asset confirmation is pending (D-74 / ZEX-S0408).
        IsBusy = true;
        try
        {
            if (loadOwner is null) return;
            await loadOwner.ValidateAsync();
            if (!OwnsSave(session, revision, originalEntry) || rawDraft != Snapshot() || moneyRevision != MoneyRevision || !MoneyContextsReady)
        { if (OwnsSave(session, revision, originalEntry)) SaveError = _translator["Amount_DraftChanged"]; return; }
            await _assetConfirmation.RunAsync(Kind, _accounts.GetValueOrDefault(Account.Id), async () =>
            {
                if (OwnsSave(session, revision, originalEntry) && rawDraft == Snapshot() && moneyRevision == MoneyRevision && MoneyContextsReady)
                    await SaveConfirmedAsync(session, revision, originalEntry, rawDraft, moneyRevision);
            });
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            if (OwnsSave(session, revision, originalEntry))
            {
                _loadSession?.Retire(); _quick.Retire(); Loading = new();
                await FailFormReadAsync(Loading, error);
            }
        }
        finally { if (OwnsSave(session, revision, originalEntry)) { IsBusy = false; PublishFormAvailability(); } }
    }

    /// <summary>Validates and saves the editor draft only after any required valued-asset consent.</summary>
    private async Task SaveConfirmedAsync(EntryEditorSaveSession session, long revision, LedgerEntry originalEntry, string rawDraft, string moneyRevision)
    {
        // D-100: publish all independent monetary problems before mutating the entry or synchronizing its fees.
        var validation = EntryDraftValidation.Validate(WithMoneyContexts(new EntryValidationInput
        {
            Kind = Kind, AccountId = Account?.Id, HasNoAccounts = HasNoAccounts, CurrencyCode = CurrencyCode,
            AmountText = AmountText, DestinationId = ToAccount?.Id,
            DestinationCurrencyCode = ToAccount?.CurrencyCode ?? CurrencyCode,
            DestinationAmountRequired = ShowToAmount, DestinationAmountText = ToAmountText,
            FeeText = FeeText, DestinationFeeApplicable = ShowDestinationFee, DestinationFeeText = DestinationFeeText,
            ForeignEnabled = ForeignEnabled, ForeignCurrency = ForeignCurrency, ForeignAmountText = ForeignAmountText,
            ReimbursableEnabled = ReimbursableEnabled, ReimbursableText = ReimbursableText,
        }), _localization.CurrentCulture);
        string? Text(string? key) => key is null ? null : _translator[key];
        AccountError = Text(validation.AccountErrorKey);
        AmountError = Text(validation.AmountErrorKey);
        ToAccountError = Text(validation.DestinationErrorKey);
        ToAmountError = Text(validation.DestinationAmountErrorKey);
        FeeError = Text(validation.FeeErrorKey);
        DestinationFeeError = Text(validation.DestinationFeeErrorKey);
        ForeignCurrencyError = Text(validation.ForeignCurrencyErrorKey);
        ForeignAmountError = validation.ForeignAmountInputExample is { } originalExample
            ? _translator.Format("Amount_InvalidWithExample", originalExample) : Text(validation.ForeignAmountErrorKey);
        ReimbursableError = Text(validation.ReimbursableErrorKey);
        if (validation.HasErrors || Account is null || validation.Amount is null)
        {
            // Details can be collapsed without clearing values (D-90); expose any invalid retained detail input.
            if (ForeignCurrencyError is not null || ForeignAmountError is not null || ReimbursableError is not null)
            { ShowDetails = true; }
            ValidationFailed?.Invoke(this, EventArgs.Empty);
            return;
        }
        if (!OwnsSave(session, revision, originalEntry) || rawDraft != Snapshot() || moneyRevision != MoneyRevision || !MoneyContextsReady)
        { if (OwnsSave(session, revision, originalEntry)) SaveError = _translator["Amount_DraftChanged"]; return; }
        var amount = validation.Amount.Value;
        var fee = validation.Fee;
        var toAmount = validation.DestinationAmount;
        var foreignAmount = validation.ForeignAmount;
        var destinationFee = validation.DestinationFee;
        var reimbursableAmount = validation.ReimbursableAmount;

        var saved = false;
        var attachmentFailed = false;
        try
        {
            var proposed = originalEntry.Copy();
            var kind = Kind;
            proposed.Kind = kind;
            proposed.ReimbursableAmount = reimbursableAmount;
            proposed.Tags = EntryTags.Parse(TagsText);
            proposed.ReimbursedBy = reimbursableAmount is null || string.IsNullOrWhiteSpace(ReimbursedBy) ? null : ReimbursedBy.Trim();
            proposed.ReimbursementDueDate = reimbursableAmount is not null && HasReimbursementDue ? ReimbursementDue : null;
            // The flag is set in Advanced; an aggregated entry edited in Simple keeps it (nothing is lost by the mode).
            proposed.IsAggregated = IsAggregated && kind is EntryKind.Income or EntryKind.Expense;
            proposed.AggregatedFrom = proposed.IsAggregated ? (AggregatedFrom <= AggregatedTo ? AggregatedFrom : AggregatedTo) : null;
            proposed.AggregatedTo = proposed.IsAggregated ? (AggregatedFrom <= AggregatedTo ? AggregatedTo : AggregatedFrom) : null;
            proposed.Amount = amount;
            proposed.AccountId = Account.Id;
            proposed.Date = Date;
            proposed.Title = string.IsNullOrWhiteSpace(EntryTitle) ? null : EntryTitle.Trim();
            proposed.Payee = string.IsNullOrWhiteSpace(Payee) ? null : Payee.Trim();
            proposed.Note = string.IsNullOrWhiteSpace(Note) ? null : Note.TrimEnd();
            proposed.OriginalAmount = foreignAmount;
            proposed.OriginalCurrencyCode = foreignAmount is null ? null : ForeignCurrency;
            proposed.Review = ReviewState.Confirmed;
            proposed.Icon = IconKey;

            var deleteIds = new List<Guid>();
            var batch = new List<LedgerEntry> { proposed };
            if (kind == EntryKind.Transfer)
            {
                proposed.CategoryId = null;
                proposed.ToAccountId = ToAccount?.Id;
                proposed.ToAmount = toAmount;
                var synced = EntryActions.SyncTransferFee(proposed, _fee?.Copy(), fee, _categories.Fees());
                if (synced is null && _fee is not null)
                {
                    deleteIds.Add(_fee.Id);
                }
                else if (synced is not null)
                {
                    batch.Add(synced);
                }


                // A destination fee is an expense there; existing fees remain editable in Simple (D-101 / ZEX-S0204).
                var destination = EntryActions.SyncDestinationFee(proposed, _destinationFee?.Copy(), destinationFee, _categories.Fees());
                if (destination is null && _destinationFee is not null)
                {
                    deleteIds.Add(_destinationFee.Id);
                }
                else if (destination is not null)
                {
                    batch.Add(destination);
                }

            }
            else
            {
                var categoryKind = kind is EntryKind.Income or EntryKind.IncomeReversal ? CategoryKind.Income : CategoryKind.Expense;
                proposed.CategoryId = Categories.FirstOrDefault(c => c.IsSelected)?.Id ?? _categories.Uncategorized(categoryKind);
                if (_fee is not null || _destinationFee is not null)
                {
                    // The entry is no longer a transfer: its fees go with it.
                    deleteIds.AddRange(new[] { _fee?.Id, _destinationFee?.Id }.OfType<Guid>());
                    proposed.GroupId = null;
                }
            }

            bool CurrentDraft() => OwnsSave(session, revision, originalEntry) && rawDraft == Snapshot() && moneyRevision == MoneyRevision && MoneyContextsReady;
            var source = _loadSession?.Review ?? throw new InvalidOperationException("The original editor retired.");
            var review = await _store.ReadEditorAggregatesAsync(source, batch, deleteIds);
            if (!CurrentDraft()) return;
            var choices = await ResolveOverlapsAsync(review, CurrentDraft);
            if (choices is null || !CurrentDraft()) return;
            var pending = _pendingAttachment;
            var document = pending is null ? null : new EntryAttachment { EntryId = originalEntry.Id,
                FileName = pending.FileName, ContentType = pending.ContentType, Data = pending.Data };
            var result = await session.SaveAsync(batch, deleteIds, originalEntry.Id, document, CurrentDraft, review, choices);
            saved = session.Accepted;
            attachmentFailed = session.AttachmentFailed;
            if (!OwnsSave(session, revision, originalEntry)) return;
            if (!result.Succeeded)
            {
                SaveError = string.Join(Environment.NewLine, result.Errors.Select(e => _translator[$"LedgerError_{e}"]));
                return;
            }

            if (session.AggregateUndo is { } undo)
                _undo.Offer(() => _store.UndoEditorAggregateAsync(undo), undo.CreatedAt, UndoNoticeKind.AggregateAdjusted);

            _snapshot = Snapshot();

            saved = true;
            _pendingAttachment = null;
            OnPropertyChanged(nameof(CanSave));
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // A fallible Undo-offer or native publication subscriber is after the financial boundary.
            // Keep the saved form read-only and clean rather than reporting an uncommitted draft.
            saved |= session.Accepted;
            if (saved)
            {
                if (OwnsSave(session, revision, originalEntry))
                {
                    _snapshot = Snapshot(); _pendingAttachment = null;
                    SaveError = _translator["Occurrence_SavedRefreshFailed"];
                    OnPropertyChanged(nameof(CanSave)); PublishFormAvailability();
                }
                return;
            }
            // The input stays in the form (TX-06, AT-04).
            if (OwnsSave(session, revision, originalEntry))
            {
                SaveError = CommercialFailureMessage.OrFallback(ex, _translator, "Common_SaveFailed", draftKept: true);
                if (ex is EntryEditorSourceChangedException)
                {
                    _loadSession?.Retire(); _quick.Retire(); Loading = new(); await FailFormReadAsync(Loading, ex);
                }
            }
        }

        // Leaving the page is not part of saving: a navigation problem after a successful save must not say "not saved".
        if (saved && OwnsSave(session, revision, originalEntry))
        {
            await Presentation.Failures.GuardAsync(async () =>
            {
                await Shell.Current.GoToAsync("..");
                if (attachmentFailed)
                {
                    await Shell.Current.DisplayAlertAsync(_translator["Receipt_Title"], _translator["Receipt_AttachFailed"], _translator["Common_Ok"]);
                }
            });
        }
    }

    private bool OwnsSave(EntryEditorSaveSession session, long revision, LedgerEntry entry) =>
        revision == _saveRevision && ReferenceEquals(_saveSession, session) && ReferenceEquals(_entry, entry);

    /// <summary>Retires only pending native/save callbacks when the form leaves; raw input and known acceptance remain.</summary>
    public void RetirePendingSave()
    {
        _publicationGuard = null; _saveRevision++; _formEpoch++; _formDeparted = true;
        _loadSession?.Retire(); _quick.Retire(); _formLoadTask = null; IsBusy = false; ResetFormCommands(); PublishFormAvailability();
    }

    private async Task CancelAsync()
    {
        var epoch = _formEpoch; var source = _loadSession; var draft = Snapshot();
        if (!IsDirty || await ConfirmDiscardAsync())
        {
            if (epoch == _formEpoch && !_formDeparted && ReferenceEquals(source, _loadSession) && draft == Snapshot())
                await Shell.Current.GoToAsync("..");
        }
    }

    private async Task AddAccountAsync()
    {
        var epoch = _formEpoch; var owner = _loadSession;
        if (owner is null) return;
        try
        {
            await owner.ValidateAsync();
            if (epoch == _formEpoch && !_formDeparted && ReferenceEquals(owner, _loadSession) && owner.Review is { } source)
            {
                EntryAccountHandoff? visit = null;
                visit = new(source, () => ReferenceEquals(_accountVisit, visit) && ReferenceEquals(owner, _loadSession));
                _accountVisit = visit;
                await Shell.Current.GoToAsync(AppShell.AccountEditorRoute, new Dictionary<string, object> { ["entryAccountHandoff"] = visit });
            }
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            if (epoch == _formEpoch && !_formDeparted && ReferenceEquals(owner, _loadSession))
            { owner.Retire(); _quick.Retire(); Loading = new(); await FailFormReadAsync(Loading, error); }
        }
    }

    /// <summary>Asks whether unsaved changes may be discarded.</summary>
    public Task<bool> ConfirmDiscardAsync() => Shell.Current.DisplayAlertAsync(
        _translator["Common_DiscardTitle"], _translator["Common_DiscardMessage"], _translator["Common_Discard"], _translator["Common_KeepEditing"]);

    // Every field the user can change, so leaving with a change – even only a tag or the reimbursement – asks first.
    private string Snapshot() => Presentation.UnsavedChanges.Fingerprint(
        KindIndex, AmountText, EntryTitle, Date, Account?.Id, ToAccount?.Id, ToAmountText, FeeText, DestinationFeeText, Payee, Note,
        ForeignEnabled, ForeignCurrency, ForeignAmountText, IconKey, Categories.FirstOrDefault(c => c.IsSelected)?.Id, TagsText,
        ReimbursableEnabled, ReimbursableText, ReimbursedBy, HasReimbursementDue, ReimbursementDue, IsAggregated, AggregatedFrom, AggregatedTo, MoneyMeaning);
}
