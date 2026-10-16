#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Media;
using Vafadar.Localization;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.App.Presentation;
using NativeElement = Microsoft.UI.Xaml.FrameworkElement;
using NativeNode = Microsoft.UI.Xaml.DependencyObject;
using NativeText = Microsoft.UI.Xaml.Controls.TextBlock;
using NativeButton = Microsoft.UI.Xaml.Controls.Button;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Only original dialog rendering and native Cancel are exercised; no transfer state, participant consent, key, role or HTTP operation is manufactured.</summary>
    private static async Task ReviewHouseholdTransferDialogsAsync(App app, IServiceProvider services, string folder)
    {
        var promptOnly = Path.GetFullPath(folder).Equals(Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "artifacts", "household-transfer-prompt-rtl-d294")), StringComparison.OrdinalIgnoreCase);
        if (AppInfo.Current.PackageName != "pro.vafadar.zanance.householdlifecycle" ||
            (!promptOnly && !Path.GetFullPath(folder).Equals(Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "artifacts", "household-transfer-dialog-layout-d294")), StringComparison.OrdinalIgnoreCase)))
            throw new IOException("Exact independently owned dialog review required.");
        const string financial = "24ff6624bc3efb92b49dbfa70568c41b413790ef184a95a0b355d1a52edad6c7";
        if (await HouseholdFinancialFingerprintAsync(services) != financial) throw new IOException("Preserve original main dataset.");
        var complete = Path.Combine(folder, "dialog-layout.complete.json");
        if (File.Exists(complete)) throw new IOException("Never replay completed dialog geometry.");
        var localization = services.GetRequiredService<ILocalizationService>(); var translator = services.GetRequiredService<Translator>();
        var themes = services.GetRequiredService<ThemeService>(); var interaction = services.GetRequiredService<MauiAppInteraction>();
        if (interaction.ReviewPublicDevicePrompt is not null) throw new IOException("No public input handoff belongs to this readonly review.");
        var progress = Path.Combine(folder, "dialog-layout-cases.json");
        var cases = File.Exists(progress) ? JsonSerializer.Deserialize<List<HouseholdUiCase>>(await File.ReadAllTextAsync(progress)) ?? throw new IOException() : [];
        var observations = new List<object>();
        foreach (var language in promptOnly ? new[] { "fa" } : new[] { "en", "fa", "de" })
        foreach (var theme in new[] { ThemeChoice.Light, ThemeChoice.Dark })
        foreach (var width in new[] { 360, 412, 1280 })
        {
            if (cases.Any(row => row.Language == language && row.Theme == theme.ToString() && row.Width == width)) continue;
            themes.Set(theme); await SetLanguageAsync(app, localization, language); app.Windows[0].Width = width; app.Windows[0].Height = 820;
            var title = translator["HouseholdOwnership_Title"]; var cancel = translator["Common_Cancel"]; var confirm = translator["Sync_Confirm"];
            var dispositions = new[] { translator["HouseholdOwnership_Leave"], translator["Household_Viewer"], translator["Household_Editor"] };
            if (!promptOnly)
            {
                var choice = interaction.ChooseAsync(title, cancel, dispositions);
                await InspectAndCancelAsync("former-role", [title, cancel, .. dispositions]);
                if (await choice is { } selected && selected != cancel) throw new IOException("A dialog review must never choose a role.");
            }
            var prompt = interaction.PromptAsync(title, translator["Household_DeviceFingerprintPrompt"], confirm, cancel, maxLength: 64);
            await InspectAndCancelAsync("public-fingerprint", [title, translator["Household_DeviceFingerprintPrompt"], confirm, cancel]);
            if (await prompt is not null) throw new IOException("No fingerprint is submitted in a geometry review.");
            foreach (var disposition in promptOnly ? Array.Empty<string>() : dispositions)
            {
                var caption = translator.Format("Household_ApprovedDevice", "df8f98b5-121e-412a-b996-b69b19808e35", translator["Household_Editor"],
                    "b11964856b1b959e01d753abe4c4459dcc6067e6f9a02aa679718b88a329b1e3");
                var body = caption + "\n\n" + disposition + "\n\n" + translator["HouseholdOwnership_Confirm"];
                var confirmation = interaction.ConfirmAsync(title, body, confirm, cancel);
                await InspectAndCancelAsync("confirmation-" + Array.IndexOf(dispositions, disposition), [title, body, confirm, cancel]);
                if (await confirmation) throw new IOException("A geometry review must never confirm a transfer.");
            }
            if (await HouseholdFinancialFingerprintAsync(services) != financial) throw new IOException("Dialog review changed original finance.");
            cases.Add(new(language, theme.ToString(), width, true));
            await File.WriteAllTextAsync(progress, JsonSerializer.Serialize(cases));

            async Task InspectAndCancelAsync(string stage, string[] captions)
            {
                if (app.Windows[0].Handler?.PlatformView is not Microsoft.UI.Xaml.Window { Content.XamlRoot: { } root }) throw new IOException();
                NativeElement? popup = null;
                for (var wait = 0; wait < 160 && popup is null; wait++)
                {
                    popup = VisualTreeHelper.GetOpenPopupsForXamlRoot(root).Select(row => row.Child).OfType<NativeElement>()
                        .FirstOrDefault(row => row.ActualWidth > 0 && row.ActualHeight > 0 && Nodes(row).OfType<NativeButton>()
                            .Any(button => FrameworkElementAutomationPeer.CreatePeerForElement(button)?.GetName() == cancel));
                    if (popup is null) await Task.Delay(50);
                }
                if (popup is null) throw new IOException("Original native dialog did not attach.");
                popup.UpdateLayout(); var native = Nodes(popup).OfType<NativeElement>().Where(Visible).ToArray();
                foreach (var caption in captions.Distinct(StringComparer.Ordinal))
                {
                    var labels = native.OfType<NativeText>().Where(row => row.Text == caption).ToArray();
                    if (labels.Length == 0) throw new IOException("Original native caption missing: " + stage);
                    foreach (var label in labels)
                    {
                        if (label.IsTextTrimmed || !label.IsTextScaleFactorEnabled || label.ActualWidth < 1 || label.ActualHeight < 1) throw new IOException("Incomplete native caption: " + stage);
                        if (promptOnly && label.FlowDirection != Microsoft.UI.Xaml.FlowDirection.RightToLeft) throw new IOException("The native Persian prompt must inherit the original page direction.");
                        var glyphs = new List<Windows.Foundation.Rect>();
                        for (var offset = 0; offset <= label.ContentEnd.Offset - label.ContentStart.Offset; offset++)
                            if (label.ContentStart.GetPositionAtOffset(offset, Microsoft.UI.Xaml.Documents.LogicalDirection.Forward) is { } pointer)
                                glyphs.Add(pointer.GetCharacterRect(Microsoft.UI.Xaml.Documents.LogicalDirection.Forward));
                        observations.Add(new { language, Theme = theme.ToString(), width, stage, Kind = "caption", label.ActualWidth, label.ActualHeight, Glyphs = glyphs.Count });
                        await File.WriteAllTextAsync(Path.Combine(folder, "dialog-layout-observations.json"), JsonSerializer.Serialize(observations));
                        if (glyphs.Count == 0 || glyphs.Min(row => row.Left) < -1 || glyphs.Max(row => row.Right) > label.ActualWidth + 1 || glyphs.Max(row => row.Bottom) > label.ActualHeight + 1)
                            throw new IOException("Realized original dialog glyphs exceed their allocation: " + stage);
                    }
                }
                foreach (var button in native.OfType<NativeButton>().Where(row => row.ActualWidth > 0 && row.ActualHeight > 0))
                {
                    var name = FrameworkElementAutomationPeer.CreatePeerForElement(button)?.GetName();
                    if (string.IsNullOrWhiteSpace(name)) continue;
                    observations.Add(new { language, Theme = theme.ToString(), width, stage, Kind = "button", Name = name, button.ActualWidth, button.ActualHeight });
                    await File.WriteAllTextAsync(Path.Combine(folder, "dialog-layout-observations.json"), JsonSerializer.Serialize(observations));
                    if (button.ActualWidth < 44 || button.ActualHeight < 44) throw new IOException("Original native dialog target is below44px: " + stage);
                }
                await CaptureWindowAsync(app.Windows[0], Path.Combine(folder, $"{language}-{theme}-{width}-{stage}.png"));
                var close = native.OfType<NativeButton>().Single(button => FrameworkElementAutomationPeer.CreatePeerForElement(button)?.GetName() == cancel);
                if (FrameworkElementAutomationPeer.CreatePeerForElement(close)?.GetPattern(PatternInterface.Invoke) is not IInvokeProvider invoke) throw new IOException("Original native Cancel Invoke required.");
                invoke.Invoke();
                for (var wait = 0; wait < 160 && VisualTreeHelper.GetOpenPopupsForXamlRoot(root).Any(row => ReferenceEquals(row.Child, popup)); wait++) await Task.Delay(25);
                if (VisualTreeHelper.GetOpenPopupsForXamlRoot(root).Any(row => ReferenceEquals(row.Child, popup))) throw new IOException("Retain the original active dialog.");
            }
        }
        await File.WriteAllTextAsync(complete, JsonSerializer.Serialize(new { Cases = cases.Count, DialogsPerCase = promptOnly ? 1 : 5, NativeObservations = observations.Count,
            OriginalNativeDialogs = true, OnlyNativeCancelInvoked = true, FingerprintSubmitted = false, ProtocolStateOrKeysCreated = false,
            NetworkRequested = false, FinancialHash = financial, PhoneOrReleaseAcceptance = false }));

        static IEnumerable<NativeNode> Nodes(NativeNode node)
        {
            yield return node;
            for (var index = 0; index < VisualTreeHelper.GetChildrenCount(node); index++)
                foreach (var child in Nodes(VisualTreeHelper.GetChild(node, index))) yield return child;
        }
        static bool Visible(NativeElement element)
        {
            for (NativeNode? current = element; current is not null; current = VisualTreeHelper.GetParent(current))
                if (current is NativeElement ancestor && (ancestor.Visibility != Microsoft.UI.Xaml.Visibility.Visible || ancestor.Opacity <= 0)) return false;
            return true;
        }
    }
}
#endif
