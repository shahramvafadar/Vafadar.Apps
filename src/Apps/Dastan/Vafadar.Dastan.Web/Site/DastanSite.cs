using System.Collections.Immutable;
using Microsoft.Extensions.Options;
using Vafadar.Dastan.Core;
using Vafadar.Dastan.Data;

namespace Vafadar.Dastan.Web.Site;

/// <summary>Where the collection lives and who the site shows it to.</summary>
public sealed class DastanOptions
{
    /// <summary>
    /// Returns the connection of the database for a part of the site that was switched on and keeps what it
    /// takes there; a site that was told to take it and has no database, or no tables for it, does not start.
    /// </summary>
    internal string DeliveryDatabase(string setting)
    {
        if (string.IsNullOrWhiteSpace(Database))
        {
            throw new InvalidOperationException($"{setting} is kept in the database of the site, and no database is named: set Dastan:Database.");
        }

        return Vafadar.Dastan.Data.Delivery.Missing(Database) is { } missing ? throw new InvalidOperationException(missing) : Database;
    }

    /// <summary>Gets or sets the folder of the collection, relative to the folder of the app.</summary>
    public string Content { get; set; } = "content";

    /// <summary>Gets or sets the sample file inside the <c>samples</c> folder of the collection.</summary>
    public string Sample { get; set; } = "shahnameh-pilot.json";

