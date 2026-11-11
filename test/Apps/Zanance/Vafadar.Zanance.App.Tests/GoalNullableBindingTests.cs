using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Goals;
using Vafadar.Zanance.App.Features.Holdings;
using Vafadar.Zanance.Core.Goals;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Original goal drafts must tolerate native clearing without replacing raw text or reaching a writer.</summary>
[Collection("Entry validation units")]
public sealed class GoalNullableBindingTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private sealed class Notifications : IGoalEditorNotifications
    {
        public bool IsSupported => false;
        public Task<bool> EnableAsync() => throw new InvalidOperationException("Clearing input cannot request permission.");
    }

    /// <summary>Each independently cleared text retains its raw value through captions, validation and reentry.</summary>
    [Theory]
    [InlineData("name", null)]
    [InlineData("amount", null)]
    [InlineData("contribution", null)]
    [InlineData("percent", null)]
    [InlineData("name", "")]
    [InlineData("amount", "")]
    [InlineData("contribution", "")]
    [InlineData("percent", "")]
    public async Task Cleared_goal_binding_preserves_raw_input_and_full_source(string field, string? raw)
    {
        using var f = new FlowFixture();
        await f.Store.GetSettingsAsync(Ct);
        var dates = new DateFormatter(f.Localization);
        var editor = new GoalEditorViewModel(f.Store, f.Services.GetRequiredService<GoalStore>(), f.Translator,
            f.Localization, dates, f.Time, f.Services.GetRequiredService<HoldingStore>(),
            new HoldingText(f.Translator, f.Localization, dates), f.Platform, new Notifications());
        try
        {
            await editor.ApplyQueryAsync(new Dictionary<string, object>()); Assert.True(editor.IsReady);
            editor.TypeIndex = 1; editor.Name = "Unsaved goal"; editor.AmountText = "123.4500";
            editor.ContributionText = "4.5000"; editor.PercentText = "10.5000";
            if (field == "percent") editor.MethodIndex = (int)ContributionMethod.ShareOfIncome;
            var before = await GoalChooserFlowTests.SnapshotAsync(f);
            void Set(string? value)
            {
                switch (field)
                {
                    case "name": editor.Name = value!; break;
                    case "amount": editor.AmountText = value!; break;
                    case "contribution": editor.ContributionText = value!; break;
                    case "percent": editor.PercentText = value!; break;
                    default: throw new InvalidOperationException();
                }
            }
            string? Read() => field switch { "name" => editor.Name, "amount" => editor.AmountText,
                "contribution" => editor.ContributionText, "percent" => editor.PercentText, _ => throw new InvalidOperationException() };
            Set(raw); Assert.Equal(raw, Read());
            _ = editor.CanEditContribution; _ = editor.TargetUnitName; _ = editor.ContributionUnitName;
            _ = editor.TargetFormatNote; _ = editor.ContributionFormatNote; _ = editor.PercentFormatNote;
            editor.RefreshDisplay(); Assert.Equal(raw, Read());
            // Required missing name prevents every Save, including optional empty contribution/percent drafts.
            if (field != "name") editor.Name = string.Empty;
            await editor.SaveCommand.ExecuteAsync(null); Assert.Equal(raw, Read());
            Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
            Set(field == "name" ? "New raw name" : "2.5000");
            Assert.Equal(field == "name" ? "New raw name" : "2.5000", Read());
            Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
            Assert.Empty(f.Platform.ConfirmationDialogs);
        }
        finally { editor.SetViewActive(false); }
    }
}
