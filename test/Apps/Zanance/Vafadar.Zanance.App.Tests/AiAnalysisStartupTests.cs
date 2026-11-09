using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Features.Analysis;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Original analysis startup must own cleanup and refuse all local/native/provider effects after draft or page replacement.</summary>
public sealed partial class AiAnalysisStartupTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    /// <summary>All explicit original operation entries, paired with distinct startup publications.</summary>
    public static TheoryData<string, string> Cases => new()
    {
        { "load", "throw" }, { "review", "throw" }, { "provider", "throw" }, { "share", "throw" },
        { "quote", "throw" }, { "send", "throw" }, { "continue", "throw" }, { "close", "throw" },
        { "load", "retire" }, { "review", "retire" }, { "provider", "retire" }, { "share", "retire" },
        { "quote", "retire" }, { "send", "retire" }, { "continue", "retire" }, { "close", "retire" },
        { "load", "draft" }, { "review", "draft" }, { "provider", "draft" }, { "share", "draft" },
        { "quote", "draft" }, { "send", "draft" }, { "continue", "draft" }, { "close", "draft" }
    };

    /// <summary>All eight operation entries retain their original Busy cleanup, page and date-draft revision.</summary>
    [Theory]
    [MemberData(nameof(Cases))]
    public async Task Ai_startup_busy_publication_keeps_original_cleanup_and_authority(string operation, string mutation)
    {
        using var f = new FlowFixture(); var s = await PrepareAsync(f, operation); var fired = false; var originalFrom = s.Model.From;
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        s.Model.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(AiAnalysisViewModel.IsBusy) || !s.Model.IsBusy || fired) return;
            fired = true;
            if (mutation == "throw") throw new IOException("D958 original startup publication failure");
            if (mutation == "retire") s.Model.RetireRoot(); else s.Model.From = originalFrom.AddDays(1);
        };
        var error = await Record.ExceptionAsync(() => AiStartupPreparation.InvokeAsync(s.Model, operation));
        Assert.True(fired); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        Assert.False(s.Model.HasPendingOperation); Assert.False(s.Model.IsBusy); Assert.True(s.Model.CanLeave); Assert.Null(error);
        Assert.Equal(0, s.Factory.Calls); Assert.Equal(0, s.Runtime.BoundaryCalls); Assert.Empty(f.Platform.ConfirmationDialogs);
        Assert.Empty(f.Platform.Routes); if (mutation == "draft") Assert.Equal(originalFrom.AddDays(1), s.Model.From);
        s.Model.Retire();
    }

    /// <summary>Clearing an old local error may change the current input before source/provider entry.</summary>
    [Theory]
    [InlineData("review")]
    [InlineData("provider")]
    public async Task Ai_startup_error_clear_cannot_adopt_a_newer_draft(string operation)
    {
        using var f = new FlowFixture(); var s = await PrepareAsync(f, operation);
        if (operation == "provider") Assert.Single(s.Model.Accounts).Selected = true;
        s.Runtime.Reset(false); s.Factory.Refuse = true; await s.Model.ReviewAsync(); Assert.NotNull(s.Model.Error);
        s.Runtime.Reset(true); s.Factory.Calls = 0; var fired = false; var originalFrom = s.Model.From;
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        s.Model.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(AiAnalysisViewModel.Error) || s.Model.Error is not null || fired) return;
            fired = true; s.Model.From = originalFrom.AddDays(1);
        };
        await AiStartupPreparation.InvokeAsync(s.Model, operation);
        Assert.True(fired); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        Assert.False(s.Model.HasPendingOperation); Assert.True(s.Model.CanLeave); Assert.Equal(originalFrom.AddDays(1), s.Model.From);
        Assert.Equal(0, s.Factory.Calls); Assert.Equal(0, s.Runtime.BoundaryCalls); Assert.Empty(f.Platform.ConfirmationDialogs); s.Model.Retire();
    }

    /// <summary>Validation captions cannot silently replace the date selection before the original review takes ownership.</summary>
    [Fact]
    public async Task Ai_startup_review_validation_publication_keeps_the_original_draft()
    {
        using var f = new FlowFixture(); var s = await PrepareAsync(f, "review"); var originalTo = s.Model.To;
        s.Model.To = s.Model.From.AddDays(-1); await s.Model.ReviewAsync(); Assert.NotNull(s.Model.ToError);
        s.Model.To = originalTo; var from = s.Model.From; var fired = false;
        s.Runtime.Reset(true); s.Factory.Calls = 0; var before = await GoalChooserFlowTests.SnapshotAsync(f);
        s.Model.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(AiAnalysisViewModel.ToError) || s.Model.ToError is not null || fired) return;
            fired = true; s.Model.From = from.AddDays(1);
        };
        await s.Model.ReviewAsync();
        Assert.True(fired); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f)); Assert.Equal(from.AddDays(1), s.Model.From);
        Assert.False(s.Model.HasPendingOperation); Assert.True(s.Model.CanLeave); Assert.Equal(0, s.Factory.Calls);
        Assert.Equal(0, s.Runtime.BoundaryCalls); Assert.Empty(f.Platform.ConfirmationDialogs); s.Model.Retire();
    }

    private static async Task<AiStartupPrepared> PrepareAsync(FlowFixture f, string operation)
    {
        var factory = f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        await using (var db = await factory.CreateDbContextAsync(Ct))
        { db.Accounts.Add(new Account { Name = "D958 new zero-activity account", Type = AccountType.Checking, CurrencyCode = "EUR", OpeningDate = new(2026, 1, 1), OpeningBalance = 0 }); await db.SaveChangesAsync(Ct); }
        return await AiStartupPreparation.CreateAsync(operation, factory, f.Services.GetRequiredService<ICommercialWriteAccessSource>(), f.Translator, f.Localization, f.Time, f.Platform);
    }
}
