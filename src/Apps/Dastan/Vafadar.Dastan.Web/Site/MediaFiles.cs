using System.Collections.Frozen;
using System.Globalization;
using Microsoft.AspNetCore.Diagnostics;
using Vafadar.Dastan.Core;

namespace Vafadar.Dastan.Web.Site;

/// <summary>
/// The addresses of the files of a media item. An address names the item and which of its files is meant – never a
/// path: the path is looked up in the model after the projection has allowed the item.
/// </summary>
public static class MediaAddress
{
    /// <summary>The beginning of every address of a media file. It carries no language.</summary>
    public const string Root = "/media";

    /// <summary>Returns the address of the image or the video itself.</summary>
    public static string File(string collection, MediaId id) => $"{Root}/{collection}/{id}/file";

    /// <summary>Returns the address of the poster of a video.</summary>
    public static string Poster(string collection, MediaId id) => $"{Root}/{collection}/{id}/poster";

    /// <summary>Returns the address of the transcript of a video.</summary>
    public static string Transcript(string collection, MediaId id) => $"{Root}/{collection}/{id}/transcript";

    /// <summary>Returns the address of the captions of a video in a language.</summary>
    public static string Captions(string collection, MediaId id, string language) => $"{Root}/{collection}/{id}/captions-{language}";

    /// <summary>Returns the address of a smaller size of an image, by its place among the sizes of the item.</summary>
    public static string Size(string collection, MediaId id, int index) =>
        $"{Root}/{collection}/{id}/size-{index.ToString(CultureInfo.InvariantCulture)}";

    /// <summary>
    /// Returns what tells a browser the sizes of an image – each with its width – so that it takes the one that
    /// fits where the picture stands; nothing for an image without smaller sizes, a video, or an image that does
    /// not say how wide it is.
    /// </summary>
    /// <param name="collection">The collection of the item.</param>
    /// <param name="item">The item.</param>
    /// <param name="available">Whether a file of the collection is there to be given.</param>
    public static string? SourceSet(string collection, MediaItem item, Func<string?, bool> available)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(available);
        if (item.Kind != MediaKind.Image || item.Width is not { } whole)
        {
            return null;
        }

        var sizes = item.Sizes
            .Select((size, index) => (size.Width, size.File, Address: Size(collection, item.Id, index)))
            .Where(size => available(size.File))
            .Select(size => $"{size.Address} {size.Width.ToString(CultureInfo.InvariantCulture)}w")
            .ToList();
        return sizes.Count == 0 ? null : string.Join(", ", sizes.Append($"{File(collection, item.Id)} {whole.ToString(CultureInfo.InvariantCulture)}w"));
    }

    /// <summary>Returns the address of another format of an image, by its place in the list of the item.</summary>
    public static string Rendition(string collection, MediaId id, int index) =>
        $"{Root}/{collection}/{id}/rendition-{index.ToString(CultureInfo.InvariantCulture)}";
}

/// <summary>
/// The only way a file of the collection reaches a browser. Every request is answered from the visitor's own
/// projection: an item they have not read to, an item that is not released and an item that does not exist are
/// the same answer – not found, with nothing in it.
/// </summary>
public static class MediaFiles
{
    // What the site serves, by the ending of the file. A file of another kind is not served, whatever the model says.
    private static readonly FrozenDictionary<string, string> Types = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        [".png"] = "image/png",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".webp"] = "image/webp",
        [".svg"] = "image/svg+xml",
        [".mp4"] = "video/mp4",
        [".webm"] = "video/webm",
        [".vtt"] = "text/vtt; charset=utf-8",
        [".txt"] = "text/plain; charset=utf-8",
    }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    /// <summary>Returns the type of a file as the site serves it, or <see langword="null"/> for a file it does not serve.</summary>
    public static string? TypeOf(string path) => Types.GetValueOrDefault(Path.GetExtension(path));

    /// <summary>Returns the path of one of the files of an item inside the folder of the collection, or <see langword="null"/>.</summary>
    /// <param name="item">The item, as the projection returned it.</param>
    /// <param name="part">The last part of the address: <c>file</c>, <c>poster</c>, <c>transcript</c>, <c>captions-en</c>, <c>rendition-0</c>.</param>
    public static string? PathOf(MediaItem item, string part)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(part);

        const string captions = "captions-";
        const string rendition = "rendition-";
        const string size = "size-";
        if (part.StartsWith(size, StringComparison.Ordinal))
        {
            return int.TryParse(part.AsSpan(size.Length), NumberStyles.None, CultureInfo.InvariantCulture, out var place) && place < item.Sizes.Length
                ? item.Sizes[place].File
                : null;
        }

        if (part.StartsWith(captions, StringComparison.Ordinal))
        {
            return item.Captions.FirstOrDefault(track => track.Language == part[captions.Length..])?.File;
        }

        if (part.StartsWith(rendition, StringComparison.Ordinal))
        {
            return int.TryParse(part.AsSpan(rendition.Length), NumberStyles.None, CultureInfo.InvariantCulture, out var index)
                && index < item.Renditions.Length
                    ? item.Renditions[index].File
                    : null;
        }

        return part switch
        {
            "file" => item.File,
            "poster" => item.Poster,
            "transcript" => item.Transcript,
            _ => null,
        };
    }

    /// <summary>Maps the addresses of <see cref="MediaAddress"/>.</summary>
    public static IEndpointConventionBuilder MapMediaFiles(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet(MediaAddress.Root + "/{collection}/{id}/{part}", (HttpContext context, DastanLibrary library, string collection, string id, string part) =>
        {
            // The answer depends on the visitor's reading position, so nothing between the site and the browser may
            // keep it, and the browser asks again each time: after the visitor moved back, the file is gone.
            context.Response.Headers.CacheControl = "private, no-cache";
            context.Response.Headers.Vary = "Cookie";

            var site = library.Find(collection);
            var projection = site?.At(ReadingPreference.Read(context, site));
            var item = id.Any(char.IsWhiteSpace) ? null : projection?.Find(new MediaId(id));
            var relative = item is null ? null : PathOf(item, part);
            if (site?.ContentFile(relative) is not { } path || !Types.TryGetValue(Path.GetExtension(path), out var type))
            {
                // A file is asked for by a page, not by a reader: the answer is the status and no page.
                if (context.Features.Get<IStatusCodePagesFeature>() is { } pages)
                {
                    pages.Enabled = false;
                }

                return Results.NotFound();
            }

            // A video is asked for in pieces, so that it can start before it has arrived and be moved in.
            return Results.File(path, type, enableRangeProcessing: true);
        });
}
