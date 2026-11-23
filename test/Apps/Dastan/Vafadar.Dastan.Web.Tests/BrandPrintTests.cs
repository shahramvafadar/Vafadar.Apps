using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Vafadar.Dastan.Web.Tests;

/// <summary>
/// The files of the logo for print and the guide of the brand (ADR 0026, addendum of 2026-11-23, D-087): a
/// file for print is read as a reader of a PDF reads it, holds outlines and the colours of print and nothing
/// else, and is cut to the logo with its clear space; the guide draws the geometry of the brand and no other.
/// </summary>
public sealed class BrandPrintTests
{
    private static readonly string Repository = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", ".."));
    private static readonly string Brand = Path.Combine(Repository, "branding", "dastan");

    // The colours of print, as the guide and the README name them.
    private static readonly string[] Colours = ["0.9 0.88 0 0.05", "0.7 0.65 0.3 0.85", "0 0 0 1", "0 0 0 0"];

    public static TheoryData<string, int, int> Files => new()
    {
        { "dastan-logo-color-cmyk", 1214, 346 },
        { "dastan-logo-black", 1214, 346 },
        { "dastan-logo-reversed-cmyk", 1214, 346 },
        { "dastan-symbol-indigo-cmyk", 330, 346 },
        { "dastan-symbol-black", 330, 346 },
        { "dastan-symbol-reversed-cmyk", 330, 346 },
    };

    private static string Read(string name) => File.ReadAllText(Path.Combine(Brand, "exports", "print", name), Encoding.Latin1);

    [Theory]
    [MemberData(nameof(Files))]
    public void A_file_for_print_is_a_whole_PDF_of_outlines_cut_to_the_clear_space(string name, int width, int height)
    {
        var pdf = Read(name + ".pdf");
        using var geometry = JsonDocument.Parse(File.ReadAllText(Path.Combine(Brand, "source", "dastan-geometry.json")));
        var clear = (int)Math.Round(geometry.RootElement.GetProperty("symbol").GetProperty("dot").GetProperty("r").GetDouble() * 2);

        // Plain text with the line ends it was written with: the places of its objects are counted in bytes.
        Assert.StartsWith("%PDF-1.4\n", pdf, StringComparison.Ordinal);
        Assert.DoesNotContain('\r', pdf);
        Assert.EndsWith("%%EOF\n", pdf, StringComparison.Ordinal);

        // The table at the end says where every object begins, and every object begins there.
        var table = int.Parse(Regex.Match(pdf, @"startxref\n(\d+)\n").Groups[1].Value, CultureInfo.InvariantCulture);
        Assert.StartsWith("xref\n0 6\n", pdf[table..], StringComparison.Ordinal);
        var places = Regex.Matches(pdf[table..], @"(\d{10}) 00000 n \n").Select(place => int.Parse(place.Groups[1].Value, CultureInfo.InvariantCulture)).ToList();
        Assert.Equal(5, places.Count);
        Assert.All(places.Select((place, index) => (place, index)), entry => Assert.StartsWith($"{entry.index + 1} 0 obj\n", pdf[entry.place..], StringComparison.Ordinal));

        // The stream is as long as it says.
        var stream = Regex.Match(pdf, @"<< /Length (\d+) >>\nstream\n(.*?)\nendstream", RegexOptions.Singleline);
        Assert.Equal(int.Parse(stream.Groups[1].Value, CultureInfo.InvariantCulture), stream.Groups[2].Value.Length);

        // The page is the drawing with its clear space on every side; its trim box is the drawing.
        Assert.Contains($"/MediaBox [0 0 {width + clear * 2} {height + clear * 2}]", pdf, StringComparison.Ordinal);
        Assert.Contains($"/TrimBox [{clear} {clear} {width + clear} {height + clear}]", pdf, StringComparison.Ordinal);

        // Outlines and nothing else: no picture of pixels, no font, no text, nothing of a screen's colours.
        Assert.DoesNotMatch(@"/Image|/Font|/XObject|\bBT\b|\brg\b|\bRG\b|/DeviceRGB", pdf);
        var content = stream.Groups[2].Value;
        Assert.All(Regex.Matches(content, @"^(.*) k$", RegexOptions.Multiline).Select(colour => colour.Groups[1].Value), colour => Assert.Contains(colour, Colours));
        Assert.Contains(" c\n", content, StringComparison.Ordinal);
        Assert.Equal(name.Contains("reversed", StringComparison.Ordinal), content.Contains(" re f\n", StringComparison.Ordinal));

        // The wordmark has holes – the bowls of its letters – and is filled so; the symbol has none.
        Assert.Equal(name.Contains("logo", StringComparison.Ordinal) ? 1 : 0, Regex.Matches(content, @"^f\*$", RegexOptions.Multiline).Count);
        Assert.Equal(3, Regex.Matches(content, @"^f$", RegexOptions.Multiline).Count);
    }

