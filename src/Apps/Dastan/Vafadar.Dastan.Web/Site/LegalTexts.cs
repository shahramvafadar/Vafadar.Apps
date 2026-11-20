using System.Collections.Immutable;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace Vafadar.Dastan.Web.Site;

/// <summary>
/// Who runs the site, as the legal pages name them (setting <c>Dastan:Operator</c>). The texts are part of the
/// program and say <c>{{operator.name}}</c> where a name belongs; what stands there comes from here, so that the
/// address of a person is in the settings of a deployment and not in the repository.
/// </summary>
public sealed class OperatorOptions
{
    /// <summary>Gets or sets the full name of the person who runs the site.</summary>
    public string? Name { get; set; }

    /// <summary>Gets or sets the street and number of an address at which that person can be served documents.</summary>
    public string? Street { get; set; }

    /// <summary>Gets or sets the postal code and the town.</summary>
    public string? Town { get; set; }

    /// <summary>Gets or sets the country.</summary>
    public string? Country { get; set; }

    /// <summary>Gets or sets the mail address for everything about the site.</summary>
    public string? Mail { get; set; }

    /// <summary>Gets or sets a telephone number, when the operator gives one.</summary>
    public string? Phone { get; set; }

    /// <summary>Gets or sets the address of the site, such as <c>https://dastan.online</c>.</summary>
    public string? Site { get; set; }

    /// <summary>Gets or sets who hosts the site: the name and the address of the company whose machines it runs on.</summary>
    public string? Host { get; set; }

    /// <summary>Gets or sets who carries the traffic of the site to its machine, when somebody other than the host does: the name and the address of the company.</summary>
    public string? Network { get; set; }

    /// <summary>
    /// Gets or sets who sends the mail of the site on its behalf (ADR 0016): the name and the address of the
    /// company the site hands its mail to. Left out, the privacy notice names no such company – which is right
    /// only where the operator's own machine sends the mail.
    /// </summary>
    public string? Mailer { get; set; }

    /// <summary>Gets or sets the authority for data protection that is competent for the operator, with its address.</summary>
    public string? Authority { get; set; }
}

/// <summary>A piece of a line of a legal text: words, or an address that is a link.</summary>
/// <param name="Text">The words.</param>
/// <param name="Href">Where it leads, when it is a link.</param>
public sealed record LegalInline(string Text, string? Href = null);

/// <summary>One block of a legal text.</summary>
/// <param name="Kind">What it is: <c>h2</c>, <c>h3</c>, <c>p</c> or <c>ul</c>.</param>
/// <param name="Lines">For a heading and a paragraph one line; for a list its items.</param>
public sealed record LegalBlock(string Kind, ImmutableArray<ImmutableArray<LegalInline>> Lines);

/// <summary>A legal text as a page shows it.</summary>
/// <param name="Language">The language it is written in.</param>
/// <param name="Blocks">Its blocks.</param>
/// <param name="Missing">What the text needs of the operator and the settings do not say.</param>
public sealed record LegalText(string Language, ImmutableArray<LegalBlock> Blocks, ImmutableArray<string> Missing);

/// <summary>
/// The legal pages of the site: who runs it (the imprint), what happens to what it knows about people (the
/// privacy notice), what holds between the site and who uses it (the terms), and what it puts on a device (the
/// page about cookies). The texts are part of the program, in English, Persian and German – German because the
/// site is run from Germany and answers to German law –; a folder the site is told
/// (<see cref="DastanOptions.Legal"/>) may hold a text that takes the place of the one of the program, so that
/// a lawyer's version needs no new build. A text is plain: a line that begins with <c>#</c> is a heading,
/// with <c>##</c> a smaller one, with <c>- </c> an item of a list; lines that follow each other are one
/// paragraph. Nothing in a text is taken as markup; an address or a mail address in it becomes a link.
/// </summary>
public sealed partial class LegalTexts
{
    /// <summary>The pages, by the last part of their address, in the order the foot of a page names them.</summary>
    public static IReadOnlyList<string> Pages { get; } = ["terms", "privacy", "cookies", "imprint"];

    /// <summary>The languages a legal text can be written in.</summary>
    public static IReadOnlyList<string> Languages { get; } = ["en", "fa", "de"];

    private const int Longest = 512 * 1024;
    private readonly string? _folder;
    private readonly IReadOnlyDictionary<string, string?> _values;

