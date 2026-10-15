#if DEBUG && WINDOWS
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization;
using Vafadar.Zanance.App.Features.Sync;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Sync;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Checks original first-run and version comparison controls only in an independent protected installation.</summary>
    private static async Task ReviewPersonalSyncAsync(App app, IServiceProvider services, string folder)
    {
        var publicReview = Environment.GetEnvironmentVariable("VAFADAR_SYNC_PUBLIC_REVIEW") == "1";
        var package = publicReview ? "pro.vafadar.zanance.syncpublicreview" : "pro.vafadar.zanance.syncreview";
        if (AppInfo.Current.PackageName != package) throw new IOException("Independent sync installation required.");
        var root = Path.GetFullPath(FileSystem.AppDataDirectory);
        if (!root.Contains(package, StringComparison.OrdinalIgnoreCase))
            throw new IOException("Independent native data root required.");
        var marker = Path.Combine(root, "fictitious-sync-window.marker");
        if (File.Exists(marker)) throw new IOException("This route requires a fresh marked installation; never reset an existing review.");
        await File.WriteAllTextAsync(marker, "Fictitious original personal sync window review v1");
        var finances = services.GetRequiredService<ZananceStore>();
        if ((await finances.GetAccountsAsync()).Count != 0) throw new IOException("Only a fresh independent installation can be seeded.");
        var local = services.GetRequiredService<ILocalizationService>();
        var themes = services.GetRequiredService<ThemeService>();
        var observations = new List<object>(); var results = new List<object>();
        var sync = services.GetRequiredService<PersonalSyncStore>();
        foreach (var comparison in publicReview ? new[] { false } : new[] { false, true })
        {
            if (comparison)
            {
                var account = new Account { Name = "Fictitious window KWD account", CurrencyCode = "KWD", OpeningDate = new(2026, 10, 15) };
                await finances.SaveAccountAsync(account);
                await sync.ConnectAsync(await sync.ReviewAsync(), new("https://127.0.0.1:5048/", new('1', 64)),
                    "fictitious-window-owner", Guid.NewGuid(), new(Guid.NewGuid(), "Fictitious window remote copy", SyncSchema.Hash, 0, false));
                var entry = new LedgerEntry { AccountId = account.Id, Kind = EntryKind.Expense, Amount = 1400,
                    Date = new(2026, 10, 15), Title = "Fictitious window contested expense" };
                if (!(await finances.SaveEntryAsync(entry)).Succeeded) throw new IOException("Original fictional financial write required.");
                await SeedSyncWindowReviewAsync(services, entry.Id, account.Id, CancellationToken.None);
            }
            var before = await ProtectedRowsFingerprintAsync(services);
            foreach (var language in new[] { "en", "fa", "de" })
            foreach (var theme in new[] { ThemeChoice.Light, ThemeChoice.Dark })
            foreach (var width in new[] { 360, 412, 1280 })
            foreach (var hosted in publicReview ? new[] { false, true } : new[] { false })
            {
                themes.Set(theme); await SetLanguageAsync(app, local, language);
                app.Windows[0].Width = width; app.Windows[0].Height = 820;
                var vm = services.GetRequiredService<PersonalSyncViewModel>();
                if (publicReview && !hosted) vm.Address = "https://127.0.0.1:5048/";
                if (vm.IsReady || vm.CanInteract) throw new IOException("Sync construction must remain covered.");
                var page = new PersonalSyncPage(vm) { FlowDirection = local.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
                var back = 0;
                if (!comparison) page.BeginOnboarding(() => { back++; return Task.CompletedTask; }, () => throw new IOException("No received account can finish this first-run fixture."));
                app.ShowPersonalSyncReview(page);
                for (var frame = 0; frame < 100 && !vm.IsReady && vm.Error is null; frame++) await Task.Delay(100);
                if (!vm.IsReady || vm.Error is not null || vm.ReviewItems.Count != (comparison ? 1 : 0))
                    throw new IOException("Original sync local snapshot did not publish.");
                if (publicReview && hosted && (vm.Address != "https://api.zanance.vafadar.pro/" || vm.Fingerprint.Length != 0))
                    throw new IOException("Native default public connection must retain normal system trust.");
                await Task.Delay(250);
                var scroll = ((Grid)page.Content!).Children.OfType<ScrollView>().Single();
                foreach (var label in VisualDescendants(page).OfType<Label>().Where(label => Visible(label) && label.FontAutoScalingEnabled && !string.IsNullOrEmpty(label.Text)))
                    CheckCaption(label, observations);
                if (comparison)
                {
                    var selected = vm.ReviewItems.Single();
                    foreach (var version in selected.Versions)
                    foreach (var field in version.Fields.Where(field => field.Value.Length != 0))
                        if (!VisualDescendants(page).OfType<Label>().Any(label => label.Text == field.Value && Visible(label)))
                            throw new IOException("Complete original comparison value is missing from its native row.");
                    var surface = VisualDescendants(page).OfType<WrappingAction>().Single(item => ReferenceEquals(item.Command, vm.KeepRemoteCommand));
                    await scroll.ScrollToAsync(surface, ScrollToPosition.End, false); await Task.Delay(150);
                    var button = VisualDescendants(surface).OfType<Button>().Single();
                    if (!ReferenceEquals(button.CommandParameter, selected.Review) || button.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button native
                        || native.ActualWidth < 44 || native.ActualHeight < 44 ||
                        Microsoft.UI.Xaml.Automation.AutomationProperties.GetName(native) != selected.RemoteAction)
                        throw new IOException("Original comparison action lost its native target, name or reviewed parameter.");
                }
                else
                {
                    var button = VisualDescendants(page).OfType<Button>().Single(item => ReferenceEquals(item.Command, vm.CloseOnboardingCommand));
                    InvokeChoiceReview(button, observations);
                    if (back != 1 || vm.CanFinishOnboarding) throw new IOException("Native first-run Back or unreceived finish gate failed.");
                }
                await CaptureWindowAsync(app.Windows[0], Path.Combine(folder, $"{language}-{theme}-{width}-{(comparison ? "comparison" : "first-run")}{(publicReview ? hosted ? "-hosted" : "-local" : "")}.png"));
                if (publicReview && hosted)
                    await CaptureHelpAsync(app, folder, $"{language}-{theme}-{width}", ["PersonalSync"]);
                vm.Deactivate();
                results.Add(new { Language = language, Theme = theme.ToString(), Width = width, Comparison = comparison, HostedAddress = hosted, OriginalPage = true });
                await File.WriteAllTextAsync(Path.Combine(folder, "personal-sync-progress.json"), JsonSerializer.Serialize(results));
            }
            if (before != await ProtectedRowsFingerprintAsync(services)) throw new IOException("Native sync layout review changed financial data.");
        }
        await File.WriteAllTextAsync(Path.Combine(folder, "personal-sync-proof.json"), JsonSerializer.Serialize(new
        { Cases = results, RetainedNativeGeometryObservations = observations.Count, FinancialRowsUnchanged = true, IndependentProtectedInstallation = true,
            NoNetworkOrCredentialEntry = true, DeviceOrScreenReaderAcceptance = false }));
    }

    private static async Task SeedSyncWindowReviewAsync(IServiceProvider services, Guid entryId, Guid accountId, CancellationToken ct)
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
            await using var reader = await command.ExecuteReaderAsync(ct); if (!await reader.ReadAsync(ct)) throw new IOException("Original capture required.");
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

}
#endif
