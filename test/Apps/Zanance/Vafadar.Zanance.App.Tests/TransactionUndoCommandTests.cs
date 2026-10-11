using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.App.Features.Transactions;
using Vafadar.Zanance.App.Presentation;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-130: the actual bound Undo command reports failures without escaping into native command dispatch.</summary>
[Trait("AT", "AT-130")]
public sealed class TransactionUndoCommandTests
{
    [Fact]
    public async Task Failed_Undo_reports_the_original_exception_and_keeps_the_offer_without_refresh()
    {
        using var f = new FlowFixture(); var undo = new UndoService(f.Store, f.Time); var refreshed = 0;
        var failure = new IOException("Owned Undo action failure"); undo.Offer(() => throw failure);
        var action = new TransactionUndoViewModel(undo, f.Platform, () => { refreshed++; return Task.CompletedTask; });
        await action.UndoDeleteCommand.ExecuteAsync(null);
        Assert.Same(failure, Assert.Single(f.Platform.Failures)); Assert.True(undo.CanUndo);
        Assert.Equal(TimeSpan.FromSeconds(8), undo.Remaining); Assert.Equal(0, refreshed);
        Assert.True(action.UndoDeleteCommand.CanExecute(null));
    }
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Successful_Undo_refreshes_once_and_consumes_only_its_original_offer()
    {
        using var f = new FlowFixture(); var undo = new UndoService(f.Store, f.Time); var calls = 0; var refreshed = 0;
        undo.Offer(() => { calls++; return Task.CompletedTask; });
        var action = new TransactionUndoViewModel(undo, f.Platform, () => { refreshed++; return Task.CompletedTask; });
        await action.UndoDeleteCommand.ExecuteAsync(null);
        Assert.Equal(1, calls); Assert.Equal(1, refreshed); Assert.Empty(f.Platform.Failures); Assert.False(undo.CanUndo);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Repeated_invocation_during_action_or_refresh_never_posts_or_refreshes_twice(bool duringRefresh)
    {
        using var f = new FlowFixture(); var undo = new UndoService(f.Store, f.Time); var calls = 0; var refreshed = 0;
        var pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        undo.Offer(async () => { calls++; if (!duringRefresh) await pending.Task.WaitAsync(Ct); });
        var action = new TransactionUndoViewModel(undo, f.Platform, async () => { refreshed++; if (duringRefresh) await pending.Task.WaitAsync(Ct); });
        var first = action.UndoDeleteCommand.ExecuteAsync(null); Assert.False(action.UndoDeleteCommand.CanExecute(null));
        await action.UndoDeleteCommand.ExecuteAsync(null); pending.SetResult(); await first;
        Assert.Equal(1, calls); Assert.Equal(1, refreshed); Assert.Empty(f.Platform.Failures); Assert.True(action.UndoDeleteCommand.CanExecute(null));
    }

    [Fact]
    public async Task Pending_failure_feedback_blocks_a_second_command_and_keeps_a_replacement_offer()
    {
        using var f = new FlowFixture(); var undo = new UndoService(f.Store, f.Time); var refreshed = 0; var newerCalls = 0;
        var pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        f.Platform.PendingFailure = _ => pending.Task.WaitAsync(Ct);
        undo.Offer(() => throw new IOException("Owned dialog failure"));
        var action = new TransactionUndoViewModel(undo, f.Platform, () => { refreshed++; return Task.CompletedTask; });
        var first = action.UndoDeleteCommand.ExecuteAsync(null); Assert.Single(f.Platform.Failures);
        undo.Offer(() => { newerCalls++; return Task.CompletedTask; });
        Assert.False(action.UndoDeleteCommand.CanExecute(null)); await action.UndoDeleteCommand.ExecuteAsync(null);
        Assert.Equal(0, newerCalls); Assert.Equal(0, refreshed); pending.SetResult(); await first;
        Assert.True(undo.CanUndo); Assert.True(action.UndoDeleteCommand.CanExecute(null));
        await action.UndoDeleteCommand.ExecuteAsync(null); Assert.Equal(1, newerCalls); Assert.Equal(1, refreshed); Assert.Single(f.Platform.Failures);
    }

    [Fact]
    public async Task A_failed_refresh_reports_failure_after_committed_Undo_without_restoring_its_offer()
    {
        using var f = new FlowFixture(); var undo = new UndoService(f.Store, f.Time); var calls = 0;
        var failure = new IOException("Owned list refresh failure"); undo.Offer(() => { calls++; return Task.CompletedTask; });
        var action = new TransactionUndoViewModel(undo, f.Platform, () => throw failure);
        await action.UndoDeleteCommand.ExecuteAsync(null);
        Assert.Same(failure, Assert.Single(f.Platform.Failures)); Assert.Equal(1, calls); Assert.False(undo.CanUndo);
        Assert.True(action.UndoDeleteCommand.CanExecute(null));
    }

    [Fact]
    public async Task Expired_offer_does_not_execute_its_action_and_still_refreshes_the_current_list()
    {
        using var f = new FlowFixture(); var undo = new UndoService(f.Store, f.Time); var calls = 0; var refreshed = 0;
        undo.Offer(() => { calls++; return Task.CompletedTask; }); f.Time.Now = f.Time.Now.AddSeconds(8);
        var action = new TransactionUndoViewModel(undo, f.Platform, () => { refreshed++; return Task.CompletedTask; });
        await action.UndoDeleteCommand.ExecuteAsync(null);
        Assert.Equal(0, calls); Assert.Equal(1, refreshed); Assert.Empty(f.Platform.Failures); Assert.False(undo.CanUndo);
    }

    [Fact]
    public async Task Fatal_memory_failure_is_not_hidden_and_the_command_releases_its_execution_guard()
    {
        using var f = new FlowFixture(); var undo = new UndoService(f.Store, f.Time); var failure = new OutOfMemoryException("Owned fatal fixture");
        undo.Offer(() => throw failure); var action = new TransactionUndoViewModel(undo, f.Platform, () => Task.CompletedTask);
        Assert.Same(failure, await Assert.ThrowsAsync<OutOfMemoryException>(() => action.UndoDeleteCommand.ExecuteAsync(null)));
        Assert.Empty(f.Platform.Failures); Assert.True(undo.CanUndo); Assert.True(action.UndoDeleteCommand.CanExecute(null));
        undo.Offer(() => Task.CompletedTask); await action.UndoDeleteCommand.ExecuteAsync(null); Assert.False(undo.CanUndo);
    }

    [Fact]
    public async Task Failed_real_ledger_Undo_reports_once_preserves_all_tables_and_retries_complete_refunds()
    {
        using var f = new FlowFixture(); var account = new Account { Name = "Owned EUR", CurrencyCode = "EUR", OpeningDate = new(2026, 10, 10) };
        await f.Store.SaveAccountAsync(account, Ct);
        var entry = new LedgerEntry { AccountId = account.Id, Date = account.OpeningDate, Kind = EntryKind.Expense, Amount = 1000, Title = "Original purchase", Note = "Retained note", Tags = ["original"] };
        Assert.True((await f.Store.SaveEntryAsync(entry, Ct)).Succeeded);
        var refund = EntryActions.CreateRefund(entry, 300, account.Id, account.OpeningDate); Assert.True((await f.Store.SaveEntryAsync(refund, Ct)).Succeeded);
        var attachment = new EntryAttachment { EntryId = entry.Id, FileName = "owned.txt", ContentType = "text/plain", Data = [1, 2, 3] };
        await f.Store.AddAttachmentAsync(attachment, Ct);
        var original = await SnapshotAsync(f, entry.Id, refund.Id); var undo = new UndoService(f.Store, f.Time); undo.Offer(await f.Store.DeleteEntryAsync(entry.Id, Ct));
        await using var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER RejectBoundUndo BEFORE INSERT ON Entries BEGIN SELECT RAISE(ABORT,'Owned command Undo failure'); END;", Ct);
        var deleted = await SnapshotAsync(f); var refreshed = 0;
        var action = new TransactionUndoViewModel(undo, f.Platform, () => { refreshed++; return Task.CompletedTask; });
        f.Time.Now = f.Time.Now.AddSeconds(2); await action.UndoDeleteCommand.ExecuteAsync(null);
        Assert.IsType<DbUpdateException>(Assert.Single(f.Platform.Failures)); Assert.True(undo.CanUndo); Assert.Equal(TimeSpan.FromSeconds(6), undo.Remaining);
        Assert.Equal(0, refreshed); Assert.Equal(deleted, await SnapshotAsync(f));
        await db.Database.ExecuteSqlRawAsync("DROP TRIGGER RejectBoundUndo", Ct); await action.UndoDeleteCommand.ExecuteAsync(null);
        Assert.False(undo.CanUndo); Assert.Equal(1, refreshed); Assert.Single(f.Platform.Failures); Assert.Equal(original, await SnapshotAsync(f, entry.Id, refund.Id));
        Assert.Equal(refund.Id, Assert.Single(await f.Store.GetRefundsAsync(entry.Id, Ct)).Id);
    }

    /// <summary>Compares every complete stored row; no application storage algorithm is duplicated.</summary>
    private static async Task<string> SnapshotAsync(FlowFixture f, params Guid[] updatedEntries)
    {
        await using var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct);
        await db.Database.OpenConnectionAsync(Ct); var connection = db.Database.GetDbConnection(); var tables = new List<string>();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name";
            using var reader = await command.ExecuteReaderAsync(Ct); while (await reader.ReadAsync(Ct)) tables.Add(reader.GetString(0));
        }
        Assert.Equal(25, tables.Count); var result = new Dictionary<string, List<string>>();
        foreach (var table in tables)
        {
            using var command = connection.CreateCommand(); command.CommandText = "SELECT * FROM \"" + table + "\"";
            using var reader = await command.ExecuteReaderAsync(Ct); var rows = new List<string>();
            while (await reader.ReadAsync(Ct))
            {
                var values = new object?[reader.FieldCount];
                for (var i = 0; i < values.Length; i++) values[i] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                // Successful restore audits only the two known rows; retain every other column and timestamp.
                if (table == "Entries" && updatedEntries.Length > 0)
                {
                    var id = Array.FindIndex(Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToArray(), n => n == "Id");
                    if (Guid.TryParse(values[id]?.ToString(), out var entryId) && updatedEntries.Contains(entryId))
                        values[reader.GetOrdinal("UpdatedAt")] = null;
                }
                rows.Add(System.Text.Json.JsonSerializer.Serialize(values));
            }
            rows.Sort(StringComparer.Ordinal); result[table] = rows;
        }
        return System.Text.Json.JsonSerializer.Serialize(result);
    }
}
