using Vafadar.Zanance.Core.Accounts;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Original loan read and both original projections handle the last supported calendar months without writes.</summary>
public sealed class LoanCalendarBoundaryFlowTests
{
    /// <summary>A private injected clock does not modify the machine clock or repeat a financial operation.</summary>
    [Theory]
    [InlineData(11, 1, "en")]
    [InlineData(11, 1, "fa")]
    [InlineData(11, 1, "de")]
    [InlineData(11, 1, "es")]
    [InlineData(11, 1, "fr")]
    [InlineData(11, 1, "it")]
    [InlineData(12, 0, "en")]
    public async Task Both_original_pages_remain_ready_at_the_calendar_horizon(int month, int expectedRows, string language)
    {
        using var f = new FlowFixture();
        var account = await AccountDetailOriginalBoundaryTests.AccountAsync(f, AccountType.Loan);
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        f.Time.Now = new(9999, month, 30, 9, 0, 0, TimeSpan.Zero);
        f.Localization.SetLanguage(f.Localization.SupportedLanguages.Single(item => item.CultureName == language));
        var schedule = LoanScheduleOriginalBoundaryTests.Create(f);
        LoanScheduleOriginalBoundaryTests.Open(schedule, account.Id);
        await schedule.LoadAsync();
        Assert.True(schedule.Loading.IsReady);
        Assert.Equal(expectedRows, schedule.Rows.Count);
        Assert.Equal(f.Translator["Loan_DateLimit"], schedule.SummaryText);
        Assert.NotEqual(f.Translator["Loan_NeverPaidOff"], schedule.SummaryText);
        Assert.NotEqual("Loan_DateLimit", schedule.SummaryText);
        var detail = AccountDetailOriginalBoundaryTests.Create(f);
        AccountDetailOriginalBoundaryTests.Open(detail, account.Id);
        await detail.LoadAsync();
        Assert.True(detail.Loading.IsReady);
        Assert.True(detail.ShowLoanEstimate);
        Assert.Equal(schedule.SummaryText, detail.LoanPayoffText);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }
}
