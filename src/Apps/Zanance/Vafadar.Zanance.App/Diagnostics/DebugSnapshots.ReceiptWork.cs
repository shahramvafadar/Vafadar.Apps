#if DEBUG && WINDOWS
using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Vafadar.Data;
using Vafadar.Documents;
using Vafadar.Localization;
using Vafadar.Zanance.App.Features.Entries;
using Vafadar.Zanance.App.Features.Home;
using Vafadar.Zanance.App.Features.Receipts;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Explicit fictitious picker, unlock and recognition ports; actual Home/detail commands and parser remain original.</summary>
    private sealed class ReceiptReviewRuntime(IAppInteraction interaction) : IReceiptRuntime
    {
        internal int Picked, Read;
        internal ReceiptDraft? Draft;
        internal TextLayoutResult Document = new("Fictitious shop\nTOTAL EUR 24.50");
        public Task<ReceiptPickedFile?> PickAsync()
        { Picked++; return Task.FromResult<ReceiptPickedFile?>(new("Fictitious compact receipt.png", "image/png", ReceiptReviewImage(), [1, 2, 3])); }
        public Task RunWhenUnlockedAsync(Func<Task> action) => action();
        public Task<TextLayoutResult> ReadAsync(byte[] data, string contentType) { Read++; return Task.FromResult(Document); }
        public Task AlertAsync(string title, string message, string cancel) => interaction.AlertAsync(title, message, cancel);
        public Task OpenEditorAsync(ReceiptDraft draft) { Draft = draft; return Task.CompletedTask; }
    }
    /// <summary>A valid independently generated one-pixel source for native preview; no OCR quality is inferred.</summary>
    private static byte[] ReceiptReviewImage() => Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aMZkAAAAASUVORK5CYII=");
    // D-170 / AT-171: native Home/attachment targets, complete translated refusal and actual typed editor publication.
    private static async Task ReviewReceiptWorkAsync(App app, IServiceProvider services, string folder, string[] languages)
    {
        var path = Path.Combine(folder, "receipt-work.db"); var access = new ForecastReviewAccess();
        await using var fixture = new ServiceCollection().AddSingleton<TimeProvider>(TimeProvider.System)
            .AddSingleton<ICommercialWriteAccessSource>(access).AddZananceData(path).BuildServiceProvider();
        fixture.MigrateLocalDatabase<ZananceDbContext>(); var store = fixture.GetRequiredService<ZananceStore>();
        var reader = fixture.GetRequiredService<ReceiptWorkStore>(); var factory = fixture.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        await ApplyOwnedReviewModeAsync(store); var day = DateOnly.FromDateTime(TimeProvider.System.GetLocalNow().DateTime);
        var account = new Account { Name = "Fictitious receipt account", CurrencyCode = "EUR", OpeningDate = day, OpeningBalance = 10000 };
        await store.SaveAccountAsync(account); var settings = await store.GetSettingsAsync(); settings.DefaultAccountId = account.Id; await store.SaveSettingsAsync(settings);
        var entry = new LedgerEntry { AccountId = account.Id, Date = day, Amount = 9876, Kind = EntryKind.Expense, Title = "Fictitious original receipt expense" };
        if (!(await store.SaveEntryAsync(entry)).Succeeded) throw new InvalidOperationException("Receipt fixture expense failed.");
        var attachment = new EntryAttachment { EntryId = entry.Id, FileName = "Fictitious original receipt.png", ContentType = "image/png", Data = ReceiptReviewImage() };
        await store.AddAttachmentAsync(attachment);
        async Task<string> StoredAsync()
        {
            await using var db = await factory.CreateDbContextAsync(); await db.Database.OpenConnectionAsync();
            using var command = db.Database.GetDbConnection().CreateCommand(); command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name";
            var names = new List<string>(); using (var rows = await command.ExecuteReaderAsync()) while (await rows.ReadAsync()) names.Add(rows.GetString(0));
            if (names.Count != 25) throw new InvalidOperationException("Incomplete original receipt fixture schema.");
            var all = new SortedDictionary<string, List<string>>();
            foreach (var name in names)
            {
                command.CommandText = "SELECT * FROM \"" + name + "\""; using var rows = await command.ExecuteReaderAsync(); var packets = new List<string>();
                while (await rows.ReadAsync()) { var values = new object?[rows.FieldCount]; for (var i = 0; i < values.Length; i++) values[i] = rows.IsDBNull(i) ? null : rows.GetValue(i); packets.Add(JsonSerializer.Serialize(values)); }
                packets.Sort(StringComparer.Ordinal); all[name] = packets;
            }
            return JsonSerializer.Serialize(all);
        }
        var before = await StoredAsync(); var translator = services.GetRequiredService<Translator>(); var localization = services.GetRequiredService<ILocalizationService>();
        var effects = services.GetRequiredService<IAppInteraction>(); var scope = new EntitlementScope(EntitlementScopeKind.PersonalProfile, Guid.Parse("93939393-8484-7575-6666-575757575757"));
        if (app.Windows[0].Handler?.PlatformView is not Microsoft.UI.Xaml.Window { Content.XamlRoot: { } root }) throw new InvalidOperationException("Missing receipt review root.");
        async Task CloseReceiptAlertAsync(string language, string scenario, string expected)
        {
            Microsoft.UI.Xaml.Controls.ContentDialog? dialog = null;
            for (var attempt = 0; attempt < 100 && dialog is null; attempt++)
            { dialog = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetOpenPopupsForXamlRoot(root).Select(p => p.Child).OfType<Microsoft.UI.Xaml.Controls.ContentDialog>().SingleOrDefault(); if (dialog is null) await Task.Delay(50); }
            if (dialog is null || dialog.Title as string != translator["Receipt_Title"]) throw new InvalidOperationException("The original receipt feedback did not open.");
            await Task.Delay(150); ProfileNativeReads.Add(dialog);
            if (!ReceiptNativeTree(dialog).OfType<Microsoft.UI.Xaml.Controls.TextBlock>().Any(block => block.Text == expected && !block.IsTextTrimmed))
                throw new InvalidOperationException("Receipt feedback was missing or clipped.");
            await CaptureWindowAsync(app.Windows[0], Path.Combine(folder, language + "-receipt-work-" + scenario + "-window.png"));
            var answer = FindNativeButton(dialog, translator["Common_Ok"]) ?? throw new InvalidOperationException("Missing original receipt answer.");
            var peer = FrameworkElementAutomationPeer.CreatePeerForElement(answer) as ButtonAutomationPeer;
            if (peer?.GetPattern(PatternInterface.Invoke) is not IInvokeProvider invoke) throw new InvalidOperationException("Missing receipt answer Invoke pattern.");
            ProfileNativeReads.Add(answer); ProfileNativeReads.Add(peer); ProfileNativeReads.Add(invoke); invoke.Invoke();
        }
        foreach (var language in languages)
        {
            await SetLanguageAsync(app, localization, language); access.Current = new(path, new(ProductPlan.Free, scope));
            var native = new ReceiptReviewRuntime(effects); var session = new ReceiptReadingSession(reader, native, translator, TimeProvider.System);
            var presenter = ActivatorUtilities.CreateInstance<Features.Goals.GoalPresenter>(services, fixture.GetRequiredService<HoldingStore>());
            var home = ActivatorUtilities.CreateInstance<HomeViewModel>(services, store, fixture.GetRequiredService<PlanStore>(), fixture.GetRequiredService<GoalStore>(),
                fixture.GetRequiredService<HoldingStore>(), presenter, session);
            var homePage = new HomePage(home, store); await Shell.Current.Navigation.PushAsync(homePage); await Task.Delay(150); await homePage.PendingPublication;
            var target = VisualDescendants(homePage).OfType<Button>().Single(button => button.Command == home.ReadReceiptCommand);
            if (FindScrollView(homePage) is { } homeScroll) await ScrollToViewIfNeededAsync(homeScroll, target);
            InvokeChoiceReview(target, ProfileNativeReads);
            await CloseReceiptAlertAsync(language, "home-refused", Presentation.CommercialFailureMessage.Describe(new CommercialWriteRejectedException(CommercialFeature.ReceiptReading, FeaturePermission.RequiresPlus), translator)!);
            if (home.ReadReceiptCommand.ExecutionTask is { } refused) await refused;
            if (native.Picked != 0 || native.Read != 0 || session.Error is null) throw new InvalidOperationException("Home opened a receipt source without access.");
            access.Current = new(path, new(ProductPlan.Plus, scope)); InvokeChoiceReview(target, ProfileNativeReads);
            if (home.ReadReceiptCommand.ExecutionTask is { } reading) await reading;
            var draft = native.Draft ?? throw new InvalidOperationException("The original Home target lost its typed receipt.");
            if (draft.File?.RecognitionData is not null || draft.Suggestion.Amount != 24.50m) throw new InvalidOperationException("The receipt lost original total or leaked transient recognition.");
            await Shell.Current.Navigation.PopAsync();
            var editor = ActivatorUtilities.CreateInstance<EntryEditorViewModel>(services, store, reader); editor.AmountText = "42";
            var editorPage = new EntryEditorPage(editor); await Shell.Current.Navigation.PushAsync(editorPage);
            var loading = typeof(EntryEditorViewModel).GetField("_loading", BindingFlags.Instance | BindingFlags.NonPublic)!;
            for (var attempt = 0; attempt < 60 && (!editor.QuickActions.CanInteract || (bool)loading.GetValue(editor)!); attempt++) await Task.Delay(50);
            if (!editor.QuickActions.CanInteract || (bool)loading.GetValue(editor)!) throw new InvalidOperationException("Original editor initialization did not finish.");
            var apply = typeof(EntryEditorViewModel).GetMethod("ApplyQueryAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
            await (Task)apply.Invoke(editor, [new Dictionary<string, object> { ["kind"] = nameof(EntryKind.Expense), ["receiptVisit"] = draft }])!;
            if (editor.AmountText != Core.Money.MoneyText.ForInput(2450, "EUR", localization.CurrentCulture) || editor.ReceiptNote is null || !editor.IsDirty)
                throw new InvalidOperationException("The original receipt editor did not retain its unsaved reviewed amount.");
            // Receipt publication can reveal the Simple form's detail action; await its actual new native layout.
            await Task.Delay(200);
            var actionGeometry = WrappingActions(editorPage).Where(Visible).Select(action =>
            {
                var caption = VisualDescendants(action).OfType<Label>().Single(); var button = VisualDescendants(action).OfType<Button>().Single();
                var nativeCaption = caption.Handler?.PlatformView as Microsoft.UI.Xaml.Controls.TextBlock;
                return new { action.Text, action.Width, action.Height, CaptionWidth = caption.Width, CaptionHeight = caption.Height,
                    nativeCaption?.ActualHeight, DesiredHeight = nativeCaption?.DesiredSize.Height, nativeCaption?.IsTextTrimmed,
                    CommandMatches = button.Command == action.Command, ParameterMatches = Equals(button.CommandParameter, action.CommandParameter),
                    SpokenName = SemanticProperties.GetDescription(button) };
            }).ToArray();
            await File.WriteAllTextAsync(Path.Combine(folder, language + "-receipt-work-original-action-geometry.json"), JsonSerializer.Serialize(actionGeometry));
            await CaptureAsync(app, folder, language + "-receipt-work-original-review"); await Shell.Current.Navigation.PopAsync();
            native.Draft = null; native.Document = new(null);
            var detail = ActivatorUtilities.CreateInstance<EntryDetailViewModel>(services, store, fixture.GetRequiredService<HoldingStore>(), session);
            detail.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = entry.Id }); var detailPage = new EntryDetailPage(detail);
            await Shell.Current.Navigation.PushAsync(detailPage);
            for (var attempt = 0; attempt < 60 && !detail.HasAttachments; attempt++) await Task.Delay(50);
            if (!detail.HasAttachments) throw new InvalidOperationException("Original detail appearance did not publish its attachments.");
            await Task.Delay(100);
            var row = detail.Attachments.Single(item => item.Id == attachment.Id);
            var read = VisualDescendants(detailPage).OfType<Button>().Single(button => ReferenceEquals(button.CommandParameter, row) && button.Command == detail.ReadReceiptCommand);
            if (FindScrollView(detailPage) is { } detailScroll) await ScrollToViewIfNeededAsync(detailScroll, read);
            InvokeChoiceReview(read, ProfileNativeReads); await CloseReceiptAlertAsync(language, "old-amount-retained", translator["Receipt_Nothing"]);
            if (detail.ReadReceiptCommand.ExecutionTask is { } rereading) await rereading;
            if (native.Draft is not null || (await store.GetEntryAsync(entry.Id))?.Amount != 9876) throw new InvalidOperationException("Empty rereading replaced the original expense.");
            await Shell.Current.Navigation.PopAsync();
            if (before != await StoredAsync()) throw new InvalidOperationException("Receipt native review wrote an original packet.");
            await File.WriteAllTextAsync(Path.Combine(folder, language + "-receipt-work-proof.json"), JsonSerializer.Serialize(new
            { ActualHomeAndAttachmentTargets = true, OriginalParserAndTypedEditor = true, NativeTranslatedRefusalAndNotice = true,
                FictitiousPickerUnlockAndDocumentReader = true, CompactPendingOnly = true, ExistingAmountAndComplete25RowsUnchanged = true }));
        }
    }
    /// <summary>Traverses only the current application's native dialog for text verification.</summary>
    private static IEnumerable<Microsoft.UI.Xaml.DependencyObject> ReceiptNativeTree(Microsoft.UI.Xaml.DependencyObject element)
    {
        yield return element;
        for (var i = 0; i < Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(element); i++)
            foreach (var child in ReceiptNativeTree(Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(element, i))) yield return child;
    }
}
#endif
