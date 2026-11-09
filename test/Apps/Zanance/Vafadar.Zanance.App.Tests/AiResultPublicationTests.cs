using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;
namespace Vafadar.Zanance.App.Tests;
/// <summary>Original accepted-result feedback is coherent and frame-owned without replaying any financial primary.</summary>
public sealed class AiResultPublicationTests
{
    /// <summary>Nine new notification/getter/draft/current conditions preserve source and never submit or authenticate.</summary>
    [Theory][InlineData(0)][InlineData(1)][InlineData(2)][InlineData(3)][InlineData(4)][InlineData(5)][InlineData(6)][InlineData(7)][InlineData(8)]
    public async Task Ai_result_publication_retains_known_outcome_without_departed_followup(int phase)
    {
        using var f=new FlowFixture();var p=await AiResultPublicationProbes.CreateAsync(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>(),f.Services.GetRequiredService<ICommercialWriteAccessSource>(),f.Translator,f.Localization,f.Time,f.Platform);
        var before=await GoalChooserFlowTests.SnapshotAsync(f);f.Platform.Confirmations.Enqueue(true);
        try
        {
            var v=await AiResultPublicationProbes.RunAsync(p,phase);Assert.Equal(before,await GoalChooserFlowTests.SnapshotAsync(f));Assert.Null(v.Exception);Assert.False(v.Pending);Assert.True(v.CanLeave);Assert.False(v.CanContinue);Assert.Equal(1,v.Closes);Assert.Equal(0,v.Forbidden);Assert.Equal(0,v.Late);
            if(phase<=4){Assert.True(v.Fired);Assert.False(v.Loaded);Assert.Equal(0,v.Reads);}
            if(phase==5){Assert.True(v.Fired);Assert.Equal(0,v.Reads);Assert.NotNull(v.Error);}
            if(phase is 6 or 7){Assert.True(v.Coherent);Assert.True(v.Changed>0);Assert.Equal(1,v.Reads);Assert.Equal(p.Runtime.Remote.Result.Text,v.Text);Assert.Equal(p.Runtime.Remote.Snapshot,v.Snapshot);}
            if(phase==6){Assert.True(v.Fired);Assert.Equal(p.From.AddDays(1),v.From);}
            if(phase==8){Assert.True(v.Fired);Assert.False(v.Loaded);Assert.Equal(1,v.Reads);}
        }
        finally{p.Model.Retire();}
    }
    /// <summary>A language callback during the new result stage keeps the actual current-language final receipt, without another primary.</summary>
    [Theory][InlineData(9)][InlineData(10)]
    public async Task Ai_result_current_culture_survives_changing_and_changed_callbacks(int phase)
    {
        using var f=new FlowFixture();var p=await AiResultPublicationProbes.CreateAsync(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>(),f.Services.GetRequiredService<ICommercialWriteAccessSource>(),f.Translator,f.Localization,f.Time,f.Platform);
        var before=await GoalChooserFlowTests.SnapshotAsync(f);f.Platform.Confirmations.Enqueue(true);
        try
        {
            var v=await AiResultPublicationProbes.RunAsync(p,phase);Assert.Equal(before,await GoalChooserFlowTests.SnapshotAsync(f));Assert.Null(v.Exception);Assert.True(v.Fired);Assert.Equal("de",f.Localization.CurrentLanguage.CultureName);Assert.Equal(f.Translator.Format("Ai_ResultUnits","3"),v.Status);
            Assert.Equal(p.Runtime.Remote.Result.Text,v.Text);Assert.Equal(p.Runtime.Remote.Snapshot,v.Snapshot);Assert.Equal(f.Translator.Format("Ai_Balance","13","0","0","0"),v.Balance);Assert.Equal(1,v.Closes);Assert.Equal(1,v.Reads);Assert.Equal(0,v.Forbidden);Assert.False(v.Pending);Assert.Null(v.Error);
        }
        finally{p.Model.Retire();}
    }

    /// <summary>Missing/foreign/regressed follow-up cannot erase a known result; the same pending request may advance.</summary>
    [Theory][InlineData(11)][InlineData(12)][InlineData(13)][InlineData(14)][InlineData(15)][InlineData(16)][InlineData(17)]
    public async Task Ai_result_followup_retains_original_identity_and_monotonic_outcome(int phase)
    {
        using var f=new FlowFixture();var p=await AiResultPublicationProbes.CreateAsync(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>(),f.Services.GetRequiredService<ICommercialWriteAccessSource>(),f.Translator,f.Localization,f.Time,f.Platform);
        var before=await GoalChooserFlowTests.SnapshotAsync(f);f.Platform.Confirmations.Enqueue(true);
        try
        {
            var v=await AiResultPublicationProbes.RunAsync(p,phase);Assert.Equal(before,await GoalChooserFlowTests.SnapshotAsync(f));Assert.Null(v.Exception);Assert.Equal(1,v.Closes);Assert.Equal(1,v.Reads);Assert.Equal(0,v.Forbidden);Assert.False(v.Pending);Assert.Equal(p.Runtime.Remote.Snapshot,v.Snapshot);
            if(phase==17){Assert.True(v.CanContinue);Assert.Null(v.Text);Assert.Equal(f.Translator["Ai_Pending"],v.Status);}
            else{Assert.False(v.CanContinue);Assert.Equal(p.Runtime.Remote.Result.Text,v.Text);Assert.Equal(f.Translator.Format("Ai_ResultUnits","3"),v.Status);}
            if(phase==16)Assert.Null(v.Error);else{Assert.True(v.Fired);Assert.NotNull(v.Error);}
        }
        finally{p.Model.Retire();}
    }

}
