using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using System.Security.Cryptography;
using System.Text.Json;
using Vafadar.Data;
using Vafadar.Zanance.App.Security;

/// <summary>Runs original protection against new fictitious keys in this independent app's UID.</summary>
[Activity(Name="pro.vafadar.zanance.keyreview.KeyActivity", Exported=true, Theme="@android:style/Theme.Material.Light.NoActionBar")]
public sealed class KeyActivity : Activity
{
    /// <inheritdoc />
    protected override void OnCreate(Bundle? state)
    {
        base.OnCreate(state);
        var phase=Intent?.GetStringExtra("review_phase");
        var run=Intent?.GetStringExtra("review_run");
        if(PackageName!="pro.vafadar.zanance.keyreview" || phase is not ("create" or "cold" or "retire")
            || !Guid.TryParseExact(run,"N",out var id)){Finish();return;}
        Window?.SetFlags(WindowManagerFlags.Secure,WindowManagerFlags.Secure);
        var text=new TextView(this){Text="Checking original device key protection"};SetContentView(text);
        _=RunAsync(Path.Combine(FilesDir!.AbsolutePath,id.ToString("N")),phase,text);
    }

    /// <summary>Leaves private encrypted fixtures in place between explicitly requested phases; logs no keys or envelopes.</summary>
    private async Task RunAsync(string root,string phase,TextView text)
    {
        try
        {
            var report=await Task.Run(()=>KeyCases.Run(root,phase));
            Android.Util.Log.Info("ZananceKeyReview",report);text.Text=report;
        }
        catch(Exception error)when(error is not OutOfMemoryException)
        {
            var report="refused="+error.GetType().FullName;
            Android.Util.Log.Error("ZananceKeyReview",report);text.Text=report;
        }
    }
}

/// <summary>Qualifies binding, native persistence, reported security and authorized alias retirement without a database.</summary>
internal static class KeyCases
{
    /// <summary>Private synthetic expectations; no key material is serialized or exposed.</summary>
    private sealed record Fixture(Guid Profile,Guid Installation,Guid Generation,int CreatePid,string ExpectedDigest);

