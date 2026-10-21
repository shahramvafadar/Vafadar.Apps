using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Maui.Mvvm;
using Vafadar.Zanance.App.Features.Sync;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Core.Sharing;
using Vafadar.Zanance.Data.Sharing;
using Vafadar.Zanance.App.Presentation;

namespace Vafadar.Zanance.App.Features.Sharing;

/// <summary>One exact contact choice; a financial label is never service membership authority.</summary>
public sealed record HouseholdContactChoice(HouseholdContact Source, string Caption);
/// <summary>One original actual cost and its complete direction-safe display packet.</summary>
public sealed record HouseholdCostItem(HouseholdCost Source, string Caption, string Amount)
{
    /// <summary>Retains date, original description and complete unit for an unambiguous refund-source choice.</summary>
    public string ChoiceCaption => Caption + " Â· " + Amount;
}
/// <summary>One original repayment with its complete direction-safe value.</summary>
public sealed record HouseholdSettlementItem(HouseholdSettlement Source, string Caption, string Amount);
/// <summary>Only explicit saved cash movements are offered for linkage.</summary>
public sealed record HouseholdMovementChoice(HouseholdCashMovement Source, string Caption);
/// <summary>Independent per-currency summary; the value is never added to the cash ledger again.</summary>
public sealed record HouseholdCostSummary(string Caption, string Amount);
/// <summary>One explicit minor-unit allocation draft, preserving its original input culture through display changes.</summary>
public sealed partial class HouseholdShareInput(HouseholdContact contact, CultureInfo culture, string currency = "EUR",
    Func<CultureInfo>? currentCulture = null, Translator? translator = null) : ObservableObject
{
    private readonly MoneyDraftInput _money = new();
    private string _currency = currency;
    /// <summary>Exact original financial contact.</summary>
    public HouseholdContact Contact { get; } = contact;
    internal CultureInfo InputCulture { get; } = culture;
    /// <summary>Gets original raw/context edit ownership, including edits whose text later returns.</summary>
    internal string Stamp => DraftFingerprint.Create(_money.Stamp, _money.Revision);
    /// <summary>Gets the original allocation unit rather than another participant's or the ambient unit.</summary>
    public string UnitName => _money.UnitName(_currency);
    /// <summary>Explains the original allocation factor when translations are attached to the actual form.</summary>
    public string? UnitNote => translator is null ? null : DisplayUnitNote.ForInput(translator, _money, _currency);
    /// <summary>Explains the retained original allocation numeric format.</summary>
    public string? FormatNote => translator is not null && _money.OriginalExample(currentCulture?.Invoke() ?? InputCulture) is { } example
        ? translator.Format("Plan_OriginalInputFormat", example) : null;
    /// <summary>Uncommitted user amount; blank means no allocation.</summary>
    [ObservableProperty] public partial string Text { get; set; } = "";
    /// <summary>Field-local exact allocation error.</summary>
    [ObservableProperty] public partial string? Error { get; set; }
    partial void OnTextChanged(string value)
    {
        _money.ChangeText(value, _currency, currentCulture?.Invoke() ?? InputCulture); Error = null; RefreshMoneyTexts();
    }
    /// <summary>Retains the existing explicit-currency-change contract: clear this original allocation before new input.</summary>
    internal void ResetCurrency(string value)
    {
        _currency = value; _money.Initialize("", value, currentCulture?.Invoke() ?? InputCulture); Text = ""; Error = null; RefreshMoneyTexts();
    }
    /// <summary>Checks original unit ownership independently of an omitted optional allocation.</summary>
    internal bool IsCurrent(string value) => !_money.NeedsReview(value);
    /// <summary>Parses the actual allocation input used by the complete cost Save.</summary>
    internal bool TryRead(string value, out long amount)
    {
        amount = 0;
        return IsCurrent(value) && _money.TryParse(out amount) && amount >= 0;
    }
    /// <summary>Refreshes display-only captions without replacing the raw allocation.</summary>
    internal void RefreshMoneyTexts()
    {
        OnPropertyChanged(nameof(UnitName)); OnPropertyChanged(nameof(UnitNote)); OnPropertyChanged(nameof(FormatNote));
        if (_money.NeedsReview(_currency) && translator is not null) Error = translator.Format("Plan_InputUnitChanged", MoneyText.UnitName(_currency));
    }
}

