using System.Collections.Immutable;
using Microsoft.Extensions.Options;
using Vafadar.Dastan.Core;
using Vafadar.Dastan.Data;

namespace Vafadar.Dastan.Web.Site;

/// <summary>Where the collection lives and who the site shows it to.</summary>
public sealed class DastanOptions
{
    /// <summary>Gets or sets the folder of the collection, relative to the folder of the app.</summary>
    public string Content { get; set; } = "content";

    /// <summary>Gets or sets the sample file inside the <c>samples</c> folder of the collection.</summary>
    public string Sample { get; set; } = "shahnameh-pilot.json";

    /// <summary>
    /// Gets or sets who the site shows the collection to. The default is the public; a collection that is not
    /// approved for the public is then not shown at all. A local demonstration has to be asked for.
    /// </summary>
    public Audience Audience { get; set; } = Audience.Public;
}

/// <summary>
/// The collection of the site, read once when the app starts, and its projections. Nothing else in the web project
/// holds the catalogue: pages get a <see cref="Projection"/> for the visitor's reading boundary and nothing more.
/// </summary>
public sealed class DastanSite
{
    // A transcript is a page of text; a file that is larger is not one and is not read into a page.
    private const int LargestText = 256 * 1024;

    private readonly Catalogue _catalogue;
    private readonly Lazy<Projection?>[] _projections;
    private readonly string _content;

    /// <summary>Initializes a new instance of the <see cref="DastanSite"/> class and reads the collection.</summary>
    /// <exception cref="SampleFormatException">The collection cannot be used; the app does not start.</exception>
    public DastanSite(IOptions<DastanOptions> options, IHostEnvironment environment)
    {
        var settings = options.Value;
        Audience = settings.Audience;
        _content = Path.GetFullPath(Path.Combine(environment.ContentRootPath, settings.Content));
        // The public gets reviewed texts only: a translation that is a draft is not read into a public site.
        var reviewedOnly = Audience == Audience.Public;
        _catalogue = PilotSample.Load(Path.Combine(_content, "samples", settings.Sample), reviewedOnly);

        // A comparison that is not released to the audience of the site does not exist for it.
        Comparisons =
        [
            .. ComparisonSample.LoadAll(Path.Combine(_content, "comparisons"), reviewedOnly).Where(comparison => comparison.MayBeShownTo(Audience)),
        ];

        // One projection per reading stage; the catalogue never changes, so a projection is built once.
        _projections =
        [
            .. _catalogue.Info.ReadingStages.Select(stage =>
                new Lazy<Projection?>(() => Projection.Of(_catalogue, stage.Level, Audience))),
        ];
    }

    /// <summary>Gets who the site shows the collection to.</summary>
    public Audience Audience { get; }

    /// <summary>Gets the comparisons the audience of the site may see, in the order of their files.</summary>
    public ImmutableArray<Comparison> Comparisons { get; }

    /// <summary>Returns the comparison with an identifier, or <see langword="null"/> when the site has none it may show.</summary>
    public Comparison? Comparison(string id) => Comparisons.FirstOrDefault(comparison => comparison.Id.Value == id);

    /// <summary>Gets the published revision the collection is, or <see langword="null"/> for an editorial sample.</summary>
    public PublicationRevision? Revision => _catalogue.Info.Publication.Revision;

    /// <summary>Gets the name of the collection in addresses and cookies: the identifier of its story.</summary>
    public string Collection => _catalogue.Info.Story.Value;

    /// <summary>Gets the stage a visitor starts at.</summary>
    public ReadingLevel DefaultLevel => _catalogue.Info.DefaultLevel;

    /// <summary>Gets the number of reading stages.</summary>
    public int StageCount => _projections.Length;

    /// <summary>
    /// Returns what a visitor at a reading boundary may see, or <see langword="null"/> when the collection is not
    /// released to the audience of the site. A boundary the collection does not have is the stage a visitor starts at.
    /// </summary>
    public Projection? At(ReadingLevel boundary) =>
        _projections[boundary.Value < _projections.Length ? boundary.Value : DefaultLevel.Value].Value;

    /// <summary>
    /// Returns where a file of the collection lies on this machine, or <see langword="null"/> when it does not exist
    /// or would lie outside the folder of the collection. The caller has asked the projection first: this method
    /// knows nothing about who may see the file.
    /// </summary>
    /// <param name="relative">The path as the model states it, relative to the folder of the collection.</param>
    public string? ContentFile(string? relative)
    {
        if (string.IsNullOrWhiteSpace(relative))
        {
            return null;
        }

        var full = Path.GetFullPath(Path.Combine(_content, relative));
        return full.StartsWith(_content + Path.DirectorySeparatorChar, StringComparison.Ordinal) && File.Exists(full) ? full : null;
    }

    /// <summary>Returns the text of a small text file of the collection, or <see langword="null"/> when there is none to show.</summary>
    /// <param name="relative">The path as the model states it, relative to the folder of the collection.</param>
    public string? ContentText(string? relative) =>
        ContentFile(relative) is { } path && new FileInfo(path).Length <= LargestText
            ? File.ReadAllText(path).ReplaceLineEndings("\n").Trim()
            : null;
}
