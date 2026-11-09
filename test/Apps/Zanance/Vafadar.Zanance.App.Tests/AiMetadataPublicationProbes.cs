using System.ComponentModel;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Analysis;
using Vafadar.Zanance.App.Interaction;
using Vafadar.Zanance.Core.Analysis;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Analysis;
using Vafadar.Zanance.Data.Commerce;
namespace Vafadar.Zanance.App.Tests;

/// <summary>Original metadata/quote publication over real sources and declared read-only historical input.</summary>
internal static class AiMetadataPublicationProbes
{
    /// <summary>Only retained-connection and quote conditions prepare their necessary prior local state.</summary>
    internal static async Task<AiMetadataPrepared> CreateAsync(int phase, IDbContextFactory<ZananceDbContext> original,
        ICommercialWriteAccessSource access, Translator text, ILocalizationService locale, TimeProvider time, IAppInteraction interaction)
    {
        var source=new AiMetadataSource(original);var runtime=new AiMetadataRuntime(time,phase);var model=new AiAnalysisViewModel(new AiSnapshotStore(source,access),text,locale,new DateFormatter(locale),time,interaction,runtime);
        await model.LoadAsync();if(!model.Loaded||model.Accounts.Count!=1)throw new InvalidDataException();
        if(phase is >=11 and <=14){model.Accounts[0].Selected=true;await model.ReviewAsync();if(!model.CanShare)throw new InvalidDataException();}
        if(phase is 6 or 16||phase is >=11 and <=14)await model.CheckProviderAsync();
        source.Calls=0;runtime.Reset();return new(model,source,runtime,text,locale,model.From);
    }
    /// <summary>One new guarded publication; only the failed-binding case explicitly retries with a different retained DTO.</summary>
    internal static async Task<AiMetadataObservation> RunAsync(AiMetadataPrepared p,int phase)
    {
        var model=p.Model;var quote=phase is >=11 and <=14;var expected=quote?p.Runtime.First:p.Runtime.Next;var fired=false;var obsolete=false;var late=0;var coherent=true;var changed=0;
        var fields=new[]{nameof(model.ProviderText),nameof(model.BalanceText),nameof(model.ResultSnapshot),nameof(model.ResultText),nameof(model.ResultStatus)};
        PropertyChangedEventHandler handler=(_,e)=>
        {
            if(obsolete&&(fields.Contains(e.PropertyName)||e.PropertyName==nameof(model.Error)))late++;
            if(!quote&&fields.Contains(e.PropertyName)&&!fired)
            {
                changed++;coherent&=model.ProviderText==expected.Address&&model.BalanceText==p.Text.Format("Ai_Balance",expected.Balance.PurchasedAvailable.ToString(),"0","0","0")
                    &&model.ResultSnapshot==expected.Snapshot&&model.ResultText==expected.Record?.Result?.Text&&model.ResultStatus==(expected.Record is null?null:expected.Record.Result is null?p.Text["Ai_Pending"]:p.Text.Format("Ai_ResultUnits","3"));
            }
            var target=phase switch{0 or 7 or 8 or 11 or 13=>nameof(model.ProviderText),1=>nameof(model.BalanceText),2 or 9 or 15=>nameof(model.ResultSnapshot),5=>nameof(model.ResultText),_=>"none"};
            if(fired||e.PropertyName!=target)return;fired=true;
            if(phase is 9 or 15)throw new IOException("D966 original metadata binding refused.");
            if(phase is 7 or 13)model.From=p.OriginalFrom.AddDays(1);
            else if(phase==8)p.Locale.SetLanguage(p.Locale.SupportedLanguages.Single(x=>x.CultureName=="de"));
            else model.RetireRoot();
            obsolete=true;
        };
        PropertyChangingEventHandler changing=(_,e)=>{if(phase!=3||fired||e.PropertyName!=nameof(model.ProviderText))return;fired=true;model.RetireRoot();obsolete=true;};
        if(phase is 4 or 12)expected.BeforeDestination=()=>{if(fired)return;fired=true;model.RetireRoot();obsolete=true;};
        if(phase==6)p.Runtime.First.BeforeDispose=()=>{if(fired)return;fired=true;model.RetireRoot();obsolete=true;};
        model.PropertyChanged+=handler;model.PropertyChanging+=changing;string? exception=null;bool readyAfterFailure=false;
        try{if(quote)await model.QuoteAsync();else await model.CheckProviderAsync();}
        catch(Exception e)when(e is not OutOfMemoryException){exception=e.GetType().Name;}
        finally{model.PropertyChanged-=handler;model.PropertyChanging-=changing;expected.BeforeDestination=null;p.Runtime.First.BeforeDispose=null;}
        if(phase==15)
        {
            readyAfterFailure=model.CanContinue;expected=p.Runtime.Next;await model.CheckProviderAsync();
            coherent&=model.ResultSnapshot==expected.Snapshot&&model.ResultText==expected.Record?.Result?.Text;
        }
        return new(fired,late,coherent,changed,exception,model.Loaded,model.HasPendingOperation,model.CanLeave,model.CanContinue,model.CanQuote,model.CanSend,model.From,p.Locale.CurrentLanguage.CultureName,
            model.Error,model.ResultSnapshot,model.ResultText,model.ResultStatus,p.Source.Calls,p.Runtime.Opens,p.Runtime.Offers,p.Runtime.Primary,p.Runtime.First.Disposals,p.Runtime.Second.Disposals,readyAfterFailure);
    }
}
/// <summary>One original current model and its counted source plus independently owned local metadata connections.</summary>
internal sealed record AiMetadataPrepared(AiAnalysisViewModel Model,AiMetadataSource Source,AiMetadataRuntime Runtime,Translator Text,ILocalizationService Locale,DateOnly OriginalFrom);
/// <summary>Publication coherence, follow-up authority, resource and effect evidence after one new condition.</summary>
internal sealed record AiMetadataObservation(bool Fired,int Late,bool Coherent,int Changed,string? Exception,bool Loaded,bool Pending,bool CanLeave,bool CanContinue,bool CanQuote,bool CanSend,DateOnly From,string Language,
    string? Error,string? Snapshot,string? ResultText,string? ResultStatus,int SourceCalls,int Opens,int Offers,int Primary,int FirstDisposals,int SecondDisposals,bool ReadyAfterFailure);
