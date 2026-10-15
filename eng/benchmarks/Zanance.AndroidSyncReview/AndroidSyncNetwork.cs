using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Zanance.App.Features.Sync;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.App;

/// <summary>Original protected Android factory, SecureStorage and pinned HTTP against an isolated original service.</summary>
internal static class AndroidSyncNetwork
{
    /// <summary>Runs only an explicit fresh receive/upload or the retained cold-process resolution/deletion stage.</summary>
    internal static async Task RunAsync(App app, IServiceProvider services, string run, bool resume, int port, string? certificate, string? attachment, bool continuation)
    {
        var checkpoint = "identity";
        using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(8)); var ct = deadline.Token;
        void Check(bool value) { if (!value) throw new InvalidDataException("The current independent native invariant failed."); }
        var result = new NetworkReport { Run = run, Resume = resume };
        try
        {
            checkpoint = "compiled-package"; Check(AppInfo.Current.PackageName == "pro.vafadar.zanance.androidsyncnetwork");
            var canonical = new Java.IO.File(FileSystem.AppDataDirectory).CanonicalPath;
            checkpoint = "canonical-installation";
            Check(canonical.Contains("/pro.vafadar.zanance.androidsyncnetwork/", StringComparison.Ordinal));
            checkpoint = "public-configuration";
            Check(port is >= 1024 and <= 65535 && certificate?.Length == 64 && attachment?.Length == 64);
            var root = Path.Combine(canonical, "network-proof", run);
            checkpoint = "original-fixture-marker";
            Check(Directory.Exists(root) == (resume || continuation));
            if (!resume && !continuation) { Directory.CreateDirectory(root); await File.WriteAllTextAsync(Path.Combine(root, "marker"), "Fictitious native HTTP sync v1", ct); }
            Check(await File.ReadAllTextAsync(Path.Combine(root, "marker"), ct) == "Fictitious native HTTP sync v1");
            Check(!continuation || !File.Exists(Path.Combine(root, "first-complete")));
            // Keep the original foreground scheduler out of this explicitly driven engine qualification.
            // Production scheduling remains unchanged; this independent root is not an AppShell destination.
            app.ShowSyncReview(new ContentPage { Content = new Label { Text = "Independent fictional network qualification" } });
            var location = services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
            checkpoint = "original-protected-publication";
            Check(location.IsProtected && location.RequiresProtection);
            checkpoint = "canonical-financial-file";
            // Android /data/user/0 and /data/data can name the same owned installation. Compare both canonical paths.
            var canonicalFile = new Java.IO.File(location.Path).CanonicalPath;
            result.PathAliasNormalized = location.Path != canonicalFile;
            Check(canonicalFile.StartsWith(canonical + "/", StringComparison.Ordinal));
            checkpoint = "original-protected-header";
            using (var file = File.OpenRead(location.Path))
            { var header = new byte[16]; file.ReadExactly(header); Check(!header.AsSpan().SequenceEqual("SQLite format 3\0"u8)); }
            if (!resume && !File.Exists(Path.Combine(root, "generation"))) await File.WriteAllTextAsync(Path.Combine(root, "generation"), location.Path, ct);
            else Check(await File.ReadAllTextAsync(Path.Combine(root, "generation"), ct) == location.Path);
            var store = services.GetRequiredService<ZananceStore>(); var journal = services.GetRequiredService<PersonalSyncStore>();
            var coordinator = services.GetRequiredService<PersonalSyncCoordinator>();
            var peer = new LocalSyncPeer("https://10.0.2.2:" + port.ToString(System.Globalization.CultureInfo.InvariantCulture) + "/", certificate!);
            using var transport = new PersonalSyncTransport(peer, services.GetRequiredService<IPersonalSyncTokenStore>());
            if (!resume)
            {
                checkpoint = "original-native-sign-in";
                Check((await store.GetAccountsAsync(cancellationToken: ct)).Count == 0);
                // Synthetic account credentials derive from public fixture identity, never owner data or a remembered password.
                var retained = await journal.ReviewAsync(ct);
                var identity = retained.RemoteProfile is null
                    ? await transport.SignInAsync("android-network-" + run + "@example.invalid", "Fictitious!" + run + "aA9", ct)
                    : await transport.IdentityAsync(ct);
                var device = retained.RemoteProfile is null ? await transport.RegisterDeviceAsync("Fictitious Android protected client", ct) : await transport.CurrentDeviceAsync(ct);
                var remote = (await transport.ProfilesAsync(device.Id, ct)).Single(value => value.Name == "Fictitious Android HTTP copy");
                if (retained.RemoteProfile is not null)
                {
                    // Continue only the observed caller's refused initial Settings proposal. This is not a new financial fixture.
                    checkpoint = "retained-first-run-destination-review";
                    Check(continuation);
                    var conflicts = await journal.ConflictsAsync(ct);
                    result.RetainedConflictTables = string.Join(',', conflicts.Select(value => value.Conflict.Proposal.Record.Table).Distinct(StringComparer.Ordinal));
                    Check(conflicts.Count != 0 && conflicts.All(value => value.Phase == 1 && value.Conflict.Proposal.Record.Table == "Settings"));
                    Check(await coordinator.RestartReviewAsync(transport, ct));
                    result.RefusedInitialSettingsRoundContinued = true;
                }
                // The original onboarding flow explicitly treats empty destination preferences as receive-only.
                await journal.ConnectAsync(await journal.ReviewAsync(ct), peer, identity.Id, device.Id, remote, ct, receiveFirstRun: true);
                checkpoint = "original-native-receive";
                var received = await coordinator.SynchronizeAsync(transport, ct);
                result.ReceiveApplied = received.Applied; result.ReceiveNeedsReview = received.NeedsReview;
                Check(received.Applied);
                checkpoint = "original-native-received-amount";
                var entry = (await store.GetEntriesAsync(cancellationToken: ct)).Single(); Check(entry.Amount == 1234);
                checkpoint = "original-native-received-attachment";
                var file = (await store.GetAttachmentsAsync(entry.Id, ct)).Single();
                var content = (await store.GetAttachmentAsync(file.Id, ct))!;
                Check(content.Data.Length == 3 * 1024 * 1024 && Convert.ToHexStringLower(SHA256.HashData(content.Data)) == attachment);
                CryptographicOperations.ZeroMemory(content.Data);
                checkpoint = "original-native-upload";
                entry.Amount = 1400; Check((await store.SaveEntryAsync(entry, ct)).Succeeded);
                Check((await coordinator.SynchronizeAsync(transport, ct)).Applied);
                // Preserve a later unsent edit for an actual competing remote version after this process stops.
                entry.Amount = 1500; Check((await store.SaveEntryAsync(entry, ct)).Succeeded);
                await File.WriteAllTextAsync(Path.Combine(root, "first-complete"), run, ct);
                result.AttachmentBytes = true; result.PublishedOriginalEdit = true; result.UnsentLaterEdit = true;
            }
            else
            {
                checkpoint = "original-native-cold-session";
                Check(await File.ReadAllTextAsync(Path.Combine(root, "first-complete"), ct) == run);
                _ = await transport.IdentityAsync(ct); // Original platform SecureStorage, without another sign-in.
                var entry = (await store.GetEntriesAsync(cancellationToken: ct)).Single(); Check(entry.Amount == 1500);
                checkpoint = "original-native-competing-money";
                Check((await coordinator.SynchronizeAsync(transport, ct)).NeedsReview);
                Check((await store.GetEntriesAsync(cancellationToken: ct)).Single().Amount == 1500);
                var review = (await journal.ConflictsAsync(ct)).Single();
                Check(review.Conflict.Proposal.Record.Table == "Entries" && review.Conflict.Fields.Contains("Amount"));
                await journal.ResolveAsync(review, false, ct);
                Check((await coordinator.SynchronizeAsync(transport, ct)).Applied);
                entry = (await store.GetEntriesAsync(cancellationToken: ct)).Single(); Check(entry.Amount == 1300);
                var file = (await store.GetAttachmentsAsync(entry.Id, ct)).Single(); var content = (await store.GetAttachmentAsync(file.Id, ct))!;
                Check(Convert.ToHexStringLower(SHA256.HashData(content.Data)) == attachment); CryptographicOperations.ZeroMemory(content.Data);
                checkpoint = "original-native-delete";
                await store.DeleteAttachmentAsync(file.Id, ct); await store.DeleteEntryAsync(entry.Id, ct);
                Check((await coordinator.SynchronizeAsync(transport, ct)).Applied);
                Check((await store.GetEntriesAsync(cancellationToken: ct)).Count == 0 && await store.GetAttachmentAsync(file.Id, ct) is null);
                result.ColdSession = true; result.ExactConflictResolution = true; result.PublishedOriginalDeletion = true; result.AttachmentBytes = true;
            }
            result.ProtectedGeneration = true;
            result.Success = true; result.Checkpoint = resume ? "native-cold-complete" : "native-first-complete";
        }
        catch (Exception error)
        { result.Success = false; result.Checkpoint = checkpoint; result.ErrorType = error.GetType().Name; }
        // Only fixed content-free status enters logcat; never exception text, addresses, sessions or payloads.
        var json = JsonSerializer.Serialize(result, NetworkReportJson.Default.NetworkReport);
        var bytes = System.Text.Encoding.UTF8.GetBytes(json); var count = (bytes.Length + 1535) / 1536;
        for (var index = 0; index < count; index++) Android.Util.Log.Info("ZananceSyncReview", run + "|" + index + "/" + count + "|" +
            Convert.ToBase64String(bytes.AsSpan(index * 1536, Math.Min(1536, bytes.Length - index * 1536))));
    }
}

