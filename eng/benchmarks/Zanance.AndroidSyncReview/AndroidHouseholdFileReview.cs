using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Cryptography;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Sharing;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.App;

/// <summary>Original native document adapters over a fictional encrypted kit in a separate protected AOT installation; no room/device grant or identity is fabricated.</summary>
internal static class AndroidHouseholdFileReview
{
    internal static async Task RunAsync(App app, IServiceProvider services, string run, bool resume)
    {
        const string package = "pro.vafadar.zanance.androidhouseholdfiles";
        var phase = "owned-installation"; var saved = false; var picked = false; var cancelled = false;
        using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(8)); var ct = deadline.Token;
        void Check(bool value) { if (!value) throw new InvalidDataException("The native recovery-file invariant failed."); }
        void Emit(string stage, string? error = null) => Android.Util.Log.Info("ZananceHouseholdFiles", JsonSerializer.Serialize(
            new HouseholdFileReviewReport(run, stage, saved, picked, cancelled, error), HouseholdFileReviewJsonContext.Default.HouseholdFileReviewReport));
        try
        {
            Check(AppInfo.Current.PackageName == package && !resume);
            var path = new Java.IO.File(FileSystem.AppDataDirectory).CanonicalPath; Check(path.Contains("/" + package + "/", StringComparison.Ordinal));
            var root = Path.Combine(path, "household-file-review", run);
            Check(!Directory.Exists(root)); Directory.CreateDirectory(root); await File.WriteAllTextAsync(Path.Combine(root, "started"), "Fictitious encrypted household-file review v1", ct);
            app.ShowSyncReview(new ContentPage { Content = new Label { Text = "Independent encrypted recovery-file review" } });
            var store = services.GetRequiredService<ZananceStore>();
            Check((await store.GetAccountsAsync(cancellationToken: ct)).Count == 0 && (await store.GetEntriesAsync(cancellationToken: ct)).Count == 0);
            var location = services.GetRequiredService<Vafadar.Data.LocalDatabaseLocation<ZananceDbContext>>(); Check(location.RequiresProtection && location.IsProtected);
            var files = services.GetRequiredService<IAppInteraction>(); Check(files.HouseholdRecoveryFilesAvailable);
            using var owner = SigningRecipientIdentity.Create();
            using var kit = HouseholdRecoveryKit.Create(Guid.NewGuid(), Guid.NewGuid(), "fictitious-file-owner", new LocalSyncPeer("https://127.0.0.1:55475", new('a', 64)),
                new(owner.Public.SigningKey, owner.Public.WrappingKey));
            var password = "Fictitious!" + run + "aA9"; var encrypted = kit.ExportEncrypted(password);
            try
            {
                phase = "save-dialog"; Emit(phase);
                saved = await files.SaveHouseholdRecoveryAsync("zanance-household-recovery-" + run + ".zhr", encrypted, ct); Check(saved);
                phase = "open-dialog"; Emit(phase);
                var selected = await files.PickHouseholdRecoveryAsync("Choose the same fictitious recovery file", ct); Check(selected is not null);
                try
                {
                    Check(CryptographicOperations.FixedTimeEquals(SHA256.HashData(encrypted), SHA256.HashData(selected!)));
                    using var restored = HouseholdRecoveryKit.ImportEncrypted(selected!, password);
                    Check(restored.Public == kit.Public && restored.EnrollmentId == kit.EnrollmentId); picked = true;
                }
                finally { if (selected is not null) CryptographicOperations.ZeroMemory(selected); }
                phase = "cancel-dialog"; Emit(phase);
                cancelled = !await files.SaveHouseholdRecoveryAsync("zanance-household-recovery-cancel-" + run + ".zhr", encrypted, ct); Check(cancelled);
                Check((await store.GetAccountsAsync(cancellationToken: ct)).Count == 0 && (await store.GetEntriesAsync(cancellationToken: ct)).Count == 0);
                phase = "complete"; Emit(phase); await File.WriteAllTextAsync(Path.Combine(root, "complete"), "Original native Save/Pick/Cancel, exact encrypted bytes and unchanged empty finance", ct);
            }
            finally { CryptographicOperations.ZeroMemory(encrypted); password = ""; }
        }
        catch (Exception error) { Emit(phase, error.GetType().Name); }
    }
}

/// <summary>Content-free runtime evidence only; no file bytes, passwords, keys or credentials enter logcat.</summary>
internal sealed record HouseholdFileReviewReport(string Run, string Phase, bool Saved, bool Picked, bool Cancelled, string? Error);

/// <summary>Reflection-free actual AOT report serialization.</summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(HouseholdFileReviewReport))]
internal partial class HouseholdFileReviewJsonContext : JsonSerializerContext;
