using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentIcons.Common;
using Vafadar.Finance.App.Presentation;
using Vafadar.Finance.Core.Accounts;
using Vafadar.Finance.Core.Categories;
using Vafadar.Finance.Core.Ledger;
using Vafadar.Finance.Core.Money;
using Vafadar.Finance.Data;
using Vafadar.Localization;
using Vafadar.Maui.Mvvm;

namespace Vafadar.Finance.App.Features.Entries;

/// <summary>An account in a picker.</summary>
public sealed record AccountChoice(Guid Id, string Name, string CurrencyCode)
{
    public override string ToString() => $"{Name} ({CurrencyCode})";
}

/// <summary>A category tile in the entry editor.</summary>
public sealed partial class CategoryChoice(Guid id, string name, Symbol icon, Color color) : ObservableObject
{
    public Guid Id { get; } = id;

    public string Name { get; } = name;

    public Symbol Icon { get; } = icon;

    public Color Color { get; } = color;

    public Color Background { get; } = color.WithAlpha(0.12f);

    [ObservableProperty]
    public partial bool IsSelected { get; set; }
}

/// <summary>
/// Creates and edits entries (UI-03): expense, income, transfer (with fee and destination amount), refund, foreign
/// amount. The entry object keeps its id across saves, so repeated taps on Save never create duplicates (AT-03).
/// </summary>
public sealed partial class EntryEditorViewModel : ViewModelBase, IQueryAttributable
{
    private static readonly EntryKind[] ChipKinds = [EntryKind.Expense, EntryKind.Income, EntryKind.Transfer];

    private readonly FinanceStore _store;
    private readonly Translator _translator;
    private readonly ILocalizationService _localization;
    private readonly TimeProvider _time;
    private LedgerEntry _entry = new();
    private LedgerEntry? _fee;
    private LedgerEntry? _refundOf;
    private CategoryLookup _categories;
    private Dictionary<Guid, Account> _accounts = [];
    private string _snapshot = string.Empty;
    private bool _loading;

    public EntryEditorViewModel(FinanceStore store, Translator translator, ILocalizationService localization, TimeProvider time)
    {
        _store = store;
        _translator = translator;
        _localization = localization;
        _time = time;
        _categories = new CategoryLookup([], translator);
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

    [ObservableProperty]
    public partial string? AmountError { get; set; }

    [ObservableProperty]
    public partial string? SaveError { get; set; }

    [ObservableProperty]
    public partial bool IsTransfer { get; set; }

    [ObservableProperty]
    public partial bool ShowCategories { get; set; }

    [ObservableProperty]
    public partial bool ShowAccountPicker { get; set; }

    [ObservableProperty]
    public partial bool ShowToAmount { get; set; }

    [ObservableProperty]
    public partial bool HasNoAccounts { get; set; }

    /// <summary>Gets the kind being edited.</summary>
    public EntryKind Kind => _refundOf is not null ? EntryKind.Refund : CanChangeKind ? ChipKinds[Math.Clamp(KindIndex, 0, ChipKinds.Length - 1)] : _entry.Kind;

    /// <summary>Gets a value indicating whether the user changed something.</summary>
    public bool IsDirty => Snapshot() != _snapshot;

    private DateOnly Today => DateOnly.FromDateTime(_time.GetLocalNow().DateTime);

    /// <summary>Query: <c>id</c> (edit), <c>duplicate</c> (copy of an entry), <c>refundOf</c> (refund), <c>kind</c> (new).</summary>
    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        _loading = true;
        try
        {
            await LoadReferenceDataAsync();
            var settings = await _store.GetSettingsAsync();
            var active = Accounts;

            if (Get(query, "id") is { } id && await _store.GetEntryAsync(id) is { } existing)
            {
                _entry = existing;
                _fee = existing.Kind == EntryKind.Transfer && existing.GroupId is { } group
                    ? EntryActions.FindTransferFee(existing, await _store.GetGroupAsync(group))
                    : null;
                if (existing.Kind == EntryKind.Refund && existing.RefundOfId is { } purchaseId)
                {
                    _refundOf = await _store.GetEntryAsync(purchaseId);
                }

                Title = _translator["Entry_EditTitle"];
                LoadFrom(existing);
            }
            else if (Get(query, "duplicate") is { } sourceId && await _store.GetEntryAsync(sourceId) is { } source)
            {
                _entry = EntryActions.Duplicate(source, Today);
                if (source.GroupId is { } group && EntryActions.FindTransferFee(source, await _store.GetGroupAsync(group)) is { } sourceFee)
                {
                    FeeText = MoneyText.ForInput(sourceFee.Amount, CurrencyOf(source.AccountId), _localization.CurrentCulture);
                }

                LoadFrom(_entry);
            }
            else if (Get(query, "refundOf") is { } purchaseId && await _store.GetEntryAsync(purchaseId) is { } purchase)
            {
                _refundOf = purchase;
                var refundable = EntryActions.Refundable(purchase, await _store.GetRefundsAsync(purchase.Id));
                _entry = EntryActions.CreateRefund(purchase, refundable, purchase.AccountId, Today);
                Title = _translator["Entry_RefundTitle"];
                LoadFrom(_entry);
            }
            else
            {
                var kind = query.TryGetValue("kind", out var value) && Enum.TryParse<EntryKind>(value?.ToString(), out var parsed) ? parsed : EntryKind.Expense;
                var defaultAccount = active.FirstOrDefault(a => a.Id == settings.DefaultAccountId) ?? active.FirstOrDefault();
                _entry = new LedgerEntry { Kind = kind, Date = Today, AccountId = defaultAccount?.Id ?? Guid.Empty };
                CanChangeKind = true;
                KindIndex = Math.Max(0, Array.IndexOf(ChipKinds, kind));
                Account = defaultAccount;
                ToAccount = active.FirstOrDefault(a => a.Id != defaultAccount?.Id);
            }
        }
        finally
        {
            _loading = false;
        }

        UpdateKindState();
        await UpdateRefundInfoAsync();
        _snapshot = Snapshot();
        query.Clear();
    }

