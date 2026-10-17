using System.Text.Json;
using System.Text.Json.Nodes;
using Android.Views;
using Android.Runtime;
using NativeView = Android.Views.View;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Sync;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Sync;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.App;

/// <summary>Renders the actual page/flow/native adapters over protected fictional data, without service credentials or HTTP.</summary>
internal static partial class AndroidSyncReview
{
#if MARKET_PRIVATE_REVIEW
    internal const string Package = "pro.vafadar.zanance.marketprivatereview";
#elif AI_PRIVATE_REVIEW
    internal const string Package = "pro.vafadar.zanance.analysisprivatereview";
#elif BILLING_RECOVERY_REVIEW
    internal const string Package = "pro.vafadar.zanance.billingrecoveryreview";
#elif HOUSEHOLD_SNAPSHOT_REVIEW
    internal const string Package = "pro.vafadar.zanance.householdsnapshotreview";
#elif HOUSEHOLD_FILE_REVIEW
    internal const string Package = "pro.vafadar.zanance.androidhouseholdfiles";
#elif SYNC_PUBLIC_REVIEW
    internal const string Package = "pro.vafadar.zanance.androidsyncpublicreview";
#elif SYNC_NETWORK_REVIEW
    internal const string Package = "pro.vafadar.zanance.androidsyncnetwork";
    private static readonly bool LinkedGroup = false;
#elif SYNC_ATTACHMENT_REVIEW
    internal const string Package = "pro.vafadar.zanance.androidsyncattachmentreview";
    private static readonly bool LinkedGroup = false;
#elif SYNC_PORTABLE_REVIEW
    internal const string Package = "pro.vafadar.zanance.androidsyncportablereview";
    private static readonly bool LinkedGroup = false;
#elif SYNC_GROUP_REVIEW
    internal const string Package = "pro.vafadar.zanance.androidsyncgroupreview";
    private static readonly bool LinkedGroup = true;
#else
    internal const string Package = "pro.vafadar.zanance.androidsyncreview";
    private static readonly bool LinkedGroup = false;
#endif
#if !SYNC_PUBLIC_REVIEW && !HOUSEHOLD_FILE_REVIEW && !BILLING_RECOVERY_REVIEW && !AI_PRIVATE_REVIEW && !MARKET_PRIVATE_REVIEW
#if SYNC_ATTACHMENT_REVIEW
    private static readonly bool AttachmentReview = true;
#else
    private static readonly bool AttachmentReview = false;
#endif
#if SYNC_PORTABLE_REVIEW
    private static readonly bool PortableRecords = true;
#else
    private static readonly bool PortableRecords = false;
#endif
#endif
    private const string Tag = "ZananceSyncReview";