    /// <summary>Executes only an unconsumed named phase against its original private fixture.</summary>
    internal static string Run(string root,string phase)
    {
        var marker=Path.Combine(root,phase+".started");
        if(phase=="create")
        {
            if(Directory.Exists(root))throw new IOException("A new key review must be fresh.");
            Directory.CreateDirectory(root);
        }
        if(!Directory.Exists(root))throw new IOException("Original key fixture missing.");
        using(var claim=new FileStream(marker,FileMode.CreateNew,FileAccess.Write)){claim.Flush(true);}
        var context=Path.Combine(root,"fixture.json");
        if(phase=="create")
        {
            var key=RandomNumberGenerator.GetBytes(32);
            try
            {
                var f=new Fixture(Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),Android.OS.Process.MyPid(),Convert.ToHexString(SHA256.HashData(key)));
                var binding=new DatabaseKeyBinding(f.Profile,f.Installation,f.Generation);
                File.WriteAllText(context,JsonSerializer.Serialize(f));
                File.WriteAllBytes(Path.Combine(root,"generation.bin"),AndroidDatabaseKeyProtector.ProtectGeneration(key,binding));
                File.WriteAllBytes(Path.Combine(root,"recovered.bin"),AndroidDatabaseKeyProtector.ProtectRecovered(key,binding));
                CheckOriginal(root,f,binding);
            }
            finally{CryptographicOperations.ZeroMemory(key);}
            return Report(root,phase,new {OriginalGenerationAndRecovery=true,NoKeyExport=true});
        }
        var saved=JsonSerializer.Deserialize<Fixture>(File.ReadAllText(context))??throw new IOException("Missing fixture.");
        var original=new DatabaseKeyBinding(saved.Profile,saved.Installation,saved.Generation);
        if(phase=="cold")
        {
            if(saved.CreatePid==Android.OS.Process.MyPid())throw new IOException("Actual cold process required.");
            CheckOriginal(root,saved,original);
            var count=0;
            foreach(var recovered in new[]{false,true})
            {
                var envelope=File.ReadAllBytes(Path.Combine(root,recovered?"recovered.bin":"generation.bin"));
                byte[] Unwrap(byte[] bytes,DatabaseKeyBinding binding)=>recovered
                    ?AndroidDatabaseKeyProtector.UnprotectRecovered(bytes,binding):AndroidDatabaseKeyProtector.Unprotect(bytes,binding);
                void Refused(byte[] bytes,DatabaseKeyBinding binding)
                {
                    try{var invalid=Unwrap(bytes,binding);CryptographicOperations.ZeroMemory(invalid);}
                    catch(CryptographicException){count++;return;}
                    throw new IOException("Changed original binding accepted.");
                }
                Refused(envelope,new DatabaseKeyBinding(Guid.NewGuid(),saved.Installation,saved.Generation));
                Refused(envelope,new DatabaseKeyBinding(saved.Profile,Guid.NewGuid(),saved.Generation));
                Refused(envelope,new DatabaseKeyBinding(saved.Profile,saved.Installation,Guid.NewGuid()));
                var changed=(byte[])envelope.Clone();changed[^1]^=1;Refused(changed,original);
                changed=(byte[])envelope.Clone();changed[5]^=1;Refused(changed,original);
                Refused(envelope[..^1],original);
            }
            if(count!=12)throw new IOException("Incomplete native refusal cohort.");
            return Report(root,phase,new {DifferentProcess=true,OriginalKeysReopened=true,Refusals=count});
        }
        if(!File.Exists(Path.Combine(root,"cold.result.json")))throw new IOException("Original cold receipt required.");
        CheckOriginal(root,saved,original);
        var denied=0;var removed=0;
        foreach(var recovered in new[]{false,true})
        {
            var envelope=File.ReadAllBytes(Path.Combine(root,recovered?"recovered.bin":"generation.bin"));
            var alias=AndroidDatabaseKeyProtector.CaptureIndependentAlias(envelope,original,recovered);
            try{AndroidDatabaseKeyProtector.RemoveCapturedIndependentAlias(alias,()=>throw new IOException("No durable retirement authority."));}
            catch(IOException){denied++;}
            // The rejected authority must not have removed or replaced the original native key.
            CheckOne(envelope,saved,original,recovered);
            var receipt=Path.Combine(root,recovered?"recovery-retirement":"generation-retirement");
            using(var durable=new FileStream(receipt,FileMode.CreateNew,FileAccess.Write)){durable.WriteByte(1);durable.Flush(true);}
            AndroidDatabaseKeyProtector.RemoveCapturedIndependentAlias(alias,()=>
            {if(!File.Exists(receipt))throw new IOException("Retirement receipt missing.");});
            AndroidDatabaseKeyProtector.RequireIndependentAliasAbsent(alias);
            try{CheckOne(envelope,saved,original,recovered);throw new IOException("Lost alias was recreated.");}
            catch(CryptographicException){removed++;}
            AndroidDatabaseKeyProtector.RequireIndependentAliasAbsent(alias);
        }
        if(denied!=2||removed!=2)throw new IOException("Incomplete original retirement.");
        return Report(root,phase,new {DeniedRetirement=denied,OwnedAliasesRemoved=removed,NoLostKeyRecreation=true});
    }

    /// <summary>Verifies both original envelope purposes and reports only OS key metadata.</summary>
    private static void CheckOriginal(string root,Fixture f,DatabaseKeyBinding binding)
    {
        var levels=new List<int?>();
        foreach(var recovered in new[]{false,true})
        {
            var bytes=File.ReadAllBytes(Path.Combine(root,recovered?"recovered.bin":"generation.bin"));
            CheckOne(bytes,f,binding,recovered);
            var alias=AndroidDatabaseKeyProtector.CaptureIndependentAlias(bytes,binding,recovered);
            levels.Add(AndroidDatabaseKeyProtector.ReadSecurityLevel(alias));
        }
        File.WriteAllText(Path.Combine(root,"reported-levels.json"),JsonSerializer.Serialize(levels));
    }

    /// <summary>Compares only in process and erases the unwrapped fictitious data key before returning.</summary>
    private static void CheckOne(byte[] envelope,Fixture f,DatabaseKeyBinding binding,bool recovered)
    {
        var key=recovered?AndroidDatabaseKeyProtector.UnprotectRecovered(envelope,binding):AndroidDatabaseKeyProtector.Unprotect(envelope,binding);
        try{if(Convert.ToHexString(SHA256.HashData(key))!=f.ExpectedDigest)throw new CryptographicException("Original key changed.");}
        finally{CryptographicOperations.ZeroMemory(key);}
    }

    /// <summary>Persists a phase receipt in private helper storage and emits only nonsecret facts.</summary>
    private static string Report(string root,string phase,object facts)
    {
        var report=JsonSerializer.Serialize(new {Phase=phase,Pid=Android.OS.Process.MyPid(),Facts=facts,
            ReportedLevels=JsonSerializer.Deserialize<int?[]>(File.ReadAllText(Path.Combine(root,"reported-levels.json")))});
        File.WriteAllText(Path.Combine(root,phase+".result.json"),report);return report;
    }
}
