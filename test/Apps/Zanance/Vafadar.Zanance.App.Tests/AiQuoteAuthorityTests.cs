using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;
namespace Vafadar.Zanance.App.Tests;

/// <summary>Unpublished or failed quote presentation cannot authorize a new primary request.</summary>
public sealed class AiQuoteAuthorityTests
{
    /// <summary>Two failed binding boundaries and a current control preserve all original source rows.</summary>
    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)]
    public async Task Quote_authority_requires_completed_current_presentation(int phase)
    {
        using var f = new FlowFixture(); var ct = TestContext.Current.CancellationToken;
        if (Environment.GetEnvironmentVariable("ZANANCE_D978_HOST_TMP") is { } owned)
            Assert.StartsWith(Path.GetFullPath(owned), Path.GetFullPath(f.Directory.Path), StringComparison.OrdinalIgnoreCase);
        var factory = f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        await using (var db = await factory.CreateDbContextAsync(ct))
        { db.Accounts.Add(new Account { Name = "D978 zero metadata", Type = AccountType.Checking, CurrencyCode = "EUR", OpeningDate = new(2026, 1, 1), OpeningBalance = 0 }); await db.SaveChangesAsync(ct); }
        var prepared = await AiQuoteAuthorityProbes.CreateAsync(factory, f.Services.GetRequiredService<ICommercialWriteAccessSource>(), f.Translator, f.Localization, f.Time, f.Platform);
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        try
        {
            var value = await AiQuoteAuthorityProbes.RunAsync(prepared, phase);
            Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
            Assert.Equal(phase != 2, value.Fired); Assert.Equal(phase == 2, value.CanSend);
            Assert.False(value.Pending); Assert.True(value.CanLeave); Assert.True(value.CanQuote);
            Assert.Equal(1, value.Offers); Assert.Equal(0, value.Primary);
            if (phase != 2) Assert.NotNull(value.Error);
            Assert.Empty(f.Platform.ConfirmationDialogs); Assert.Empty(f.Platform.Alerts); Assert.Empty(f.Platform.Routes);
        }
        finally { prepared.Model.RetireRoot(); }
    }
}
