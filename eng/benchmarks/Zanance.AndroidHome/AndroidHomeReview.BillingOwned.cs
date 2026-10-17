using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Maui.Localization;
using Vafadar.Zanance.App.Features.Commerce;
using Vafadar.Zanance.App.Features.Commerce.Billing;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Billing;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    /// <summary>Checks actual original purchase-history labels on independent fictitious metadata, without store or payment effects.</summary>
    private static async Task<object> ReviewOwnedBillingAsync(App app, IServiceProvider services, string root, string comparison, CancellationToken ct)
    {
        async Task RootAsync()
        {
            for (var i = 0; i < 300; i++)
            {
                if (Shell.Current?.CurrentPage?.Handler?.PlatformView is Android.Views.View { IsAttachedToWindow: true } native)
                { await WaitLayoutAsync(native, ct); return; }
                await Task.Delay(50, ct);
            }
            throw new ReviewCheckException("Original review root did not attach.");
        }
        app.OpenReviewShell(); await RootAsync();
        using var data = new ServiceCollection().AddZananceData(Path.Combine(root, "owned-billing.db")).BuildServiceProvider();
        await Task.Run(() => data.MigrateLocalDatabase<ZananceDbContext>(), ct);
        var store = data.GetRequiredService<ZananceStore>();
        async Task<string> StoredAsync() => JsonSerializer.Serialize(new { Settings = await store.GetSettingsAsync(), Accounts = await store.GetAccountsAsync(), Entries = await store.GetEntriesAsync() });
        var before = await StoredAsync(); var local = services.GetRequiredService<ILocalizationService>();
        var translator = services.GetRequiredService<Translator>(); var themes = services.GetRequiredService<ThemeService>();
        var observations = new List<object>();
        static IEnumerable<Microsoft.Maui.Controls.View> Views(IVisualTreeElement node)
        {
            if (node is Microsoft.Maui.Controls.View view) yield return view;
            foreach (var child in node.GetVisualChildren()) foreach (var childView in Views(child)) yield return childView;
        }
        foreach (var theme in new[] { ThemeChoice.Light, ThemeChoice.Dark })
        {
            var rebuilt = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            void Rebuilt(object? sender, EventArgs args) => rebuilt.TrySetResult();
            app.ShellRebuilt += Rebuilt;
            try { themes.Set(theme); await rebuilt.Task.WaitAsync(TimeSpan.FromSeconds(30), ct); }
            finally { app.ShellRebuilt -= Rebuilt; }
            await RootAsync();
            foreach (var language in new[] { "en", "fa", "de" })
            {
                await SetLanguageAsync(app, local, language, ct); await RootAsync();
                var vm = new ProductPlansViewModel(data.GetRequiredService<CommercialAccessStore>(), translator,
                    services.GetRequiredService<IAppInteraction>(), new OwnedBillingEffect(), new DateFormatter(local));
                var page = new ProductPlansPage(vm, local).WithFlowDirection(local);
                await Shell.Current.Navigation.PushAsync(page); await vm.LoadAsync();
                if (vm.BillingOwnedPurchases.Count != 3 || vm.CanBuyBilling) throw new ReviewCheckException("Original verified history or no-payment state differs.");
                var scroll = Views(page).OfType<ScrollView>().Single(); var captions = vm.BillingOwnedPurchases.SelectMany(row => new[] { row.Title, row.Details }).ToArray();
                foreach (var caption in captions)
                {
                    var label = Views(page).OfType<Label>().Single(value => value.Text == caption);
                    await scroll.ScrollToAsync(label, ScrollToPosition.Start, false); await Task.Delay(100, ct);
                    if (label.Handler?.PlatformView is not Android.Widget.TextView native) throw new ReviewCheckException("Original purchase text is not realized.");
                    await WaitLayoutAsync(native, ct);
                    if (native.Layout is not { } layout || layout.Height > native.Height - native.TotalPaddingTop - native.TotalPaddingBottom + 1)
                        throw new ReviewCheckException("Original purchase paragraph is clipped vertically.");
                    for (var line = 0; line < layout.LineCount; line++)
                        if (layout.GetEllipsisCount(line) != 0 || layout.GetLineRight(line) > native.Width - native.TotalPaddingRight + 1 || layout.GetLineLeft(line) < -1)
                            throw new ReviewCheckException("Original purchase paragraph is clipped horizontally.");
                }
                if (before != await StoredAsync()) throw new ReviewCheckException("Purchase history display changed original stored inputs.");
                observations.Add(new { Language = language, Theme = theme.ToString(), OriginalNativeParagraphs = captions.Length, CompleteTextLayout = true, NoStoreOrFinancialEffect = true });
                vm.DeactivateBilling(); await Shell.Current.Navigation.PopAsync();
            }
        }
        return new { Comparison = comparison, OriginalProductPlansPage = true, NoMainDataTouched = true, NoSystemSettingsChanged = true, OriginalStoredInputsUnchanged = true, ActualStoreAcceptance = false, Observations = observations };
    }
    private sealed class OwnedBillingEffect : IProductBillingEnvironment
    {
        public bool Available => true;
        public Task<ProductBillingPresentation> LoadAsync(CancellationToken ct) => Task.FromResult(OwnedBillingReviewState());
        public Task<ProductBillingPresentation> PurchaseAsync(NativeBillingOffer offer, CancellationToken ct) => throw new ReviewCheckException("No purchase belongs to metadata review.");
        public Task<ProductBillingPresentation> RestoreAsync(CancellationToken ct) => throw new ReviewCheckException("No restore belongs to metadata review.");
        public Task<ProductBillingPresentation> ContinueAsync(CancellationToken ct) => throw new ReviewCheckException("No continuation belongs to metadata review.");
        public Task ManageAsync(CancellationToken ct) => throw new ReviewCheckException("No management belongs to metadata review.");
    }
    private static ProductBillingPresentation OwnedBillingReviewState()
    {
        var now = new DateTimeOffset(2026, 10, 20, 0, 0, 0, TimeSpan.Zero);
        var grants = new[]
        {
            new BillingCachedGrant(BillingIssuer.GooglePlay, BillingEnvironment.Sandbox,
                new(BillingProduct.PlusMonthly, BillingPurchaseState.Active, now.AddMonths(-1), now.AddDays(7), null, false, false, true, "fictional-v1",
                    ScheduledChange: new(BillingProduct.ProYearly, now.AddDays(7), "fictional-v2")), false, now),
            new BillingCachedGrant(BillingIssuer.GooglePlay, BillingEnvironment.Sandbox,
                new(BillingProduct.ProYearly, BillingPurchaseState.Grace, now.AddYears(-1), now.AddDays(-1), now.AddDays(3), true, false, true, "fictional-v1"), true, now),
            new BillingCachedGrant(BillingIssuer.AppStore, BillingEnvironment.Sandbox,
                new(BillingProduct.PlusLifetime, BillingPurchaseState.Active, now.AddYears(-1), null, null, false, false, true, "fictional-v1"), true, now)
        };
        foreach (var grant in grants) grant.Facts.Validate();
        var view = new BillingClientView(new(1, BillingSnapshotKind.Current, Guid.NewGuid(), Guid.NewGuid(), new string('a', 64),
            BillingIssuer.GooglePlay, BillingEnvironment.Sandbox, null, Guid.NewGuid(), 0, now, grants, false),
            new(ProductPlan.Free, true, true), false, null, false);
        return new(ProductBillingStatus.VerificationUnavailable, view, null, [], true, false);
    }
}