/// <summary>Counts the original actual SQLite factory, without substituting its source reader.</summary>
internal sealed class AiMetadataSource(IDbContextFactory<ZananceDbContext> original):IDbContextFactory<ZananceDbContext>
{
    internal int Calls;
    public ZananceDbContext CreateDbContext(){Calls++;return original.CreateDbContext();}
    public Task<ZananceDbContext>CreateDbContextAsync(CancellationToken ct=default){Calls++;return original.CreateDbContextAsync(ct);}
}
/// <summary>Local scope and metadata only; no native authorization or primary client exists.</summary>
internal sealed class AiMetadataRuntime:IAiRuntime
{
    internal readonly AiMetadataRemote First,Second;private readonly int phase;private int returned;
    internal int Opens,Offers,Primary;
    internal AiMetadataRuntime(TimeProvider time,int phase)
    {
        this.phase=phase;First=new(this,time,"first",phase);Second=new(this,time,"second",phase);
    }
    internal AiMetadataRemote Next=>phase==16||returned==0?First:Second;
    internal void Reset(){Opens=Offers=Primary=0;First.Disposals=Second.Disposals=0;}
    public bool NetworkAvailable=>true;
    public AiUiAccess Capture(string file)=>new(()=>{},CancellationToken.None);
    public Task<IAiRemote>OpenAsync(string file,CancellationToken ct){Opens++;var next=Next;returned++;return Task.FromResult<IAiRemote>(next);}
    public Task<bool>ConfirmDisclosureAsync(){Primary++;return Task.FromException<bool>(new InvalidOperationException("No real authentication"));}
    public Task ShareAsync(string exactText){Primary++;return Task.FromException(new InvalidOperationException("No share"));}
}
/// <summary>Two distinct declared metadata records are inputs, never results of a primary submission or financial write.</summary>
internal sealed class AiMetadataRemote:IAiRemote
{
    private readonly AiMetadataRuntime owner;private readonly TimeProvider time;
    internal readonly string Address;internal readonly AiBalance Balance;internal readonly AiClientRecord? Record;
    internal int Disposals;internal Action?BeforeDestination,BeforeDispose;
    internal AiMetadataRemote(AiMetadataRuntime owner,TimeProvider time,string tag,int phase)
    {
        this.owner=owner;this.time=time;Address="https://localhost/declared-"+tag;Balance=new(phase is >=11 and <=14?0:tag=="first"?17:29,0,0,0);
        if(phase is >=11 and <=14)return;
        var payload=new AiPayload(1,AiPurpose.PeriodExplanation,"en",new(new(2026,10,1),new(2026,10,9),1,true,[new("EUR",0,0,0,0)],0),null);
        var offer=new AiOffer(Guid.NewGuid(),"declared","input-only",payload.Purpose,7,time.GetUtcNow().AddDays(1));var id=Guid.NewGuid();
        Record=new(1,new(id,offer,payload),phase is 2 or 5?new(id,AiProtocol.Digest(payload),AiRequestState.Completed,3,"Declared untrusted "+tag,Balance):null);
    }
    public string Destination{get{BeforeDestination?.Invoke();if(Disposals>0)throw new ObjectDisposedException("Original declared metadata connection");return Address;}}
    internal string?Snapshot=>Record is null?null:Address+Environment.NewLine+JsonSerializer.Serialize(Record.Submission,AiJson.Default.AiSubmission);
    public Task<AiAvailability>AvailabilityAsync(CancellationToken ct)=>Task.FromResult(new AiAvailability(true,"declared","input-only",Balance));
    public Task<AiClientRecord?>ReadAsync(CancellationToken ct)=>Task.FromResult(Record);
    public Task<AiOffer>OfferAsync(AiPurpose purpose,CancellationToken ct){owner.Offers++;return Task.FromResult(new AiOffer(Guid.NewGuid(),"declared","input-only",purpose,7,time.GetUtcNow().AddDays(1)));}
    public Task<AiRequestResult>SubmitAsync(AiOffer offer,AiPayload payload,Func<Task>check,CancellationToken ct){owner.Primary++;return Task.FromException<AiRequestResult>(new InvalidOperationException("No primary"));}
    public Task<AiRequestResult>ContinueAsync(CancellationToken ct){owner.Primary++;return Task.FromException<AiRequestResult>(new InvalidOperationException("No continuation"));}
    public Task<AiRequestResult>CloseAsync(CancellationToken ct){owner.Primary++;return Task.FromException<AiRequestResult>(new InvalidOperationException("No close"));}
    public void Dispose(){Disposals++;BeforeDispose?.Invoke();}
}
