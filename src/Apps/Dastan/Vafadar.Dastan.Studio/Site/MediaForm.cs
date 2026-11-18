using System.Globalization;
using System.Text.Json.Nodes;
using Vafadar.Dastan.Data;

namespace Vafadar.Dastan.Studio.Site;

/// <summary>
/// A media item and the files that were brought in for it (step 13.7): where a record names a file that was
/// taken in, and the record a new item starts with.
/// </summary>
public static class MediaForm
{
    /// <summary>The longer sides of the smaller sizes the tool makes of an image that is brought in (D-056): one for a card among others, one for a page.</summary>
    public static IReadOnlyList<int> Sizes { get; } = [480, 1200];

    // The smaller sizes that were made of the file of an image, as the record names them; an image that got
    // none names none – those of the file it had before are not its sizes.
    private static void Smaller(JsonObject record, TakenFile file) => Smaller(record, file.Sizes);

    private static void Smaller(JsonObject record, IReadOnlyList<SmallerImage> sizes)
    {
        record.Remove("sizes");
        if (sizes.Count > 0)
        {
            record["sizes"] = new JsonArray([.. sizes.Select(size => (JsonNode)new JsonObject { ["width"] = size.Width, ["url"] = TakenFile.UrlOf(size) })]);
        }
    }

    /// <summary>
    /// Returns where the file of an image lies when the item is an image whose file was brought in and that
    /// names no smaller sizes yet – an image the tool can make sizes of (step 18.3.1); else nothing.
    /// </summary>
    /// <param name="item">The record of the item.</param>
    /// <param name="samplesFolder">The folder of the samples; the files lie in the folder <c>media</c> beside it.</param>
    public static string? FileWithoutSizes(JsonObject item, string samplesFolder)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentException.ThrowIfNullOrWhiteSpace(samplesFolder);
        const string beside = "../media/";
        if ((string?)item["type"] != "image" || item["sizes"] is not null || item["url"] is not JsonValue address
            || !address.TryGetValue<string>(out var url) || !url.StartsWith(beside, StringComparison.Ordinal))
        {
            return null;
        }

        var name = url[beside.Length..];
        var path = Path.GetFullPath(Path.Combine(samplesFolder, "..", "media", name));
        return name == Path.GetFileName(name) && File.Exists(path) ? path : null;
    }

    /// <summary>Returns the record of an item with the smaller sizes that were made of its image.</summary>
    public static JsonObject WithSizes(JsonObject item, IReadOnlyList<SmallerImage> sizes)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(sizes);
        var record = item.DeepClone().AsObject();
        Smaller(record, sizes);
        return record;
    }

    /// <summary>
    /// Returns the record of an item with a file that was taken in for one of its parts, or nothing when the file
    /// is of another kind than the item – a video brought in for an image.
    /// </summary>
    public static JsonObject? With(JsonObject item, string part, string? language, TakenFile file)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(file);
        var record = item.DeepClone().AsObject();
        switch (part)
        {
            case "file":
                if ((string?)record["type"] != (file.Kind == MediaFileKind.Video ? "video" : "image"))
                {
                    return null;
                }

                record["url"] = file.Url;
                if (file is { Width: { } width, Height: { } height })
                {
                    record["dimensions"] = new JsonObject { ["width"] = width, ["height"] = height };
                }

                Smaller(record, file);
                break;

            case "poster":
                record["posterUrl"] = file.Url;
                break;

            case "captions":
                var tracks = record["captions"] as JsonArray;
                if (tracks is null)
                {
                    record["captions"] = tracks = [];
                }

                // The captions of a language stand in the place of those the item had in that language.
                if (tracks.FirstOrDefault(track => (string?)track!["language"] == language) is { } held)
                {
                    held["url"] = file.Url;
                }
                else
                {
                    tracks.Add(new JsonObject { ["language"] = language, ["url"] = file.Url });
                }

                break;

            case "transcript":
                record["transcriptUrl"] = file.Url;
                record["transcriptFormat"] = "text/plain";
                break;

            default:
                var renditions = record["renditions"] as JsonArray;
                if (renditions is null)
                {
                    record["renditions"] = renditions = [];
                }

                renditions.Add(new JsonObject { ["format"] = file.ContentType, ["url"] = file.Url });
                break;
        }

        return record;
    }

    /// <summary>
    /// Returns the record a new item starts with: its file, what the editor states about the right to show it –
    /// an item whose rights are not established has no file – and that it is not shown yet. An item becomes
    /// visible by what an editor then says about it, never by being brought in.
    /// </summary>
    public static JsonObject New(string id, string label, string caption, string creator, string source, string world, string rights, string statement, DateOnly today, TakenFile file)
    {
        ArgumentNullException.ThrowIfNull(file);
        var record = new JsonObject
        {
            ["id"] = id,
            ["type"] = file.Kind == MediaFileKind.Video ? "video" : "image",
            ["label"] = label,
            ["url"] = file.Url,
            ["creator"] = creator,
            ["rights"] = new JsonObject
            {
                ["status"] = rights,
                ["statement"] = statement,
                ["checkedAt"] = today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            },
            ["depictedEntityIds"] = new JsonArray(),
            ["depictedEventIds"] = new JsonArray(),
            ["depictedContext"] = new JsonObject { ["kind"] = "present_day_explanatory_diagram", ["worldId"] = world },
            ["creationDate"] = new JsonObject
            {
                ["label"] = today.Year.ToString(CultureInfo.InvariantCulture), ["year"] = today.Year,
                ["precision"] = "year", ["approximate"] = false, ["era"] = "CE",
            },
            ["spoilerLevel"] = 0,
            ["sourceIds"] = new JsonArray(source),
            ["caption"] = caption,
            ["demoRenderEligibility"] = new JsonObject { ["allowed"] = false, ["reason"] = "Brought in; nobody has said yet that it may be shown." },
        };
        if (file is { Width: { } width, Height: { } height })
        {
            record["dimensions"] = new JsonObject { ["width"] = width, ["height"] = height };
        }

        Smaller(record, file);
        return record;
    }
}
