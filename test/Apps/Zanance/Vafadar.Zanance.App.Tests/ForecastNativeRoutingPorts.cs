namespace Vafadar.Zanance.App.Features.Forecast;

/// <summary>Headless route constants only; actual Shell routing is compiled and qualified separately.</summary>
internal static class AppShell
{
    /// <summary>Retains the original saved-comparison route.</summary>
    public const string SnapshotRoute = AppRoutes.SnapshotRoute;
    /// <summary>Retains the original plan editor route.</summary>
    public const string PlanEditorRoute = AppRoutes.PlanEditorRoute;
    /// <summary>Retains the original occurrence route.</summary>
    public const string OccurrenceRoute = AppRoutes.OccurrenceRoute;
}

/// <summary>Headless metadata input only; no device package/version identity is asserted by these checks.</summary>
internal sealed class AppInfo
{
    /// <summary>Returns only this explicit headless metadata input.</summary>
    public static AppInfo Current { get; } = new();
    /// <summary>Returns the fictitious metadata version used by source-boundary checks.</summary>
    public string VersionString => "0.1.0-boundary";
}
