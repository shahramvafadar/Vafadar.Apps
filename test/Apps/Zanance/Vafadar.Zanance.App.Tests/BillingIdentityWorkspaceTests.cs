using Vafadar.Zanance.App.Features.Commerce.Billing;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Original optional identity read and native consent ownership; all effects are isolated and fictitious.</summary>
public sealed class BillingIdentityWorkspaceTests
{
    [Fact]
    public async Task Actual_appearance_coalesces_the_original_pending_session_read()
    {
        using var f = new FlowFixture(); var pending = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var effects = new Effects { Session = _ => pending.Task }; var vm = new BillingIdentityViewModel(effects,f.Translator,f.Platform);
        var first = vm.ActivateAsync(); var second = vm.ActivateAsync();
        try { Assert.Equal(1,effects.SessionReads); }
        finally { pending.TrySetResult(false); await first; await second; vm.Deactivate(); }
    }

    [Fact]
    public async Task Changed_actual_translator_retires_the_original_signout_consent()
    {
        using var f = new FlowFixture(); var effects = new Effects(); var vm = new BillingIdentityViewModel(effects,f.Translator,f.Platform);
        await vm.ActivateAsync(); var pending = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        f.Platform.PendingConfirmation = () => pending.Task; var action = vm.SignOutCommand.ExecuteAsync(null);
        Assert.Single(f.Platform.ConfirmationDialogs);
        f.Localization.SetLanguage(f.Localization.SupportedLanguages.Single(x=>x.CultureName=="de"));
        pending.SetResult(true); await action; Assert.Equal(0,effects.Signouts); vm.Deactivate();
    }

    [Fact]
    public async Task Changed_literal_registration_input_cannot_submit_the_old_native_consent()
    {
        using var f = new FlowFixture(); var effects = new Effects(); var vm = new BillingIdentityViewModel(effects,f.Translator,f.Platform);
        await vm.ActivateAsync(); vm.ModeIndex=1; vm.Email="original-fixture@example.invalid"; vm.Password="Fictional-only-test-passphrase";
        var pending = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        f.Platform.PendingConfirmation=()=>pending.Task; var action=vm.SubmitCommand.ExecuteAsync(null);
        Assert.Single(f.Platform.ConfirmationDialogs); vm.Email="changed-fixture@example.invalid";
        pending.SetResult(true); await action; Assert.Equal(0,effects.Registrations); vm.Deactivate();
    }

    [Theory]
    [InlineData(true)][InlineData(false)]
    public async Task Returned_signout_acknowledgement_survives_failed_session_refresh_and_readonly_retry(bool remoteConfirmed)
    {
        using var f = new FlowFixture(); var effects = new Effects { RemoteConfirmed=remoteConfirmed };
        var vm = new BillingIdentityViewModel(effects,f.Translator,f.Platform); await vm.ActivateAsync();
        effects.Session=_=>Task.FromException<bool>(new IOException("Controlled private-session read failure."));
        f.Platform.Confirmations.Enqueue(true); await vm.SignOutCommand.ExecuteAsync(null);
        Assert.Equal(1,effects.Signouts); Assert.False(vm.IsReady); Assert.False(vm.CanSubmit); Assert.True(vm.CanRead);
        Assert.Contains(f.Translator[remoteConfirmed?"BillingIdentity_SignedOut":"BillingIdentity_LocalSignOut"],vm.Status!);
        Assert.Contains(f.Translator["BillingIdentity_FollowUpFailed"],vm.Status!);
        effects.Session=null; await vm.RefreshSessionCommand.ExecuteAsync(null);
        Assert.True(vm.IsReady); Assert.False(vm.Connected); Assert.Equal(1,effects.Signouts);
        Assert.DoesNotContain(f.Translator["BillingIdentity_FollowUpFailed"],vm.Status!); vm.Deactivate();
    }

