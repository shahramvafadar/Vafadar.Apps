#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Media;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Maui.Localization;
using Vafadar.Zanance.App.Features.DataFiles;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Tax;
using NativeNode = Microsoft.UI.Xaml.DependencyObject;
using NativeButton = Microsoft.UI.Xaml.Controls.Button;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Actual native account selection, reviewed scope, wrapped captions and Cancel with unchanged fictitious finance.</summary>
    private static async Task ReviewAccountantAsync(App app, IServiceProvider services, string folder)
    {
        if (AppInfo.Current.PackageName != "pro.vafadar.zanance.accountantreview" || Path.GetFullPath(folder) !=
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "artifacts", "accountant-native-d296"))) throw new IOException("Exact independent accountant review required.");
        var complete = Path.Combine(folder, "complete.json"); if (File.Exists(complete)) throw new IOException("Completed native cases must not be replayed.");
        await using var fixture = new ServiceCollection().AddSingleton<TimeProvider>(TimeProvider.System)
            .AddZananceData(Path.Combine(folder, "accountant-fixture.db")).BuildServiceProvider();
        fixture.MigrateLocalDatabase<ZananceDbContext>();
        await using (var db = await fixture.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync())
        {
            if (!await db.Accounts.AnyAsync())
            {
                var account = new Account { Name = "Fictitious salary / نمونهٔ حقوق / Beispiel Gehalt", CurrencyCode = "EUR", OpeningDate = new(2026, 1, 1), IsArchived = true };
                db.Accounts.Add(account); db.Entries.Add(new() { AccountId = account.Id, Amount = 12345, Kind = EntryKind.Expense, Date = new(2026, 1, 2), Title = "Fictitious accountant record" });
                await db.SaveChangesAsync();
            }
        }
        var before = await HouseholdFinancialFingerprintAsync(fixture);
        var local = services.GetRequiredService<ILocalizationService>(); var text = services.GetRequiredService<Translator>();
        var themes = services.GetRequiredService<ThemeService>(); var window = app.Windows[0];
        var progress = Path.Combine(folder, "cases-title-v2.json");
        var finished = File.Exists(progress) ? JsonSerializer.Deserialize<List<string>>(await File.ReadAllTextAsync(progress))! : [];
        foreach (var language in new[] { "en", "fa", "de" })
        foreach (var theme in new[] { ThemeChoice.Light, ThemeChoice.Dark })
        foreach (var width in new[] { 360, 412, 1280 })
        {
            var key = $"{language}-{theme}-{width}"; if (finished.Contains(key)) continue;
            themes.Set(theme); await SetLanguageAsync(app, local, language); window.Width = width; window.Height = 820;
            var native = new AccountantLayoutNative();
            var model = new AccountantExportViewModel(fixture.GetRequiredService<AccountantExportStore>(), text, TimeProvider.System,
                services.GetRequiredService<IAppInteraction>(), native);
            var page = new AccountantExportPage(model).WithFlowDirection(local); var root = window.Page ?? throw new IOException();
            await root.Navigation.PushModalAsync(new NavigationPage(page).WithFlowDirection(local), false);
            try
            {
                for (var wait = 0; wait < 200 && (!model.Loaded || model.IsBusy); wait++) await Task.Delay(25);
                if (!model.Loaded || model.IsBusy) throw new IOException("Accountant form did not finish its original reader.");
                var reads = new List<object>(); var scroll = VisualDescendants(page).OfType<ScrollView>().Single();
                var select = VisualDescendants(page).OfType<WrappingAction>().Single(a => ReferenceEquals(a.Command, model.SelectAllCommand));
                await ScrollToViewIfNeededAsync(scroll, select); InvokeChoiceReview(VisualDescendants(select).OfType<Button>().Single(), reads);
                if (model.Accounts.Any(a => !a.Selected)) throw new IOException("Native select-all did not select actual identities.");
                var review = VisualDescendants(page).OfType<WrappingAction>().Single(a => ReferenceEquals(a.Command, model.ReviewCommand));
                InvokeChoiceReview(VisualDescendants(review).OfType<Button>().Single(), reads);
                for (var wait = 0; wait < 400 && model.IsBusy; wait++) await Task.Delay(25);
                if (!model.CanShare || model.Summary is null) throw new IOException("Actual original accountant review failed: " + model.Error);
                var index = 0;
                foreach (var label in VisualDescendants(page).OfType<Label>().Where(Visible).ToArray())
                {
                    if (label.Text is not { Length: > 1 }) continue;
                    await ScrollToViewIfNeededAsync(scroll, label); await Task.Delay(25); CheckCaption(label, reads);
                    await CaptureWindowAsync(window, Path.Combine(folder, $"{key}-{index++}.png"));
                }
                foreach (var action in VisualDescendants(page).OfType<WrappingAction>())
                {
                    if (VisualDescendants(action).OfType<Button>().Single().Handler?.PlatformView is not NativeButton { ActualWidth: >= 44, ActualHeight: >= 44 })
                        throw new IOException("Accountant action native target is smaller than44.");
                }
                var share = VisualDescendants(page).OfType<WrappingAction>().Single(a => ReferenceEquals(a.Command, model.ShareCommand));
                InvokeChoiceReview(VisualDescendants(share).OfType<Button>().Single(), reads);
                if (window.Handler?.PlatformView is not Microsoft.UI.Xaml.Window { Content.XamlRoot: { } xamlRoot }) throw new IOException();
                NativeButton? cancel = null;
                for (var wait = 0; wait < 200 && cancel is null; wait++)
                {
                    cancel = VisualTreeHelper.GetOpenPopupsForXamlRoot(xamlRoot).SelectMany(p => Nodes(p.Child)).OfType<NativeButton>()
                        .FirstOrDefault(b => FrameworkElementAutomationPeer.CreatePeerForElement(b)?.GetName() == text["Common_Cancel"]);
                    if (cancel is null) await Task.Delay(25);
                }
                if (cancel is null || FrameworkElementAutomationPeer.CreatePeerForElement(cancel)?.GetPattern(PatternInterface.Invoke) is not IInvokeProvider invoke) throw new IOException("Native readable-ZIP Cancel unavailable.");
                await CaptureWindowAsync(window, Path.Combine(folder, key + "-confirm-cancel.png")); invoke.Invoke();
                for (var wait = 0; wait < 200 && model.IsBusy; wait++) await Task.Delay(25);
                if (!model.CanShare || native.Writes != 0 || await HouseholdFinancialFingerprintAsync(fixture) != before)
                    throw new IOException("Cancel changed original data, wrote a ZIP or retired valid review.");
                finished.Add(key); await File.WriteAllTextAsync(progress, JsonSerializer.Serialize(finished));
                await File.WriteAllTextAsync(Path.Combine(folder, key + "-proof.json"), JsonSerializer.Serialize(new { NativeReads = reads.Count, FinancialHash = before, NativeSelectionReviewCancel = true, FilesWritten = native.Writes }));
            }
            finally { await root.Navigation.PopModalAsync(false); }
        }
        await File.WriteAllTextAsync(complete, JsonSerializer.Serialize(new { Cases = finished.Count, FinancialHash = before }));
        static IEnumerable<NativeNode> Nodes(NativeNode node)
        {
            yield return node;
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++) foreach (var child in Nodes(VisualTreeHelper.GetChild(node, i))) yield return child;
        }
    }
    private sealed class AccountantLayoutNative : IAccountantExportRuntime
    {
        internal int Writes;
        public void EnsureAvailable() { }
        public Task<bool> ConfirmAsync() => throw new IOException("Native Cancel must precede authentication.");
        public Task<string> WriteAsync(Func<Stream, Task> write) { Writes++; throw new IOException("Native Cancel must precede cache output."); }
        public Task ShareAsync(string path) => throw new IOException("No outside share belongs to own-window layout review.");
        public void Remove(string path) => throw new IOException("No file belongs to Cancel.");
    }
}
#endif
