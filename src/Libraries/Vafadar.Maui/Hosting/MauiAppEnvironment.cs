using Vafadar.Core.Hosting;

namespace Vafadar.Maui.Hosting;

/// <summary>
/// <see cref="IAppEnvironment"/> backed by MAUI's <see cref="AppInfo"/> and <see cref="DeviceInfo"/>.
/// </summary>
/// <remarks>
/// The app id is configured explicitly instead of using <see cref="IAppInfo.PackageName"/>, which differs between
/// platforms (e.g. unpackaged Windows apps). Backups created on one platform must restore on another.
/// </remarks>
internal sealed class MauiAppEnvironment(string appId) : IAppEnvironment
{
    public string AppId { get; } = appId;

    public string DisplayName => AppInfo.Current.Name;

    public Version Version => AppInfo.Current.Version;

    public string? DeviceName => DeviceInfo.Current.Name;

    public string Platform => DeviceInfo.Current.Platform.ToString();
}