    internal static async Task RunAsync(App app, IServiceProvider services, string run, bool resume, string? fromLanguage = null)
    {
#if MARKET_PRIVATE_REVIEW
        await AndroidMarketPrivateReview.RunAsync(run, resume);
#elif AI_PRIVATE_REVIEW
        await AndroidAnalysisPrivateReview.RunAsync(run, resume);
#elif BILLING_RECOVERY_REVIEW
        await AndroidBillingRecoveryReview.RunAsync(app, services, run, resume);
#elif HOUSEHOLD_SNAPSHOT_REVIEW
        await AndroidHouseholdSnapshotReview.RunAsync(app, run);
#elif HOUSEHOLD_FILE_REVIEW
        await AndroidHouseholdFileReview.RunAsync(app, services, run, resume);
#elif SYNC_PUBLIC_REVIEW
        await RunPublicServiceAsync(app, services, run, resume);
#else
        var checkpoint = "identity";
        using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(4)); var ct = deadline.Token;
        try
        {
            Check(AppInfo.Current.PackageName == Package, "Independent identity required.");
            var path = new Java.IO.File(FileSystem.AppDataDirectory).CanonicalPath;
            Check(path.Contains("/" + Package + "/", StringComparison.Ordinal), "Independent native data path required.");
            var root = Path.Combine(path, "sync-review", run); Directory.CreateDirectory(root);
            Check(File.Exists(Path.Combine(root, "started")) == resume, "Explicit continuation or fresh fixture required.");
            if (!resume) await File.WriteAllTextAsync(Path.Combine(root, "started"), "Fictitious original-page review", ct);
            var finances = services.GetRequiredService<ZananceStore>();
            var sync = services.GetRequiredService<PersonalSyncStore>();
            LedgerEntry entry;
            checkpoint = "fictional-journal";
            if (!resume)
            {
            var account = new Account { Name = "Fictitious native KWD account", CurrencyCode = "KWD", OpeningDate = new(2026, 10, 15) };
            await finances.SaveAccountAsync(account, ct);
            await sync.ConnectAsync(await sync.ReviewAsync(ct), new("https://127.0.0.1:5048/", new('1', 64)),
                "fictitious-native-owner", Guid.NewGuid(), new(Guid.NewGuid(), "Fictitious native remote copy", SyncSchema.Hash, 0, false), ct);
            entry = new LedgerEntry { AccountId = account.Id, Kind = EntryKind.Expense, Amount = 1400, Date = new(2026, 10, 15),
                Title = "Fictitious native contested expense" };
            Check((await finances.SaveEntryAsync(entry, ct)).Succeeded, "Original financial write required.");
            if (AttachmentReview) await SeedAttachmentReviewAsync(services, entry, account, ct);
            else if (PortableRecords) await SeedPortableReviewAsync(services, account, ct);
            else if (LinkedGroup) await SeedGroupReviewAsync(services, entry, account, ct);
            else await SeedReviewAsync(services, entry.Id, account.Id, ct);
            }
            else entry = (await finances.GetEntriesAsync(cancellationToken: ct)).Single(row => row.Title == "Fictitious native contested expense");
            var local = services.GetRequiredService<ILocalizationService>();
            var translator = services.GetRequiredService<Translator>();
            var results = new List<object>();
            var languages = new[] { "en", "fa", "de" };
            var first = fromLanguage is null ? 0 : Array.IndexOf(languages, fromLanguage);
            Check(first >= 0 && (fromLanguage is null || resume), "An explicit retained language continuation is required.");
            foreach (var language in languages.Skip(first))
            foreach (var theme in new[] { AppTheme.Light, AppTheme.Dark })
            {
                local.SetLanguage(local.SupportedLanguages.Single(item => item.CultureName == language));
                app.UserAppTheme = theme;
                await Task.Delay(300, ct);
                var vm = services.GetRequiredService<PersonalSyncViewModel>();
                Check(!vm.IsReady && !vm.CanInteract, "Construction must remain covered.");
                var page = new PersonalSyncPage(vm) { FlowDirection = local.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
                app.ShowSyncReview(page);
                for (var frame = 0; frame < 120 && !vm.IsReady; frame++) await Task.Delay(100, ct);
                checkpoint = "native-publication";
                Check(vm.IsReady && vm.Error is null && vm.ReviewItems.Count == (PortableRecords ? 7 : 1), "Original native local read did not publish.");
                if (PortableRecords) Check(vm.ReviewItems.All(item => item.CanChooseLocal && item.CanChooseRemote), "All original portable records must be reviewable.");
                var selected = PortableRecords ? vm.ReviewItems.Single(item => item.Review.Conflict.Proposal.Record.Table == "Templates") : vm.ReviewItems[0];
                if (AttachmentReview) await CheckAttachmentPreviewAsync(page, vm, ct);
                await Task.Delay(600, ct);
                var native = (NativeView)page.Handler!.PlatformView!;
                var nodes = NativeViews(native).OfType<Android.Widget.TextView>().ToArray();
                var text = nodes.Select(node => node.Text ?? "").ToArray();
                if (PortableRecords)
                {
                    checkpoint = "native-complete-portable-values";
                    foreach (var packet in vm.ReviewItems.SelectMany(item => item.Versions.SelectMany(version => version.Fields)
                        .Select(field => (Table: item.Review.Conflict.Proposal.Record.Table, Value: field.Value)))
                        .Where(packet => packet.Value.Length != 0).DistinctBy(packet => packet.Value, StringComparer.Ordinal))
                    {
                        checkpoint = "native-portable-" + packet.Table + "-value";
                        // The original native label converts display digits even for ordinary stored text.
                        // Compare its actual presentation, without changing stored values or the device locale.
                        var value = NativeDigits.Apply(packet.Value)!;
                        var rendered = nodes.Where(node => node.Text == value).ToArray();
                        Check(rendered.Length != 0, "Every original portable comparison value must reach a native text view.");
                        checkpoint = "native-portable-" + packet.Table + "-explicit-lines";
                        if (value.Contains('\n')) Check(rendered.Any(node => node.LineCount >= value.Split('\n').Length),
                            "Every explicit forecast/settings line must remain visible in a growing native text view.");
                    }
                }
                checkpoint = "native-headings";
                Check(text.Any(value => value.Contains(translator["Sync_ReviewSubmitted"], StringComparison.Ordinal)) &&
                    text.Any(value => value.Contains(translator["Sync_Remote"], StringComparison.Ordinal)), "Original native version headings missing.");
                if (LinkedGroup)
                    Check(vm.ReviewItems[0].Versions.Count == 6 && vm.ReviewItems[0].Review.GroupMembers?.Count == 2 &&
                        vm.ReviewItems[0].CanChooseLocal && vm.ReviewItems[0].CanChooseRemote && text.Contains(translator["Sync_LinkedGroup"]),
                        "Complete original linked-group review required.");
                var values = selected.Versions.SelectMany(version => version.Fields).Where(field => field.Caption == translator[AttachmentReview ? "Sync_AttachmentContent" : "Entry_Amount"]).Select(field => field.Value).ToArray();
                checkpoint = "native-money";
                Check(values.Length == (LinkedGroup ? 4 : 2) && values.All(text.Contains), "Original native money packets missing.");
                checkpoint = "find-original-scroll";
                // Android may defer native button realization below the current viewport.
                // Scroll the actual original page to its actions before measuring their targets/names.
                var outer = ((Grid)page.Content!).Children.OfType<ScrollView>().Single();
                checkpoint = "scroll-original-actions";
                await outer.ScrollToAsync(0, outer.ContentSize.Height, false);
                await Task.Delay(300, ct);
                checkpoint = "find-original-command-owner";
                var surface = page.GetVisualTreeDescendants().OfType<WrappingAction>().Single(item => ReferenceEquals(item.Command, vm.KeepRemoteCommand) && ReferenceEquals(item.CommandParameter, selected.Review));
                var commandButton = ((Grid)surface.Content!).Children.OfType<Button>().Single();
                Check(ReferenceEquals(commandButton.CommandParameter, selected.Review), "Native action must retain the original reviewed command parameter.");
                checkpoint = "original-command-native-handler";
                var action = commandButton.Handler?.PlatformView as NativeView;
                using var buttonClass = Java.Lang.Class.FromType(typeof(Android.Widget.Button));
                Check(action is not null && buttonClass.IsInstance(action), "Original action must own a real Android button.");
                Check(commandButton.IsEnabled && action!.Enabled, "Original native review action must be enabled while the page is active.");
                checkpoint = "original-command-native-description";
                // MAUI supplies semantic names through its accessibility delegate, not View.ContentDescription.
                // Inspect the same actual accessibility node that UI automation and screen readers consume.
                using var accessible = action!.CreateAccessibilityNodeInfo();
                Check(accessible!.ContentDescription == selected.RemoteAction, "Original native action must expose its complete spoken name.");
                Check(native.Width > 0 && native.Height > 0, "Native page did not lay out.");
                Check((Platform.CurrentActivity!.Window!.Attributes!.Flags & WindowManagerFlags.Secure) != 0, "Secure screenshot flag must stay enabled.");
                Check((await finances.GetEntriesAsync(cancellationToken: ct)).Single(row => row.Id == entry.Id).Amount == 1400, "Rendering must not change money.");
                results.Add(new { Language = language, Theme = theme.ToString(), NativeWidth = native.Width, NativeHeight = native.Height,
                    Density = native.Resources!.DisplayMetrics!.Density, NativeTexts = nodes.Length, CoveredConstruction = true,
                    OriginalMoneyPackets = !AttachmentReview, AttachmentMetadataValues = AttachmentReview,
                    CompleteLinkedGroup = LinkedGroup, PortableRecordTypes = PortableRecords ? 7 : 0,
                    PortableFieldsRendered = PortableRecords, NativeAttachmentPreview = AttachmentReview, NativeChoiceButton = true, FinancialValueUnchanged = true, SecureFlag = true });
                // Keep the last original page active for an actual native confirmation/cancellation check.
                if (language != "de" || theme != AppTheme.Dark) vm.Deactivate();
            }
            var report = new { Passed = true, Fictitious = true, MainPackageTouched = false, Cases = results };
            var json = JsonSerializer.Serialize(report); await File.WriteAllTextAsync(Path.Combine(root, "result.json"), json, ct); Emit(run, json);
        }
        catch (Exception error)
        {
            // Only type/stack are public; no credentials, keys, financial payloads or platform error messages.
            Emit(run, JsonSerializer.Serialize(new { Passed = false, Checkpoint = checkpoint, ErrorType = error.GetType().FullName, Stack = error.StackTrace }));
        }
#endif
    }

