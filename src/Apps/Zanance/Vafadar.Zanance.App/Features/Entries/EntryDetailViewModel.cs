using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentIcons.Common;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Data;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Maui.Mvvm;

namespace Vafadar.Zanance.App.Features.Entries;

/// <summary>An attachment in the entry details; images get a preview.</summary>
public sealed record AttachmentRow(Guid Id, string Name, string Details, ImageSource? Preview)
{
    public bool HasPreview => Preview is not null;
}

/// <summary>A labelled value in the entry details.</summary>
public sealed record DetailLine(string Label, string Value);

/// <summary>Details of one entry with refund, duplicate, edit and delete (UI-04).</summary>
public sealed partial class EntryDetailViewModel(
    ZananceStore store,
    Translator translator,
    ILocalizationService localization,
    IDateFormatter dates,
    UndoService undo) : ViewModelBase, IQueryAttributable
{
    private Guid _id;
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
        CanSplit = EntryActions.CanSplit(related);
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

            if (entry.GroupId is { } group && EntryActions.FindTransferFee(entry, await store.GetGroupAsync(group)) is { } fee)
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
            Lines.Add(new DetailLine(string.Empty, translator["Entry_FromPlan"]));
        }

        Refunds.Clear();
        if (entry.Kind == EntryKind.Expense)
        {
            var refunds = await store.GetRefundsAsync(entry.Id);
            foreach (var refund in refunds)
            {
                Refunds.Add(presenter.Row(refund) with { Subtitle = dates.Format(refund.Date, DateFormatStyle.Short) });
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
            Attachments.Add(new AttachmentRow(info.Id, info.FileName, $"\u2066\u200E{size} · {dates.Format(DateOnly.FromDateTime(info.CreatedAt.LocalDateTime), DateFormatStyle.Short)}\u200E\u2069", preview));
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

            if (picked.Data.Length > EntryAttachment.MaxBytes)
            {
                await Shell.Current.DisplayAlertAsync(translator["Attachment_Title"], translator["Attachment_TooLarge"], translator["Common_Ok"]);
                return;
            }

            await store.AddAttachmentAsync(new EntryAttachment { EntryId = _id, FileName = picked.Name, ContentType = picked.ContentType, Data = picked.Data });
            await LoadAttachmentsAsync();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or PermissionException)
        {
            await Shell.Current.DisplayAlertAsync(translator["Attachment_Title"], translator["Attachment_Failed"], translator["Common_Ok"]);
        }
    }

    [RelayCommand]
    private async Task OpenAttachmentAsync(AttachmentRow row)
    {
        if (await store.GetAttachmentAsync(row.Id) is { } attachment)
        {
            await AttachmentFiles.OpenAsync(attachment);
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
    private Task EditAsync() => Shell.Current.GoToAsync(AppShell.EntryEditorRoute, new Dictionary<string, object> { ["id"] = _id });

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

        IsBusy = true;
        try
        {
            // No confirmation: deleting is undoable from the list for a few seconds (TX-05).
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
