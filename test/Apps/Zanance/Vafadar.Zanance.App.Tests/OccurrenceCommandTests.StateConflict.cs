using Vafadar.Zanance.App.Features.Plans;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Plans;

namespace Vafadar.Zanance.App.Tests;

public sealed partial class OccurrenceCommandTests
{
    [Theory]
    [InlineData("skip"), InlineData("change"), InlineData("unskip"), InlineData("link")]
    public async Task A_retained_correction_cannot_replace_a_settlement_accepted_after_the_form_loaded(string action)
    {
        using var f = new FlowFixture(); var seed = await SeedAsync(f, action);
        if (action == "unskip") await seed.Plans.UnskipAsync(seed.Occurrence, Ct);
        var answer = new TaskCompletionSource<bool>(); f.Platform.PendingConfirmation = () => answer.Task;
        var commands = new OccurrenceCommands(seed.Plans, f.Translator, f.Platform);
        var completed = 0;
        Task<bool>? pending = action == "link" ? ActAsync(commands, seed, action,
            () => { completed++; return Task.CompletedTask; }) : null;
        Assert.True((await seed.Plans.SettleAsync(seed.Occurrence,
            Occurrences.CreateEntry(seed.Occurrence, 1000, Day, ReviewState.Confirmed), Ct)).Succeeded);
        var before = await SnapshotAsync(f); var notifications = 0;
        seed.Plans.Changed += (_, _) => notifications++;
        answer.SetResult(true);
        var accepted = pending is not null ? await pending : await ActAsync(commands, seed, action,
            () => { completed++; return Task.CompletedTask; });
        Assert.False(accepted); Assert.Equal(before, await SnapshotAsync(f));
        Assert.Equal(0, notifications); Assert.Equal(0, completed);
        Assert.IsType<InvalidOperationException>(Assert.Single(f.Platform.Failures)); Assert.Empty(f.Platform.Alerts);
    }

    [Fact]
    public async Task A_link_consent_cannot_replace_a_skip_accepted_while_the_original_dialog_is_pending()
    {
        using var f = new FlowFixture(); var seed = await SeedAsync(f, "link");
        var answer = new TaskCompletionSource<bool>(); f.Platform.PendingConfirmation = () => answer.Task;
        var commands = new OccurrenceCommands(seed.Plans, f.Translator, f.Platform);
        var completed = 0;
        var pending = ActAsync(commands, seed, "link", () => { completed++; return Task.CompletedTask; });
        await seed.Plans.SkipAsync(seed.Occurrence, Ct);
        var before = await SnapshotAsync(f); var notifications = 0;
        seed.Plans.Changed += (_, _) => notifications++;
        answer.SetResult(true);
        Assert.False(await pending); Assert.Equal(before, await SnapshotAsync(f));
        Assert.Equal(0, notifications); Assert.Equal(0, completed);
        Assert.IsType<InvalidOperationException>(Assert.Single(f.Platform.Failures));
    }
}
