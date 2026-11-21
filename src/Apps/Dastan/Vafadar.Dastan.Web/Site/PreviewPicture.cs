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

    // The symbol of the logo (ADR 0026) in white on the indigo of the brand, as large as leaves room around
    // it. Its geometry is the one of the brand (BrandGeometry), so the picture cannot drift from the logo.
    private static byte[] Draw()
    {
        using var bitmap = new SKBitmap(new SKImageInfo(Width, Height, SKColorType.Rgba8888, SKAlphaType.Opaque));
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColor.Parse(BrandGeometry.Indigo));
            const float scale = 1f;
            canvas.Translate((Width - (BrandGeometry.SymbolWidth * scale)) / 2, (Height - (BrandGeometry.Height * scale)) / 2);
            canvas.Scale(scale);
            using var fill = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill, Color = SKColors.White };
            using var letter = SKPath.ParseSvgPathData(BrandGeometry.Letter);
            using var page = SKPath.ParseSvgPathData(BrandGeometry.Page);
            canvas.DrawPath(letter, fill);
            canvas.DrawPath(page, fill);
            canvas.DrawCircle(BrandGeometry.DotX, BrandGeometry.DotY, BrandGeometry.DotRadius, fill);
        }

        using var image = SKImage.FromBitmap(bitmap);
        using var written = image.Encode(SKEncodedImageFormat.Png, 100);
        return written.ToArray();
    }
}
