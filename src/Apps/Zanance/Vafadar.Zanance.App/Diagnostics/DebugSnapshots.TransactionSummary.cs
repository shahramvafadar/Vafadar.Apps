#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Maui.Localization;
using Vafadar.Zanance.App.Features.Transactions;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Checks the actual transaction summary on an independent fictitious store without financial UI writes.</summary>
    private static async Task ReviewTransactionSummaryAsync(App app, IServiceProvider services, string folder)
    {
        if (AppInfo.Current.PackageName != "pro.vafadar.zanance.transactionsummary"
            || !Path.GetFullPath(folder).StartsWith(Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(),
                "artifacts", "transaction-summary-d326")) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new IOException("Independent transaction summary review identity and output required.");
        var path = Path.Combine(folder, "transaction-summary.db");
        var continuing = File.Exists(path);
        if (File.Exists(Path.Combine(folder, "transaction-summary-proof.json"))) throw new IOException("Do not repeat completed summary qualification.");
        using var fixture = new ServiceCollection().AddZananceData(path).BuildServiceProvider();
        fixture.MigrateLocalDatabase<ZananceDbContext>();
        var store = fixture.GetRequiredService<ZananceStore>();
        var time = services.GetRequiredService<TimeProvider>();
        var today = DateOnly.FromDateTime(time.GetLocalNow().DateTime);
        var source = new Account { Name = "Fictitious original source", CurrencyCode = "EUR", OpeningDate = today, OpeningBalance = 10000 };
        var destination = new Account { Name = "Fictitious original destination", CurrencyCode = "EUR", OpeningDate = today };
        if (!continuing)
        {
            await store.SaveAccountAsync(source); await store.SaveAccountAsync(destination);
            await store.SaveEntryAsync(new LedgerEntry { AccountId = source.Id, ToAccountId = destination.Id,
                Kind = EntryKind.Transfer, Amount = 100, Date = today, Title = "Fictitious transfer" });
            await store.SaveEntryAsync(new LedgerEntry { AccountId = source.Id, Kind = EntryKind.Expense,
                Amount = 100, Date = today, Title = "Fictitious expense" });
            await store.SaveEntryAsync(new LedgerEntry { AccountId = source.Id, Kind = EntryKind.Refund,
                Amount = 100, Date = today, Title = "Fictitious refund" });
        }
        var original = await store.GetEntriesAsync();
        if (original.Count != 3 || original.Any(row => row.Amount != 100)
            || original.Count(row => row.Kind == EntryKind.Transfer) != 1
            || original.Count(row => row.Kind == EntryKind.Expense) != 1
            || original.Count(row => row.Kind == EntryKind.Refund) != 1)
            throw new IOException("Preserve the original independent summary fixture.");
        async Task<string> StoredAsync() => JsonSerializer.Serialize(new { Accounts = await store.GetAccountsAsync(), Entries = await store.GetEntriesAsync() });
        var before = await StoredAsync();
        var translator = services.GetRequiredService<Translator>();
        var localization = services.GetRequiredService<ILocalizationService>();
        var observations = new List<object>();
        var progressPath = Path.Combine(folder, "completed-contexts.json");
        var completed = File.Exists(progressPath)
            ? JsonSerializer.Deserialize<List<string>>(await File.ReadAllTextAsync(progressPath)) ?? throw new IOException("Invalid original summary progress.")
            : new List<string>();
        var retainedCount = completed.Count;
        var window = app.Windows[0];
        var themes = services.GetRequiredService<ThemeService>();
        foreach (var language in new[] { "en", "fa", "de" })
        foreach (var theme in new[] { ThemeChoice.Light, ThemeChoice.Dark })
        foreach (var width in new[] { 360, 412, 1280 })
        {
            var context = language + "-" + theme + "-" + width;
            if (completed.Contains(context)) continue;
            themes.Set(theme); window.Width = width; window.Height = 820;
            await SetLanguageAsync(app, localization, language);
            var vm = new TransactionsViewModel(store, translator, localization, new DateFormatter(localization), time,
                new UndoService(store, time), services.GetRequiredService<IAppInteraction>());
            var page = new TransactionsPage(vm).WithFlowDirection(localization);
            var root = window.Page ?? throw new IOException("Original review root missing.");
            await root.Navigation.PushModalAsync(new NavigationPage(page).WithFlowDirection(localization), false);
            await vm.LoadAsync();
            vm.KindIndex = (int)KindFilter.Transfers; await Task.Delay(300);
            if (vm.SummaryText != translator.Format("Report_TagCount", 1) || vm.Days.SelectMany(day => day).Count() != 1)
                throw new IOException("Transfer summary retained an empty total or lost its original row.");
            var label = VisualDescendants(page).OfType<Label>().Single(value => value.Text == vm.SummaryText && Visible(value));
            await CaptureAsync(app, folder, language + "-" + theme + "-" + width + "-transfer-summary");
            CheckCaption(label);
            if (label.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.TextBlock native || native.Text != NativeDigits.Apply(vm.SummaryText))
                throw new IOException("The actual summary caption was not realized.");
            observations.Add(new { Language = language, Theme = theme.ToString(), Width = width, Kind = "transfer", native.Text, native.ActualWidth, native.ActualHeight });
            vm.KindIndex = (int)KindFilter.Expenses; await Task.Delay(200);
            var expected = translator.Format("Tx_FilterTotal", 2, Core.Money.MoneyText.Format(0, "EUR", localization.CurrentCulture, showPlus: true));
            if (vm.SummaryText != expected || vm.Days.SelectMany(day => day).Count() != 2)
                throw new IOException("A real zero result lost its currency packet or original rows.");
            label = VisualDescendants(page).OfType<Label>().Single(value => value.Text == vm.SummaryText && Visible(value));
            await CaptureAsync(app, folder, language + "-" + theme + "-" + width + "-zero-summary");
            CheckCaption(label);
            observations.Add(new { Language = language, Theme = theme.ToString(), Width = width, Kind = "zero-result", Text = label.Text, CaptionWidth = label.Width, CaptionHeight = label.Height });
            await root.Navigation.PopModalAsync(false); await Task.Delay(100);
            completed.Add(context);
            await File.WriteAllTextAsync(progressPath, JsonSerializer.Serialize(completed));
        }
        if (before != await StoredAsync()) throw new IOException("Summary review changed original stored inputs.");
        await File.WriteAllTextAsync(Path.Combine(folder, "transaction-summary-proof.json"), JsonSerializer.Serialize(new
        { OriginalPageAndViewModel = true, TransferCountOnly = true, RealZeroTotalRetained = true, StoredInputsUnchanged = true, RetainedCompleteContexts = retainedCount, CompletedContexts = completed, Observations = observations }));
    }
}
#endif
