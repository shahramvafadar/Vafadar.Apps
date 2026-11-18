using Microsoft.Extensions.Options;

namespace Vafadar.Dastan.Web.Site;

/// <summary>One block of a legal text: a heading or a paragraph.</summary>
/// <param name="IsHeading">Whether the block is a heading.</param>
/// <param name="Text">Its words.</param>
public sealed record LegalBlock(bool IsHeading, string Text);

/// <summary>
/// The legal pages of the site (step 18.3.4): its terms, what it does with what it keeps about people, and who
/// is responsible for it. The site has the places; the words are the owner's and come from a folder the site is
/// told (<see cref="DastanOptions.Legal"/>) – nothing in the program says what a visitor agrees to. A text is a
/// plain file for a page and a language, <c>terms.en.txt</c>; a text that is not there is said to be missing,
/// on the page, in so many words.
/// </summary>
public sealed class LegalTexts
{
    /// <summary>The pages, by the last part of their address, in the order the foot of a page names them.</summary>
    public static IReadOnlyList<string> Pages { get; } = ["terms", "privacy", "imprint"];

    private const int Longest = 512 * 1024;
    private readonly string? _folder;

    /// <summary>Initializes a new instance of the <see cref="LegalTexts"/> class.</summary>
    public LegalTexts(IOptions<DastanOptions> options, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(environment);
        _folder = string.IsNullOrWhiteSpace(options.Value.Legal) ? null : Path.GetFullPath(Path.Combine(environment.ContentRootPath, options.Value.Legal));
    }

    /// <summary>Gets a value indicating whether the site was told a folder for its legal texts: only then it has the pages and names them.</summary>
    public bool IsOffered => _folder is not null;

    /// <summary>
    /// Returns the text of a page in a language as blocks, or nothing when it is not written: a line that begins
    /// with <c>#</c> is a heading, lines that follow each other are one paragraph, an empty line ends it.
    /// </summary>
    public IReadOnlyList<LegalBlock>? Of(string page, string language)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(language);
        if (_folder is null || !Pages.Contains(page) || language.Length is 0 or > 12 || !language.All(letter => char.IsAsciiLetterOrDigit(letter) || letter == '-'))
        {
            return null;
        }

        var file = new FileInfo(Path.Combine(_folder, $"{page}.{language}.txt"));
        if (!file.Exists || file.Length is 0 or > Longest)
        {
            return null;
        }

        var blocks = new List<LegalBlock>();
        var paragraph = new List<string>();
        foreach (var line in File.ReadLines(file.FullName).Select(line => line.Trim().TrimStart('﻿')).Append(string.Empty))
        {
            if (line.Length == 0 || line[0] == '#')
            {
                if (paragraph.Count > 0)
                {
                    blocks.Add(new(false, string.Join(' ', paragraph)));
                    paragraph.Clear();
                }

                if (line.TrimStart('#').Trim() is { Length: > 0 } heading)
                {
                    blocks.Add(new(true, heading));
                }
            }
            else
            {
                paragraph.Add(line);
            }
        }

        return blocks.Count == 0 ? null : blocks;
    }
}
