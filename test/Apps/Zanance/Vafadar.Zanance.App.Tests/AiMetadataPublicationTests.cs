using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;
namespace Vafadar.Zanance.App.Tests;
/// <summary>Metadata/quote publication is coherent, current and fully owned before follow-up authority becomes available.</summary>
public sealed class AiMetadataPublicationTests
{
    /// <summary>Seventeen new metadata/resource/quote/coherence conditions leave actual source and financial data unchanged.</summary>
    [Theory]
    [InlineData(0)][InlineData(1)][InlineData(2)][InlineData(3)][InlineData(4)][InlineData(6)][InlineData(7)][InlineData(8)]
    [InlineData(9)][InlineData(10)][InlineData(11)][InlineData(13)][InlineData(14)][InlineData(15)][InlineData(16)]
    public async Task Ai_metadata_publication_owns_coherent_values_resources_and_followup(int phase)
        => await VerifyAsync(phase);
    /// <summary>Corrected observer targets the retained quote connection and the completed result text.</summary>
    [Theory][InlineData(5)][InlineData(12)]
    public async Task Ai_metadata_corrected_observer_contract(int phase) => await VerifyAsync(phase);
    private static async Task VerifyAsync(int phase)
    {
        var ct=TestContext.Current.CancellationToken;using var f=new FlowFixture();
        if(Environment.GetEnvironmentVariable("ZANANCE_D966_HOST_TMP")is{}owned)Assert.StartsWith(Path.GetFullPath(owned),Path.GetFullPath(f.Directory.Path),StringComparison.OrdinalIgnoreCase);
        var factory=f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>();
        await using(var db=await factory.CreateDbContextAsync(ct)){db.Accounts.Add(new Account{Name="D966 metadata only",Type=AccountType.Checking,CurrencyCode="EUR",OpeningDate=new(2026,1,1),OpeningBalance=0});await db.SaveChangesAsync(ct);}
        var p=await AiMetadataPublicationProbes.CreateAsync(phase,factory,f.Services.GetRequiredService<ICommercialWriteAccessSource>(),f.Translator,f.Localization,f.Time,f.Platform);
        var before=await GoalChooserFlowTests.SnapshotAsync(f);
        try
        {
            var v=await AiMetadataPublicationProbes.RunAsync(p,phase);Assert.Equal(before,await GoalChooserFlowTests.SnapshotAsync(f));Assert.Null(v.Exception);Assert.False(v.Pending);Assert.True(v.CanLeave);Assert.Equal(0,v.SourceCalls);Assert.Equal(0,v.Primary);Assert.Equal(0,v.Late);
            if(phase<=6||phase is 11 or 12){Assert.True(v.Fired);Assert.False(v.Loaded);Assert.False(v.CanContinue);}
            if(phase==6){Assert.Equal(1,v.FirstDisposals);Assert.Equal(1,v.SecondDisposals);}
            if(phase is 7 or 8){Assert.True(v.Fired);Assert.False(v.CanContinue);if(phase==7)Assert.Equal(p.OriginalFrom.AddDays(1),v.From);else Assert.Equal("de",v.Language);}
            if(phase==9){Assert.True(v.Fired);Assert.False(v.CanContinue);Assert.NotNull(v.Error);}
            if(phase==10){Assert.False(v.Fired);Assert.True(v.CanContinue);Assert.True(v.Coherent);}
            if(phase is 0 or 1 or 2 or 5 or 7 or 8 or 9 or 10 or 15)Assert.True(v.Coherent);
            if(phase==13){Assert.True(v.Fired);Assert.Equal(p.OriginalFrom.AddDays(1),v.From);Assert.Null(v.Error);}
            if(phase==14){Assert.False(v.Fired);Assert.NotNull(v.Error);Assert.False(v.CanSend);Assert.True(v.CanQuote);}
            if(phase==15){Assert.True(v.Fired);Assert.False(v.ReadyAfterFailure);Assert.True(v.CanContinue);Assert.Equal(2,v.Opens);}
            if(phase==16){Assert.True(v.CanContinue);Assert.Equal(0,v.FirstDisposals);}
            Assert.Equal(phase is >=11 and <=14?1:0,v.Offers);Assert.Empty(f.Platform.Alerts);Assert.Empty(f.Platform.ConfirmationDialogs);Assert.Empty(f.Platform.Routes);
        }
        finally{p.Model.Retire();}
    }
}
