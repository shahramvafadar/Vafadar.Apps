using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Entries;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    /// <summary>Compares the unchanged single-entry oracle and original native reimbursement page on fictitious inputs.</summary>
    private static async Task<object> ReviewReimbursementsAsync(App app, IServiceProvider services, string root, string comparison, CancellationToken ct)
    {
        RequireHomeWindowFocus();
        var timings = await Task.Run(() =>
        {
            var observations = new List<object>();
            foreach (var count in new[] { 64, 1024, 8192 })
            {
                ct.ThrowIfCancellationRequested();
                var entries = new List<LedgerEntry>();
                for (var index = 0; index < count; index++)
                {
                    var expense = new LedgerEntry { Kind = EntryKind.Expense, Date = new DateOnly(2026, 10, 1).AddDays(index % 3),
                        Amount = 1000, ReimbursableAmount = 1000, Title = "Fictitious receivable " + index, Tags = ["preserved"] };
                    entries.Add(expense);
                    entries.Add(new LedgerEntry { Kind = EntryKind.Refund, RefundOfId = expense.Id, Amount = 200, Date = expense.Date });
                }
                var before = JsonSerializer.Serialize(entries);
                var clock = Stopwatch.StartNew();
                var original = entries.Where(e => e.Kind == EntryKind.Expense && e.ReimbursableAmount is > 0)
                    .Select(e => (Expense: e, Open: EntryActions.OpenReimbursement(e, entries)))
                    .Where(item => item.Open > 0).OrderBy(item => item.Expense.Date).ToArray();
                clock.Stop(); var originalMs = clock.Elapsed.TotalMilliseconds;
                clock.Restart(); var indexed = EntryActions.OpenReimbursements(entries); clock.Stop();
                if (original.Length != count || indexed.Count != count || !original.SequenceEqual(indexed)
                    || indexed.Any(item => item.Open != 800) || before != JsonSerializer.Serialize(entries))
                    throw new ReviewCheckException("Original reimbursement amounts, order or complete inputs differ.");
                observations.Add(new { Expenses = count, Entries = entries.Count, SingleEntryOracleMilliseconds = originalMs,
                    IndexedMilliseconds = clock.Elapsed.TotalMilliseconds, OriginalAmountsOrderAndObjects = true, CompleteInputsUnchanged = true });
            }
            return observations;
        }, ct);

        app.OpenReviewShell();
        for (var attempt = 0; Shell.Current?.CurrentPage?.Handler?.PlatformView is not Android.Views.View { IsAttachedToWindow: true }; attempt++)
        {
            if (attempt >= 300) throw new ReviewCheckException("Original review Shell did not attach.");
            await Task.Delay(50, ct);
        }
        RequireHomeWindowFocus();
        var path = Path.Combine(root, "reimbursements.db");
        if (File.Exists(path)) throw new ReviewCheckException("Existing reimbursement inputs must not be reseeded.");
        using var fixture = new ServiceCollection().AddZananceData(path).BuildServiceProvider();
        await Task.Run(() => fixture.MigrateLocalDatabase<ZananceDbContext>(), ct);
        var store = fixture.GetRequiredService<ZananceStore>();
        var account = new Account { Name = "Fictitious reimbursement account", CurrencyCode = "EUR", OpeningDate = new DateOnly(2026, 10, 1) };
        await store.SaveAccountAsync(account, ct);
        var expenseRow = new LedgerEntry { AccountId = account.Id, Kind = EntryKind.Expense, Date = account.OpeningDate,
            Amount = 1000, ReimbursableAmount = 1000, Title = "Fictitious original reimbursable expense", Tags = ["retained"] };
        await store.SaveEntryAsync(expenseRow, cancellationToken: ct);
        await store.SaveEntryAsync(EntryActions.CreateReimbursement(expenseRow, 200, account.Id, expenseRow.Date), cancellationToken: ct);
        async Task<string> StoredAsync() => JsonSerializer.Serialize(new { Accounts = await store.GetAccountsAsync(cancellationToken: ct),
            Entries = await store.GetEntriesAsync(cancellationToken: ct), Settings = await store.GetSettingsAsync(ct) });
        var stored = await StoredAsync();
        var translator = services.GetRequiredService<Translator>(); var local = services.GetRequiredService<ILocalizationService>();
        var vm = new ReimbursementsViewModel(store, translator, local, new DateFormatter(local));
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void Published(object? sender, System.ComponentModel.PropertyChangedEventArgs args)
        {
            if (args.PropertyName == nameof(vm.TotalText) && vm.TotalText is not null) ready.TrySetResult();
        }
        vm.PropertyChanged += Published;
        var page = new ReimbursementsPage(vm);
        try
        {
            RequireHomeWindowFocus();
            await Shell.Current.Navigation.PushAsync(page);
            await ready.Task.WaitAsync(TimeSpan.FromSeconds(30), ct);
            if (page.Handler?.PlatformView is not Android.Views.View native) throw new ReviewCheckException("Original native reimbursement page missing.");
            await WaitLayoutAsync(native, ct); RequireHomeWindowFocus();
            var texts = NativeTexts(native).ToHashSet(StringComparer.Ordinal);
            if (vm.Items.Count != 1 || vm.IsEmpty || vm.Items[0].Id != expenseRow.Id || vm.Items[0].Title != expenseRow.Title
                || vm.TotalText != vm.Items[0].AmountText || !texts.Contains(expenseRow.Title) || !texts.Contains(vm.TotalText))
                throw new ReviewCheckException("Original native reimbursement page did not publish the partial refund correctly.");
            if (stored != await StoredAsync()) throw new ReviewCheckException("Read-only reimbursement review changed original stored inputs.");
        }
        finally { vm.PropertyChanged -= Published; await Shell.Current.Navigation.PopAsync(); }
        return new { Comparison = comparison, OriginalWindowFocusRequired = true, Timings = timings,
            OriginalNativeReimbursementPage = true, PartialRefundDisplayed = true, CompleteStoredInputsUnchanged = true,
            IndependentFictitiousFixture = true, MainPackageTouched = false, SystemSettingsChanged = false,
            WholeHomeLatencyQualified = false, LargeNativeListQualified = false };
    }
}
