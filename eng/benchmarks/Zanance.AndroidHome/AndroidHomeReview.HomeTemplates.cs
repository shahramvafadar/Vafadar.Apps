using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Zanance.App.Features.Home;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    /// <summary>Exercises original Home template publication, rebinding and legitimate removal without financial commands.</summary>
    private static async Task<object> ReviewHomeTemplatesAsync(App app, IServiceProvider services, string root, string comparison, CancellationToken ct)
    {
        var path = Path.Combine(root, "home-templates.db");
        using var fixture = new ServiceCollection().AddZananceData(path).BuildServiceProvider();
        if (!File.Exists(path))
        {
            await Task.Run(() => fixture.MigrateLocalDatabase<ZananceDbContext>(), ct);
            var seed = fixture.GetRequiredService<ZananceStore>();
            var account = new Account { Name = "Fictitious template account", CurrencyCode = "EUR", OpeningDate = new(2026, 10, 1) };
            await seed.SaveAccountAsync(account, ct);
            foreach (var name in new[] { "Coffee", "Groceries" })
                await seed.SaveTemplateAsync(new EntryTemplate { Name = name, Kind = EntryKind.Expense, AccountId = account.Id, Amount = 350 }, ct);
        }
        var store = fixture.GetRequiredService<ZananceStore>();
        async Task<string> StoredAsync() => JsonSerializer.Serialize(new { Accounts = await store.GetAccountsAsync(cancellationToken: ct),
            Entries = await store.GetEntriesAsync(cancellationToken: ct), Settings = await store.GetSettingsAsync(ct), Templates = await store.GetTemplatesAsync(ct) });
        var before = await StoredAsync(); var observations = new List<object>();
        var local = services.GetRequiredService<ILocalizationService>(); var theme = services.GetRequiredService<ThemeService>();
        await AwaitReimbursementFocusAsync(ct); app.OpenReviewShell();
        foreach (var choice in new[] { ThemeChoice.Light, ThemeChoice.Dark })
        {
            var rebuilt = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            void Rebuilt(object? sender, EventArgs args) => rebuilt.TrySetResult();
            app.ShellRebuilt += Rebuilt;
            try { theme.Set(choice); await rebuilt.Task.WaitAsync(TimeSpan.FromSeconds(30), ct); }
            finally { app.ShellRebuilt -= Rebuilt; }
            foreach (var language in new[] { "en", "fa", "de" })
            {
                await SetLanguageAsync(app, local, language, ct); await AwaitReimbursementFocusAsync(ct);
                var vm = ActivatorUtilities.CreateInstance<HomeViewModel>(services, store);
                var page = new HomePage(vm) { FlowDirection = local.CurrentLanguage.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
                await Shell.Current.Navigation.PushAsync(page, false); await page.PendingPublication;
                var strip = ReimbursementViews(page).OfType<HorizontalStackLayout>()
                    .Single(view => ReferenceEquals(BindableLayout.GetItemsSource(view), vm.QuickTemplates));
                var viewport = page.FindByName<ScrollView>("ContentViewport");
                await viewport.ScrollToAsync(strip, ScrollToPosition.MakeVisible, false);
                var roots = strip.Children.OfType<Microsoft.Maui.Controls.View>().ToArray();
                var native = roots.Select(view => view.Handler?.PlatformView).ToArray();
                if (roots.Length != 2 || native.Any(value => value is null)) throw new ReviewCheckException("Original native templates unavailable.");
                await vm.LoadAsync();
                if (strip.Children.Count != roots.Length || roots.Where((view, index) => !ReferenceEquals(view, strip.Children[index])
                    || !ReferenceEquals(native[index], view.Handler?.PlatformView)).Any())
                    throw new ReviewCheckException("Unchanged original native template controls recreated.");
                var originals = vm.QuickTemplates.ToArray();
                foreach (var count in new[] { 2, 1, 0, 2, 0, 2 })
                {
                    var rows = originals.Reverse().Take(count).Select(item => item with { Name = item.Name + " retained",
                        Description = item.Description + " retained", IconColor = Palette.WarningText, CanUse = false }).ToArray();
                    vm.QuickTemplates.ReplaceAll(rows); vm.HasQuickTemplates = rows.Length != 0;
                    await Task.Delay(100, ct);
                    if (strip.Children.Count != count) throw new ReviewCheckException("Original template count differs.");
                    foreach (var row in rows)
                    {
                        var rootView = strip.Children.OfType<Microsoft.Maui.Controls.View>().Single(view => ReferenceEquals(view.BindingContext, row));
                        var button = ReimbursementViews(rootView).OfType<Button>().Single();
                        var label = ReimbursementViews(rootView).OfType<Label>().Single(value => value.Text == row.Name);
                        if (button.IsEnabled || !ReferenceEquals(button.CommandParameter, row) || !ReferenceEquals(button.Command, vm.UseTemplateCommand)
                            || SemanticProperties.GetDescription(button) != row.Description || label.Handler?.PlatformView is not Android.Widget.TextView text
                            || text.Text != row.Name)
                            throw new ReviewCheckException("Original rebound template metadata or command differs.");
                    }
                }
                await vm.LoadAsync(); await viewport.ScrollToAsync(strip, ScrollToPosition.MakeVisible, false);
                await AwaitReimbursementFocusAsync(ct);
                if (!vm.Loading.IsReady || before != await StoredAsync()) throw new ReviewCheckException("Original stored inputs changed.");
                observations.Add(new { Language = language, Theme = choice.ToString(), OriginalNativeRowsRetained = true,
                    CountTransitions = new[] { 2, 1, 0, 2, 0, 2 }, FreshMetadataAndCommands = true, ActualWindowFocus = true });
                page.RetirePublication(); await Shell.Current.Navigation.PopAsync(false);
            }
        }
        return new { Comparison = comparison, Observations = observations, StoredInputsUnchanged = before == await StoredAsync(), FinancialCommandsInvoked = false };
    }
}
