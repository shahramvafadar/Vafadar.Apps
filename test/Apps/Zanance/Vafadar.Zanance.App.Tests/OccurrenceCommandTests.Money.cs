using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Features.Plans;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Core.Plans;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Tests;

public sealed partial class OccurrenceCommandTests
{
    private static Task<bool> OriginalMoneyCommand(OccurrenceCommands commands, Seed seed, string action, string path,
        Func<bool> current, Func<Task> completed) => action == "change"
        ? commands.ChangeAsync(seed.Occurrence, Day.AddDays(2), 1234, "Original raw note", completed, path, "EUR", current)
        : commands.PayAsync(seed.Occurrence, action == "partial" ? 400 : 1234, Day, action == "partial", _ => { }, completed, path, "EUR", current);

    /// <summary>The last original-owner check must roll back saved money/state together, not just prevent notification.</summary>
    [Theory, InlineData("full"), InlineData("partial"), InlineData("change"), Trait("AT", "AT-436")]
    public async Task Original_occurrence_owner_retirement_before_commit_rolls_back_the_entire_writer(string action)
    {
        using var f = new FlowFixture(); var seed = await SeedAsync(f, action); var original = await seed.Plans.ReadOriginalScheduleAsync(seed.Plan.Id, Ct);
        var before = await SnapshotAsync(f); var checks = 0; var publications = 0;
        var result = await OriginalMoneyCommand(new(seed.Plans, f.Translator, f.Platform), seed, action, original.DatabasePath,
            () => ++checks < 4, () => { publications++; return Task.CompletedTask; });
        Assert.False(result); Assert.Equal(4, checks); Assert.Equal(0, publications);
        Assert.Single(f.Platform.Failures); Assert.Equal(before, await SnapshotAsync(f));
    }

    [Theory, InlineData("full"), InlineData("partial"), InlineData("change"), Trait("AT", "AT-436")]
    public async Task Original_occurrence_money_commands_refuse_another_actual_file(string action)
    {
        using var f = new FlowFixture(); var seed = await SeedAsync(f, action); var before = await SnapshotAsync(f);
        Assert.False(await OriginalMoneyCommand(new(seed.Plans, f.Translator, f.Platform), seed, action, f.Directory.Combine("different.db"),
            () => true, () => throw new InvalidOperationException("A rejected command cannot publish.")));
        Assert.Single(f.Platform.Failures); Assert.Equal(before, await SnapshotAsync(f));
    }

    [Theory, InlineData("full", false), InlineData("partial", false), InlineData("change", false),
        InlineData("full", true), InlineData("partial", true), InlineData("change", true), Trait("AT", "AT-436")]
    public async Task Stored_currency_or_financial_target_changes_cannot_adopt_original_occurrence_input(string action, bool changedTarget)
    {
        using var f = new FlowFixture(); var seed = await SeedAsync(f, action); var original = await seed.Plans.ReadOriginalScheduleAsync(seed.Plan.Id, Ct);
        if (changedTarget)
        {
            await SqlAsync(f, $"UPDATE Schedules SET Kind={(int)Vafadar.Zanance.Core.Ledger.EntryKind.Income}");
            Assert.NotEqual(seed.Plan.Kind, (await seed.Plans.GetScheduleAsync(seed.Plan.Id, Ct))!.Kind);
        }
        else await SqlAsync(f, "UPDATE Accounts SET CurrencyCode='USD'");
        var before = await SnapshotAsync(f);
        Assert.False(await OriginalMoneyCommand(new(seed.Plans, f.Translator, f.Platform), seed, action, original.DatabasePath,
            () => true, () => throw new InvalidOperationException("A rejected command cannot publish.")));
        Assert.Single(f.Platform.Failures); Assert.Equal(before, await SnapshotAsync(f));
    }

    [Theory, InlineData("full"), InlineData("partial"), InlineData("change"), Trait("AT", "AT-436")]
    public async Task Accepted_original_occurrence_input_is_consumed_before_fallible_notification(string action)
    {
        using var f = new FlowFixture(); var seed = await SeedAsync(f, action); var original = await seed.Plans.ReadOriginalScheduleAsync(seed.Plan.Id, Ct);
        var owns = true; var publications = 0;
        if (action == "change") seed.Plans.Changed += (_, _) => throw new IOException("Original accepted observer failure");
        else f.Store.Changed += (_, _) => throw new IOException("Original accepted observer failure");
        var commands = new OccurrenceCommands(seed.Plans, f.Translator, f.Platform);
        Assert.True(await OriginalMoneyCommand(commands, seed, action, original.DatabasePath, () => owns,
            () => { owns = false; publications++; return Task.CompletedTask; }));
        Assert.Equal(1, publications); Assert.Single(f.Platform.Alerts); Assert.Empty(f.Platform.Failures);
        var accepted = await SnapshotAsync(f);
        Assert.False(await OriginalMoneyCommand(commands, seed, action, original.DatabasePath, () => owns,
            () => throw new InvalidOperationException("An original accepted command cannot publish twice.")));
        Assert.Equal(1, publications); Assert.Equal(accepted, await SnapshotAsync(f));
    }
}
