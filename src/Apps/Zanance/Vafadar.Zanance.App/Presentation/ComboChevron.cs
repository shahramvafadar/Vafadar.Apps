using FluentIcons.Common;
using FluentIcons.Maui;
using Syncfusion.Maui.Inputs;
using System.ComponentModel;
using Vafadar.Localization;

namespace Vafadar.Zanance.App.Presentation;

/// <summary>
/// Gives currency inputs their thin chevron (D-43), scalable original rows and native list/selection actions (D-302).
/// Each input owns its views and subscriptions because a view can have only one parent.
/// </summary>
public static class ComboChevron
{
    /// <summary>Identifies the common currency-list action presentation.</summary>
    public static readonly BindableProperty IsEnabledProperty = BindableProperty.CreateAttached(
        "IsEnabled", typeof(bool), typeof(ComboChevron), false, propertyChanged: OnIsEnabledChanged);

    private static readonly BindableProperty CleanupProperty = BindableProperty.CreateAttached(
        "Cleanup", typeof(Action), typeof(ComboChevron), null);

    /// <summary>Gets whether this input owns the common accessible list action.</summary>
    public static bool GetIsEnabled(BindableObject view) => (bool)view.GetValue(IsEnabledProperty);

    /// <summary>Enables the same currency-list action without replacing filtering or selection.</summary>
    public static void SetIsEnabled(BindableObject view, bool value) => view.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is not SfComboBox box)
        {
            return;
        }
        (box.GetValue(CleanupProperty) as Action)?.Invoke();
        box.ClearValue(CleanupProperty);
        if (newValue is not true) return;

        var icon = new SymbolIcon
        {
            Symbol = Symbol.ChevronDown,
            FontSize = 16,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            InputTransparent = true,
        };
        icon.SetDynamicResource(SymbolIcon.ForegroundColorProperty, "SecondaryText");
        AutomationProperties.SetIsInAccessibleTree(icon, false);
        IconTextScale.SetIsFixed(icon, true);
        // D-302: the vendor's hidden default/36-unit custom surface neither supplies the promised
        // list affordance nor a native Invoke action. Keep its original list/filter and give it a real target.
        var target = new Button
        {
            MinimumWidthRequest = 44,
            MinimumHeightRequest = 44,
            AutomationId = "CurrencyDropDownAction",
            Command = new Command(() => { if (box.IsEnabled) box.IsDropDownOpen = !box.IsDropDownOpen; }),
        };
        target.SetDynamicResource(VisualElement.StyleProperty, "OverlayButton");
        // Give the original drawable an explicit measured native action surface.
        var surface = new Grid { WidthRequest = 44, HeightRequest = 44 };
        surface.Children.Add(icon);
        surface.Children.Add(target);
        var prior = box.DropDownButtonSettings;
        var owned = new DropDownButtonSettings { View = surface, Width = 44, Height = 44 };
        void RefreshName(object? sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == SemanticProperties.DescriptionProperty.PropertyName)
                SemanticProperties.SetDescription(target, SemanticProperties.GetDescription(box));
        }
        SemanticProperties.SetDescription(target, SemanticProperties.GetDescription(box));
        box.PropertyChanged += RefreshName;
        box.DropDownButtonSettings = owned;
        var priorTemplate = box.ItemTemplate;
        // Use the editor's scalable MAUI typography and native action surface for original currency rows.
        // Keep the vendor's filtering and selection rather than maintaining another search algorithm.
        var template = new DataTemplate(() =>
        {
            var row = new Grid { Padding = new Thickness(12, 4), MinimumHeightRequest = 44, FlowDirection = FlowDirection.LeftToRight };
            var caption = new Label { FontSize = box.DropDownItemFontSize, FontAutoScalingEnabled = true,
                VerticalOptions = LayoutOptions.Center, LineBreakMode = LineBreakMode.NoWrap };
            caption.SetBinding(Label.TextProperty, new Binding("."));
            caption.SetDynamicResource(Label.FontFamilyProperty, "AppFont");
            caption.SetDynamicResource(Label.TextColorProperty, "AmountText");
            var choose = new Button { MinimumHeightRequest = 44, Margin = new Thickness(-12, -4),
                Command = new Command(() =>
                {
                    if (!box.IsEnabled || row.BindingContext is not string code) return;
                    box.SelectedItem = code;
                    box.IsDropDownOpen = false;
                }) };
            choose.SetDynamicResource(VisualElement.StyleProperty, "OverlayButton");
            void RefreshCaption()
            {
                if (row.BindingContext is not string code) return;
                row.SetDynamicResource(VisualElement.BackgroundColorProperty,
                    Equals(box.SelectedItem, code) ? "PrimarySoft" : "CardBackground");
                SemanticProperties.SetDescription(choose, Equals(box.SelectedItem, code)
                    ? Translator.Instance.Format("Common_ChipSelected", code) : code);
            }
            void OnOwnerChanged(object? sender, PropertyChangedEventArgs args) => RefreshCaption();
            row.BindingContextChanged += (_, _) => RefreshCaption();
            row.Loaded += (_, _) => { box.PropertyChanged -= OnOwnerChanged; box.PropertyChanged += OnOwnerChanged; RefreshCaption(); };
            row.Unloaded += (_, _) => box.PropertyChanged -= OnOwnerChanged;
            row.Children.Add(caption);
            row.Children.Add(choose);
            return row;
        });
        box.ItemTemplate = template;
        box.SetValue(CleanupProperty, (Action)(() =>
        {
            box.PropertyChanged -= RefreshName;
            if (ReferenceEquals(box.DropDownButtonSettings, owned)) box.DropDownButtonSettings = prior;
            if (ReferenceEquals(box.ItemTemplate, template)) box.ItemTemplate = priorTemplate;
        }));
    }
}
