namespace Vafadar.Core.Hosting;

/// <summary>
/// Describes the running app and the device it runs on.
/// </summary>
/// <remarks>
/// MAUI apps get an implementation from <c>Vafadar.Maui</c>; tests and web apps can use <see cref="StaticAppEnvironment"/>.
/// </remarks>
public interface IAppEnvironment
{
    /// <summary>Gets the stable app identifier, e.g. <c>pro.vafadar.finance</c> (the Android package name).</summary>
    string AppId { get; }

    /// <summary>Gets the user-facing app name.</summary>
    string DisplayName { get; }

    /// <summary>Gets the app version (e.g. <c>1.2.0</c>).</summary>
    Version Version { get; }

    /// <summary>Gets a human-readable name of the current device, if available.</summary>
    string? DeviceName { get; }

    /// <summary>Gets the platform name, e.g. <c>Android</c>, <c>iOS</c>, <c>Windows</c> or <c>Web</c>.</summary>
    string Platform { get; }
}