    [Fact]
    public async Task Unknown_primary_failure_is_not_an_acknowledged_request_and_retry_only_reads_session()
    {
        using var f=new FlowFixture(); var effects=new Effects { SignIn=Task.FromException(new IOException("Controlled unknown original request.")) };
        var vm=new BillingIdentityViewModel(effects,f.Translator,f.Platform); await vm.ActivateAsync();
        vm.Email="fixture@example.invalid"; vm.Password="Fictional-only-test-passphrase"; await vm.SubmitCommand.ExecuteAsync(null);
        Assert.Equal(1,effects.SignIns); Assert.False(vm.IsReady); Assert.True(vm.CanRead);
        Assert.Equal(f.Translator["BillingIdentity_Unavailable"],vm.Status); Assert.Empty(vm.Password);
        await vm.RefreshSessionCommand.ExecuteAsync(null); Assert.Equal(1,effects.SignIns); Assert.True(vm.IsReady); Assert.Null(vm.Status); vm.Deactivate();
    }

    [Fact]
    public async Task Newer_request_only_draft_survives_original_return_and_receipt_is_labelled_previous()
    {
        using var f=new FlowFixture(); var pending=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var effects=new Effects { SignIn=pending.Task }; var vm=new BillingIdentityViewModel(effects,f.Translator,f.Platform); await vm.ActivateAsync();
        vm.Email="original@example.invalid"; vm.Password="Fictional-only-test-passphrase"; var action=vm.SubmitCommand.ExecuteAsync(null);
        Assert.Empty(vm.Password); vm.Email="newer@example.invalid"; vm.Password="Fictional-newer-draft-only";
        pending.SetResult(); await action;
        Assert.Equal("newer@example.invalid",vm.Email); Assert.Equal("Fictional-newer-draft-only",vm.Password);
        Assert.Contains(f.Translator["BillingIdentity_PreviousResult"],vm.Status!); Assert.Equal(1,effects.SignIns); vm.Deactivate(); Assert.Empty(vm.Password);
    }

    [Fact]
    public async Task Independent_original_submission_blocks_direct_reentry_even_if_shared_busy_changes()
    {
        using var f=new FlowFixture(); var pending=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var effects=new Effects { SignIn=pending.Task }; var vm=new BillingIdentityViewModel(effects,f.Translator,f.Platform); await vm.ActivateAsync();
        vm.Email="fixture@example.invalid"; vm.Password="Fictional-only-test-passphrase"; var action=vm.SubmitCommand.ExecuteAsync(null);
        vm.IsBusy=false; Assert.True(vm.HasPendingOperation); Assert.False(vm.CanLeave); Assert.False(vm.CanRead);
        await vm.SubmitCommand.ExecuteAsync(null); await vm.RefreshSessionCommand.ExecuteAsync(null); await vm.ActivateAsync();
        Assert.Equal(1,effects.SignIns); Assert.Equal(1,effects.SessionReads); pending.SetResult(); await action; vm.Deactivate();
    }

