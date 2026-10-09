namespace Vafadar.Zanance.App.Presentation;

public sealed partial class ThemeService
{
    /// <summary>Attaches the MAUI application before applying its saved theme.</summary>
    public void Initialize(Application app)
    {
        ArgumentNullException.ThrowIfNull(app);
        ((MauiThemeHost)_host).Attach(app);
        Initialize();
    }
}

/// <summary>Updates native controls and the existing semantic palette; application flows stay open.</summary>
internal sealed class MauiThemeHost : IThemeHost
{
    private Application? _app;

    /// <inheritdoc />
    public event EventHandler? SystemThemeChanged;

    /// <inheritdoc />
    public bool IsAvailable => _app is not null;

    /// <inheritdoc />
    public bool SystemIsDark => _app?.PlatformAppTheme == AppTheme.Dark;

    /// <summary>Attaches once to the current application instance.</summary>
    internal void Attach(Application app)
    {
        if (_app == app) { return; }
        if (_app is not null) { _app.RequestedThemeChanged -= OnSystemThemeChanged; }
        _app = app;
        app.RequestedThemeChanged += OnSystemThemeChanged;
    }

    private void OnSystemThemeChanged(object? sender, AppThemeChangedEventArgs e) => SystemThemeChanged?.Invoke(this, EventArgs.Empty);

    /// <inheritdoc />
    public void Apply(ThemeChoice choice, bool dark)
    {
        if (_app is null) { return; }
        _app.UserAppTheme = choice switch { ThemeChoice.Light => AppTheme.Light, ThemeChoice.Dark => AppTheme.Dark, _ => AppTheme.Unspecified };
        Palette.Apply(_app.Resources, dark);
    }
}
