using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Original collection and default-reset notifications cannot revive retired loads or overwrite newer inputs.</summary>
public sealed class AiLoadPublicationTests
{
    /// <summary>Seven collection/ready boundaries, retained selection, real-file defaults and two reentrant reset boundaries preserve source.</summary>
    [Theory]
    [InlineData(0)][InlineData(1)][InlineData(2)][InlineData(3)][InlineData(4)][InlineData(5)]
    [InlineData(6)][InlineData(7)][InlineData(8)][InlineData(9)][InlineData(10)]
    public async Task Ai_load_publication_preserves_current_owner_selection_and_file_reset(int phase)
    {
        var ct = TestContext.Current.CancellationToken;
        using var first = new FlowFixture(); using var second = new FlowFixture();
        if (Environment.GetEnvironmentVariable("ZANANCE_D963_HOST_TMP") is { } owned)
        {
            Assert.StartsWith(Path.GetFullPath(owned), Path.GetFullPath(first.Directory.Path), StringComparison.OrdinalIgnoreCase);
            Assert.StartsWith(Path.GetFullPath(owned), Path.GetFullPath(second.Directory.Path), StringComparison.OrdinalIgnoreCase);
        }
        var factories = new[] { first, second }.Select(f => f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>()).ToArray();
        foreach (var factory in factories)
        {
            await using var db = await factory.CreateDbContextAsync(ct);
            for (var i = 0; i < 2; i++) db.Accounts.Add(new Account { Name = "D963 metadata " + i, Type = AccountType.Checking,
                CurrencyCode = "EUR", OpeningDate = new(2026, 1, 1), OpeningBalance = 0 });
            await db.SaveChangesAsync(ct);
        }
        var prepared = await AiLoadPublicationProbes.CreateAsync(phase, factories[0], factories[1],
            first.Services.GetRequiredService<ICommercialWriteAccessSource>(), first.Translator, first.Localization, first.Time, first.Platform);
        var beforeFirst = await GoalChooserFlowTests.SnapshotAsync(first); var beforeSecond = await GoalChooserFlowTests.SnapshotAsync(second);
        try
        {
            var observed = await AiLoadPublicationProbes.RunAsync(prepared, phase);
            Assert.Equal(beforeFirst, await GoalChooserFlowTests.SnapshotAsync(first)); Assert.Equal(beforeSecond, await GoalChooserFlowTests.SnapshotAsync(second));
            Assert.False(observed.Pending); Assert.True(observed.CanLeave); Assert.False(observed.CanShare);
            Assert.Equal(1, observed.SourceCalls); Assert.Equal(1, observed.Captures); Assert.Equal(1, observed.Validations); Assert.Equal(0, observed.Effects);
            if (phase is 7 or 8)
            {
                Assert.False(observed.Fired); Assert.True(observed.Loaded); Assert.Equal(2, observed.Accounts); Assert.Null(observed.Error);
                Assert.Equal(phase == 7 ? new[] { true, false } : new[] { false, false }, observed.Selected);
                Assert.Equal(phase == 7 ? new DateOnly(2026, 8, 5) : new DateOnly(2026, 10, 1), observed.From);
                Assert.Equal(phase == 7 ? new DateOnly(2026, 9, 6) : new DateOnly(2026, 10, 9), observed.To);
                Assert.Equal(phase == 8, observed.ConfirmedOnly); Assert.Equal(phase == 7, observed.ComparePrevious);
            }
            else
            {
                Assert.True(observed.Fired); Assert.False(observed.Loaded);
                if (phase < 6) Assert.Equal(phase % 2, observed.Accounts);
                if (phase is 2 or 3) Assert.Equal(new(2026, 7, 7), observed.From);
                if (phase == 9) Assert.Equal(new(2026, 12, 12), observed.To);
                if (phase is 4 or 5) Assert.NotNull(observed.Error);
            }
            Assert.Empty(first.Platform.Alerts); Assert.Empty(first.Platform.ConfirmationDialogs); Assert.Empty(first.Platform.Routes);
        }
        finally { prepared.Model.Retire(); }
    }
}
