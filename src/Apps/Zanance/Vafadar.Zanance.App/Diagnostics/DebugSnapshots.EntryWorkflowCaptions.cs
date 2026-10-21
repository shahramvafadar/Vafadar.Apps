#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Entries;
using Vafadar.Zanance.App.Features.Home;
using Vafadar.Zanance.App.Features.Transactions;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Reviews the new refusal caption through real cancelled editor decisions, never another accepted financial Save.</summary>
    private static async Task ReviewEntryWorkflowCaptionsWindowsAsync(App app, IServiceProvider services,
        IServiceProvider fixture, ZananceStore store, string original, string folder, string originalBaseRows)
    {
        var restored = Path.Combine(original, "aggregate-undo-accepted.json");
        if (!File.Exists(restored)) throw new IOException("Retain the original completed native adjustment and Undo first.");
        var rows = await store.GetEntriesAsync();
        using (var receipt = JsonDocument.Parse(await File.ReadAllTextAsync(restored)))
            if (JsonSerializer.Serialize(rows.OrderBy(e => e.Id)) != JsonSerializer.Serialize(receipt.RootElement.GetProperty("Entries").Deserialize<LedgerEntry[]>()!.OrderBy(e => e.Id)))
                throw new IOException("The original accepted financial rows changed.");
        var aggregate = rows.Where(e => e.IsAggregated).OrderBy(e => e.Id).First();
        const string documentName = "D428 fictitious aggregate document.pdf";
        var files = await store.GetAttachmentsAsync(aggregate.Id);
        if (files.Count == 0)
        {
            var document = new EntryAttachment { EntryId = aggregate.Id, FileName = documentName, ContentType = "application/pdf", Data = [1, 2] };
            var intention = Path.Combine(original, "warning-document-input.json");
            if (File.Exists(intention)) throw new IOException("Observe the original pending document; never replay its metadata Save.");
            await File.WriteAllTextAsync(intention, JsonSerializer.Serialize(new { document.Id, document.EntryId, document.FileName }));
            await store.AddAttachmentAsync(document);
            await File.WriteAllTextAsync(Path.Combine(original, "warning-document-accepted.json"), JsonSerializer.Serialize(new { document.Id, ProcessId = Environment.ProcessId }));
        }
        else if (files.Count != 1 || files[0].FileName != documentName) throw new IOException("Original fictitious document ownership differs.");
        var baseline = await ImportFinancialNativeReview.StoredAsync(fixture, CancellationToken.None);
        var local = services.GetRequiredService<ILocalizationService>(); var theme = services.GetRequiredService<ThemeService>();
        var language = local.CurrentLanguage; var themeChoice = theme.Choice; var translator = services.GetRequiredService<Translator>();
        var time = services.GetRequiredService<TimeProvider>(); var interaction = services.GetRequiredService<IAppInteraction>();
        var captures = new List<object>(); var cases = new List<object>();
        var blockedOnly = Environment.GetEnvironmentVariable("VAFADAR_ENTRY_WORKFLOW_BLOCKED_ONLY") == "1";
        IEnumerable<Microsoft.UI.Xaml.DependencyObject> Nodes(Microsoft.UI.Xaml.DependencyObject root)
        {
            yield return root;
            for (var i = 0; i < Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(root); i++)
                foreach (var child in Nodes(Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(root, i))) yield return child;
        }
        async Task Wait(Func<bool> ready)
        {
            var until = DateTime.UtcNow.AddSeconds(45);
            while (!ready()) { if (DateTime.UtcNow > until) throw new IOException("Retain this original uncommitted caption review."); await Task.Delay(50); }
        }
        try
        {
            foreach (var name in new[] { "en", "fa", "de" })
            foreach (var value in new[] { ThemeChoice.Light, ThemeChoice.Dark })
            foreach (var width in new[] { 360, 412, 1280 })
            foreach (var blocked in blockedOnly ? new[] { true } : [false, true])
            {
                var stage = name + "-" + value + "-" + width + (blocked ? "-blocked" : "-review");
                await SetStoreThemeAsync(app, theme, value); await SetLanguageAsync(app, local, name);
                app.Windows[0].Width = width; app.Windows[0].Height = 820;
                var model = new EntryEditorViewModel(store, translator, local, time, new UndoService(store, time),
                    new AssetEntryConfirmation(translator, interaction), interaction, new ReceiptWorkStore(
                        fixture.GetRequiredService<Microsoft.EntityFrameworkCore.IDbContextFactory<ZananceDbContext>>(),
                        fixture.GetRequiredService<ICommercialWriteAccessSource>()));
                var page = new EntryEditorPage(model); var route = "original-caption-" + Guid.NewGuid().ToString("N");
                Routing.RegisterRoute(route, new WorkflowPageFactory(page));
                await Shell.Current.GoToAsync(route, new Dictionary<string, object> { ["account"] = aggregate.AccountId });
                await Wait(() => model.IsFormEditable);
                var amount = blocked ? 40000 : 100;
                model.AmountText = MoneyText.ForInput(amount, "EUR", local.CurrentCulture);
                var expectedMessage = translator.Format("Aggregate_Overlap", aggregate.Title!,
                    MoneyText.Format(aggregate.Amount, "EUR", local.CurrentCulture), MoneyText.Format(amount, "EUR", local.CurrentCulture),
                    MoneyText.Format(Math.Max(0, aggregate.Amount - amount), "EUR", local.CurrentCulture));
                if (blocked) expectedMessage += "\n\n" + translator["Aggregate_LinkUnavailable"] + "\n\n" + translator["Import_AggregateKept"];
                var command = model.SaveCommand;
                var button = VisualDescendants(page).OfType<Button>().Single(b => ReferenceEquals(b.Command, command));
                await Wait(() => button.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.Button { IsEnabled: true, ActualWidth: >= 44, ActualHeight: >= 44 });
                var native = (Microsoft.UI.Xaml.Controls.Button)button.Handler!.PlatformView!;
                if (FrameworkElementAutomationPeer.CreatePeerForElement(native)?.GetPattern(PatternInterface.Invoke) is not IInvokeProvider save)
                    throw new IOException("Original native preview Invoke unavailable.");
                save.Invoke();
                Microsoft.UI.Xaml.Controls.TextBlock? caption = null; IInvokeProvider? cancel = null; IInvokeProvider? next = null;
                Microsoft.UI.Xaml.DependencyObject[] OwnedElements()
                {
                    if (app.Windows[0].Handler?.PlatformView is not Microsoft.UI.Xaml.Window { Content.XamlRoot: { } root }) return [];
                    return [.. Microsoft.UI.Xaml.Media.VisualTreeHelper.GetOpenPopupsForXamlRoot(root).SelectMany(p => Nodes(p.Child))];
                }
                IInvokeProvider? NativeAction(IEnumerable<Microsoft.UI.Xaml.DependencyObject> elements, string text) => elements
                    .OfType<Microsoft.UI.Xaml.FrameworkElement>().Select(FrameworkElementAutomationPeer.CreatePeerForElement)
                    .Where(p => p?.GetName() == text).Select(p => p!.GetPattern(PatternInterface.Invoke)).OfType<IInvokeProvider>().FirstOrDefault();
                await Wait(() =>
                {
                    var elements = OwnedElements();
                    caption = elements.OfType<Microsoft.UI.Xaml.Controls.TextBlock>().FirstOrDefault(t => t.Text == expectedMessage);
                    cancel = NativeAction(elements, translator["Common_Cancel"]); next = NativeAction(elements, translator["Common_Next"]);
                    return caption is { IsLoaded: true, ActualWidth: > 0, ActualHeight: > 0 } && cancel is not null
                        && (blocked ? NativeAction(elements, translator["Import_KeepBothShort"]) is not null : next is not null);
                });
                if (caption!.IsTextTrimmed || OwnedElements().OfType<Microsoft.UI.Xaml.Controls.TextBlock>()
                    .Any(t => t.Text == translator["Import_KeepBothShort"] && t.IsTextTrimmed))
                    throw new IOException("The realized original aggregate caption or action is trimmed.");
                var output = Path.Combine(folder, stage); Directory.CreateDirectory(output);
                await CaptureWindowAsync(app.Windows[0], Path.Combine(output, "workflow.png"));
                captures.Add(new { Language = stage, File = "workflow.png" });
                var observation = new { Stage = stage, caption.Text, caption.ActualWidth, caption.ActualHeight, caption.IsTextTrimmed,
                    OriginalSourceRetained = true, NativeChoice = "Cancel", ProcessId = Environment.ProcessId };
                await File.WriteAllTextAsync(Path.Combine(output, "before-cancel.json"), JsonSerializer.Serialize(observation));
                if (!blocked)
                {
                    await File.WriteAllTextAsync(Path.Combine(output, "before-next.json"), JsonSerializer.Serialize(new { Stage = stage, CompleteOriginalAmountsReviewed = true }));
                    next!.Invoke();
                    await Wait(() =>
                    {
                        var elements = OwnedElements(); cancel = NativeAction(elements, translator["Common_Cancel"]);
                        return !elements.OfType<Microsoft.UI.Xaml.Controls.TextBlock>().Any(t => t.Text == expectedMessage)
                            && NativeAction(elements, translator["Aggregate_Replace"]) is not null && cancel is not null;
                    });
                }
                cancel!.Invoke();
                if (command.ExecutionTask is { } pending) await pending.WaitAsync(TimeSpan.FromSeconds(30));
                if (model.FormAccepted || model.IsBusy || model.SaveError is not null || baseline != await ImportFinancialNativeReview.StoredAsync(fixture, CancellationToken.None))
                    throw new IOException("A declined original overlap changed financial or document ownership.");
                await File.WriteAllTextAsync(Path.Combine(output, "complete.json"), JsonSerializer.Serialize(new { Observation = observation, FinancialAndDocumentRowsUnchanged = true, NativeCancelConsumed = true }));
                cases.Add(observation);
                await Shell.Current.GoToAsync("..", false); Routing.UnRegisterRoute(route);
                if (blockedOnly)
                {
                    foreach (var host in new[] { "home", "transactions" })
                    {
                        var displayOffer = new UndoService(store, time);
                        Page noticePage; Task loaded;
                        if (host == "home")
                        {
                            var homeModel = ActivatorUtilities.CreateInstance<HomeViewModel>(services, store, displayOffer);
                            noticePage = new HomePage(homeModel); loaded = Task.CompletedTask;
                        }
                        else
                        {
                            var listModel = new TransactionsViewModel(store, translator, local, services.GetRequiredService<IDateFormatter>(), time, displayOffer, interaction);
                            noticePage = new TransactionsPage(listModel); loaded = Task.CompletedTask;
                        }
                        var noticeRoute = "original-undo-caption-" + Guid.NewGuid().ToString("N");
                        Routing.RegisterRoute(noticeRoute, new WorkflowPageFactory(noticePage)); await Shell.Current.GoToAsync(noticeRoute);
                        if (noticePage is HomePage homePage) loaded = homePage.PendingPublication;
                        await loaded;
                        await Wait(() => noticePage.BindingContext is HomeViewModel { Loading.IsReady: true }
                            or TransactionsViewModel { Loading.IsReady: true });
                        // Presentation-only offer in this fictitious native layout case. No Undo command is invoked.
                        displayOffer.Offer(() => Task.CompletedTask, time.GetUtcNow(), UndoNoticeKind.AggregateAdjusted);
                        var label = noticePage.FindByName<Label>("UndoCaption");
                        Microsoft.UI.Xaml.Controls.TextBlock? text = null;
                        await Wait(() => (text = label.Handler?.PlatformView as Microsoft.UI.Xaml.Controls.TextBlock)
                            is { ActualWidth: > 0, ActualHeight: > 0 } && text.Text == translator["Aggregate_UndoNotice"]);
                        if (text!.IsTextTrimmed) throw new IOException("The original transient Undo meaning is trimmed.");
                        var noticeStage = stage + "-" + host + "-undo"; var noticeOutput = Path.Combine(folder, noticeStage);
                        Directory.CreateDirectory(noticeOutput); await CaptureWindowAsync(app.Windows[0], Path.Combine(noticeOutput, "workflow.png"));
                        captures.Add(new { Language = noticeStage, File = "workflow.png" });
                        var noticeObservation = new { Stage = noticeStage, text.Text, text.ActualWidth, text.ActualHeight, text.IsTextTrimmed,
                            PresentationOnly = true, NativeUndoInvocations = 0, ProcessId = Environment.ProcessId };
                        cases.Add(noticeObservation);
                        displayOffer.Dismiss();
                        if (baseline != await ImportFinancialNativeReview.StoredAsync(fixture, CancellationToken.None))
                            throw new IOException("An Undo caption display fixture changed stored rows.");
                        await File.WriteAllTextAsync(Path.Combine(noticeOutput, "complete.json"), JsonSerializer.Serialize(noticeObservation));
                        await Shell.Current.GoToAsync("..", false); Routing.UnRegisterRoute(noticeRoute);
                    }
                }
            }
        }
        catch (Exception error)
        { await File.WriteAllTextAsync(Path.Combine(folder, "primary-error.txt"), error.ToString()); throw; }
        finally { await SetStoreThemeAsync(app, theme, themeChoice); await SetLanguageAsync(app, local, language.CultureName); }
        if (originalBaseRows != await ImportFinancialNativeReview.StoredAsync(services, CancellationToken.None))
            throw new IOException("The isolated caption workflow changed its base installation.");
        await File.WriteAllTextAsync(Path.Combine(folder, "complete.json"), JsonSerializer.Serialize(new { Captures = captures, Cases = cases,
            Package = AppInfo.Current.PackageName, ProcessId = Environment.ProcessId, OnlySeparateFictitiousLedgerChanged = true,
            OriginalBaseStoreUnchanged = true, OriginalFinancialRowsUnchanged = true, NativeFinancialSaves = 0, NoAcceptedCommandReplayed = true }));
    }
}
#endif
