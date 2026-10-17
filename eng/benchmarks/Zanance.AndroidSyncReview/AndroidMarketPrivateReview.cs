#if MARKET_PRIVATE_REVIEW
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Vafadar.Zanance.App.Features.Rates;

namespace Vafadar.Zanance.App;

/// <summary>Two real Release processes qualify only the original private cache adapter over their independent device-owned scope.</summary>
internal static class AndroidMarketPrivateReview
{
    internal static async Task RunAsync(string run, bool resume)
    {
        string? root = null;
        try
        {
            Require(AppInfo.Current.PackageName == "pro.vafadar.zanance.marketprivatereview" && run.Length == 32 && run.All(char.IsAsciiHexDigit));
            root = Path.Combine(FileSystem.AppDataDirectory, "market-fixture", run); Directory.CreateDirectory(root);
            var marker = Path.Combine(root, "fixture.marker");
            if (!File.Exists(marker)) { Require(!resume); await File.WriteAllTextAsync(marker, "Fictitious private market d299"); }
            Require(await File.ReadAllTextAsync(marker) == "Fictitious private market d299" && !File.Exists(Path.Combine(root, "complete.json")));
            var scope = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes("fictitious-market-bound:" + run)));
            var other = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes("fictitious-market-other:" + run)));
            var storage = new SecureMarketCache(); var expected = new string('x', 65536);
            if (!resume)
            {
                var original = await storage.ReadAsync(scope, CancellationToken.None);
                if (original is null) await storage.WriteAsync(scope, expected, CancellationToken.None); else Require(original == expected);
                Require(await storage.ReadAsync(scope, CancellationToken.None) == expected && await storage.ReadAsync(other, CancellationToken.None) is null);
                var oversized = false;
                try { await storage.WriteAsync(scope, expected + "x", CancellationToken.None); } catch (IOException) { oversized = true; }
                Require(oversized && await storage.ReadAsync(scope, CancellationToken.None) == expected);
                var invalid = false;
                try { await storage.WriteAsync("../outside", "fictitious", CancellationToken.None); } catch (ArgumentException) { invalid = true; }
                Require(invalid);
                using var canceled = new CancellationTokenSource(); canceled.Cancel(); var cancellation = false;
                try { await storage.WriteAsync(scope, "replacement-refused", canceled.Token); } catch (OperationCanceledException) { cancellation = true; }
                Require(cancellation && await storage.ReadAsync(scope, CancellationToken.None) == expected);
                await File.WriteAllTextAsync(Path.Combine(root, "staged.json"), JsonSerializer.Serialize(new { Process = Environment.ProcessId,
                    ExactBound = true, OversizedPreserved = true, ScopeSeparated = true, InvalidScopeRefused = true, CanceledPreserved = true }));
                Android.Util.Log.Info("ZananceMarketReview", "private-market-staged");
            }
            else
            {
                using var staged = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(root, "staged.json")));
                Require(staged.RootElement.GetProperty("Process").GetInt32() != Environment.ProcessId);
                Require(await storage.ReadAsync(scope, CancellationToken.None) == expected && await storage.ReadAsync(other, CancellationToken.None) is null);
                await File.WriteAllTextAsync(Path.Combine(root, "complete.json"), JsonSerializer.Serialize(new { Process = Environment.ProcessId,
                    ColdOriginalRead = true, ActualSecureStorage = true, ScopeSeparated = true, RealProvider = false, MainDataTouched = false }));
                Android.Util.Log.Info("ZananceMarketReview", "private-market-complete");
            }
        }
        catch (Exception error)
        {
            if (root is not null) await File.WriteAllTextAsync(Path.Combine(root, "error.txt"), error.GetType().Name);
            Android.Util.Log.Error("ZananceMarketReview", "private-market-failed:" + error.GetType().Name);
        }
    }
    private static void Require(bool value) { if (!value) throw new IOException("Original private market invariant failed."); }
}
#endif
