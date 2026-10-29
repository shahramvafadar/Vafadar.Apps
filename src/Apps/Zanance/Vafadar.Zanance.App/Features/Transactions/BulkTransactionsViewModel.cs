using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Localization;
using Vafadar.Maui.Mvvm;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Features.Transactions;

/// <summary>Selection and validated bulk commands shared by the actual transaction screen and non-UI application tests.</summary>
public sealed partial class BulkTransactionsViewModel(ZananceStore store, Translator translator, IAppInteraction interaction,
    UndoService undo, Func<Task> refresh, Func<bool>? isCurrent = null) : ViewModelBase
{
    private readonly HashSet<Guid> _selected = [];
    private HashSet<Guid> _known = [];
    private IReadOnlyList<LedgerEntry> _entries = [];
    private IReadOnlyList<Category> _categories = [];
    private Func<Guid?, string> _name = _ => string.Empty;
    private int _publicationRevision;
    private int _operationRevision;
    private TransactionsReadSnapshot? _source;
    private TransactionsReadSnapshot? _operationSource;

    /// <summary>Gets known original writer acceptance, independently of a later notification or reload failure.</summary>
    public bool Accepted { get; private set; }

    /// <summary>Retires native consent from a departing page without discarding its original retained selection.</summary>
    public void Retire() => Interlocked.Increment(ref _publicationRevision);

    private void RequireCurrent()
    {
        if (Volatile.Read(ref _operationRevision) != Volatile.Read(ref _publicationRevision) || isCurrent?.Invoke() == false)
            throw new OperationCanceledException("The original bulk transaction publication retired.");
    }

    /// <summary>Raised when the screen should synchronize its selection indicators.</summary>
    public event EventHandler? Changed;

    /// <summary>Gets or sets whether the transaction list is selecting rows.</summary>
    [ObservableProperty]
    public partial bool IsSelecting { get; set; }

    /// <summary>Gets whether a bulk action has selected entries.</summary>
    public bool HasSelection => _selected.Count > 0;

    /// <summary>Gets the translated selection count.</summary>
    public string SelectionText => translator.Format("Bulk_Selected", _selected.Count);

    /// <summary>Refreshes source snapshots without mutating them; removed rows lose their selection.</summary>
    public void Load(IReadOnlyList<LedgerEntry> entries, IReadOnlyList<Category> categories, Func<Guid?, string> name,
        TransactionsReadSnapshot? source = null)
    {
        Interlocked.Increment(ref _publicationRevision);
        _source = source;
        // D-105: index the fresh complete snapshot once, avoiding one ledger scan per selected row.
        var known = entries.Select(entry => entry.Id).ToHashSet();
        _entries = entries; _categories = categories; _name = name; _known = known;
        _selected.IntersectWith(known);
        Publish();
    }

    /// <summary>Returns whether the row is selected.</summary>
    public bool IsSelected(Guid id) => _selected.Contains(id);

    /// <summary>Enables selection without selecting or changing a transaction.</summary>
    public void Start() { if (!IsBusy && isCurrent?.Invoke() != false) { IsSelecting = true; Publish(); } }

    /// <summary>Clears selection without changing the ledger.</summary>
    public void Stop() { if (!IsBusy) { Clear(); } }

    /// <summary>Selects visible known rows only, preserving previously selected known rows.</summary>
    public void SelectAll(IEnumerable<Guid> visibleIds)
    {
        if (IsBusy) { return; }
        foreach (var id in visibleIds.Where(_known.Contains)) { _selected.Add(id); }
        Publish();
    }

    /// <summary>Toggles one known row without rebuilding its presentation.</summary>
    public void Toggle(Guid id)
    {
        if (IsBusy || isCurrent?.Invoke() == false || !_known.Contains(id)) { return; }
        if (!_selected.Add(id)) { _selected.Remove(id); }
        Publish();
    }

    [RelayCommand]
    private Task ReviewedAsync() => RunAsync(async () =>
    {
        var changed = SelectedCopies().Where(e => e.Review == ReviewState.Unreviewed).ToList();
        changed.ForEach(e => e.Review = ReviewState.Confirmed);
        await SaveAsync(changed);
    });

    [RelayCommand]
    private Task CategoryAsync() => RunAsync(async () =>
    {
        var entries = SelectedCopies().Where(e => e.Kind is EntryKind.Expense or EntryKind.Income or EntryKind.Refund or EntryKind.IncomeReversal).ToList();
        var kinds = entries.Select(e => e.Kind is EntryKind.Income or EntryKind.IncomeReversal ? CategoryKind.Income : CategoryKind.Expense).Distinct().ToList();
        if (kinds.Count != 1) { await interaction.AlertAsync(translator["Bulk_Title"], translator["Bulk_CategoryMixed"], translator["Common_Ok"]); return; }
        // Parent names disambiguate identically named subcategories, matching the transaction editor.
        var options = _categories.Where(c => c.Kind == kinds[0] && !c.IsArchived)
            .OrderBy(c => c.ParentId is null ? c.SortOrder : _categories.FirstOrDefault(p => p.Id == c.ParentId)?.SortOrder ?? 0)
            .ThenBy(c => c.ParentId is null ? 0 : 1)
            .Select(c => (c.Id, Name: c.ParentId is null ? _name(c.Id) : $"{_name(c.ParentId)} › {_name(c.Id)}")).ToList();
        var choice = await interaction.ChooseAsync(translator["Bulk_ChooseCategory"], translator["Common_Cancel"], [.. options.Select(o => o.Name)]);
        RequireCurrent();
        var picked = options.FindIndex(o => o.Name == choice);
        if (picked < 0) { return; }
        entries.ForEach(e => e.CategoryId = options[picked].Id);
        await SaveAsync(entries);
    });

    [RelayCommand]
    private Task TagAsync() => RunAsync(async () =>
    {
        var tag = EntryTags.Parse(await interaction.PromptAsync(translator["Bulk_AddTag"], translator["Bulk_AddTagMessage"],
            translator["Common_Ok"], translator["Common_Cancel"], maxLength: EntryTags.MaxLength)).FirstOrDefault();
        RequireCurrent();
        if (tag is null) { return; }
        var entries = SelectedCopies(); entries.ForEach(e => e.Tags = EntryTags.Normalize([.. e.Tags, tag]));
        await SaveAsync(entries);
    });

    [RelayCommand]
    private Task DeleteAsync() => RunAsync(async () =>
    {
        var ids = _selected.ToList();
        if (ids.Count == 0 || !await interaction.ConfirmAsync(translator["Bulk_Title"], translator.Format("Bulk_DeleteMessage", ids.Count),
                translator["Common_Delete"], translator["Common_Cancel"])) { return; }
        RequireCurrent();
        var deleted = await store.DeleteTransactionsAsync(_operationSource!, ids, Current, accepted: () => Accepted = true);
        if (deleted.Count > 0) { undo.Offer(deleted); }
        Clear(); await refresh();
    });

    private List<LedgerEntry> SelectedCopies() => [.. _entries.Where(e => _selected.Contains(e.Id)).Select(e => e.Copy())];

    private async Task SaveAsync(List<LedgerEntry> changed)
    {
        RequireCurrent();
        if (changed.Count == 0) { return; }
        // Work on copies: a rejected write must not change the list's in-memory state or its next command.
        var result = await store.SaveTransactionsAsync(_operationSource!, changed, Current, accepted: () => Accepted = true);
        if (!result.Succeeded)
        {
            await interaction.AlertAsync(translator["Bulk_Title"], string.Join(Environment.NewLine,
                result.Errors.Distinct().Select(e => translator[$"LedgerError_{e}"])), translator["Common_Ok"]);
            return;
        }
        Clear(); await refresh();
    }

    private async Task RunAsync(Func<Task> action)
    {
        if (IsBusy || isCurrent?.Invoke() == false) { return; }
        _operationRevision = _publicationRevision;
        Accepted = false;
        IsBusy = true; Publish();
        try
        {
            _operationSource = _source ?? await store.ReadTransactionsAsync();
            RequireCurrent();
            var source = _operationSource; var originalRows = _entries;
            var matches = await Task.Run(() => source.MatchesEntries(originalRows));
            RequireCurrent();
            if (!matches)
            {
                // Keep the original rejected-batch feedback and selection; a changed detached row grants no writer authority.
                await interaction.AlertAsync(translator["Bulk_Title"], translator["Error_Unexpected"], translator["Common_Ok"]);
                return;
            }
            await store.ValidateTransactionsAsync(_operationSource); RequireCurrent();
            await action();
        }
        catch (Data.Commerce.CommittedWriteNotificationException<IReadOnlyList<LedgerEntry>> committed)
        {
            Accepted = true;
            if (committed.Result.Count > 0) undo.Offer(committed.Result);
            Clear(); await ShowAcceptedFailureAsync();
        }
        catch (Data.Commerce.CommittedWriteNotificationException<SaveResult>)
        { Accepted = true; Clear(); await ShowAcceptedFailureAsync(); }
        catch (OperationCanceledException) when (!Current()) { }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            if (Accepted) { Clear(); await ShowAcceptedFailureAsync(); }
            else await interaction.ShowFailureAsync(ex);
        }
        finally { IsBusy = false; Publish(); }
    }

    private bool Current() => Volatile.Read(ref _operationRevision) == Volatile.Read(ref _publicationRevision) && isCurrent?.Invoke() != false;
    private Task ShowAcceptedFailureAsync() => interaction.AlertAsync(translator["Error_Title"],
        translator["Occurrence_SavedRefreshFailed"], translator["Common_Ok"]);

    private void Clear() { IsSelecting = false; _selected.Clear(); Publish(); }
    private void Publish() => Changed?.Invoke(this, EventArgs.Empty);
}
