#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Features.Transactions;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    // AT-110 exercises retained complete selection through the actual page and native non-writing actions.
    private static async Task ReviewBulkSelectionReloadAsync(IServiceProvider services, TransactionsViewModel vm,
        string folder, string language)
    {
        var store = services.GetRequiredService<ZananceStore>();
        var unchanged = JsonSerializer.Serialize(await store.GetEntriesAsync());
        var originalPeriod = vm.PeriodIndex;
        try
        {
            vm.PeriodIndex = 3;
            var expected = vm.Days.SelectMany(day => day).Select(row => row.Id).ToHashSet();
            if (expected.Count == 0) { throw new InvalidOperationException("Fictitious complete transaction rows required."); }
            vm.StartSelectingCommand.Execute(null); await Task.Delay(300);
            InvokeWrappingAction(Shell.Current.CurrentPage, vm.SelectAllCommand); await Task.Delay(100);
            if (vm.Days.SelectMany(day => day).Any(row => !row.Selection.IsSelected))
            { throw new InvalidOperationException("Native Select all omitted a visible row."); }
            await vm.LoadAsync(); vm.PeriodIndex = 3; await Task.Delay(400);
            var fresh = vm.Days.SelectMany(day => day).ToArray();
            if (!expected.SetEquals(fresh.Select(row => row.Id)) || fresh.Any(row => !row.Selection.IsSelected))
            { throw new InvalidOperationException("A fresh complete snapshot lost selection or rows."); }
            InvokeWrappingAction(Shell.Current.CurrentPage, vm.StopSelectingCommand); await Task.Delay(100);
            if (vm.IsSelecting || vm.HasSelection || fresh.Any(row => row.Selection.IsSelected))
            { throw new InvalidOperationException("Native Cancel did not clear the fresh selection."); }
            if (unchanged != JsonSerializer.Serialize(await store.GetEntriesAsync()))
            { throw new InvalidOperationException("Selection/reload changed fictitious financial data."); }
            File.WriteAllText(Path.Combine(folder, $"{language}-bulk-selection-reload.json"), JsonSerializer.Serialize(new
            { CompleteRows = fresh.Length, AllSelectedAfterReload = true, NativeSelectAllAndCancel = true, FinancialDataUnchanged = true }));
        }
        finally
        {
            vm.StopSelectingCommand.Execute(null); vm.PeriodIndex = originalPeriod;
        }
    }
}
#endif
