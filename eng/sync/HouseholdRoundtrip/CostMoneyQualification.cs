using System.Resources;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Zanance.Data;
using Vafadar.Core.Settings;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Sharing;
using Vafadar.Zanance.App.Features.Sync;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.Core.Sharing;
using Vafadar.Zanance.Data.Sharing;
using Vafadar.Zanance.Data.Sync;
using Vafadar.Zanance.ProtectedSync;

/// <summary>D-436 continuation over the original approved protected Editor, without HTTP or replacement membership authority.</summary>
internal static class CostMoneyQualification
{
    internal static async Task RunAsync(string root, Action<string> proof, CancellationToken ct)
    {
        var done = Path.Combine(root, "costs-money.complete.json");
        var started = Path.Combine(root, "costs-money.started.json");
        var accepted = Path.Combine(root, "costs-money.accepted.json");
        if (File.Exists(done) || File.Exists(started) || File.Exists(accepted))
            throw new IOException("Retain the original monetary continuation; never replay an accepted or uncertain financial command.");
        var clientRoot = Path.Combine(root, "returning-owner");
        var peer = new LocalSyncPeer("https://localhost:55555", new('a', 64));
        using var client = await ProtectedClient.OpenAsync(clientRoot, peer, false, ct);
        // Reopen the retained keys/generation through the original current additive schema path.
        client.Services.MigrateLocalDatabase<ZananceDbContext>();
        var store = client.Services.GetRequiredService<HouseholdCostStore>(); var before = await store.ReadAsync(ct);
        await File.WriteAllTextAsync(Path.Combine(root, "costs-money.preflight.json"), JsonSerializer.Serialize(new
        { before.CanWrite, Contacts = before.Contacts.Count, Costs = before.Costs.Count, Settlements = before.Settlements.Count }), ct);
        if (!before.CanWrite || before.Costs.Count == 0 || before.Settlements.Count == 0)
            throw new InvalidDataException("The retained protected client needs its actual current household write permission and original cost state.");
        var original = before.Costs.First(row => row.Kind == HouseholdCostKind.Expense);
        static string Facts(HouseholdCostReview value) => JsonSerializer.Serialize(new
        { value.Contacts, value.Costs, value.Allocations, value.Settlements, value.LinkedMovements });
        var originalFacts = Facts(before);
        var translator = new Translator(); translator.AddResources(new ResourceManager("HouseholdRoundtrip.Resources.AppResources", typeof(CostMoneyQualification).Assembly));
        var local = new LocalizationService(new LocalizationOptions(), new InMemorySettingsStore(), translator);
        local.SetLanguage(local.SupportedLanguages.Single(row => row.CultureName == "en")); local.SetFormattingCulture("en-US");
        void Validate() { ct.ThrowIfCancellationRequested(); if (File.ReadAllText(Path.Combine(clientRoot, "client.marker")) != "Fictitious protected sync client v1") throw new IOException(); }
        var effects = new Effects(new ForegroundQualification.NativeEffects(peer, client, ct, Validate), async () =>
        {
            // This receipt is written only after the original production Save returns known acceptance.
            await File.WriteAllTextAsync(accepted, Facts(await store.ReadAsync(ct)), ct);
            throw new IOException("Owned post-acceptance publication failure.");
        });
        var dialogs = new Dialogs();
        var vm = new HouseholdCostsViewModel(store, effects, dialogs, local, new DateFormatter(local), translator, TimeProvider.System);
        await vm.ActivateAsync();
        if (!vm.CanWrite || vm.Error is not null) throw new InvalidDataException("Original household monetary form did not publish.");
        await vm.EditCostCommand.ExecuteAsync(vm.Costs.Single(row => row.Source.Id == original.Id));
        var title = vm.Title;
        dialogs.Answer = () => { vm.Title = title + " changed during consent"; vm.Title = title; return true; };
        await vm.SaveCommand.ExecuteAsync(null);
        if (vm.Error is null || !vm.HasEditor || Facts(await store.ReadAsync(ct)) != originalFacts)
            throw new InvalidDataException("Edit-and-return during original consent was not refused.");
        proof("original-household-cost-consent-edit-and-return-refused-with-all-financial-rows-unchanged");
        var shares = before.Allocations.Where(row => row.CostId == original.Id).ToArray();
        var checks = 0;
        try { await store.SaveCostAsync(before, original, shares, ct, () => ++checks < 3); throw new InvalidDataException("Original cost writer unexpectedly accepted a retired owner."); }
        catch (InvalidOperationException) when (checks == 3) { }
        if (Facts(await store.ReadAsync(ct)) != originalFacts) throw new InvalidDataException("Retired cost writer did not roll back.");
        checks = 0;
        try { await store.SaveSettlementAsync(before, before.Settlements[0], ct, () => ++checks < 3); throw new InvalidDataException("Original repayment writer unexpectedly accepted a retired owner."); }
        catch (InvalidOperationException) when (checks == 3) { }
        if (Facts(await store.ReadAsync(ct)) != originalFacts) throw new InvalidDataException("Retired repayment writer did not roll back.");
        proof("original-household-cost-and-repayment-post-save-owner-retirement-roll-back-on-native-protected-storage");
        vm.CancelEditCommand.Execute(null);
        await vm.EditCostCommand.ExecuteAsync(vm.Costs.Single(row => row.Source.Id == original.Id));
        vm.Title += " original monetary continuation"; dialogs.Answer = () => true;
        // Refuse an uncertain replay even if the observer fails before it can persist the accepted receipt.
        await File.WriteAllTextAsync(started, original.Id.ToString("D"), ct);
        await vm.SaveCommand.ExecuteAsync(null);
        var after = await store.ReadAsync(ct); var afterFacts = Facts(after);
        if (!File.Exists(accepted) || vm.HasEditor || vm.CanWrite || vm.Error != translator["HouseholdCost_SavedFollowUpFailed"]
            || after.Costs.Single(row => row.Id == original.Id).Title != title + " original monetary continuation"
            || after.Costs.Single(row => row.Id == original.Id).Amount != original.Amount)
            throw new InvalidDataException("Known original household acceptance was not consumed before publication failure.");
        await vm.SaveCommand.ExecuteAsync(null);
        if (afterFacts != Facts(await store.ReadAsync(ct))) throw new InvalidDataException("Known original cost acceptance replayed.");
        await vm.ReloadCommand.ExecuteAsync(null);
        if (!vm.CanWrite || vm.HasEditor || vm.Error is not null) throw new InvalidDataException("Explicit readback did not recover the accepted original household.");
        vm.Deactivate();
        await File.WriteAllTextAsync(done, JsonSerializer.Serialize(new
        { OriginalCost = original.Id, OriginalAmount = original.Amount, AcceptedCostMetadataEdits = 1, AcceptedNewCosts = 0,
            AcceptedRepayments = 0, OriginalLedgerUnchanged = before.LinkedMovements.SequenceEqual(after.LinkedMovements),
            PostSaveRollbackCases = 2, NoHttp = true, NoAcceptedCommandReplayed = true }), ct);
        proof("original-household-known-acceptance-consumed-before-failed-publication-explicit-readback-without-replay-pass");
    }

