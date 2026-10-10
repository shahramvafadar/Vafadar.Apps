#if DEBUG && WINDOWS
using System.Text.Json;

namespace Vafadar.Zanance.App.Diagnostics;

/// <summary>Measures actual native financial rows and persistent actions in the fictitious snapshot review.</summary>
internal static class DebugLayoutChecks
{
    /// <summary>Includes actual growing navigation, actions, date inputs and large captions in the own-window review.</summary>
    internal static bool AppliesTo(ContentPage page) => Descendants(page).Any(element =>
        element is Presentation.InsightsTabs or Presentation.PageHeader or Presentation.AmountReadout or Vafadar.Maui.Controls.DateField or Picker && IsVisibleThroughParents(element));

    /// <summary>Rejects action overlap or clipped realized financial identities after the native layout pass.</summary>
    internal static void Check(ContentPage page, string folder, string name)
    {
        var evidence = new List<object>();
        foreach (var picker in Descendants(page).OfType<Picker>()
            .Where(picker => IsVisibleThroughParents(picker) && picker.SelectedIndex >= 0))
        { evidence.Add(DebugPickerText.Check(picker, folder, name)); }
        // AT-99: section identities remain complete beside their original reorder/visibility controls (D-94).
        var homeRows = Descendants(page).OfType<Grid>()
            .Where(row => row.BindingContext is Features.Home.HomeSectionRow && row.Children.OfType<Switch>().Any()).ToArray();
        if (page.BindingContext is Features.Home.HomeLayoutViewModel homeLayout && homeRows.Length != homeLayout.Rows.Count)
        { throw new InvalidOperationException("A Home customization section lacks its actual native row."); }
        foreach (var row in homeRows)
        {
            var model = (Features.Home.HomeSectionRow)row.BindingContext;
            var label = row.Children.OfType<Label>().Single();
            var toggle = row.Children.OfType<Switch>().Single();
            if (label.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.TextBlock native)
            { throw new InvalidOperationException("The Home customization caption is not realized."); }
            var slot = Microsoft.UI.Xaml.Controls.Primitives.LayoutInformation.GetLayoutSlot(native);
            var boundaries = new List<Windows.Foundation.Rect>();
            for (var offset = 0; offset <= native.ContentEnd.Offset - native.ContentStart.Offset; offset++)
            {
                var pointer = native.ContentStart.GetPositionAtOffset(offset, Microsoft.UI.Xaml.Documents.LogicalDirection.Forward);
                if (pointer is not null) { boundaries.Add(pointer.GetCharacterRect(Microsoft.UI.Xaml.Documents.LogicalDirection.Forward)); }
            }
            if (label.Text != model.Name || native.IsTextTrimmed || !native.IsTextScaleFactorEnabled
                || boundaries.Count == 0 || boundaries.Min(rect => rect.Left) < -1
                || boundaries.Max(rect => rect.Right) > slot.Width + 1 || boundaries.Max(rect => rect.Bottom) > slot.Height + 1
                || toggle.Width < 44 || toggle.Height < 44 || toggle.IsToggled != model.IsVisible
                || label.Width < row.Width - row.Padding.HorizontalThickness - 1
                || toggle.Y < label.Y + label.Height - 1
                || SemanticProperties.GetDescription(toggle) != model.Name
                || row.Children.OfType<Button>().Where(button => button.IsVisible)
                    .Any(button => button.Width < 44 || button.Height < 44 || button.Command is null || button.CommandParameter != model))
            {
                File.WriteAllText(Path.Combine(folder, name + "-home-customization-failure.json"),
                    JsonSerializer.Serialize(new { model.Name, label.Width, label.Height, native.IsTextTrimmed,
                        native.FontSize, slot, boundaries, toggleWidth = toggle.Width, toggleHeight = toggle.Height,
                        Buttons = row.Children.OfType<Button>().Select(button => new { button.Width, button.Height, button.IsVisible,
                            HasCommand = button.Command is not null, SameParameter = button.CommandParameter == model }),
                        toggle.IsToggled, model.IsVisible, SpokenName = SemanticProperties.GetDescription(toggle) },
                        new JsonSerializerOptions { WriteIndented = true }));
                throw new InvalidOperationException("A Home customization section clips its name or loses its complete native controls.");
            }
            evidence.Add(new { kind = "Home customization section", model.Section, model.Name, row.Width, row.Height,
                native.FontSize, native.IsTextScaleFactorEnabled, native.IsTextTrimmed, renderedBoundaries = boundaries.Count,
                toggleWidth = toggle.Width, toggleHeight = toggle.Height, model.CanMoveUp, model.CanMoveDown });
        }
        foreach (var header in Descendants(page).OfType<Presentation.PageHeader>())
        {
            var title = header.Children.OfType<Label>().Single();
            var back = header.Children.OfType<ImageButton>().Single();
            if (title.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.TextBlock native
                || back.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button nativeBack)
            { throw new InvalidOperationException("The growing page header is not realized."); }
            var boundaries = new List<Windows.Foundation.Rect>();
            for (var offset = 0; offset <= native.ContentEnd.Offset - native.ContentStart.Offset; offset++)
            {
                var pointer = native.ContentStart.GetPositionAtOffset(offset, Microsoft.UI.Xaml.Documents.LogicalDirection.Forward);
                if (pointer is not null) { boundaries.Add(pointer.GetCharacterRect(Microsoft.UI.Xaml.Documents.LogicalDirection.Forward)); }
            }
            var spoken = new Microsoft.UI.Xaml.Automation.Peers.ButtonAutomationPeer(nativeBack).GetName();
            if (native.IsTextTrimmed || title.Text != page.Title || title.Width <= 0 || !native.IsTextScaleFactorEnabled
                || back.Width < 44 || back.Height < 44 || spoken != Vafadar.Localization.Translator.Instance["Common_Back"]
                || boundaries.Count == 0 || boundaries.Min(rect => rect.Left) < -1
                || boundaries.Max(rect => rect.Right) > native.ActualWidth + 1
                || boundaries.Max(rect => rect.Bottom) > native.ActualHeight + 1
                || header.Parent is not Grid host || Grid.GetRow(header) != 0
                || host.Children.OfType<VisualElement>().Single(child => Grid.GetRow(child) == 1).Y < header.Y + header.Height - 1)
            {
                File.WriteAllText(Path.Combine(folder, name + "-header-layout-failure.json"),
                    JsonSerializer.Serialize(new { page.Title, title.Width, title.Height, native.ActualWidth, native.ActualHeight,
                        native.FontSize, native.IsTextTrimmed, spoken, backWidth = back.Width, backHeight = back.Height, boundaries },
                        new JsonSerializerOptions { WriteIndented = true }));
                throw new InvalidOperationException("A page header clips its title or loses its translated back target.");
            }
            evidence.Add(new { kind = "page header", title.Text, title.Width, title.Height, native.FontSize,
                native.IsTextScaleFactorEnabled, native.IsTextTrimmed, spoken, backWidth = back.Width, backHeight = back.Height,
                renderedBoundaries = boundaries.Count, rootWidth = page.Content?.Width });
        }
        foreach (var tabs in Descendants(page).OfType<Presentation.InsightsTabs>())
        {
            var selectedCount = 0;
            foreach (var tab in tabs.Children.OfType<Grid>())
            {
                var caption = tab.Children.OfType<Label>().Single();
                var button = tab.Children.OfType<Button>().Single();
                var bar = tab.Children.OfType<BoxView>().Single();
                if (caption.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.TextBlock native
                    || button.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button nativeButton)
                { throw new InvalidOperationException("An Insights destination is not realized."); }
                var boundaries = new List<Windows.Foundation.Rect>();
                for (var offset = 0; offset <= native.ContentEnd.Offset - native.ContentStart.Offset; offset++)
                {
                    var pointer = native.ContentStart.GetPositionAtOffset(offset, Microsoft.UI.Xaml.Documents.LogicalDirection.Forward);
                    if (pointer is not null) { boundaries.Add(pointer.GetCharacterRect(Microsoft.UI.Xaml.Documents.LogicalDirection.Forward)); }
                }
                // Centered WinUI character rectangles include the offset within the actual allocated layout slot;
                // ActualWidth is the tight text width and is not the caption's arranged viewport.
                var slot = Microsoft.UI.Xaml.Controls.Primitives.LayoutInformation.GetLayoutSlot(native);
                var spoken = new Microsoft.UI.Xaml.Automation.Peers.ButtonAutomationPeer(nativeButton).GetName();
                var selected = (string?)button.CommandParameter == tabs.CurrentRoute;
                if (selected) { selectedCount++; }
                if (native.IsTextTrimmed || !native.IsTextScaleFactorEnabled || button.Width < 44 || button.Height < 44
                    || spoken != caption.Text || button.Command is null || tab.Children.Last() != button
                    || selected != (bar.Opacity == 1) || selected && SemanticProperties.GetHint(button) != Vafadar.Localization.Translator.Instance["Common_Selected"]
                    || boundaries.Count == 0 || boundaries.Min(rect => rect.Left) < -1
                    || slot.Width > caption.Width + 1 || slot.Height > caption.Height + 1
                    || boundaries.Max(rect => rect.Right) > slot.Width + 1
                    || boundaries.Max(rect => rect.Bottom) > slot.Height + 1
                    || tab.X < -1 || tab.X + tab.Width > tabs.Width + 1
                    || tabs.Parent is not Grid host || host.Children.OfType<VisualElement>().Single(child => Grid.GetRow(child) == 1).Y < tabs.Y + tabs.Height - 1)
                {
                    File.WriteAllText(Path.Combine(folder, name + "-insights-layout-failure.json"),
                        JsonSerializer.Serialize(new { caption.Text, spoken, selected, tab.X, tab.Y, tab.Width, tab.Height,
                            tabsWidth = tabs.Width, buttonWidth = button.Width, buttonHeight = button.Height, native.FontSize,
                            native.ActualWidth, native.ActualHeight, native.IsTextTrimmed, native.IsTextScaleFactorEnabled,
                            native.ActualOffset, native.HorizontalAlignment, native.VerticalAlignment, native.Padding, native.Margin, captionWidth = caption.Width, captionHeight = caption.Height, slot,
                            hint = SemanticProperties.GetHint(button), expectedHint = Vafadar.Localization.Translator.Instance["Common_Selected"],
                            hasCommand = button.Command is not null, lastButton = tab.Children.Last() == button, bar.Opacity,
                            tabsTop = tabs.Y, tabsHeight = tabs.Height, parentType = tabs.Parent?.GetType().Name,
                            bodyY = (tabs.Parent as Grid)?.Children.OfType<VisualElement>().Single(child => Grid.GetRow(child) == 1).Y, boundaries },
                            new JsonSerializerOptions { WriteIndented = true }));
                    throw new InvalidOperationException("An Insights destination clips text, overlaps the body or loses its selected command target.");
                }
                evidence.Add(new { kind = "Insights destination", caption.Text, spoken, selected, tabs.CurrentRoute,
                    native.FontSize, native.IsTextScaleFactorEnabled, tab.X, tab.Y, tab.Width, tab.Height,
                    slot, columns = tabs.ColumnDefinitions.Count, buttonWidth = button.Width, buttonHeight = button.Height,
                    renderedBoundaries = boundaries.Count });
            }
            if (selectedCount != 1 || tabs.Children.Count != 4 || Presentation.InsightsTabs.RouteOf(page) != tabs.CurrentRoute)
            { throw new InvalidOperationException("Insights must show four destinations with one current page."); }
        }
        if (page.FindByName<Button>("BalanceHeadingAction") is { } heading && IsVisibleThroughParents(heading))
        {
            // D-81: the account link no longer spans the amount; its own heading still needs a full touch target.
            if (heading.Width < 44 || heading.Height < 44 || heading.Command is null
                || string.IsNullOrEmpty(SemanticProperties.GetDescription(heading)))
            { throw new InvalidOperationException("The balance heading lost its account command or usable target."); }
            if (heading.Parent is Grid balanceGrid
                && balanceGrid.Children.OfType<VerticalStackLayout>().FirstOrDefault(child => Grid.GetRow(child) == 1) is { } values
                && heading.Y + heading.Height > values.Y + 1)
            { throw new InvalidOperationException("The account heading covers the amount scroll viewport."); }
            evidence.Add(new { kind = "balance heading", heading.Width, heading.Height,
                spokenName = SemanticProperties.GetDescription(heading), hasCommand = true });
        }
        if (page.FindByName<VisualElement>("ContentViewport") is { IsVisible: true } viewport)
        {
            foreach (var actionName in new[] { "AddAction", "BulkActions", "UndoNotice" })
            {
                if (page.FindByName<VisualElement>(actionName) is not { IsVisible: true } action) { continue; }
                // D-78: both siblings are measured in their common grid, so scrolling cannot hide overlap.
                if (viewport.Width <= 0 || viewport.Height <= 0 || action.Height < 44
                    || action.Y < viewport.Y + viewport.Height - 1)
                {
                    File.WriteAllText(Path.Combine(folder, name + "-action-layout-failure.json"),
                        JsonSerializer.Serialize(new { actionName, viewport.Width, viewport.Height, viewport.Y,
                            actionTop = action.Y, actionHeight = action.Height }, new JsonSerializerOptions { WriteIndented = true }));
                    throw new InvalidOperationException("A persistent action covers its viewport or has no usable target.");
                }
                evidence.Add(new { kind = "action dock", actionName, viewport.Width, viewport.Height,
                    viewportBottom = viewport.Y + viewport.Height, actionTop = action.Y, actionHeight = action.Height });
            }
        }

        foreach (var row in Descendants(page).OfType<Grid>())
        {
            if (!IsVisibleThroughParents(row)) { continue; }
            if (row.BindingContext is not (Presentation.EntryRow or Features.Accounts.AccountItem or Features.Holdings.HoldingRow)) { continue; }
            var identity = row.Children.OfType<VerticalStackLayout>().FirstOrDefault(child => Grid.GetColumn(child) == 1 && Grid.GetRow(child) == 0);
            var title = identity?.Children.OfType<Label>().FirstOrDefault();
            if (title?.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.TextBlock text) { continue; }
            var amount = row.Children.OfType<VisualElement>().FirstOrDefault(child => Grid.GetColumn(child) == 1 && Grid.GetRow(child) == 1);
            if (amount is null || identity is null) { continue; }
            // A realized identity must have useful width, and its amount must start after the whole identity stack.
            if (text.IsTextTrimmed || title.Width <= 0 || identity.Width < row.Width * .4
                || amount.Y < identity.Y + identity.Height - 1)
            {
                File.WriteAllText(Path.Combine(folder, name + "-financial-layout-failure.json"),
                    JsonSerializer.Serialize(new { type = row.BindingContext.GetType().Name, title.Text, text.IsTextTrimmed,
                        row.Width, row.Height, row.IsVisible, identityWidth = identity.Width, titleWidth = title.Width,
                        identityTop = identity.Y, identityHeight = identity.Height, amountTop = amount.Y, amountHeight = amount.Height },
                        new JsonSerializerOptions { WriteIndented = true }));
                throw new InvalidOperationException("A financial row clips its identity or overlaps its amount.");
            }
            evidence.Add(new { kind = "financial row", type = row.BindingContext.GetType().Name, title.Text,
                text.FontSize, text.IsTextScaleFactorEnabled, text.IsTextTrimmed, row.Width,
                identityWidth = identity.Width, identityBottom = identity.Y + identity.Height, amountTop = amount.Y });
            if (row.BindingContext is Features.Accounts.AccountItem)
            {
                // AT-101: a complete title does not prove that wrapping type/default/completeness badges fit.
                foreach (var badge in Descendants(identity).OfType<Label>().Where(label => label != title && IsVisibleThroughParents(label)))
                {
                    if (badge.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.TextBlock native)
                    { throw new InvalidOperationException("An account description has no actual native text."); }
                    var slot = Microsoft.UI.Xaml.Controls.Primitives.LayoutInformation.GetLayoutSlot(native);
                    var boundaries = new List<Windows.Foundation.Rect>();
                    for (var offset = 0; offset <= native.ContentEnd.Offset - native.ContentStart.Offset; offset++)
                    {
                        var pointer = native.ContentStart.GetPositionAtOffset(offset, Microsoft.UI.Xaml.Documents.LogicalDirection.Forward);
                        if (pointer is not null) { boundaries.Add(pointer.GetCharacterRect(Microsoft.UI.Xaml.Documents.LogicalDirection.Forward)); }
                    }
                    if (native.IsTextTrimmed || !native.IsTextScaleFactorEnabled || boundaries.Count == 0
                        || boundaries.Min(rect => rect.Left) < -1 || boundaries.Max(rect => rect.Right) > slot.Width + 1
                        || boundaries.Max(rect => rect.Bottom) > slot.Height + 1
                        || boundaries.Max(rect => rect.Right) > badge.Width + 1
                        || boundaries.Max(rect => rect.Bottom) > badge.Height + 1)
                    {
                        File.WriteAllText(Path.Combine(folder, name + "-account-description-failure.json"),
                            JsonSerializer.Serialize(new { title.Text, Badge = badge.Text, native.IsTextTrimmed, native.FontSize,
                                badge.Width, badge.Height, native.ActualWidth, native.ActualHeight, slot, boundaries,
                                identityHeight = identity.Height, amountTop = amount.Y }, new JsonSerializerOptions { WriteIndented = true }));
                        throw new InvalidOperationException("An account type or status description clips its actual glyphs.");
                    }
                    evidence.Add(new { kind = "account description", badge.Text, native.FontSize, native.IsTextScaleFactorEnabled,
                        native.IsTextTrimmed, badge.Width, badge.Height, slot, renderedBoundaries = boundaries.Count });
                }
            }
        }

        // AT-94: these three period choices are a form decision; every real native target must stay visible without sideways scrolling.
        if (page.FindByName<Vafadar.Maui.Controls.ChoiceChips>("BudgetPeriods") is { } periods && IsVisibleThroughParents(periods))
        {
            var chips = Descendants(periods).OfType<Grid>().Where(chip => chip.Children.OfType<Button>().Any()).ToArray();
            if (chips.Length != periods.ItemsSource?.Count) { throw new InvalidOperationException("Budget period targets are incomplete."); }
            for (var index = 0; index < chips.Length; index++)
            {
                var chip = chips[index]; var button = chip.Children.OfType<Button>().Single();
                var caption = chip.Children.OfType<Border>().Single().Content as Label
                    ?? throw new InvalidOperationException("Budget period caption is unavailable.");
                if (caption.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.TextBlock native
                    || button.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button nativeButton)
                { throw new InvalidOperationException("Budget period target is not realized."); }
                var boundaries = new List<Windows.Foundation.Rect>();
                for (var offset = 0; offset <= native.ContentEnd.Offset - native.ContentStart.Offset; offset++)
                {
                    var pointer = native.ContentStart.GetPositionAtOffset(offset, Microsoft.UI.Xaml.Documents.LogicalDirection.Forward);
                    if (pointer is not null) { boundaries.Add(pointer.GetCharacterRect(Microsoft.UI.Xaml.Documents.LogicalDirection.Forward)); }
                }
                var slot = Microsoft.UI.Xaml.Controls.Primitives.LayoutInformation.GetLayoutSlot(native);
                var selected = index == periods.SelectedIndex;
                var expectedName = selected ? Vafadar.Localization.Translator.Instance.Format("Common_ChipSelected", caption.Text) : caption.Text;
                var spoken = new Microsoft.UI.Xaml.Automation.Peers.ButtonAutomationPeer(nativeButton).GetName();
                if (native.IsTextTrimmed || !native.IsTextScaleFactorEnabled || button.Width < 44 || button.Height < 44
                    || spoken != expectedName || chip.Children.Last() != button || caption.FontAttributes.HasFlag(FontAttributes.Bold) != selected
                    || chip.X < -1 || chip.X + chip.Width > periods.Width + 1 || chip.Y < -1 || chip.Y + chip.Height > periods.Height + 1
                    || boundaries.Count == 0 || boundaries.Min(rect => rect.Left) < -1
                    || slot.Width > caption.Width + 1 || slot.Height > caption.Height + 1
                    || boundaries.Max(rect => rect.Right) > slot.Width + 1 || boundaries.Max(rect => rect.Bottom) > slot.Height + 1)
                {
                    File.WriteAllText(Path.Combine(folder, name + "-period-choice-failure.json"), JsonSerializer.Serialize(new
                    { caption.Text, spoken, expectedName, selected, chip.X, chip.Y, chip.Width, chip.Height,
                        availableWidth = periods.Width, availableHeight = periods.Height, buttonWidth = button.Width,
                        buttonHeight = button.Height, native.FontSize, native.IsTextTrimmed, slot, boundaries }, new JsonSerializerOptions { WriteIndented = true }));
                    throw new InvalidOperationException("A budget period choice is hidden, clips text or loses its full selected native target.");
                }
                evidence.Add(new { kind = "budget period choice", caption.Text, selected, spoken, chip.X, chip.Y,
                    chip.Width, chip.Height, buttonWidth = button.Width, buttonHeight = button.Height, native.FontSize,
                    native.IsTextScaleFactorEnabled, renderedBoundaries = boundaries.Count });
            }
        }
        // An icon template also inherits BudgetLine; select actual figure rows and independently require every visible model row.
        var budgetRows = Descendants(page).OfType<Grid>().Where(row => row.BindingContext is Features.Budget.BudgetLine
            && row.Children.OfType<Presentation.AmountReadout>().Any() && IsVisibleThroughParents(row)).ToArray();
        if (page.BindingContext is Features.Budget.BudgetViewModel budget
            && budgetRows.Length != budget.TotalLines.Count + budget.CategoryLines.Count)
        { throw new InvalidOperationException("A visible budget model row lacks its figure surface."); }
        foreach (var row in budgetRows)
        {
            var line = (Features.Budget.BudgetLine)row.BindingContext;
            var nameCaption = row.Children.OfType<Label>().Single();
            var values = row.Children.OfType<Presentation.AmountReadout>().OrderBy(value => Grid.GetRow((BindableObject)value)).ToArray();
            if (nameCaption.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.TextBlock native || values.Length != 2)
            { throw new InvalidOperationException("A budget row lacks its actual identity and separate figures."); }
            var slot = Microsoft.UI.Xaml.Controls.Primitives.LayoutInformation.GetLayoutSlot(native);
            var boundaries = new List<Windows.Foundation.Rect>();
            for (var offset = 0; offset <= native.ContentEnd.Offset - native.ContentStart.Offset; offset++)
            {
                var pointer = native.ContentStart.GetPositionAtOffset(offset, Microsoft.UI.Xaml.Documents.LogicalDirection.Forward);
                if (pointer is not null) { boundaries.Add(pointer.GetCharacterRect(Microsoft.UI.Xaml.Documents.LogicalDirection.Forward)); }
            }
            var spendingCaption = (Label)Descendants(values[0]).OfType<ScrollView>().Single().Content;
            var limitCaption = (Label)Descendants(values[1]).OfType<ScrollView>().Single().Content;
            if (native.IsTextTrimmed || nameCaption.Text != line.Name || !native.IsTextScaleFactorEnabled
                || boundaries.Count == 0 || boundaries.Min(rect => rect.Left) < -1
                || boundaries.Max(rect => rect.Right) > slot.Width + 1 || boundaries.Max(rect => rect.Bottom) > slot.Height + 1
                || values[0].AmountText != line.SpentText || values[1].AmountText != line.LimitText
                || spendingCaption.FontSize != 14 || limitCaption.FontSize != 13 || spendingCaption.FontAttributes != FontAttributes.Bold
                || values.Any(value => value.Width < row.Width - 1 || Grid.GetColumnSpan((BindableObject)value) != 2)
                || values[0].Y < nameCaption.Y + nameCaption.Height - 1 || values[1].Y < values[0].Y + values[0].Height - 1)
            {
                File.WriteAllText(Path.Combine(folder, name + "-budget-layout-failure.json"),
                    JsonSerializer.Serialize(new { line.Name, line.SpentText, line.LimitText, row.Width, row.Height,
                        titleWidth = nameCaption.Width, titleHeight = nameCaption.Height, native.FontSize, native.IsTextTrimmed,
                        spentWidth = values[0].Width, spentY = values[0].Y, spentHeight = values[0].Height,
                        limitWidth = values[1].Width, limitY = values[1].Y, spendingFont = spendingCaption.FontSize,
                        limitFont = limitCaption.FontSize, slot, boundaries }, new JsonSerializerOptions { WriteIndented = true }));
                throw new InvalidOperationException("A budget identity or full spending/limit packet clips, overlaps or changes typography.");
            }
            evidence.Add(new { kind = "budget figures", line.Name, line.SpentText, line.LimitText, row.Width, row.Height,
                native.IsTextScaleFactorEnabled, spendingFont = spendingCaption.FontSize, limitFont = limitCaption.FontSize,
                spentWidth = values[0].Width, limitWidth = values[1].Width, separateRows = true, renderedTitleBoundaries = boundaries.Count });
        }
        // D-80: check the actual caption and last native command button, never a source-text mirror.
        foreach (var action in Descendants(page).OfType<Presentation.WrappingAction>())
        {
            if (!IsVisibleThroughParents(action)) { continue; }
            if (action.Content is not Grid grid || grid.Children.FirstOrDefault() is not Border { Content: Label caption }
                || grid.Children.LastOrDefault() is not Button button
                || caption.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.TextBlock text)
            { throw new InvalidOperationException("A growing action lacks its actual caption/native button."); }
            if (text.IsTextTrimmed || caption.Width <= 0 || action.Width < 44 || action.Height < 44
                || text.ActualHeight + 1 < text.DesiredSize.Height
                || (action.Appearance == Presentation.ActionAppearance.Suggestion && caption.Height + 15 > action.Height)
                || button.Command != action.Command || button.CommandParameter != action.CommandParameter
                || SemanticProperties.GetDescription(button) != action.Text)
            { throw new InvalidOperationException("A growing action clips its caption or loses its command/name."); }
            evidence.Add(new { kind = "wrapping action", action.Text, action.Appearance, action.Width, action.Height,
                captionWidth = caption.Width, captionHeight = caption.Height, text.IsTextTrimmed,
                text.FontSize, nativeButtonEnabled = button.IsEnabled, spokenName = SemanticProperties.GetDescription(button) });
        }
        // D-81: compare native glyph width with the real available input width, not a fixed source constant.
        foreach (var field in Descendants(page).OfType<Vafadar.Maui.Controls.DateField>())
        {
            if (!IsVisibleThroughParents(field)) { continue; }
            var boxes = Descendants(field).OfType<Entry>().ToArray();
            if (boxes.Length != 3) { throw new InvalidOperationException("A date field must retain three real native inputs."); }
            foreach (var box in boxes)
            {
                if (box.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.TextBox native) { continue; }
                var glyphs = new Microsoft.UI.Xaml.Controls.TextBlock { Text = box.MaxLength == 4 ? "8888" : "88",
                    FontFamily = native.FontFamily, FontSize = native.FontSize, FontWeight = native.FontWeight,
                    FontStyle = native.FontStyle, FontStretch = native.FontStretch, CharacterSpacing = native.CharacterSpacing,
                    IsTextScaleFactorEnabled = native.IsTextScaleFactorEnabled };
                glyphs.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
                var available = native.ActualWidth - native.Padding.Left - native.Padding.Right
                    - native.BorderThickness.Left - native.BorderThickness.Right;
                if (available + 1 < glyphs.DesiredSize.Width || box.Width < 44 || box.Height < 44)
                {
                    File.WriteAllText(Path.Combine(folder, name + "-date-input-failure.json"), JsonSerializer.Serialize(new
                    {
                        box.Text, box.MaxLength, box.Width, box.Height, native.ActualWidth, native.ActualHeight,
                        native.FontSize, native.Padding, native.BorderThickness, availableWidth = available,
                        requiredDigitWidth = glyphs.DesiredSize.Width, field.Date, fieldWidth = field.Width, fieldHeight = field.Height,
                    }, new JsonSerializerOptions { WriteIndented = true }));
                    throw new InvalidOperationException("A date input clips its complete digit count or has an undersized target.");
                }
                evidence.Add(new { kind = "date input", box.Text, box.MaxLength, box.Width, box.Height,
                    native.FontSize, availableWidth = available, requiredDigitWidth = glyphs.DesiredSize.Width,
                    spokenName = SemanticProperties.GetDescription(box), field.Date });
            }
        }
        foreach (var readout in Descendants(page).OfType<Presentation.AmountReadout>())
        {
            if (!IsVisibleThroughParents(readout)) { continue; }
            var scroll = Descendants(readout).OfType<ScrollView>().Single();
            if (string.IsNullOrEmpty(readout.AmountText))
            {
                if (scroll.IsVisible || Descendants(readout).OfType<Label>().Any(label => label.IsVisible && label.Text == Vafadar.Localization.Translator.Instance["Amount_ScrollHint"]))
                { throw new InvalidOperationException("An empty amount retains a blank scroll area or stale overflow hint."); }
                evidence.Add(new { kind = "empty amount", viewportVisible = scroll.IsVisible });
                continue;
            }
            var label = scroll.Content as Label ?? throw new InvalidOperationException("The real amount caption is missing.");
            if (label.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.TextBlock native) { continue; }
            if (native.IsTextTrimmed || scroll.Width <= 0 || label.Text != readout.AmountText
                || SemanticProperties.GetDescription(label) != readout.AmountText || label.LineBreakMode != LineBreakMode.NoWrap)
            { throw new InvalidOperationException("A large readout loses its full display or spoken packet."); }
            // A detached TextBlock can resolve a bundled font differently from the realized caption. Check the
            // actual rendered caret/glyph boundaries, including every direction mark and the final currency glyph.
            var rendered = new List<Windows.Foundation.Rect>();
            for (var offset = 0; offset <= native.ContentEnd.Offset - native.ContentStart.Offset; offset++)
            {
                var pointer = native.ContentStart.GetPositionAtOffset(offset, Microsoft.UI.Xaml.Documents.LogicalDirection.Forward);
                if (pointer is not null) { rendered.Add(pointer.GetCharacterRect(Microsoft.UI.Xaml.Documents.LogicalDirection.Forward)); }
            }
            if (rendered.Count == 0 || rendered.Any(rect => rect.IsEmpty || rect.Height <= 0)
                || rendered.Min(rect => rect.Left) < -1 || rendered.Max(rect => rect.Right) > native.ActualWidth + 1
                || rendered.Max(rect => rect.Bottom) > native.ActualHeight + 1
                || rendered.Max(rect => rect.Top) - rendered.Min(rect => rect.Top) > 1)
            {
                File.WriteAllText(Path.Combine(folder, name + "-amount-layout-failure.json"),
                    JsonSerializer.Serialize(new { readout.AmountText, label.Width, label.Height, viewportWidth = scroll.Width,
                        native.ActualWidth, native.ActualHeight, native.FontSize, fontFamily = native.FontFamily.Source,
                        native.FlowDirection, native.TextAlignment, native.IsTextScaleFactorEnabled, rendered },
                        new JsonSerializerOptions { WriteIndented = true }));
                throw new InvalidOperationException("A whole amount exceeds its actual rendered content or splits across lines.");
            }
            evidence.Add(new { kind = "large amount", readout.AmountText, label.Width, label.Height, viewportWidth = scroll.Width,
                native.FontSize, native.IsTextTrimmed,
                fullPacketWidth = rendered.Max(rect => rect.Right) - rendered.Min(rect => rect.Left),
                renderedBoundaries = rendered.Count, overflow = label.Width > scroll.Width + 1 });
        }
        File.WriteAllText(Path.Combine(folder, name + "-layout-checks.json"),
            JsonSerializer.Serialize(evidence, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static IEnumerable<VisualElement> Descendants(IVisualTreeElement root)
    {
        foreach (var child in root.GetVisualChildren())
        {
            if (child is VisualElement visual) { yield return visual; }
            foreach (var descendant in Descendants(child)) { yield return descendant; }
        }
    }

    private static bool IsVisibleThroughParents(VisualElement element)
    {
        // Hidden Home sections still contain handlers but have never been measured; they are not native row evidence.
        for (Element? current = element; current is not null; current = current.Parent)
        {
            if (current is VisualElement { IsVisible: false }) { return false; }
        }
        return true;
    }
}
#endif
