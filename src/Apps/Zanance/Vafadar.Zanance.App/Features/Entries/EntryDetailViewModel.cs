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
    Interaction.IAppInteraction interaction, Receipts.ReceiptReadingSession receipts) : ViewModelBase, IQueryAttributable, Presentation.IThemeAware
{
    private readonly Commerce.QuickResourceActions _quick = new(store, translator, interaction);
    private Guid _id;
    private readonly EntryAttachmentSession _attachments = new(store);
    private long _attachmentRevision;
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

    private Dictionary<string, IAsyncRelayCommand>? _commands;
    private long _commandGeneration;

    private IAsyncRelayCommand OwnedCommand(string name, Func<object?, Task> action)
    {
        _commands ??= [];
        if (_commands.TryGetValue(name, out var command)) return command;
        var generation = _commandGeneration; var owner = _detail; var source = owner?.Displayed;
        var created = new AsyncRelayCommand<object?>(parameter => generation == _commandGeneration
            && ReferenceEquals(owner, _detail) && source is not null && ReferenceEquals(source, owner?.Displayed)
            && Loading.IsReady && !NotFound && IsNotBusy && owner!.CanInteract
                ? action(parameter) : Task.CompletedTask);
        _commands.Add(name, created); return created;
    }

    private void ResetCommands()
    {
        _commandGeneration++; _commands = null;
        OnPropertyChanged(nameof(ReadReceiptCommand));
        OnPropertyChanged(nameof(AddAttachmentCommand));
        OnPropertyChanged(nameof(OpenAttachmentCommand));
        OnPropertyChanged(nameof(DeleteAttachmentCommand));
        OnPropertyChanged(nameof(EditCommand));
        OnPropertyChanged(nameof(RecordReimbursementCommand));
        OnPropertyChanged(nameof(RefundCommand));
        OnPropertyChanged(nameof(DuplicateCommand));
        OnPropertyChanged(nameof(SaveRuleCommand));
        OnPropertyChanged(nameof(SplitCommand));
        OnPropertyChanged(nameof(PayBackCommand));
        OnPropertyChanged(nameof(SaveAsTemplateCommand));
        OnPropertyChanged(nameof(MakeRecurringCommand));
        OnPropertyChanged(nameof(OpenRefundCommand));
        OnPropertyChanged(nameof(DeleteCommand));
        OnPropertyChanged(nameof(MarkReviewedCommand));
    }

    /// <summary>Gets ReadReceipt bound to this complete original detail publication.</summary>
    public IAsyncRelayCommand ReadReceiptCommand => OwnedCommand(nameof(ReadReceiptCommand), value => value is AttachmentRow row ? ReadReceiptAsync(row) : Task.CompletedTask);

    /// <summary>Gets AddAttachment bound to this complete original detail publication.</summary>
    public IAsyncRelayCommand AddAttachmentCommand => OwnedCommand(nameof(AddAttachmentCommand), _ => AddAttachmentAsync());

    /// <summary>Gets OpenAttachment bound to this complete original detail publication.</summary>
    public IAsyncRelayCommand OpenAttachmentCommand => OwnedCommand(nameof(OpenAttachmentCommand), value => value is AttachmentRow row ? OpenAttachmentAsync(row) : Task.CompletedTask);

    /// <summary>Gets DeleteAttachment bound to this complete original detail publication.</summary>
    public IAsyncRelayCommand DeleteAttachmentCommand => OwnedCommand(nameof(DeleteAttachmentCommand), value => value is AttachmentRow row ? DeleteAttachmentAsync(row) : Task.CompletedTask);

    /// <summary>Gets Edit bound to this complete original detail publication.</summary>
    public IAsyncRelayCommand EditCommand => OwnedCommand(nameof(EditCommand), _ => EditAsync());

    /// <summary>Gets RecordReimbursement bound to this complete original detail publication.</summary>
    public IAsyncRelayCommand RecordReimbursementCommand => OwnedCommand(nameof(RecordReimbursementCommand), _ => RecordReimbursementAsync());

    /// <summary>Gets Refund bound to this complete original detail publication.</summary>
    public IAsyncRelayCommand RefundCommand => OwnedCommand(nameof(RefundCommand), _ => RefundAsync());

    /// <summary>Gets Duplicate bound to this complete original detail publication.</summary>
    public IAsyncRelayCommand DuplicateCommand => OwnedCommand(nameof(DuplicateCommand), _ => DuplicateAsync());

    /// <summary>Gets SaveRule bound to this complete original detail publication.</summary>
    public IAsyncRelayCommand SaveRuleCommand => OwnedCommand(nameof(SaveRuleCommand), _ => SaveRuleAsync());

    /// <summary>Gets Split bound to this complete original detail publication.</summary>
    public IAsyncRelayCommand SplitCommand => OwnedCommand(nameof(SplitCommand), _ => SplitAsync());

    /// <summary>Gets PayBack bound to this complete original detail publication.</summary>
    public IAsyncRelayCommand PayBackCommand => OwnedCommand(nameof(PayBackCommand), _ => PayBackAsync());

    /// <summary>Gets SaveAsTemplate bound to this complete original detail publication.</summary>
    public IAsyncRelayCommand SaveAsTemplateCommand => OwnedCommand(nameof(SaveAsTemplateCommand), _ => SaveAsTemplateAsync());

    /// <summary>Gets MakeRecurring bound to this complete original detail publication.</summary>
    public IAsyncRelayCommand MakeRecurringCommand => OwnedCommand(nameof(MakeRecurringCommand), _ => MakeRecurringAsync());

    /// <summary>Gets OpenRefund bound to this complete original detail publication.</summary>
    public IAsyncRelayCommand OpenRefundCommand => OwnedCommand(nameof(OpenRefundCommand), value => value is EntryRow row ? OpenRefundAsync(row) : Task.CompletedTask);

    /// <summary>Gets Delete bound to this complete original detail publication.</summary>
    public IAsyncRelayCommand DeleteCommand => OwnedCommand(nameof(DeleteCommand), _ => DeleteAsync());

    /// <summary>Gets MarkReviewed bound to this complete original detail publication.</summary>
    public IAsyncRelayCommand MarkReviewedCommand => OwnedCommand(nameof(MarkReviewedCommand), _ => MarkReviewedAsync());

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.TryGetValue("id", out var value) && (value is Guid id || Guid.TryParse(value?.ToString(), out id)))
        {
            if (_id != id)
            {
                Retire(); _id = id; _detail = new EntryDetailSession(store, id);
                _entry = null; Feedback = null; IsBusy = false;
            }
        }
    }

    Task Presentation.IThemeAware.RefreshThemeAsync()
    {
        if (Loading.IsReady && _detail?.Displayed is { } original)
        {
            var prepared = Prepare(original);
            AmountColor = prepared.AmountColor; IconColor = prepared.IconColor; IconBackground = prepared.IconBackground;
            Refunds.Clear(); foreach (var row in prepared.Refunds) Refunds.Add(row);
        }
        return Task.CompletedTask;
    }

    /// <summary>Retires complete detail work on departure, retaining no action authority in a hidden tab.</summary>
    public void Retire()
    {
        _revision++; _detail?.Retire(); _quick.Retire(); RetireAttachments(); IsBusy = false; Loading = new(); ResetCommands();
    }

    /// <summary>Gets the cover for one complete original detail, including explicit read-only Retry.</summary>
    [ObservableProperty]
    public partial SnapshotLoadState Loading { get; private set; } = new();

    /// <summary>Gets feedback that preserves a known accepted change when its following view step failed.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LoadFailureText))]
    public partial string? Feedback { get; private set; }

    /// <summary>Gets the current translated failure, never a misleading uncommitted outcome.</summary>
    public string LoadFailureText => Feedback ?? translator["Error_Unexpected"];

    /// <summary>Gets the complete published source for bounded native qualification, never a pending packet.</summary>
    internal EntryDetailSnapshot? Displayed => Loading.IsReady ? _detail?.Displayed : null;

    /// <summary>Gets whether the current complete source owns available detail actions.</summary>
    internal bool CanInteract => Loading.IsReady && !NotFound && IsNotBusy && _detail?.CanInteract == true;

    private int _revision;
    private EntryDetailSession? _detail;

    /// <summary>Reads and prepares the complete original packet before synchronous covered publication.</summary>
    public async Task LoadAsync()
    {
        var owner = _detail ??= new EntryDetailSession(store, _id);
        var revision = _revision; var loading = Loading; var id = _id;
        EntryDetailSnapshot? source = null; Prepared? prepared = null;
        void Current()
        {
            if (revision != _revision || !ReferenceEquals(owner, _detail) || !ReferenceEquals(loading, Loading) || id != _id)
                throw new OperationCanceledException("The original transaction detail retired.");
        }
        try
        {
            await loading.RunAsync(async () =>
            {
                Current(); RetireAttachments(); _quick.Retire(); ResetCommands();
                source = await owner.ReadAsync(); Current();
                prepared = await Task.Run(() => Prepare(source)); Current();
                await store.ValidateEntryDetailSnapshotAsync(source); Current(); source.EnsureAccessCurrent();
            }, () =>
            {
                var original = source ?? throw new InvalidOperationException("The complete original transaction detail is unavailable.");
                var ready = prepared ?? throw new InvalidOperationException("The complete original transaction presentation is unavailable.");
                void Publish(Action change) { Current(); original.EnsureAccessCurrent(); change(); Current(); original.EnsureAccessCurrent(); }
                Publish(() => Heading = ready.Heading);
                Publish(() => KindText = ready.KindText);
                Publish(() => AmountText = ready.AmountText);
                Publish(() => AmountColor = ready.AmountColor);
                Publish(() => Icon = ready.Icon);
                Publish(() => IconColor = ready.IconColor);
                Publish(() => IconBackground = ready.IconBackground);
                Publish(() => IsUnreviewed = ready.IsUnreviewed);
                Publish(() => CanPayBack = ready.CanPayBack);
                Publish(() => CanMakeRecurring = ready.CanMakeRecurring);
                Publish(() => CanSaveTemplate = ready.CanSaveTemplate);
                Publish(() => RuleActionText = ready.RuleActionText);
                Publish(() => IsHoldingMoney = ready.IsHoldingMoney);
                Publish(() => CanSplit = ready.CanSplit);
                Publish(() => SplitActionText = ready.SplitActionText);
                Publish(() => SplitText = ready.SplitText);
                Publish(() => ReimbursementText = ready.ReimbursementText);
                Publish(() => CanRecordReimbursement = ready.CanRecordReimbursement);
                Publish(() => CanRefund = ready.CanRefund);
                Publish(() => RefundableText = ready.RefundableText);
                Publish(() => HasRefunds = ready.HasRefunds);
                Publish(() => HasAttachments = ready.HasAttachments);
                Publish(() => NotFound = ready.NotFound);
                Publish(() => Lines.Clear()); foreach (var line in ready.Lines) Publish(() => Lines.Add(line));
                Publish(() => Refunds.Clear()); foreach (var row in ready.Refunds) Publish(() => Refunds.Add(row));
                Publish(() => Attachments.Clear()); foreach (var row in ready.Attachments) Publish(() => Attachments.Add(row));
                _entry = original.Entry?.Copy();
                if (_entry is not null && original.Inputs.AttachmentReview is { } attachments)
                    Publish(() => _attachments.PublishReview(attachments, _entry));
                Publish(() => _quick.PublishOriginal(original.Inputs.Resources));
                Publish(() => owner.Publish(original)); Feedback = null; ResetCommands();
            });
        }
        catch (OperationCanceledException) when (revision != _revision || !ReferenceEquals(owner, _detail)) { }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            if (revision != _revision || !ReferenceEquals(owner, _detail)) return;
            Feedback ??= CommercialFailureMessage.OrFallback(exception, translator, "Error_Unexpected");
            owner.Retire(); _quick.Retire(); RetireAttachments(); ResetCommands();
            System.Diagnostics.Debug.WriteLine($"Original transaction detail load failed: {exception}");
        }
    }

    private sealed class Prepared
    {
        public Guid? HoldingTypeId;
        public string? RuleMatch;
        public List<DetailLine> Lines { get; } = [];
        public List<EntryRow> Refunds { get; } = [];
        public List<AttachmentRow> Attachments { get; } = [];
        public string? Heading;
        public string? KindText;
        public string? AmountText;
        public Color? AmountColor;
        public Symbol Icon;
        public Color? IconColor;
        public Color? IconBackground;
        public bool IsUnreviewed;
        public bool CanPayBack;
        public bool CanMakeRecurring;
        public bool CanSaveTemplate;
        public string? RuleActionText;
        public bool IsHoldingMoney;
        public bool CanSplit;
        public string? SplitActionText;
        public string? SplitText;
        public string? ReimbursementText;
        public bool CanRecordReimbursement;
        public bool CanRefund;
        public string? RefundableText;
        public bool HasRefunds;
        public bool HasAttachments;
        public bool NotFound;
    }

    private Prepared Prepare(EntryDetailSnapshot source)
    {
        var state = new Prepared { NotFound = source.Entry is null };
        if (source.Entry is null) return state;
        var entry = source.Entry!;
        var review = source.Inputs.Resources;
        var accounts = review.Accounts.ToDictionary(a => a.Id);
        var categories = new CategoryLookup(review.Categories, translator);
        var culture = localization.CurrentCulture;
        var presenter = new EntryPresenter(accounts, categories, translator, culture);
        var row = presenter.Row(entry);

        state.Heading = row.Title;
        state.KindText = translator[$"EntryKind_{entry.Kind}"];
        state.AmountText = row.AmountText;
        state.AmountColor = row.AmountColor;
        state.Icon = row.Icon;
        state.IconColor = row.IconColor;
        state.IconBackground = row.IconBackground;
        state.IsUnreviewed = row.IsUnreviewed;
        state.CanPayBack = entry.Kind == EntryKind.Income;
        state.CanMakeRecurring = entry.Kind is EntryKind.Income or EntryKind.Expense or EntryKind.Transfer && entry.ScheduleId is null;
        state.CanSaveTemplate = entry.Kind is EntryKind.Income or EntryKind.Expense or EntryKind.Transfer && review.Work.CanCreateTemplate;
        state.RuleMatch = entry.Kind is EntryKind.Income or EntryKind.Expense && entry.CategoryId is { } ruleCategory
            && categories.Get(ruleCategory) is { SystemKey: not Core.Categories.DefaultCategories.Uncategorized }
                ? (entry.Payee ?? entry.Title)?.Trim()
                : null;
        state.RuleActionText = state.RuleMatch is { Length: >= Core.Categories.CategoryRules.MinLength } match
            ? translator.Format("Rule_Always", match, categories.Name(entry.CategoryId))
            : null;
        var ruleKind = entry.Kind == EntryKind.Income ? Core.Categories.CategoryKind.Income : Core.Categories.CategoryKind.Expense;
        if (source.Inputs.Rules.Any(rule => rule.Kind == ruleKind && rule.CategoryId == entry.CategoryId
            && string.Equals(rule.Match, state.RuleMatch, StringComparison.CurrentCultureIgnoreCase))) state.RuleActionText = null;
        var related = source.Inputs.Related;

        // The money of a holding purchase or sale (and its fee) is edited and deleted with the holding (ZEX-AS14).
        state.HoldingTypeId = source.Inputs.HoldingTypeId;
        state.IsHoldingMoney = state.HoldingTypeId is not null;
        if (state.IsHoldingMoney)
        {
            state.CanPayBack = state.CanMakeRecurring = state.CanSaveTemplate = false;
            state.RuleActionText = null;
        }

        // Only refunds and paybacks of these entries matter for splitting; reading the whole ledger for them was slow.
        var paybacks = source.Inputs.Paybacks;

        state.CanSplit = !state.IsHoldingMoney && EntryActions.CanSplit(related) && !EntryActions.HasPaybacks(related, paybacks);
        var isSplit = EntryActions.IsSplit(related);
        state.SplitActionText = translator[isSplit ? "Split_Edit" : "Split_Action"];
        state.SplitText = isSplit
            ? translator.Format("Split_PartOf", MoneyText.Format(related.Sum(e => e.Amount), presenter.CurrencyOf(entry.AccountId), culture), related.Count)
            : null;

        state.Lines.Clear();
        state.Lines.Add(new DetailLine(translator["Entry_Date"], dates.Format(entry.Date, DateFormatStyle.Long)));
        if (entry.Kind == EntryKind.Transfer)
        {
            state.Lines.Add(new DetailLine(translator["Entry_FromAccount"], presenter.AccountName(entry.AccountId)));
            state.Lines.Add(new DetailLine(translator["Entry_ToAccount"], presenter.AccountName(entry.ToAccountId)));
            if (entry.ToAmount is { } toAmount && entry.ToAccountId is { } to)
            {
                state.Lines.Add(new DetailLine(translator.Format("Entry_ToAmount", presenter.CurrencyOf(to)), MoneyText.Format(toAmount, presenter.CurrencyOf(to), culture)));
            }

            if (EntryActions.FindTransferFee(entry, related) is { } fee)
            {
                state.Lines.Add(new DetailLine(translator["Entry_Fee"], MoneyText.Format(fee.Amount, presenter.CurrencyOf(fee.AccountId), culture)));
            }
        }
        else
        {
            state.Lines.Add(new DetailLine(translator["Entry_Account"], presenter.AccountName(entry.AccountId)));
            if (entry.Kind != EntryKind.Adjustment)
            {
                state.Lines.Add(new DetailLine(translator["Entry_Category"], categories.Name(entry.CategoryId)));
            }
        }

        if (entry.Kind == EntryKind.Refund && source.Inputs.Purchase is { } purchase)
        {
            state.Lines.Add(new DetailLine(translator["EntryKind_Refund"], translator.Format("Entry_RefundOf", presenter.Title(purchase))));
        }

        if (!string.IsNullOrEmpty(entry.Payee))
        {
            state.Lines.Add(new DetailLine(translator["Entry_Payee"], entry.Payee));
        }

        if (entry.OriginalAmount is { } original && entry.OriginalCurrencyCode is { } originalCurrency)
        {
            state.Lines.Add(new DetailLine(translator["Entry_ForeignAmount"], MoneyText.Format(original, originalCurrency, culture)));
        }

        if (!string.IsNullOrEmpty(entry.Note))
        {
            state.Lines.Add(new DetailLine(translator["Entry_Note"], entry.Note));
        }

        if (entry.Tags.Count > 0)
        {
            state.Lines.Add(new DetailLine(translator["Entry_Tags"], string.Join("  ", entry.Tags.Select(EntryTags.Display))));
        }

        if (entry.Source == EntrySource.Schedule)
        {
            state.Lines.Add(new DetailLine(null, translator["Entry_FromPlan"]));
        }

        state.Refunds.Clear();
        if (entry.Kind == EntryKind.Expense)
        {
            var refunds = source.Inputs.Paybacks.Where(value => value.RefundOfId == entry.Id).ToList();
            foreach (var refund in refunds)
            {
                state.Refunds.Add(presenter.Row(refund) with { Subtitle = dates.Format(refund.Date, DateFormatStyle.Long) });
            }

            var refundable = EntryActions.Refundable(entry, refunds);
            if (entry.ReimbursableAmount is { } reimbursable)
            {
                var open = EntryActions.OpenReimbursement(entry, refunds);
                var currency = presenter.CurrencyOf(entry.AccountId);
                state.ReimbursementText = translator.Format(open > 0 ? "Entry_ReimbursementOpen" : "Entry_ReimbursementDone",
                    MoneyText.Format(reimbursable, currency, culture), entry.ReimbursedBy ?? translator["Entry_ReimbursedBySomeone"], MoneyText.Format(open, currency, culture));
                state.CanRecordReimbursement = open > 0 && accounts.ContainsKey(entry.AccountId);
            }
            else
            {
                state.ReimbursementText = null;
                state.CanRecordReimbursement = false;
            }

            state.CanRefund = refundable > 0 && accounts.ContainsKey(entry.AccountId);
            state.RefundableText = refunds.Count > 0 ? translator.Format("Entry_Refundable", MoneyText.Format(refundable, presenter.CurrencyOf(entry.AccountId), culture)) : null;
        }
        else
        {
            state.CanRefund = false;
            state.RefundableText = null;
        }

        state.HasRefunds = state.Refunds.Count > 0;
        if (source.Inputs.AttachmentReview is { } documents)
        {
            foreach (var info in documents.Inputs.Attachments.OrderBy(value => value.CreatedAt).ThenBy(value => value.Id))
            {
                ImageSource? preview = null;
                if (source.Inputs.Images.SingleOrDefault(image => image.AttachmentId == info.Id) is { } image)
                { var bytes = image.Data; preview = ImageSource.FromStream(() => new MemoryStream(bytes)); }
                var size = info.Size >= 1024 * 1024 ? $"{info.Size / 1024d / 1024d:0.0} MB" : $"{Math.Max(1, info.Size / 1024)} KB";
                state.Attachments.Add(new(info.Id, info.FileName, $"\u2066\u200E{size} · {dates.Format(DateOnly.FromDateTime(info.CreatedAt.LocalDateTime), DateFormatStyle.Short)}\u200E\u2069", preview)
                { CanRead = entry.Kind == EntryKind.Expense && DocumentReader.CanRead(info.ContentType) });
            }
        }
        state.HasAttachments = state.Attachments.Count > 0;
        return state;
    }

    private Task ReadReceiptAsync(AttachmentRow row) => RunOriginalAsync(owner => owner.NavigateAsync(async source =>
    {
        var revision = _revision; var attachmentRevision = _attachmentRevision;
        bool Current() => Owns(owner, revision) && attachmentRevision == _attachmentRevision && ReferenceEquals(owner.Displayed, source);
        if (!row.CanRead || source.Entry?.Kind != EntryKind.Expense
            || source.Inputs.AttachmentReview?.Inputs.Attachments.Any(value => value.Id == row.Id) != true) return;
        await _attachments.ReadAsync(row.Id);
        if (Current()) await receipts.ReadAttachmentAsync(source.EntryId, row.Id, Current);
    }));

    /// <summary>Retires pending native attachment work when this original detail leaves navigation.</summary>
    public void RetireAttachments() { _attachmentRevision++; _attachments.Retire(); }

    private Task AddAttachmentAsync() => RunOriginalAsync(async owner =>
    {
        var revision = _revision; var attachmentRevision = _attachmentRevision;
        try
        {
            await owner.NavigateAsync(async _ => await _attachments.AddAsync(async () =>
            {
                var picked = await AttachmentFiles.PickAsync(translator["Attachment_Pick"]);
                if (picked is null) return null;
                if (!Owns(owner, revision) || attachmentRevision != _attachmentRevision)
                    throw new OperationCanceledException("The original file picker retired.");
                if (picked.Data.Length == 0 || picked.Data.Length > EntryAttachment.MaxBytes)
                {
                    await interaction.AlertAsync(translator["Attachment_Title"],
                        translator[picked.Data.Length == 0 ? "Attachment_Failed" : "Attachment_TooLarge"], translator["Common_Ok"]);
                    return null;
                }
                return new EntryAttachment { FileName = picked.Name, ContentType = picked.ContentType, Data = picked.Data };
            }));
        }
        catch (Data.Commerce.CommittedWriteNotificationException) when (_attachments.Accepted) { }
        if (Owns(owner, revision) && attachmentRevision == _attachmentRevision && _attachments.Accepted)
            await ReloadAcceptedAsync();
    });

    private Task OpenAttachmentAsync(AttachmentRow row) => RunOriginalAsync(owner => owner.NavigateAsync(async source =>
    {
        var revision = _revision; var attachmentRevision = _attachmentRevision;
        bool Current() => Owns(owner, revision) && attachmentRevision == _attachmentRevision && ReferenceEquals(owner.Displayed, source);
        var attachment = await _attachments.ReadAsync(row.Id);
        if (!Current()) return;
        var opened = await AttachmentFiles.OpenAsync(attachment, async () =>
        {
            if (!Current()) return false;
            await store.ValidateEntryDetailSnapshotAsync(source);
            var current = await _attachments.ReadAsync(row.Id);
            return Current() && current.Data.AsSpan().SequenceEqual(attachment.Data);
        });
        if (!opened && Current())
            await interaction.AlertAsync(translator["Attachment_Title"], translator["Attachment_OpenFailed"], translator["Common_Ok"]);
    }));

    private Task DeleteAttachmentAsync(AttachmentRow row) => RunOriginalAsync(async owner =>
    {
        var revision = _revision; var attachmentRevision = _attachmentRevision;
        try
        {
            await owner.NavigateAsync(async _ => await _attachments.DeleteAsync(row.Id, name => interaction.ConfirmAsync(
                translator["Attachment_Delete"], name, translator["Common_Delete"], translator["Common_Cancel"])));
        }
        catch (Data.Commerce.CommittedWriteNotificationException) when (_attachments.Accepted) { }
        if (Owns(owner, revision) && attachmentRevision == _attachmentRevision && _attachments.Accepted)
            await ReloadAcceptedAsync();
    });

    private bool Owns(EntryDetailSession owner, int revision) => revision == _revision && ReferenceEquals(owner, _detail);

    private async Task RunOriginalAsync(Func<EntryDetailSession, Task> action)
    {
        if (IsBusy || !Loading.IsReady || NotFound || _detail is not { CanInteract: true } owner) return;
        var revision = _revision; IsBusy = true;
        try { await action(owner); }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            if (!Owns(owner, revision)) return;
            Feedback ??= owner.Accepted ? translator["Occurrence_SavedRefreshFailed"]
                : CommercialFailureMessage.OrFallback(exception, translator, "Error_Unexpected");
            System.Diagnostics.Debug.WriteLine($"Original transaction detail action failed: {exception}");
            owner.Retire(); _quick.Retire(); RetireAttachments(); ResetCommands();
            var failed = Loading = new SnapshotLoadState();
            // Present one covered read-only Retry instead of leaving a stale or already accepted action enabled.
            try { await failed.RunAsync(() => Task.FromException(exception), () => { }); }
            catch (Exception error) when (error is not OutOfMemoryException) { }
        }
        finally { if (Owns(owner, revision)) IsBusy = false; }
    }

    private Task NavigateAsync(Func<EntryDetailSnapshot, Task> target) => RunOriginalAsync(owner => owner.NavigateAsync(target));

    private Task EditAsync() => NavigateAsync(source => source.Inputs.HoldingTypeId is { } holding
        ? Shell.Current.GoToAsync(AppShell.HoldingDetailRoute, new Dictionary<string, object> { ["id"] = holding })
        : Shell.Current.GoToAsync(AppShell.EntryEditorRoute, new Dictionary<string, object> { ["id"] = source.EntryId }));

    private Task RecordReimbursementAsync() => NavigateAsync(source => source.Inputs.HoldingTypeId is null
        && source.Entry is { Kind: EntryKind.Expense } entry
        && EntryActions.OpenReimbursement(entry, source.Inputs.Paybacks.Where(value => value.RefundOfId == entry.Id).ToArray()) > 0
            ? Shell.Current.GoToAsync(AppShell.EntryEditorRoute, new Dictionary<string, object> { ["refundOf"] = source.EntryId, ["reimburse"] = true })
            : Task.CompletedTask);

    private Task RefundAsync() => NavigateAsync(source => source.Inputs.HoldingTypeId is null
        && source.Entry is { Kind: EntryKind.Expense } entry
        && EntryActions.Refundable(entry, source.Inputs.Paybacks.Where(value => value.RefundOfId == entry.Id).ToArray()) > 0
            ? Shell.Current.GoToAsync(AppShell.EntryEditorRoute, new Dictionary<string, object> { ["refundOf"] = source.EntryId })
            : Task.CompletedTask);

    private Task DuplicateAsync() => NavigateAsync(source => source.Inputs.HoldingTypeId is null
        ? Shell.Current.GoToAsync(AppShell.EntryEditorRoute, new Dictionary<string, object> { ["duplicate"] = source.EntryId }) : Task.CompletedTask);

    private Task SaveRuleAsync() => RunOriginalAsync(async owner =>
    {
        var revision = _revision;
        try { await owner.SaveRuleAsync(); }
        catch (Data.Commerce.CommittedWriteNotificationException) when (owner.Accepted) { }
        if (Owns(owner, revision) && owner.Accepted) await ReloadAcceptedAsync();
    });

    private Task SplitAsync() => NavigateAsync(source => source.Inputs.HoldingTypeId is null
        && EntryActions.CanSplit(source.Inputs.Related) && !EntryActions.HasPaybacks(source.Inputs.Related, source.Inputs.Paybacks)
            ? Shell.Current.GoToAsync(AppShell.SplitRoute, new Dictionary<string, object> { ["id"] = source.EntryId }) : Task.CompletedTask);

    private Task PayBackAsync() => NavigateAsync(source => source.Inputs.HoldingTypeId is null && source.Entry?.Kind == EntryKind.Income
        ? Shell.Current.GoToAsync(AppShell.EntryEditorRoute, new Dictionary<string, object> { ["kind"] = nameof(EntryKind.IncomeReversal), ["of"] = source.EntryId })
        : Task.CompletedTask);

    private Task SaveAsTemplateAsync() => RunOriginalAsync(async owner =>
    {
        if (!CanSaveTemplate || _entry is not { } displayed) return;
        var revision = _revision; var quick = _quick;
        await owner.NavigateAsync(_ => quick.CreateTemplateAsync(displayed, Heading ?? string.Empty, AmountText ?? string.Empty));
        if (!Owns(owner, revision)) return;
        if (quick.Accepted) await ReloadAcceptedAsync();
        else CanSaveTemplate = quick.Review?.Work.CanCreateTemplate == true;
    });

    private Task MakeRecurringAsync() => NavigateAsync(source => source.Inputs.HoldingTypeId is null
        && source.Entry is { ScheduleId: null, Kind: EntryKind.Income or EntryKind.Expense or EntryKind.Transfer }
            ? Shell.Current.GoToAsync(AppShell.PlanEditorRoute, new Dictionary<string, object> { ["fromEntry"] = source.EntryId }) : Task.CompletedTask);

    private Task OpenRefundAsync(EntryRow row) => NavigateAsync(source => source.Inputs.Paybacks.Any(value => value.Id == row.Id && value.RefundOfId == source.EntryId)
        ? Shell.Current.GoToAsync(AppShell.EntryDetailRoute, new Dictionary<string, object> { ["id"] = row.Id }) : Task.CompletedTask);

    private Task DeleteAsync() => RunOriginalAsync(async owner =>
    {
        var source = owner.Displayed!; var revision = _revision;
        if (source.Inputs.HoldingTypeId is { } holding)
        { await owner.NavigateAsync(_ => Shell.Current.GoToAsync(AppShell.HoldingDetailRoute, new Dictionary<string, object> { ["id"] = holding })); return; }
        var heading = Heading; var amount = AmountText;
        var stack = Shell.Current.Navigation.NavigationStack;
        var backToList = stack.Count >= 2 && stack[^2] is Transactions.TransactionsPage;
        try
        {
            await owner.DeleteAsync(() => backToList ? Task.FromResult(true) : interaction.ConfirmAsync(
                translator["Entry_DeleteQuestion"], $"{heading} · {amount}", translator["Common_Delete"], translator["Common_Cancel"]));
        }
        catch (Data.Commerce.CommittedWriteNotificationException<IReadOnlyList<LedgerEntry>>) when (owner.Accepted) { }
        if (!Owns(owner, revision) || !owner.Accepted) return;
        // Offer consumes its original deadline before a fallible subscriber; never offer again to repair navigation.
        if (owner.Deleted is { Count: > 0 } deleted) undo.Offer(deleted);
        await Shell.Current.GoToAsync("..");
    });

    private Task MarkReviewedAsync() => RunOriginalAsync(async owner =>
    {
        if (owner.Displayed?.Entry?.Review != ReviewState.Unreviewed) return;
        var revision = _revision;
        try { await owner.MarkReviewedAsync(); }
        catch (Data.Commerce.CommittedWriteNotificationException) when (owner.Accepted) { }
        if (Owns(owner, revision) && owner.Accepted) await ReloadAcceptedAsync();
    });

    private Task ReloadAcceptedAsync()
    {
        Feedback = translator["Occurrence_SavedRefreshFailed"];
        return LoadAsync();
    }
}
