#if AI_PRIVATE_REVIEW
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Vafadar.Zanance.App.Features.Analysis;

namespace Vafadar.Zanance.App;

/// <summary>Two independent Release processes qualify the actual bounded private adapter, never production data or an AI service.</summary>
internal static class AndroidAnalysisPrivateReview
{
    internal static async Task RunAsync(string run, bool resume)
    {
        string? root = null;
        try
        {
            Require(AppInfo.Current.PackageName == "pro.vafadar.zanance.analysisprivatereview" && run.Length == 32 && run.All(char.IsAsciiHexDigit));
            root = Path.Combine(FileSystem.AppDataDirectory, "analysis-fixture", run); Directory.CreateDirectory(root);
            var marker = Path.Combine(root, "fixture.marker");
            if (!File.Exists(marker)) { Require(!resume); await File.WriteAllTextAsync(marker, "Fictitious private analysis d298"); }
            Require(await File.ReadAllTextAsync(marker) == "Fictitious private analysis d298" && !File.Exists(Path.Combine(root, "complete.json")));
            var scope = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes("fictitious-analysis-bound:" + run)));
            var storage = new SecureAiStorage(); var expected = new string('x', 65536);
            if (!resume)
            {
                // An observation-channel repair resumes an already staged original slot; it never writes a substitute value.
                var original = await storage.ReadAsync(scope, CancellationToken.None);
                if (original is not null) Require(original == expected);
                else
                {
                    Require(!File.Exists(Path.Combine(root, "staged.json")));
                    await storage.WriteAsync(scope, expected, CancellationToken.None);
                }
                Require(await storage.ReadAsync(scope, CancellationToken.None) == expected);
                var report = JsonSerializer.Serialize(new AnalysisPrivateReport(true, false, 65536, true, false), AnalysisPrivateJson.Default.AnalysisPrivateReport);
                if (!File.Exists(Path.Combine(root, "staged.json"))) await File.WriteAllTextAsync(Path.Combine(root, "staged.json"), report);
                Android.Util.Log.Info("ZananceAnalysisPrivate", report);
            }
            else
            {
                Require(File.Exists(Path.Combine(root, "staged.json")) && await storage.ReadAsync(scope, CancellationToken.None) == expected);
                // Exact fictional slot only: surviving public loss marker must refuse an absent platform-private value.
                SecureStorage.Default.Remove("analysis.private.v1." + scope); var refused = false;
                try { await storage.ReadAsync(scope, CancellationToken.None); } catch (IOException) { refused = true; }
                Require(refused);
                var report = JsonSerializer.Serialize(new AnalysisPrivateReport(true, true, 65536, true, true), AnalysisPrivateJson.Default.AnalysisPrivateReport);
                await File.WriteAllTextAsync(Path.Combine(root, "complete.json"), report); Android.Util.Log.Info("ZananceAnalysisPrivate", report);
            }
        }
        catch (Exception error)
        {
            // Content-free failure only; no private value, native key or parser content enters diagnostics.
            if (root is not null) await File.WriteAllTextAsync(Path.Combine(root, "error.txt"), error.GetType().Name);
            Android.Util.Log.Error("ZananceAnalysisPrivate", error.GetType().Name);
        }
    }
    private static void Require(bool value) { if (!value) throw new IOException("Original independent private stage refused."); }
}
internal sealed record AnalysisPrivateReport(bool ActualAdapter, bool ColdRead, int Bytes, bool IndependentRelease, bool MissingPrivateValueRefused);
[JsonSerializable(typeof(AnalysisPrivateReport))]
internal sealed partial class AnalysisPrivateJson : JsonSerializerContext;
#endif
