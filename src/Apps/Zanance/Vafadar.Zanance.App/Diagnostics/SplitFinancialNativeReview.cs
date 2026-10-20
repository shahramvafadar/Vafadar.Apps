#if (DEBUG && WINDOWS) || ZANANCE_INDEPENDENT_HOME_REVIEW
using System.Text.Json;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.App.Features.Entries;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

/// <summary>Original split/join financial commands once, then retained outcome forms for read-only native presentation.</summary>
internal static class SplitFinancialNativeReview
{
    /// <summary>Gets only bounded pre-invoke counts/identity predicates, never paths, contents or key material.</summary>
    internal static object? PreInvokeEvidence { get; private set; }

    /// <summary>A real original outcome remains attached to its original session across presentation-only contexts.</summary>
    internal sealed record State(string Name, SplitEditorViewModel Model, string ErrorKey);
    /// <summary>Retained original states and complete financial receipts; no presentation case posts money.</summary>
    internal sealed record Prepared(IReadOnlyList<State> States, IReadOnlyList<object> Cases);

    /// <summary>Records original navigation without replacing the independently owned review page.</summary>
    private sealed class Dialogs : IAppInteraction
    {
        public int Navigations { get; private set; }
        public Task AlertAsync(string title, string message, string cancel) => throw new IOException("Unexpected split review alert.");
        public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel) => throw new IOException("No supplied split consent belongs here.");
        public Task<string?> PromptAsync(string title, string message, string accept, string cancel, string? initialValue = null, int maxLength = -1) => throw new IOException("No split review prompt belongs here.");
        public Task<string?> ChooseAsync(string title, string cancel, params string[] actions) => throw new IOException("No split review menu belongs here.");
        public Task NavigateAsync(string route, IDictionary<string, object>? parameters = null)
        { if (route != "..") throw new IOException("Unexpected original split route."); Navigations++; return Task.CompletedTask; }
        public Task ShowFailureAsync(Exception exception) => throw new IOException("Unexpected original split failure.", exception);
    }

    /// <summary>Refuses existing fixtures instead of replaying accepted money for a later native observer.</summary>
    internal static async Task<Prepared> PrepareAsync(IServiceProvider services, string folder,
        Func<SplitEditorPage, Task> show, Func<SplitEditorPage, ICommand, Task> invoke,
        Func<SplitEditorPage, Task> close, CancellationToken ct, bool resumeAfterRefusal = false, Action<string>? progress = null)
    {
        PreInvokeEvidence = null;
        var package = AppInfo.Current.PackageName;
        if (!package.StartsWith("pro.vafadar.zanance.storepreview.", StringComparison.Ordinal)
            && package != "pro.vafadar.zanance.androidhomereview") throw new IOException("Independent split review identity required.");
        Directory.CreateDirectory(folder);
        var text = services.GetRequiredService<Translator>(); var local = services.GetRequiredService<ILocalizationService>();
        var time = services.GetRequiredService<TimeProvider>(); var day = DateOnly.FromDateTime(time.GetLocalNow().DateTime);
        var states = new List<State>(); var cases = new List<object>();
        foreach (var join in new[] { false, true })
        {
            var name = join ? "join" : "split"; var file = Path.Combine(folder, name + ".db");
            var retained = resumeAfterRefusal && !join;
            PreInvokeEvidence = new { RetainedRequested = retained, OriginalFileExists = File.Exists(file),
                RefusalAlreadyRecorded = File.Exists(Path.Combine(folder, "split-refused.json")),
                AcceptanceAlreadyRecorded = File.Exists(Path.Combine(folder, "split-accepted.json")),
                JoinAlreadyPrepared = File.Exists(Path.Combine(folder, "join.db")) };
            if (File.Exists(file) != retained) throw new IOException("Retain the original split fixture; only its exact pre-invoke continuation may reuse it.");
            await using var fixture = new ServiceCollection().AddZananceData(file).AddSingleton(time).BuildServiceProvider();
            if (!retained) await Task.Run(() => fixture.MigrateLocalDatabase<ZananceDbContext>(), ct);
            var store = fixture.GetRequiredService<ZananceStore>(); await store.GetSettingsAsync(ct);
            Account account; LedgerEntry entry;
            if (retained)
            {
                if (!File.Exists(Path.Combine(folder, "split-refused.json")) || File.Exists(Path.Combine(folder, "split-accepted.json"))
                    || File.Exists(Path.Combine(folder, "join.db"))) throw new IOException("A progressed original financial case cannot be replayed.");
                var accounts = await store.GetAccountsAsync(); var retainedRows = await store.GetEntriesAsync(cancellationToken: ct);
                PreInvokeEvidence = new { RetainedRequested = true, AccountCount = accounts.Count, EntryCount = retainedRows.Count };
                if (accounts.Count != 1 || retainedRows.Count != 1) throw new IOException("Exact original uninvoked split seed required.");
                account = accounts.Single(); entry = retainedRows.Single();
                var attachmentCount = (await store.GetAttachmentsAsync(entry.Id, ct)).Count;
                PreInvokeEvidence = new { RetainedRequested = true, AccountCount = accounts.Count, EntryCount = retainedRows.Count,
                    AccountNameMatches = account.Name == "D407 fictitious split", CurrencyMatches = account.CurrencyCode == "EUR",
                    OpeningMatches = account.OpeningBalance == 90000, EntryAccountMatches = entry.AccountId == account.Id,
                    EntryKindMatches = entry.Kind == EntryKind.Expense, OriginalUngrouped = entry.GroupId is null,
                    AmountMatches = entry.Amount == 10000, TitleMatches = entry.Title == "D407 original purchase",
                    PayeeMatches = entry.Payee == "D407 original payee", NoteMatches = entry.Note == "D407 newer original note",
                    TagsMatch = entry.Tags.SequenceEqual(new[] { "D407", "retained" }), AttachmentCount = attachmentCount };
                if (account.Name != "D407 fictitious split" || account.CurrencyCode != "EUR" || account.OpeningBalance != 90000
                    || entry.AccountId != account.Id || entry.Kind != EntryKind.Expense || entry.GroupId is not null || entry.Amount != 10000
                    || entry.Title != "D407 original purchase" || entry.Payee != "D407 original payee"
                    || entry.Note != "D407 newer original note" || !entry.Tags.SequenceEqual(new[] { "D407", "retained" })
                    || attachmentCount != 0)
                    throw new IOException("Original pre-invoke identity, amount or metadata changed; do not reseed.");
                await File.WriteAllTextAsync(Path.Combine(folder, "split-retained-after-refusal.json"), JsonSerializer.Serialize(new
                { OriginalEntryId = entry.Id, OriginalAccountId = account.Id, OriginalAmountMinor = entry.Amount,
                    OriginalSourceNote = entry.Note, SameOriginalSeed = true, SeedOrFinancialCommandRepeated = false }), ct);
                progress?.Invoke("retained-original-refusal-and-seed");
            }
            else
            {
                account = new Account { Name = "D407 fictitious " + name, CurrencyCode = "EUR", OpeningDate = day, OpeningBalance = 90000 };
                await store.SaveAccountAsync(account, ct);
                entry = new LedgerEntry { AccountId = account.Id, Kind = EntryKind.Expense, Date = day, Amount = 10000,
                    Title = "D407 original purchase", Payee = "D407 original payee", Note = "D407 original note", Tags = ["D407", "retained"] };
                if (!(await store.SaveEntryAsync(entry, ct)).Succeeded) throw new IOException("Original split fixture failed.");
            }
            if (join)
            {
                var (seed, delete) = EntryActions.Split([entry.Copy()], [(null, 6000), (null, 4000)]);
                if (!(await store.SaveEntriesAsync(seed, delete, seed[0].Id, ct)).Succeeded) throw new IOException("Original join fixture failed.");
                var second = seed.Single(e => e.Id != entry.Id);
                await store.AddAttachmentAsync(new EntryAttachment { EntryId = second.Id, FileName = "fictitious.txt", ContentType = "text/plain", Data = [1, 2, 3] }, ct);
            }
            SplitEditorViewModel Model(Dialogs port) => new(store, text, local, port);
            async Task Load(SplitEditorViewModel vm)
            {
                await vm.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = entry.Id });
                if (!join) { vm.Parts[0].AmountText = MoneyText.ForInput(6000, "EUR", local.CurrentCulture); vm.Parts[1].AmountText = MoneyText.ForInput(4000, "EUR", local.CurrentCulture); }
            }
            async Task Invoke(SplitEditorPage page, IAsyncRelayCommand command)
            {
                await invoke(page, command);
                var deadline = DateTime.UtcNow.AddSeconds(10);
                while (command.ExecutionTask is null)
                {
                    if (DateTime.UtcNow >= deadline) throw new IOException("Observe the original native invocation without replay; command did not publish its task.");
                    await Task.Delay(20, ct);
                }
                await command.ExecutionTask;
            }

            var refusedPort = new Dialogs(); var refused = Model(refusedPort); await Load(refused);
            var draft = JsonSerializer.Serialize(refused.Parts.Select(p => new { Category = p.Category?.Id, p.AmountText }));
            var newer = (await store.GetEntryAsync(entry.Id, ct))!;
            if (retained)
            {
                using var receipt = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(folder, "split-refused.json"), ct));
                var original = receipt.RootElement;
                if (!original.GetProperty("OriginalSourceUnchanged").GetBoolean() || !original.GetProperty("OriginalRawDraftKept").GetBoolean()
                    || !original.GetProperty("OriginalCommandRefused").GetBoolean() || original.GetProperty("Package").GetString() != package
                    || original.GetProperty("ProcessId").GetInt32() <= 0)
                    throw new IOException("Exact original successful refusal receipt required; never repeat it.");
                var beforePresentation = await ImportFinancialNativeReview.StoredAsync(fixture, ct);
                // Rehydrate only the recorded refused presentation. Its successful financial command is not replayed.
                refused.RetireReview(); refused.Error = text["Split_SourceChanged"];
                if (beforePresentation != await ImportFinancialNativeReview.StoredAsync(fixture, ct) || refused.CanSave || refused.CanJoin)
                    throw new IOException("Read-only original refusal presentation changed source or authority.");
                await File.WriteAllTextAsync(Path.Combine(folder, "split-refusal-presentation-continuation.json"), JsonSerializer.Serialize(new
                { OriginalRefusalProcessId = original.GetProperty("ProcessId").GetInt32(), OriginalSuccessfulRefusalRetained = true,
                    ReadOnlyPresentationRehydrated = true, NoSeedSourceEditOrRefusalReplay = true }), ct);
                progress?.Invoke("split-original-successful-refusal-retained");
            }
            else
            {
                newer.Note = "D407 newer original note";
                if (!(await store.SaveEntryAsync(newer, ct)).Succeeded) throw new IOException("Original independent source edit failed.");
                var before = await ImportFinancialNativeReview.StoredAsync(fixture, ct);
                var refusedPage = new SplitEditorPage(refused); await show(refusedPage);
                progress?.Invoke(name + "-native-refusal-requested");
                await Invoke(refusedPage, join ? refused.JoinCommand : refused.SaveCommand);
                if (before != await ImportFinancialNativeReview.StoredAsync(fixture, ct)
                    || refused.Error != text["Split_SourceChanged"] || refused.CanSave || refused.CanJoin
                    || draft != JsonSerializer.Serialize(refused.Parts.Select(p => new { Category = p.Category?.Id, p.AmountText }))
                    || refusedPort.Navigations != 0) throw new IOException("Original stale split/join refusal did not preserve its complete source and raw parts.");
                await File.WriteAllTextAsync(Path.Combine(folder, name + "-refused.json"), JsonSerializer.Serialize(new
                { OriginalSourceUnchanged = true, OriginalRawDraftKept = true, OriginalCommandRefused = true, Package = package, ProcessId = Environment.ProcessId }), ct);
                progress?.Invoke(name + "-native-refused"); await close(refusedPage);
            }
            if (!join) states.Add(new("refused", refused, "Split_SourceChanged"));

            var acceptedPort = new Dialogs(); var accepted = Model(acceptedPort); await Load(accepted);
            var page = new SplitEditorPage(accepted); await show(page);
            EventHandler broken = (_, _) => throw new IOException("Deliberate original split post-commit notification failure.");
            if (!join) store.Changed += broken;
            progress?.Invoke(name + "-native-acceptance-requested");
            try { await Invoke(page, join ? accepted.JoinCommand : accepted.SaveCommand); }
            finally { if (!join) store.Changed -= broken; }
            var rows = await store.GetEntriesAsync(cancellationToken: ct);
            if (rows.Count != (join ? 1 : 2) || rows.Sum(e => e.Amount) != 10000 || accepted.CanSave || accepted.CanJoin || accepted.CanEdit || accepted.IsDirty
                || accepted.CloseText != text["Common_Close"] || (join ? acceptedPort.Navigations != 1 : accepted.Error != text["Split_SavedNeedsReview"]))
                throw new IOException("Original split/join accepted state is not terminal.");
            var first = rows.Single(e => e.Id == entry.Id);
            if (first.Note != newer.Note || rows.Any(e => e.Payee != entry.Payee || e.Title != entry.Title || !e.Tags.SequenceEqual(entry.Tags)))
                throw new IOException("Original split/join metadata changed.");
            if (join)
            {
                var moved = await store.GetAttachmentsAsync(entry.Id, ct);
                if (moved.Count != 1 || (await store.GetAttachmentAsync(moved[0].Id, ct))!.Data is not [1, 2, 3])
                    throw new IOException("Original join attachment ownership changed.");
            }
            var result = new { Kind = name, StaleRefusalBeforeMoney = true, CompleteOriginalRowsUnchangedOnRefusal = true,
                RawPartsKept = true, FreshReviewAcceptedOnce = true, Count = rows.Count, TotalMinor = rows.Sum(e => e.Amount),
                OriginalMetadataPreserved = true, AttachmentOwnerPreserved = true, AcceptedPostCommitNotificationFailure = !join,
                AcceptedFormRetired = true, ContinuedOriginalPreInvokeSeed = retained, Package = package, ProcessId = Environment.ProcessId };
            await File.WriteAllTextAsync(Path.Combine(folder, name + "-accepted.json"), JsonSerializer.Serialize(result), ct);
            progress?.Invoke(name + "-native-accepted");
            cases.Add(result); await close(page);
            if (!join) states.Add(new("accepted", accepted, "Split_SavedNeedsReview"));
        }
        return new(states, cases);
    }
}
#endif
