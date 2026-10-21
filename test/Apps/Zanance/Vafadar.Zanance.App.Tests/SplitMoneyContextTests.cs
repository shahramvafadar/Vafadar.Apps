using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Features.Entries;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Money;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-432: actual original split view model with native effects ported to isolated SQLite.</summary>
[Trait("AT", "AT-432")]
[Collection("Entry validation units")]
public sealed class SplitMoneyContextTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static async Task<SplitEditorViewModel> EditorAsync(FlowFixture f, long amount)
    {
        var account = new Account { Name = "Original split money", CurrencyCode = "EUR", OpeningDate = new(2020, 1, 1) };
        await f.Store.SaveAccountAsync(account, Ct); var entry = new LedgerEntry { AccountId = account.Id, Date = new(2026, 10, 9), Kind = EntryKind.Expense, Amount = amount };
        Assert.True((await f.Store.SaveEntryAsync(entry, Ct)).Succeeded);
        f.Localization.SetFormattingCulture("en-US");
        var vm = new SplitEditorViewModel(f.Store, f.Translator, f.Localization, f.Platform);
        await vm.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = entry.Id }); return vm;
    }

    /// <summary>Partial edits cannot acknowledge a new unit merely because their rescaled numbers balance.</summary>
    [Fact]
    public async Task Rebalanced_digits_in_another_unit_require_complete_reentry()
    {
        var units = DisplayUnits.All;
        try
        {
            DisplayUnits.Set([]); using var f = new FlowFixture(); var vm = await EditorAsync(f, 10000);
            vm.Parts[0].AmountText = "60"; vm.Parts[1].AmountText = "40"; Assert.True(vm.CanSave);
            DisplayUnits.Set([new("EUR", "Hundred euros", 2)]); vm.Parts[0].AmountText = "0.6"; vm.Parts[1].AmountText = "0.4";
            Assert.False(vm.CanSave); Assert.Equal(10000, Assert.Single(await f.Store.GetEntriesAsync(cancellationToken: Ct)).Amount);
        }
        finally { DisplayUnits.Set(units); }
    }

    /// <summary>Changing regional separators does not invalidate already typed original shares.</summary>
    [Fact]
    public async Task Original_share_format_survives_a_regional_display_change()
    {
        var units = DisplayUnits.All;
        try
        {
            DisplayUnits.Set([]); using var f = new FlowFixture(); var vm = await EditorAsync(f, 123400);
            vm.Parts[0].AmountText = "1,000"; vm.Parts[1].AmountText = "234"; Assert.True(vm.CanSave);
            f.Localization.SetFormattingCulture("de-DE"); await ((Vafadar.Zanance.App.Presentation.IThemeAware)vm).RefreshThemeAsync();
            Assert.True(vm.CanSave); Assert.Equal("1,000", vm.Parts[0].AmountText);
        }
        finally { DisplayUnits.Set(units); }
    }

    /// <summary>Complete explicit reentry saves exact original total once, including a failed acceptance notification.</summary>
    [Theory][InlineData(false)][InlineData(true)]
    public async Task Complete_reentry_keeps_total_and_consumes_known_acceptance(bool notificationFailure)
    {
        var units = DisplayUnits.All;
        try
        {
            DisplayUnits.Set([]); using var f = new FlowFixture(); var vm = await EditorAsync(f, 10000);
            vm.Parts[0].AmountText = "60"; vm.Parts[1].AmountText = "40"; DisplayUnits.Set([new("EUR", "Hundred euros", 2)]);
            vm.Parts[0].AmountText = string.Empty; vm.Parts[1].AmountText = string.Empty;
            vm.Parts[0].AmountText = "0.6"; vm.Parts[1].AmountText = "0.4"; Assert.True(vm.CanSave);
            var notices = 0; EventHandler observer = (_, _) => { notices++; if (notificationFailure) throw new IOException("Owned split notification failure."); };
            f.Store.Changed += observer;
            try
            {
                await vm.SaveCommand.ExecuteAsync(null); Assert.False(vm.CanSave); Assert.False(vm.IsDirty);
                Assert.Equal(new long[] { 4000, 6000 }, (await f.Store.GetEntriesAsync(cancellationToken: Ct)).Select(row => row.Amount).Order().ToArray());
                var after = await GoalChooserFlowTests.SnapshotAsync(f); await vm.SaveCommand.ExecuteAsync(null);
                Assert.Equal(after, await GoalChooserFlowTests.SnapshotAsync(f)); Assert.Equal(1, notices);
                if (notificationFailure) Assert.Equal(f.Translator["Split_SavedNeedsReview"], vm.Error);
            }
            finally { f.Store.Changed -= observer; }
        }
        finally { DisplayUnits.Set(units); }
    }

    /// <summary>Real SQLite split writers reject changed units, edit-and-return and a departed source owner.</summary>
    [Theory][InlineData("unit")][InlineData("revision")][InlineData("departure")]
    public async Task Original_monetary_writer_rolls_back_without_rearming_departed_sources(string change)
    {
        var units = DisplayUnits.All;
        try
        {
            DisplayUnits.Set([]); using var f = new FlowFixture(); var vm = await EditorAsync(f, 10000);
            vm.Parts[0].AmountText = "60"; vm.Parts[1].AmountText = "40"; var before = await GoalChooserFlowTests.SnapshotAsync(f); var reached = false;
            f.Time.BeforeRead = () =>
            {
                if (!new System.Diagnostics.StackTrace().GetFrames().Any(frame => frame.GetMethod()?.DeclaringType == typeof(Vafadar.Data.Auditing.AuditingSaveChangesInterceptor))) return;
                reached = true; f.Time.BeforeRead = null;
                if (change == "unit") DisplayUnits.Set([new("EUR", "Hundred euros", 2)]);
                else if (change == "departure") vm.RetireReview();
                else { vm.Parts[0].AmountText = "99"; vm.Parts[0].AmountText = "60"; }
            };
            await vm.SaveCommand.ExecuteAsync(null); Assert.True(reached); Assert.False(vm.IsBusy);
            Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f)); Assert.NotNull(vm.Error);
            if (change == "departure") { Assert.False(vm.CanSave); await vm.SaveCommand.ExecuteAsync(null); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f)); }
            else
            {
                if (change == "unit")
                {
                    vm.Parts[0].AmountText = string.Empty; vm.Parts[1].AmountText = string.Empty;
                    vm.Parts[0].AmountText = "0.6"; vm.Parts[1].AmountText = "0.4";
                }
                Assert.True(vm.CanSave); await vm.SaveCommand.ExecuteAsync(null);
                Assert.Equal(10000, (await f.Store.GetEntriesAsync(cancellationToken: Ct)).Sum(row => row.Amount)); Assert.False(vm.CanSave);
            }
        }
        finally { DisplayUnits.Set(units); }
    }

    /// <summary>Explicit Join keeps the original saved total rather than interpreting unused dirty share text.</summary>
    [Fact]
    public async Task Join_uses_original_total_without_interpreting_stale_share_input()
    {
        var units = DisplayUnits.All;
        try
        {
            DisplayUnits.Set([]); using var f = new FlowFixture(); var vm = await EditorAsync(f, 10000);
            vm.Parts[0].AmountText = "60"; vm.Parts[1].AmountText = "40"; await vm.SaveCommand.ExecuteAsync(null);
            var first = (await f.Store.GetEntriesAsync(cancellationToken: Ct)).First();
            var joined = new SplitEditorViewModel(f.Store, f.Translator, f.Localization, f.Platform);
            await joined.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = first.Id });
            DisplayUnits.Set([new("EUR", "Hundred euros", 2)]); joined.Parts[0].AmountText = "999";
            Assert.False(joined.CanSave); Assert.True(joined.CanJoin); await joined.JoinCommand.ExecuteAsync(null);
            Assert.Equal(10000, Assert.Single(await f.Store.GetEntriesAsync(cancellationToken: Ct)).Amount);
            Assert.False(joined.CanJoin); Assert.False(joined.IsDirty);
        }
        finally { DisplayUnits.Set(units); }
    }
}
