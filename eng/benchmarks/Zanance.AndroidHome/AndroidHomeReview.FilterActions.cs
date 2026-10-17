using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Transactions;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    /// <summary>Invokes original saved filters and checks native bounds on independently owned fictitious inputs only.</summary>
    private static async Task<object> ReviewFilterActionsAsync(App app, IServiceProvider services, string root, string comparison, CancellationToken ct)
    {
        // Await actual native root attachment before another root can be queued. A logical ShellRebuilt
        // notification precedes Android fragment attachment; arbitrary delays do not establish that boundary.
        async Task WaitRootAsync()
        {
            for (var attempt = 0; attempt < 300; attempt++)
            {
                if (app.Windows[0].Page?.Handler?.PlatformView is Android.Views.View { IsAttachedToWindow: true } rootView
                    && Shell.Current?.CurrentPage?.Handler?.PlatformView is Android.Views.View { IsAttachedToWindow: true } pageView)
                {
                    await WaitLayoutAsync(rootView, ct); await WaitLayoutAsync(pageView, ct); return;
                }
                await Task.Delay(50, ct);
            }
            throw new ReviewCheckException("Original review Shell did not attach its native page.");
        }
        app.OpenReviewShell(); await WaitRootAsync();
        var path = Path.Combine(root, "filter-actions.db");
        var continuing = File.Exists(path);
        using var fixture = new ServiceCollection().AddZananceData(path).BuildServiceProvider();
        await Task.Run(() => fixture.MigrateLocalDatabase<ZananceDbContext>(), ct);
        var store = fixture.GetRequiredService<ZananceStore>(); var time = services.GetRequiredService<TimeProvider>();
        var today = DateOnly.FromDateTime(time.GetLocalNow().DateTime);
        var account = new Account { Name = "Fictitious original filter account", CurrencyCode = "EUR", OpeningDate = today, OpeningBalance = 10000 };
        if (!continuing) await store.SaveAccountAsync(account, ct);
        if (!continuing) await store.SaveEntryAsync(new LedgerEntry { AccountId = account.Id, Kind = EntryKind.Expense, Amount = 1234,
            Date = today, Title = "Fictitious retained filter result", Tags = ["retained"] }, cancellationToken: ct);
        string[] names = ["Fictitious original transaction filter with complete long name", "Fiktiver gespeicherter Transaktionsfilter mit langem Namen", "فیلتر آزمایشی تراکنش‌های اصلی با نام کامل و توضیح بلند"];
        if (!continuing) foreach (var name in names) await store.SaveSavedFilterAsync(new SavedFilter { Name = name, Period = 3, Search = "#retained" }, ct);
        async Task<string> StoredAsync() => JsonSerializer.Serialize(new { Settings = await store.GetSettingsAsync(ct),
            Accounts = await store.GetAccountsAsync(cancellationToken: ct), Entries = await store.GetEntriesAsync(cancellationToken: ct), Filters = await store.GetSavedFiltersAsync(ct) });
        if (continuing)
        {
            var accounts = await store.GetAccountsAsync(cancellationToken: ct); var entries = await store.GetEntriesAsync(cancellationToken: ct);
            var filters = await store.GetSavedFiltersAsync(ct);
            if (accounts.Count != 1 || accounts[0].Name != account.Name || accounts[0].OpeningBalance != 10000 || accounts[0].CurrencyCode != "EUR"
                || entries.Count != 1 || entries[0].AccountId != accounts[0].Id || entries[0].Amount != 1234 || entries[0].Kind != EntryKind.Expense
                || entries[0].Title != "Fictitious retained filter result" || !entries[0].Tags.SequenceEqual(new[] { "retained" })
                || filters.Count != names.Length || !filters.Select(value => value.Name).Order().SequenceEqual(names.Order())
                || filters.Any(value => value.Search != "#retained" || value.Period != 3))
                throw new ReviewCheckException("Retained failed fixture differs; never replace its financial inputs.");
        }
        var before = await StoredAsync(); var translator = services.GetRequiredService<Translator>();
        var local = services.GetRequiredService<ILocalizationService>(); var theme = services.GetRequiredService<ThemeService>();
        var observations = new List<object>();
        static IEnumerable<Microsoft.Maui.Controls.View> Views(IVisualTreeElement element)
        {
            if (element is Microsoft.Maui.Controls.View view) yield return view;
            foreach (var child in element.GetVisualChildren()) foreach (var nested in Views(child)) yield return nested;
        }
        foreach (var choice in new[] { ThemeChoice.Light, ThemeChoice.Dark })
        {
            var rebuilt = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            void Rebuilt(object? sender, EventArgs args) => rebuilt.TrySetResult();
            app.ShellRebuilt += Rebuilt;
            try { theme.Set(choice); await rebuilt.Task.WaitAsync(TimeSpan.FromSeconds(30), ct); }
            finally { app.ShellRebuilt -= Rebuilt; }
            await WaitRootAsync();
            foreach (var language in new[] { "en", "fa", "de" })
            {
                await SetLanguageAsync(app, local, language, ct); await WaitRootAsync();
                var vm = new TransactionsViewModel(store, translator, local, new DateFormatter(local), time,
                    new UndoService(store, time), services.GetRequiredService<IAppInteraction>());
                var page = new TransactionsPage(vm); await Shell.Current.Navigation.PushAsync(page); await vm.LoadAsync();
                vm.IsFilterOpen = true; await Task.Delay(500, ct);
                var buttons = Views(page).OfType<Button>().Where(value => ReferenceEquals(value.Command, vm.ApplySavedFilterCommand)).ToArray();
                if (buttons.Length != names.Length) throw new ReviewCheckException("Original native filter set is incomplete.");
                foreach (var button in buttons)
                {
                    var saved = (SavedFilter)button.CommandParameter;
                    if (button.Handler?.PlatformView is not Android.Views.View native)
                        throw new ReviewCheckException("Original filter native target missing.");
                    await WaitLayoutAsync(native, ct);
                    // MAUI exposes Semantics through its accessibility delegate, not View.ContentDescription.
                    // Read the actual node used by the platform, as the independent UI hierarchy confirms.
                    using (var node = native.CreateAccessibilityNodeInfo())
                        if (node?.ContentDescription != saved.Name)
                            throw new ReviewCheckException("Original filter native accessible name missing.");
                    native.RequestRectangleOnScreen(new Android.Graphics.Rect(0, 0, native.Width, native.Height), true);
                    await Task.Delay(250, ct); await WaitLayoutAsync(native, ct);
                    var density = native.Resources!.DisplayMetrics!.Density;
                    var visible = new Android.Graphics.Rect();
                    if (!native.GetGlobalVisibleRect(visible) || visible.Width() + 1 < native.Width || visible.Height() + 1 < native.Height
                        || native.Width / density < 43.5 || native.Height / density < 43.5)
                        throw new ReviewCheckException("Original saved filter is outside its actual viewport or touch target.");
                    foreach (var label in Views((IVisualTreeElement)button.Parent).OfType<Label>())
                    {
                        if (label.Handler?.PlatformView is not Android.Widget.TextView text || text.Layout is not { } layout)
                            throw new ReviewCheckException("Original saved caption has no native text layout.");
                        for (var line = 0; line < layout.LineCount; line++)
                            if (layout.GetEllipsisCount(line) != 0 || layout.GetLineRight(line) > text.Width - text.TotalPaddingRight + 1)
                                throw new ReviewCheckException("Original saved caption is truncated.");
                    }
                    if (!native.PerformClick()) throw new ReviewCheckException("Original native filter click refused.");
                    await Task.Delay(250, ct);
                    if (vm.SearchText != "#retained" || vm.PeriodIndex != 3 || vm.Days.SelectMany(day => day).Count() != 1)
                        throw new ReviewCheckException("Original saved filter query changed.");
                    observations.Add(new { Language = language, Theme = choice.ToString(), NativeWidth = native.Width / density,
                        NativeHeight = native.Height / density, CompleteNameAndCaption = true, NativeClickAndOriginalQuery = true });
                }
                if (before != await StoredAsync()) throw new ReviewCheckException("Filter-only actions changed stored finance or saved inputs.");
                await Shell.Current.Navigation.PopAsync();
            }
        }
        return new { Comparison = comparison, OriginalNativePageAndActions = true, FictitiousIndependentFile = true,
            CompleteStoredInputsUnchanged = true, RetainedFailedFixture = continuing, NoMainDataTouched = true, NoSystemSettingsChanged = true, Observations = observations };
    }
}
