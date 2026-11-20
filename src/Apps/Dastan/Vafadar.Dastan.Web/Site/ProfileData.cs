using System.Globalization;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Localization;
using Vafadar.Dastan.Core;
using Vafadar.Dastan.People;
using Vafadar.Dastan.Web.Resources;
using Vafadar.Temporal;
using Vafadar.Web;

namespace Vafadar.Dastan.Web.Site;

/// <summary>
/// The profile of a node as the layer in the browser gets it (step 19.7, ADR 0019): the words the page of the
/// same visitor shows, written out – a name, a kind, a picture, the facts – with nothing to interpret and
/// nothing to hide in the browser.
/// </summary>
/// <param name="Name">The name.</param>
/// <param name="NameLanguage">The language of the name where it is not the language of the page; else nothing.</param>
/// <param name="Kind">What the node is, in the words of the page; nothing where a page says none.</param>
/// <param name="Address">The address of the page of the node, as a link of a page writes it.</param>
/// <param name="Summary">A summary, where the page of the node shows one.</param>
/// <param name="Picture">The picture, when this visitor may see one.</param>
/// <param name="NoPicture">What a page says in the place of a picture that is not there.</param>
/// <param name="Facts">The facts, in the order of the page.</param>
/// <param name="Note">What the page says about the profile as a whole, where it does.</param>
public sealed record ProfileData(
    string Name,
    string? NameLanguage,
    string? Kind,
    string Address,
    ProfileWords? Summary,
    ProfilePictureData? Picture,
    string NoPicture,
    IReadOnlyList<ProfileFactData> Facts,
    string? Note);

/// <summary>The picture of a profile.</summary>
/// <param name="Source">The address of the file.</param>
/// <param name="Sizes">The addresses of smaller sizes with their widths, as an image names them; nothing where there are none.</param>
/// <param name="Words">The words that describe it.</param>
public sealed record ProfilePictureData(string Source, string? Sizes, string Words);

/// <summary>A fact of a profile: what it is called and what is stated.</summary>
/// <param name="Label">The name of the fact.</param>
/// <param name="Values">What is stated; one value that says "not known" where nothing is.</param>
public sealed record ProfileFactData(string Label, IReadOnlyList<ProfileWords> Values);

/// <summary>Words of a profile.</summary>
/// <param name="Text">The words.</param>
/// <param name="Language">Their language where it is not the language of the page; else nothing.</param>
/// <param name="Address">Where the words lead, for a value that names another node.</param>
/// <param name="Unknown">Whether the words say that something is not known.</param>
public sealed record ProfileWords(
    string Text,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Language = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Address = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] bool Unknown = false);

/// <summary>
/// The addresses that give the browser the profile of a node (step 19.7, ADR 0019). Each answers with exactly
/// what the page of the same visitor shows: the profile of an entity from the projection at the stage the
/// visitor has read to and for the audience of the site; the profile of a person of a tree as the account
/// that asks may see the tree, or as everybody may when the tree is public. What a visitor may not see is
/// "not found", like its page. Nothing is sent and then hidden, and no answer may be kept: by nothing between
/// the site and the browser, and – for want of an address the service worker keeps – not on the device.
/// </summary>
public static class ProfileDataEndpoints
{
    /// <summary>The ending of the address of the profile of a node, after the address of its page.</summary>
    public const string Ending = "/profile.json";

