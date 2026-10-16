using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Cryptography;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Core.Sharing;
using Vafadar.Zanance.Core.Sync;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Sharing;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.ProtectedSync;

/// <summary>Actual protected source selection and atomic separate household seeding, including private counterparts and a real interrupted write.</summary>
internal static class ProjectionQualification
{
    /// <summary>Only the independently marked original fixture is used; every cold phase reopens its retained device keys and files.</summary>
    internal static async Task RunAsync(string root, string mode, Action<string> proof, CancellationToken ct)
    {
        void Require(bool value) { if (!value) throw new InvalidDataException("The actual household projection invariant failed."); }
        var peer = new LocalSyncPeer("https://localhost:55555", new('a', 64)); const string identity = "fictitious-projection-owner";
        var prepare = mode == "projection-prepare";
        using var source = await ProtectedClient.OpenAsync(Path.Combine(root, "source"), peer, prepare, ct);
        using var target = await ProtectedClient.OpenAsync(Path.Combine(root, "target"), peer, prepare, ct);
        source.Services.MigrateLocalDatabase<ZananceDbContext>(); target.Services.MigrateLocalDatabase<ZananceDbContext>();
        var sourceFactory = source.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        var targetFactory = target.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        var projection = source.Services.GetRequiredService<HouseholdProjectionStore>(); var journal = new HouseholdJournal(targetFactory);
        var proofPath = Path.Combine(root, "projection-proof.json"); ProjectionProof expected; HouseholdJournalState state;
        if (mode is "projection-prepare" or "projection-prepare-resume")
        {
            if (prepare) await SeedSourceAsync();
            else Require((await source.Store.GetAccountsAsync(cancellationToken: ct)).Count == 4 && (await source.Store.GetEntriesAsync(cancellationToken: ct)).Count == 9);
            proof("original-fictitious-selected-and-private-source-retained-before-projection-review");
            var existing = await journal.CaptureAsync(peer, identity, ct);
            if (existing is not null) state = existing;
            else
            {
                using var owner = SigningRecipientIdentity.Create(); using var device = SigningRecipientIdentity.Create();
                state = await journal.BeginDraftAsync(peer, identity, owner.Public, true, device, owner, null, ct);
            }
            var selected = (await source.Store.GetAccountsAsync(cancellationToken: ct)).Where(account => account.Name.StartsWith("Fictitious selected", StringComparison.Ordinal)).OrderBy(account => account.Name).ToArray();
            Require(selected.Length == 2);
            using var review = await projection.ReviewAsync(new(selected.Select(account => account.Id).ToArray(), true, true, "Excluded private counterpart"), ct);
            proof("actual-selected-source-projection-review-created");
            Require(review.Accounts.Count == 2 && review.Entries == 8 && review.Attachments == 1 && review.PrivateTransfers == 3 && review.DetachedRefunds == 1);
            var before = await SourceDigestAsync();
            var earlyRefused = false;
            try { await journal.ApplyProjectionAsync(state.Authority, review, ct); } catch (InvalidOperationException) { earlyRefused = true; }
            Require(earlyRefused && await journal.ReadProjectionReceiptAsync(state.Authority, ct) is null);
            var changed = (await source.Store.GetEntriesAsync(cancellationToken: ct)).Single(entry => entry.Title == "Fictitious selected purchase");
            var originalAmount = changed.Amount; changed.Amount++;
            Require((await source.Store.SaveEntryAsync(changed, ct)).Succeeded);
            var staleRefused = false;
            try { await projection.RequireCurrentAsync(review, ct); } catch (InvalidOperationException) { staleRefused = true; }
            Require(staleRefused); changed.Amount = originalAmount; Require((await source.Store.SaveEntryAsync(changed, ct)).Succeeded);
            await projection.RequireCurrentAsync(review, ct); Require(await SourceDigestAsync() == before);
            proof("unreviewed-target-copy-and-stale-selected-source-refused-with-original-source-restored-and-unchanged");
            expected = new(review.Id, before, state.PinnedRoot.Fingerprint, state.DeviceFingerprint, selected.Select(account => account.Id).ToArray());
            await File.WriteAllTextAsync(proofPath, JsonSerializer.Serialize(expected), ct);
            var interrupted = new HouseholdJournal(new SeedCutFactory(targetFactory, () =>
            {
                proof("original-selected-seed-intent-and-keys-persisted-and-eight-financial-inserts-complete-before-native-commit-and-process-exit");
                Environment.Exit(34);
            }));
            _ = await interrupted.ApplyProjectionAsync(state.Authority, review, ct);
            throw new InvalidDataException("The actual selected-source process interruption was not exercised.");
        }
        expected = JsonSerializer.Deserialize<ProjectionProof>(await File.ReadAllTextAsync(proofPath, ct)) ?? throw new InvalidDataException();
        state = await journal.CaptureAsync(peer, identity, ct) ?? throw new InvalidDataException();
        Require(state.PinnedRoot.Fingerprint == expected.Root && state.DeviceFingerprint == expected.Device && await SourceDigestAsync() == expected.SourceDigest);
        if (mode is "projection-intent-draft" or "projection-intent-draft-cold")
        {
            var fresh = mode == "projection-intent-draft";
            var draftRoot = Path.Combine(root, "original-source-intent-draft");
            var coldProof = Path.Combine(root, "original-source-intent-draft.cold.complete");
            var intentProof = Path.Combine(root, "original-source-intent-draft-proof.json");
            if (fresh && Directory.Exists(draftRoot) || !fresh && File.Exists(coldProof))
                throw new IOException("The original completed selected-copy draft phase cannot be replayed.");
            using var client = await ProtectedClient.OpenAsync(draftRoot, peer, fresh, ct);
            client.Services.MigrateLocalDatabase<ZananceDbContext>();
            var draftJournal = client.Services.GetRequiredService<HouseholdJournal>(); HouseholdJournalState drafted;
            if (fresh)
            {
                using var reviewed = await projection.ReviewAsync(new(expected.Selected, true, true, "Excluded private counterpart"), ct);
                var sourceLocation = source.Services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
                using (var owner = await sourceLocation.Access.AcquireExclusiveAsync(ct))
                    await projection.RequireCurrentForMaintenanceAsync(reviewed, owner, ct);
                using var ownerKeys = SigningRecipientIdentity.Create(); using var deviceKeys = SigningRecipientIdentity.Create();
                drafted = await draftJournal.BeginDraftAsync(peer, identity, ownerKeys.Public, true, deviceKeys, ownerKeys, null, ct,
                    requireEmptyFinancial: true, selectedSource: reviewed);
                var draftReceipt = await draftJournal.ReadProjectionReceiptAsync(drafted.Authority, ct);
                Require(draftReceipt is { Completed: false } && draftReceipt.ReviewId == reviewed.Id);
                await File.WriteAllTextAsync(intentProof, JsonSerializer.Serialize(new ProjectionIntentProof(drafted.PinnedRoot.Fingerprint, drafted.DeviceFingerprint,
                    drafted.Authority.SetupOperation, reviewed.Id)), ct);
            }
            else drafted = await draftJournal.CaptureAsync(peer, identity, ct) ?? throw new InvalidDataException();
            var original = JsonSerializer.Deserialize<ProjectionIntentProof>(await File.ReadAllTextAsync(intentProof, ct)) ?? throw new InvalidDataException();
            Require(drafted.PinnedRoot.Fingerprint == original.Root && drafted.DeviceFingerprint == original.Device && drafted.Authority.SetupOperation == original.Setup);
            using var actualKeys = await draftJournal.ReadKeysAsync(drafted.Authority, ct);
            Require(actualKeys.Device.Public.Fingerprint == original.Device && actualKeys.Owner?.Public.Fingerprint == original.Root);
            var pending = await draftJournal.ReadProjectionReceiptAsync(drafted.Authority, ct);
            Require(pending is { Completed: false } && pending.ReviewId == original.Review && pending.Entries == 8 && pending.Attachments == 1);
            var refused = false;
            try { await draftJournal.BindRemoteAsync(drafted.Authority, Guid.NewGuid(), Guid.NewGuid(), ct); }
            catch (CryptographicException) { refused = true; }
            Require(refused && (await client.Store.GetAccountsAsync(cancellationToken: ct)).Count == 0 && await SourceDigestAsync() == expected.SourceDigest);
            if (!fresh) await File.WriteAllTextAsync(coldProof, "Original source intent and original owner/device keys reopen cold; incomplete copy cannot bind remotely.", ct);
            proof(fresh ? "original-source-copy-obligation-and-owner-device-keys-commit-together-before-any-copy-or-remote-binding"
                : "original-source-copy-obligation-and-same-keys-reopen-cold-and-refuse-empty-remote-publication"); return;
        }
        if (mode == "projection-maintenance-handoff")
        {
            var completedPath = Path.Combine(root, "projection-maintenance-handoff.complete");
            if (File.Exists(completedPath)) throw new InvalidOperationException("This original completed handoff qualification must not be replayed.");
            using var reviewed = await projection.ReviewAsync(new(expected.Selected, true, true, "Excluded private counterpart"), ct);
            var location = source.Services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>();
            using (var owner = await location.Access.AcquireExclusiveAsync(ct))
                await projection.RequireCurrentForMaintenanceAsync(reviewed, owner, ct);
            using var handoff = new Vafadar.Zanance.App.Features.Sharing.HouseholdProjectionHandoff();
            var destination = Guid.NewGuid().ToString("N"); handoff.Retain(destination, peer, identity, reviewed);
            Require(handoff.Read(destination, new LocalSyncPeer(peer.Address.AbsoluteUri, peer.Fingerprint), identity) == reviewed);
            Require(handoff.Read(destination, peer, "another-fictitious-identity") is null && handoff.Read("another-profile", peer, identity) is null);
            var refused = false;
            try { handoff.Retain(Guid.NewGuid().ToString("N"), peer, identity, reviewed); }
            catch (InvalidOperationException) { refused = true; }
            Require(refused && await SourceDigestAsync() == expected.SourceDigest);
            handoff.Finish(reviewed); Require(handoff.Read(destination, peer, identity) is null);
            await File.WriteAllTextAsync(completedPath, "Original protected source rechecked under drained maintenance; exact destination and peer/identity handoff verified without financial mutations.", ct);
            proof("original-protected-source-maintenance-reader-closes-before-exact-profile-peer-identity-handoff-and-source-finance-is-unchanged"); return;
        }
        if (mode == "projection-boundary-tamper")
        {
            using var boundary = await ProtectedClient.OpenAsync(Path.Combine(root, "write-boundary-target"), peer, false, ct);
            boundary.Services.MigrateLocalDatabase<ZananceDbContext>();
            var boundaryFactory = boundary.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
            var before = await boundary.Store.GetSettingsAsync(ct); var oldMonth = before.MonthStartDay;
            before.MonthStartDay = oldMonth == 7 ? 8 : 7; var intactRefusal = false;
            try { boundary.Store.SaveSettings(before); } catch (HouseholdWriteRejectedException error) { intactRefusal = error.Reason == HouseholdWriteBlockReason.Unpublished; }
            Require(intactRefusal);
            string original;
            await using (var db = await boundaryFactory.CreateDbContextAsync(ct))
            {
                await db.Database.OpenConnectionAsync(ct); using var command = db.Database.GetDbConnection().CreateCommand();
                command.CommandText = "SELECT sql FROM sqlite_schema WHERE type='trigger' AND name='zhh_guard_Settings_update'";
                original = (string)(await command.ExecuteScalarAsync(ct) ?? throw new InvalidDataException());
                await db.Database.ExecuteSqlRawAsync("DROP TRIGGER zhh_guard_Settings_update", ct);
            }
            var missingRefused = false;
            try
            {
                var changed = await boundary.Store.GetSettingsAsync(ct); changed.MonthStartDay = oldMonth == 7 ? 8 : 7;
                try { boundary.Store.SaveSettings(changed); } catch (HouseholdWriteRejectedException error) { missingRefused = error.Reason == HouseholdWriteBlockReason.Unverified; }
                Require(missingRefused && (await boundary.Store.GetSettingsAsync(ct)).MonthStartDay == oldMonth);
            }
            finally
            {
                await using var db = await boundaryFactory.CreateDbContextAsync(ct);
                await db.Database.ExecuteSqlRawAsync(original, ct);
            }
            var boundaryJournal = new HouseholdJournal(boundaryFactory);
            var draft = await boundaryJournal.CaptureAsync(peer, identity, ct) ?? throw new InvalidDataException();
            Require((await boundaryJournal.ReadProjectionReceiptAsync(draft.Authority, ct))?.Completed == true && await SourceDigestAsync() == expected.SourceDigest);
            proof("original-native-guard-definitions-are-required-before-financial-save-and-a-missing-trigger-cannot-borrow-migration-history-authority"); return;
        }
        if (mode == "projection-write-boundary")
        {
            var boundaryRoot = Path.Combine(root, "write-boundary-target");
            using var boundary = await ProtectedClient.OpenAsync(boundaryRoot, peer, !Directory.Exists(boundaryRoot), ct);
            boundary.Services.MigrateLocalDatabase<ZananceDbContext>();
            var boundaryFactory = boundary.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
            var boundaryJournal = new HouseholdJournal(boundaryFactory);
            var draft = await boundaryJournal.CaptureAsync(peer, identity, ct);
            if (draft is null)
            {
                using var owner = SigningRecipientIdentity.Create(); using var device = SigningRecipientIdentity.Create();
                draft = await boundaryJournal.BeginDraftAsync(peer, identity, owner.Public, true, device, owner, null, ct);
            }
            if ((await boundaryJournal.ReadProjectionReceiptAsync(draft.Authority, ct)) is { Completed: true }) return;
            await using (var db = await boundaryFactory.CreateDbContextAsync(ct))
                Require(!boundary.Services.GetRequiredService<Vafadar.Zanance.Data.Commerce.ICommercialWriteAccessSource>().Capture(DatabaseFileIdentity.ConfiguredPath(db)).Enforced);
            var ordinaryRefused = false;
            try { await boundary.Store.SaveAccountAsync(new Account { Name = "Fictitious unauthorized draft account", CurrencyCode = "EUR", OpeningDate = new(2026, 10, 16) }, ct); }
            catch (HouseholdWriteRejectedException error) { ordinaryRefused = error.Reason == HouseholdWriteBlockReason.Unpublished; }
            Require(ordinaryRefused);
            var trackedRefused = false;
            await using (var db = await boundaryFactory.CreateDbContextAsync(ct))
            {
                db.Accounts.Add(new Account { Name = "Fictitious direct tracked draft account", CurrencyCode = "EUR", OpeningDate = new(2026, 10, 16) });
                try { await db.SaveChangesAsync(ct); } catch (HouseholdWriteRejectedException error) { trackedRefused = error.Reason == HouseholdWriteBlockReason.Unpublished; }
            }
            Require(trackedRefused);
            var rawRefused = false;
            await using (var db = await boundaryFactory.CreateDbContextAsync(ct))
            {
                try { await db.Database.ExecuteSqlRawAsync("UPDATE Settings SET EssentialEstimate=17", ct); }
                catch (Microsoft.Data.Sqlite.SqliteException) { rawRefused = true; }
                Require(rawRefused && await db.Accounts.CountAsync(ct) == 0 && (await db.Settings.AsNoTracking().SingleAsync(ct)).EssentialEstimate != 17);
            }
            var settings = await boundary.Store.GetSettingsAsync(ct); var beforeMonth = settings.MonthStartDay;
            var originalReminder = settings.ReviewReminderEnabled; settings.ReviewReminderEnabled = !originalReminder; boundary.Store.SaveSettings(settings);
            settings = await boundary.Store.GetSettingsAsync(ct); Require(settings.ReviewReminderEnabled != originalReminder);
            settings.MonthStartDay = beforeMonth == 7 ? 8 : 7; var financialSettingsRefused = false;
            try { boundary.Store.SaveSettings(settings); } catch (HouseholdWriteRejectedException error) { financialSettingsRefused = error.Reason == HouseholdWriteBlockReason.Unpublished; }
            Require(financialSettingsRefused && (await boundary.Store.GetSettingsAsync(ct)).MonthStartDay == beforeMonth);
            proof("inactive-commercial-switch-cannot-authorize-draft-store-tracked-raw-or-financial-settings-writes-while-device-only-preferences-remain-usable");
            using var approved = await projection.ReviewAsync(new(expected.Selected, true, true, "Excluded private counterpart"), ct);
            await projection.RequireCurrentAsync(approved, ct);
            Require((await boundaryJournal.ApplyProjectionAsync(draft.Authority, approved, ct)).Completed);
            Require((await boundary.Store.GetEntriesAsync(cancellationToken: ct)).Count == 8 && (await boundary.Store.GetAccountsAsync(cancellationToken: ct)).Count == 4);
            Require(await SourceDigestAsync() == expected.SourceDigest);
            proof("original-reviewed-seed-uses-bounded-verified-maintenance-without-opening-an-editor-or-raw-SQL-bypass"); return;
        }
        if (mode == "projection-replace")
        {
            var replacementRoot = Path.Combine(root, "replacement-target");
            using var replacementTarget = await ProtectedClient.OpenAsync(replacementRoot, peer, !Directory.Exists(replacementRoot), ct);
            replacementTarget.Services.MigrateLocalDatabase<ZananceDbContext>();
            var replacementJournal = new HouseholdJournal(replacementTarget.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>());
            var replacementState = await replacementJournal.CaptureAsync(peer, identity, ct);
            if (replacementState is null)
            {
                using var owner = SigningRecipientIdentity.Create(); using var device = SigningRecipientIdentity.Create();
                replacementState = await replacementJournal.BeginDraftAsync(peer, identity, owner.Public, true, device, owner, null, ct);
            }
            var complete = await replacementJournal.ReadProjectionReceiptAsync(replacementState.Authority, ct);
            if (complete is { Completed: true }) { Require(complete.Entries == 8 && complete.Attachments == 0); return; }
            using var first = await projection.ReviewAsync(new(expected.Selected, false, false, "Excluded private counterpart"), ct);
            await projection.RequireCurrentAsync(first, ct);
            var pending = await replacementJournal.StageProjectionReviewAsync(replacementState.Authority, first, ct); Require(!pending.Completed);
            using var next = await projection.ReviewAsync(new(expected.Selected, true, false, "Excluded private counterpart"), ct);
            await projection.RequireCurrentAsync(next, ct);
            var implicitChangeRefused = false;
            try { await replacementJournal.ApplyProjectionAsync(replacementState.Authority, next, ct); } catch (CryptographicException) { implicitChangeRefused = true; }
            Require(implicitChangeRefused && (await replacementTarget.Store.GetAccountsAsync(cancellationToken: ct)).Count == 0);
            var staleConsentRefused = false;
            try { await replacementJournal.ReplacePendingProjectionAsync(replacementState.Authority, Guid.NewGuid(), next, ct); } catch (CryptographicException) { staleConsentRefused = true; }
            Require(staleConsentRefused);
            var changed = await replacementJournal.ReplacePendingProjectionAsync(replacementState.Authority, pending.ReviewId, next, ct);
            Require(!changed.Completed && changed.ReviewId == next.Id);
            var applied = await replacementJournal.ApplyProjectionAsync(replacementState.Authority, next, ct); Require(applied.Completed && applied.Entries == 8 && applied.Attachments == 0);
            var after = await replacementJournal.CaptureAsync(peer, identity, ct) ?? throw new InvalidDataException();
            Require(after.Authority == replacementState.Authority && after.PinnedRoot == replacementState.PinnedRoot && after.DeviceFingerprint == replacementState.DeviceFingerprint);
            Require(await SourceDigestAsync() == expected.SourceDigest);
            proof("pending-source-choice-never-changes-implicitly-and-explicit-current-owner-re-review-preserves-setup-keys-and-private-source"); return;
        }
        var receipt = await journal.ReadProjectionReceiptAsync(state.Authority, ct) ?? throw new InvalidDataException();
        Require(receipt.ReviewId == expected.Operation);
        if (mode == "projection-resume")
        {
            Require(!receipt.Completed && (await target.Store.GetAccountsAsync(cancellationToken: ct)).Count == 0 && (await target.Store.GetEntriesAsync(cancellationToken: ct)).Count == 0);
            var premature = false;
            try { await journal.BindRemoteAsync(state.Authority, Guid.NewGuid(), Guid.NewGuid(), ct); } catch (CryptographicException) { premature = true; }
            Require(premature);
            using var review = await projection.ReviewAsync(new(expected.Selected, true, true, "Excluded private counterpart"), ct);
            await projection.RequireCurrentAsync(review, ct);
            receipt = await journal.ApplyProjectionAsync(state.Authority, review, ct);
            Require(receipt.Completed && receipt.ReviewId == expected.Operation);
            Require(await journal.ApplyProjectionAsync(state.Authority, review, ct) == receipt);
            proof("cold-original-key-and-copy-operation-continue-after-native-rollback-and-apply-once-to-separate-protected-draft");
        }
        if (mode == "projection-cold")
        {
            var input = expected.Selected.ToArray();
            using (var accountsOnly = await projection.ReviewAsync(new(input, false, false, "Excluded private counterpart"), ct))
            {
                input[0] = Guid.NewGuid(); await projection.RequireCurrentAsync(accountsOnly, ct);
                Require(accountsOnly.Entries == 0 && accountsOnly.Attachments == 0 && accountsOnly.PrivateTransfers == 0 && accountsOnly.Graph.Records.Count == 2);
            }
            using (var withoutFiles = await projection.ReviewAsync(new(expected.Selected, true, false, "Excluded private counterpart"), ct))
                Require(withoutFiles.Entries == 8 && withoutFiles.Attachments == 0 && withoutFiles.PrivateTransfers == 3);
            proof("original-explicit-account-only-history-and-file-choices-remain-independent-and-mutable-UI-selection-cannot-change-captured-review");
        }
        Require(receipt.Completed); await VerifyTargetAsync();
        Require(await SourceDigestAsync() == expected.SourceDigest);
        var readableRefused = false;
        try { await target.Sync.ConnectAsync(await target.Sync.ReviewAsync(ct), peer, identity, Guid.NewGuid(), new(Guid.NewGuid(), "Fictitious readable wrong route", SyncSchema.Hash, 0, true), ct); }
        catch (InvalidOperationException) { readableRefused = true; }
        Require(readableRefused);
        proof("selected-EUR-KWD-history-private-side-transfers-refunds-attachment-and-fresh-identities-preserve-money-without-private-rows-or-readable-publication");

        async Task<string> SourceDigestAsync()
        {
            await using var db = await sourceFactory.CreateDbContextAsync(ct); await using var transaction = await db.Database.BeginTransactionAsync(ct);
            return HouseholdProjectionStore.Fingerprint((await PersonalSyncStore.ReadFinancialAsync(db, ct)).Select(row => new SyncRecord(row.Key, 0, row.Value)));
        }
        async Task VerifyTargetAsync()
        {
            await using var db = await targetFactory.CreateDbContextAsync(ct); await using var transaction = await db.Database.BeginTransactionAsync(ct);
            var actual = await PersonalSyncStore.ReadFinancialAsync(db, ct); var records = actual.Select(row => new SyncRecord(row.Key, 0, row.Value)).ToArray();
            Require(SyncFinancialValidation.Validate([], records).Count == 0);
            var text = string.Join('\n', actual.Values.Select(value => value?.GetRawText()));
            Require(!text.Contains("PRIVATE-CANARY", StringComparison.Ordinal) && !text.Contains("735369", StringComparison.Ordinal));
            var originalIds = (await source.Store.GetAccountsAsync(cancellationToken: ct)).Select(account => account.Id)
                .Concat((await source.Store.GetEntriesAsync(cancellationToken: ct)).Select(entry => entry.Id)).ToArray();
            Require(originalIds.All(id => !text.Contains(id.ToString("D"), StringComparison.OrdinalIgnoreCase)));
            var accounts = await db.Accounts.AsNoTracking().ToArrayAsync(ct); Require(accounts.Length == 4);
            var selectedEuro = accounts.Single(account => account.Name == "Fictitious selected EUR");
            var selectedKwd = accounts.Single(account => account.Name == "Fictitious selected KWD");
            Require(selectedEuro.OpeningBalance == 100000 && selectedKwd.OpeningBalance == 200000);
            var counterparts = accounts.Where(account => account.Name == "Excluded private counterpart").ToArray();
            Require(counterparts.Length == 2 && counterparts.All(account => !account.IncludeInTotals && !account.UsableForPayments && !account.OpeningBalanceKnown && account.OpeningBalance == 0 && account.Counterparty is null && account.CountryCode is null));
            var entries = await db.Entries.AsNoTracking().ToArrayAsync(ct); Require(entries.Length == 8);
            var sourceAccounts = await source.Store.GetAccountsAsync(cancellationToken: ct); var sourceEntries = await source.Store.GetEntriesAsync(cancellationToken: ct);
            foreach (var account in new[] { selectedEuro, selectedKwd })
            {
                var original = sourceAccounts.Single(item => item.Name == account.Name);
                Require(LedgerCalculator.Balance(account, entries, new(2026, 10, 16)) == LedgerCalculator.Balance(original, sourceEntries, new(2026, 10, 16)));
            }
            var sourceTotals = LedgerCalculator.Totals(sourceAccounts, sourceEntries, new(new(2026, 1, 1), new(2026, 12, 31), expected.Selected));
            var targetTotals = LedgerCalculator.Totals(accounts, entries, new(new(2026, 1, 1), new(2026, 12, 31), [selectedEuro.Id, selectedKwd.Id]));
            Require(sourceTotals.SequenceEqual(targetTotals));
            var eurToPrivate = entries.Single(entry => entry.Title == "Fictitious outgoing private side");
            Require(eurToPrivate.Kind == EntryKind.Transfer && eurToPrivate.AccountId == selectedEuro.Id && eurToPrivate.Amount == 2500 && eurToPrivate.ToAmount == 2500);
            var privateToKwd = entries.Single(entry => entry.Title == "Fictitious incoming private side");
            Require(privateToKwd.Kind == EntryKind.Transfer && privateToKwd.ToAccountId == selectedKwd.Id && privateToKwd.Amount == 900 && privateToKwd.ToAmount == 900);
            var sameCurrency = entries.Single(entry => entry.Title == "Fictitious same-currency incoming");
            Require(sameCurrency.Amount == 700 && sameCurrency.ToAmount == 700 && sameCurrency.ToAccountId == selectedEuro.Id);
            var selectedTransfer = entries.Single(entry => entry.Title == "Fictitious selected FX");
            Require(selectedTransfer.AccountId == selectedEuro.Id && selectedTransfer.ToAccountId == selectedKwd.Id && selectedTransfer.Amount == 1000 && selectedTransfer.ToAmount == 330);
            var purchase = entries.Single(entry => entry.Title == "Fictitious selected purchase");
            Require(entries.Single(entry => entry.Title == "Fictitious selected refund").RefundOfId == purchase.Id && entries.Single(entry => entry.Title == "Fictitious detached refund").RefundOfId is null);
            Require(entries.All(entry => entry.ScheduleId is null && entry.OccurrenceDate is null && entry.ImportBatchId is null && entry.GroupId is null));
            Require(await db.Schedules.CountAsync(ct) == 0 && await db.CategoryRules.CountAsync(ct) == 0);
            var attachment = await db.Attachments.AsNoTracking().SingleAsync(ct);
            try { Require(attachment.Data.AsSpan().SequenceEqual(Encoding.UTF8.GetBytes("Fictitious explicitly shared attachment")) && attachment.EntryId == purchase.Id); }
            finally { CryptographicOperations.ZeroMemory(attachment.Data); }
        }
        async Task SeedSourceAsync()
        {
            var eur = new Account { Name = "Fictitious selected EUR", CurrencyCode = "EUR", OpeningBalance = 100000, OpeningDate = new(2026, 1, 1) };
            var kwd = new Account { Name = "Fictitious selected KWD", CurrencyCode = "KWD", OpeningBalance = 200000, OpeningDate = new(2026, 1, 1) };
            var usd = new Account { Name = "PRIVATE-CANARY USD account", CurrencyCode = "USD", OpeningBalance = 735369, OpeningDate = new(2026, 1, 1), Counterparty = "PRIVATE-CANARY lender", CountryCode = "US" };
            var privateEuro = new Account { Name = "PRIVATE-CANARY EUR account", CurrencyCode = "EUR", OpeningBalance = 735369, OpeningDate = new(2026, 1, 1) };
            foreach (var account in new[] { eur, kwd, usd, privateEuro }) Require(await source.Store.SaveAccountAsync(account, ct));
            var parent = new Category { Name = "Fictitious selected parent", Kind = CategoryKind.Expense };
            var category = new Category { Name = "Fictitious selected category", Kind = CategoryKind.Expense, ParentId = parent.Id };
            var hidden = new Category { Name = "PRIVATE-CANARY category", Kind = CategoryKind.Expense };
            foreach (var item in new[] { parent, category, hidden }) await source.Store.SaveCategoryAsync(item, ct);
            var day = new DateOnly(2026, 10, 16);
            var purchase = new LedgerEntry { AccountId = eur.Id, Kind = EntryKind.Expense, Amount = 1000, Date = day, Title = "Fictitious selected purchase", CategoryId = category.Id };
            var privatePurchase = new LedgerEntry { AccountId = privateEuro.Id, Kind = EntryKind.Expense, Amount = 500, Date = day, Title = "PRIVATE-CANARY purchase", CategoryId = hidden.Id };
            foreach (var entry in new[] { purchase, privatePurchase }) Require((await source.Store.SaveEntryAsync(entry, ct)).Succeeded);
            var entries = new[] {
                new LedgerEntry { AccountId = eur.Id, Kind = EntryKind.Refund, Amount = 200, Date = day, Title = "Fictitious selected refund", RefundOfId = purchase.Id, CategoryId = category.Id },
                new LedgerEntry { AccountId = eur.Id, Kind = EntryKind.Refund, Amount = 100, Date = day, Title = "Fictitious detached refund", RefundOfId = privatePurchase.Id, CategoryId = category.Id },
                new LedgerEntry { AccountId = eur.Id, ToAccountId = usd.Id, Kind = EntryKind.Transfer, Amount = 2500, ToAmount = 2700, Date = day, Title = "Fictitious outgoing private side" },
                new LedgerEntry { AccountId = usd.Id, ToAccountId = kwd.Id, Kind = EntryKind.Transfer, Amount = 3000, ToAmount = 900, Date = day, Title = "Fictitious incoming private side" },
                new LedgerEntry { AccountId = privateEuro.Id, ToAccountId = eur.Id, Kind = EntryKind.Transfer, Amount = 700, Date = day, Title = "Fictitious same-currency incoming" },
                new LedgerEntry { AccountId = eur.Id, ToAccountId = kwd.Id, Kind = EntryKind.Transfer, Amount = 1000, ToAmount = 330, Date = day, Title = "Fictitious selected FX" },
                new LedgerEntry { AccountId = eur.Id, Kind = EntryKind.Expense, Amount = 25, Date = day, Title = "Fictitious selected fee", CategoryId = category.Id, ImportBatchId = Guid.NewGuid(), GroupId = Guid.NewGuid() }
            };
            foreach (var entry in entries) Require((await source.Store.SaveEntryAsync(entry, ct)).Succeeded);
            await source.Store.AddAttachmentAsync(new EntryAttachment { EntryId = purchase.Id, FileName = "fictitious-selected.txt", ContentType = "text/plain", Data = Encoding.UTF8.GetBytes("Fictitious explicitly shared attachment") }, ct);
            await source.Store.AddAttachmentAsync(new EntryAttachment { EntryId = privatePurchase.Id, FileName = "PRIVATE-CANARY.txt", ContentType = "text/plain", Data = Encoding.UTF8.GetBytes("PRIVATE-CANARY attachment must never be shared") }, ct);
            await using var db = await sourceFactory.CreateDbContextAsync(ct);
            db.Schedules.Add(new Schedule { Name = "PRIVATE-CANARY plan", AccountId = privateEuro.Id, Amount = 99, Rule = new() { Start = day, Frequency = Frequency.Once }, CategoryId = hidden.Id });
            db.CategoryRules.Add(new CategoryRule { Match = "PRIVATE-CANARY rule", CategoryId = hidden.Id, Kind = CategoryKind.Expense });
            var settings = await db.Settings.SingleAsync(ct); settings.EssentialEstimate = 735369; await db.SaveChangesAsync(ct);
        }
    }

