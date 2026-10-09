namespace Vafadar.Zanance.App.Presentation;

/// <summary>The platform's effective theme and semantic-resource application, independent of preference policy.</summary>
public interface IThemeHost
{
    /// <summary>Raised when the system's light/dark choice changes.</summary>
    event EventHandler? SystemThemeChanged;

    /// <summary>Gets whether an application window can receive theme changes.</summary>
    bool IsAvailable { get; }

    /// <summary>Gets the current system theme.</summary>
    bool SystemIsDark { get; }

    /// <summary>Applies the requested platform choice and effective semantic palette.</summary>
    void Apply(ThemeChoice choice, bool dark);
}
