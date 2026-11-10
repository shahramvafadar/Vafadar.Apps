using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;
namespace Vafadar.Zanance.App.Tests;

/// <summary>Last-boundary owner changes cannot turn original manual consent into a stale recipient handoff.</summary>
public sealed class AiManualHandoffTests
{
    /// <summary>Three owners at each actual callback boundary, plus one current refusal-port control, preserve original source.</summary>
    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)]
    [InlineData(5)] [InlineData(6)] [InlineData(7)] [InlineData(8)] [InlineData(9)]
    public async Task Manual_handoff_rechecks_original_owner_after_last_external_callback(int phase)
    {
        var ct = TestContext.Current.CancellationToken; using var f = new FlowFixture();
        if (Environment.GetEnvironmentVariable("ZANANCE_D975_HOST_TMP") is { } ownedRoot)
            Assert.StartsWith(Path.GetFullPath(ownedRoot), Path.GetFullPath(f.Directory.Path), StringComparison.OrdinalIgnoreCase);
        var factory = f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        await using (var db = await factory.CreateDbContextAsync(ct))
        { db.Accounts.Add(new Account { Name = "D975 zero metadata", Type = AccountType.Checking, CurrencyCode = "EUR", OpeningDate = new(2026, 1, 1), OpeningBalance = 0 }); await db.SaveChangesAsync(ct); }
        var prepared = await AiManualHandoffProbes.CreateAsync(factory, f.Services.GetRequiredService<ICommercialWriteAccessSource>(), f.Translator, f.Localization, f.Time);
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        try
        {
            var v = await AiManualHandoffProbes.RunAsync(prepared, phase);
            Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
            Assert.Equal(phase != 9, v.Fired); Assert.False(v.Pending); Assert.True(v.CanLeave); Assert.Equal(0, v.Opens);
            Assert.Equal(phase == 9 ? 1 : 0, v.Shares);
            Assert.Equal(phase is >= 6 and <= 8 ? 0 : 1, v.Confirmations);
            Assert.Equal(phase is >= 6 and <= 8 ? 0 : 1, v.Authorizations);
            if (phase < 9 && phase % 3 == 1) Assert.Equal(prepared.OriginalFrom.AddDays(1), v.From);
            if (phase < 9 && phase % 3 == 2) Assert.Equal("de", v.Language);
        }
        finally { prepared.Model.RetireRoot(); }
    }
}
