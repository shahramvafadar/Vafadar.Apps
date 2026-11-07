using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.App.Features.Plans;
using Vafadar.Zanance.Core.Accounts;

namespace Vafadar.Zanance.App.Tests;

/// <summary>A retained bill must not silently acquire another account or currency during its explicit retry.</summary>
public sealed class SettlementRetainedTargetTests
{
    [Theory] [InlineData(false)] [InlineData(true)]
    public async Task Retry_preserves_raw_input_but_refuses_a_changed_original_account_or_currency(bool currencyOnly)
    {
        using var f = new FlowFixture(); var ct = TestContext.Current.CancellationToken;
        var id = await SettlementOwnershipSnapshotTests.SeedAsync(f);
        var model = new SettlementViewModel(f.Store, f.Translator, f.Localization, f.Time, f.Platform);
        await model.LoadAsync(id); Assert.True(model.CanEdit); model.ActualText = "2.34";
        await using (var db = await OccurrenceOriginFailureTests.Factory(f).CreateDbContextAsync(ct))
        {
            if (currencyOnly) await db.Accounts.ExecuteUpdateAsync(a => a.SetProperty(x => x.CurrencyCode, "USD"), ct);
            else
            {
                var next = new Account { Name = "Distinct fictitious retained target", Type = AccountType.Checking, CurrencyCode = "EUR",
                    OpeningDate = OccurrenceOriginFailureTests.Day, OpeningBalance = 0 };
                db.Accounts.Add(next); await db.SaveChangesAsync(ct);
                await db.Schedules.Where(s => s.Id == id).ExecuteUpdateAsync(s => s.SetProperty(x => x.AccountId, next.Id), ct);
            }
        }
        var expected = await GoalChooserFlowTests.SnapshotAsync(f);
        await model.ReloadCommand.ExecuteAsync(null);
        Assert.Equal(expected, await GoalChooserFlowTests.SnapshotAsync(f));
        Assert.Equal("2.34", model.ActualText); Assert.False(model.HasCommitted);
        Assert.False(model.CanEdit); Assert.True(model.Loading.HasFailed);
    }
}
