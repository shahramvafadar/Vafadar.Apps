using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.DataFiles;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.DataFiles;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-297: original bank-source review accepts the full permitted metadata without posting money.</summary>
[Trait("AT", "AT-297")]
[Collection("Entry validation units")]
public sealed class StarMoneyCsvBoundaryFlowTests
{
    private sealed class Source(string text) : IDataFileInteraction
    {
        public Task<PickedDataFile?> PickCsvAsync(string title) => Task.FromResult<PickedDataFile?>(null);
        public Task<PickedDataFile?> PickBankCsvAsync(string title, BankCsvEncoding encoding) =>
            Task.FromResult<PickedDataFile?>(new("fictitious-original-bank-boundary.csv", text));
        public Task<bool> ConfirmExportAsync(string reason) => throw new InvalidOperationException("No export is part of source review.");
        public Task ShareCsvAsync(string name, string title, string text) => throw new InvalidOperationException("No sharing is part of source review.");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Original_source_review_retains_the_complete_permitted_comment_without_financial_save(bool quoted)
    {
        using var fixture = new FlowFixture();
        var ct = TestContext.Current.CancellationToken;
        await fixture.Store.EnsureDefaultCategoriesAsync(ct);
        await fixture.Store.SaveAccountAsync(new Account
        {
            Name = "Fictitious bounded original bank target", Type = AccountType.Checking,
            CurrencyCode = "EUR", OpeningDate = new(2026, 1, 1),
        }, ct);
        var original = new string(quoted ? ';' : 'x', 4096);
        var files = new Source(BankCsvFlowTests.Fixture(count: 1, changeColumn: "Kommentar", changeValue: original));
        var model = new ImportExportViewModel(fixture.Store, fixture.Translator, new DateFormatter(fixture.Localization),
            fixture.Localization, fixture.Time, fixture.Services.GetRequiredService<HoldingStore>(), files, fixture.Platform);
        await model.LoadAsync();
        Assert.Empty(await fixture.Store.GetEntriesAsync(cancellationToken: ct));
        await model.PickStarMoneyCommand.ExecuteAsync(null);
        Assert.Null(model.ImportError);
        Assert.True(model.IsBankPreset);
        Assert.True(model.HasPreview);
        Assert.Contains(original, Assert.Single(model.BankLines).Detail);
        Assert.False(model.BankAcknowledged);
        Assert.False(model.CanRunImport);
        Assert.Empty(await fixture.Store.GetEntriesAsync(cancellationToken: ct));
    }
}
