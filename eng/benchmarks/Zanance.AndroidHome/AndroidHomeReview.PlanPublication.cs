using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization;
using Vafadar.Zanance.App.Features.Plans;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    /// <summary>Separates original protected list data, binding and native realization after the first actual editor Save.</summary>
    private static async Task<object> ReviewFirstPlanPublicationAsync(App app, IServiceProvider services, string root, string comparison, CancellationToken ct, bool retainedFixture = false)
    {
        var statePath = Path.Combine(root, "first-plan-intent.json");
        var store = services.GetRequiredService<ZananceStore>(); var plans = services.GetRequiredService<PlanStore>();
        var accounts = await store.GetAccountsAsync(cancellationToken: ct);
        var existingSchedules = await plans.GetSchedulesAsync(ct);
        if ((await store.GetEntriesAsync(cancellationToken: ct)).Count != 0)
            throw new ReviewCheckException("Only the independent unpaid fixture may exercise this transition.");
        if (retainedFixture)
        {
            if (!File.Exists(statePath) || accounts.Count != 1 || accounts[0].Name != "D318 independent first-plan account"
                || accounts[0].OpeningBalance != 100000 || existingSchedules.Count != 1
                || existingSchedules[0].Name != "D318 independent first plan" || existingSchedules[0].Amount != 300
                || existingSchedules[0].AutoPost || existingSchedules[0].ReminderEnabled)
                throw new ReviewCheckException("Preserve the original independent account, keys and unpaid plan fixture.");
            using var intent = JsonDocument.Parse(await File.ReadAllTextAsync(statePath, ct));
            if (intent.RootElement.GetProperty("AccountId").GetGuid() != accounts[0].Id)
                throw new ReviewCheckException("The original account identity changed.");
            // Retain the original unpaid definition before a new empty-to-first-row transition on the same database.
            await using (var receipt = new FileStream(Path.Combine(root, $"plan-retirement-{comparison}.json"), FileMode.CreateNew))
                await JsonSerializer.SerializeAsync(receipt, existingSchedules[0], cancellationToken: ct);
            if (!await plans.DeleteScheduleAsync(existingSchedules[0].Id, ct))
                throw new ReviewCheckException("The original unpaid definition could not be retired safely.");
        }
        else
        {
            if (File.Exists(statePath) || accounts.Count != 0 || existingSchedules.Count != 0)
                throw new ReviewCheckException("Preserve the original first-plan intent; do not recreate its transition.");
            var today = DateOnly.FromDateTime(services.GetRequiredService<TimeProvider>().GetLocalNow().DateTime);
            var account = new Account { Name = "D318 independent first-plan account", Type = AccountType.Checking,
                CurrencyCode = "EUR", OpeningDate = today, OpeningBalance = 100000 };
            await File.WriteAllTextAsync(statePath, JsonSerializer.Serialize(new { AccountId = account.Id,
                AccountName = account.Name, PlanName = "D318 independent first plan", FinancialWrites = 0 }), ct);
            await store.SaveAccountAsync(account, ct);
        }
        var originalAccounts = JsonSerializer.Serialize(await store.GetAccountsAsync(cancellationToken: ct));
        var originalSettings = JsonSerializer.Serialize(await store.GetSettingsAsync(ct));
        var shell = app.OpenReviewShell();
        await UntilAsync(() => shell.Handler?.PlatformView is Android.Views.View { IsAttachedToWindow: true }, ct);
        await shell.GoToAsync("//plans");
        await UntilAsync(() => shell.CurrentPage is PlansPage { BindingContext: PlansViewModel { HasNoPlans: true } }, ct);
        var page = (PlansPage)shell.CurrentPage; var vm = (PlansViewModel)page.BindingContext;
        await UntilAsync(() => page.Handler?.PlatformView is Android.Views.View { IsAttachedToWindow: true }, ct);
        var translator = services.GetRequiredService<Translator>();
        await UntilAsync(() => NativeTexts((Android.Views.View)page.Handler!.PlatformView!).Contains(translator["Plans_Empty"]), ct);
        if (vm.Rows.Count != 0) throw new ReviewCheckException("Actual first plan list must start empty.");
        await vm.AddCommand.ExecuteAsync(null);
        PlanEditorPage? editor = null;
        await UntilAsync(() =>
        {
            editor = shell.Navigation.ModalStack.OfType<PlanEditorPage>().LastOrDefault() ?? shell.CurrentPage as PlanEditorPage;
            return editor?.BindingContext is PlanEditorViewModel { CanInteract: true };
        }, ct);
        var draft = (PlanEditorViewModel)editor!.BindingContext;
        draft.Name = "D318 independent first plan"; draft.AmountText = "3";
        if (draft.AutoPost || draft.ReminderEnabled) throw new ReviewCheckException("No notification or automatic payment is authorized.");
        await UntilAsync(() => draft.CanSave, ct);
        var save = Views(editor).OfType<Button>().Single(button => ReferenceEquals(button.Command, draft.SaveCommand));
        if (save.Handler?.PlatformView is not Android.Views.View nativeSave) throw new ReviewCheckException("Original native Save target missing.");
        await WaitLayoutAsync(nativeSave, ct);
        var samples = new List<object>(); var clock = Stopwatch.StartNew();
        if (!nativeSave.PerformClick()) throw new ReviewCheckException("Original native Save refused.");
        if (draft.SaveCommand.ExecutionTask is not { } execution) throw new ReviewCheckException("Original Save did not begin.");
        await execution.WaitAsync(ct);
        await UntilAsync(() => ReferenceEquals(shell.CurrentPage, page), ct);
        var viewport = page.FindByName<CollectionView>("ContentViewport");
        var realized = false;
        for (var attempt = 0; attempt < 50; attempt++)
        {
            await Task.Delay(100, ct);
            var native = page.Handler?.PlatformView as Android.Views.View;
            realized = native is not null && NativeTexts(native).Contains(draft.Name);
            var recycler = viewport.Handler?.PlatformView as AndroidX.RecyclerView.Widget.RecyclerView;
            samples.Add(new { ElapsedMs = clock.Elapsed.TotalMilliseconds, ViewModelRows = vm.Rows.Count,
                ViewModelHasNoPlans = vm.HasNoPlans, ItemsSourceRows = (viewport.ItemsSource as System.Collections.ICollection)?.Count,
                SameItemsSource = ReferenceEquals(viewport.ItemsSource, vm.Rows), NativeItems = recycler?.GetAdapter()?.ItemCount,
                NativeChildren = recycler?.ChildCount, NativeNameRealized = realized, Width = recycler?.Width, Height = recycler?.Height });
            if (realized) break;
        }
        var schedules = await plans.GetSchedulesAsync(ct);
        if (schedules.Count != 1 || schedules[0].Name != draft.Name || schedules[0].Amount != 300
            || schedules[0].AutoPost || schedules[0].ReminderEnabled || (await store.GetEntriesAsync(cancellationToken: ct)).Count != 0
            || originalAccounts != JsonSerializer.Serialize(await store.GetAccountsAsync(cancellationToken: ct))
            || originalSettings != JsonSerializer.Serialize(await store.GetSettingsAsync(ct)))
            throw new ReviewCheckException("First-plan diagnostic changed original financial/settings packets.");
        if (retainedFixture && !realized) throw new ReviewCheckException("The first plan is still absent from the actual native view.");
        return new { RetainedAccountAndFiles = retainedFixture, Comparison = comparison, Device = DeviceEvidence(), OriginalEditorSaveInvocations = 1,
            StoredSchedules = schedules.Count, StoredEntries = 0, ViewModelRows = vm.Rows.Count, NativeNameRealized = realized,
            SameItemsSource = ReferenceEquals(viewport.ItemsSource, vm.Rows), Samples = samples,
            OriginalAccountsAndSettingsPreserved = true, MainPackageTouched = false,
            Boundary = "Independent original production protected installation/page/editor; observation distinguishes data, binding and native realization, not a cause inferred from missing accessibility alone." };

        static async Task UntilAsync(Func<bool> ready, CancellationToken cancellation)
        {
            for (var attempt = 0; attempt < 400; attempt++) { if (ready()) return; await Task.Delay(50, cancellation); }
            throw new ReviewCheckException("Original native plan boundary did not complete; preserve the same fixture/process.");
        }
        static IEnumerable<VisualElement> Views(IVisualTreeElement element)
        {
            if (element is VisualElement view) yield return view;
            foreach (var child in element.GetVisualChildren()) foreach (var descendant in Views(child)) yield return descendant;
        }
    }
}
