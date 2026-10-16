#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Media;
using Vafadar.Localization;
using Vafadar.Maui.Controls;
using Vafadar.Maui.Localization;
using Vafadar.Zanance.App.Features.Commerce.Billing;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.App.Presentation;
using NativeNode = Microsoft.UI.Xaml.DependencyObject;
using NativeElement = Microsoft.UI.Xaml.FrameworkElement;
using NativeButton = Microsoft.UI.Xaml.Controls.Button;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>New actual identity form, native invalid-submit and Cancel only; no account, password or token is submitted.</summary>
    private static async Task ReviewBillingIdentityLayoutAsync(App app, IServiceProvider services, string folder)
    {
        if (AppInfo.Current.PackageName != "pro.vafadar.zanance.billinglayout" || !Path.GetFullPath(folder).Equals(
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "artifacts", "billing-identity-layout-d295")), StringComparison.OrdinalIgnoreCase))
            throw new IOException("Exact independent identity layout required.");
        var complete = Path.Combine(folder, "identity-layout.complete.json");
        if (File.Exists(complete)) throw new IOException("Never replay completed identity layout.");
        var progress = Path.Combine(folder, "identity-layout-cases.json");
        var cases = File.Exists(progress) ? JsonSerializer.Deserialize<List<BillingLayoutCase>>(await File.ReadAllTextAsync(progress)) ?? throw new IOException() : [];
        var localization = services.GetRequiredService<ILocalizationService>(); var text = services.GetRequiredService<Translator>();
        var themes = services.GetRequiredService<ThemeService>(); var window = app.Windows[0];
        foreach (var language in new[] { "en", "fa", "de" })
        foreach (var theme in new[] { ThemeChoice.Light, ThemeChoice.Dark })
        foreach (var width in new[] { 360, 412, 1280 })
        {
            themes.Set(theme); await SetLanguageAsync(app, localization, language); window.Width = width; window.Height = 820;
            foreach (var mode in Enumerable.Range(0, 5))
            {
                var key = new BillingLayoutCase(language, theme.ToString(), width, mode.ToString(System.Globalization.CultureInfo.InvariantCulture));
                if (cases.Contains(key)) continue;
                var effects = new IdentityLayoutEffects();
                var model = new BillingIdentityViewModel(effects, text, services.GetRequiredService<IAppInteraction>());
                var page = new BillingIdentityPage(model, localization).WithFlowDirection(localization);
                var root = window.Page ?? throw new IOException();
                await root.Navigation.PushModalAsync(new NavigationPage(page).WithFlowDirection(localization), false);
                try
                {
                    for (var wait = 0; wait < 160 && !model.CanSubmit; wait++) await Task.Delay(25);
                    if (!model.CanSubmit || !model.Connected) throw new IOException("Original identity form did not publish its fictional session hint.");
                    var chips = VisualDescendants(page).OfType<ChoiceChips>().Single(); var reads = new List<object>();
                    if (mode != 0)
                    {
                        InvokeChoiceReview(VisualDescendants(chips).OfType<Button>().ElementAt(mode), reads); await Task.Delay(75);
                        if (model.ModeIndex != mode) throw new IOException("Original native identity action did not select.");
                    }
                    var submit = VisualDescendants(page).OfType<WrappingAction>().Single(action => ReferenceEquals(action.Command, model.SubmitCommand));
                    var actualSubmit = VisualDescendants(submit).OfType<Button>().Single();
                    if (actualSubmit.Handler?.PlatformView is not NativeButton { ActualWidth: >= 44, ActualHeight: >= 44 } nativeSubmit ||
                        FrameworkElementAutomationPeer.CreatePeerForElement(nativeSubmit)?.GetName() != model.SubmitText)
                        throw new IOException("Original identity action target/name differs.");
                    InvokeChoiceReview(actualSubmit, reads); await Task.Delay(100);
                    if (model.ShowEmail && model.EmailError is null || model.ShowPassword && model.PasswordError is null ||
                        model.ShowChallenge && (model.ChallengeError is null || model.TokenError is null) || effects.Submissions != 0)
                        throw new IOException("Empty native submit must display every field error and make no request.");
                    var scroll = VisualDescendants(page).OfType<ScrollView>().Single(); var index = 0;
                    foreach (var label in VisualDescendants(scroll).OfType<Label>().Where(Visible))
                    {
                        if (label.Text is not { Length: > 1 }) continue; // Decorative single glyphs retain their existing exemption.
                        await ScrollToViewIfNeededAsync(scroll, label); await Task.Delay(25); CheckCaption(label, reads);
                        await CaptureWindowAsync(window, Path.Combine(folder, $"{language}-{theme}-{width}-mode{mode}-{index++}.png"));
                    }
                    foreach (var button in VisualDescendants(chips).OfType<Button>())
                        if (button.Handler?.PlatformView is not NativeButton { ActualWidth: >= 44, ActualHeight: >= 44 })
                            throw new IOException("Identity choice native target is too small.");
                    if (mode == 0)
                    {
                        var signOut = VisualDescendants(page).OfType<WrappingAction>().Single(action => ReferenceEquals(action.Command, model.SignOutCommand));
                        InvokeChoiceReview(VisualDescendants(signOut).OfType<Button>().Single(), reads);
                        if (window.Handler?.PlatformView is not Microsoft.UI.Xaml.Window { Content.XamlRoot: { } xamlRoot }) throw new IOException();
                        NativeElement? popup = null;
                        for (var wait = 0; wait < 160 && popup is null; wait++)
                        {
                            popup = VisualTreeHelper.GetOpenPopupsForXamlRoot(xamlRoot).Select(p => p.Child).OfType<NativeElement>()
                                .FirstOrDefault(p => p.ActualWidth > 0 && Nodes(p).OfType<NativeButton>().Any(b => FrameworkElementAutomationPeer.CreatePeerForElement(b)?.GetName() == text["Common_Cancel"]));
                            if (popup is null) await Task.Delay(25);
                        }
                        if (popup is null) throw new IOException("Original sign-out confirmation missing.");
                        var cancel = Nodes(popup).OfType<NativeButton>().Single(b => FrameworkElementAutomationPeer.CreatePeerForElement(b)?.GetName() == text["Common_Cancel"]);
                        if (FrameworkElementAutomationPeer.CreatePeerForElement(cancel)?.GetPattern(PatternInterface.Invoke) is not IInvokeProvider invoke)
                            throw new IOException("Original native Cancel required.");
                        await CaptureWindowAsync(window, Path.Combine(folder, $"{language}-{theme}-{width}-signout-cancel.png")); invoke.Invoke();
                        for (var wait = 0; wait < 160 && !model.CanSignOut; wait++) await Task.Delay(25);
                        if (!model.CanSignOut || effects.Submissions != 0) throw new IOException("Cancel must preserve the original identity hint.");
                    }
                    if (model.Password.Length != 0 || model.Token.Length != 0 || effects.Submissions != 0) throw new IOException("No secret or identity submission belongs to layout review.");
                    await File.WriteAllTextAsync(Path.Combine(folder, $"{language}-{theme}-{width}-mode{mode}-proof.json"),
                        JsonSerializer.Serialize(new { NativeReads = reads.Count, Mode = mode, Effects = effects.Submissions, RealIdentityOrStoreAcceptance = false }));
                    cases.Add(key); await File.WriteAllTextAsync(progress, JsonSerializer.Serialize(cases));
                }
                finally { await root.Navigation.PopModalAsync(false); }
            }
        }
        await File.WriteAllTextAsync(complete, JsonSerializer.Serialize(new { Cases = cases.Count, Modes = 5, NativeInvalidSubmit = true,
            NativeSignOutCancel = true, IdentitiesCreated = 0, SecretsSubmitted = false, RealIdentityOrStoreAcceptance = false }));

        static bool Visible(Label label)
        { for (Element? row = label; row is not null; row = row.Parent) if (row is VisualElement { IsVisible: false }) return false; return true; }
        static IEnumerable<NativeNode> Nodes(NativeNode node)
        {
            yield return node;
            for (var index = 0; index < VisualTreeHelper.GetChildrenCount(node); index++)
                foreach (var child in Nodes(VisualTreeHelper.GetChild(node, index))) yield return child;
        }
    }
    private sealed class IdentityLayoutEffects : IBillingIdentityEnvironment
    {
        public bool Available => true;
        internal int Submissions;
        public Task<bool> ConnectedAsync(CancellationToken ct) => Task.FromResult(true);
        public Task SignInAsync(string email, string password, CancellationToken ct) => Forbidden();
        public Task<bool> SignOutAsync(CancellationToken ct) { Submissions++; throw new IOException("No real identity effect belongs to layout review."); }
        public Task RegisterAsync(string email, string password, CancellationToken ct) => Forbidden();
        public Task RecoverAsync(string email, CancellationToken ct) => Forbidden();
        public Task CompleteAsync(Guid challenge, string token, string? newPassword, CancellationToken ct) => Forbidden();
        private Task Forbidden() { Submissions++; throw new IOException("No real identity effect belongs to layout review."); }
    }
}
#endif
