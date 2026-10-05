namespace Vafadar.Zanance.App.Presentation;

/// <summary>
/// A view model with colors computed in code (from <see cref="Palette"/>, e.g. a red amount or a category tile) instead
/// of bound as dynamic resources. A theme change recolors the open pages through this interface; the shell is not
/// rebuilt, because that would close an open editor and lose what was typed (the device can switch to dark on its own
/// while the user is entering an expense).
/// </summary>
/// <remarks>
/// Pages that are not shown reload when they appear again; an implementation must keep everything the user typed or
/// chose and only recompute what is drawn.
/// </remarks>
internal interface IThemeAware
{
    /// <summary>Recomputes the colors of the current theme.</summary>
    Task RefreshThemeAsync();
}
