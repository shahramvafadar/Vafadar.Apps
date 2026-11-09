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

/// <summary>One declared retained result returned through the original Close command; no submit/authentication or source write.</summary>
internal static class AiResultPublicationProbes
{
    /// <summary>Loads the actual empty source and existing declared pending metadata, never a newly submitted analysis.</summary>
    internal static async Task<AiResultPrepared> CreateAsync(IDbContextFactory<ZananceDbContext> factory,
        ICommercialWriteAccessSource access,Translator text,ILocalizationService locale,TimeProvider time,IAppInteraction interaction)
    {
        var runtime=new AiResultRuntime(time);var model=new AiAnalysisViewModel(new AiSnapshotStore(factory,access),text,locale,new DateFormatter(locale),time,interaction,runtime);
        await model.LoadAsync();if(!model.Loaded||model.Accounts.Count!=0)throw new InvalidDataException();
        await model.CheckProviderAsync();if(!model.CanContinue)throw new InvalidDataException();
        runtime.Remote.Reads=0;return new(model,runtime,text,locale,model.From);
    }
    /// <summary>New reentrant result-publication conditions preserve accepted status and prohibit departed follow-up.</summary>
    internal static async Task<AiResultObservation> RunAsync(AiResultPrepared p,int phase)
    {
        var model=p.Model;var remote=p.Runtime.Remote;var fired=false;var obsolete=false;var late=0;var coherent=true;var changed=0;
        var fields=new[]{nameof(model.ResultSnapshot),nameof(model.ResultText),nameof(model.ResultStatus),nameof(model.BalanceText)};
        var expectedStatus=p.Text.Format("Ai_ResultUnits","3");var expectedBalance=p.Text.Format("Ai_Balance","13","0","0","0");
        PropertyChangedEventHandler handler=(_,e)=>
        {
            if(obsolete&&fields.Contains(e.PropertyName))late++;
            if(fields.Contains(e.PropertyName))
            {changed++;coherent&=model.ResultSnapshot==remote.Snapshot&&model.ResultText==remote.Result.Text&&model.ResultStatus==expectedStatus&&model.BalanceText==expectedBalance;}
            var target=phase switch{0 or 5 or 6 or 9=>nameof(model.ResultText),1=>nameof(model.ResultStatus),2=>nameof(model.BalanceText),_=>"none"};
            if(fired||e.PropertyName!=target)return;fired=true;
            if(phase==5)throw new IOException("D969 controlled result binding failure.");
            if(phase==6)model.From=p.From.AddDays(1);else if(phase==9)p.Locale.SetLanguage(p.Locale.SupportedLanguages.Single(x=>x.CultureName=="de"));else {model.RetireRoot();obsolete=true;}
        };
        PropertyChangingEventHandler changing=(_,e)=>
        {if(phase is not(3 or 10)||fired||e.PropertyName!=nameof(model.ResultText))return;fired=true;if(phase==10)p.Locale.SetLanguage(p.Locale.SupportedLanguages.Single(x=>x.CultureName=="de"));else {model.RetireRoot();obsolete=true;}};
        if(phase==4)remote.BeforeDestination=()=>{if(fired)return;fired=true;model.RetireRoot();obsolete=true;};
        if(phase==8)remote.BeforeRead=()=>{fired=true;model.RetireRoot();obsolete=true;};
        if(phase is 16 or 17)remote.ReturnPending=true;
        if(phase is >=11 and <=15||phase==17)remote.BeforeRead=()=>
        {
            fired=true;
            if(phase is 11 or 17){remote.Current=null;return;}
            var current=remote.Current!;
            if(phase==12){var id=Guid.NewGuid();remote.Current=current with{Submission=current.Submission with{RequestId=id},Result=current.Result! with{RequestId=id}};}
            if(phase==13)remote.Current=current with{Submission=current.Submission with{Offer=current.Submission.Offer with{Model="different-retained-input"}}};
            if(phase==14)remote.Current=current with{Result=current.Result! with{State=AiRequestState.Pending,ChargedUnits=0,Text=null}};
            if(phase==15)remote.Current=current with{Result=current.Result! with{Text="Different terminal content"}};
        };
        model.PropertyChanged+=handler;model.PropertyChanging+=changing;string? error=null;
        try{await model.CloseAsync();}catch(Exception e)when(e is not OutOfMemoryException){error=e.GetType().Name;}
        finally{model.PropertyChanged-=handler;model.PropertyChanging-=changing;remote.BeforeDestination=remote.BeforeRead=null;}
        return new(fired,late,coherent,changed,error,model.Loaded,model.HasPendingOperation,model.CanLeave,model.CanContinue,model.From,
            model.ResultSnapshot,model.ResultText,model.ResultStatus,model.BalanceText,model.Error,remote.Reads,remote.Closes,p.Runtime.Forbidden);
    }
}
/// <summary>Original model and declared status-only result input.</summary>
internal sealed record AiResultPrepared(AiAnalysisViewModel Model,AiResultRuntime Runtime,Translator Text,ILocalizationService Locale,DateOnly From);
/// <summary>Bounded coherent publication and effect counts; no financial data or keys.</summary>
internal sealed record AiResultObservation(bool Fired,int Late,bool Coherent,int Changed,string? Exception,bool Loaded,bool Pending,bool CanLeave,bool CanContinue,DateOnly From,
    string? Snapshot,string? Text,string? Status,string? Balance,string? Error,int Reads,int Closes,int Forbidden);