    private static Guid? Get(IDictionary<string, object> query, string key) =>
        query.TryGetValue(key, out var value) && (value is Guid guid || Guid.TryParse(value?.ToString(), out guid)) ? guid : null;

    private async Task LoadReferenceDataAsync()
    {
        var accounts = await _store.GetAccountsAsync();
        _accounts = accounts.ToDictionary(a => a.Id);
        _categories = new CategoryLookup(await _store.GetCategoriesAsync(), _translator);
        Accounts = [.. accounts.Where(a => !a.IsArchived).Select(a => new AccountChoice(a.Id, a.Name, a.CurrencyCode))];
        ToAccounts = Accounts;
        HasNoAccounts = Accounts.Count == 0;
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
        AmountText = entry.Amount > 0 ? MoneyText.ForInput(entry.Amount, currency, culture) : string.Empty;
        ToAmountText = entry.ToAmount is { } toAmount && ToAccount is { } toAccount ? MoneyText.ForInput(toAmount, toAccount.CurrencyCode, culture) : string.Empty;
        if (_fee is not null)
        {
            FeeText = MoneyText.ForInput(_fee.Amount, currency, culture);
        }

        EntryTitle = entry.Title ?? string.Empty;
        Date = entry.Date;
        Payee = entry.Payee ?? string.Empty;
        Note = entry.Note ?? string.Empty;
        ForeignEnabled = entry.OriginalAmount is not null;
        if (entry.OriginalAmount is { } original && entry.OriginalCurrencyCode is { } originalCurrency)
        {
            ForeignCurrency = originalCurrency;
            ForeignAmountText = MoneyText.ForInput(original, originalCurrency, culture);
        }

        ShowDetails = !string.IsNullOrEmpty(entry.Payee) || !string.IsNullOrEmpty(entry.Note) || ForeignEnabled;
        BuildCategories(entry.CategoryId);
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

    partial void OnAccountChanged(AccountChoice? value) => UpdateCurrencies();

    partial void OnToAccountChanged(AccountChoice? value) => UpdateCurrencies();

    private void UpdateKindState()
    {
        IsTransfer = Kind == EntryKind.Transfer;
        ShowCategories = Kind is EntryKind.Expense or EntryKind.Income or EntryKind.Refund or EntryKind.IncomeReversal;
        if (_refundOf is not null && _accounts.TryGetValue(_refundOf.AccountId, out var purchaseAccount))
        {
            // A refund is compared with the purchase amount, so it goes to an account in the same currency.
            Accounts = [.. Accounts.Where(a => a.CurrencyCode == purchaseAccount.CurrencyCode)];
        }

        ShowAccountPicker = Accounts.Count > 1 || IsTransfer;
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
        ToAmountLabel = _translator.Format("Entry_ToAmount", ToCurrencyCode);
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

        foreach (var category in candidates)
        {
            var name = CategoryLookup.NameOf(category, _translator);
            var parent = _categories.Get(category.ParentId);
            var label = parent is null ? name : $"{CategoryLookup.NameOf(parent, _translator)} › {name}";
            Categories.Add(new CategoryChoice(category.Id, label, Icons.Parse(category.Icon, Symbol.Tag), CategoryLookup.ParseColor(category.Color))
            {
                IsSelected = category.Id == selectedId,
            });
        }
    }

    private async Task UpdateRefundInfoAsync()
    {
        if (_refundOf is null)
        {
            RefundInfo = null;
            return;
        }

        var refundable = EntryActions.Refundable(_refundOf, await _store.GetRefundsAsync(_refundOf.Id), exceptRefundId: _entry.Id);
        var presenter = new EntryPresenter(_accounts, _categories, _translator, _localization.CurrentCulture);
        RefundInfo = _translator.Format("Entry_RefundOf", presenter.Title(_refundOf)) + Environment.NewLine
            + _translator.Format("Entry_Refundable", MoneyText.Format(refundable, CurrencyOf(_refundOf.AccountId), _localization.CurrentCulture));
    }

    [RelayCommand]
    private void SelectCategory(CategoryChoice choice)
    {
        foreach (var category in Categories)
        {
            category.IsSelected = ReferenceEquals(category, choice) && !category.IsSelected;
        }
    }

    [RelayCommand]
    private void ToggleDetails() => ShowDetails = !ShowDetails;

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (IsBusy)
        {
            return;
        }

        SaveError = null;
        AmountError = null;
        if (Account is null)
        {
            SaveError = _translator["Entry_NoAccounts"];
            return;
        }

        var culture = _localization.CurrentCulture;
        var currency = Currencies.TryGet(Account.CurrencyCode, out var known) ? known : Currencies.Euro;
        var parsed = MoneyAmount.TryParse(AmountText, currency, culture, out var amount);
        if (!parsed || amount <= 0)
        {
            AmountError = parsed || string.IsNullOrWhiteSpace(AmountText) ? _translator["LedgerError_AmountMustBePositive"] : _translator["Amount_Invalid"];
            return;
        }

        long fee = 0;
        if (IsTransfer && !string.IsNullOrWhiteSpace(FeeText) && !MoneyAmount.TryParse(FeeText, currency, culture, out fee))
        {
            SaveError = _translator["Amount_Invalid"];
            return;
        }

        long? toAmount = null;
        if (ShowToAmount && ToAccount is not null)
        {
            if (!MoneyAmount.TryParse(ToAmountText, Currencies.Get(ToAccount.CurrencyCode), culture, out var parsedTo) || parsedTo <= 0)
            {
                SaveError = _translator["LedgerError_DestinationAmountRequired"];
                return;
            }

            toAmount = parsedTo;
        }

        long? foreignAmount = null;
        if (ForeignEnabled && !IsTransfer)
        {
            if (!Currencies.TryGet(ForeignCurrency, out var foreign) || !MoneyAmount.TryParse(ForeignAmountText, foreign, culture, out var parsedForeign))
            {
                SaveError = _translator["LedgerError_InvalidOriginalCurrency"];
                return;
            }

            foreignAmount = parsedForeign;
        }

        IsBusy = true;
        try
        {
            var kind = Kind;
            _entry.Kind = kind;
            _entry.Amount = amount;
            _entry.AccountId = Account.Id;
            _entry.Date = Date;
            _entry.Title = string.IsNullOrWhiteSpace(EntryTitle) ? null : EntryTitle.Trim();
            _entry.Payee = string.IsNullOrWhiteSpace(Payee) ? null : Payee.Trim();
            _entry.Note = string.IsNullOrWhiteSpace(Note) ? null : Note.TrimEnd();
            _entry.OriginalAmount = foreignAmount;
            _entry.OriginalCurrencyCode = foreignAmount is null ? null : ForeignCurrency;
            _entry.Review = ReviewState.Confirmed;

            var deleteIds = new List<Guid>();
            var batch = new List<LedgerEntry> { _entry };
            if (kind == EntryKind.Transfer)
            {
                _entry.CategoryId = null;
                _entry.ToAccountId = ToAccount?.Id;
                _entry.ToAmount = toAmount;
                var synced = EntryActions.SyncTransferFee(_entry, _fee, fee, _categories.Fees());
                if (synced is null && _fee is not null)
                {
                    deleteIds.Add(_fee.Id);
                }
                else if (synced is not null)
                {
                    batch.Add(synced);
                }

                _fee = synced;
            }
            else
            {
                var categoryKind = kind is EntryKind.Income or EntryKind.IncomeReversal ? CategoryKind.Income : CategoryKind.Expense;
                _entry.CategoryId = Categories.FirstOrDefault(c => c.IsSelected)?.Id ?? _categories.Uncategorized(categoryKind);
                if (_fee is not null)
                {
                    // The entry is no longer a transfer: its fee goes with it.
                    deleteIds.Add(_fee.Id);
                    _fee = null;
                    _entry.GroupId = null;
                }
            }

            var result = await _store.SaveEntriesAsync(batch, deleteIds);
            if (!result.Succeeded)
            {
                SaveError = string.Join(Environment.NewLine, result.Errors.Select(e => _translator[$"LedgerError_{e}"]));
                return;
            }

            _snapshot = Snapshot();
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception)
        {
            // The input stays in the form (TX-06, AT-04).
            SaveError = _translator["Common_SaveFailed"];
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        if (!IsDirty || await ConfirmDiscardAsync())
        {
            await Shell.Current.GoToAsync("..");
        }
    }

    [RelayCommand]
    private Task AddAccountAsync() => Shell.Current.GoToAsync($"../{AppShell.AccountEditorRoute}");

    /// <summary>Asks whether unsaved changes may be discarded.</summary>
    public Task<bool> ConfirmDiscardAsync() => Shell.Current.DisplayAlertAsync(
        _translator["Common_DiscardTitle"], _translator["Common_DiscardMessage"], _translator["Common_Discard"], _translator["Common_KeepEditing"]);

    private string Snapshot() => string.Join('|',
        KindIndex, AmountText, EntryTitle, Date, Account?.Id, ToAccount?.Id, ToAmountText, FeeText, Payee, Note,
        ForeignEnabled, ForeignCurrency, ForeignAmountText, Categories.FirstOrDefault(c => c.IsSelected)?.Id);
}
