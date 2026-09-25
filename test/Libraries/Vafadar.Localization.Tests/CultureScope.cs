using System.Globalization;

namespace Vafadar.Localization.Tests;

/// <summary>Restores all process cultures when disposed, so tests do not leak culture changes.</summary>
internal sealed class CultureScope : IDisposable
{
    private readonly CultureInfo _culture = CultureInfo.CurrentCulture;
    private readonly CultureInfo _uiCulture = CultureInfo.CurrentUICulture;
    private readonly CultureInfo? _defaultCulture = CultureInfo.DefaultThreadCurrentCulture;
    private readonly CultureInfo? _defaultUiCulture = CultureInfo.DefaultThreadCurrentUICulture;

    public static CultureScope WithUiCulture(string name)
    {
        var scope = new CultureScope();
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(name);
        return scope;
    }

    public void Dispose()
    {
        CultureInfo.CurrentCulture = _culture;
        CultureInfo.CurrentUICulture = _uiCulture;
        CultureInfo.DefaultThreadCurrentCulture = _defaultCulture;
        CultureInfo.DefaultThreadCurrentUICulture = _defaultUiCulture;
    }
}
