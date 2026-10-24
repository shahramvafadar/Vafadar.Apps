using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.DataFiles;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.DataFiles;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-516: consent belongs to one selected original format/history and is retired on source switching.</summary>
[Trait("AT", "AT-516")]
[Collection("Entry validation units")]
public sealed class SupaCsvConsentTests
{
    private sealed class Files : IDataFileInteraction
    {
        internal string Text = SupaCsvFlowTests.Fixture();
        public Task<PickedDataFile?> PickCsvAsync(string title) => Task.FromResult<PickedDataFile?>(null);
        public Task<PickedDataFile?> PickBankCsvAsync(string title, BankCsvEncoding encoding) =>
            Task.FromResult<PickedDataFile?>(new("fictitious-original-bank.csv", Text));
        public Task<bool> ConfirmExportAsync(string reason) => throw new InvalidOperationException("No export belongs to source consent.");
        public Task ShareCsvAsync(string name, string title, string text) => throw new InvalidOperationException("No sharing belongs to source consent.");
    }

    [Theory]
    [InlineData("en")][InlineData("fa")][InlineData("de")][InlineData("es")][InlineData("fr")][InlineData("it")]
    public async Task Source_history_consent_is_format_specific_in_every_language_and_a_new_preset_needs_fresh_consent(string language)
    {
        var ct = TestContext.Current.CancellationToken;
        using var fixture = new FlowFixture();
        fixture.Localization.SetLanguage(fixture.Localization.SupportedLanguages.Single(item => item.CultureName == language));
        await fixture.Store.EnsureDefaultCategoriesAsync(ct);
        await fixture.Store.SaveAccountAsync(new Account { Name = "Original consent target", CurrencyCode = "EUR", OpeningDate = new(2026, 1, 1) }, ct);
        var files = new Files();
        var model = new ImportExportViewModel(fixture.Store, fixture.Translator, new DateFormatter(fixture.Localization), fixture.Localization,
            fixture.Time, fixture.Services.GetRequiredService<HoldingStore>(), files, fixture.Platform);
        await model.LoadAsync(); var notices = new List<string?>(); model.PropertyChanged += (_, args) => notices.Add(args.PropertyName);
        await model.PickSupaCommand.ExecuteAsync(null);
        Assert.True(model.HasPreview); Assert.False(model.BankAcknowledged); Assert.False(model.CanRunImport);
        Assert.Equal(fixture.Translator["SupaCsv_Acknowledge"], model.BankPresetAcknowledgement);
        Assert.NotEqual(fixture.Translator["BankCsv_Acknowledge"], model.BankPresetAcknowledgement);
        Assert.Contains(nameof(model.BankPresetAcknowledgement), notices);
        model.BankAcknowledged = true; Assert.True(model.CanRunImport);
        files.Text = BankCsvFlowTests.Fixture(); notices.Clear(); await model.PickStarMoneyCommand.ExecuteAsync(null);
        Assert.True(model.HasPreview); Assert.False(model.BankAcknowledged); Assert.False(model.CanRunImport);
        Assert.Equal(fixture.Translator["BankCsv_Acknowledge"], model.BankPresetAcknowledgement);
        Assert.Equal(fixture.Translator["BankCsv_Warning"], model.BankPresetWarning);
        Assert.Contains(nameof(model.BankPresetAcknowledgement), notices);
        Assert.Empty(await fixture.Store.GetEntriesAsync(cancellationToken: ct));
    }
}
