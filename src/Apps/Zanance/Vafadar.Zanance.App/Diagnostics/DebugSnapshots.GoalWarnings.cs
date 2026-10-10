#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Documents;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Goals;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Checks one complete warning packet per fictitious goal card without saving any data.</summary>
    private static async Task ReviewGoalWarningsAsync(App app, IServiceProvider services, GoalsPage page,
        string folder, string language)
    {
        var vm = (GoalsViewModel)page.BindingContext;
        var store = services.GetRequiredService<ZananceStore>();
        var goals = services.GetRequiredService<GoalStore>();
        var presenter = services.GetRequiredService<GoalPresenter>();
        var translator = services.GetRequiredService<Translator>();
        async Task<string> StoredAsync() => JsonSerializer.Serialize(new
        {
            Accounts = await store.GetAccountsAsync(), Entries = await store.GetEntriesAsync(),
            Settings = await store.GetSettingsAsync(), Goals = await goals.GetGoalsAsync(),
            Allocations = await goals.GetAllocationsAsync(), Plans = await goals.GetContributionPlansAsync(),
        });
        var stored = await StoredAsync();
        var active = vm.Active.ToArray(); var finished = vm.Finished.ToArray(); var earmarks = vm.Earmarks.ToArray();
        var originalFlags = (vm.HasActive, vm.HasFinished, vm.IsEmpty);
        var warning = translator.Format("Goal_Unfunded", presenter.Money(50_000, "EUR")) + Environment.NewLine
            + translator.Format("Goal_OverdueNeeded", presenter.Money(30_000, "EUR"));
        var row = new GoalRow(Guid.NewGuid(), translator["Goals_Title"], FluentIcons.Common.Symbol.Savings,
            translator.Format("Goal_Funded", presenter.Money(20_000, "EUR"), presenter.Money(100_000, "EUR")),
            .2, Palette.NearLimit, null, null, warning, null);
        var checks = new List<object>(); var failed = false;
        try
        {
            vm.Active.Clear(); vm.Active.Add(row);
            vm.Finished.Clear(); vm.Finished.Add(row with { Id = Guid.NewGuid(), StateText = translator["GoalState_Archived"] });
            vm.Earmarks.Clear(); vm.HasActive = true; vm.HasFinished = true; vm.IsEmpty = false;
            await Task.Delay(300);
            var scroll = page.FindByName<ScrollView>("ContentViewport");
            if (scroll.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.ScrollViewer viewport)
            { throw new InvalidOperationException("The goal review lacks its actual native viewport."); }
            var provider = new ScrollViewerAutomationPeer(viewport).GetPattern(PatternInterface.Scroll) as IScrollProvider
                ?? throw new InvalidOperationException("The goal viewport lacks native Scroll.");
            foreach (var fixture in vm.Active.Concat(vm.Finished))
            {
                var card = VisualDescendants(page).OfType<Border>().Single(item => ReferenceEquals(item.BindingContext, fixture));
                var labels = VisualDescendants(card).OfType<Label>().Where(item => item.Text == warning).ToArray();
                var button = VisualDescendants(card).OfType<Button>().Single();
                var warningChecks = new List<object>();
                foreach (var label in labels)
                {
                    if (label.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.TextBlock text)
                    { throw new InvalidOperationException("The goal warning lacks its native text."); }
                    Windows.Foundation.Rect Bounds() => text.TransformToVisual(viewport).TransformBounds(
                        new Windows.Foundation.Rect(0, 0, text.ActualWidth, text.ActualHeight));
                    var initial = Bounds();
                    if (viewport.ScrollableHeight > 0)
                    {
                        var desired = viewport.VerticalOffset + initial.Top - (viewport.ActualHeight - initial.Height) / 2;
                        provider.SetScrollPercent(-1, Math.Clamp(desired, 0, viewport.ScrollableHeight) / viewport.ScrollableHeight * 100);
                    }
                    await Task.Delay(150);
                    var bounds = Bounds();
                    var slot = LayoutInformation.GetLayoutSlot(text);
                    var glyphs = new List<Windows.Foundation.Rect>();
                    for (var offset = 0; offset <= text.ContentEnd.Offset - text.ContentStart.Offset; offset++)
                    {
                        if (text.ContentStart.GetPositionAtOffset(offset, LogicalDirection.Forward) is { } pointer)
                        { glyphs.Add(pointer.GetCharacterRect(LogicalDirection.Forward)); }
                    }
                    // Persian labels shape display digits in the native handler; bound packets remain unchanged.
                    var expectedText = NativeDigits.IsEnabled && label.FlowDirection != FlowDirection.LeftToRight
                        ? NativeDigits.Apply(warning) : warning;
                    var complete = text.Text == expectedText && label.Text == warning && !text.IsTextTrimmed && text.IsTextScaleFactorEnabled
                        && bounds.Left >= -1 && bounds.Right <= viewport.ActualWidth + 1
                        && bounds.Top >= -1 && bounds.Bottom <= viewport.ActualHeight + 1 && glyphs.Count > 0
                        && glyphs.Min(box => box.Left) >= -1 && glyphs.Max(box => box.Right) <= slot.Width + 1
                        && glyphs.Max(box => box.Bottom) <= slot.Height + 1;
                    warningChecks.Add(new { complete, bounds, text.Text, text.FontSize, text.IsTextTrimmed, text.IsTextScaleFactorEnabled, slot, glyphs });
                    failed |= !complete;
                    await CaptureAsync(app, folder, language + "-goal-warning-" + checks.Count + "-" + warningChecks.Count);
                }
                var native = button.Handler?.PlatformView as Microsoft.UI.Xaml.Controls.Button
                    ?? throw new InvalidOperationException("The goal card lacks its original native button.");
                var peer = new ButtonAutomationPeer(native);
                var valid = labels.Length == 1 && button.Command == vm.OpenCommand
                    && ReferenceEquals(button.CommandParameter, fixture) && peer.GetName() == fixture.Description
                    && native.ActualWidth >= 44 && native.ActualHeight >= 44;
                failed |= !valid;
                checks.Add(new { fixture.Id, WarningCount = labels.Length, valid, WarningChecks = warningChecks });
            }
        }
        finally
        {
            vm.Active.Clear(); foreach (var item in active) { vm.Active.Add(item); }
            vm.Finished.Clear(); foreach (var item in finished) { vm.Finished.Add(item); }
            vm.Earmarks.Clear(); foreach (var item in earmarks) { vm.Earmarks.Add(item); }
            (vm.HasActive, vm.HasFinished, vm.IsEmpty) = originalFlags;
            if (!active.SequenceEqual(vm.Active) || !finished.SequenceEqual(vm.Finished) || !earmarks.SequenceEqual(vm.Earmarks)
                || stored != await StoredAsync())
            { throw new InvalidOperationException("The goal-warning review changed original presentation or stored rows."); }
        }
        File.WriteAllText(Path.Combine(folder, language + "-goal-warnings.json"), JsonSerializer.Serialize(new
        { Pass = !failed, Checks = checks, NoSave = true, OriginalRowsRestored = true, CompleteStoredRowsUnchanged = true }));
        if (failed) { throw new InvalidOperationException("A goal warning is duplicated or incomplete in the actual native viewport."); }
    }
}
#endif
