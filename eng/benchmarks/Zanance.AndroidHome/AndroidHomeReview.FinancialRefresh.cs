using System.ComponentModel;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Diagnostics;
using Vafadar.Zanance.App.Features.Home;
using Vafadar.Zanance.App.Profiles;
using Vafadar.Zanance.App.Security;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    /// <summary>Commits one independent expense after the original Home read, while its real page remains covered.</summary>
    private static async Task<object> ReviewHomeFinancialRefreshAsync(App app, IServiceProvider services,
        string root, string comparison, CancellationToken ct)
    {
        if (Directory.Exists(root) || RedirectedAncestors(root).Length != 0)
            throw new ReviewCheckException("Fresh financial refresh source required; never repeat an accepted writer.");
        if (services.GetRequiredService<AppLockService>() is { IsEnabled: true } or { IsLocked: true })
            throw new ReviewCheckException("This independent cohort neither configures nor bypasses authentication.");
        Directory.CreateDirectory(root);
        var profiles = services.GetRequiredService<ProfileService>();
        if (profiles.All.Any(profile => profile.Name?.StartsWith("Financial refresh ", StringComparison.Ordinal) == true))
            throw new ReviewCheckException("Preserve the original partial fixture instead of reseeding it.");
        var originalProfile = profiles.Current;
        var originalCount = profiles.All.Count;
        var original = await ImportFinancialNativeReview.StoredAsync(services, ct);
        var owned = await profiles.CreateReviewedAsync(profiles.ReadWorkReview(), "Financial refresh " + comparison[..8]);
        await File.WriteAllTextAsync(Path.Combine(root, "owned-profile.json"), JsonSerializer.Serialize(new { owned.Id, owned.Name }), ct);
        if (!await profiles.SwitchToAsync(owned)) throw new ReviewCheckException("Original independent profile switch refused.");
        var store = services.GetRequiredService<ZananceStore>();
        var today = DateOnly.FromDateTime(services.GetRequiredService<TimeProvider>().GetLocalNow().DateTime);
        if ((await store.GetAccountsAsync(cancellationToken: ct)).Count != 0 || (await store.GetEntriesAsync(cancellationToken: ct)).Count != 0)
            throw new ReviewCheckException("Independent source is not empty; no financial replay.");
        var account = new Account { Name = "Original concurrent cash", CurrencyCode = "EUR", OpeningDate = today, OpeningBalance = 10000 };
        if (!await store.SaveAccountAsync(account, ct)) throw new ReviewCheckException("Original fixture account refused.");
        await store.UpdateSettingsAsync(settings => { settings.OnboardingCompleted = true; settings.DefaultAccountId = account.Id; }, ct);
        var page = services.GetRequiredService<HomePage>();
        var vm = (HomeViewModel)page.BindingContext;
        var writes = 0; var publications = 0; var coveredAtWrite = false;
        void Changed(object? sender, PropertyChangedEventArgs args)
        {
            // This actual view-model notification follows GetHomeSnapshotAsync and precedes financial projection.
            // The independent host alone pauses that callback; no timing hook enters production source.
            if (args.PropertyName != nameof(HomeViewModel.QuickAddTargetText) || writes != 0) return;
            coveredAtWrite = vm.Loading.IsLoading && !vm.Loading.IsReady;
            if (!coveredAtWrite) throw new ReviewCheckException("Original Home cover is not active at the source change.");
            writes++;
            File.WriteAllText(Path.Combine(root, "expense-started.json"), "{\"ExpenseMinor\":1000}");
            var saved = Task.Run(() => store.SaveEntryAsync(new LedgerEntry
                { AccountId = account.Id, Date = today, Kind = EntryKind.Expense, Amount = 1000, Title = "Concurrent original ten" }, ct), ct)
                .GetAwaiter().GetResult();
            if (!saved.Succeeded) throw new ReviewCheckException("Original concurrent fixture expense refused.");
            File.WriteAllText(Path.Combine(root, "expense-accepted.json"), "{\"ExpenseMinor\":1000}");
        }
        void Presented() => publications++;
        vm.PropertyChanged += Changed; vm.PresentingSnapshot += Presented;
        try
        {
            app.OpenFinancialReviewHome(page);
            for (var attempt = 0; attempt < 600
                && !(vm.Loading.IsReady && page.PendingPublication.IsCompletedSuccessfully) && !vm.Loading.HasFailed; attempt++)
                await Task.Delay(50, ct);
            await File.WriteAllTextAsync(Path.Combine(root, "publication-observed.json"), JsonSerializer.Serialize(new
                { CoveredAtWrite = coveredAtWrite, AcceptedWriters = writes, CompletePublications = publications,
                    vm.Loading.IsReady, vm.Loading.HasFailed, PublicationCompleted = page.PendingPublication.IsCompletedSuccessfully }), ct);
            if (!vm.Loading.IsReady || !page.PendingPublication.IsCompletedSuccessfully || writes != 1 || publications != 1)
                throw new ReviewCheckException("The original covered Home did not publish exactly once after its accepted writer.");
            await AwaitOriginalMoneyWindowAsync(ct);
            var expected = MoneyText.Format(9000, "EUR", services.GetRequiredService<ILocalizationService>().CurrentCulture);
            if (vm.Balances.Single().Text != expected || (await store.GetEntriesAsync(cancellationToken: ct)).Count != 1)
                throw new ReviewCheckException("Original Home published stale financial facts or replayed the writer.");
            var labels = PlanEditorNativeReview.Views(page).OfType<Label>().Where(label => label.Text == expected).ToArray();
            if (!labels.Any(label => label.Handler?.PlatformView is Android.Widget.TextView
                { IsShown: true, HasWindowFocus: true, Height: > 0 } native && native.Text == expected))
                throw new ReviewCheckException("The current original financial total is not natively visible.");
            await File.WriteAllTextAsync(Path.Combine(root, "home-current-accepted.json"), JsonSerializer.Serialize(new
                { CoveredAtWrite = coveredAtWrite, AcceptedWriters = writes, CompletePublications = publications, CurrentBalanceMinor = 9000 }), ct);
        }
        finally { vm.PropertyChanged -= Changed; vm.PresentingSnapshot -= Presented; }
        if (!await profiles.SwitchToAsync(originalProfile)) throw new ReviewCheckException("Original base-profile return refused.");
        await profiles.DeleteAsync(owned); app.ShowCurrentProfile();
        if (profiles.All.Count != originalCount || original != await ImportFinancialNativeReview.StoredAsync(services, ct))
            throw new ReviewCheckException("Original independent base changed across financial fixture retirement.");
        return new { Comparison = comparison, Completed = true, CoveredAtWrite = coveredAtWrite, AcceptedWriters = writes,
            CompletePublications = publications, CurrentBalanceMinor = 9000, OriginalBaseUnchanged = true,
            OwnedProfileDeleted = true, NativeFinancialFormAcceptance = false, Device = DeviceEvidence() };
    }

    /// <summary>Audits and retires an accepted original financial writer without repeating its preparation or write.</summary>
    private static async Task<object> ResumeHomeFinancialRefreshAsync(App app, IServiceProvider services,
        string root, string comparison, CancellationToken ct)
    {
        if (!Directory.Exists(root) || RedirectedAncestors(root).Length != 0
            || !File.Exists(Path.Combine(root, "expense-started.json"))
            || !File.Exists(Path.Combine(root, "expense-accepted.json"))
            || !Directory.EnumerateFiles(root, "failure-*.json").Any()
            || File.Exists(Path.Combine(root, "resume-retired.json")))
            throw new ReviewCheckException("An accepted failed original financial fixture is required; no preparation replay.");
        using var owner = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(root, "owned-profile.json"), ct));
        var ownedId = owner.RootElement.GetProperty("Id").GetString()
            ?? throw new ReviewCheckException("The original profile identity is absent.");
        var profiles = services.GetRequiredService<ProfileService>();
        var owned = profiles.All.Single(profile => profile.Id == ownedId);
        if (profiles.All.Count != 2 || profiles.Current.Id != ownedId)
            throw new ReviewCheckException("Original current fictional source or its unique base changed; no cleanup guess.");
        var original = profiles.All.Single(profile => profile.Id != ownedId);
        var store = services.GetRequiredService<ZananceStore>();
        var account = (await store.GetAccountsAsync(cancellationToken: ct)).Single();
        var entry = (await store.GetEntriesAsync(cancellationToken: ct)).Single();
        if (account.Name != "Original concurrent cash" || account.CurrencyCode != "EUR" || account.OpeningBalance != 10000
            || entry.AccountId != account.Id || entry.Kind != EntryKind.Expense || entry.Amount != 1000
            || entry.Title != "Concurrent original ten" || entry.Date != account.OpeningDate)
            throw new ReviewCheckException("Accepted original financial source changed; never replay the writer.");
        var packet = await store.GetHomeSnapshotAsync(account.OpeningDate, 0, ct);
        await store.ValidateHomeSnapshotAsync(packet, ct);
        if (LedgerCalculator.Balance(account, packet.Entries, account.OpeningDate) != 9000)
            throw new ReviewCheckException("The same accepted source no longer has its original current balance.");
        await File.WriteAllTextAsync(Path.Combine(root, "retained-source-accepted.json"), JsonSerializer.Serialize(new
            { AccountId = account.Id, EntryId = entry.Id, AcceptedWriters = 1, ReplayedWriters = 0, CurrentBalanceMinor = 9000 }), ct);
        if (!await profiles.SwitchToAsync(original)) throw new ReviewCheckException("Original unique base-profile return refused.");
        await profiles.DeleteAsync(owned); app.ShowCurrentProfile();
        if (profiles.All.Count != 1 || profiles.Current.Id != original.Id)
            throw new ReviewCheckException("Owned accepted fixture retirement incomplete.");
        await File.WriteAllTextAsync(Path.Combine(root, "resume-retired.json"), "{\"Completed\":true}", ct);
        return new { Comparison = comparison, Completed = true, RetainedAcceptedWriters = 1, ReplayedWriters = 0,
            CurrentBalanceMinor = 9000, OwnedProfileDeleted = true, OriginalPublicationGuardFailureRetained = true,
            InitialPublicationCountUnaccepted = true, ReadOnlyContinuation = true, Device = DeviceEvidence() };
    }
}
