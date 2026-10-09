using FluentIcons.Maui;

namespace Vafadar.Zanance.App.Presentation;

/// <summary>
/// Keeps decorative font glyphs inside their icon tiles while adjacent readable text follows system scaling.
/// The icon library measures its inner formatted label at an unscaled size, so scaling that glyph clips it (D-77).
/// </summary>
public static class IconTextScale
{
    /// <summary>Identifies the fixed decorative glyph size used by the app's symbol style.</summary>
    public static readonly BindableProperty IsFixedProperty = BindableProperty.CreateAttached(
        "IsFixed", typeof(bool), typeof(IconTextScale), false, propertyChanged: OnChanged);

    /// <summary>Gets whether this symbol keeps its explicit decorative size.</summary>
    public static bool GetIsFixed(BindableObject view) => (bool)view.GetValue(IsFixedProperty);

    /// <summary>Sets whether this symbol keeps its explicit decorative size.</summary>
    public static void SetIsFixed(BindableObject view, bool value) => view.SetValue(IsFixedProperty, value);

    private static void OnChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is not SymbolIcon icon) { return; }
        Apply(icon);
        icon.Loaded -= OnLoaded;
        if (newValue is true) { icon.Loaded += OnLoaded; }
    }

    private static void OnLoaded(object? sender, EventArgs args)
    {
        if (sender is SymbolIcon icon) { Apply(icon); }
    }

    private static void Apply(SymbolIcon icon)
    {
        // Use the public content tree; do not depend on private fields or replace the library's glyph renderer.
        if (icon.Content is not Layout layout) { return; }
        foreach (var label in layout.Children.OfType<Label>())
        {
            label.FontAutoScalingEnabled = !GetIsFixed(icon);
            if (label.FormattedText is not { } formatted) { continue; }
            foreach (var span in formatted.Spans) { span.FontAutoScalingEnabled = label.FontAutoScalingEnabled; }
        }
    }
}