    /// <summary>Wraps the actual registered protected factory and observes the final SaveChanges boundary before its original transaction commits.</summary>
    private sealed class SeedCutFactory(IDbContextFactory<ZananceDbContext> original, Action cut) : IDbContextFactory<ZananceDbContext>
    {
        public ZananceDbContext CreateDbContext() => Observe(original.CreateDbContext());
        public async Task<ZananceDbContext> CreateDbContextAsync(CancellationToken ct = default) => Observe(await original.CreateDbContextAsync(ct));
        private ZananceDbContext Observe(ZananceDbContext db)
        {
            db.SavingChanges += (_, _) =>
            {
                if (db.ChangeTracker.Entries<HouseholdLocalBinding>().Any(row => row.Entity.ProjectionContentHash is not null))
                {
                    if (db.Entries.Count() != 8 || db.Accounts.Count() != 4) throw new InvalidDataException("The actual selected financial writes were not complete before interruption.");
                    cut();
                }
            };
            return db;
        }
    }
    /// <summary>Synthetic public identifiers/digests only; no original private keys or financial content is written here.</summary>
    private sealed record ProjectionProof(Guid Operation, string SourceDigest, string Root, string Device, Guid[] Selected);
    private sealed record ProjectionIntentProof(string Root, string Device, Guid Setup, Guid Review);
}