    [Theory]
    [MemberData(nameof(Files))]
    public void The_EPS_of_a_file_for_print_says_the_same_as_its_PDF(string name, int width, int height)
    {
        var (eps, pdf) = (Read(name + ".eps"), Read(name + ".pdf"));

        Assert.StartsWith("%!PS-Adobe-3.0 EPSF-3.0\n", eps, StringComparison.Ordinal);
        Assert.DoesNotContain('\r', eps);
        Assert.Contains($"%%BoundingBox: 0 0 {width + 116} {height + 116}\n", eps, StringComparison.Ordinal);
        Assert.EndsWith("grestore\n%%EOF\n", eps, StringComparison.Ordinal);
        Assert.DoesNotMatch(@"setrgbcolor|\bimage\b|findfont|\bshow\b", eps);

        // The same steps in the same order: a move, a curve, a line and a closing in one are the same in the other.
        static IEnumerable<string> Steps(string text, string move, string line, string curve, string close) =>
            Regex.Matches(text, $@"^(?:([\d. -]+) ({move}|{line}|{curve})|({close}))$", RegexOptions.Multiline)
                .Select(step => step.Groups[3].Success ? "z" : step.Groups[1].Value + " " + (step.Groups[2].Value == move ? "m" : step.Groups[2].Value == line ? "l" : "c"));
        var stream = Regex.Match(pdf, @"stream\n(.*?)\nendstream", RegexOptions.Singleline).Groups[1].Value;
        var drawn = Steps(stream, "m", "l", "c", "h").ToList();
        Assert.NotEmpty(drawn);
        Assert.Equal(drawn, Steps(eps, "moveto", "lineto", "curveto", "closepath"));
        Assert.Equal(Regex.Matches(stream, @"^(.*) k$", RegexOptions.Multiline).Select(colour => colour.Groups[1].Value), Regex.Matches(eps, @"^(.*) setcmykcolor$", RegexOptions.Multiline).Select(colour => colour.Groups[1].Value));
    }

    [Fact]
    public void The_guide_of_the_brand_draws_the_geometry_of_the_brand_and_fetches_nothing()
    {
        var guide = File.ReadAllText(Path.Combine(Brand, "guide", "brand-guide.html"));
        using var geometry = JsonDocument.Parse(File.ReadAllText(Path.Combine(Brand, "source", "dastan-geometry.json")));
        var symbol = geometry.RootElement.GetProperty("symbol");

        Assert.Contains($"d=\"{symbol.GetProperty("letter").GetString()}\"", guide, StringComparison.Ordinal);
        Assert.Contains($"d=\"{symbol.GetProperty("page").GetString()}\"", guide, StringComparison.Ordinal);
        Assert.Contains($"d=\"{geometry.RootElement.GetProperty("wordmark").GetProperty("outline").GetString()}\"", guide, StringComparison.Ordinal);

        // One page that holds everything it shows: no script, no picture of pixels, nothing asked of another place.
        Assert.DoesNotMatch("<script|<img|<link|@import|url\\(\\s*['\"]?http|src=|<iframe", guide);

        // It says the colours of print the files carry, and that they are a beginning.
        Assert.Contains("90 / 88 / 0 / 5", guide, StringComparison.Ordinal);
        Assert.Contains("70 / 65 / 30 / 85", guide, StringComparison.Ordinal);
        Assert.Contains("not a measurement", guide, StringComparison.Ordinal);
        Assert.True(File.Exists(Path.Combine(Brand, "guide", "dastan-brand-guide.pdf")));
    }
}
