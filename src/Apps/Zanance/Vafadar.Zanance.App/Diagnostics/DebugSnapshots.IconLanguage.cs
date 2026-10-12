#if DEBUG && WINDOWS
using Vafadar.Localization;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

/// <summary>Native checks of original icon buttons through transient language changes without Save.</summary>
internal static partial class DebugSnapshots
{
    private static async Task ReviewIconLanguageAsync(App app, IServiceProvider services,
        Features.Entries.EntryEditorPage page, string folder, string language)
    {
        var vm = (Features.Entries.EntryEditorViewModel)page.BindingContext;
        var translator = services.GetRequiredService<Translator>();
        var store = services.GetRequiredService<ZananceStore>();
        var originalCulture = translator.Culture; var originalDirection = page.FlowDirection;
        var originalIcon = vm.IconKey; var originalDetails = vm.ShowDetails;
        string Draft() => System.Text.Json.JsonSerializer.Serialize(new
        { vm.AmountText, vm.EntryTitle, vm.Payee, vm.Note, vm.Date, vm.IconKey });
        async Task<string> StoredAsync() => System.Text.Json.JsonSerializer.Serialize(new
        { Accounts = await store.GetAccountsAsync(), Entries = await store.GetEntriesAsync(), Settings = await store.GetSettingsAsync() });
        var before = await StoredAsync();
        try
        {
            vm.ShowDetails = true; vm.IconKey = "Home";
            await Task.Delay(200);
            var picker = VisualDescendants(page).OfType<IconPicker>().Single();
            var scroll = FindScrollView(page) ?? throw new InvalidOperationException("The original icon picker has no viewport.");
            await ScrollToViewIfNeededAsync(scroll, picker);
            await Task.Delay(250);
            var buttons = VisualDescendants(picker).OfType<Button>().ToArray();
            var keys = IconPicker.Keys.Prepend(null).ToArray();
            var draft = Draft(); var changes = new List<object>();
            var invocations = 0;
            void Invoke(Button button)
            {
                if (button.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button native
                    || new Microsoft.UI.Xaml.Automation.Peers.ButtonAutomationPeer(native).GetPattern(
                        Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Invoke)
                        is not Microsoft.UI.Xaml.Automation.Provider.IInvokeProvider provider)
                    throw new InvalidOperationException("Original icon button has no native Invoke pattern.");
                provider.Invoke(); invocations++;
            }
            if (buttons.Length != keys.Length) throw new InvalidOperationException("Original icon target count changed.");
            foreach (var culture in new[] { "en", "fa", "de", language })
            {
                translator.SetCulture(System.Globalization.CultureInfo.GetCultureInfo(culture));
                page.FlowDirection = culture == "fa" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
                await Task.Delay(200);
                await ScrollToViewIfNeededAsync(scroll, picker);
                for (var i = 0; i < keys.Length; i++)
                {
                    var expected = translator[keys[i] is null ? "Icon_Default" : "Icon_" + keys[i]];
                    if (keys[i] == "Home") expected = translator.Format("Common_SelectedItem", expected);
                    if (buttons[i].Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button native)
                        throw new InvalidOperationException("Original icon button has no native handler.");
                    var peer = new Microsoft.UI.Xaml.Automation.Peers.ButtonAutomationPeer(native);
                    var actual = peer.GetName();
                    changes.Add(new { Culture = culture, Key = keys[i], Expected = expected, Actual = actual });
                    if (actual != expected || native.ActualWidth < 44 || native.ActualHeight < 44)
                    {
                        await File.WriteAllTextAsync(Path.Combine(folder, language + "-icon-language-negative.json"),
                            System.Text.Json.JsonSerializer.Serialize(changes));
                        throw new InvalidOperationException("The retained original icon target has a stale native name or small target.");
                    }
                }
                Invoke(buttons[0]);
                if (vm.IconKey is not null) throw new InvalidOperationException("Native default icon invocation lost its original selection binding.");
                Invoke(buttons[1]);
                if (vm.IconKey != "Home") throw new InvalidOperationException("Native Home icon invocation lost its original selection binding.");
                await ScrollToViewIfNeededAsync(scroll, picker);
                await CaptureAsync(app, folder, language + "-icon-language-" + culture);
                if (Draft() != draft || await StoredAsync() != before)
                    throw new InvalidOperationException("Language review changed the original draft or stored data.");
            }
            if (picker.Parent is not VerticalStackLayout parent)
                throw new InvalidOperationException("The original icon picker has no retained parent.");
            var position = parent.Children.IndexOf(picker);
            parent.Children.Remove(picker);
            await Task.Delay(100);
            translator.SetCulture(System.Globalization.CultureInfo.GetCultureInfo("de"));
            parent.Children.Insert(position, picker);
            await Task.Delay(250);
            if (vm.IconKey != "Home" || Draft() != draft || !buttons.SequenceEqual(VisualDescendants(picker).OfType<Button>()))
                throw new InvalidOperationException("Icon reattachment replaced original buttons or draft selection.");
            for (var i = 0; i < keys.Length; i++)
            {
                var expected = translator[keys[i] is null ? "Icon_Default" : "Icon_" + keys[i]];
                if (keys[i] == "Home") expected = translator.Format("Common_SelectedItem", expected);
                if (buttons[i].Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button native
                    || new Microsoft.UI.Xaml.Automation.Peers.ButtonAutomationPeer(native).GetName() != expected)
                    throw new InvalidOperationException("Reattached original icon target did not catch up with its current language.");
            }
            translator.SetCulture(System.Globalization.CultureInfo.GetCultureInfo(language));
            await Task.Delay(200);
            await ScrollToViewIfNeededAsync(scroll, picker);
            await CaptureAsync(app, folder, language + "-entry-icon-language");
            await File.WriteAllTextAsync(Path.Combine(folder, language + "-icon-language-proof.json"),
                System.Text.Json.JsonSerializer.Serialize(new { Names = changes, NativeInvocations = invocations, RetainedTargets = true,
                    ReattachedCurrentNamesAndOriginalTargets = true, OriginalDraftAndStoredRowsUnchanged = true, NoSave = true }));
        }
        finally
        {
            translator.SetCulture(originalCulture); page.FlowDirection = originalDirection; vm.IconKey = originalIcon; vm.ShowDetails = originalDetails;
        }
    }
}
#endif
