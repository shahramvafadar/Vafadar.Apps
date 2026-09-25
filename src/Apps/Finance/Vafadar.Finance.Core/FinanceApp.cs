namespace Vafadar.Finance.Core;

/// <summary>
/// Identity of the Finance app.
/// </summary>
public static class FinanceApp
{
    /// <summary>
    /// The app id: Android package name, iOS bundle id and backup identifier.
    /// It can never change after the first store release.
    /// </summary>
    public const string AppId = "pro.vafadar.finance";

    /// <summary>The file name of the on-device database.</summary>
    public const string DatabaseFileName = "finance.db";
}
