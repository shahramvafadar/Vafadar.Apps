#if DEBUG && WINDOWS
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Vafadar.Data;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Goals;
using Vafadar.Zanance.App.Features.Holdings;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Diagnostics;

internal static partial class DebugSnapshots
{
    /// <summary>Invokes original lifecycle actions and native confirmation against a new independently owned goal fixture.</summary>
    private static async Task ReviewGoalIntegrityAsync(App app, IServiceProvider services, string folder, string[] languages)
    {
        var path = Path.Combine(folder, "goal-review-integrity.db");
        if (File.Exists(path)) throw new IOException("A fresh independently owned goal fixture is required.");
        using var fixture = new ServiceCollection().AddZananceData(path).BuildServiceProvider();
        fixture.MigrateLocalDatabase<ZananceDbContext>();
        var factory = fixture.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        var store = fixture.GetRequiredService<ZananceStore>(); var goals = fixture.GetRequiredService<GoalStore>();
        var holdings = fixture.GetRequiredService<HoldingStore>(); var plans = fixture.GetRequiredService<PlanStore>();
        var day = DateOnly.FromDateTime(services.GetRequiredService<TimeProvider>().GetLocalNow().DateTime);
        var account = new Account { Name = "Fictitious original funding", CurrencyCode = "EUR", OpeningDate = day, OpeningBalance = 10000 };
        await store.SaveAccountAsync(account);
        var goal = new Goal { Name = "Fictitious original lifecycle goal", CurrencyCode = "EUR", TargetAmount = 50000 };
        await goals.SaveGoalAsync(goal);
        var localization = services.GetRequiredService<ILocalizationService>(); var translator = services.GetRequiredService<Translator>();
        var interaction = services.GetRequiredService<IAppInteraction>(); var evidence = new List<object>();
        foreach (var language in languages)
        {
            await SetLanguageAsync(app, localization, language);
            var dates = services.GetRequiredService<IDateFormatter>();
            var presenter = new GoalPresenter(translator, dates, localization, holdings, new HoldingText(translator, localization, dates));
            var vm = new GoalDetailViewModel(store, goals, plans, holdings, presenter, translator, dates, localization,
                services.GetRequiredService<TimeProvider>(), interaction);
            vm.ApplyQueryAttributes(new Dictionary<string, object> { ["id"] = goal.Id });
            var page = new GoalDetailPage(vm); await Shell.Current.Navigation.PushAsync(page); await vm.LoadAsync();
            await Task.Delay(150);
            vm.AmountText = MoneyText.ForInput(123, "EUR", localization.CurrentCulture); var draft = vm.AmountText;
            async Task CheckAsync(GoalState expected, string operation)
            {
                await using var db = await factory.CreateDbContextAsync();
                if ((await db.Goals.AsNoTracking().SingleAsync()).State != expected || vm.SaveError is not null
                    || vm.AmountText != draft || await db.Entries.AnyAsync() || await db.GoalAllocations.AnyAsync()
                    || (await db.Accounts.AsNoTracking().SingleAsync()).OpeningBalance != 10000)
                    throw new IOException("Original goal lifecycle, retained draft or unchanged ledger differs.");
                evidence.Add(new { Language = language, Operation = operation, StoredState = expected.ToString(),
                    OriginalNativeCommand = true, RetainedUnsavedAmount = true, NoLedgerPostingOrAllocation = true });
            }
            async Task InvokeAsync(CommunityToolkit.Mvvm.Input.IAsyncRelayCommand command, string operation, GoalState expected, bool confirm = false)
            {
                var button = VisualDescendants(page).OfType<Button>().Single(value => ReferenceEquals(value.Command, command) && Visible(value));
                if (FindScrollView(page) is { } scroll) await ScrollToViewIfNeededAsync(scroll, button);
                InvokeChoiceReview(button); await Task.Delay(100);
                if (confirm)
                {
                    if (app.Windows[0].Handler?.PlatformView is not Microsoft.UI.Xaml.Window { Content.XamlRoot: { } root })
                        throw new IOException("Original own-window dialog root missing.");
                    Microsoft.UI.Xaml.Controls.ContentDialog? dialog = null;
                    for (var attempt = 0; attempt < 100; attempt++)
                    {
                        dialog = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetOpenPopupsForXamlRoot(root)
                            .Select(popup => popup.Child).OfType<Microsoft.UI.Xaml.Controls.ContentDialog>()
                            .SingleOrDefault(value => value.Title as string == translator["Goal_Complete"]);
                        if (dialog is not null) break;
                        await Task.Delay(50);
                    }
                    if (dialog is null || !vm.IsBusy) throw new IOException("Original busy native goal confirmation missing.");
                    await CaptureWindowAsync(app.Windows[0], Path.Combine(folder, language + "-goal-original-confirmation.png"));
                    var answer = FindNativeButton(dialog, translator["Goal_Complete"]) ?? throw new IOException("Original confirmation target missing.");
                    var peer = FrameworkElementAutomationPeer.CreatePeerForElement(answer) as ButtonAutomationPeer;
                    if (peer?.GetPattern(PatternInterface.Invoke) is not IInvokeProvider invoke) throw new IOException("Original confirmation Invoke missing.");
                    invoke.Invoke();
                }
                if (command.ExecutionTask is not { } pending) throw new IOException("Original goal command did not start.");
                await pending; await CheckAsync(expected, operation);
                await CaptureAsync(app, folder, language + "-goal-" + operation);
            }
            await InvokeAsync(vm.ToggleCompleteCommand, "completed", GoalState.Completed, confirm: true);
            await InvokeAsync(vm.ToggleCompleteCommand, "reopened", GoalState.Active);
            await InvokeAsync(vm.TogglePauseCommand, "paused", GoalState.Paused);
            await InvokeAsync(vm.TogglePauseCommand, "resumed", GoalState.Active);
            InvokeSnapshotBack(page); await Task.Delay(150);
        }
        await File.WriteAllTextAsync(Path.Combine(folder, "goal-review-integrity-proof.json"), JsonSerializer.Serialize(new
        { ActualOriginalGoalDetailAndWriter = true, OriginalNativeConfirmationAndCommands = true,
            RetainedReviewedSourceAndUnsavedAmount = true, NoLedgerPostingOrAllocation = true, FictitiousIndependentDatabase = true, Evidence = evidence }));
    }
}
#endif
