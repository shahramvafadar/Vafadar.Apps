using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Features.Budget;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Native budget clearing retains independent parser contexts and raw drafts through original reload.</summary>
[Collection("Entry validation units")]
public sealed class BudgetNullableBindingTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>Total and category inputs treat null as complete clearing, without normalizing the retained raw text.</summary>
    [Theory]
    [InlineData(false, null)]
    [InlineData(true, null)]
    [InlineData(false, "")]
    [InlineData(true, "")]
    public async Task Cleared_budget_binding_survives_reload_and_keeps_other_input_context(bool category, string? raw)
    {
        using var f = new FlowFixture();
        await f.Store.GetSettingsAsync(Ct);
        var definition = new Category { Kind = CategoryKind.Expense, Name = "Independent limit" };
        await using (var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct))
        {
            (await db.Settings.SingleAsync(Ct)).Mode = ExperienceMode.Advanced;
            db.Categories.Add(definition); await db.SaveChangesAsync(Ct);
        }
        f.Localization.SetFormattingCulture("en-US");
        var editor = new BudgetEditorViewModel(f.Store, f.Translator, f.Localization, f.Platform);
        try
        {
            await editor.ApplyQueryAsync(new Dictionary<string, object> { ["year"] = 2026, ["month"] = 10, ["currency"] = "EUR" });
            Assert.True(editor.IsReady);
            var row = Assert.Single(editor.Limits); Assert.Equal(definition.Id, row.CategoryId);
            editor.TotalText = "12.34"; row.Text = "9.50";
            var before = await GoalChooserFlowTests.SnapshotAsync(f);
            f.Localization.SetFormattingCulture("de-DE"); editor.RefreshDisplay();
            Assert.NotNull(editor.TotalFormatNote); Assert.NotNull(row.FormatNote);
            if (category) row.Text = raw!; else editor.TotalText = raw!;
            string? Read() => category ? editor.Limits.Single().Text : editor.TotalText;
            Assert.Equal(raw, Read());
            _ = editor.TotalUnitName; _ = editor.TotalFormatNote; _ = editor.MoneyReviewText; _ = editor.CanSave;
            if (category) { Assert.Null(row.FormatNote); Assert.NotNull(editor.TotalFormatNote); }
            else { Assert.Null(editor.TotalFormatNote); Assert.NotNull(row.FormatNote); }
            await editor.ReloadCommand.ExecuteAsync(null);
            Assert.True(editor.IsReady); Assert.Equal(raw, Read());
            row = Assert.Single(editor.Limits);
            Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
            if (category)
            {
                row.Text = "2,50";
                Assert.True(row.Input!.TryParse(row.Text, out var minor)); Assert.Equal(250, minor);
                editor.TotalText = "invalid";
            }
            else { editor.TotalText = "2,50"; row.Text = "invalid"; }
            Assert.True(editor.CanSave);
            await editor.SaveCommand.ExecuteAsync(null);
            if (category) { Assert.Null(row.Error); Assert.NotNull(editor.TotalError); }
            else { Assert.Null(editor.TotalError); Assert.NotNull(row.Error); }
            Assert.Equal("2,50", Read());
            Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
            Assert.Empty(f.Platform.ConfirmationDialogs);
        }
        finally { editor.SetViewActive(false); }
    }
}
