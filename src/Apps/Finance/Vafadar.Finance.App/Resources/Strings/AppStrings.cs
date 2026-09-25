using System.Resources;

namespace Vafadar.Finance.App.Resources.Strings;

/// <summary>
/// The app's string resources (<c>AppResources.resx</c> and its translations).
/// </summary>
/// <remarks>
/// Keys use the <c>Area_Name</c> convention. Shared strings (<c>Common_*</c>, <c>Settings_*</c>, <c>Backup_*</c>)
/// come from Vafadar.Localization; defining the same key here overrides them for this app.
/// </remarks>
internal static class AppStrings
{
    public static ResourceManager ResourceManager { get; } =
        new("Vafadar.Finance.App.Resources.Strings.AppResources", typeof(AppStrings).Assembly);
}
