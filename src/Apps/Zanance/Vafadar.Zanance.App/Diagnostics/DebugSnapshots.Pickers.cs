#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Vafadar.Zanance.App.Features.Settings;
using Vafadar.Zanance.Data;
using ScrollView = Microsoft.Maui.Controls.ScrollView;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    // The fictitious Settings review reselects only existing choices, never a security preference or financial Save.
    private static async Task ReviewPickerTextAsync(IServiceProvider services, SettingsPage page, string folder, string language)
    {
        var vm = (SettingsViewModel)page.BindingContext;
        await vm.LoadAsync();
        await Task.Delay(300);
        var store = services.GetRequiredService<ZananceStore>();
        async Task<string> StoredAsync() => JsonSerializer.Serialize(new { Settings = await store.GetSettingsAsync(),
            Accounts = await store.GetAccountsAsync(), Entries = await store.GetEntriesAsync() });
        var before = await StoredAsync();
        var draft = (vm.EssentialText, vm.EssentialPeriodIndex);
        var proof = new List<object>();
        var viewport = page.FindByName<ScrollView>("SettingsContent")
            ?? throw new InvalidOperationException("The native Settings viewport is missing.");
        static bool Visible(VisualElement element)
        {
            for (Element? current = element; current is not null; current = current.Parent)
            { if (current is VisualElement { IsVisible: false }) { return false; } }
            return true;
        }
        try
        {
            vm.EssentialText = "619.07"; vm.EssentialPeriodIndex = 2;
            var pickers = VisualDescendants(page).OfType<Picker>().Where(p => Visible(p) && p.SelectedIndex >= 0).ToArray();
            if (pickers.Length < 6) { throw new InvalidOperationException("The Settings picker review is incomplete."); }
            foreach (var picker in pickers)
            {
                await ScrollToViewIfNeededAsync(viewport, picker);
                var selected = DebugPickerText.Check(picker, folder, language + "-settings-pickers");
                if (picker.Handler?.PlatformView is not ComboBox combo
                    || FrameworkElementAutomationPeer.CreatePeerForElement(combo) is not ComboBoxAutomationPeer peer
                    || peer.GetPattern(PatternInterface.ExpandCollapse) is not IExpandCollapseProvider expand)
                { throw new InvalidOperationException("The actual picker lacks its native expand pattern."); }
                var index = picker.SelectedIndex;
                expand.Expand();
                ComboBoxItem? item = null;
                try
                {
                    for (var attempt = 0; attempt < 40; attempt++)
                    {
                        await Task.Delay(50);
                        if (!ReferenceEquals(Shell.Current.CurrentPage, page) || !ReferenceEquals(picker.Handler?.PlatformView, combo))
                        { throw new InvalidOperationException("The popup picker was retired during arrangement."); }
                        if (combo.ContainerFromIndex(index) is ComboBoxItem { IsLoaded: true, ActualWidth: > 0, ActualHeight: > 0 } current)
                        { item = current; break; }
                    }
                    if (item is null) { throw new InvalidOperationException("The selected native popup row did not finish arrangement."); }
                    var itemPeer = peer.GetChildren()?.OfType<ComboBoxItemDataAutomationPeer>()
                        .SingleOrDefault(child => Equals(child.Item, combo.Items[index]));
                    if (itemPeer?.GetPattern(PatternInterface.SelectionItem) is not ISelectionItemProvider selection)
                    { throw new InvalidOperationException("The selected popup row lacks its real SelectionItem pattern."); }
                    var popup = DebugPickerText.CheckPopup(picker, item, itemPeer.GetName(), folder, language + "-settings-pickers");
                    selection.Select();
                    proof.Add(new { selected, popup, NativeExistingChoiceReselected = true });
                }
                finally { expand.Collapse(); }
                if (picker.SelectedIndex != index || vm.EssentialText != "619.07" || vm.EssentialPeriodIndex != 2)
                { throw new InvalidOperationException("Native picker reselection changed the choice or unsaved estimate."); }
            }
            if (before != await StoredAsync()) { throw new InvalidOperationException("Native picker review changed stored preferences or money."); }
            File.WriteAllText(Path.Combine(folder, language + "-settings-pickers-proof.json"),
                JsonSerializer.Serialize(new { Pickers = proof, NativeExistingChoices = true,
                    UnsavedEstimatePreserved = true, CompleteStoredDataUnchanged = true, NoFinancialSave = true }));
        }
        finally { vm.EssentialText = draft.EssentialText; vm.EssentialPeriodIndex = draft.EssentialPeriodIndex; }
    }
}
#endif
