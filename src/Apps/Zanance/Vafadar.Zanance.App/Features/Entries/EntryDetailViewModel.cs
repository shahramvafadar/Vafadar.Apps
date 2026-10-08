using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentIcons.Common;
using Vafadar.Documents.Maui;
using Vafadar.Localization.Formatting;
using Vafadar.Localization;
using Vafadar.Maui.Mvvm;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Features.Entries;

/// <summary>An attachment in the entry details; images get a preview.</summary>
public sealed record AttachmentRow(Guid Id, string Name, string Details, ImageSource? Preview)
{
    public bool HasPreview => Preview is not null;

    /// <summary>Gets a value indicating whether the receipt can be read on this device (a photo or PDF of an expense, D-31, D-33).</summary>
    public bool CanRead { get; init; }
}

/// <summary>A labelled value in the entry details.</summary>
/// <param name="Label">The label; <see langword="null"/> for a line without one (e.g. "From a plan").</param>
/// <param name="Value">The value.</param>
public sealed record DetailLine(string? Label, string Value);

/// <summary>Details of one entry with refund, duplicate, edit and delete (UI-04).</summary>
public sealed partial class EntryDetailViewModel(
    ZananceStore store,
    Translator translator,
    ILocalizationService localization,
    IDateFormatter dates,
    UndoService undo,
    HoldingStore holdings,
    TimeProvider time) : ViewModelBase, IQueryAttributable, Presentation.IThemeAware
{
    private Guid _id;
    private Guid? _holdingTypeId;
    private LedgerEntry? _entry;

    public ObservableCollection<DetailLine> Lines { get; } = [];

    public ObservableCollection<EntryRow> Refunds { get; } = [];

    // Receipts and documents (F2-TX-04).
    public ObservableCollection<AttachmentRow> Attachments { get; } = [];

    [ObservableProperty]
    public partial bool HasAttachments { get; set; }

    [ObservableProperty]
    public partial string? Heading { get; set; }

    [ObservableProperty]
    public partial string? KindText { get; set; }

    [ObservableProperty]
    public partial string? AmountText { get; set; }

    [ObservableProperty]
    public partial Color? AmountColor { get; set; }

    [ObservableProperty]
    public partial Symbol Icon { get; set; }

    [ObservableProperty]
    public partial Color? IconColor { get; set; }

    [ObservableProperty]
    public partial Color? IconBackground { get; set; }

    [ObservableProperty]
    public partial bool CanRefund { get; set; }

    [ObservableProperty]
    public partial bool CanPayBack { get; set; }

    [ObservableProperty]
    public partial bool CanMakeRecurring { get; set; }

    [ObservableProperty]
    public partial bool CanSaveTemplate { get; set; }

    // Split across categories (F2-TX-01): offered for a single entry, and as "Edit split" for a part of one.
    [ObservableProperty]
    public partial bool CanSplit { get; set; }

    /// <summary>Gets a value indicating whether the entry is the money (or fee) of a holding purchase or sale.</summary>
    [ObservableProperty]
    public partial bool IsHoldingMoney { get; set; }

    // Reimbursable expense (F2-TX-03): what is still to be paid back.
    [ObservableProperty]
    public partial string? ReimbursementText { get; set; }

    [ObservableProperty]
    public partial bool CanRecordReimbursement { get; set; }

    // "Always use this category for …" creates a categorization rule from this entry (F2-TX-04).
    [ObservableProperty]
    public partial string? RuleActionText { get; set; }

    private string? _ruleMatch;

    [ObservableProperty]
    public partial string? SplitText { get; set; }

    [ObservableProperty]
    public partial string? SplitActionText { get; set; }

    [ObservableProperty]
    public partial bool IsUnreviewed { get; set; }

    [ObservableProperty]
    public partial bool HasRefunds { get; set; }

    [ObservableProperty]
    public partial string? RefundableText { get; set; }

    [ObservableProperty]
    public partial bool NotFound { get; set; }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.TryGetValue("id", out var value) && (value is Guid id || Guid.TryParse(value?.ToString(), out id)))
        {
            _id = id;
        }
    }

    // The page's colors are computed while loading; loading again keeps the filters and choices of the page.
    Task Presentation.IThemeAware.RefreshThemeAsync() => LoadAsync();

    public async Task LoadAsync()
    {
        _entry = await store.GetEntryAsync(_id);
        NotFound = _entry is null;
        if (_entry is null)
        {
            return;
        }

        var entry = _entry;
        var accounts = (await store.GetAccountsAsync()).ToDictionary(a => a.Id);
        var categories = new CategoryLookup(await store.GetCategoriesAsync(), translator);
        var culture = localization.CurrentCulture;
        var presenter = new EntryPresenter(accounts, categories, translator, culture);
        var row = presenter.Row(entry);

        Heading = row.Title;
        KindText = translator[$"EntryKind_{entry.Kind}"];
        AmountText = row.AmountText;
        AmountColor = row.AmountColor;
        Icon = row.Icon;
        IconColor = row.IconColor;
        IconBackground = row.IconBackground;
        IsUnreviewed = row.IsUnreviewed;
        CanPayBack = entry.Kind == EntryKind.Income;
        CanMakeRecurring = entry.Kind is EntryKind.Income or EntryKind.Expense or EntryKind.Transfer && entry.ScheduleId is null;
        CanSaveTemplate = entry.Kind is EntryKind.Income or EntryKind.Expense or EntryKind.Transfer;
        _ruleMatch = entry.Kind is EntryKind.Income or EntryKind.Expense && entry.CategoryId is { } ruleCategory
            && categories.Get(ruleCategory) is { SystemKey: not Core.Categories.DefaultCategories.Uncategorized }
                ? (entry.Payee ?? entry.Title)?.Trim()
                : null;
        RuleActionText = _ruleMatch is { Length: >= Core.Categories.CategoryRules.MinLength } match
            ? translator.Format("Rule_Always", match, categories.Name(entry.CategoryId))
            : null;
        var related = entry.GroupId is { } groupId ? await store.GetGroupAsync(groupId) : [entry];

        // The money of a holding purchase or sale (and its fee) is edited and deleted with the holding (ZEX-AS14).
        _holdingTypeId = entry.GroupId is { } holdingGroup ? await holdings.FindTypeOfGroupAsync(holdingGroup) : null;
        IsHoldingMoney = _holdingTypeId is not null;
        if (IsHoldingMoney)
        {
            CanPayBack = CanMakeRecurring = CanSaveTemplate = false;
            RuleActionText = null;
        }

        // Only refunds and paybacks of these entries matter for splitting; reading the whole ledger for them was slow.
        var paybacks = new List<LedgerEntry>();
        foreach (var part in related)
        {
            paybacks.AddRange(await store.GetRefundsAsync(part.Id));
        }

        CanSplit = !IsHoldingMoney && EntryActions.CanSplit(related) && !EntryActions.HasPaybacks(related, paybacks);
        var isSplit = EntryActions.IsSplit(related);
        SplitActionText = translator[isSplit ? "Split_Edit" : "Split_Action"];
        SplitText = isSplit
            ? translator.Format("Split_PartOf", MoneyText.Format(related.Sum(e => e.Amount), presenter.CurrencyOf(entry.AccountId), culture), related.Count)
            : null;

        Lines.Clear();
        Lines.Add(new DetailLine(translator["Entry_Date"], dates.Format(entry.Date, DateFormatStyle.Long)));
        if (entry.Kind == EntryKind.Transfer)
        {
            Lines.Add(new DetailLine(translator["Entry_FromAccount"], presenter.AccountName(entry.AccountId)));
            Lines.Add(new DetailLine(translator["Entry_ToAccount"], presenter.AccountName(entry.ToAccountId)));
            if (entry.ToAmount is { } toAmount && entry.ToAccountId is { } to)
            {
                Lines.Add(new DetailLine(translator.Format("Entry_ToAmount", presenter.CurrencyOf(to)), MoneyText.Format(toAmount, presenter.CurrencyOf(to), culture)));
            }

            if (EntryActions.FindTransferFee(entry, related) is { } fee)
            {
                Lines.Add(new DetailLine(translator["Entry_Fee"], MoneyText.Format(fee.Amount, presenter.CurrencyOf(fee.AccountId), culture)));
            }
        }
        else
        {
            Lines.Add(new DetailLine(translator["Entry_Account"], presenter.AccountName(entry.AccountId)));
            if (entry.Kind != EntryKind.Adjustment)
            {
                Lines.Add(new DetailLine(translator["Entry_Category"], categories.Name(entry.CategoryId)));
            }
        }

        if (entry.Kind == EntryKind.Refund && entry.RefundOfId is { } purchaseId && await store.GetEntryAsync(purchaseId) is { } purchase)
        {
            Lines.Add(new DetailLine(translator["EntryKind_Refund"], translator.Format("Entry_RefundOf", presenter.Title(purchase))));
        }

        if (!string.IsNullOrEmpty(entry.Payee))
        {
            Lines.Add(new DetailLine(translator["Entry_Payee"], entry.Payee));
        }

        if (entry.OriginalAmount is { } original && entry.OriginalCurrencyCode is { } originalCurrency)
        {
            Lines.Add(new DetailLine(translator["Entry_ForeignAmount"], MoneyText.Format(original, originalCurrency, culture)));
        }

        if (!string.IsNullOrEmpty(entry.Note))
        {
            Lines.Add(new DetailLine(translator["Entry_Note"], entry.Note));
        }

        if (entry.Tags.Count > 0)
        {
            Lines.Add(new DetailLine(translator["Entry_Tags"], string.Join("  ", entry.Tags.Select(EntryTags.Display))));
        }

        if (entry.Source == EntrySource.Schedule)
        {
            Lines.Add(new DetailLine(null, translator["Entry_FromPlan"]));
        }

        Refunds.Clear();
        if (entry.Kind == EntryKind.Expense)
        {
            var refunds = await store.GetRefundsAsync(entry.Id);
            foreach (var refund in refunds)
            {
                Refunds.Add(presenter.Row(refund) with { Subtitle = dates.Format(refund.Date, DateFormatStyle.Long) });
            }

            var refundable = EntryActions.Refundable(entry, refunds);
            if (entry.ReimbursableAmount is { } reimbursable)
            {
                var open = EntryActions.OpenReimbursement(entry, refunds);
                var currency = presenter.CurrencyOf(entry.AccountId);
                ReimbursementText = translator.Format(open > 0 ? "Entry_ReimbursementOpen" : "Entry_ReimbursementDone",
                    MoneyText.Format(reimbursable, currency, culture), entry.ReimbursedBy ?? translator["Entry_ReimbursedBySomeone"], MoneyText.Format(open, currency, culture));
                CanRecordReimbursement = open > 0 && accounts.ContainsKey(entry.AccountId);
            }
            else
            {
                ReimbursementText = null;
                CanRecordReimbursement = false;
            }

            CanRefund = refundable > 0 && accounts.ContainsKey(entry.AccountId);
            RefundableText = refunds.Count > 0 ? translator.Format("Entry_Refundable", MoneyText.Format(refundable, presenter.CurrencyOf(entry.AccountId), culture)) : null;
        }
        else
        {
            CanRefund = false;
            RefundableText = null;
        }

        HasRefunds = Refunds.Count > 0;
        await LoadAttachmentsAsync();
    }

    // On-device reading of a receipt photo or PDF (D-31, D-33). The found values open the editor for review; nothing is
    // changed until the user saves there.
    [RelayCommand]
    private async Task ReadReceiptAsync(AttachmentRow row)
    {
        if (IsBusy || await store.GetAttachmentAsync(row.Id) is not { } attachment)
        {
            return;
        }

        Vafadar.Documents.TextLayoutResult document;
        IsBusy = true;
        try
        {
            document = await DocumentReader.ReadDocumentAsync(attachment.Data, attachment.ContentType);
        }
        finally
        {
            IsBusy = false;
        }

        var receipt = Core.Receipts.ReceiptParser.Parse(document.Text, DateOnly.FromDateTime(time.GetLocalNow().DateTime), document.IsAmbiguous);
        if (receipt.IsEmpty)
        {
            await Shell.Current.DisplayAlertAsync(translator["Receipt_Title"], translator["Receipt_Nothing"], translator["Common_Ok"]);
            return;
        }

        var query = new Dictionary<string, object> { ["id"] = _id, ["receipt"] = receipt };

        await Shell.Current.GoToAsync(AppShell.EntryEditorRoute, query);
    }

    private async Task LoadAttachmentsAsync()
    {
        Attachments.Clear();
        foreach (var info in await store.GetAttachmentsAsync(_id))
        {
            ImageSource? preview = null;
            if (info.IsImage && await store.GetAttachmentAsync(info.Id) is { } image)
            {
                var bytes = image.Data;
                preview = ImageSource.FromStream(() => new MemoryStream(bytes));
            }

            // Size and date form one left-to-right run; isolates and marks keep the order in right-to-left layouts.
            var size = info.Size >= 1024 * 1024 ? $"{info.Size / 1024d / 1024d:0.0} MB" : $"{Math.Max(1, info.Size / 1024)} KB";
            Attachments.Add(new AttachmentRow(info.Id, info.FileName, $"\u2066\u200E{size} · {dates.Format(DateOnly.FromDateTime(info.CreatedAt.LocalDateTime), DateFormatStyle.Short)}\u200E\u2069", preview)
            {
                CanRead = _entry?.Kind == EntryKind.Expense && DocumentReader.CanRead(info.ContentType),
            });
        }

        HasAttachments = Attachments.Count > 0;
    }

    [RelayCommand]
    private async Task AddAttachmentAsync()
    {
        try
        {
            if (await AttachmentFiles.PickAsync(translator["Attachment_Pick"]) is not { } picked)
            {
                return;
            }

            if (picked.Data.Length == 0)
            {
                // E.g. a cloud file that is not downloaded yet.
                await Shell.Current.DisplayAlertAsync(translator["Attachment_Title"], translator["Attachment_Failed"], translator["Common_Ok"]);
                return;
            }

            if (picked.Data.Length > EntryAttachment.MaxBytes)
            {
                await Shell.Current.DisplayAlertAsync(translator["Attachment_Title"], translator["Attachment_TooLarge"], translator["Common_Ok"]);
                return;
            }

            await store.AddAttachmentAsync(new EntryAttachment { EntryId = _id, FileName = picked.Name, ContentType = picked.ContentType, Data = picked.Data });
            await LoadAttachmentsAsync();
        }
        catch (InvalidDataException)
        {
            await Shell.Current.DisplayAlertAsync(translator["Attachment_Title"], translator["Attachment_PhotoFailed"], translator["Common_Ok"]);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or PermissionException or ArgumentException or InvalidOperationException)
        {
            // Also an entry deleted meanwhile, or a file the store rejects.
            await Shell.Current.DisplayAlertAsync(translator["Attachment_Title"], translator["Attachment_Failed"], translator["Common_Ok"]);
        }
    }

    [RelayCommand]
    private async Task OpenAttachmentAsync(AttachmentRow row)
    {
        if (await store.GetAttachmentAsync(row.Id) is not { } attachment)
        {
            return;
        }

        // A device without an app for the file type (e.g. no PDF viewer) says so instead of doing nothing or failing.
        bool opened;
        try
        {
            opened = await AttachmentFiles.OpenAsync(attachment);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException or NotSupportedException)
        {
            opened = false;
        }

        if (!opened)
        {
            await Shell.Current.DisplayAlertAsync(translator["Attachment_Title"], translator["Attachment_OpenFailed"], translator["Common_Ok"]);
        }
    }

    [RelayCommand]
    private async Task DeleteAttachmentAsync(AttachmentRow row)
    {
        if (await Shell.Current.DisplayAlertAsync(translator["Attachment_Delete"], row.Name, translator["Common_Delete"], translator["Common_Cancel"]))
        {
            await store.DeleteAttachmentAsync(row.Id);
            await LoadAttachmentsAsync();
        }
    }

    [RelayCommand]
    private Task EditAsync() => _holdingTypeId is { } holding
        ? Shell.Current.GoToAsync(AppShell.HoldingDetailRoute, new Dictionary<string, object> { ["id"] = holding })
        : Shell.Current.GoToAsync(AppShell.EntryEditorRoute, new Dictionary<string, object> { ["id"] = _id });

    [RelayCommand]
    private Task RecordReimbursementAsync() => Shell.Current.GoToAsync(AppShell.EntryEditorRoute, new Dictionary<string, object> { ["refundOf"] = _id, ["reimburse"] = true });

    [RelayCommand]
    private Task RefundAsync() => Shell.Current.GoToAsync(AppShell.EntryEditorRoute, new Dictionary<string, object> { ["refundOf"] = _id });

    [RelayCommand]
    private Task DuplicateAsync() => Shell.Current.GoToAsync(AppShell.EntryEditorRoute, new Dictionary<string, object> { ["duplicate"] = _id });

    [RelayCommand]
    private async Task SaveRuleAsync()
    {
        if (_entry is not { CategoryId: { } categoryId } || _ruleMatch is null)
        {
            return;
        }

        await store.SaveCategoryRuleAsync(new Core.Categories.CategoryRule
        {
            Match = _ruleMatch,
            CategoryId = categoryId,
            Kind = _entry.Kind == EntryKind.Income ? Core.Categories.CategoryKind.Income : Core.Categories.CategoryKind.Expense,
        });
        await Shell.Current.DisplayAlertAsync(translator["Rules_Title"], translator.Format("Rule_Saved", _ruleMatch), translator["Common_Ok"]);
    }

    [RelayCommand]
    private Task SplitAsync() => Shell.Current.GoToAsync(AppShell.SplitRoute, new Dictionary<string, object> { ["id"] = _id });

    [RelayCommand]
    private Task PayBackAsync() => Shell.Current.GoToAsync(AppShell.EntryEditorRoute, new Dictionary<string, object> { ["kind"] = nameof(EntryKind.IncomeReversal), ["of"] = _id });

    // TX-04: a quick template fills the entry form later; it never creates an entry by itself.
    [RelayCommand]
    private async Task SaveAsTemplateAsync()
    {
        if (_entry is null || !CanSaveTemplate)
        {
            return;
        }

        var name = await Shell.Current.DisplayPromptAsync(
            translator["Templates_SaveTitle"], translator["Templates_NamePrompt"], translator["Common_Save"], translator["Common_Cancel"],
            initialValue: Heading ?? string.Empty, maxLength: 60);
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        var keepAmount = _entry.Amount > 0 && await Shell.Current.DisplayAlertAsync(
            translator["Templates_SaveTitle"], translator.Format("Templates_KeepAmount", AmountText ?? string.Empty),
            translator["Templates_KeepAmountYes"], translator["Templates_KeepAmountNo"]);
        await store.SaveTemplateAsync(EntryTemplate.From(_entry, name, keepAmount));
        await Shell.Current.DisplayAlertAsync(translator["Templates_SaveTitle"], translator.Format("Templates_Saved", name.Trim()), translator["Common_Ok"]);
    }

    // TX-04: an entry becomes the template of a plan; the entry itself stays as it is.
    [RelayCommand]
    private Task MakeRecurringAsync() => Shell.Current.GoToAsync(AppShell.PlanEditorRoute, new Dictionary<string, object> { ["fromEntry"] = _id });

    [RelayCommand]
    private Task OpenRefundAsync(EntryRow row) => Shell.Current.GoToAsync(AppShell.EntryDetailRoute, new Dictionary<string, object> { ["id"] = row.Id });

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (IsBusy)
        {
            return;
        }

        if (_holdingTypeId is not null)
        {
            await EditAsync();
            return;
        }

        // Undo is offered on the transaction list for a few seconds (TX-05), so from there no question is asked. Opened from
        // anywhere else (Home, an account, a plan), the page shown after deleting offers no undo: confirm first.
        var stack = Shell.Current.Navigation.NavigationStack;
        var backToList = stack.Count >= 2 && stack[^2] is Transactions.TransactionsPage;
        if (!backToList && !await Shell.Current.DisplayAlertAsync(translator["Entry_DeleteQuestion"], $"{Heading} · {AmountText}", translator["Common_Delete"], translator["Common_Cancel"]))
        {
            return;
        }

        IsBusy = true;
        try
        {
            var deleted = await store.DeleteEntryAsync(_id);
            if (deleted.Count > 0)
            {
                undo.Offer(deleted);
            }

            await Shell.Current.GoToAsync("..");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task MarkReviewedAsync()
    {
        if (_entry is null)
        {
            return;
        }

        _entry.Review = ReviewState.Confirmed;
        await store.SaveEntryAsync(_entry);
        await LoadAsync();
    }
}
