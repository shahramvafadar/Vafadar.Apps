using System.Globalization;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Zanance.App.Features.Entries;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.DataFiles;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Features.DataFiles;

public sealed partial class ImportExportViewModel
{
    private bool _active = true, _rootRetired, _loading, _accountInitialized;
    private int _frameVersion, _operation, _batchRevision;
    private Func<bool>? _rootCurrent;
    private string? _sourceFile;
    private CancellationTokenSource _frame = new();
    private DataFileWorkSnapshot? _workspace;
    /// <summary>Gets original source loading/ready/failure cover, independently of a retained raw preview.</summary>
    public SnapshotLoadState Loading { get; private set; } = new();
    private bool Current => _active && !_rootRetired && !_frame.IsCancellationRequested && _rootCurrent?.Invoke() != false;
    /// <summary>Gets whether explicit original file actions own the current native frame.</summary>
    public bool CanUseDataFiles => Current && !_loading && !IsBusy && !Loading.HasFailed;
    /// <summary>Gets whether the original metadata source may be explicitly retried without reinterpreting CSV.</summary>
    public bool CanReloadDataSource => Current && !_loading && !IsBusy;
    /// <summary>Retries only the original local metadata source; never re-picks, reinterprets or imports a file.</summary>
    [RelayCommand(CanExecute = nameof(CanReloadDataSource))]
    private Task ReloadSourceAsync() => LoadAsync();
    /// <summary>Binds only the actual original root; replacement roots cannot borrow its file or consent.</summary>
    public void BindRootOwner(Func<bool> current) => _rootCurrent = current;
    /// <summary>Activates only actual page appearance/return, never public old Load or a delayed picker.</summary>
    public void SetPresentationActive(bool value)
    {
        if (!value) { Deactivate(); return; }
        if (_rootRetired || _rootCurrent?.Invoke() == false) return;
        if (!_active) { _frame.Dispose(); _frame = new(); _active = true; }
        RefreshActions();
    }
    /// <summary>Terminal original root retirement, retaining raw source and known accepted outcomes.</summary>
    public void RetireRoot() { _rootRetired = true; Deactivate(); }
    /// <summary>Cancels all original readers/native continuations, not merely a bank preset's review.</summary>
    public void Deactivate()
    {
        _active = false; Interlocked.Increment(ref _frameVersion); _operation++; _frame.Cancel();
        _loading = false; IsBusy = false; InvalidateBankReview(); _previewStale = true; CanImport = false;
        Loading = new(); OnPropertyChanged(nameof(Loading)); RefreshActions();
    }
    private sealed class FrameOwner(ImportExportViewModel owner, int version, CancellationToken token)
    {
        /// <summary>Gets the original frame token even after a new page return creates another frame.</summary>
        internal CancellationToken Token => token;
        /// <summary>Gets whether this original frame still owns its actual root.</summary>
        internal bool IsCurrent => !token.IsCancellationRequested && version == Volatile.Read(ref owner._frameVersion) && owner.Current;
        /// <summary>Refuses a retired original native continuation without creating replacement consent.</summary>
        internal void EnsureCurrent()
        { if (!IsCurrent) throw new OperationCanceledException("The original CSV page retired.", token); }
    }
    private sealed class WorkLease(ImportExportViewModel owner, int operation, FrameOwner frame) : IDisposable
    {
        /// <summary>Gets the original frame, whose consent remains distinct from later commands.</summary>
        internal FrameOwner Frame => frame;
        /// <inheritdoc />
        public void Dispose() { if (operation == owner._operation) { owner.IsBusy = false; owner.RefreshActions(); } }
    }
    private FrameOwner CaptureFrame()
    { var result = new FrameOwner(this, Volatile.Read(ref _frameVersion), _frame.Token); result.EnsureCurrent(); return result; }
    private WorkLease BeginWork()
    { var frame = CaptureFrame(); var result = new WorkLease(this, ++_operation, frame); IsBusy = true; RefreshActions(); return result; }
    private bool AcceptFile(FrameOwner owner, string file)
    { owner.EnsureCurrent(); _sourceFile ??= file; return _sourceFile == file; }
    private async Task<string> CaptureFileAsync(FrameOwner owner)
    {
        owner.EnsureCurrent(); var file = await _store.CaptureImportFileAsync(owner.Token);
        if (!AcceptFile(owner, file)) throw new InvalidOperationException("The original CSV file changed."); return file;
    }
    private async Task LoadWorkspaceAsync()
    {
        if (!CanReloadDataSource) return;
        using var work = BeginWork(); var frame = work.Frame; var loading = Loading; _loading = true; RefreshActions();
        DataFileWorkSnapshot? snapshot = null;
        try
        {
            await loading.RunAsync(async () => snapshot = await _store.ReadDataFileWorkAsync(frame.Token, file => AcceptFile(frame, file), _workspace), () =>
            {
                frame.EnsureCurrent(); var source = snapshot ?? throw new InvalidOperationException("Original CSV source unavailable."); source.EnsureCurrent();
                _workspace = source; Accounts = [.. source.Accounts.Select(account => new AccountChoice(account.Id, account.Name, account.CurrencyCode))];
                if (!_accountInitialized) { Account ??= Accounts.FirstOrDefault(); _accountInitialized = true; }
                CanExportHoldings = source.CanExportHoldings; PublishBatches(source);
            });
        }
        catch (Exception error) when (error is not OutOfMemoryException) { /* Original source cover owns read-only Retry; raw CSV is retained. */ }
        finally { if (frame.IsCurrent) { _loading = false; RefreshActions(); } }
    }
    private void PublishBatches(DataFileWorkSnapshot source)
    {
        Batches.ReplaceAll(source.Batches.Select(batch => new BatchRow(batch.BatchId,
            _translator.Format("Import_Batch", _dates.Format(batch.ImportedAt, Vafadar.Localization.Formatting.DateFormatStyle.Long), batch.Count))));
        UndoCommand.NotifyCanExecuteChanged();
    }
    private async Task RefreshBatchSourceAsync(FrameOwner? original = null, DataFileImportReceipt? acceptedImport = null)
    {
        var owner = original ?? CaptureFrame(); owner.EnsureCurrent();
        var source = await _store.ReadDataFileWorkAsync(owner.Token, file => AcceptFile(owner, file), _workspace, acceptedImport);
        owner.EnsureCurrent(); source.EnsureCurrent(); _workspace = source; PublishBatches(source);
    }
    private async Task ExportOriginalAsync(bool holdings)
    {
        if (!CanUseDataFiles) return;
        using var work = BeginWork(); var owner = work.Frame; var from = ExportFrom; var to = ExportTo; var notes = IncludeNotes;
        void CurrentRequest()
        {
            owner.EnsureCurrent();
            if (IncludeNotes != notes || !holdings && (ExportFrom != from || ExportTo != to))
                throw new InvalidOperationException("The original CSV disclosure choices changed.");
        }
        try
        {
            var file = await CaptureFileAsync(owner);
            if (!await _files.ConfirmExportAsync(_translator["Lock_ConfirmExport"]) || !owner.IsCurrent) return;
            CurrentRequest(); string content; int count;
            if (holdings)
            {
                var source = await _store.ReadDataFileHoldingsAsync(file, owner.Token); CurrentRequest(); source.EnsureCurrent();
                content = await Task.Run(() => HoldingsCsv.Write(source.Types, source.Locations, source.Events, source.Valuations, notes), owner.Token);
                count = source.Events.Count; source.EnsureCurrent();
            }
            else
            {
                var source = await _store.ReadDataFileLedgerAsync(file, from, to, owner.Token); CurrentRequest(); source.EnsureCurrent();
                var accounts = source.Accounts.ToDictionary(account => account.Id); var lookup = new CategoryLookup(source.Categories, _translator);
                content = await Task.Run(() => CsvExport.Write(source.Entries, accounts, id => lookup.Name(id), notes), owner.Token);
                count = source.Entries.Count; source.EnsureCurrent();
            }
            CurrentRequest(); var prefix = holdings ? "zanance-holdings-" : "zanance-export-";
            var name = string.Create(CultureInfo.InvariantCulture, $"{prefix}{_time.GetLocalNow():yyyyMMdd-HHmm}.csv");
            await _files.ShareCsvAsync(name, _translator["Export_Title"], content, CurrentRequest, owner.Token);
            // Offering is known even if the native share surface departs; never automatically send it again.
            ExportResult = _translator.Format(holdings ? "Export_HoldingsDone" : "Export_Done", count);
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        { if (owner.IsCurrent) await _interaction.ShowFailureAsync(error); }
    }
    private async Task UndoOriginalAsync(BatchRow batch)
    {
        if (!CanUndo(batch) || _workspace is not { } source) return;
        using var work = BeginWork(); var owner = work.Frame; var revision = Volatile.Read(ref _batchRevision);
        var review = source.ReviewUndo(batch.BatchId); var accepted = false;
        void NativeCurrent()
        {
            owner.EnsureCurrent();
            if (Volatile.Read(ref _batchRevision) != revision || !ReferenceEquals(_workspace, source))
                throw new InvalidOperationException("The original CSV Undo selection changed.");
        }
        try
        {
            if (!await _interaction.ConfirmAsync(_translator["Import_Undo"], _translator["Import_UndoMessage"], _translator["Import_Undo"], _translator["Common_Cancel"]) || !owner.IsCurrent) return;
            NativeCurrent(); UndoError = null; ImportResult = null; ImportUndoResult result; Exception? publication = null;
            try { result = await _store.TryUndoImportAsync(batch.BatchId, owner.Token, review, NativeCurrent); }
            catch (CommittedWriteNotificationException<ImportUndoResult> saved) { result = saved.Result; publication = saved; }
            if (result.Conflict) { if (owner.IsCurrent) UndoError = _translator["Import_UndoConflict"]; return; }
            // Record the original known commit before fallible source/history/native publication.
            accepted = true; ImportError = null; _completedUndo.Add(batch.BatchId);
            foreach (var row in Batches.Where(row => row.BatchId == batch.BatchId).ToList()) Batches.Remove(row);
            ImportResult = _translator["Import_UndoDone"]; HasPreview = false; CanImport = false; _previewStale = true; _previewAccepted = false;
            _preview = []; Overlaps.Clear(); ClearLedgerPreview();
            if (!owner.IsCurrent) return;
            try { await RefreshBatchSourceAsync(owner); }
            catch (Exception error) when (error is not OutOfMemoryException) { publication ??= error; }
            if (publication is not null && owner.IsCurrent) { UndoError = _translator["Import_UndoFollowUpFailed"]; await _interaction.ShowFailureAsync(publication); }
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            if (!owner.IsCurrent) return;
            UndoError = accepted ? _translator["Import_UndoFollowUpFailed"] : CommercialFailureMessage.OrFallback(error, _translator, "Import_UndoFailed");
            await _interaction.ShowFailureAsync(error);
        }
    }
}