/// <summary>Complete explicit actual-cost editor over the original native store; drafts, cash movements and service access remain separate.</summary>
public sealed partial class HouseholdCostsViewModel(HouseholdCostStore store, IPersonalSyncEnvironment environment,
    IAppInteraction interaction, ILocalizationService localization, IDateFormatter dates, Translator translator, TimeProvider time) : ViewModelBase
{
    private CancellationTokenSource? visible;
    private HouseholdCostReview? review;
    private Guid? editingCost;
    private Guid? editingSettlement;
    private CultureInfo inputCulture = CultureInfo.InvariantCulture;
    private string? errorKey;
    private bool ready;
    private readonly MoneyDraftInput _amountInput = new();
    private bool needsRefresh;
    private long editorRevision;
    private bool committedInAction;
    /// <summary>Gets the cost/repayment's original visible amount unit.</summary>
    public string AmountUnitName => _amountInput.UnitName(CurrencyCode);
    /// <summary>Explains the original cost/repayment factor.</summary>
    public string? AmountUnitNote => DisplayUnitNote.ForInput(translator, _amountInput, CurrencyCode);
    /// <summary>Explains the original cost/repayment numeric format.</summary>
    public string? AmountFormatNote => _amountInput.OriginalExample(localization.CurrentCulture) is { } example
        ? translator.Format("Plan_OriginalInputFormat", example) : null;
    /// <summary>Shows unit review next to the actual amount without claiming that a write failed.</summary>
    public string? AmountContextError => _amountInput.NeedsReview(CurrencyCode)
        ? translator.Format("Plan_InputUnitChanged", MoneyText.UnitName(CurrencyCode)) : null;
    partial void OnAmountTextChanged(string value)
    {
        _amountInput.ChangeText(value, CurrencyCode, localization.CurrentCulture); AmountError = null; RefreshMoneyTexts();
    }
    /// <summary>Reads the actual submitted amount without taking service or ledger authority.</summary>
    internal bool TryReadOriginalAmount(out long amount)
    {
        // A unit definition can change without translation. Publish the actual field refusal before returning to its native editor.
        RefreshMoneyTexts();
        amount = 0;
        if (!_amountInput.NeedsReview(CurrencyCode) && _amountInput.TryParse(out amount) && amount > 0) return true;
        amount = 0; AmountError = AmountContextError is null ? translator["Amount_Invalid"] : null; return false;
    }
    private void RefreshMoneyTexts()
    {
        foreach (var name in new[] { nameof(AmountUnitName), nameof(AmountUnitNote), nameof(AmountFormatNote), nameof(AmountContextError) }) OnPropertyChanged(name);
        foreach (var row in Shares) row.RefreshMoneyTexts();
    }
    /// <summary>Tracks actual form edits, including values changed back while native consent or a writer waits.</summary>
    protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs args)
    {
        if (args.PropertyName is nameof(Title) or nameof(AmountText) or nameof(CurrencyCode) or nameof(Date) or nameof(IsRefund)
            or nameof(UsesSharedCash) or nameof(EditingCost) or nameof(EditingSettlement) or nameof(Payer) or nameof(Recipient)
            or nameof(RefundOriginal) or nameof(Movement) or nameof(UsesDestinationAmount) or nameof(Note))
            Interlocked.Increment(ref editorRevision);
        base.OnPropertyChanged(args);
    }
    private string EditorStamp() => DraftFingerprint.Create(Interlocked.Read(ref editorRevision), editingCost, editingSettlement,
        Title, _amountInput.Stamp, _amountInput.Revision, CurrencyCode, Date, IsRefund, UsesSharedCash, EditingCost, EditingSettlement,
        Payer?.Source.Id, Recipient?.Source.Id, RefundOriginal?.Source.Id, Movement?.Source.Id, UsesDestinationAmount, Note,
        string.Join(";", Shares.Select(row => DraftFingerprint.Create(row.Contact.Id, row.Stamp))));
    // Acceptance retires the original input before publishing service changes or reading another snapshot.
    private void AcceptEditor()
    { committedInAction = true; needsRefresh = true; ClearEditor(); Refresh(); }
    private void Failed(string key)
    { errorKey = committedInAction ? "HouseholdCost_SavedFollowUpFailed" : key; if (committedInAction) needsRefresh = true; }
    /// <summary>Covered publication only after the complete coherent actual native read.</summary>
    public bool IsReady => ready;
    /// <summary>Current read-only hint; Save still rechecks actual role/file/clock under the writer.</summary>
    public bool CanWrite => ready && !needsRefresh && visible is not null && !IsBusy && review?.CanWrite == true;
    /// <summary>Read and explicit cancel remain available without financial write authority.</summary>
    public bool CanInteract => ready && visible is not null && !IsBusy;
    /// <summary>Financial actions cannot be retired during their submitted save.</summary>
    public bool CanLeave => !IsBusy;
    /// <summary>Actual cost and repayment drafts share one explicit Save/Cancel surface.</summary>
    public bool HasEditor => EditingCost || EditingSettlement;
    /// <summary>A new unsaved draft has no durable cost/repayment record to remove.</summary>
    public bool CanDelete => CanWrite && (editingCost is not null || editingSettlement is not null);
    /// <summary>Content-free translated operation feedback.</summary>
    public string? Error => errorKey is null ? null : translator[errorKey];
    /// <summary>All actual costs with original stable identities.</summary>
    public ObservableCollection<HouseholdCostItem> Costs { get; } = [];
    /// <summary>All actual repayments; opening the list posts nothing.</summary>
    public ObservableCollection<HouseholdSettlementItem> Settlements { get; } = [];
    /// <summary>Contacts retain their exact selection identity through live translation.</summary>
    public ObservableCollection<HouseholdContactChoice> Contacts { get; } = [];
    /// <summary>Only complete original expenses can be selected as a refund source.</summary>
    public ObservableCollection<HouseholdCostItem> OriginalCosts { get; } = [];
    /// <summary>Exact user allocations, never inferred equal shares.</summary>
    public ObservableCollection<HouseholdShareInput> Shares { get; } = [];
    /// <summary>Actual cash search results, bounded to an explicitly reviewed date and optional title.</summary>
    public ObservableCollection<HouseholdMovementChoice> Movements { get; } = [];
    /// <summary>Shared totals and private advances occupy separate rows.</summary>
    public ObservableCollection<HouseholdCostSummary> Summaries { get; } = [];
    /// <summary>Supported explicit ISO units, independent of the interface language.</summary>
    public IReadOnlyList<string> Currencies { get; } = Core.Money.Currencies.All.Select(row => row.Code).ToArray();
    /// <summary>Actual description, saved only after confirmation.</summary>
    [ObservableProperty] public partial string Title { get; set; } = "";
    /// <summary>Uncommitted exact amount in the selected currency.</summary>
    [ObservableProperty] public partial string AmountText { get; set; } = "";
    /// <summary>Explicit amount unit, never silently reinterpreted after a currency change.</summary>
    [ObservableProperty] public partial string CurrencyCode { get; set; } = "EUR";
    /// <summary>Always initialized actual date.</summary>
    [ObservableProperty] public partial DateOnly Date { get; set; } = DateOnly.FromDateTime(time.GetLocalNow().DateTime);
    /// <summary>True only for an explicitly reviewed refund of a saved expense.</summary>
    [ObservableProperty] public partial bool IsRefund { get; set; }
    /// <summary>Whether this actual cost links to an existing cash entry instead of a private payer.</summary>
    [ObservableProperty] public partial bool UsesSharedCash { get; set; }
    /// <summary>Cost editor visibility; opening it never saves.</summary>
    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasEditor))] public partial bool EditingCost { get; set; }
    /// <summary>Repayment editor visibility; this does not create a transfer.</summary>
    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasEditor))] public partial bool EditingSettlement { get; set; }
    /// <summary>Exact private payer or repayment sender.</summary>
    [ObservableProperty] public partial HouseholdContactChoice? Payer { get; set; }
    /// <summary>Distinct actual repayment recipient.</summary>
    [ObservableProperty] public partial HouseholdContactChoice? Recipient { get; set; }
    /// <summary>Explicit original refund cost.</summary>
    [ObservableProperty] public partial HouseholdCostItem? RefundOriginal { get; set; }
    /// <summary>Explicit existing cash movement; never the first search result selected automatically.</summary>
    [ObservableProperty] public partial HouseholdMovementChoice? Movement { get; set; }
    /// <summary>Explicit destination side for an existing transfer; its real currency/amount still require exact validation.</summary>
    [ObservableProperty] public partial bool UsesDestinationAmount { get; set; }
    /// <summary>Optional encrypted actual note.</summary>
    [ObservableProperty] public partial string Note { get; set; } = "";
    /// <summary>Explicit local search filter over cash descriptions.</summary>
    [ObservableProperty] public partial string MovementSearch { get; set; } = "";
    /// <summary>Field-local title validation.</summary>
    [ObservableProperty] public partial string? TitleError { get; set; }
    /// <summary>Field-local amount validation.</summary>
    [ObservableProperty] public partial string? AmountError { get; set; }
    /// <summary>Field-local participant/reference validation.</summary>
    [ObservableProperty] public partial string? ReferenceError { get; set; }
    /// <summary>Field-local exact allocation-sum feedback.</summary>
    [ObservableProperty] public partial string? SharesError { get; set; }

    /// <summary>Starts a covered local read only after native appearance.</summary>
    public async Task ActivateAsync()
    {
        visible?.Cancel(); visible?.Dispose(); visible = new(); ready = false;
        translator.PropertyChanged -= TranslationChanged; translator.PropertyChanged += TranslationChanged;
        await ReloadAsync();
    }
    /// <summary>Retires drafts and stale completions without an implicit save.</summary>
    public void Deactivate()
    {
        visible?.Cancel(); visible?.Dispose(); visible = null; review = null; ready = false; needsRefresh = true;
        translator.PropertyChanged -= TranslationChanged; ClearEditor(); Costs.Clear(); Settlements.Clear(); Summaries.Clear(); Contacts.Clear(); Refresh();
    }
    [RelayCommand] private Task ReloadAsync() => RunAsync(async (access, ct) =>
    {
        if ((EditingCost || EditingSettlement) && !await Confirm("HouseholdCost_Discard")) return;
        await ReadAsync(access, ct); ClearEditor();
    }, allowCovered: true);
    [RelayCommand] private Task NewCostAsync() => RunAsync(async (access, _) =>
    { if (review?.CanWrite == true && (!HasEditor || await Confirm("HouseholdCost_Discard"))) { access.Validate(); StartCost(null); } });
    [RelayCommand] private Task EditCostAsync(HouseholdCostItem item) => RunAsync(async (access, _) =>
    { if (Costs.Contains(item) && (!HasEditor || await Confirm("HouseholdCost_Discard"))) { access.Validate(); StartCost(item.Source); } });
    [RelayCommand] private Task NewSettlementAsync() => RunAsync(async (access, _) =>
    { if (review?.CanWrite == true && (!HasEditor || await Confirm("HouseholdCost_Discard"))) { access.Validate(); StartSettlement(null); } });
    [RelayCommand] private Task EditSettlementAsync(HouseholdSettlementItem item) => RunAsync(async (access, _) =>
    { if (Settlements.Contains(item) && (!HasEditor || await Confirm("HouseholdCost_Discard"))) { access.Validate(); StartSettlement(item.Source); } });
    [RelayCommand] private void CancelEdit() { if (CanInteract) { ClearEditor(); Refresh(); } }
    [RelayCommand] private Task AddContactAsync() => RunAsync(async (access, ct) =>
    {
        if (review?.CanWrite != true) return;
        var name = await interaction.PromptAsync(translator["HouseholdCost_Contacts"], translator["HouseholdCost_ContactName"],
            translator["Common_Save"], translator["Common_Cancel"], maxLength: 100);
        if (string.IsNullOrWhiteSpace(name)) return; access.Validate();
        if ((EditingCost || EditingSettlement) && !await Confirm("HouseholdCost_Discard")) return;
        access.Validate(); await store.SaveContactAsync(review, new() { Name = name.Trim() }, ct); AcceptEditor(); await ReadAsync(access, ct);
    });
    [RelayCommand] private Task ManageContactAsync(HouseholdContactChoice item) => RunAsync(async (access, ct) =>
    {
        if (review?.CanWrite != true || !Contacts.Contains(item)) return;
        var choice = await interaction.ChooseAsync(translator["HouseholdCost_Contacts"], translator["Common_Cancel"],
            translator["HouseholdCost_Rename"], translator[item.Source.IsArchived ? "HouseholdCost_Reactivate" : "HouseholdCost_Archive"]);
        if (choice is null || choice == translator["Common_Cancel"]) return; access.Validate();
        var name = item.Source.Name; var archived = item.Source.IsArchived;
        if (choice == translator["HouseholdCost_Rename"])
        {
            var text = await interaction.PromptAsync(translator["HouseholdCost_Contacts"], translator["HouseholdCost_ContactName"],
                translator["Common_Save"], translator["Common_Cancel"], name, 100);
            if (string.IsNullOrWhiteSpace(text)) return; name = text.Trim();
        }
        else archived = !archived;
        if ((EditingCost || EditingSettlement) && !await Confirm("HouseholdCost_Discard")) return;
        access.Validate(); await store.SaveContactAsync(review, new(item.Source.Id) { Name = name, IdentityId = item.Source.IdentityId, IsArchived = archived }, ct);
        AcceptEditor(); await ReadAsync(access, ct);
    });
    [RelayCommand] private Task FindMovementsAsync() => RunAsync(async (access, ct) =>
    {
        var selectedDate = Date; var kind = EditingSettlement ? EntryKind.Transfer : IsRefund ? EntryKind.Refund : EntryKind.Expense;
        var rows = await store.FindMovementsAsync(selectedDate, kind, MovementSearch, ct); access.Validate();
        Movements.Clear(); Movement = null;
        foreach (var row in rows) Movements.Add(new(row, MovementCaption(row)));
    });
    [RelayCommand] private Task SaveAsync() => RunAsync(async (access, ct) =>
    {
        if (review?.CanWrite != true || !EditingCost && !EditingSettlement) return;
        TitleError = AmountError = ReferenceError = SharesError = null;
        foreach (var row in Shares) row.Error = null;
        var valid = true; var amount = 0L;
        if (!TryReadOriginalAmount(out amount))
        { valid = false; }
        if (EditingCost && string.IsNullOrWhiteSpace(Title)) { TitleError = translator["HouseholdCost_TitleRequired"]; valid = false; }
        if (Payer is null && (!UsesSharedCash || EditingSettlement) || EditingSettlement && (Recipient is null || Recipient.Source.Id == Payer?.Source.Id) ||
            EditingCost && IsRefund && RefundOriginal is null || UsesSharedCash && Movement is null)
        { ReferenceError = translator["HouseholdCost_ReferencesRequired"]; valid = false; }
        var costId = editingCost ?? Guid.CreateVersion7(); var allocations = new List<HouseholdCostAllocation>();
        if (EditingCost && Core.Money.Currencies.TryGet(CurrencyCode, out _))
        {
            foreach (var row in Shares.Where(row => !string.IsNullOrWhiteSpace(row.Text)))
            {
                if (!row.TryRead(CurrencyCode, out var share))
                { row.Error = row.IsCurrent(CurrencyCode) ? translator["Amount_Invalid"]
                    : translator.Format("Plan_InputUnitChanged", MoneyText.UnitName(CurrencyCode)); valid = false; }
                else allocations.Add(new() { CostId = costId, ContactId = row.Contact.Id, Amount = share });
            }
            if (allocations.Count == 0 || allocations.Sum(row => (decimal)row.Amount) != amount)
            { SharesError = translator["HouseholdCost_SumRequired"]; valid = false; }
        }
        if (!valid) return;
        var reviewed = review; var stamp = EditorStamp(); var currencyCode = CurrencyCode; var isCost = EditingCost;
        var cost = isCost ? new HouseholdCost(costId) { Title = Title.Trim(), Date = Date, Amount = amount, CurrencyCode = currencyCode,
                Kind = IsRefund ? HouseholdCostKind.Refund : HouseholdCostKind.Expense, PaymentKind = UsesSharedCash ? HouseholdPaymentKind.SharedAccount : HouseholdPaymentKind.Private,
                PaidByContactId = UsesSharedCash ? null : Payer!.Source.Id, EntryId = UsesSharedCash ? Movement!.Source.Id : null,
                RefundOfCostId = IsRefund ? RefundOriginal!.Source.Id : null, Note = Empty(Note) } : null;
        var settlement = !isCost ? new HouseholdSettlement(editingSettlement ?? Guid.CreateVersion7()) { FromContactId = Payer!.Source.Id, ToContactId = Recipient!.Source.Id,
                Date = Date, Amount = amount, CurrencyCode = currencyCode, EntryId = UsesSharedCash ? Movement?.Source.Id : null,
                UsesDestinationAmount = UsesSharedCash && UsesDestinationAmount, Note = Empty(Note) } : null;
        bool Current() => visible is not null && !ct.IsCancellationRequested && ReferenceEquals(review, reviewed)
            && stamp == EditorStamp() && !_amountInput.NeedsReview(currencyCode) && Shares.All(row => row.IsCurrent(currencyCode));
        if (!await Confirm("HouseholdCost_SaveConfirm")) return; access.Validate();
        if (!Current()) throw new InvalidOperationException("The original household financial draft changed during consent.");
        if (isCost) await store.SaveCostAsync(reviewed, cost!, allocations, ct, Current);
        else await store.SaveSettlementAsync(reviewed, settlement!, ct, Current);
        AcceptEditor();
        access.Validate(); await environment.PublishAppliedAsync(ct); await ReadAsync(access, ct); ClearEditor();
    });
    [RelayCommand] private Task DeleteAsync() => RunAsync(async (access, ct) =>
    {
        if (review?.CanWrite != true || editingCost is null && editingSettlement is null || !await Confirm("HouseholdCost_DeleteConfirm")) return;
        access.Validate();
        if (editingCost is { } cost) await store.DeleteCostAsync(review, cost, ct);
        else await store.DeleteSettlementAsync(review, editingSettlement!.Value, ct);
        AcceptEditor();
        access.Validate(); await environment.PublishAppliedAsync(ct); await ReadAsync(access, ct); ClearEditor();
    });

    private void StartCost(HouseholdCost? row)
    {
        ClearEditor(); editingCost = row?.Id; EditingCost = true; inputCulture = localization.CurrentCulture;
        Title = row?.Title ?? ""; CurrencyCode = row?.CurrencyCode ?? "EUR"; Date = row?.Date ?? DateOnly.FromDateTime(time.GetLocalNow().DateTime);
        AmountText = row is null ? "" : MoneyText.ForInput(row.Amount, row.CurrencyCode, inputCulture); IsRefund = row?.Kind == HouseholdCostKind.Refund;
        UsesSharedCash = row?.PaymentKind == HouseholdPaymentKind.SharedAccount; Payer = Contacts.SingleOrDefault(item => item.Source.Id == row?.PaidByContactId);
        RefundOriginal = OriginalCosts.SingleOrDefault(item => item.Source.Id == row?.RefundOfCostId); Note = row?.Note ?? "";
        foreach (var contact in review!.Contacts.Where(contact => !contact.IsArchived || review.Allocations.Any(share => share.CostId == row?.Id && share.ContactId == contact.Id)))
        {
            var share = review.Allocations.SingleOrDefault(share => share.CostId == row?.Id && share.ContactId == contact.Id);
            Shares.Add(new(contact, inputCulture, CurrencyCode, () => localization.CurrentCulture, translator)
                { Text = share is null ? "" : MoneyText.ForInput(share.Amount, CurrencyCode, inputCulture) });
        }
        SelectOriginalMovement(row?.EntryId); Refresh();
    }
    private void StartSettlement(HouseholdSettlement? row)
    {
        ClearEditor(); editingSettlement = row?.Id; EditingSettlement = true; inputCulture = localization.CurrentCulture;
        CurrencyCode = row?.CurrencyCode ?? "EUR"; Date = row?.Date ?? DateOnly.FromDateTime(time.GetLocalNow().DateTime);
        AmountText = row is null ? "" : MoneyText.ForInput(row.Amount, CurrencyCode, inputCulture); Note = row?.Note ?? "";
        Payer = Contacts.SingleOrDefault(item => item.Source.Id == row?.FromContactId); Recipient = Contacts.SingleOrDefault(item => item.Source.Id == row?.ToContactId);
        UsesSharedCash = row?.EntryId is not null; UsesDestinationAmount = row?.UsesDestinationAmount ?? false;
        SelectOriginalMovement(row?.EntryId); Refresh();
    }
    private async Task ReadAsync(PersonalSyncAccess access, CancellationToken ct)
    { var next = await store.ReadAsync(ct); access.Validate(); ct.ThrowIfCancellationRequested(); review = next; ready = true; needsRefresh = false; Publish(); }
    private void SelectOriginalMovement(Guid? id)
    {
        if (id is null) return;
        var row = review!.LinkedMovements.Single(item => item.Id == id);
        var choice = new HouseholdMovementChoice(row, MovementCaption(row));
        Movements.Add(choice); Movement = choice;
    }
    private string MovementCaption(HouseholdCashMovement row)
    {
        var text = translator.Format("HouseholdCost_Movement", row.Title ?? translator["HouseholdCost_Unnamed"],
            NativeDigits.Apply(MoneyText.Format(row.Amount, row.CurrencyCode, localization.CurrentCulture)));
        var destination = row.DestinationAmount ?? (row.DestinationCurrencyCode == row.CurrencyCode ? row.Amount : (long?)null);
        if (destination is { } amount && row.DestinationCurrencyCode is { } currency)
            text += " Â· " + translator.Format("HouseholdCost_DestinationValue", NativeDigits.Apply(MoneyText.Format(amount, currency, localization.CurrentCulture)));
        return text;
    }
    partial void OnCurrencyCodeChanged(string value)
    {
        // A currency change is a new amount decision, never a relabel of retained numeric drafts.
        _amountInput.Initialize("", value, localization.CurrentCulture); AmountText = "";
        foreach (var row in Shares) row.ResetCurrency(value); Movement = null; Movements.Clear(); RefreshMoneyTexts();
    }
    partial void OnDateChanged(DateOnly value) { Movement = null; Movements.Clear(); }
    partial void OnIsRefundChanged(bool value) { Movement = null; Movements.Clear(); }
    partial void OnUsesSharedCashChanged(bool value) { Movement = null; Movements.Clear(); }
    private void Publish()
    {
        if (review is null) return;
        var payerId = Payer?.Source.Id; var recipientId = Recipient?.Source.Id; var originalId = RefundOriginal?.Source.Id;
        Contacts.Clear(); foreach (var row in review.Contacts) Contacts.Add(new(row, row.IsArchived ? translator.Format("HouseholdCost_ArchivedContact", row.Name) : row.Name));
        Costs.Clear(); OriginalCosts.Clear();
        foreach (var row in review.Costs.OrderByDescending(row => row.Date).ThenBy(row => row.Id))
        {
            var item = new HouseholdCostItem(row, translator.Format("HouseholdCost_RecordCaption", row.Title, dates.Format(row.Date)),
                NativeDigits.Apply(MoneyText.Format(row.Kind == HouseholdCostKind.Refund ? -row.Amount : row.Amount, row.CurrencyCode, localization.CurrentCulture))!);
            Costs.Add(item); if (row.Kind == HouseholdCostKind.Expense) OriginalCosts.Add(item);
        }
        Settlements.Clear(); foreach (var row in review.Settlements.OrderByDescending(row => row.Date))
            Settlements.Add(new(row, translator.Format("HouseholdCost_FromTo", Name(row.FromContactId), Name(row.ToContactId)), MoneyText.Format(row.Amount, row.CurrencyCode, localization.CurrentCulture)));
        Summaries.Clear();
        foreach (var row in review.Totals)
        {
            Summaries.Add(new(translator["HouseholdCost_NetTotal"], MoneyText.Format(row.NetCost, row.CurrencyCode, localization.CurrentCulture)));
            Summaries.Add(new(translator["HouseholdCost_SharedPaid"], MoneyText.Format(row.PaidFromSharedAccounts, row.CurrencyCode, localization.CurrentCulture)));
            Summaries.Add(new(translator["HouseholdCost_PrivatePaid"], MoneyText.Format(row.PaidPrivately, row.CurrencyCode, localization.CurrentCulture)));
        }
        foreach (var row in review.Members)
        {
            Summaries.Add(new(translator.Format("HouseholdCost_Attributed", Name(row.ContactId)), MoneyText.Format(row.AttributedNetCost, row.CurrencyCode, localization.CurrentCulture)));
            Summaries.Add(new(translator.Format("HouseholdCost_Advance", Name(row.ContactId)), MoneyText.Format(row.PrivateAdvanceBalance, row.CurrencyCode, localization.CurrentCulture)));
        }
        Payer = Contacts.SingleOrDefault(item => item.Source.Id == payerId); Recipient = Contacts.SingleOrDefault(item => item.Source.Id == recipientId);
        RefundOriginal = OriginalCosts.SingleOrDefault(item => item.Source.Id == originalId);
        string Name(Guid id) => review.Contacts.Single(item => item.Id == id).Name;
    }
    private async Task RunAsync(Func<PersonalSyncAccess, CancellationToken, Task> action, bool allowCovered = false)
    {
        if (visible is null || IsBusy || (!ready || needsRefresh) && !allowCovered) return;
        IsBusy = true; errorKey = null; committedInAction = false; Refresh();
        try
        {
            var access = environment.Capture(); using var linked = CancellationTokenSource.CreateLinkedTokenSource(access.Cancellation, visible.Token);
            await action(access, linked.Token); access.Validate();
        }
        catch (OperationCanceledException) { if (committedInAction) Failed("HouseholdCost_ActionFailed"); }
        catch (HouseholdCostRejectedException) { Failed("HouseholdCost_InvalidGraph"); }
        catch (HouseholdWriteRejectedException) { Failed("HouseholdCost_ReadOnly"); }
        catch (Exception error) when (error is not OutOfMemoryException) { Failed("HouseholdCost_ActionFailed"); }
        finally { IsBusy = false; Refresh(); }
    }
    private Task<bool> Confirm(string key) => interaction.ConfirmAsync(translator["HouseholdCost_Title"], translator[key], translator["Common_Save"], translator["Common_Cancel"]);
    private static string? Empty(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private void ClearEditor()
    {
        editingCost = editingSettlement = null; EditingCost = EditingSettlement = false; Title = AmountText = Note = MovementSearch = "";
        Payer = Recipient = null; RefundOriginal = null; Movement = null; Shares.Clear(); Movements.Clear();
        TitleError = AmountError = ReferenceError = SharesError = null;
    }
    private void TranslationChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs args) { Publish(); RefreshMoneyTexts(); Refresh(); }
    private void Refresh()
    { foreach (var property in new[] { nameof(IsReady), nameof(CanWrite), nameof(CanInteract), nameof(CanLeave), nameof(CanDelete), nameof(Error) }) OnPropertyChanged(property); }
}