    /// <summary>Continues only changed-row rollback qualification; the accepted metadata edit above is never replayed.</summary>
    internal static async Task RunRollbackAsync(string root, Action<string> proof, CancellationToken ct)
    {
        var done = Path.Combine(root, "costs-money-rollback.complete.json");
        if (!File.Exists(Path.Combine(root, "costs-money.complete.json")) || File.Exists(done))
            throw new IOException("Continue only the completed monetary source with new changed-row rollback cases.");
        using var client = await ProtectedClient.OpenAsync(Path.Combine(root, "returning-owner"), new("https://localhost:55555", new('a', 64)), false, ct);
        var store = client.Services.GetRequiredService<HouseholdCostStore>(); var original = await store.ReadAsync(ct);
        if (!original.CanWrite) throw new InvalidDataException("Actual original native household permission is required.");
        static string Facts(HouseholdCostReview value) => JsonSerializer.Serialize(new
        { value.Contacts, value.Costs, value.Allocations, value.Settlements, value.LinkedMovements });
        var before = Facts(original);
        // Detached proposals contain actual different values so SaveChanges executes DML before retirement.
        var cost = JsonSerializer.Deserialize<HouseholdCost>(JsonSerializer.Serialize(original.Costs.First()))!;
        cost.Title += " rolled back pending edit";
        var repayment = JsonSerializer.Deserialize<HouseholdSettlement>(JsonSerializer.Serialize(original.Settlements.First()))!;
        repayment.Note = "Owned pending repayment edit that must roll back";
        var checks = 0;
        try { await store.SaveCostAsync(original, cost, original.Allocations.Where(row => row.CostId == cost.Id).ToArray(), ct, () => ++checks < 3);
            throw new InvalidDataException("Changed native cost accepted a retired owner."); }
        catch (InvalidOperationException) when (checks == 3) { }
        if (before != Facts(await store.ReadAsync(ct))) throw new InvalidDataException("Changed native cost did not roll back.");
        checks = 0;
        try { await store.SaveSettlementAsync(original, repayment, ct, () => ++checks < 3);
            throw new InvalidDataException("Changed native repayment accepted a retired owner."); }
        catch (InvalidOperationException) when (checks == 3) { }
        if (before != Facts(await store.ReadAsync(ct))) throw new InvalidDataException("Changed native repayment did not roll back.");
        await File.WriteAllTextAsync(done, JsonSerializer.Serialize(new { ChangedRowRollbacks = 2, AcceptedWrites = 0, NoHttp = true }), ct);
        proof("changed-native-cost-title-and-repayment-note-roll-back-after-original-save-before-commit-without-accepted-action-replay");
    }

