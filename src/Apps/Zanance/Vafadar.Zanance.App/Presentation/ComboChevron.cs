using FluentIcons.Common;
using FluentIcons.Maui;
using Syncfusion.Maui.Inputs;

namespace Vafadar.Zanance.App.Presentation;

/// <summary>
/// Gives a Syncfusion combo box (the currency boxes) the same thin chevron as the other pickers instead of its filled
/// triangle (D-43). Set from a style; each box gets an icon of its own, because a view can have only one parent.
/// </summary>
public static class ComboChevron
{
    public static readonly BindableProperty IsEnabledProperty = BindableProperty.CreateAttached(
        "IsEnabled", typeof(bool), typeof(ComboChevron), false, propertyChanged: OnIsEnabledChanged);

    public static bool GetIsEnabled(BindableObject view) => (bool)view.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(BindableObject view, bool value) => view.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is not SfComboBox box || newValue is not true)
        {
            return;
        }

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
        box.DropDownButtonSettings = new DropDownButtonSettings { View = icon, Width = 36, Height = 36 };
    }
}
