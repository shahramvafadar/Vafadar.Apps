using System.Globalization;

namespace Vafadar.Localization;

/// <summary>
/// A user interface language an app can be displayed in.
/// </summary>
/// <param name="CultureName">The .NET culture name, e.g. <c>fa</c>. Resource files use the same name (<c>*.fa.resx</c>).</param>
/// <param name="NativeName">The name of the language in the language itself, shown in language pickers.</param>
/// <param name="EnglishName">The English name of the language.</param>
public sealed record AppLanguage(string CultureName, string NativeName, string EnglishName)
{
    /// <summary>Gets the culture for this language.</summary>
    public CultureInfo Culture => CultureInfo.GetCultureInfo(CultureName);

    /// <summary>Gets a value indicating whether the language is written right-to-left.</summary>
    public bool IsRightToLeft => Culture.TextInfo.IsRightToLeft;

    /// <summary>Returns <see cref="NativeName"/>, so pickers can display the language without a display binding.</summary>
    public override string ToString() => NativeName;
}