    /// <summary>
    /// Gets or sets a value indicating whether the site reads its collections from the published revisions of
    /// its database (<see cref="Database"/>, ADR 0015) – for each collection the revision that was released –
    /// and not from the files of the folder <c>samples</c> – and its comparisons from the ones that were released,
    /// not from the folder <c>comparisons</c>. The files of media stay in the folder of content.
    /// A site that reads from the database shows what was released when it started.
    /// </summary>
    public bool Published { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the collection a visitor enters first, for a site that reads its
    /// collections from the database. Empty, it is the first by its identifier.
    /// </summary>
    public string? First { get; set; }

    /// <summary>
    /// Gets or sets who the site shows the collection to. The default is the public; a collection that is not
    /// approved for the public is then not shown at all. A local demonstration has to be asked for.
    /// </summary>
    public Audience Audience { get; set; } = Audience.Public;

    /// <summary>
    /// Gets or sets a value indicating whether the site takes the reports of visitors (step 14.8) and keeps them
    /// in its database (<see cref="Database"/>, ADR 0015). Off, the site offers no form for them.
    /// </summary>
    public bool Reports { get; set; }

    /// <summary>
    /// Gets or sets a text who runs the site has to tell everybody who uses it, shown on every page until it is
    /// taken out of the settings again: that the terms change, that the site or a part of it ends, that
    /// something happened to its security. The site knows no mail address of its visitors, so this is how it
    /// reaches them. Written by the operator in the languages of the site, as plain text.
    /// </summary>
    public string? Announcement { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the site takes notices about content with a form
    /// (<see cref="NoticeDesk"/>) and keeps them in its database (<see cref="Database"/>). Off, the page for a
    /// notice names the mail address of who runs the site and has no form.
    /// </summary>
    public bool Notices { get; set; }

    /// <summary>
    /// Gets or sets the connection of the PostgreSQL database of the site (ADR 0015), as Npgsql reads it. With
    /// it the site has accounts of visitors and what belongs to them; without it nothing of that exists. Its
    /// password is a secret: it is told to the running site and never written into a file of the repository.
    /// </summary>
    public string? Database { get; set; }

    /// <summary>
    /// Gets the server the mail of the site leaves by (<c>Dastan:Mail</c>, ADR 0016): its name and port, how
    /// the connection is protected, who the site signs in as, and the address it sends from. Without a server
    /// the site sends no mail. The password is a secret: it is told to the running site and never written into
    /// a file of the repository.
    /// </summary>
    public Vafadar.Dastan.Data.MailOptions Mail { get; } = new();

    /// <summary>
    /// Gets or sets the folder the photographs of the visitors are kept in (step 16.5), on a volume no web
    /// server shows. Empty – or without <see cref="People"/> – the site has no photographs.
    /// </summary>
    public string? Photos { get; set; }

    /// <summary>
    /// Gets or sets the folder of the legal texts of the site (step 18.3.4): its terms, its privacy notice and
    /// its imprint, a plain file for each page and language. Empty, the site has no such pages. The words are
    /// the owner's; the site has only the places.
    /// </summary>
    public string? Legal { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a new account needs an invitation (D-068): the site of a closed
    /// trial. Who has an account invites others; nobody else makes one. The first account is made before this
    /// is set.
    /// </summary>
    public bool InvitationOnly { get; set; }

    /// <summary>
    /// Gets or sets a folder in which the keys that protect the forms of the site are kept, so that a form a
    /// visitor opened before the site was started again is still accepted afterwards. Without it the keys live as
    /// long as the process: enough for a development machine, not for a site that is updated while it is visited.
    /// </summary>
    public string? Keys { get; set; }

    /// <summary>
    /// Gets or sets the proxies the site believes when they say how a visitor came – by https, from which
    /// address: single addresses or networks (<c>10.0.0.0/8</c>). Empty, the site believes nobody.
    /// </summary>
    public IList<string> TrustedProxies { get; } = [];
}

/// <summary>
/// Every collection of the site (step 14.10), read once when the app starts: the files of the folder
/// <c>samples</c> that are collections, the one the site was told first (<see cref="DastanOptions.Sample"/>)
/// and then the others by their names. A collection that cannot be used stops the app.
/// </summary>
public sealed class DastanLibrary
{
    // The first part of an address that is no collection: a collection of that name could not be reached.
    private static readonly string[] Taken = ["account", "compare", "cookies", "media", "imprint", "notice", "parts", "people", "photo", "preferences", "preview.png", "privacy", "terms", "report", "saved", "search", "themes", "signin-google", "signin-microsoft", "trees", "robots.txt", "sitemap.xml", "health", "error", "not-found", "refused"];

    /// <summary>Returns whether a first part of an address belongs to the site itself, so that no collection can have that name.</summary>
    public static bool IsTaken(string name) => Taken.Contains(name, StringComparer.OrdinalIgnoreCase);

    /// <summary>Initializes a new instance of the <see cref="DastanLibrary"/> class and reads the collections.</summary>
    /// <exception cref="SampleFormatException">A collection cannot be used; the app does not start.</exception>
    public DastanLibrary(IOptions<DastanOptions> options, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(environment);
        var settings = options.Value;
        Audience = settings.Audience;
        var content = Path.GetFullPath(Path.Combine(environment.ContentRootPath, settings.Content));

        // The public gets reviewed texts only: a translation that is a draft is not read into a public site.
        var reviewedOnly = Audience == Audience.Public;

        // A comparison that is not released to the audience of the site does not exist for it.
        // A site that reads from the database reads its comparisons there too: the ones that were released.
        var comparisons = settings.Published && !string.IsNullOrWhiteSpace(settings.Database)
            ? [.. PublishedCollections.ReadComparisons(settings.Database).Select(released => ComparisonSample.Read(released.Json, released.Translations, reviewedOnly))]
            : ComparisonSample.LoadAll(Path.Combine(content, "comparisons"), reviewedOnly);
        Comparisons = [.. comparisons.Where(comparison => comparison.MayBeShownTo(Audience))];

        var samples = Path.Combine(content, "samples");
        if (settings.Published)
        {
            // What was released, as the database holds it; the files of media are looked for where they always lie.
            if (string.IsNullOrWhiteSpace(settings.Database))
            {
                throw new InvalidOperationException("Dastan:Published asks for the collections of the database, and no database is named: set Dastan:Database.");
            }

            var released = PublishedCollections.Read(settings.Database);
            if (released.IsEmpty)
            {
                throw new InvalidOperationException("The database holds no released revision: publish a collection and release it (\"store release\"), then start the site again.");
            }

            Collections =
            [
                .. released.OrderByDescending(revision => revision.Collection == settings.First).Select(revision => new DastanSite(
                    PilotSample.Read(revision.Json, new PilotSampleOptions
                    {
                        SampleDirectory = samples,
                        ContentRoot = content,
                        ReviewRoot = Path.GetDirectoryName(content) ?? content,
                        Translations = [.. revision.Translations],
                        ReviewedTextsOnly = reviewedOnly,
                    }),
                    Audience,
                    content,
                    Comparisons)),
            ];
        }
        else
        {
            // No collection named to begin with: the site begins with the first file of the folder, in the
            // order of the names – a folder that is written into, where no file is the first for good.
            var first = string.IsNullOrWhiteSpace(settings.Sample)
                ? (Directory.Exists(samples) ? Directory.EnumerateFiles(samples, "*.json").Where(IsCollection).Order(StringComparer.Ordinal).FirstOrDefault() : null)
                    ?? throw new InvalidOperationException($"The folder {samples} holds no collection: the site has nothing to show. Write one, or name the folder of another content root in Dastan:Content.")
                : Path.Combine(samples, settings.Sample);
            IEnumerable<string> others = Directory.Exists(samples)
                ? Directory.EnumerateFiles(samples, "*.json").Where(file => !PathsEqual(file, first) && IsCollection(file)).Order(StringComparer.Ordinal)
                : [];
            Collections = [.. new[] { first }.Concat(others).Select(file => new DastanSite(PilotSample.Load(file, reviewedOnly), Audience, content, Comparisons))];
        }

        var twice = Collections.GroupBy(site => site.Collection, StringComparer.Ordinal).FirstOrDefault(group => group.Count() > 1);
        if (twice is not null)
        {
            throw new InvalidOperationException($"Two collections of the site are about {twice.Key}: an address can lead to one of them only.");
        }

        // A correspondence the editors state with another collection of the site names stages that collection has.
        foreach (var site in Collections)
        {
            foreach (var correspondence in site.Correspondences)
            {
                if (Find(correspondence.Collection) is { } other && correspondence.Stages.Values.Any(there => there >= other.StageCount))
                {
                    throw new InvalidOperationException($"{site.Collection} states a correspondence with a stage {correspondence.Collection} does not have.");
                }
            }
        }

        if (Collections.FirstOrDefault(site => Taken.Contains(site.Collection, StringComparer.Ordinal)) is { } unreachable)
        {
            throw new InvalidOperationException($"A collection about {unreachable.Collection} cannot be reached: the site has an address of that name.");
        }
    }

    /// <summary>Gets who the site shows its collections to.</summary>
    public Audience Audience { get; }

    /// <summary>Gets the collections, the one a visitor enters first at the beginning.</summary>
    public ImmutableArray<DastanSite> Collections { get; }

    /// <summary>Gets the comparisons the audience of the site may see, in the order of their files.</summary>
    public ImmutableArray<Comparison> Comparisons { get; }

    /// <summary>
    /// Gets the places of the collections that people can have been at (step 20.4, ADR 0023), read from what
    /// a visitor who has read nothing may see of each collection: a place that only a later stage of a story
    /// shows is none a private statement can point at – pointing at it would say that it exists.
    /// </summary>
    public Institutions Institutions => _institutions ??= new(Collections
        .Select(site => (site.Collection, Projection: site.At(new ReadingLevel(0))))
        .Where(read => read.Projection is not null)
        .Select(read => (read.Collection, read.Projection!)));

    private Institutions? _institutions;

    /// <summary>
    /// Returns the other collections of the site that are about the same world (step 14.11): another edition
    /// of the same work, another telling of the same story. Each is a collection of its own.
    /// </summary>
    public ImmutableArray<DastanSite> SameWorld(DastanSite collection)
    {
        ArgumentNullException.ThrowIfNull(collection);
        return [.. Collections.Where(other => other != collection && other.World == collection.World)];
    }

    /// <summary>Returns the collection of that name, or <see langword="null"/>.</summary>
    public DastanSite? Find(string? collection) => Collections.FirstOrDefault(site => site.Collection == collection);

    /// <summary>
    /// Returns the collection a request is about: the one its address names first, or the first collection of the
    /// site for an address that names none – the start page, a comparison.
    /// </summary>
    public DastanSite Of(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var path = context.Request.Path.Value ?? string.Empty;
        var end = path.IndexOf('/', 1);
        var named = path.Length > 1 ? (end < 0 ? path[1..] : path[1..end]) : string.Empty;
        return Find(named) ?? Collections[0];
    }

    // A file of the folder is a collection when it says so: a translation and anything else is none.
    private static bool IsCollection(string file)
    {
        try
        {
            using var stream = File.OpenRead(file);
            using var document = System.Text.Json.JsonDocument.Parse(stream);
            return document.RootElement.ValueKind == System.Text.Json.JsonValueKind.Object && document.RootElement.TryGetProperty("schemaVersion", out _);
        }
        catch (System.Text.Json.JsonException)
        {
            // A file that is no JSON is no collection the site could offer; the check of the content says what it is.
            return false;
        }
    }

    private static bool PathsEqual(string one, string other) =>
        string.Equals(Path.GetFullPath(one), Path.GetFullPath(other), OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
}

/// <summary>
/// One collection of the site and its projections. A request has the one its address names
/// (<see cref="DastanLibrary.Of"/>). Nothing else in the web project holds the catalogue: pages get a
/// <see cref="Projection"/> for the visitor's reading boundary and nothing more.
/// </summary>
public sealed class DastanSite
{
    // A transcript is a page of text; a file that is larger is not one and is not read into a page.
    private const int LargestText = 256 * 1024;

    private readonly Catalogue _catalogue;
    private readonly Lazy<Projection?>[] _projections;
    private readonly string _content;

    internal DastanSite(Catalogue catalogue, Audience audience, string content, ImmutableArray<Comparison> comparisons)
    {
        _catalogue = catalogue;
        _content = content;
        Audience = audience;
        Comparisons = comparisons;

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

    /// <summary>Gets the world the collection is about: collections that name the same world tell of the same things.</summary>
    public string World => _catalogue.Info.WorldId;

    /// <summary>Gets what the editors state about the stages of other tellings of the same world.</summary>
    public ImmutableArray<StageCorrespondence> Correspondences => _catalogue.Info.Correspondences;

    /// <summary>
    /// Returns the stage of another collection that a reader of a stage of this one has read, as the editors
    /// state it; <see langword="null"/> when they state nothing for it.
    /// </summary>
    public ReadingLevel? Carried(DastanSite other, ReadingLevel here)
    {
        ArgumentNullException.ThrowIfNull(other);
        return Correspondences.FirstOrDefault(correspondence => correspondence.Collection == other.Collection)?.For(here);
    }

    /// <summary>Gets the language the texts of the collection are written in.</summary>
    public string Language => _catalogue.Info.Language;

    /// <summary>Gets the name of the collection in addresses and cookies: the identifier of what it is about.</summary>
    public string Collection => _catalogue.Info.Subject.Value;

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
