#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Media;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Entries;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;
using NativeButton = Microsoft.UI.Xaml.Controls.Button;
using NativeNode = Microsoft.UI.Xaml.DependencyObject;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Checks the actual detail/attachment Cancel and native feedback on an independent non-input desktop.</summary>
    private static async Task ReviewEntryAttachmentsWindowsAsync(App app, IServiceProvider services, string folder)
    {
        var local = services.GetRequiredService<ILocalizationService>(); var theme = services.GetRequiredService<ThemeService>();
        var text = services.GetRequiredService<Translator>(); var language = local.CurrentLanguage; var originalTheme = theme.Choice;
        var interaction = services.GetRequiredService<IAppInteraction>(); var captures = new List<object>();
        var sourceRun = Environment.GetEnvironmentVariable("VAFADAR_ENTRY_ATTACHMENT_SOURCE_RUN");
        var continuation = !string.IsNullOrEmpty(sourceRun);
        if (continuation && !System.Text.RegularExpressions.Regex.IsMatch(sourceRun!, "^[0-9a-f]{32}$"))
            throw new IOException("Original attachment continuation identity required.");
        var originalFolder = continuation ? Path.Combine(Path.GetDirectoryName(folder)!, sourceRun!) : folder;
        var source = Path.Combine(originalFolder, "original-attachments.db");
        if (continuation)
        {
            using var completed = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(originalFolder, "complete.json")));
            if (!completed.RootElement.GetProperty("OriginalTypedRowsUnchanged").GetBoolean()
                || completed.RootElement.GetProperty("Captures").GetArrayLength() != 54 || !File.Exists(source))
                throw new IOException("Exact completed original attachment source required.");
        }
        else if (File.Exists(source)) throw new IOException("Preserve the original attachment fixture; never reseed it.");
        await using var fixture = new ServiceCollection().AddZananceData(source).BuildServiceProvider();
        var store = fixture.GetRequiredService<ZananceStore>(); var time = services.GetRequiredService<TimeProvider>();
        LedgerEntry entry; EntryAttachment file;
        if (continuation)
        {
            entry = (await store.GetEntriesAsync()).Single();
            file = (await store.GetAttachmentAsync((await store.GetAttachmentsAsync(entry.Id)).Single().Id))!;
            if (entry.Title != "D424 original purchase" || entry.Amount != 1234 || file.FileName != "D424-original.pdf"
                || !file.Data.SequenceEqual(new byte[] { 1, 2, 3, 4 })) throw new IOException("Original attachment facts differ.");
        }
        else
        {
            await Task.Run(() => fixture.MigrateLocalDatabase<ZananceDbContext>());
            var account = new Account { Name = "D424 original account", CurrencyCode = "EUR", OpeningDate = new(2026, 10, 1) };
            await store.SaveAccountAsync(account);
            entry = new LedgerEntry { AccountId = account.Id, Kind = EntryKind.Expense, Amount = 1234,
                Date = new(2026, 10, 9), Title = "D424 original purchase", Note = "D424 unchanged original source" };
            if (!(await store.SaveEntryAsync(entry)).Succeeded) throw new IOException("Original attachment fixture refused.");
            file = new EntryAttachment { EntryId = entry.Id, FileName = "D424-original.pdf", ContentType = "application/pdf", Data = [1, 2, 3, 4] };
            await store.AddAttachmentAsync(file);
        }
        var before = await ImportFinancialNativeReview.StoredAsync(fixture, CancellationToken.None);
        var baseStore = services.GetRequiredService<ZananceStore>();
        async Task<string> BaseRows() => JsonSerializer.Serialize(new { Accounts = await baseStore.GetAccountsAsync(), Entries = await baseStore.GetEntriesAsync() });
        var baseBefore = await BaseRows();
        async Task SetTheme(ThemeChoice choice)
        {
            if (theme.Choice == choice) return;
            var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            void Rebuilt(object? sender, EventArgs args) => ready.TrySetResult();
            app.ShellRebuilt += Rebuilt;
            try { theme.Set(choice); await ready.Task.WaitAsync(TimeSpan.FromSeconds(30)); }
            finally { app.ShellRebuilt -= Rebuilt; }
        }
        async Task CaptureDialog(string context, string stage, string message, Task pending, string close)
        {
            if (app.Windows[0].Handler?.PlatformView is not Microsoft.UI.Xaml.Window { Content.XamlRoot: { } xamlRoot })
                throw new IOException("Original native attachment window unavailable.");
            NativeButton? button = null;
            for (var wait = 0; wait < 200 && button is null; wait++)
            {
                button = VisualTreeHelper.GetOpenPopupsForXamlRoot(xamlRoot).SelectMany(p => NativeNodes(p.Child)).OfType<NativeButton>()
                    .SingleOrDefault(b => FrameworkElementAutomationPeer.CreatePeerForElement(b)?.GetName() == close && b.IsEnabled);
                if (button is null) await Task.Delay(25);
            }
            if (button is null || FrameworkElementAutomationPeer.CreatePeerForElement(button)?.GetPattern(PatternInterface.Invoke) is not IInvokeProvider invoke)
                throw new IOException("Original native attachment dialog action missing.");
            var blocks = VisualTreeHelper.GetOpenPopupsForXamlRoot(xamlRoot).SelectMany(p => NativeNodes(p.Child))
                .OfType<Microsoft.UI.Xaml.Controls.TextBlock>().Where(b => b.Text == message || b.Text == NativeDigits.Apply(message)).ToArray();
            if (blocks.Length != 1 || blocks[0].IsTextTrimmed || blocks[0].ActualHeight <= 0)
                throw new IOException("Original complete attachment dialog text missing.");
            var key = context + "-" + stage; var directory = Path.Combine(folder, key); Directory.CreateDirectory(directory);
            await CaptureWindowAsync(app.Windows[0], Path.Combine(directory, "attachment.png"));
            captures.Add(new { Language = key, File = "attachment.png" }); invoke.Invoke(); await pending;
        }
        try
        {
            foreach (var name in new[] { "en", "fa", "de" })
            foreach (var choice in new[] { ThemeChoice.Light, ThemeChoice.Dark })
            foreach (var width in new[] { 360, 412, 1280 })
            {
                await SetTheme(choice); await SetLanguageAsync(app, local, name);
                app.Windows[0].Width = width; app.Windows[0].Height = 820;
                var context = name + "-" + choice + "-" + width;
                var model = new EntryDetailViewModel(store, text, local, new DateFormatter(local), new UndoService(store, time),
                    interaction, services.GetRequiredService<Features.Receipts.ReceiptReadingSession>());
                model.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = entry.Id });
                var page = new EntryDetailPage(model); await Shell.Current.Navigation.PushAsync(page, false);
                try
                {
                    var deadline = DateTime.UtcNow.AddSeconds(20);
                    while (!model.HasAttachments) { if (DateTime.UtcNow > deadline) throw new IOException("Original attachment page did not publish."); await Task.Delay(50); }
                    if (model.Attachments.Count != 1 || model.Attachments[0].Id != file.Id || model.Attachments[0].Name != file.FileName)
                        throw new IOException("Original attachment metadata differs.");
                    var target = VisualDescendants(page).OfType<Button>().Single(b => ReferenceEquals(b.Command, model.DeleteAttachmentCommand));
                    var scroll = VisualDescendants(page).OfType<ScrollView>().Single(); await ScrollToViewIfNeededAsync(scroll, target);
                    if (target.Handler?.PlatformView is not NativeButton { IsEnabled: true, ActualWidth: >= 44, ActualHeight: >= 44 } native
                        || FrameworkElementAutomationPeer.CreatePeerForElement(native)?.GetPattern(PatternInterface.Invoke) is not IInvokeProvider invoke)
                        throw new IOException("Original attachment Delete target unavailable.");
                    if (continuation)
                    {
                        var nameLabel = VisualDescendants(page).OfType<Label>().Single(label => label.Text == file.FileName);
                        await ScrollToViewIfNeededAsync(scroll, nameLabel); await Task.Delay(100);
                        if (nameLabel.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.TextBlock { ActualWidth: > 0, ActualHeight: > 0, IsTextTrimmed: false } caption
                            || caption.Text != NativeDigits.Apply(file.FileName)) throw new IOException("Original complete attachment name missing.");
                        var directory = Path.Combine(folder, context); Directory.CreateDirectory(directory);
                        await CaptureWindowAsync(app.Windows[0], Path.Combine(directory, "attachment.png"));
                        captures.Add(new { Language = context, File = "attachment.png" });
                    }
                    else
                    {
                        invoke.Invoke(); await Task.Yield();
                        await CaptureDialog(context, "cancel", file.FileName, model.DeleteAttachmentCommand.ExecutionTask!, text["Common_Cancel"]);
                        foreach (var key in new[] { "Attachment_SourceChanged", "Attachment_SavedRefresh" })
                        {
                            var pending = interaction.AlertAsync(text["Attachment_Title"], text[key], text["Common_Ok"]);
                            await CaptureDialog(context, key, text[key], pending, text["Common_Ok"]);
                        }
                    }
                    if (model.Attachments.Count != 1 || before != await ImportFinancialNativeReview.StoredAsync(fixture, CancellationToken.None))
                        throw new IOException("Attachment Cancel or feedback changed original rows.");
                }
                finally
                {
                    if (!ReferenceEquals(Shell.Current.CurrentPage, page)) throw new IOException("Original attachment detail changed.");
                    await Shell.Current.Navigation.PopAsync(false);
                }
            }
        }
        finally { await SetTheme(originalTheme); await SetLanguageAsync(app, local, language.CultureName); }
        if (baseBefore != await BaseRows()) throw new IOException("Attachment qualification changed the base store.");
        await File.WriteAllTextAsync(Path.Combine(folder, "complete.json"), JsonSerializer.Serialize(new
        { Captures = captures, Package = AppInfo.Current.PackageName, ProcessId = Environment.ProcessId,
            OriginalTypedRowsUnchanged = true, OriginalBaseStoreUnchanged = true, OnlySeparateFictitiousLedgerChanged = true,
            FinancialRowsUnchangedAfterSeeding = true, RealNativeDeleteCancel = !continuation, NativeFeedbackCaptionOnly = !continuation,
            OriginalCaptionSourceRun = sourceRun, NoOriginalSeedOrAcceptedCommandReplayed = continuation, CompleteFileNameObserved = continuation,
            NoFilePickerOrFinancialCommand = true, OriginalLanguageAndThemeRestored = true }));
        static IEnumerable<NativeNode> NativeNodes(NativeNode node)
        {
            yield return node;
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++)
                foreach (var child in NativeNodes(VisualTreeHelper.GetChild(node, i))) yield return child;
        }
    }
}
#endif
