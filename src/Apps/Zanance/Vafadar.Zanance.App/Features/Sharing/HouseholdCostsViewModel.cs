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

namespace Vafadar.Zanance.App.Features.Sharing;

/// <summary>One exact contact choice; a financial label is never service membership authority.</summary>
public sealed record HouseholdContactChoice(HouseholdContact Source, string Caption);
/// <summary>One original actual cost and its complete direction-safe display packet.</summary>
public sealed record HouseholdCostItem(HouseholdCost Source, string Caption, string Amount)
{
    /// <summary>Retains date, original description and complete unit for an unambiguous refund-source choice.</summary>
    public string ChoiceCaption => Caption + " · " + Amount;
}
/// <summary>One original repayment with its complete direction-safe value.</summary>
public sealed record HouseholdSettlementItem(HouseholdSettlement Source, string Caption, string Amount);
/// <summary>Only explicit saved cash movements are offered for linkage.</summary>
public sealed record HouseholdMovementChoice(HouseholdCashMovement Source, string Caption);
/// <summary>Independent per-currency summary; the value is never added to the cash ledger again.</summary>
public sealed record HouseholdCostSummary(string Caption, string Amount);
/// <summary>One explicit minor-unit allocation draft, preserving its original input culture through display changes.</summary>
public sealed partial class HouseholdShareInput(HouseholdContact contact, CultureInfo culture) : ObservableObject
{
    /// <summary>Exact original financial contact.</summary>
    public HouseholdContact Contact { get; } = contact;
    internal CultureInfo InputCulture { get; } = culture;
    /// <summary>Uncommitted user amount; blank means no allocation.</summary>
    [ObservableProperty] public partial string Text { get; set; } = "";
    /// <summary>Field-local exact allocation error.</summary>
    [ObservableProperty] public partial string? Error { get; set; }
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
    /// <summary>Covered publication only after the complete coherent actual native read.</summary>
    public bool IsReady => ready;
    /// <summary>Current read-only hint; Save still rechecks actual role/file/clock under the writer.</summary>
    public bool CanWrite => ready && visible is not null && !IsBusy && review?.CanWrite == true;
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
        visible?.Cancel(); visible?.Dispose(); visible = null; review = null; ready = false;
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
        access.Validate(); await store.SaveContactAsync(review, new() { Name = name.Trim() }, ct); await ReadAsync(access, ct); ClearEditor();
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
        await ReadAsync(access, ct); ClearEditor();
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
        if (!Core.Money.Currencies.TryGet(CurrencyCode, out var currency) || !MoneyText.TryParse(AmountText, currency, inputCulture, out amount) || amount <= 0)
        { AmountError = translator["Amount_Invalid"]; valid = false; }
        if (EditingCost && string.IsNullOrWhiteSpace(Title)) { TitleError = translator["HouseholdCost_TitleRequired"]; valid = false; }
        if (Payer is null && (!UsesSharedCash || EditingSettlement) || EditingSettlement && (Recipient is null || Recipient.Source.Id == Payer?.Source.Id) ||
            EditingCost && IsRefund && RefundOriginal is null || UsesSharedCash && Movement is null)
        { ReferenceError = translator["HouseholdCost_ReferencesRequired"]; valid = false; }
        var costId = editingCost ?? Guid.CreateVersion7(); var allocations = new List<HouseholdCostAllocation>();
        if (EditingCost && currency is not null)
        {
            foreach (var row in Shares.Where(row => !string.IsNullOrWhiteSpace(row.Text)))
            {
                if (!MoneyText.TryParse(row.Text, currency, row.InputCulture, out var share) || share < 0)
                { row.Error = translator["Amount_Invalid"]; valid = false; }
                else allocations.Add(new() { CostId = costId, ContactId = row.Contact.Id, Amount = share });
            }
            if (allocations.Count == 0 || allocations.Sum(row => (decimal)row.Amount) != amount)
            { SharesError = translator["HouseholdCost_SumRequired"]; valid = false; }
        }
        if (!valid) return;
        if (!await Confirm("HouseholdCost_SaveConfirm")) return; access.Validate();
        if (EditingCost)
            await store.SaveCostAsync(review, new(costId) { Title = Title.Trim(), Date = Date, Amount = amount, CurrencyCode = CurrencyCode,
                Kind = IsRefund ? HouseholdCostKind.Refund : HouseholdCostKind.Expense, PaymentKind = UsesSharedCash ? HouseholdPaymentKind.SharedAccount : HouseholdPaymentKind.Private,
                PaidByContactId = UsesSharedCash ? null : Payer!.Source.Id, EntryId = UsesSharedCash ? Movement!.Source.Id : null,
                RefundOfCostId = IsRefund ? RefundOriginal!.Source.Id : null, Note = Empty(Note) }, allocations, ct);
        else
            await store.SaveSettlementAsync(review, new(editingSettlement ?? Guid.CreateVersion7()) { FromContactId = Payer!.Source.Id, ToContactId = Recipient!.Source.Id,
                Date = Date, Amount = amount, CurrencyCode = CurrencyCode, EntryId = UsesSharedCash ? Movement?.Source.Id : null,
                UsesDestinationAmount = UsesSharedCash && UsesDestinationAmount, Note = Empty(Note) }, ct);
        access.Validate(); await environment.PublishAppliedAsync(ct); await ReadAsync(access, ct); ClearEditor();
    });
    [RelayCommand] private Task DeleteAsync() => RunAsync(async (access, ct) =>
    {
        if (review?.CanWrite != true || editingCost is null && editingSettlement is null || !await Confirm("HouseholdCost_DeleteConfirm")) return;
        access.Validate();
        if (editingCost is { } cost) await store.DeleteCostAsync(review, cost, ct);
        else await store.DeleteSettlementAsync(review, editingSettlement!.Value, ct);
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
            Shares.Add(new(contact, inputCulture) { Text = share is null ? "" : MoneyText.ForInput(share.Amount, CurrencyCode, inputCulture) });
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
    { var next = await store.ReadAsync(ct); access.Validate(); ct.ThrowIfCancellationRequested(); review = next; ready = true; Publish(); }
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
            text += " · " + translator.Format("HouseholdCost_DestinationValue", NativeDigits.Apply(MoneyText.Format(amount, currency, localization.CurrentCulture)));
        return text;
    }
    partial void OnCurrencyCodeChanged(string value)
    {
        // A currency change is a new amount decision, never a relabel of retained numeric drafts.
        AmountText = ""; foreach (var row in Shares) row.Text = ""; Movement = null; Movements.Clear();
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
        if (visible is null || IsBusy || !ready && !allowCovered) return;
        IsBusy = true; errorKey = null; Refresh();
        try
        {
            var access = environment.Capture(); using var linked = CancellationTokenSource.CreateLinkedTokenSource(access.Cancellation, visible.Token);
            await action(access, linked.Token); access.Validate();
        }
        catch (OperationCanceledException) { }
        catch (HouseholdCostRejectedException) { errorKey = "HouseholdCost_InvalidGraph"; }
        catch (HouseholdWriteRejectedException) { errorKey = "HouseholdCost_ReadOnly"; }
        catch (Exception error) when (error is not OutOfMemoryException) { errorKey = "HouseholdCost_ActionFailed"; }
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
    private void TranslationChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs args) { Publish(); Refresh(); }
    private void Refresh()
    { foreach (var property in new[] { nameof(IsReady), nameof(CanWrite), nameof(CanInteract), nameof(CanLeave), nameof(CanDelete), nameof(Error) }) OnPropertyChanged(property); }
}
