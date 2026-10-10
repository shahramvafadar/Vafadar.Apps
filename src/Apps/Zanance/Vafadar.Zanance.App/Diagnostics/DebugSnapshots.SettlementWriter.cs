#if DEBUG && WINDOWS
using Microsoft.EntityFrameworkCore;
using Vafadar.Localization;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    // AT-133: real bound native Save retains the reviewed bill after SQLite failure or a changed advance row.
    private static async Task ReviewSettlementWriterFailuresAsync(App app, IServiceProvider services, string folder, string language)
    {
        var page = app.Windows[0].Page?.Navigation.ModalStack.LastOrDefault() as Features.Plans.SettlementPage
            ?? Shell.Current.CurrentPage as Features.Plans.SettlementPage
            ?? throw new InvalidOperationException("Missing actual settlement page.");
        var vm = (Features.Plans.SettlementViewModel)page.BindingContext;
        var store = services.GetRequiredService<ZananceStore>();
        var plans = services.GetRequiredService<PlanStore>();
        var factory = services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        var plan = (await plans.GetSchedulesAsync()).Single(p => p.Name == vm.PlanName);
        var entries = await store.GetEntriesAsync();
        var paid = Core.Plans.AdvanceSettlement.Compute(plan, entries, vm.From, vm.To, 0);
        var advance = paid.Advances.First();
        var original = (vm.From, vm.To, vm.ActualText);
        vm.ActualText = Core.Money.MoneyText.ForInput(paid.Paid + 5000, "EUR", services.GetRequiredService<ILocalizationService>().CurrentCulture);
        async Task<string> StoredAsync()
        {
            // Read every complete persisted row, including receipts and auditing, rather than projected ledger values.
            await using var db = await factory.CreateDbContextAsync();
            var connection = db.Database.GetDbConnection();
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name";
            var tables = new List<string>();
            await using (var names = await command.ExecuteReaderAsync())
            { while (await names.ReadAsync()) { tables.Add(names.GetString(0)); } }
            var rows = new Dictionary<string, List<string[]>>();
            foreach (var table in tables)
            {
                command.CommandText = "SELECT * FROM \"" + table.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
                var values = new List<string[]>();
                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var row = new string[reader.FieldCount];
                    for (var index = 0; index < row.Length; index++)
                    {
                        row[index] = reader.IsDBNull(index) ? "null" : reader.GetValue(index) is byte[] bytes
                            ? "bytes:" + Convert.ToHexString(bytes)
                            : reader.GetFieldType(index).Name + ":" + Convert.ToString(reader.GetValue(index), System.Globalization.CultureInfo.InvariantCulture);
                    }
                    values.Add(row);
                }
                rows[table] = [.. values.OrderBy(row => System.Text.Json.JsonSerializer.Serialize(row), StringComparer.Ordinal)];
            }
            return System.Text.Json.JsonSerializer.Serialize(rows);
        }
        var originalRows = await StoredAsync();
        var observations = new List<object>();
        async Task CheckAsync(string scenario)
        {
            var before = await StoredAsync();
            var draft = (vm.From, vm.To, vm.ActualText, vm.AdvancesText, vm.ResultText);
            InvokeWrappingAction(page, vm.SaveCommand);
            if (vm.SaveCommand.ExecutionTask is { } saving) await saving;
            await Task.Delay(300);
            if (vm.IsBusy || vm.Error != Translator.Instance["Common_SaveFailed"] || before != await StoredAsync()
                || draft != (vm.From, vm.To, vm.ActualText, vm.AdvancesText, vm.ResultText))
                throw new InvalidOperationException("Rejected native bill Save changed the draft or any complete stored row.");
            var label = VisualDescendants(page).OfType<Label>().Single(l => l.IsVisible && l.Text == vm.Error);
            await ScrollToViewIfNeededAsync(page.FindByName<ScrollView>("ValidationViewport"), label);
            await Task.Delay(200);
            if (label.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.TextBlock native || native.IsTextTrimmed
                || !native.IsTextScaleFactorEnabled || native.ActualHeight < 1)
                throw new InvalidOperationException("Missing actual readable translated bill failure.");
            var action = page.FindByName<Presentation.WrappingAction>("SaveAction");
            var button = VisualDescendants(action).OfType<Button>().Single();
            if (button.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button target || !target.IsEnabled
                || target.ActualHeight < 44 || target.ActualWidth < 44)
                throw new InvalidOperationException("The rejected bill lost its real Save target.");
            observations.Add(new { scenario, Error = native.Text, DraftRetained = true, All24TablesRetained = true });
            await CaptureAsync(app, folder, language + "-settlement-writer-" + scenario);
        }
        try
        {
            await using (var db = await factory.CreateDbContextAsync())
            {
                var connection = db.Database.GetDbConnection();
                await connection.OpenAsync();
                await using var command = connection.CreateCommand();
                // SQLite trigger DDL cannot bind parameters; this literal contains only the typed sample Guid.
                command.CommandText = "CREATE TRIGGER DebugBillWriterFailure BEFORE INSERT ON Entries WHEN NEW.AccountId = '"
                    + plan.AccountId.ToString().ToUpperInvariant() + "' BEGIN SELECT RAISE(ABORT, 'Fictitious bill failure'); END";
                await command.ExecuteNonQueryAsync();
            }
            await CheckAsync("storage");
            await CheckAsync("retry");
            await using (var db = await factory.CreateDbContextAsync())
            {
                await db.Database.ExecuteSqlRawAsync("DROP TRIGGER DebugBillWriterFailure");
                await db.Entries.Where(e => e.Id == advance.Id).ExecuteUpdateAsync(e => e.SetProperty(x => x.Amount, advance.Amount + 1));
            }
            await CheckAsync("stale-advance");
        }
        finally
        {
            await using var db = await factory.CreateDbContextAsync();
            await db.Database.ExecuteSqlRawAsync("DROP TRIGGER IF EXISTS DebugBillWriterFailure");
            await db.Entries.Where(e => e.Id == advance.Id).ExecuteUpdateAsync(e => e.SetProperty(x => x.Amount, advance.Amount));
            vm.From = original.From; vm.To = original.To; vm.ActualText = original.ActualText; vm.Error = null;
        }
        if (originalRows != await StoredAsync()) throw new InvalidOperationException("The bill writer review did not restore its exact fictitious rows.");
        await File.WriteAllTextAsync(Path.Combine(folder, language + "-settlement-writer-proof.json"),
            System.Text.Json.JsonSerializer.Serialize(new { NativeRejectedSaves = 3, Observations = observations, CompleteRowsRestored = true }));
    }
}
#endif
