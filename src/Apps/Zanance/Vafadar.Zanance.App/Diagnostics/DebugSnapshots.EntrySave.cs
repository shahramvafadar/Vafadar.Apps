#if DEBUG && WINDOWS
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Vafadar.Data;
using Vafadar.Zanance.App.Features.Entries;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Receipts;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Invokes the actual original editor Save once with a pending fictitious receipt and failed change observers.</summary>
    private static async Task ReviewEntrySaveWindowsAsync(App app, IServiceProvider services, string folder)
    {
        var sourceRun = Environment.GetEnvironmentVariable("VAFADAR_ENTRY_SAVE_SOURCE_RUN");
        var continuation = !string.IsNullOrEmpty(sourceRun);
        if (continuation && !System.Text.RegularExpressions.Regex.IsMatch(sourceRun!, "^[0-9a-f]{32}$"))
            throw new IOException("Original uncommitted editor source identity required.");
        var originalFolder = continuation ? Path.Combine(Path.GetDirectoryName(folder)!, sourceRun!) : folder;
        var source = Path.Combine(originalFolder, "original-editor.db");
        if (continuation)
        {
            if (!File.Exists(source) || File.Exists(Path.Combine(originalFolder, "complete.json"))
                || !File.Exists(Path.Combine(originalFolder, "error.txt")))
                throw new IOException("Only the retained failed, uncommitted editor source may continue.");
        }
        else if (File.Exists(source)) throw new IOException("Preserve the original editor sequence; never reseed it.");
        await using var fixture = new ServiceCollection().AddZananceData(source).BuildServiceProvider();
        if (!continuation) await Task.Run(() => fixture.MigrateLocalDatabase<ZananceDbContext>());
        var store = fixture.GetRequiredService<ZananceStore>();
        var baseStore = services.GetRequiredService<ZananceStore>();
        async Task<string> BaseRows() => JsonSerializer.Serialize(new { Accounts = await baseStore.GetAccountsAsync(), Entries = await baseStore.GetEntriesAsync() });
        var baseBefore = await BaseRows();
        Account account;
        if (continuation)
        {
            account = (await store.GetAccountsAsync()).Single();
            if (account.Name != "D425 fictitious cash" || account.CurrencyCode != "EUR" || account.OpeningBalance != 0
                || (await store.GetEntriesAsync()).Count != 0)
                throw new IOException("The original account or uncommitted editor checkpoint differs.");
        }
        else
        {
            account = new Account { Name = "D425 fictitious cash", CurrencyCode = "EUR", OpeningDate = new(2026, 10, 1) };
            await store.SaveAccountAsync(account);
        }
        var model = ActivatorUtilities.CreateInstance<EntryEditorViewModel>(services, store,
            fixture.GetRequiredService<Data.Commerce.ReceiptWorkStore>());
        var load = typeof(EntryEditorViewModel).GetMethod("ApplyQueryAsync", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new IOException("Original editor loader unavailable.");
        // Match routed construction: the real page exists before its query publishes Picker choices.
        var page = new EntryEditorPage(model); await Shell.Current.Navigation.PushAsync(page, false);
        await (Task)load.Invoke(model, [new Dictionary<string, object> { ["kind"] = "Expense", ["account"] = account.Id,
            ["receipt"] = ReceiptParser.Parse("Fictitious D425 merchant\nTOTAL EUR 12.34"),
            ["receiptFile"] = new PendingAttachment("D425-original.pdf", "application/pdf", [1, 2, 3, 4]) }])!;
        model.AmountText = "12.34"; model.EntryTitle = "D425 original editor purchase";
        var button = VisualDescendants(page).OfType<Button>().Single(value => ReferenceEquals(value.Command, model.SaveCommand));
        // Save has its own fixed footer; scrolling a sibling viewport cannot bring that footer into view.
        await WaitForStorePageAsync(app);
        if (page.Handler?.PlatformView is Microsoft.UI.Xaml.FrameworkElement frame) frame.UpdateLayout();
        var directory = Path.Combine(folder, "original-save"); Directory.CreateDirectory(directory);
        await CaptureWindowAsync(app.Windows[0], Path.Combine(directory, "editor.png"));
        await File.WriteAllTextAsync(Path.Combine(folder, "original-save-target.json"), JsonSerializer.Serialize(new
        { model.CanSave, model.IsBusy, AccountId = model.Account?.Id, button.IsEnabled, button.Width, button.Height,
            NativeType = button.Handler?.PlatformView?.GetType().FullName,
            NativeWidth = (button.Handler?.PlatformView as Microsoft.UI.Xaml.FrameworkElement)?.ActualWidth,
            NativeHeight = (button.Handler?.PlatformView as Microsoft.UI.Xaml.FrameworkElement)?.ActualHeight }));
        if (!model.CanSave || button.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button
            { IsEnabled: true, ActualWidth: >= 44, ActualHeight: >= 44 } native
            || FrameworkElementAutomationPeer.CreatePeerForElement(native)?.GetPattern(PatternInterface.Invoke) is not IInvokeProvider invoke)
            throw new IOException("Original native editor Save target unavailable.");
        var notices = 0;
        store.Changed += (_, _) => { notices++; throw new IOException("Deliberate original postcommit observer refusal."); };
        invoke.Invoke();
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (ReferenceEquals(Shell.Current.CurrentPage, page) || model.IsBusy)
        { if (DateTime.UtcNow > deadline) throw new IOException("Preserve the original pending editor Save."); await Task.Delay(50); }
        var entry = (await store.GetEntriesAsync()).Single(); var metadata = (await store.GetAttachmentsAsync(entry.Id)).Single();
        var document = (await store.GetAttachmentAsync(metadata.Id))!;
        if (model.CanSave || entry.Amount != 1234 || entry.Title != "D425 original editor purchase"
            || entry.AccountId != account.Id || document.FileName != "D425-original.pdf"
            || !document.Data.SequenceEqual(new byte[] { 1, 2, 3, 4 }) || notices != 2 || baseBefore != await BaseRows())
            throw new IOException("The original native editor commit, document or base rows differ.");
        await File.WriteAllTextAsync(Path.Combine(folder, "complete.json"), JsonSerializer.Serialize(new
        { Captures = new[] { new { Language = "original-save", File = "editor.png" } }, Package = AppInfo.Current.PackageName,
            ProcessId = Environment.ProcessId, OnlySeparateFictitiousLedgerChanged = true, OriginalBaseStoreUnchanged = true,
            OriginalSaveInvocations = 1, AcceptedFinancialEntries = 1, OriginalDocumentBytesMatched = true,
            OriginalSaveConsumed = !model.CanSave, DeliberatePostcommitObserverFailures = notices,
            OriginalSourceRun = sourceRun, NoAcceptedCommandReplayed = true }));
    }
}
#endif
