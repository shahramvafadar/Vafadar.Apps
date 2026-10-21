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
                || new[] { "review_physical_rates", "review_secondary_settings", "review_secondary_fields", "review_secondary_units",
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
                _ => throw new IOException("Unknown original monetary scope."),
            };
            if (files.Any(file => !File.Exists(Path.Combine(folder, file))))
                throw new IOException("Complete original accepted receipts required; never seed or replay a writer.");
        }

        /// <summary>Allows only source reads and the known invalid purchase field refusal, never a new accepted write.</summary>
        internal void RequireReadOnly(Page page, System.Windows.Input.ICommand command)
        {
            if (Name == "account" && page.BindingContext is AccountDetailViewModel account && ReferenceEquals(command, account.CompareCommand)) return;
            if (Name == "holding" && page.BindingContext is AssetEventEditorViewModel { SecondAmountText: "not money" } holding
                && ReferenceEquals(command, holding.SaveCommand)) return;
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
