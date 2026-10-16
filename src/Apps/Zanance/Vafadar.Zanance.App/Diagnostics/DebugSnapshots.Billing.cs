#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Media;
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
using Vafadar.Zanance.Data.Commerce;
using NativeNode = Microsoft.UI.Xaml.DependencyObject;
using NativeElement = Microsoft.UI.Xaml.FrameworkElement;
using NativeButton = Microsoft.UI.Xaml.Controls.Button;
using NativeText = Microsoft.UI.Xaml.Controls.TextBlock;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Actual main comparison page and native Cancel only, with fictional store effects and an independent financial file.</summary>
    private static async Task ReviewBillingLayoutAsync(App app, IServiceProvider services, string folder)
    {
        var noteOnly = Environment.GetEnvironmentVariable("VAFADAR_SNAPSHOT_ONLY") == "billing-comparison-note";
        var retryOnly = Environment.GetEnvironmentVariable("VAFADAR_SNAPSHOT_ONLY") == "billing-retry-layout";
        var captionOnly = Environment.GetEnvironmentVariable("VAFADAR_SNAPSHOT_ONLY") == "billing-dialog-caption";
        var replacementOnly = Environment.GetEnvironmentVariable("VAFADAR_SNAPSHOT_ONLY") == "billing-replacement-layout";
        var restoreOnly = captionOnly || Environment.GetEnvironmentVariable("VAFADAR_SNAPSHOT_ONLY") == "billing-recovery-layout";
        if (AppInfo.Current.PackageName != "pro.vafadar.zanance.billinglayout" || !Path.GetFullPath(folder).Equals(
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "artifacts", replacementOnly ? "billing-replacement-layout-d295" : captionOnly ? "billing-dialog-caption-d295" : restoreOnly ? "billing-recovery-layout-d295" : retryOnly ? "billing-retry-layout-d295" : noteOnly ? "billing-comparison-note-d295" : "billing-layout-d295")), StringComparison.OrdinalIgnoreCase))
            throw new IOException("Exact independently owned billing layout required.");
        var complete = Path.Combine(folder, "billing-layout.complete.json");
        if (File.Exists(complete)) throw new IOException("Never replay completed billing layout.");
        var progress = Path.Combine(folder, "billing-layout-cases.json");
        var cases = File.Exists(progress) ? JsonSerializer.Deserialize<List<BillingLayoutCase>>(await File.ReadAllTextAsync(progress)) ?? throw new IOException() : [];
        await using var fixture = new ServiceCollection().AddSingleton<TimeProvider>(TimeProvider.System)
            .AddZananceData(Path.Combine(folder, "billing-layout-fixture.db")).BuildServiceProvider();
        fixture.MigrateLocalDatabase<ZananceDbContext>();
        var before = await HouseholdFinancialFingerprintAsync(fixture);
        var localization = services.GetRequiredService<ILocalizationService>(); var translator = services.GetRequiredService<Translator>();
        var themes = services.GetRequiredService<ThemeService>(); var window = app.Windows[0];
        foreach (var language in new[] { "en", "fa", "de" })
        foreach (var theme in new[] { ThemeChoice.Light, ThemeChoice.Dark })
        foreach (var width in new[] { 360, 412, 1280 })
        foreach (var status in replacementOnly ? new[] { ProductBillingStatus.Ready, ProductBillingStatus.ManageExisting } : restoreOnly ? new[] { ProductBillingStatus.RecoveryRequired } : retryOnly ? new[] { ProductBillingStatus.VerificationUnavailable, ProductBillingStatus.NotConfigured } : noteOnly ? new[] { ProductBillingStatus.Ready } : new[] { ProductBillingStatus.Ready, ProductBillingStatus.Pending, ProductBillingStatus.VerificationUnavailable, ProductBillingStatus.NotConfigured })
        {
            var key = new BillingLayoutCase(language, theme.ToString(), width, status.ToString());
            if (cases.Contains(key)) continue;
            themes.Set(theme); await SetLanguageAsync(app, localization, language);
            window.Width = width; window.Height = 820;
            var effect = new BillingLayoutStore(status, noteOnly, replacementOnly);
            var vm = new ProductPlansViewModel(fixture.GetRequiredService<CommercialAccessStore>(), translator,
                services.GetRequiredService<IAppInteraction>(), effect, services.GetRequiredService<IDateFormatter>());
            var page = new ProductPlansPage(vm, localization).WithFlowDirection(localization);
            var root = window.Page ?? throw new IOException("Original review root unavailable.");
            var navigation = new NavigationPage(page).WithFlowDirection(localization);
            await root.Navigation.PushModalAsync(navigation, false);
            try
            {
                if (noteOnly)
                {
                    for (var wait = 0; wait < 160 && (!vm.Loading.IsReady || !vm.BillingBusy); wait++) await Task.Delay(25);
                    var indicator = VisualDescendants(page).OfType<ActivityIndicator>().Single(row => row.IsRunning);
                    await ScrollToViewIfNeededAsync(VisualDescendants(page).OfType<ScrollView>().Single(), indicator); await Task.Delay(100);
                    if (!vm.BillingBusy || !indicator.IsRunning || indicator.Handler?.PlatformView is not NativeElement { ActualWidth: >= 44, ActualHeight: >= 44 })
                        throw new IOException("Native billing loading indicator is missing.");
                    await CaptureWindowAsync(window, Path.Combine(folder, $"{language}-{theme}-{width}-loading.png"));
                    effect.Release();
                }
                for (var wait = 0; wait < 160 && (!vm.Loading.IsReady || vm.BillingBusy || vm.BillingStatusText.Length == 0); wait++) await Task.Delay(25);
                if (!vm.Loading.IsReady || vm.BillingBusy || !vm.ShowBilling) throw new IOException("Original billing page is not ready.");
                var scroll = VisualDescendants(page).OfType<ScrollView>().Single(); var reads = new List<object>();
                var captions = captionOnly ? new List<string>() : retryOnly || restoreOnly ? new List<string> { vm.BillingStatusText } : noteOnly ? new List<string> { vm.AvailabilityText } : new List<string>
                    { translator["Billing_Title"], vm.BillingPlanText, vm.BillingIdentityText, vm.BillingStatusText, translator["Billing_TestNotice"] };
                if (status == ProductBillingStatus.Ready && !noteOnly)
                {
                    var offer = vm.BillingOffers.Single(); captions.Add(offer.Title); captions.Add(offer.Terms);
                }
                foreach (var caption in captions.Distinct(StringComparer.Ordinal))
                {
                    var label = VisualDescendants(page).OfType<Label>().Single(row => row.Text == caption);
                    await ScrollToViewIfNeededAsync(scroll, label); await Task.Delay(30); CheckCaption(label, reads);
                    await CaptureWindowAsync(window, Path.Combine(folder, $"{language}-{theme}-{width}-{status}-{captions.IndexOf(caption)}.png"));
                }
                foreach (var action in VisualDescendants(page).OfType<WrappingAction>().Where(action =>
                    restoreOnly ? ReferenceEquals(action.Command, vm.RestoreBillingCommand) : retryOnly ? ReferenceEquals(action.Command, vm.RetryBillingCommand) : noteOnly ? ReferenceEquals(action.Command, vm.BillingIdentityCommand) : ReferenceEquals(action.Command, vm.BuyBillingCommand) || ReferenceEquals(action.Command, vm.RestoreBillingCommand) ||
                    ReferenceEquals(action.Command, vm.ContinueBillingCommand) || ReferenceEquals(action.Command, vm.ManageBillingCommand)))
                {
                    await ScrollToViewIfNeededAsync(scroll, action); await Task.Delay(30);
                    var button = VisualDescendants(action).OfType<Button>().Single();
                    if (button.Handler?.PlatformView is not NativeButton native || native.ActualWidth < 44 || native.ActualHeight < 44 ||
                        FrameworkElementAutomationPeer.CreatePeerForElement(native)?.GetName() != action.Text)
                        throw new IOException("Original billing target/name/size differs.");
                }
                if (retryOnly)
                {
                    var retry = VisualDescendants(page).OfType<WrappingAction>().Single(action => ReferenceEquals(action.Command, vm.RetryBillingCommand));
                    await ScrollToViewIfNeededAsync(scroll, retry);
                    var beforeReads = effect.Reads; InvokeChoiceReview(VisualDescendants(retry).OfType<Button>().Single(), reads);
                    for (var wait = 0; wait < 160 && vm.BillingBusy; wait++) await Task.Delay(25);
                    if (effect.Reads != beforeReads + 1 || effect.Purchases != 0 || !vm.CanRetryBilling || vm.CanContinueBilling)
                        throw new IOException("Native retry must perform only a fresh status read without inventing a payment.");
                    await CaptureWindowAsync(window, Path.Combine(folder, $"{language}-{theme}-{width}-{status}-retry.png"));
                }
                if (status == ProductBillingStatus.Ready && !noteOnly || restoreOnly)
                {
                    var action = VisualDescendants(page).OfType<WrappingAction>().Single(row => ReferenceEquals(row.Command, restoreOnly ? vm.RestoreBillingCommand : vm.BuyBillingCommand));
                    await ScrollToViewIfNeededAsync(scroll, action); var button = VisualDescendants(action).OfType<Button>().Single();
                    InvokeChoiceReview(button, reads);
                    if (window.Handler?.PlatformView is not Microsoft.UI.Xaml.Window { Content.XamlRoot: { } xamlRoot }) throw new IOException();
                    NativeElement? popup = null;
                    for (var wait = 0; wait < 160 && popup is null; wait++)
                    {
                        popup = VisualTreeHelper.GetOpenPopupsForXamlRoot(xamlRoot).Select(p => p.Child).OfType<NativeElement>()
                            .FirstOrDefault(p => p.ActualWidth > 0 && Nodes(p).OfType<NativeButton>().Any(b => FrameworkElementAutomationPeer.CreatePeerForElement(b)?.GetName() == translator["Common_Cancel"]));
                        if (popup is null) await Task.Delay(25);
                    }
                    if (popup is null) throw new IOException("Native store review confirmation missing.");
                    var cancel = Nodes(popup).OfType<NativeButton>().Single(b => FrameworkElementAutomationPeer.CreatePeerForElement(b)?.GetName() == translator["Common_Cancel"]);
                    if (cancel.ActualWidth < 44 || cancel.ActualHeight < 44 ||
                        FrameworkElementAutomationPeer.CreatePeerForElement(cancel)?.GetPattern(PatternInterface.Invoke) is not IInvokeProvider invoke)
                        throw new IOException("Native billing Cancel target unavailable.");
                    foreach (var text in Nodes(popup).OfType<NativeText>().Where(t => t.Visibility == Microsoft.UI.Xaml.Visibility.Visible && t.Text.Length > 0))
                        if (text.IsTextTrimmed) throw new IOException("Native billing review text trimmed.");
                    if (captionOnly)
                    {
                        foreach (var target in Nodes(popup).OfType<NativeButton>().Where(button => button.ActualWidth > 0 && button.ActualHeight > 0))
                        foreach (var caption in Nodes(target).OfType<NativeText>().Where(text => text.Text.Length > 0))
                        {
                            var slot = Microsoft.UI.Xaml.Controls.Primitives.LayoutInformation.GetLayoutSlot(caption);
                            var parent = VisualTreeHelper.GetParent(caption) as NativeElement ?? throw new IOException();
                            var origin = parent.TransformToVisual(target).TransformPoint(new Windows.Foundation.Point(slot.X, slot.Y));
                            var glyphs = new List<Windows.Foundation.Rect>();
                            for (var offset = 0; offset <= caption.ContentEnd.Offset - caption.ContentStart.Offset; offset++)
                                if (caption.ContentStart.GetPositionAtOffset(offset, Microsoft.UI.Xaml.Documents.LogicalDirection.Forward) is { } pointer)
                                    glyphs.Add(pointer.GetCharacterRect(Microsoft.UI.Xaml.Documents.LogicalDirection.Forward));
                            var proof = new { caption.Text, caption.ActualWidth, caption.ActualHeight, caption.FontSize, slot, origin, glyphs,
                                ButtonWidth = target.ActualWidth, ButtonHeight = target.ActualHeight,
                                Spoken = FrameworkElementAutomationPeer.CreatePeerForElement(target)?.GetName() };
                            reads.Add(proof);
                            await File.WriteAllTextAsync(Path.Combine(folder, $"{language}-{theme}-{width}-caption-{reads.Count}.json"), JsonSerializer.Serialize(proof));
                            if (glyphs.Count == 0 || caption.IsTextTrimmed || caption.TextWrapping != Microsoft.UI.Xaml.TextWrapping.Wrap ||
                                glyphs.Min(rect => rect.Left) < -1 || glyphs.Max(rect => rect.Right) > slot.Width + 1 || glyphs.Max(rect => rect.Bottom) > slot.Height + 1 ||
                                origin.X + glyphs.Min(rect => rect.Left) < -1 || origin.X + glyphs.Max(rect => rect.Right) > target.ActualWidth + 1 ||
                                origin.Y + glyphs.Max(rect => rect.Bottom) > target.ActualHeight + 1 || target.ActualWidth < 44 || target.ActualHeight < 44)
                                throw new IOException("Realized native dialog action glyphs are clipped or outside their original target.");
                        }
                    }
                    await CaptureWindowAsync(window, Path.Combine(folder, $"{language}-{theme}-{width}-review-cancel.png"));
                    invoke.Invoke();
                    for (var wait = 0; wait < 160 && !(restoreOnly ? vm.CanRestoreBilling : vm.CanBuyBilling); wait++) await Task.Delay(25);
                    if (!(restoreOnly ? vm.CanRestoreBilling : vm.CanBuyBilling) || effect.Purchases != 0 || effect.Restores != 0) throw new IOException("Cancel must not enter store payment/restoration or retire the original action.");
                }
                if (await HouseholdFinancialFingerprintAsync(fixture) != before || effect.Purchases != 0) throw new IOException("Layout changed finance or started a native payment.");
                await File.WriteAllTextAsync(Path.Combine(folder, $"{language}-{theme}-{width}-{status}-proof.json"), JsonSerializer.Serialize(new
                { Language = language, Theme = theme.ToString(), Width = width, Status = status.ToString(), NativeReads = reads.Count,
                    NativeCancelOnly = status == ProductBillingStatus.Ready || restoreOnly, StoreEffects = effect.Purchases, FinancialHash = before, ActualStoreSandbox = false }));
                cases.Add(key); await File.WriteAllTextAsync(progress, JsonSerializer.Serialize(cases));
            }
            finally { await root.Navigation.PopModalAsync(false); }
        }
        await File.WriteAllTextAsync(complete, JsonSerializer.Serialize(new { Cases = cases.Count, FinancialHash = before,
            MainControls = true, FictionalEffectPort = true, NativeStoreCalls = 0, CurrentOrPhoneStoreAcceptance = false }));

        static IEnumerable<NativeNode> Nodes(NativeNode node)
        {
            yield return node;
            for (var index = 0; index < VisualTreeHelper.GetChildrenCount(node); index++)
                foreach (var child in Nodes(VisualTreeHelper.GetChild(node, index))) yield return child;
        }
    }
    private sealed record BillingLayoutCase(string Language, string Theme, int Width, string Status);
    private sealed class BillingLayoutStore(ProductBillingStatus status, bool hold = false, bool replacement = false) : IProductBillingEnvironment
    {
        public bool Available => true;
        internal int Purchases;
        private readonly TaskCompletionSource<ProductBillingPresentation>? held = hold ? new(TaskCreationOptions.RunContinuationsAsynchronously) : null;
        internal void Release() => held?.TrySetResult(State());
        internal int Reads;
        internal int Restores;
        public Task<ProductBillingPresentation> LoadAsync(CancellationToken ct)
        { Reads++; return held?.Task ?? Task.FromResult(State()); }
        private ProductBillingPresentation State() => new(status, null, null,
            status == ProductBillingStatus.Ready ? [new("fictional-store-choice", "Fictional Zanance Pro", "Fictional store catalog only",
                new("fictional-store-choice", BillingProduct.ProYearly, BillingOfferKind.ProTrial,
                    [new(0, "EUR", "0.00 EUR", "P14D", 1, false), new(39990000, "EUR", "39.99 EUR", "P1Y", 0, true)], true), "fictional-store-identity",
                    Replacement: replacement ? new(BillingProduct.ProMonthly, new string('a', 64), "fictional.pro", BillingReplacementTiming.Deferred,
                        new(2026, 11, 16, 0, 0, 0, TimeSpan.Zero)) : null, ExistingSubscription: replacement)] : [],
            status is ProductBillingStatus.Ready or ProductBillingStatus.Pending or ProductBillingStatus.RecoveryRequired or ProductBillingStatus.ManageExisting,
            replacement, ProRestoreRequired: replacement, OfferChanged: replacement && status == ProductBillingStatus.Ready);
        public Task<ProductBillingPresentation> PurchaseAsync(NativeBillingOffer offer, CancellationToken ct)
        { Purchases++; throw new IOException("Native effects are forbidden in layout review."); }
        public Task<ProductBillingPresentation> RestoreAsync(CancellationToken ct)
        { Restores++; throw new IOException("No purchase restore belongs to layout review."); }
        public Task<ProductBillingPresentation> ContinueAsync(CancellationToken ct) => throw new IOException("No pending purchase belongs to layout review.");
        public Task ManageAsync(CancellationToken ct) => throw new IOException("No external management navigation belongs to layout review.");
    }
}
#endif
