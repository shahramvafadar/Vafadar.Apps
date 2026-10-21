using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Features.Accounts;
using Vafadar.Zanance.App.Features.Holdings;

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    /// <summary>Requires the actual unlocked independent Activity before replacing a retained review root.</summary>
    private static async Task AwaitOriginalMoneyWindowAsync(CancellationToken ct)
    {
        if (AppInfo.Current.PackageName != Package || IPlatformApplication.Current!.Services.GetRequiredService<Security.AppLockService>().IsLocked)
            throw new IOException("Unlocked independent monetary caller required.");
        // D-437: a previous cached Home may already be retired; only the owned Activity can authorize initial replacement.
        while (Microsoft.Maui.ApplicationModel.Platform.CurrentActivity is not
            { PackageName: Package, Window.DecorView: { IsShown: true, IsAttachedToWindow: true, HasWindowFocus: true } })
            await Task.Delay(100, ct);
    }

    /// <summary>Restricts new OS scopes to exact completed original fixtures and read-only native work.</summary>
    private sealed record MonetaryOsScope(string Name, int Width)
    {
        private static readonly Dictionary<string, string> Groups = new()
        {
            ["account"] = "review_account_money", ["holding"] = "review_holding_money",
            ["entry"] = "review_entry_money", ["goal-budget"] = "review_goal_budget_money",
            ["secondary-fields"] = "review_secondary_fields", ["secondary-settings"] = "review_secondary_settings",
            ["secondary-units"] = "review_secondary_units",
        };

        /// <summary>Validates the actual independent OS configuration and an unambiguous existing monetary route.</summary>
        internal static MonetaryOsScope? Requested()
        {
            var intent = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.Intent;
            if (intent?.GetBooleanExtra("review_money_os", false) != true) return null;
            var groups = Groups.Where(row => intent.GetBooleanExtra(row.Value, false)).Select(row => row.Key).ToArray();
            var width = intent.GetIntExtra("review_os_width", 0);
            var resources = Android.App.Application.Context.Resources;
            if (AppInfo.Current.PackageName != Package || !intent.GetBooleanExtra("review_plan_resume_actions", false)
                || groups.Length != 1 || width is not (360 or 412)
                || Math.Abs((resources?.Configuration?.FontScale ?? 0) - 2f) > 0.01f
                || resources?.DisplayMetrics is not { Density: > 0 } metrics || Math.Abs(metrics.WidthPixels / metrics.Density - width) > 1
                || new[] { "review_physical_rates",
                    "review_secondary_household", "review_account_currency_only", "review_account_default_only",
                    "review_plan_money_debt_receipt", "review_plan_money_submission_only" }.Any(flag => intent.GetBooleanExtra(flag, false)))
                throw new IOException("One original resumed monetary route and actual OS200% width required.");
            return new(groups[0], width);
        }

        /// <summary>Retains old receipts and refuses a fresh or partially accepted fixture before opening forms.</summary>
        internal void RequireCompleted(string expected, string folder)
        {
            if (Name != expected) throw new IOException("Original OS monetary route differs.");
            string[] files = Name switch
            {
                "account" => ["account-money-latest.json", "account-money-adjust-accepted.json"],
                "holding" => ["holding-money-latest.json", "purchase-money-intention.json", "opening-money-intention.json", "price-money-intention.json"],
                "entry" => ["entry-money-transfer-source.json", "entry-money-expense-source.json", "entry-money-split-source.json",
                    "entry-money-actions-complete.json", "entry-money-adoption-complete.json", "entry-money-repeat-complete.json"],
                "goal-budget" => ["goal-native-accepted.json", "budget-native-accepted.json"],
                "secondary-fields" => ["secondary-fields-goal-accepted.json", "secondary-fields-price-accepted.json",
                    "secondary-fields-override-accepted.json", "secondary-fields-partial-accepted.json", "secondary-fields-bill-accepted.json"],
                "secondary-settings" => ["secondary-settings-accepted.json", "secondary-settings-accepted.marker"],
                "secondary-units" => ["secondary-units-accepted.json", "secondary-units-accepted.marker"],
                _ => throw new IOException("Unknown original monetary scope."),
            };
            // A successful original bill needs no recovery marker; require it only if that recovery actually happened.
            if (Name == "secondary-fields" && File.Exists(Path.Combine(folder, "secondary-fields-bill-observer-recovery.json"))
                && !File.Exists(Path.Combine(folder, "secondary-fields-bill-native-readback.complete")))
                throw new ReviewCheckException("Complete original bill recovery readback required.");
            var missing = files.Where(file => !File.Exists(Path.Combine(folder, file))).ToArray();
            if (missing.Length != 0)
                throw new ReviewCheckException("Complete original accepted receipts required: " + string.Join(", ", missing));
        }

        /// <summary>Allows source reads, known field/factory refusals and draft suggestions, never a new accepted write.</summary>
        internal void RequireReadOnly(Page page, System.Windows.Input.ICommand command, bool refusedFactory = false)
        {
            if (Name == "account" && page.BindingContext is AccountDetailViewModel account && ReferenceEquals(command, account.CompareCommand)) return;
            if (Name == "holding" && page.BindingContext is AssetEventEditorViewModel { SecondAmountText: "not money" } holding
                && ReferenceEquals(command, holding.SaveCommand)) return;
            var changedUnit = Vafadar.Zanance.Core.Money.DisplayUnits.All.SingleOrDefault(row => row.CurrencyCode == "EUR")?.Exponent == 2;
            if (Name == "secondary-fields" && changedUnit && page.BindingContext is Features.Plans.OccurrenceViewModel occurrence
                && ((ReferenceEquals(command, occurrence.PayPartCommand) && occurrence.ActualAmountText == "1,234"
                    && occurrence.PaymentUnitName == "EUR" && occurrence.PaymentFormatNote is not null)
                    || (ReferenceEquals(command, occurrence.SaveChangeCommand) && occurrence.OverrideAmountText == "1,234"
                    && occurrence.OverrideUnitName == "EUR" && occurrence.OverrideFormatNote is not null))) return;
            if (Name == "secondary-fields" && changedUnit && page.BindingContext is Features.Plans.SettlementViewModel
                { ActualText: "1,234", AmountError: not null, HasCommitted: false } bill && ReferenceEquals(command, bill.SaveCommand)) return;
            if (Name == "secondary-settings" && changedUnit && page.BindingContext is Features.Settings.SettingsViewModel
                { EssentialText: "1,234", EssentialError: not null } settings && ReferenceEquals(command, settings.SaveEssentialCommand)) return;
            if (Name == "secondary-units" && page.BindingContext is Features.Rates.DisplayUnitsViewModel units
                && ((refusedFactory && units.Name == "Original unit draft" && ReferenceEquals(command, units.AddCommand))
                    || ReferenceEquals(command, units.UseTomanCommand))) return;
            throw new IOException("OS caption scope cannot invoke an accepted or unrelated command.");
        }

        /// <summary>Checks native final-line height as well as the existing full-text and ellipsis guards.</summary>
        internal static void RequireCompleteHeight(Android.Widget.TextView text, Android.Text.Layout layout)
        {
            if (layout.GetLineBottom(layout.LineCount - 1) > text.Height - text.CompoundPaddingTop - text.CompoundPaddingBottom + 1)
                throw new IOException("Actual OS-scaled monetary caption clips its native last line.");
        }

        /// <summary>Separates new native scenes from prior immutable ordinary-size observations.</summary>
        internal string Prefix => "os200-" + Width + "-";

        /// <summary>Separates whole-scope reports from prior accepted source receipts.</summary>
        internal string Phase => "money-os-" + Name + "-" + Width;
    }
}
