using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization;
using Vafadar.Maui.Localization;
using Vafadar.Zanance.App.Features.Accounts;
using Vafadar.Zanance.App.Features.Home;
using Vafadar.Zanance.App.Features.Transactions;
using Vafadar.Zanance.App.Presentation;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App;

internal static partial class AndroidHomeReview
{
    /// <summary>Checks original ledger/account hosts, complete packet colors and actual selection without financial writes.</summary>
    private static async Task<object> ReviewLedgerRowPagesAsync(App app, IServiceProvider services, IServiceProvider fixture,
        ILocalizationService local, Translator translator, Func<ContentPage, string, string, string, Task<int>> check, CancellationToken ct)
    {
        var store = fixture.GetRequiredService<ZananceStore>();
        var negative = MoneyText.Format(long.MinValue, "EUR", local.CurrentCulture);
        var positive = MoneyText.Format(long.MaxValue, "EUR", local.CurrentCulture);
        var multiple = positive + " · " + MoneyText.Format(long.MinValue, "USD", local.CurrentCulture);
        var home = ActivatorUtilities.CreateInstance<HomeViewModel>(services, store);
        var homePage = new HomePage(home).WithFlowDirection(local);
        await Shell.Current.Navigation.PushAsync(homePage); await homePage.PendingPublication;
        var originalRecent = home.RecentEntries.First();
        var recent = originalRecent with { AmountText = negative };
        home.RecentEntries[0] = recent;
        await Task.Delay(250, ct);
        var homeEntries = await check(homePage, recent.Title, negative, negative);
        CheckTarget(homePage, recent, recent.Title, negative, home.OpenEntryCommand);
        CheckColor(homePage, negative, recent.AmountColor);
        home.RecentEntries[0] = originalRecent;
        var account = home.Accounts.First() with { BalanceText = negative, IsNegative = true };
        home.Accounts[0] = account; homePage.PreviewAccountSectionForReview(true);
        await Task.Delay(250, ct);
        var homeAccounts = await check(homePage, account.Name, negative, negative);
        CheckTarget(homePage, account, account.Name, negative, null);
        CheckColor(homePage, negative, Palette.ExpenseText);
        await Shell.Current.Navigation.PopAsync();

        var transactions = ActivatorUtilities.CreateInstance<TransactionsViewModel>(services, store);
        var transactionsPage = new TransactionsPage(transactions).WithFlowDirection(local);
        await Shell.Current.Navigation.PushAsync(transactionsPage); await transactions.LoadAsync();
        var originalDay = transactions.Days.First();
        var entry = originalDay.First() with { AmountText = positive };
        var day = new EntryDayGroup(originalDay.Header, multiple, new[] { entry });
        transactions.Days = new[] { day };
        await Task.Delay(300, ct);
        var transactionAmounts = await check(transactionsPage, entry.Title, positive, multiple);
        var entryButton = CheckTarget(transactionsPage, entry, entry.Title, positive, transactions.OpenCommand);
        CheckColor(transactionsPage, positive, entry.AmountColor);
        CheckColor(transactionsPage, multiple, Palette.SecondaryText);
        transactions.StartSelectingCommand.Execute(null);
        if (entryButton.Handler?.PlatformView is not Android.Views.View nativeAction || !nativeAction.PerformClick())
            throw new ReviewCheckException("Original native selection action refused.");
        await Task.Delay(100, ct);
        var selectedRow = LedgerViews(transactionsPage).OfType<Grid>().Single(row => ReferenceEquals(row.BindingContext, entry)
            && row.Children.OfType<Button>().Contains(entryButton));
        if (!transactions.HasSelection || !entry.Selection.IsSelected || selectedRow.BackgroundColor != Palette.PrimarySoft)
            throw new ReviewCheckException("Original selected transaction feedback changed.");
        if (!nativeAction.PerformClick()) throw new ReviewCheckException("Original native deselection action refused.");
        await Task.Delay(100, ct); transactions.StopSelectingCommand.Execute(null);
        if (transactions.IsSelecting || transactions.HasSelection || entry.Selection.IsSelected)
            throw new ReviewCheckException("Original transaction selection did not clear.");
        await Shell.Current.Navigation.PopAsync();

        var accounts = ActivatorUtilities.CreateInstance<AccountsViewModel>(services, store, fixture.GetRequiredService<HoldingStore>());
        var accountsPage = new AccountsPage(accounts).WithFlowDirection(local);
        await Shell.Current.Navigation.PushAsync(accountsPage);
        for (var wait = 0; accounts.Groups.Count == 0 && wait < 300; wait++) await Task.Delay(50, ct);
        var originalGroup = accounts.Groups.First();
        var item = originalGroup.Items.First() with { BalanceText = positive, IsNegative = false };
        var group = originalGroup with { Items = new[] { item }, TotalsText = multiple };
        accounts.Groups.Clear(); accounts.Groups.Add(group);
        await Task.Delay(250, ct);
        var accountAmounts = await check(accountsPage, item.Name, positive, multiple);
        CheckTarget(accountsPage, item, item.Name, positive, null);
        CheckColor(accountsPage, positive, Palette.AmountText);
        CheckColor(accountsPage, multiple, Palette.SecondaryText);
        await Shell.Current.Navigation.PopAsync();
        if (homeEntries != 1 || homeAccounts != 1 || transactionAmounts != 2 || accountAmounts != 2)
            throw new ReviewCheckException("All six original monetary row/header viewports are required.");
        return new { OriginalHomeTransactionsAndAccountsHosts = true, CompleteMonetaryViewports = 6,
            OriginalCommandsAndTargets = true, ActualNativeAccessibleNames = true,
            OriginalNativeColors = true, ActualSelectionAndDeselection = true, FinancialCommandsInvoked = false };

        static Button CheckTarget(ContentPage page, object model, string title, string packet, System.Windows.Input.ICommand? command)
        {
            var row = LedgerViews(page).OfType<Grid>().Single(value => ReferenceEquals(value.BindingContext, model)
                && value.Children.OfType<Button>().Any());
            var button = row.Children.OfType<Button>().Single();
            if (button.Width < 44 || button.Height < 44 || SemanticProperties.GetDescription(button) != title
                || SemanticProperties.GetHint(button) != packet || command is not null &&
                (!ReferenceEquals(button.Command, command) || !ReferenceEquals(button.CommandParameter, model))
                || button.Handler?.PlatformView is not Android.Views.View native)
                throw new ReviewCheckException("Original ledger/account target, command or accessible identity changed.");
            using var node = native.CreateAccessibilityNodeInfo();
            var name = node?.ContentDescription?.ToString() ?? node?.Text?.ToString() ?? string.Empty;
            if (!name.Contains(title, StringComparison.Ordinal)) throw new ReviewCheckException("Original native row name is incomplete.");
            return button;
        }
        static void CheckColor(ContentPage page, string packet, Color expected)
        {
            var caption = LedgerViews(page).OfType<Label>().Single(value => value.Text == packet);
            if (caption.Handler?.PlatformView is not Android.Widget.TextView text)
                throw new ReviewCheckException("Original native amount caption is unavailable.");
            var actual = new Android.Graphics.Color(text.CurrentTextColor);
            if (Math.Abs(actual.R - expected.Red * 255) > 1 || Math.Abs(actual.G - expected.Green * 255) > 1
                || Math.Abs(actual.B - expected.Blue * 255) > 1 || Math.Abs(actual.A - expected.Alpha * 255) > 1)
                throw new ReviewCheckException("Original native financial semantic color changed.");
        }
    }

    /// <summary>Enumerates only original visual descendants belonging to the current independent review page.</summary>
    private static IEnumerable<View> LedgerViews(IVisualTreeElement node)
    {
        if (node is View view) yield return view;
        foreach (var child in node.GetVisualChildren()) foreach (var descendant in LedgerViews(child)) yield return descendant;
    }
}