    /// <summary>Initializes a new instance of the <see cref="LegalTexts"/> class.</summary>
    public LegalTexts(IOptions<DastanOptions> options, IOptions<OperatorOptions> who, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(who);
        ArgumentNullException.ThrowIfNull(environment);
        _folder = string.IsNullOrWhiteSpace(options.Value.Legal) ? null : Path.GetFullPath(Path.Combine(environment.ContentRootPath, options.Value.Legal));
        var named = who.Value;
        _values = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["operator.name"] = named.Name,
            ["operator.street"] = named.Street,
            ["operator.town"] = named.Town,
            ["operator.country"] = named.Country,
            ["operator.mail"] = named.Mail,
            ["operator.phone"] = named.Phone,
            ["site"] = named.Site,
            ["host"] = named.Host,
            ["network"] = named.Network,
            ["mailer"] = named.Mailer,
            ["authority"] = named.Authority,
        };
    }

    /// <summary>
    /// Returns the text of a page in a language, or nothing when there is none in that language. A value of the
    /// operator that a text needs and the settings do not say is written as a visible gap and named in
    /// <see cref="LegalText.Missing"/>: a page with a gap is not a page to go public with.
    /// </summary>
    public LegalText? Of(string page, string language)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(language);
        if (!Pages.Contains(page) || !Languages.Contains(language) || Source(page, language) is not { } source)
        {
            return null;
        }

        var missing = new SortedSet<string>(StringComparer.Ordinal);
        var blocks = ImmutableArray.CreateBuilder<LegalBlock>();
        var paragraph = new List<string>();
        var items = new List<string>();
        var kept = new List<string>();
        foreach (var raw in source.Split('\n').Append(string.Empty))
        {
            var line = raw.Trim().TrimStart('﻿');

            // A line that names a value the operator may leave out (a telephone number) is left out with it.
            if (line.StartsWith('?'))
            {
                var needed = Placeholder().Matches(line).Select(match => match.Groups[1].Value).ToList();
                if (needed.Any(name => string.IsNullOrWhiteSpace(_values.GetValueOrDefault(name))))
                {
                    continue;
                }

                line = line[1..].TrimStart();
            }

            // Lines that keep their ends – an address – begin with a bar.
            var isKept = line.StartsWith("| ", StringComparison.Ordinal);
            if (!isKept && kept.Count > 0)
            {
                blocks.Add(new("lines", [.. kept.Select(item => Inlines(item, missing))]));
                kept.Clear();
            }

            var isItem = line.StartsWith("- ", StringComparison.Ordinal);
            if (!isItem && items.Count > 0)
            {
                blocks.Add(new("ul", [.. items.Select(item => Inlines(item, missing))]));
                items.Clear();
            }

            if (line.Length == 0 || line[0] == '#' || isItem || isKept)
            {
                if (paragraph.Count > 0)
                {
                    blocks.Add(new("p", [Inlines(string.Join(' ', paragraph), missing)]));
                    paragraph.Clear();
                }

                if (isItem)
                {
                    items.Add(line[2..].Trim());
                }
                else if (isKept)
                {
                    kept.Add(line[2..].Trim());
                }
                else if (line.TrimStart('#').Trim() is { Length: > 0 } heading)
                {
                    blocks.Add(new(line.StartsWith("##", StringComparison.Ordinal) ? "h3" : "h2", [Inlines(heading, missing)]));
                }
            }
            else
            {
                paragraph.Add(line);
            }
        }

        return blocks.Count == 0 ? null : new(language, blocks.ToImmutable(), [.. missing]);
    }

    /// <summary>Returns the languages a page has a text in, in the order of <see cref="Languages"/>.</summary>
    public IReadOnlyList<string> Versions(string page) => [.. Languages.Where(language => Pages.Contains(page) && Source(page, language) is not null)];

    /// <summary>Returns what the texts need of the operator and the settings do not say, over all pages and languages.</summary>
    public IReadOnlyList<string> Missing() =>
        [.. Pages.SelectMany(page => Languages.Select(language => Of(page, language))).OfType<LegalText>().SelectMany(text => text.Missing).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)];

    // The text as it is written: from the folder the site was told, when it has one for the page and the
    // language, else the one of the program.
    private string? Source(string page, string language)
    {
        if (_folder is not null)
        {
            var file = new FileInfo(Path.Combine(_folder, $"{page}.{language}.txt"));
            if (file.Exists && file.Length is > 0 and <= Longest)
            {
                return File.ReadAllText(file.FullName, Encoding.UTF8).ReplaceLineEndings("\n");
            }
        }

        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"Vafadar.Dastan.Web.Legal.{page}.{language}.txt");
        if (stream is null)
        {
            return null;
        }

        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd().ReplaceLineEndings("\n");
    }

    // A line as words and links: the values of the operator are put in, and what is an address becomes a link.
    private ImmutableArray<LegalInline> Inlines(string line, SortedSet<string> missing)
    {
        var filled = Placeholder().Replace(line, match =>
        {
            var name = match.Groups[1].Value;
            if (_values.GetValueOrDefault(name) is { Length: > 0 } value && !string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }

            missing.Add(name);
            return $"[… {name} …]";
        });

        var inlines = ImmutableArray.CreateBuilder<LegalInline>();
        var at = 0;
        foreach (Match link in Address().Matches(filled))
        {
            // What closes a sentence is not part of an address.
            var text = link.Value.TrimEnd('.', ',', ';', ':', ')', '!', '?', '،', '؛');
            if (text.Length == 0)
            {
                continue;
            }

            if (link.Index > at)
            {
                inlines.Add(new(filled[at..link.Index]));
            }

            inlines.Add(new(text, text.Contains("://", StringComparison.Ordinal) ? text : $"mailto:{text}"));
            at = link.Index + text.Length;
        }

        if (at < filled.Length)
        {
            inlines.Add(new(filled[at..]));
        }

        return inlines.ToImmutable();
    }

    [GeneratedRegex(@"\{\{([a-z.]+)\}\}", RegexOptions.CultureInvariant)]
    private static partial Regex Placeholder();

    [GeneratedRegex(@"https://[^\s<>""]+|[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}", RegexOptions.CultureInvariant)]
    private static partial Regex Address();
}
