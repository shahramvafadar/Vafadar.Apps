using System.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Core.Settings;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Sharing;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.Core.Sharing;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Sharing;
using Vafadar.Zanance.Data.Sync;
using Vafadar.Zanance.ProtectedSync;

/// <summary>Original application editor source over the same real protected role/cost state; only native dialogs/lifetime effects are ports.</summary>
internal static class CostApplicationQualification
{
    internal static async Task RunAsync(string root, ProtectedClient client, LocalSyncPeer peer, Action<string> proof, CancellationToken ct)
    {
        const string marker = "original-app-cost-editor-preserves-live-language-drafts-clears-changed-currency-rejects-partial-sum-and-saves-once-on-native-household";
        if (File.ReadLines(Path.Combine(root, "checkpoints.txt")).Contains(marker)) throw new IOException("Do not repeat a completed original app cost save.");
        var store = client.Services.GetRequiredService<HouseholdCostStore>(); var before = await store.ReadAsync(ct);
        if (!before.CanWrite || before.Totals.Single().NetCost != 1001) throw new InvalidDataException("The retained exact cost fixture is required.");
        var original = before.Costs.Single(row => row.Kind == HouseholdCostKind.Expense);
        var translator = new Translator(); translator.AddResources(new ResourceManager("HouseholdRoundtrip.Resources.AppResources", typeof(CostApplicationQualification).Assembly));
        var localization = new LocalizationService(new LocalizationOptions(), new InMemorySettingsStore(), translator);
        localization.SetLanguage(localization.SupportedLanguages.Single(row => row.CultureName == "en"));
        void Validate() { ct.ThrowIfCancellationRequested(); if (File.ReadAllText(Path.Combine(root, "client", "client.marker")) != "Fictitious protected sync client v1") throw new IOException(); }
        var environment = new ForegroundQualification.NativeEffects(peer, client, ct, Validate); var dialogs = new OriginalDialogs();
        var vm = new HouseholdCostsViewModel(store, environment, dialogs, localization, new DateFormatter(localization), translator, TimeProvider.System);
        if (vm.IsReady || vm.CanWrite) throw new InvalidDataException("The cost editor was not covered during construction.");
        await vm.ActivateAsync();
        if (!vm.IsReady || !vm.CanWrite || vm.Error is not null) throw new InvalidDataException("The original cost snapshot did not publish.");
        await vm.EditCostCommand.ExecuteAsync(vm.Costs.Single(row => row.Source.Id == original.Id));
        var amount = vm.AmountText; var shares = vm.Shares.Select(row => row.Text).ToArray();
        foreach (var language in new[] { "fa", "de" })
        {
            localization.SetLanguage(localization.SupportedLanguages.Single(row => row.CultureName == language));
            if (vm.AmountText != amount || !vm.Shares.Select(row => row.Text).SequenceEqual(shares))
                throw new InvalidDataException("Live translation reinterpreted the numeric cost draft.");
        }
        vm.CurrencyCode = "EUR";
        if (vm.AmountText.Length != 0 || vm.Shares.Any(row => row.Text.Length != 0)) throw new InvalidDataException("Currency change relabelled retained amounts.");
        vm.CancelEditCommand.Execute(null);
        await vm.EditCostCommand.ExecuteAsync(vm.Costs.Single(row => row.Source.Id == original.Id));
        vm.Shares[0].Text = "0"; await vm.SaveCommand.ExecuteAsync(null);
        if (vm.SharesError is null || dialogs.Confirmed != 0 || (await store.ReadAsync(ct)).Costs.Single(row => row.Id == original.Id).Amount != original.Amount)
            throw new InvalidDataException("An incomplete actual share sum reached a native save.");
        vm.CancelEditCommand.Execute(null);
        await vm.EditCostCommand.ExecuteAsync(vm.Costs.Single(row => row.Source.Id == original.Id));
        vm.Title += " reviewed in original app"; dialogs.AcceptNext = true; await vm.SaveCommand.ExecuteAsync(null);
        var after = await store.ReadAsync(ct);
        if (vm.Error is not null || vm.HasEditor || dialogs.Confirmed != 1 || environment.Applied != 1 || environment.ReadableOpens != 0 ||
            after.Costs.Single(row => row.Id == original.Id).Title != vm.Costs.Single(row => row.Source.Id == original.Id).Source.Title ||
            after.Totals.Single().NetCost != 1001 || after.Costs.Count != before.Costs.Count || after.Allocations.Count != before.Allocations.Count)
            throw new InvalidDataException("The original app cost save or exact native publication failed.");
        if ((await store.FindMovementsAsync(original.Date, EntryKind.Expense, null, ct)).Count != 0)
            throw new InvalidDataException("A cost editor manufactured a cash movement.");
        vm.Deactivate();
        if (vm.CanInteract || vm.AmountText.Length != 0 || vm.Shares.Count != 0 || vm.Costs.Count != 0)
            throw new InvalidDataException("A retired cost page retained its original financial draft.");
        proof(marker);
    }

    internal static async Task ColdAsync(ProtectedClient client, Action<string> proof, CancellationToken ct)
    {
        var snapshot = await client.Services.GetRequiredService<HouseholdCostStore>().ReadAsync(ct);
        if (snapshot.Contacts.Count != 2 || snapshot.Costs.Count != 2 || snapshot.Allocations.Count != 4 || snapshot.Settlements.Count != 1 ||
            snapshot.Totals.Single().NetCost != 1001 || !snapshot.CanWrite ||
            snapshot.Members.Single(row => snapshot.Contacts.Single(contact => contact.Id == row.ContactId).Name == "Fictitious cost payer").PrivateAdvanceBalance != 200)
            throw new InvalidDataException("The same cold native household cost state did not survive.");
        await using var db = await client.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(ct);
        if (await db.Entries.AnyAsync(ct)) throw new InvalidDataException("A private cost created a cash posting.");
        proof("cold-same-native-household-cost-keys-roles-exact-shares-refund-and-private-advance-pass");
    }

    /// <summary>Explicit one-use fictional confirmation, never an external/native credential or replacement financial algorithm.</summary>
    private sealed class OriginalDialogs : IAppInteraction
    {
        internal bool AcceptNext;
        internal int Confirmed;
        public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel)
        { if (!AcceptNext) throw new InvalidDataException("An unexpected original app confirmation occurred."); AcceptNext = false; Confirmed++; return Task.FromResult(true); }
        public Task AlertAsync(string title, string message, string cancel) => throw new InvalidOperationException();
        public Task<string?> PromptAsync(string title, string message, string accept, string cancel, string? initialValue = null, int maxLength = -1) => throw new InvalidOperationException();
        public Task<string?> ChooseAsync(string title, string cancel, params string[] actions) => throw new InvalidOperationException();
        public Task NavigateAsync(string route, IDictionary<string, object>? parameters = null) => throw new InvalidOperationException();
        public Task ShowFailureAsync(Exception exception) => throw new InvalidOperationException();
    }
}