    /// <summary>Original access/configuration effects with one explicit fallible post-acceptance publication.</summary>
    private sealed class Effects(IPersonalSyncEnvironment original, Func<Task> publish) : IPersonalSyncEnvironment
    {
        public bool Available => original.Available;
        public event EventHandler? StatusChanged { add { } remove { } }
        public PersonalSyncAccess Capture() => original.Capture();
        public PersonalSyncConnection ReadConnection() => original.ReadConnection();
        public PersonalSyncTransport Open(LocalSyncPeer peer, Action validate) => throw new IOException("No HTTP belongs to this continuation.");
        public Task PublishAppliedAsync(CancellationToken ct) { ct.ThrowIfCancellationRequested(); return publish(); }
        public void RememberPeer(LocalSyncPeer peer) => throw new InvalidOperationException();
        public void ConnectionChanged() => throw new InvalidOperationException();
        public Guid CreationOperation(string digest) => throw new InvalidOperationException();
        public void CompleteCreation(string digest, Guid operation) => throw new InvalidOperationException();
        public Task<PersonalSyncRenderedPage> RenderAttachmentPageAsync(PersonalSyncAttachmentContent content, int index, CancellationToken ct) => throw new InvalidOperationException();
    }

    /// <summary>Only explicit native consent effects; no replacement household algorithm or financial authority.</summary>
    private sealed class Dialogs : IAppInteraction
    {
        internal Func<bool>? Answer;
        public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel) => Task.FromResult(Answer?.Invoke() ?? false);
        public Task AlertAsync(string title, string message, string cancel) => throw new InvalidOperationException();
        public Task<string?> PromptAsync(string title, string message, string accept, string cancel, string? initialValue = null, int maxLength = -1) => throw new InvalidOperationException();
        public Task<string?> ChooseAsync(string title, string cancel, params string[] actions) => throw new InvalidOperationException();
        public Task NavigateAsync(string route, IDictionary<string, object>? parameters = null) => throw new InvalidOperationException();
        public Task ShowFailureAsync(Exception exception) => throw new InvalidOperationException();
    }
}