/// <summary>Content-free native network qualification result.</summary>
internal sealed class NetworkReport
{
    /// <summary>Public isolated fixture identity, never an authentication credential.</summary>
    public string Run { get; set; } = "";
    /// <summary>Whether this is the explicitly requested second OS process.</summary>
    public bool Resume { get; set; }
    /// <summary>Whether every invariant of the requested stage completed.</summary>
    public bool Success { get; set; }
    /// <summary>Fixed checkpoint name, without financial or credential values.</summary>
    public string Checkpoint { get; set; } = "";
    /// <summary>Exception type only; messages and private request details are excluded.</summary>
    public string? ErrorType { get; set; }
    /// <summary>Original keyed publication and non-plaintext header were verified.</summary>
    public bool ProtectedGeneration { get; set; }
    /// <summary>Android's raw path differed from its canonical installation alias.</summary>
    public bool PathAliasNormalized { get; set; }
    /// <summary>Original initial receive completed its financial transaction.</summary>
    public bool ReceiveApplied { get; set; }
    /// <summary>Original initial receive still requires explicit review.</summary>
    public bool ReceiveNeedsReview { get; set; }
    /// <summary>Fixed schema table names observed before continuing the refused caller round.</summary>
    public string RetainedConflictTables { get; set; } = "";
    /// <summary>Only the known empty-destination Settings refusal was explicitly continued.</summary>
    public bool RefusedInitialSettingsRoundContinued { get; set; }
    /// <summary>Complete original attachment bytes match the fictional counterpart digest.</summary>
    public bool AttachmentBytes { get; set; }
    /// <summary>An original local financial edit was uploaded through actual HTTPS.</summary>
    public bool PublishedOriginalEdit { get; set; }
    /// <summary>A later original edit remains unsent for a real competing remote version.</summary>
    public bool UnsentLaterEdit { get; set; }
    /// <summary>Original platform SecureStorage authenticated after a process restart without sign-in.</summary>
    public bool ColdSession { get; set; }
    /// <summary>Actual competing amounts waited for the explicit original whole-row choice.</summary>
    public bool ExactConflictResolution { get; set; }
    /// <summary>Original deletion operations were published through the service.</summary>
    public bool PublishedOriginalDeletion { get; set; }
}

/// <summary>Trim-safe fixed report serialization; no reflection over financial or session data.</summary>
[JsonSerializable(typeof(NetworkReport))]
internal partial class NetworkReportJson : JsonSerializerContext;
