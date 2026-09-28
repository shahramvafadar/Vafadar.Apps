namespace Vafadar.Zanance.Core;

/// <summary>
/// Identity of the Zanance app.
/// </summary>
public static class ZananceApp
{
    /// <summary>
    /// The app id: Android package name, iOS bundle id and backup identifier.
    /// It can never change after the first store release.
    /// </summary>
    public const string AppId = "pro.vafadar.zanance";

    /// <summary>The file name of the on-device database.</summary>
    public const string DatabaseFileName = "zanance.db";

    /// <summary>The file name used before the rename to Zanance (D-24); an existing file is moved once.</summary>
    public const string LegacyDatabaseFileName = "finance.db";
}
