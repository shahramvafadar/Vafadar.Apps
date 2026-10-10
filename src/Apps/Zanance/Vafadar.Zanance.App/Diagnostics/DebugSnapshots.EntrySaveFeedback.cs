#if DEBUG && WINDOWS
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization;
using Vafadar.Zanance.App.Features.Entries;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

/// <summary>Native rejected-Save checks against the walk-through's independently owned fictitious database.</summary>
internal static partial class DebugSnapshots
{
    // AT-132: both ledger rejection and SQLite failure keep their full caption beside the reachable native Save.
    private static async Task ReviewEntrySaveFeedbackAsync(App app, IServiceProvider services, string folder,
        string language, Guid expenseId)
    {
        var page = app.Windows[0].Page?.Navigation.ModalStack.LastOrDefault() as EntryEditorPage
            ?? throw new InvalidOperationException("The actual entry modal is missing.");
        var vm = (EntryEditorViewModel)page.BindingContext;
        var factory = services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        var culture = services.GetRequiredService<ILocalizationService>().CurrentCulture;
        var viewport = page.FindByName<ScrollView>("ValidationViewport");
        var error = page.FindByName<Label>("SaveErrorLabel");
        var footer = ((Grid)page.Content).Children.OfType<VerticalStackLayout>().Single(view => Grid.GetRow(view) == 2);
        var save = footer.Children.OfType<Button>().Single();
        var root = page.Handler?.PlatformView as Microsoft.UI.Xaml.FrameworkElement
            ?? throw new InvalidOperationException("The entry modal has no native root.");
        if (save.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button nativeSave
            || Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.CreatePeerForElement(nativeSave)?.GetPattern(
                Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Invoke) is not Microsoft.UI.Xaml.Automation.Provider.IInvokeProvider invoke)
        { throw new InvalidOperationException("The entry modal lacks its actual native Save action."); }
        string Draft() => System.Text.Json.JsonSerializer.Serialize(new
        {
            vm.AmountText, vm.Account, vm.ToAccount, vm.KindIndex, vm.EntryTitle, vm.Date, vm.Note, vm.Payee, vm.TagsText,
            vm.FeeText, vm.DestinationFeeText, vm.ToAmountText, vm.ForeignEnabled, vm.ForeignCurrency, vm.ForeignAmountText,
            vm.IsAggregated, vm.AggregatedFrom, vm.AggregatedTo, vm.ReimbursableEnabled, vm.ReimbursableText,
            vm.ReimbursedBy, vm.ReimbursementDue, vm.HasReimbursementDue, vm.ShowDetails,
        });
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
        var before = await StoredAsync();
        var original = (vm.AmountText, vm.Note, vm.ShowDetails);
        var observations = new List<object>();
        var invocations = 0;
        async Task CheckAsync(string name, string key)
        {
            var draft = Draft();
            invoke.Invoke();
            if (vm.SaveCommand.ExecutionTask is { } saving) { await saving; }
            await Task.Delay(300);
            invocations++;
            if (vm.SaveError != Translator.Instance[key] || vm.IsBusy || draft != Draft() || before != await StoredAsync())
            { throw new InvalidOperationException("Rejected Save lost its error, changed the draft or wrote stored data."); }
            foreach (var atEnd in new[] { false, true })
            {
                if (atEnd) { await ScrollToEndIfNeededAsync(viewport); }
                else if (viewport.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.ScrollViewer scroller
                    && scroller.VerticalOffset > 1)
                { NativeScrollProvider(scroller).SetScrollPercent(-1, 0); await Task.Delay(300); }
                await Task.Delay(100);
                if (error.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.TextBlock text || text.IsTextTrimmed
                    || !text.IsTextScaleFactorEnabled || !error.IsVisible || text.Text != vm.SaveError)
                { throw new InvalidOperationException("The rejected Save lacks its full scaled native caption."); }
                var origin = text.TransformToVisual(root).TransformPoint(new Windows.Foundation.Point());
                var action = nativeSave.TransformToVisual(root).TransformPoint(new Windows.Foundation.Point());
                var boxes = new List<Windows.Foundation.Rect>();
                for (var offset = 0; offset <= text.ContentEnd.Offset - text.ContentStart.Offset; offset++)
                {
                    if (text.ContentStart.GetPositionAtOffset(offset, Microsoft.UI.Xaml.Documents.LogicalDirection.Forward) is { } pointer)
                    { boxes.Add(pointer.GetCharacterRect(Microsoft.UI.Xaml.Documents.LogicalDirection.Forward)); }
                }
                var nameText = Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.CreatePeerForElement(text)?.GetName();
                if (boxes.Count == 0 || boxes.Min(box => box.Left) < -1 || boxes.Max(box => box.Right) > error.Width + 1
                    || boxes.Max(box => box.Bottom) > error.Height + 1 || nameText != vm.SaveError
                    || origin.X < -1 || origin.Y < -1 || origin.X + error.Width > root.ActualWidth + 1
                    || origin.Y + error.Height > action.Y + 1 || action.Y + nativeSave.ActualHeight > root.ActualHeight + 1
                    || nativeSave.ActualHeight < 44 || nativeSave.ActualWidth < 44 || !nativeSave.IsEnabled
                    || viewport.Height < 44)
                { throw new InvalidOperationException("The complete error or native Save is outside its growing modal footer."); }
                observations.Add(new { name, atEnd, Error = text.Text, SpokenName = nameText, error.Width, error.Height,
                    origin, action, SaveHeight = nativeSave.ActualHeight, ViewportHeight = viewport.Height, Glyphs = boxes });
                await CaptureAsync(app, folder, language + "-entry-save-feedback-" + name + (atEnd ? "-end" : "-start"));
            }
        }
        try
        {
            vm.AmountText = Core.Money.MoneyText.ForInput(200, vm.CurrencyCode, culture);
            vm.Note = "Fictitious retained failed-Save draft";
            await CheckAsync("ledger", "LedgerError_RefundExceedsPurchase");
            await CheckAsync("retry", "LedgerError_RefundExceedsPurchase");
            vm.AmountText = original.AmountText;
            // Only this exact sample purchase fails; no production switch or unrelated SQLite object is changed.
            await using (var db = await factory.CreateDbContextAsync())
            {
                var connection = db.Database.GetDbConnection();
                await connection.OpenAsync();
                await using var trigger = connection.CreateCommand();
                // SQLite trigger DDL cannot bind parameters; this literal contains only the typed sample Guid.
                trigger.CommandText = "CREATE TRIGGER DebugEntrySaveFailure BEFORE UPDATE ON Entries WHEN OLD.Id = '"
                    + expenseId.ToString().ToUpperInvariant() + "' BEGIN SELECT RAISE(ABORT, 'Fictitious entry save failure'); END";
                await trigger.ExecuteNonQueryAsync();
            }
            await CheckAsync("storage", "Common_SaveFailed");
            vm.AmountText = "0";
            invoke.Invoke();
            if (vm.SaveCommand.ExecutionTask is { } invalid) { await invalid; }
            await Task.Delay(300);
            if (vm.SaveError is not null || error.IsVisible || vm.AmountError is null || before != await StoredAsync())
            { throw new InvalidOperationException("A new input-validation attempt retained stale save feedback or wrote data."); }
        }
        finally
        {
            await using var db = await factory.CreateDbContextAsync();
            await db.Database.ExecuteSqlRawAsync("DROP TRIGGER IF EXISTS DebugEntrySaveFailure");
            vm.AmountText = original.AmountText; vm.Note = original.Note; vm.ShowDetails = original.ShowDetails;
            vm.SaveError = null; vm.AmountError = null;
        }
        if (before != await StoredAsync()) { throw new InvalidOperationException("The failed-Save review did not retain all persisted rows."); }
        File.WriteAllText(Path.Combine(folder, language + "-entry-save-feedback-proof.json"), System.Text.Json.JsonSerializer.Serialize(new
        {
            NativeRejectedSaveInvocations = invocations, CompleteStoredRowsAndDraftRetained = true,
            FooterVisibleAtBothScrollEnds = true, CompleteNativeGlyphsNamesAndSaveTarget = true,
            StaleSaveErrorClearedOnInputValidation = true, OriginalDraftRestored = true, Observations = observations,
        }));
    }
}
#endif
