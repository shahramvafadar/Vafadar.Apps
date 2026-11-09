using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Analysis;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;
/// <summary>Language callbacks cannot publish after retirement, rewrite immutable history or erase newer completed previews.</summary>
public sealed class AiLanguagePublicationTests
{
    /// <summary>Thirteen new publication/history/notification/scheduler contracts preserve the complete source.</summary>
    [Theory]
    [InlineData(0)][InlineData(1)][InlineData(2)][InlineData(3)][InlineData(4)][InlineData(5)][InlineData(6)]
    [InlineData(7)][InlineData(8)][InlineData(9)][InlineData(10)][InlineData(11)][InlineData(12)]
    public async Task Ai_language_publication_preserves_current_owner_history_and_newer_preview(int phase)
    {
        var ct = TestContext.Current.CancellationToken; using var fixture = new FlowFixture();
        if (Environment.GetEnvironmentVariable("ZANANCE_D965_HOST_TMP") is { } owned)
            Assert.StartsWith(Path.GetFullPath(owned), Path.GetFullPath(fixture.Directory.Path), StringComparison.OrdinalIgnoreCase);
        var factory = fixture.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        if (phase != 2)
        {
            await using var db = await factory.CreateDbContextAsync(ct);
            db.Accounts.Add(new Account { Name = "D965 metadata only", Type = AccountType.Checking, CurrencyCode = "EUR", OpeningDate = new(2026, 1, 1), OpeningBalance = 0 });
            await db.SaveChangesAsync(ct);
        }
        var p = await AiLanguagePublicationProbes.CreateAsync(phase, factory, fixture.Services.GetRequiredService<ICommercialWriteAccessSource>(), fixture.Translator, fixture.Localization, fixture.Time, fixture.Platform);
        var before = await GoalChooserFlowTests.SnapshotAsync(fixture);
        try
        {
            var v = await AiLanguagePublicationProbes.RunAsync(p, phase);
            Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(fixture)); Assert.Null(v.ExceptionType);
            Assert.False(v.Pending); Assert.True(v.CanLeave); Assert.Equal(0, v.Effects); Assert.Equal(0, p.Runtime.Remote.Calls);
            if (phase >= 11)
            {
                Assert.True(v.Fired); Assert.True(v.CanShare); Assert.NotNull(v.Summary); Assert.NotNull(v.Disclosure);
                var payload = JsonSerializer.Deserialize(v.Disclosure!, AiJson.Default.AiPayload)!;
                Assert.Equal(phase == 11 ? "en" : "de", payload.Language);
                Assert.Equal(phase == 11 ? p.OriginalFrom.AddDays(1) : p.OriginalFrom, payload.Current.From);
                Assert.Equal(payload.Current.From, v.From); Assert.Equal(1, v.SourceCalls); Assert.Equal(1, v.Captures); Assert.Equal(2, v.Validations);
            }
            else
            {
                Assert.Equal(0, v.SourceCalls); Assert.Equal(0, v.Captures); Assert.Equal(0, v.Validations); Assert.False(v.CanShare);
                Assert.Equal(0, v.LatePublications); Assert.Equal(0, v.FrozenNotifications); Assert.True(v.HistoricalValuesExact);
                if (phase <= 4 || phase == 7) { Assert.True(v.Fired); Assert.False(v.Loaded); }
                else
                {
                    Assert.True(v.Loaded); Assert.Equal(phase is 6 or 10, v.Fired);
                    if (phase != 10)
                    {
                        Assert.Equal(phase == 6 ? "it" : "de", v.Language);
                        Assert.Equal(fixture.Translator["Ai_ChooseAccounts"], v.AccountsError);
                        Assert.Equal(fixture.Translator["Ai_InvalidRange"], v.FromError); Assert.Equal(fixture.Translator["Ai_InvalidRange"], v.ToError);
                    }
                    if (phase is 5 or 9)
                    { Assert.Equal(fixture.Translator.Format("Ai_ResultUnits", "3"), v.ResultStatus); Assert.Equal(p.OriginalSnapshot, v.Snapshot); Assert.Equal(p.OriginalText, v.ResultText); }
                }
            }
            Assert.Empty(fixture.Platform.Alerts); Assert.Empty(fixture.Platform.ConfirmationDialogs); Assert.Empty(fixture.Platform.Routes);
        }
        finally { p.Model.Retire(); }
    }
}
