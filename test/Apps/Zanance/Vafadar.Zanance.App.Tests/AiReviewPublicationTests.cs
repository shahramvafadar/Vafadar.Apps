using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Analysis;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Incomplete, changed or retired review publication must not retain sharing authority or stale disclosure.</summary>
public sealed class AiReviewPublicationTests
{
    /// <summary>Five late refusal boundaries, a complete comparison and a same-failed-model Retry preserve the original source.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public async Task Ai_review_publication_commits_only_a_complete_current_preview(int phase)
    {
        var ct = TestContext.Current.CancellationToken;
        using var fixture = new FlowFixture();
        if (Environment.GetEnvironmentVariable("ZANANCE_D962_HOST_TMP") is { } ownedRoot)
            Assert.StartsWith(Path.GetFullPath(ownedRoot), Path.GetFullPath(fixture.Directory.Path), StringComparison.OrdinalIgnoreCase);
        var factory = fixture.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        await using (var db = await factory.CreateDbContextAsync(ct))
        {
            db.Accounts.Add(new Account { Name = "D962 new metadata only", Type = AccountType.Checking, CurrencyCode = "EUR", OpeningDate = new(2026, 1, 1), OpeningBalance = 0 });
            await db.SaveChangesAsync(ct);
        }
        var prepared = await AiReviewPublicationProbes.CreateAsync(factory,
            fixture.Services.GetRequiredService<ICommercialWriteAccessSource>(), fixture.Translator, fixture.Localization, fixture.Time, fixture.Platform);
        var before = await GoalChooserFlowTests.SnapshotAsync(fixture);
        try
        {
            var observed = await AiReviewPublicationProbes.RunAsync(prepared, phase == 6 ? 0 : phase);
            Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(fixture));
            Assert.False(observed.Pending); Assert.True(observed.CanLeave);
            Assert.Equal(1, observed.SourceCalls); Assert.Equal(1, observed.Captures); Assert.Equal(2, observed.Validations);
            Assert.Equal(0, observed.NativeOrProviderEffects);
            if (phase != 5)
            {
                Assert.True(observed.Fired); Assert.False(observed.CanShare);
                Assert.Null(observed.Disclosure); Assert.Null(observed.Summary);
                if (phase is 0 or 3 or 6) Assert.NotNull(observed.Error);
                if (phase == 1) Assert.Equal(observed.OriginalFrom.AddDays(1), observed.CurrentFrom);
                if (phase is 2 or 4) Assert.False(observed.Loaded);
            }
            else
            {
                Assert.True(observed.CanShare); Assert.NotNull(observed.Summary); Assert.Null(observed.Error);
                var payload = JsonSerializer.Deserialize(observed.Disclosure!, AiJson.Default.AiPayload)!;
                Assert.Equal(AiPurpose.PeriodComparison, payload.Purpose); Assert.NotNull(payload.Previous);
                Assert.Equal(0, payload.Current.Totals[0].Result); Assert.Equal(0, payload.Previous.Totals[0].Result);
            }
            if (phase == 6)
            {
                var retry = await AiReviewPublicationProbes.RetryAsync(prepared);
                Assert.True(retry.CanShare); Assert.NotNull(retry.Disclosure); Assert.NotNull(retry.Summary); Assert.Null(retry.Error);
                Assert.False(retry.Pending); Assert.True(retry.CanLeave); Assert.Equal(2, retry.SourceCalls);
                Assert.Equal(0, retry.NativeOrProviderEffects); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(fixture));
            }
            Assert.Empty(fixture.Platform.ConfirmationDialogs); Assert.Empty(fixture.Platform.Routes); Assert.Empty(fixture.Platform.Alerts);
        }
        finally { prepared.Model.Retire(); }
    }
}
