using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;
namespace Vafadar.Zanance.App.Tests;

/// <summary>A failed new quote command must not revive previously published terms.</summary>
public sealed class AiQuoteEntryAuthorityTests
{
    /// <summary>Changing and changed busy callbacks refuse before a second offer and preserve the entire source.</summary>
    [Theory]
    [InlineData(0)] [InlineData(1)]
    public async Task Earlier_quote_is_revoked_before_fallible_entry_notification(int phase)
    {
        using var f = new FlowFixture(); var ct = TestContext.Current.CancellationToken;
        var owned = Environment.GetEnvironmentVariable("ZANANCE_D979_HOST_TMP");
        if (owned is not null) Assert.StartsWith(Path.GetFullPath(owned), Path.GetFullPath(f.Directory.Path), StringComparison.OrdinalIgnoreCase);
        var factory = f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        await using (var db = await factory.CreateDbContextAsync(ct))
        { db.Accounts.Add(new Account { Name = "D979 zero metadata", Type = AccountType.Checking, CurrencyCode = "EUR", OpeningDate = new(2026, 1, 1), OpeningBalance = 0 }); await db.SaveChangesAsync(ct); }
        var prepared = await AiQuoteAuthorityProbes.CreateAsync(factory, f.Services.GetRequiredService<ICommercialWriteAccessSource>(), f.Translator, f.Localization, f.Time, f.Platform);
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        try
        {
            var value = await AiQuoteAuthorityProbes.RunEntryAsync(prepared, phase);
            Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
            Assert.True(value.Fired); Assert.False(value.CanSend); Assert.True(value.CanQuote);
            Assert.False(value.Pending); Assert.True(value.CanLeave); Assert.NotNull(value.Error);
            Assert.Equal(1, value.Offers); Assert.Equal(0, value.Primary);
            Assert.Empty(f.Platform.ConfirmationDialogs); Assert.Empty(f.Platform.Alerts); Assert.Empty(f.Platform.Routes);
        }
        finally { prepared.Model.RetireRoot(); }
    }
}