    private static async Task SeedReviewAsync(IServiceProvider services, Guid entryId, Guid accountId, CancellationToken ct)
    {
        await using var db = await services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(ct);
        await db.Database.OpenConnectionAsync(ct);
        async Task<(SyncRecord Record, long Sequence)> Captured(string table, Guid id)
        {
            var key = JsonSerializer.Serialize(new[] { id.ToString("D").ToUpperInvariant() });
            await using var command = db.Database.GetDbConnection().CreateCommand();
            command.CommandText = "SELECT Body,Sequence FROM SyncOutbox WHERE TableName=$table AND RecordKey=$key ORDER BY Sequence DESC LIMIT 1";
            var t = command.CreateParameter(); t.ParameterName = "$table"; t.Value = table; command.Parameters.Add(t);
            var k = command.CreateParameter(); k.ParameterName = "$key"; k.Value = key; command.Parameters.Add(k);
            await using var reader = await command.ExecuteReaderAsync(ct); Check(await reader.ReadAsync(ct), "Original capture required.");
            return (new(new(table, key), 0, JsonSerializer.Deserialize<JsonElement>(reader.GetString(0))), reader.GetInt64(1));
        }
        var entry = await Captured("Entries", entryId); var account = await Captured("Accounts", accountId);
        var changed = JsonNode.Parse(entry.Record.Values!.Value.GetRawText())!.AsObject(); changed["Amount"] = 1300;
        var remote = entry.Record with { Version = 7, Values = JsonSerializer.SerializeToElement(changed) };
        var conflict = new SyncConflict(new(entry.Record.Record, 6, entry.Record.Values), null, remote, ["Amount"], "CompetingChanges",
            Context: [account.Record with { Version = 5 }]);
        var payload = JsonSerializer.Serialize(conflict, PersonalSyncJsonContext.Default.SyncConflict); var operation = Guid.NewGuid();
        // Only fictional received journal facts are seeded; original production stores create financial rows and triggers.
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO SyncClientRound(Id,OperationId,RemoteId,Cutoff,Phase,PartCount,SentParts,ContentHash,ThroughCursor,ReadCursor) VALUES(1,{operation},NULL,{entry.Sequence},1,1,1,{new string('1', 64)},NULL,0)", ct);
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO SyncConflicts(TableName,RecordKey,Payload,LocalSequence,UseProposal) VALUES({entry.Record.Record.Table},{entry.Record.Record.Key},{payload},{entry.Sequence},NULL)", ct);
    }

