using SkiaSharp;

namespace Vafadar.Dastan.Web.Site;

/// <summary>
/// The picture a link to the site is shown with where somebody passes it on (step 17.4, D-056): the mark of
/// the site on its ground, drawn by the site itself. One picture for every page – what a page is about is
/// said by its title and its description, which stand beside the picture. A picture with the words of a page
/// on it would need fonts for every script the site writes, in the program; that is not decided.
/// </summary>
public static class PreviewPicture
{
    /// <summary>The address of the picture, without a language.</summary>
    public const string Address = "/preview.png";

    /// <summary>The width of the picture in pixels: what the places that show a link ask for.</summary>
    public const int Width = 1200;

    /// <summary>The height of the picture in pixels.</summary>
    public const int Height = 630;

    private static readonly Lazy<byte[]> Drawn = new(Draw);

    /// <summary>Gets the picture, as a PNG. It is drawn once.</summary>
    public static byte[] Png => Drawn.Value;

    /// <summary>Adds the address of the picture to the site.</summary>
    public static IEndpointRouteBuilder MapPreviewPicture(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        endpoints.MapGet(Address, (HttpContext context) =>
        {
            // The picture is the same for everybody and changes with a release of the site only.
            context.Response.Headers.CacheControl = "public, max-age=86400";
            return Results.Bytes(Png, "image/png");
        });
        return endpoints;
    }

    // The mark of icon.svg – two paths that meet and one that continues – on the ground of the mark, as large
    // as leaves room around it. The mark is drawn in a square of 64, like the icon.
    private static byte[] Draw()
    {
        using var bitmap = new SKBitmap(new SKImageInfo(Width, Height, SKColorType.Rgba8888, SKAlphaType.Opaque));
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(new SKColor(0x2f, 0x2e, 0x7a));
            const float scale = 9f;
            canvas.Translate((Width - (64 * scale)) / 2, (Height - (64 * scale)) / 2);
            canvas.Scale(scale);
            using var line = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 5, StrokeCap = SKStrokeCap.Round };
            using var first = SKPath.ParseSvgPathData("M14 18c14 0 14 28 28 28");
            using var second = SKPath.ParseSvgPathData("M14 46c10 0 12-10 20-14s10-14 16-14");
            line.Color = new SKColor(0xfb, 0xfa, 0xf7);
            canvas.DrawPath(first, line);
            line.Color = new SKColor(0xf4, 0xb8, 0x60);
            canvas.DrawPath(second, line);
            using var dot = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill, Color = new SKColor(0xf4, 0xb8, 0x60) };
            canvas.DrawCircle(50, 18, 4.5f, dot);
        }

        using var image = SKImage.FromBitmap(bitmap);
        using var written = image.Encode(SKEncodedImageFormat.Png, 100);
        return written.ToArray();
    }
}
