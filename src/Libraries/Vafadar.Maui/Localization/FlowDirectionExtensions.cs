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

    /// <summary>
    /// Gives every modal page the current language's direction before it is shown. Modal pages are not part of
    /// the window's visual tree, so they do not inherit the direction of the root page. Call once from the
    /// <see cref="Application"/> constructor.
    /// </summary>
    public static void ApplyToModalPages(this Application application, ILocalizationService localization)
    {
        ArgumentNullException.ThrowIfNull(application);
        ArgumentNullException.ThrowIfNull(localization);
        application.ModalPushing += (_, e) => e.Modal.FlowDirection = localization.GetFlowDirection();
    }

    /// <summary>Applies the current language's direction to the root page and open modal pages of every window.</summary>
    internal static void ApplyToAllWindows(ILocalizationService localization)
    {
        var direction = localization.GetFlowDirection();
        foreach (var window in Application.Current?.Windows ?? [])
        {
            if (window.Page is { } page)
            {
                page.FlowDirection = direction;
                foreach (var modal in page.Navigation.ModalStack)
                {
                    modal.FlowDirection = direction;
                }
            }
        }
    }
}
