using System.Security.Cryptography;
using System.Text.Json;
using Vafadar.Documents.Maui;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.Core.Receipts;

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    /// <summary>Reads ten fixed licensed public receipt images with the original on-device reader, without a financial writer.</summary>
    private static async Task<object> ReviewReceiptCorpusAsync(IServiceProvider services, string root, string comparison, CancellationToken ct)
    {
        RequireHomeWindowFocus();
        await using var source = await FileSystem.OpenAppPackageFileAsync("receipt-corpus/manifest.json");
        using var manifest = await JsonDocument.ParseAsync(source, cancellationToken: ct);
        var header = manifest.RootElement;
        if (header.GetProperty("Source").GetString() != "naver-clova-ix/cord-v2"
            || header.GetProperty("License").GetString() != "CC-BY-4.0"
            || header.GetProperty("Selection").GetString() != "first-ten-test-rows"
            || header.GetProperty("Cases").GetArrayLength() != 10)
            throw new ReviewCheckException("Exact licensed first-ten public corpus required.");
        var today = DateOnly.FromDateTime(services.GetRequiredService<TimeProvider>().GetLocalNow().DateTime);
        var results = new List<object>();
        var ids = new HashSet<int>();
        foreach (var item in header.GetProperty("Cases").EnumerateArray())
        {
            ct.ThrowIfCancellationRequested(); RequireHomeWindowFocus();
            var id = item.GetProperty("Id").GetInt32(); var file = item.GetProperty("File").GetString();
            if (id is < 0 or > 9 || !ids.Add(id) || file != $"test-{id:000}.jpg")
                throw new ReviewCheckException("A corpus image identity differs.");
            await using var input = await FileSystem.OpenAppPackageFileAsync("receipt-corpus/" + file);
            using var memory = new MemoryStream(); await input.CopyToAsync(memory, ct); var bytes = memory.ToArray();
            if (bytes.Length is < 3 or > 5 * 1024 * 1024 || bytes[0] != 0xff || bytes[1] != 0xd8
                || Convert.ToHexStringLower(SHA256.HashData(bytes)) != item.GetProperty("Sha256").GetString())
                throw new ReviewCheckException("Original receipt image bytes differ.");
            // Keep raw recognized text solely with this public corpus's private independent evidence. No Main file,
            // account, key, attachment, backup, or financial save service is opened by this qualification.
            var clock = System.Diagnostics.Stopwatch.StartNew();
            var layout = await DocumentReader.ReadDocumentAsync(bytes, "image/jpeg").WaitAsync(ct);
            clock.Stop(); RequireHomeWindowFocus();
            var suggestion = ReceiptParser.Parse(layout.Text, today, layout.IsAmbiguous);
            var result = new
            {
                Id = id, ObservationDate = today, ImageSha256 = item.GetProperty("Sha256").GetString(), ImageBytes = bytes.Length,
                AnnotatedTotal = item.GetProperty("AnnotatedTotal").GetString(), RecognitionMilliseconds = clock.Elapsed.TotalMilliseconds,
                RecognizedText = layout.Text, layout.IsAmbiguous, suggestion.AmountStatus, suggestion.Amount,
                suggestion.AmountSource, Candidates = suggestion.Candidates.Select(candidate => new
                {
                    candidate.Amount, candidate.CurrencyCode, candidate.MajorUnitFactor, candidate.UnitLabel, candidate.Source,
                    CanUseIdr = candidate.TryMinor("IDR", out _), CanUseEur = candidate.TryMinor("EUR", out _),
                }).ToArray(),
                ReaderModule = typeof(DocumentReader).Module.ModuleVersionId, ParserModule = typeof(ReceiptParser).Module.ModuleVersionId,
                RecognitionCompleted = true, FinancialSaves = 0,
            };
            await File.WriteAllTextAsync(Path.Combine(root, $"receipt-{id:000}.json"), JsonSerializer.Serialize(result), ct);
            results.Add(result);
        }
        return new { Comparison = comparison, Source = "naver-clova-ix/cord-v2", License = "CC-BY-4.0",
            Selection = "first-ten-test-rows", Results = results, OriginalDocumentReader = true, OriginalReceiptParser = true,
            FinancialSaves = 0, MainPackageTouched = false, SystemSettingsChanged = false,
            Boundary = "Finite public Latin-script corpus; not six-language, camera, saved-entry or release acceptance", Device = DeviceEvidence() };
    }
}
