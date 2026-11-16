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
    private readonly Catalogue _catalogue;
    private readonly Lazy<Projection?>[] _projections;

    /// <summary>Initializes a new instance of the <see cref="DastanSite"/> class and reads the collection.</summary>
    /// <exception cref="SampleFormatException">The collection cannot be used; the app does not start.</exception>
    public DastanSite(IOptions<DastanOptions> options, IHostEnvironment environment)
    {
        var settings = options.Value;
        Audience = settings.Audience;
        _catalogue = PilotSample.Load(Path.Combine(environment.ContentRootPath, settings.Content, "samples", settings.Sample));

        // One projection per reading stage; the catalogue never changes, so a projection is built once.
        _projections =
        [
            .. _catalogue.Info.ReadingStages.Select(stage =>
                new Lazy<Projection?>(() => Projection.Of(_catalogue, stage.Level, Audience))),
        ];
    }

    /// <summary>Gets who the site shows the collection to.</summary>
    public Audience Audience { get; }

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
}
