using System.Text.Json;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Zanance.App.Features.Plans;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    /// <summary>Continues only the three untested callers on an existing original fictitious editor source.</summary>
    private static async Task<object> ReviewValidationLayoutAsync(App app, IServiceProvider services, string root, string comparison, CancellationToken ct)
    {
        var file = Path.Combine(root, "work-editor-text.db");
        if (!File.Exists(file) || !File.Exists(Path.Combine(root, "work-editor-text-prepared.marker")))
            throw new ReviewCheckException("Retained original work editor source required; no preparation or seed.");
        await using var fixture = new ServiceCollection().AddZananceData(file).BuildServiceProvider();
        var store = fixture.GetRequiredService<ZananceStore>();
        var plans = fixture.GetRequiredService<PlanStore>();
        var original = services.GetRequiredService<ZananceStore>();
        async Task<string> StoredAsync(ZananceStore current, PlanStore schedules, GoalStore goals) => JsonSerializer.Serialize(new
        {
            Accounts = await current.GetAccountsAsync(cancellationToken: ct),
            Entries = await current.GetEntriesAsync(cancellationToken: ct),
            Plans = await schedules.GetSchedulesAsync(ct), Settings = await current.GetSettingsAsync(ct),
            Goals = await goals.GetGoalsAsync(ct), Budgets = await current.GetBudgetsAsync(ct)
        });
        var sourceGoals = fixture.GetRequiredService<GoalStore>();
        var basePlans = services.GetRequiredService<PlanStore>();
        var baseGoals = services.GetRequiredService<GoalStore>();
        var source = await StoredAsync(store, plans, sourceGoals);
        var baseBefore = await StoredAsync(original, basePlans, baseGoals);
        var digest = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(source)));
        var schedule = (await plans.GetSchedulesAsync(ct)).Single();
        app.OpenReviewShell(); await AwaitRootHomeAsync(ct);
        var cases = new List<object>();
        var nativeActionsThisPass = 0;
        async Task Ready(Func<bool> predicate)
        {
            for (var i = 0; i < 200; i++) { if (predicate()) return; await Task.Delay(50, ct); }
            throw new ReviewCheckException("Original validation caller did not reach its required native state.");
        }
        foreach (var kind in new[] { "plan", "occurrence", "settlement" })
        {
            var receipt = Path.Combine(root, "validation-layout-" + kind + "-accepted.json");
            if (File.Exists(receipt))
            {
                using var prior = JsonDocument.Parse(await File.ReadAllTextAsync(receipt, ct));
                var accepted = prior.RootElement;
                if (accepted.GetProperty("Kind").GetString() != kind || accepted.GetProperty("InputHash").GetString() != digest
                    || accepted.GetProperty("InvalidAmount").GetString() != "#" || !accepted.GetProperty("NativeAction").GetBoolean()
                    || accepted.GetProperty("AcceptedFinancialSaves").GetInt32() != 0 || !accepted.GetProperty("StoredFieldsEqual").GetBoolean())
                    throw new ReviewCheckException("Completed validation source differs; never replay.");
                cases.Add(new { Kind = kind, Retained = true, Original = accepted.Clone() }); continue;
            }
            ContentPage page; Func<ICommand> command; Func<string?> error; Func<bool> invalid;
            if (kind == "plan")
            {
                var vm = ActivatorUtilities.CreateInstance<PlanEditorViewModel>(services, store, plans);
                await vm.ApplyQueryAsync(new Dictionary<string, object>());
                vm.Name = "Fictitious retained validation draft"; vm.AmountText = "#";
                page = new PlanEditorPage(vm); command = () => vm.SaveCommand; error = () => vm.AmountError;
                invalid = () => vm.AmountText == "#";
            }
            else if (kind == "occurrence")
            {
                var commands = ActivatorUtilities.CreateInstance<OccurrenceCommands>(services, plans);
                var vm = ActivatorUtilities.CreateInstance<OccurrenceViewModel>(services, store, plans, commands);
                vm.ApplyQueryAttributes(new Dictionary<string, object> { ["plan"] = schedule.Id, ["date"] = schedule.Rule.Start });
                page = new OccurrencePage(vm); command = () => vm.PayPartCommand; error = () => vm.PaymentAmountError;
                invalid = () => vm.ActualAmountText == "#";
            }
            else
            {
                var vm = ActivatorUtilities.CreateInstance<SettlementViewModel>(services, store);
                await vm.LoadAsync(schedule.Id).WaitAsync(ct);
                vm.ActualText = "#";
                page = new SettlementPage(vm); command = () => vm.SaveCommand; error = () => vm.PeriodError ?? vm.AmountError;
                invalid = () => vm.ActualText == "#";
            }
            await Shell.Current.Navigation.PushModalAsync(page, false);
            if (page is OccurrencePage occurrence)
            {
                await occurrence.PendingPublication.WaitAsync(ct);
                ((OccurrenceViewModel)occurrence.BindingContext).ActualAmountText = "#";
            }
            // The original plan appearance retires the pre-appearance command and publishes a new current one.
            await Ready(() => ReimbursementViews(page).OfType<Button>().Count(b => ReferenceEquals(b.Command, command())) == 1);
            var button = ReimbursementViews(page).OfType<Button>().Single(b => ReferenceEquals(b.Command, command()));
            await Ready(() => button.Handler?.PlatformView is Android.Widget.Button { IsAttachedToWindow: true, IsShown: true, HasWindowFocus: true, Enabled: true });
            // Persistent Save actions reserve a separate row outside the scroll viewport.
            if (page.FindByName<ScrollView>("ValidationViewport") is { } viewport && ReimbursementViews(viewport).Contains(button))
                await viewport.ScrollToAsync(button, ScrollToPosition.MakeVisible, false);
            if (!invalid() || button.Handler?.PlatformView is not Android.Widget.Button native || native.Context?.PackageName != Package || !native.PerformClick())
                throw new ReviewCheckException("Original invalid validation action was not invoked.");
            nativeActionsThisPass++;
            await Ready(() => error() is not null);
            await Ready(() => ReimbursementViews(page).OfType<Label>().Any(label =>
            {
                if (label.Text != error() || label.Handler?.PlatformView is not Android.Widget.TextView { IsAttachedToWindow: true, IsShown: true } text)
                    return false;
                using var bounds = new Android.Graphics.Rect();
                return text.GetGlobalVisibleRect(bounds);
            }));
            if (source != await StoredAsync(store, plans, sourceGoals) || baseBefore != await StoredAsync(original, basePlans, baseGoals))
                throw new ReviewCheckException("Validation modified original source or base data.");
            var result = new { Kind = kind, InputHash = digest, InvalidAmount = "#", NativeAction = true, Error = error(), AcceptedFinancialSaves = 0, StoredFieldsEqual = true };
            await File.WriteAllTextAsync(receipt, JsonSerializer.Serialize(result), ct);
            cases.Add(result);
            await Shell.Current.Navigation.PopModalAsync(false);
        }
        return new { Comparison = comparison, Cases = cases, Completed = 3, NativeActionsThisPass = nativeActionsThisPass,
            AcceptedFinancialSaves = 0, StoredFieldsEqual = true, NoGeneralStabilityClaim = true };
    }
}
