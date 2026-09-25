using Vafadar.Localization;

namespace Vafadar.Maui.Localization;

/// <summary>
/// Applies the current language's text direction (left-to-right or right-to-left) to MAUI elements.
/// </summary>
public static class FlowDirectionExtensions
{
    /// <summary>Gets the flow direction of the current language.</summary>
    public static FlowDirection GetFlowDirection(this ILocalizationService localization)
    {
        ArgumentNullException.ThrowIfNull(localization);
        return localization.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
    }

    /// <summary>Sets <paramref name="element"/>'s flow direction to the current language's direction.</summary>
    public static TElement WithFlowDirection<TElement>(this TElement element, ILocalizationService localization)
        where TElement : VisualElement
    {
        ArgumentNullException.ThrowIfNull(element);
        element.FlowDirection = localization.GetFlowDirection();
        return element;
    }

    /// <summary>Applies the current language's direction to the root page of every open window.</summary>
    internal static void ApplyToAllWindows(ILocalizationService localization)
    {
        var direction = localization.GetFlowDirection();
        foreach (var window in Application.Current?.Windows ?? [])
        {
            if (window.Page is { } page)
            {
                page.FlowDirection = direction;
            }
        }
    }
}
