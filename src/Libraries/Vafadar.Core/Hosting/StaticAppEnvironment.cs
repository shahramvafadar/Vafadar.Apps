namespace Vafadar.Core.Hosting;

/// <summary>
/// An <see cref="IAppEnvironment"/> with fixed values, for tests, tools and web hosts.
/// </summary>
public sealed record StaticAppEnvironment(
    string AppId,
    string DisplayName,
    Version Version,
    string? DeviceName = null,
    string Platform = "Unknown") : IAppEnvironment;