    private static async Task SeedGroupReviewAsync(IServiceProvider services, LedgerEntry fee, Account account, CancellationToken ct)
    {
        var holdings = services.GetRequiredService<HoldingStore>();
        var type = new AssetType { Name = "Fictitious native gold", Dimension = AssetDimension.Mass, PriceCurrencyCode = "KWD" };
        Check(await holdings.SaveTypeAsync(type, ct), "Original holding type required.");
        var location = new AssetLocation { Name = "Fictitious native safe" }; await holdings.SaveLocationAsync(location, ct);
        var purchase = new AssetEvent { AssetTypeId = type.Id, LocationId = location.Id, Kind = AssetEventKind.Purchase,
            Date = fee.Date, Quantity = 1000, BasisAmount = 3456 };
        var payment = new LedgerEntry { AccountId = account.Id, Kind = EntryKind.AssetPurchase, Amount = 3456, Date = fee.Date };
        Check((await holdings.SaveEventAsync(purchase, [payment, fee], ct)).Succeeded, "Original complete holding write required.");
        await using var db = await services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(ct);
        await db.Database.OpenConnectionAsync(ct);
        var captured = new List<(SyncRecord Record, long Sequence)>();
        await using (var query = db.Database.GetDbConnection().CreateCommand())
        {
            query.CommandText = "SELECT TableName,RecordKey,Body,Sequence FROM SyncOutbox WHERE Body IS NOT NULL ORDER BY Sequence";
            await using var rows = await query.ExecuteReaderAsync(ct);
            while (await rows.ReadAsync(ct)) captured.Add((new(new(rows.GetString(0), rows.GetString(1)), 5,
                JsonSerializer.Deserialize<JsonElement>(rows.GetString(2))), rows.GetInt64(3)));
        }
        var latest = captured.GroupBy(row => row.Record.Record).Select(group => group.Last()).ToArray();
        var groupRows = latest.Where(row => SyncConflictGrouping.Group(row.Record.Record, row.Record.Values) == purchase.GroupId).ToArray();
        Check(groupRows.Length == 3, "Whole original group capture required.");
        var remote = groupRows.Select(row =>
        {
            var body = JsonNode.Parse(row.Record.Values!.Value.GetRawText())!.AsObject();
            if (row.Record.Record.Table == "AssetEvents") body["BasisAmount"] = 4567;
            else body["Amount"] = body["Kind"]!.GetValue<int>() == (int)EntryKind.AssetPurchase ? 4567 : 1300;
            return row.Record with { Version = 7, Values = JsonSerializer.SerializeToElement(body) };
        }).ToArray();
        var context = remote.Concat(latest.Where(row => !groupRows.Any(member => member.Record.Record == row.Record.Record)).Select(row => row.Record)).ToArray();
        var operation = Guid.NewGuid(); var cutoff = latest.Max(row => row.Sequence);
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO SyncClientRound(Id,OperationId,RemoteId,Cutoff,Phase,PartCount,SentParts,ContentHash,ThroughCursor,ReadCursor) VALUES(1,{operation},NULL,{cutoff},1,1,1,{new string('1', 64)},NULL,0)", ct);
        foreach (var member in groupRows)
        {
            var conflict = new SyncConflict(new(member.Record.Record, 6, member.Record.Values), null,
                remote.Single(row => row.Record == member.Record.Record), ["GroupId"], "LinkedGroupReview", Context: context, LinkedGroup: purchase.GroupId);
            var payload = JsonSerializer.Serialize(conflict, PersonalSyncJsonContext.Default.SyncConflict);
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO SyncConflicts(TableName,RecordKey,Payload,LocalSequence,UseProposal) VALUES({member.Record.Record.Table},{member.Record.Record.Key},{payload},{member.Sequence},NULL)", ct);
        }
    }

    private static IEnumerable<NativeView> NativeViews(NativeView view)
    {
        yield return view;
        // Android's private DecorView can be surfaced as a managed View rather than ViewGroup.
        // Check Java identity and cast explicitly so traversal cannot stop at the actual window root.
        using var groupClass = Java.Lang.Class.FromType(typeof(ViewGroup));
        if (groupClass.IsInstance(view))
        {
            var group = view as ViewGroup ?? view.JavaCast<ViewGroup>();
            for (var index = 0; index < group.ChildCount; index++)
                foreach (var child in NativeViews(group.GetChildAt(index)!)) yield return child;
        }
    }
    private static void Check(bool condition, string reason) { if (!condition) throw new InvalidOperationException(reason); }
    private static void Emit(string run, string json)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(json); var count = (bytes.Length + 1535) / 1536;
        for (var index = 0; index < count; index++)
            Android.Util.Log.Info(Tag, run + "|" + index + "/" + count + "|" + Convert.ToBase64String(bytes.AsSpan(index * 1536, Math.Min(1536, bytes.Length - index * 1536))));
    }
}