/// <summary>No primary submission, sharing, account or credential port can succeed.</summary>
internal sealed class AiResultRuntime(TimeProvider time):IAiRuntime
{
    internal readonly AiResultRemote Remote=new(time);internal int Forbidden;
    public bool NetworkAvailable=>true;
    public AiUiAccess Capture(string file)=>new(()=>{},CancellationToken.None);
    public Task<IAiRemote>OpenAsync(string file,CancellationToken ct)=>Task.FromResult<IAiRemote>(Remote);
    public Task<bool>ConfirmDisclosureAsync(){Forbidden++;throw new InvalidOperationException("No real authentication");}
    public Task ShareAsync(string text){Forbidden++;throw new InvalidOperationException("No sharing");}
}
/// <summary>Declared retained request/result only; Close returns an already completed DTO, never a financial HTTP call.</summary>
internal sealed class AiResultRemote:IAiRemote
{
    internal readonly AiClientRecord Pending;internal readonly AiRequestResult Result;internal AiClientRecord? Current;internal bool ReturnPending;
    internal int Reads,Closes;internal Action? BeforeDestination,BeforeRead;
    internal AiResultRemote(TimeProvider time)
    {
        var payload=new AiPayload(1,AiPurpose.PeriodExplanation,"en",new(new(2026,10,1),new(2026,10,9),1,true,[new("EUR",0,0,0,0)],0),null);
        var offer=new AiOffer(Guid.NewGuid(),"declared","historical-input",payload.Purpose,7,time.GetUtcNow().AddDays(1));var id=Guid.NewGuid();
        Pending=new(1,new(id,offer,payload),null);Current=Pending;Result=new(id,AiProtocol.Digest(payload),AiRequestState.Completed,3,"Declared historical untrusted result",new(13,0,0,0));
    }
    public string Destination{get{BeforeDestination?.Invoke();return "https://localhost/declared-result-input";}}
    internal string Snapshot=>"https://localhost/declared-result-input"+Environment.NewLine+JsonSerializer.Serialize(Pending.Submission,AiJson.Default.AiSubmission);
    public Task<AiAvailability>AvailabilityAsync(CancellationToken ct)=>Task.FromResult(new AiAvailability(true,"declared","historical-input",new(17,0,0,0)));
    public Task<AiClientRecord?>ReadAsync(CancellationToken ct){Reads++;BeforeRead?.Invoke();return Task.FromResult<AiClientRecord?>(Current);}
    public Task<AiOffer>OfferAsync(AiPurpose purpose,CancellationToken ct)=>throw new InvalidOperationException("No offer");
    public Task<AiRequestResult>SubmitAsync(AiOffer offer,AiPayload payload,Func<Task> check,CancellationToken ct)=>throw new InvalidOperationException("No submission");
    public Task<AiRequestResult>ContinueAsync(CancellationToken ct)=>throw new InvalidOperationException("No continuation");
    public Task<AiRequestResult>CloseAsync(CancellationToken ct){Closes++;Current=Pending with{Result=Result};return Task.FromResult(ReturnPending?Result with{State=AiRequestState.Pending,ChargedUnits=0,Text=null}:Result);}
    public void Dispose(){}
}
