using System.Xml.Linq;

namespace Vafadar.Testing;

/// <summary>
/// Reads the languages a project declares for its texts (ADR 0011): the <c>VafadarLanguages</c> property of the nearest
/// <c>Directory.Build.props</c> above a file. Apps declare their own list; the repository root holds the default.
/// </summary>
/// <remarks>
/// Only <c>Directory.Build.props</c> files are read, not project files and not conditions: the declaration is one
/// plain list per folder tree, so a test can find it without evaluating MSBuild.
/// </remarks>
public static class ProjectLanguages
{
    /// <summary>The MSBuild property that holds the list, separated by semicolons.</summary>
    public const string PropertyName = "VafadarLanguages";

    /// <summary>The language of every neutral resource file; always the first of a list.</summary>
    public const string Neutral = "en";

    private const string DeclarationFileName = "Directory.Build.props";

    /// <summary>Returns the culture names declared for <paramref name="path"/>, a file or folder in the repository.</summary>
    public static IReadOnlyList<string> Of(string path) => Of(path, RepositoryPaths.Root);

    /// <summary>
    /// Returns the culture names declared for <paramref name="path"/>, searching upwards as far as
    /// <paramref name="root"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// No declaration exists between the path and the root, or a declaration does not start with English.
    /// </exception>
    public static IReadOnlyList<string> Of(string path, string root)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(root);

        var rootFolder = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
        var start = Path.GetFullPath(path);
        for (var folder = Directory.Exists(start) ? new DirectoryInfo(start) : new FileInfo(start).Directory;
             folder is not null;
             folder = folder.Parent)
        {
            var declaration = Path.Combine(folder.FullName, DeclarationFileName);
            if (File.Exists(declaration) && Read(declaration) is { } languages)
            {
                return languages;
            }

            if (string.Equals(Path.TrimEndingDirectorySeparator(folder.FullName), rootFolder, StringComparison.OrdinalIgnoreCase))
            {
                break;
            }
        }

        throw new InvalidOperationException($"No {PropertyName} is declared in a {DeclarationFileName} between '{path}' and '{root}'.");
    }

    private static IReadOnlyList<string>? Read(string declarationFile)
    {
        var value = XDocument.Load(declarationFile).Descendants(PropertyName).FirstOrDefault()?.Value;
        if (value is null)
        {
            return null;
        }

        var languages = value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (languages.Length == 0 || languages[0] != Neutral)
        {
            throw new InvalidOperationException(
                $"{PropertyName} in '{declarationFile}' must start with '{Neutral}', the neutral language of the resource files.");
        }

        return languages;
    }
}
