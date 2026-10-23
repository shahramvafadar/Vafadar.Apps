using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Documents;
using Vafadar.Documents.Maui;
using Vafadar.Zanance.Core.Receipts;

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    /// <summary>Observes fixed image-only PDFs through the original renderer/reader without a financial writer.</summary>
    private static async Task<object> ReviewScannedReceiptsAsync(IServiceProvider services, string root,
        string comparison, CancellationToken ct)
    {
        RequireHomeWindowFocus();
        await using var source = await FileSystem.OpenAppPackageFileAsync("scanned-receipts/manifest.json");
        using var manifest = await JsonDocument.ParseAsync(source, cancellationToken: ct);
        var header = manifest.RootElement;
        if (header.GetProperty("Selection").GetString() != "fixed-six-language-scans-and-page-boundaries"
            || header.GetProperty("Cases").GetArrayLength() != 9)
            throw new ReviewCheckException("Exact independent scanned-receipt cohort required.");
        var today = DateOnly.FromDateTime(services.GetRequiredService<TimeProvider>().GetLocalNow().DateTime);
        var results = new List<object>();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in header.GetProperty("Cases").EnumerateArray())
        {
            ct.ThrowIfCancellationRequested(); RequireHomeWindowFocus();
            var id = item.GetProperty("Id").GetString()!;
            if (id is not ("scan-en" or "scan-de" or "scan-es" or "scan-fr" or "scan-it" or "scan-fa"
                or "scan-last-page" or "scan-conflict" or "scan-no-total") || !ids.Add(id)
                || item.GetProperty("File").GetString() != id + ".pdf")
                throw new ReviewCheckException("A scanned-receipt identity differs.");
            await using var input = await FileSystem.OpenAppPackageFileAsync("scanned-receipts/" + id + ".pdf");
            using var memory = new MemoryStream(); await input.CopyToAsync(memory, ct); var bytes = memory.ToArray();
            if (bytes.Length > 8 * 1024 * 1024 || !PdfText.IsPdf(bytes)
                || Convert.ToHexStringLower(SHA256.HashData(bytes)) != item.GetProperty("Sha256").GetString())
                throw new ReviewCheckException("Original scanned PDF bytes differ.");
            // The original extractor must find no text layer, proving this is the unqualified raster path.
            var pageCount = await Task.Run(() => PdfText.PageCount(bytes), ct);
            var layer = await Task.Run(() => PdfText.Extract(bytes, DocumentReader.MaxPages, minimumPerPage: 0), ct);
            if (pageCount != item.GetProperty("PageCount").GetInt32() || !string.IsNullOrWhiteSpace(layer))
                throw new ReviewCheckException("Image-only PDF/page-count precondition differs.");
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var layout = await DocumentReader.ReadDocumentAsync(bytes, "application/pdf").WaitAsync(ct);
            watch.Stop(); RequireHomeWindowFocus();
            var suggestion = ReceiptParser.Parse(layout.Text, today, layout.IsAmbiguous);
            // Expectations are compared by the host after recognition; they never guide the reader/parser.
            var result = new
            {
                Id = id, Language = item.GetProperty("Language").GetString(), ObservationDate = today,
                PdfSha256 = item.GetProperty("Sha256").GetString(), PdfBytes = bytes.Length, PageCount = pageCount,
                SelectedPages = TextLayout.PagesToRead(pageCount, DocumentReader.MaxPages), TextLayerEmpty = true,
                RecognitionMilliseconds = watch.Elapsed.TotalMilliseconds, RecognizedText = layout.Text,
                layout.IsAmbiguous, suggestion.AmountStatus, suggestion.Amount, suggestion.AmountSource,
                Candidates = suggestion.Candidates.Select(candidate => new
                {
                    candidate.Amount, candidate.CurrencyCode, candidate.MajorUnitFactor, candidate.UnitLabel,
                    candidate.Source, CanUseEur = candidate.TryMinor("EUR", out _),
                }).ToArray(),
                ReaderModule = typeof(DocumentReader).Module.ModuleVersionId,
                ParserModule = typeof(ReceiptParser).Module.ModuleVersionId, FinancialSaves = 0,
            };
            await File.WriteAllTextAsync(Path.Combine(root, id + ".json"), JsonSerializer.Serialize(result), ct);
            results.Add(result);
        }
        return new { Comparison = comparison, Results = results, OriginalDocumentReader = true,
            OriginalReceiptParser = true, FinancialSaves = 0, MainPackageTouched = false,
            SystemSettingsChanged = false, Boundary = "Synthetic scanned PDFs; no Persian image-OCR or full quality acceptance",
            Device = DeviceEvidence() };
    }
}
