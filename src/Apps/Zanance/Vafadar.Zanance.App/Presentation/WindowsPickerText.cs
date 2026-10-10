#if WINDOWS
using Microsoft.UI.Xaml.Markup;
using DataTemplate = Microsoft.UI.Xaml.DataTemplate;

namespace Vafadar.Zanance.App.Presentation;

/// <summary>Allows native Windows picker captions to grow with the selected text and its accessibility scale.</summary>
internal static class WindowsPickerText
{
    private static bool _registered;

    /// <summary>Retains the native picker and its selection while replacing only its single-line item presentation.</summary>
    internal static void Register()
    {
        if (_registered) { return; }
        _registered = true;
        Microsoft.Maui.Handlers.PickerHandler.Mapper.AppendToMapping("ZananceGrowingPickerText", (handler, _) =>
        {
            // D-107: WinUI uses the item template for the closed selection as well as the popup. Its default single-line
            // text clips complete calendar/region names at large text sizes; inherit font, direction and colors.
            handler.PlatformView.ItemTemplate = (DataTemplate)XamlReader.Load("""
                <DataTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
                    <TextBlock Text="{Binding}" TextWrapping="Wrap" TextTrimming="None" />
                </DataTemplate>
                """);
            // The standard popup row can be shorter than the app's 44-unit touch target at normal text size.
            // Keep any existing item style and the native theme template; add only the required minimum surface.
            var itemStyle = new Microsoft.UI.Xaml.Style
            {
                TargetType = typeof(Microsoft.UI.Xaml.Controls.ComboBoxItem),
                BasedOn = handler.PlatformView.ItemContainerStyle,
            };
            itemStyle.Setters.Add(new Microsoft.UI.Xaml.Setter
            { Property = Microsoft.UI.Xaml.FrameworkElement.MinHeightProperty, Value = 44d });
            handler.PlatformView.ItemContainerStyle = itemStyle;
        });
    }
}
#endif