    [Theory]
    [InlineData(false)][InlineData(true)]
    public async Task Replaced_root_retires_original_registration_consent_and_never_reactivates(bool retire)
    {
        using var f=new FlowFixture(); var effects=new Effects(); var vm=new BillingIdentityViewModel(effects,f.Translator,f.Platform);
        var owns=true;vm.BindRootOwner(()=>owns);await vm.ActivateAsync();vm.ModeIndex=1;vm.Email="fixture@example.invalid";vm.Password="Fictional-only-test-passphrase";
        var pending=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);f.Platform.PendingConfirmation=()=>pending.Task;
        var action=vm.SubmitCommand.ExecuteAsync(null);if(retire)vm.RetireRoot();else owns=false;
        pending.SetResult(true);await action;var reads=effects.SessionReads;await vm.ActivateAsync();
        Assert.Equal(0,effects.Registrations);Assert.Equal(reads,effects.SessionReads);Assert.False(vm.CanSubmit);vm.Deactivate();Assert.Empty(vm.Password);
    }

    [Fact]
    public async Task Visible_source_failure_preserves_nonsecret_raw_inputs_and_requires_explicit_read_retry()
    {
        using var f=new FlowFixture();var effects=new Effects {Session=_=>Task.FromException<bool>(new IOException("Controlled read failure."))};
        var vm=new BillingIdentityViewModel(effects,f.Translator,f.Platform){Email=" raw@example.invalid ",Challenge=" raw-challenge "};
        await vm.ActivateAsync();Assert.False(vm.IsReady);Assert.True(vm.CanRead);var reads=effects.SessionReads;
        effects.Session=null;await vm.ActivateAsync();Assert.Equal(reads,effects.SessionReads);Assert.False(vm.IsReady);
        await vm.RefreshSessionCommand.ExecuteAsync(null);Assert.True(vm.IsReady);Assert.Equal(" raw@example.invalid ",vm.Email);Assert.Equal(" raw-challenge ",vm.Challenge);vm.Deactivate();
    }

    [Theory]
    [InlineData("en")][InlineData("fa")][InlineData("de")][InlineData("es")][InlineData("fr")][InlineData("it")]
    public void New_session_read_and_acknowledgement_feedback_has_complete_real_translations(string language)
    {
        using var f=new FlowFixture();f.Localization.SetLanguage(f.Localization.SupportedLanguages.Single(x=>x.CultureName==language));
        foreach(var key in new[]{"BillingIdentity_ReadSession","BillingIdentity_FollowUpFailed","BillingIdentity_PreviousResult"})
        { var text=f.Translator[key];Assert.NotEqual(key,text);Assert.False(string.IsNullOrWhiteSpace(text)); }
    }

    [Fact]
    public async Task Confirmed_signout_clears_old_request_only_secrets_before_network_and_preserves_newer_draft()
    {
        using var f=new FlowFixture();var pending=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var effects=new Effects { SignOut=pending.Task };var vm=new BillingIdentityViewModel(effects,f.Translator,f.Platform);await vm.ActivateAsync();
        vm.Password="Fictional-old-input-only";vm.Token="Fictional-old-token-only";f.Platform.Confirmations.Enqueue(true);
        var action=vm.SignOutCommand.ExecuteAsync(null);Assert.Empty(vm.Password);Assert.Empty(vm.Token);
        vm.Password="Fictional-newer-input-only";pending.SetResult(true);await action;
        Assert.Equal("Fictional-newer-input-only",vm.Password);Assert.Contains(f.Translator["BillingIdentity_PreviousResult"],vm.Status!);vm.Deactivate();
    }

    [Fact]
    public async Task Native_confirmation_failure_never_submits_and_releases_original_form()
    {
        using var f=new FlowFixture();var effects=new Effects();var vm=new BillingIdentityViewModel(effects,f.Translator,f.Platform);await vm.ActivateAsync();
        f.Platform.PendingConfirmation=()=>Task.FromException<bool>(new IOException("Controlled native presentation failure."));
        await vm.SignOutCommand.ExecuteAsync(null);Assert.Equal(0,effects.Signouts);Assert.True(vm.CanSignOut);Assert.True(vm.CanLeave);
        Assert.Equal(f.Translator["BillingIdentity_Unavailable"],vm.Status);vm.Deactivate();
    }

    /// <summary>Records only requests to the original interface; it has no credential store, network or real identity.</summary>
    private sealed class Effects : IBillingIdentityEnvironment
    {
        public bool Available=>true;
        internal int SessionReads;
        internal int Signouts;
        internal int Registrations;
        internal int SignIns;
        internal bool Connected=true;
        internal bool RemoteConfirmed=true;
        internal Task SignIn=Task.CompletedTask;
        internal Task<bool>? SignOut;
        internal Func<CancellationToken,Task<bool>>? Session;
        public Task<bool> ConnectedAsync(CancellationToken ct) { SessionReads++; return Session?.Invoke(ct)??Task.FromResult(Connected); }
        public Task<bool> SignOutAsync(CancellationToken ct) { Signouts++; Connected=false; return SignOut??Task.FromResult(RemoteConfirmed); }
        public Task RegisterAsync(string email,string password,CancellationToken ct) { Registrations++; return Task.CompletedTask; }
        public Task SignInAsync(string email,string password,CancellationToken ct){SignIns++;return SignIn;}
        public Task RecoverAsync(string email,CancellationToken ct)=>Task.CompletedTask;
        public Task CompleteAsync(Guid challenge,string token,string? newPassword,CancellationToken ct)=>Task.CompletedTask;
    }
}
