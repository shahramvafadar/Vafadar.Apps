using Vafadar.Zanance.Core.DataFiles;

namespace Vafadar.Zanance.App.Tests;

public sealed partial class DataFileOutcomeFlowTests
{
    [Theory, Trait("AT", "AT-387")]
    [InlineData("\"12\"34")]
    [InlineData("\"12\" 34")]
    [InlineData("12\"34")]
    public async Task Misplaced_amount_quotes_cannot_replace_the_original_complete_review(string amount)
    {
        using var fixture = new FlowFixture(); var files = new Files(); await LedgerAsync(fixture, files);
        var (_, _, _, vm) = Create(fixture, new(), files); await vm.LoadAsync(); await vm.PickCommand.ExecuteAsync(null);
        var draft = Draft(vm); var before = await GoalChooserFlowTests.SnapshotAsync(fixture);
        var original = files.Picked!; Assert.Contains(",12.34,", original.Text);
        files.Picked = new("fictitious-misplaced-quotes.csv", original.Text.Replace(",12.34,", "," + amount + ",", StringComparison.Ordinal));
        await vm.PickCommand.ExecuteAsync(null);
        Assert.Equal(draft, Draft(vm)); Assert.True(vm.CanRunImport); Assert.False(vm.IsBusy);
        Assert.Equal(fixture.Translator["Import_ReadMalformed"], vm.ImportError);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(fixture));
    }
}