    /// <summary>Returns the profile of an entity as the page of this visitor shows it.</summary>
    public static ProfileData Of(NodeProfile profile, Visitor visitor, DastanSite site, IStringLocalizer<Ui> text)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(visitor);
        ArgumentNullException.ThrowIfNull(site);
        ArgumentNullException.ThrowIfNull(text);
        var name = visitor.Text(profile.Entity.Label);
        var kind = text[$"Type_{profile.Entity.Type.Key}"];
        var picture = profile.Picture is { Kind: MediaKind.Image } item && site.ContentFile(item.File) is not null
            ? new ProfilePictureData(
                MediaAddress.File(site.Collection, item.Id),
                MediaAddress.SourceSet(site.Collection, item, file => site.ContentFile(file) is not null),
                visitor.Text(item.AltText ?? item.Label).Value)
            : null;
        return new(
            name.Value,
            Other(name, visitor),
            kind.ResourceNotFound ? profile.Entity.Type.Key : kind.Value,
            $"{site.Collection}/entity/{profile.Entity.Id}",
            Words(profile.Entity.Summary, visitor),
            picture,
            text["Profile_NoPicture"],
            [.. profile.Essential.Concat(profile.More).Select(line => new ProfileFactData(text[$"Fact_{line.Kind.Key}"], Values(line, visitor, site, text)))],
            null);
    }

    /// <summary>Returns the profile of a person of a tree as the page of who looks shows it.</summary>
    /// <param name="tree">The tree, as who looks may see it.</param>
    /// <param name="person">The person.</param>
    /// <param name="address">The address of the tree, below which the pages of its people stand.</param>
    /// <param name="visitor">Who looks.</param>
    /// <param name="text">The words of the pages.</param>
    public static ProfileData Of(TreeView tree, TreePerson person, string address, Visitor visitor, IStringLocalizer<Ui> text)
    {
        ArgumentNullException.ThrowIfNull(tree);
        ArgumentNullException.ThrowIfNull(person);
        ArgumentNullException.ThrowIfNull(visitor);
        ArgumentNullException.ThrowIfNull(text);
        ProfileWords Time(TemporalExpression? time) => time is null or UnknownTime ? Unknown(text) : new(TimeText.Of(time, visitor, text));
        ProfileWords Place(string? place) => place is null ? Unknown(text) : new(place);
        var facts = new List<ProfileFactData> { new(text["Fact_born"], [Time(person.Born)]) };
        if (!person.NotAsked)
        {
            facts.Add(new(text["Fact_birthPlace"], [Place(person.BirthPlace)]));
        }

        if (person.Deceased)
        {
            facts.Add(new(text["Fact_died"], [Time(person.Died)]));
            facts.Add(new(text["Fact_deathPlace"], [Place(person.DeathPlace)]));
        }
        else
        {
            facts.Add(new(text["Fact_died"], [new(text["Fact_died_None"])]));
        }

        return new(
            person.Name,
            null,
            null,
            $"{address}/person/{person.Id}",
            person.About is { } about ? new(about) : null,
            person.Picture is { } picture
                ? new($"{PhotoEndpoints.File}/{picture.Id}", null, picture.Caption.Length > 0 ? picture.Caption : text["Photos_Alt"])
                : null,
            text["Person_NoPicture"],
            facts,
            person.NotAsked ? text["Person_NotAsked"].Value : null);
    }

    /// <summary>Maps the addresses.</summary>
    public static void MapProfileData(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        // An entity of a collection: the address of its page, and "/profile.json".
        endpoints.MapGet("/{collection}/entity/{id}" + Ending, (HttpContext context, Visitor visitor, DastanSite site, IStringLocalizer<Ui> text, string collection, string id) =>
        {
            // An entity the visitor has not read to yet and one that does not exist are the same answer.
            var known = collection == site.Collection && id.Length > 0 && !id.Any(char.IsWhiteSpace);
            var profile = known && visitor.Projection?.Entity(new EntityId(id)) is not null ? visitor.Projection.Profile(new EntityId(id)) : null;
            return Answer(context, profile is null ? null : Of(profile, visitor, site, text));
        });

        // A person of a tree, for the account that may see the tree.
        endpoints.MapGet(TreeEndpoints.Page + "/{tree}/person/{id}" + Ending, (HttpContext context, AccountDesk desk, Visitor visitor, IStringLocalizer<Ui> text, string tree, string id) =>
        {
            TreeView? seen = null;
            if (desk.IsOpen && tree.Length <= 40 && id.Length <= 40 && AccountDesk.Session(context.User) is { } session)
            {
                using var store = desk.Open();
                seen = store.Tree(session.Id, tree);
            }

            var person = seen?.People.FirstOrDefault(candidate => candidate.Id == id);
            return Answer(context, person is null ? null : Of(seen!, person, $"account/trees/{seen!.Tree.Id}", visitor, text));
        });

        // A person of a tree that is public, for everybody.
        endpoints.MapGet(TreeEndpoints.Public + "/{tree}/person/{id}" + Ending, (HttpContext context, AccountDesk desk, Visitor visitor, IStringLocalizer<Ui> text, string tree, string id) =>
        {
            TreeView? seen = null;
            if (desk.IsOpen && tree.Length <= 40 && id.Length <= 40)
            {
                using var store = desk.Open();
                seen = store.PublicTree(tree);
            }

            var person = seen?.People.FirstOrDefault(candidate => candidate.Id == id);
            return Answer(context, person is null ? null : Of(seen!, person, $"trees/{seen!.Tree.Id}", visitor, text));
        });
    }

    // The answer depends on who asks – the stage read to, the account – so nothing keeps it; and it is data
    // for a page, not a page: what is not there is the status and no page for "not found".
    private static IResult Answer(HttpContext context, ProfileData? profile)
    {
        context.Response.Headers.CacheControl = "no-store";
        context.Response.Headers.Vary = "Cookie";
        context.Response.Headers["X-Robots-Tag"] = "noindex";
        if (profile is not null)
        {
            return Results.Json(profile);
        }

        if (context.Features.Get<Microsoft.AspNetCore.Diagnostics.IStatusCodePagesFeature>() is { } pages)
        {
            pages.Enabled = false;
        }

        return Results.NotFound();
    }

    private static List<ProfileWords> Values(ProfileLine line, Visitor visitor, DastanSite site, IStringLocalizer<Ui> text)
    {
        if (line.IsUnknown)
        {
            return [Unknown(text)];
        }

        var values = new List<ProfileWords>();
        foreach (var fact in line.Stated)
        {
            if (fact.Words is { } words)
            {
                values.Add(Words(words, visitor)!);
            }
            else if (fact.Node is { } node && visitor.Projection!.Find(node) is { } other)
            {
                values.Add(Words(other.Label, visitor)! with { Address = $"{site.Collection}/entity/{other.Id}" });
            }
            else if (fact.Time is UnknownTime)
            {
                values.Add(Unknown(text));
            }
            else if (fact.Time is InapplicableTime)
            {
                values.Add(new(text[$"Fact_{line.Kind.Key}_None"] is { ResourceNotFound: false } none ? none.Value : text["Profile_Inapplicable"].Value));
            }
            else if (fact.Time is { } time)
            {
                values.Add(new(TimeText.Of(time, visitor, text)));
            }
        }

        return values;
    }

    private static ProfileWords Unknown(IStringLocalizer<Ui> text) => new(text["Profile_Unknown"], Unknown: true);

    private static ProfileWords? Words(LocalizedText? words, Visitor visitor)
    {
        if (words is null)
        {
            return null;
        }

        var resolved = visitor.Text(words);
        return new(resolved.Value, Other(resolved, visitor));
    }

    private static string? Other(ResolvedText text, Visitor visitor) => text.Language == visitor.Language.Tag ? null : text.Language;
}
