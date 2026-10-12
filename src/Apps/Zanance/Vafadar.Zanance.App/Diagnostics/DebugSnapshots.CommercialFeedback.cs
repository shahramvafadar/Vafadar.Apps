#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Transactions;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Supplies only fictitious menu/name effects; error presentation delegates to the actual native adapter.</summary>
    private sealed class CommercialReviewInteraction(IAppInteraction native, Translator translator) : IAppInteraction
    {
        /// <summary>Gets the original retry name observed by the fictitious input effect.</summary>
        internal string? LastInitial;
        /// <inheritdoc />
        public Task<string?> ChooseAsync(string title, string cancel, params string[] actions) => Task.FromResult<string?>(translator["Filter_SaveCurrent"]);
        /// <inheritdoc />
        public Task<string?> PromptAsync(string title, string message, string accept, string cancel, string? initialValue = null, int maxLength = -1)
        { LastInitial = initialValue; return Task.FromResult<string?>("  Fictitious retained filter name  "); }
        /// <inheritdoc />
        public Task AlertAsync(string title, string message, string cancel) => native.AlertAsync(title, message, cancel);
        /// <inheritdoc />
        public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel) => throw new InvalidOperationException("No replacement confirmation belongs to this fictitious case.");
        /// <inheritdoc />
        public Task NavigateAsync(string route, IDictionary<string, object>? parameters = null) => throw new InvalidOperationException("A rejected filter must not navigate.");
        /// <inheritdoc />
        public Task ShowFailureAsync(Exception exception) => native.ShowFailureAsync(exception);
    }
    // D-160 / AT-162: actual native saved-filter commands and dialogs use only an independent fictitious file.
    private static async Task ReviewCommercialFeedbackAsync(App app, IServiceProvider services, string folder, string[] languages)
    {
        var path = Path.Combine(folder, "commercial-feedback.db"); var access = new ActiveItemsReviewAccess();
        await using var fixture = new ServiceCollection().AddSingleton<TimeProvider>(TimeProvider.System)
            .AddSingleton<ICommercialWriteAccessSource>(access).AddZananceData(path).BuildServiceProvider();
        fixture.MigrateLocalDatabase<ZananceDbContext>(); var store = fixture.GetRequiredService<ZananceStore>();
        var day = DateOnly.FromDateTime(DateTime.Now);
        var account = new Account { Name = "Fictitious retained filter funding", CurrencyCode = "EUR", OpeningDate = day, OpeningBalance = 10000 };
        await store.SaveAccountAsync(account);
        await store.SaveEntryAsync(new LedgerEntry { AccountId = account.Id, Kind = EntryKind.Expense, Amount = 1234, Date = day,
            Title = "Fictitious original search result", Tags = ["retained"] });
        await store.SaveSavedFilterAsync(new SavedFilter { Name = "Fictitious existing saved query", Search = "#original" });
        await store.GetSettingsAsync();
        access.Current = new(path, new(ProductPlan.Free, new(EntitlementScopeKind.PersonalProfile, Guid.Parse("79797979-4545-5656-6767-898989898989"))));
        var factory = fixture.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        async Task<string> StoredAsync()
        {
            await using var db = await factory.CreateDbContextAsync(); await db.Database.OpenConnectionAsync();
            using var command = db.Database.GetDbConnection().CreateCommand(); var tables = new List<string>();
            command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name";
            using (var reader = await command.ExecuteReaderAsync()) while (await reader.ReadAsync()) tables.Add(reader.GetString(0));
            if (tables.Count != 25) throw new InvalidOperationException("Incomplete commercial-feedback fixture schema.");
            var all = new SortedDictionary<string, List<string>>();
            foreach (var table in tables)
            {
                command.CommandText = "SELECT * FROM \"" + table + "\""; using var reader = await command.ExecuteReaderAsync(); var rows = new List<string>();
                while (await reader.ReadAsync()) { var values = new object?[reader.FieldCount]; for (var i = 0; i < values.Length; i++) values[i] = reader.IsDBNull(i) ? null : reader.GetValue(i); rows.Add(JsonSerializer.Serialize(values)); }
                rows.Sort(StringComparer.Ordinal); all[table] = rows;
            }
            return JsonSerializer.Serialize(all);
        }
        var before = await StoredAsync(); var translator = services.GetRequiredService<Translator>();
        var localization = services.GetRequiredService<ILocalizationService>();
        var interaction = new CommercialReviewInteraction(services.GetRequiredService<IAppInteraction>(), translator);
        foreach (var language in languages)
        {
            await SetLanguageAsync(app, localization, language);
            var vm = new TransactionsViewModel(store, translator, localization, new DateFormatter(localization), TimeProvider.System,
                new UndoService(store, TimeProvider.System), interaction);
            var page = new TransactionsPage(vm); ProfileNativeReads.Add(page);
            await Shell.Current.Navigation.PushAsync(page); await vm.LoadAsync(); await Task.Delay(250);
            vm.SearchText = "#retained complete unsaved search"; vm.IsFilterOpen = true; await Task.Delay(350);
            var original = (vm.SearchText, vm.PeriodIndex, vm.KindIndex, vm.UnreviewedOnly, vm.SelectedAccount?.Id);
            var observers = Failures.Observed; CommercialWriteRejectedException? observed = null;
            Failures.Observed = exception =>
            {
                if (exception is CommercialWriteRejectedException { Quota: QuotaKind.SavedFilters, Maximum: 1, Current: 1, Requested: 1 } rejected)
                    observed = rejected;
                else observers?.Invoke(exception);
            };
            try
            {
                if (app.Windows[0].Handler?.PlatformView is not Microsoft.UI.Xaml.Window { Content.XamlRoot: { } root })
                    throw new InvalidOperationException("Missing own commercial dialog root.");
                async Task<Microsoft.UI.Xaml.Controls.ContentDialog> DialogAsync(string title)
                {
                    for (var attempt = 0; attempt < 100; attempt++)
                    {
                        var current = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetOpenPopupsForXamlRoot(root).Select(popup => popup.Child)
                            .OfType<Microsoft.UI.Xaml.Controls.ContentDialog>().SingleOrDefault();
                        if (current?.Title as string == title) { ProfileNativeReads.Add(current); await Task.Delay(150); return current; }
                        await Task.Delay(50);
                    }
                    var pendingDialogs = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetOpenPopupsForXamlRoot(root).Select(popup => popup.Child)
                        .OfType<Microsoft.UI.Xaml.Controls.ContentDialog>().Select(dialog => new { TitleType = dialog.Title?.GetType().FullName,
                            Title = dialog.Title?.ToString(), dialog.PrimaryButtonText, dialog.SecondaryButtonText, dialog.CloseButtonText }).ToArray();
                    await File.WriteAllTextAsync(Path.Combine(folder, language + "-commercial-dialog-negative.json"), JsonSerializer.Serialize(new
                    { ExpectedTitle = title, pendingDialogs, CommandFinished = vm.SavedFilterMenuCommand.ExecutionTask?.IsCompleted }));
                    await CaptureWindowAsync(app.Windows[0], Path.Combine(folder, language + "-commercial-dialog-negative.png"));
                    throw new InvalidOperationException("The expected current native commercial dialog did not open: " + title);
                }
                void Answer(Microsoft.UI.Xaml.Controls.ContentDialog dialog, string caption)
                {
                    var button = FindNativeButton(dialog, caption) ?? throw new InvalidOperationException("Missing native commercial dialog answer.");
                    var peer = FrameworkElementAutomationPeer.CreatePeerForElement(button) as ButtonAutomationPeer;
                    if (peer?.GetPattern(PatternInterface.Invoke) is not IInvokeProvider invoke)
                        throw new InvalidOperationException("Missing native commercial answer Invoke pattern.");
                    ProfileNativeReads.Add(button); ProfileNativeReads.Add(peer); ProfileNativeReads.Add(invoke); invoke.Invoke();
                }
                IEnumerable<Microsoft.UI.Xaml.DependencyObject> NativeTree(Microsoft.UI.Xaml.DependencyObject element)
                {
                    yield return element;
                    for (var i = 0; i < Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(element); i++)
                        foreach (var child in NativeTree(Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(element, i))) yield return child;
                }
                var target = VisualDescendants(page).OfType<Button>().Single(button => button.Command == vm.SavedFilterMenuCommand);
                if (FindScrollView(page) is { } scroll) await ScrollToViewIfNeededAsync(scroll, target);
                InvokeChoiceReview(target, ProfileNativeReads);
                var failure = await DialogAsync(translator["Commercial_ActionUnavailableTitle"]); var message = CommercialFailureMessage.Describe(observed
                    ?? throw new InvalidOperationException("The real original writer rejection was not observed."), translator)!;
                if (failure.Title as string != translator["Commercial_ActionUnavailableTitle"])
                    throw new InvalidOperationException("The native rejection retained a generic error title.");
                var blocks = NativeTree(failure).OfType<Microsoft.UI.Xaml.Controls.TextBlock>().Where(block => !string.IsNullOrWhiteSpace(block.Text)).ToArray();
                if (!blocks.Any(block => block.Text == message) || blocks.Any(block => block.IsTextTrimmed))
                    throw new InvalidOperationException("The native reason was missing or trimmed.");
                await CaptureWindowAsync(app.Windows[0], Path.Combine(folder, language + "-commercial-feedback-native-dialog.png"));
                Answer(failure, translator["Common_Ok"]);
                if (vm.SavedFilterMenuCommand.ExecutionTask is { } pending) await pending;
                InvokeChoiceReview(target, ProfileNativeReads);
                var retry = await DialogAsync(translator["Commercial_ActionUnavailableTitle"]);
                if (interaction.LastInitial != "  Fictitious retained filter name  ")
                    throw new InvalidOperationException("The next real menu command lost its raw original name.");
                Answer(retry, translator["Common_Ok"]);
                if (vm.SavedFilterMenuCommand.ExecutionTask is { } retried) await retried;
                if (original != (vm.SearchText, vm.PeriodIndex, vm.KindIndex, vm.UnreviewedOnly, vm.SelectedAccount?.Id)
                    || !ReferenceEquals(Shell.Current.CurrentPage, page) || await StoredAsync() != before)
                    throw new InvalidOperationException("Native rejection discarded the original form or wrote complete rows.");
                await File.WriteAllTextAsync(Path.Combine(folder, language + "-commercial-feedback-proof.json"), JsonSerializer.Serialize(new
                { ActualTransactionsTargetAndWriter = true, NativeTranslatedReasonAndClose = true, FictitiousMenuAndNameEffectPorts = true,
                    ActualSecondInvokeRereadsAndRetainsRawName = true, OriginalSearchScopeRetained = true,
                    Complete25TablesUnchanged = true, NoFinancialSaveOrCommercialActivation = true,
                    DialogTitle = failure.Title, message, NativeText = blocks.Select(block => new { block.Text, block.FontSize, block.ActualWidth, block.ActualHeight, block.IsTextTrimmed }) }));
            }
            finally { Failures.Observed = observers; InvokeSnapshotBack(page); await Task.Delay(150); }
        }
    }
}
#endif
