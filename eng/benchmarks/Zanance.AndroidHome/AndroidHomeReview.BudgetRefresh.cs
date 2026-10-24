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
using Vafadar.Zanance.Core.Budgets;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    /// <summary>Changes one budget after the original Home read, without creating a ledger transaction.</summary>
    private static async Task<object> ReviewHomeBudgetRefreshAsync(App app, IServiceProvider services,
        string root, string comparison, CancellationToken ct)
    {
        if (AppInfo.Current.PackageName != "pro.vafadar.zanance.homebudgetreview" || Directory.Exists(root)
            || RedirectedAncestors(root).Length != 0)
            throw new ReviewCheckException("Fresh separately owned budget publication source required.");
        if (services.GetRequiredService<AppLockService>() is { IsEnabled: true } or { IsLocked: true })
            throw new ReviewCheckException("The independent budget cohort never configures or bypasses authentication.");
        var profiles = services.GetRequiredService<ProfileService>();
        if (profiles.All.Count != 1)
            throw new ReviewCheckException("Preserve a partial source; never repeat its accepted preparation.");
        Directory.CreateDirectory(root);
        var originalProfile = profiles.Current;
        var original = await ImportFinancialNativeReview.StoredAsync(services, ct);
        await File.WriteAllTextAsync(Path.Combine(root, "base-before.json"), original, ct);
        await File.WriteAllTextAsync(Path.Combine(root, "original-profile.json"), JsonSerializer.Serialize(new { originalProfile.Id, originalProfile.Name }), ct);
        var owned = await profiles.CreateReviewedAsync(profiles.ReadWorkReview(), "Budget refresh " + comparison[..8]);
        await File.WriteAllTextAsync(Path.Combine(root, "owned-profile.json"), JsonSerializer.Serialize(new { owned.Id, owned.Name }), ct);
        if (!await profiles.SwitchToAsync(owned)) throw new ReviewCheckException("Independent budget profile switch refused.");
        var store = services.GetRequiredService<ZananceStore>();
        var today = DateOnly.FromDateTime(services.GetRequiredService<TimeProvider>().GetLocalNow().DateTime);
        var account = new Account { Name = "Original zero cash", CurrencyCode = "EUR", OpeningDate = today, OpeningBalance = 0 };
        if (!await store.SaveAccountAsync(account, ct)) throw new ReviewCheckException("Original zero-opening account refused.");
        await store.UpdateSettingsAsync(settings =>
        {
            settings.OnboardingCompleted = true; settings.DefaultAccountId = account.Id;
            settings.BudgetCalendar = PeriodCalendar.Gregorian; settings.MonthStartDay = 1;
        }, ct);
        var budget = new Budget { Year = today.Year, Month = today.Month, Calendar = PeriodCalendar.Gregorian,
            CurrencyCode = "EUR", TotalLimit = 1000 };
        await store.SaveBudgetAsync(budget, ct);
        await File.WriteAllTextAsync(Path.Combine(root, "setup-accepted.json"), JsonSerializer.Serialize(new
            { AccountId = account.Id, BudgetId = budget.Id, OriginalLimitMinor = 1000, LedgerTransactions = 0 }), ct);
        var page = services.GetRequiredService<HomePage>(); var vm = (HomeViewModel)page.BindingContext;
        var writes = 0; var publications = 0; var coveredAtWrite = false;
        void Changed(object? sender, PropertyChangedEventArgs args)
        {
            // The original callback follows the captured read; the host alone holds publication for this write.
            if (args.PropertyName != nameof(HomeViewModel.QuickAddTargetText) || writes != 0) return;
            coveredAtWrite = vm.Loading.IsLoading && !vm.Loading.IsReady;
            if (!coveredAtWrite) throw new ReviewCheckException("Original Home is not covered at the late budget change.");
            writes++; budget.TotalLimit = 2000;
            File.WriteAllText(Path.Combine(root, "budget-write-started.json"), "{\"LimitMinor\":2000}");
            Task.Run(() => store.SaveBudgetAsync(budget, ct), ct).GetAwaiter().GetResult();
            File.WriteAllText(Path.Combine(root, "budget-write-accepted.json"), "{\"LimitMinor\":2000}");
        }
        void Presented() => publications++;
        vm.PropertyChanged += Changed; vm.PresentingSnapshot += Presented;
        try
        {
            app.OpenFinancialReviewHome(page);
            for (var attempt = 0; attempt < 600
                && !(vm.Loading.IsReady && page.PendingPublication.IsCompletedSuccessfully) && !vm.Loading.HasFailed; attempt++)
                await Task.Delay(50, ct);
            var publication = new { CoveredAtWrite = coveredAtWrite, AcceptedBudgetChanges = writes,
                CompletePublications = publications, vm.Loading.IsReady, vm.Loading.HasFailed,
                PublicationCompleted = page.PendingPublication.IsCompletedSuccessfully };
            await File.WriteAllTextAsync(Path.Combine(root, "publication-observed.json"), JsonSerializer.Serialize(publication), ct);
            // Preserve counters publicly before the terminal guard; a failure must never require writer replay.
            LogReport("home-budget-publication", Path.GetFileName(root), publication);
            if (!vm.Loading.IsReady || !page.PendingPublication.IsCompletedSuccessfully || writes != 1 || publications != 1)
                throw new ReviewCheckException("Original Home budget source did not complete exactly one current publication.");
            await AwaitOriginalMoneyWindowAsync(ct);
            var amount = MoneyText.Format(2000, "EUR", services.GetRequiredService<ILocalizationService>().CurrentCulture);
            var expected = services.GetRequiredService<Translator>().Format("Home_BudgetLeft", amount, amount);
            if (!vm.HasBudget || vm.BudgetText != expected || vm.BudgetProgress != 0
                || (await store.GetEntriesAsync(cancellationToken: ct)).Count != 0)
                throw new ReviewCheckException("Original Home budget is stale or unexpected ledger data exists.");
            var label = PlanEditorNativeReview.Views(page).OfType<Label>().Single(view => view.Text == expected);
            var stored = (await store.GetBudgetsAsync(ct)).Single();
            if (stored.Id != budget.Id || stored.TotalLimit != 2000)
                throw new ReviewCheckException("Original stored budget identity or accepted limit differs.");
            var scroll = page.FindByName<ScrollView>("ContentViewport")
                ?? throw new ReviewCheckException("Original Home viewport missing.");
            // D-455: a laid-out child does not establish parent scroll readiness. Observe both before
            // sending a stock request; never manufacture readiness or enter its pre-layout retry path.
            for (var attempt = 0; attempt < 100; attempt++)
            {
                if (label.Handler?.PlatformView is Android.Widget.TextView
                    { IsLaidOut: true, IsAttachedToWindow: true, HasWindowFocus: true, Height: > 0 }
                    && scroll.Handler?.PlatformView is Android.Views.View
                    { IsLaidOut: true, IsAttachedToWindow: true, HasWindowFocus: true, IsLayoutRequested: false, IsShown: true }) break;
                await Task.Delay(50, ct);
            }
            if (label.Handler?.PlatformView is not Android.Widget.TextView
                { IsLaidOut: true, IsAttachedToWindow: true, HasWindowFocus: true, Height: > 0 } native
                || scroll.Handler?.PlatformView is not Android.Views.View
                { IsLaidOut: true, IsAttachedToWindow: true, HasWindowFocus: true, IsLayoutRequested: false, IsShown: true })
                throw new ReviewCheckException("Original budget viewport has no ready owned native layout; no scroll requested.");
            await scroll.ScrollToAsync(label, ScrollToPosition.Center, false);
            await Task.Delay(150, ct);
            using var visible = new Android.Graphics.Rect();
            if (native.Text != expected || !native.IsShown || !native.GetGlobalVisibleRect(visible) || visible.Height() <= 0)
                throw new ReviewCheckException("The current original budget text is not actually visible.");
            var accepted = new { CoveredAtWrite = coveredAtWrite, AcceptedBudgetChanges = writes,
                CompletePublications = publications, BudgetLimitMinor = 2000, LedgerTransactions = 0,
                NativeBudgetText = native.Text, VisibleWidth = visible.Width(), VisibleHeight = visible.Height() };
            await File.WriteAllTextAsync(Path.Combine(root, "budget-home-accepted.json"), JsonSerializer.Serialize(accepted), ct);
            LogReport("home-budget-visible", Path.GetFileName(root), accepted);
        }
        finally { vm.PropertyChanged -= Changed; vm.PresentingSnapshot -= Presented; }
        if (!await profiles.SwitchToAsync(originalProfile)) throw new ReviewCheckException("Original independent base return refused.");
        await profiles.DeleteAsync(owned); app.ShowCurrentProfile();
        if (profiles.All.Count != 1 || original != await ImportFinancialNativeReview.StoredAsync(services, ct))
            throw new ReviewCheckException("Original independent base changed across budget fixture retirement.");
        return new { Comparison = comparison, Completed = true, CoveredAtWrite = coveredAtWrite,
            AcceptedBudgetChanges = writes, CompletePublications = publications, LedgerTransactions = 0,
            OriginalBaseUnchanged = true, OwnedProfileDeleted = true, BudgetFormAcceptance = false, Device = DeviceEvidence() };
    }
}
