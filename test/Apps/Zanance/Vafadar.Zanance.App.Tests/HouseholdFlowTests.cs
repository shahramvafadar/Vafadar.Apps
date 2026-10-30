using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Zanance.App.Features.Sharing;
using Vafadar.Zanance.App.Features.Sync;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Profiles;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Sync;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Sharing;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Original household page lifecycle/read isolation with real SQLite and explicit native-effect ports; not native encrypted acceptance.</summary>
[Trait("AT", "AT-294")]
public sealed class HouseholdFlowTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static HouseholdViewModel Create(FlowFixture f, PersonalSyncFlowTests.EnvironmentPort environment, Tokens tokens, HouseholdJournal? journal = null, ProfileService? profileService = null)
    {
        var profiles = profileService ?? new ProfileService(f.Services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>(), f.Store,
            f.Preferences, new UnusedRuntime(), f.Translator);
        return new(f.Services.GetRequiredService<HouseholdWorkspace>(), journal ?? f.Services.GetRequiredService<HouseholdJournal>(),
            f.Services.GetRequiredService<HouseholdSyncCoordinator>(), f.Services.GetRequiredService<PersonalSyncStore>(), f.Store,
            environment, tokens, profiles, f.Platform, f.Platform, f.Translator, f.Time,
            f.Services.GetRequiredService<HouseholdProjectionStore>(), new HouseholdProjectionHandoff());
    }

    [Fact]
    public async Task Original_first_run_close_keeps_other_actions_and_leave_blocked_until_its_native_return_finishes()
    {
        using var f = new FlowFixture(); using var environment = new PersonalSyncFlowTests.EnvironmentPort(); var tokens = new Tokens();
        var page = Create(f, environment, tokens); await page.ActivateAsync();
        var returning = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var closes = 0;
        page.BeginOnboarding(async () => { closes++; entered.TrySetResult(); await returning.Task; }, () => Task.CompletedTask);
        var close = page.CloseCommand.ExecuteAsync(null);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10), Ct); page.IsBusy = false;
            Assert.False(page.CanInteract); Assert.False(page.CanLeave);
            await page.CloseCommand.ExecuteAsync(null);
            Assert.Equal(1, closes); Assert.Equal(0, tokens.Reads); Assert.Equal(0, environment.Opens);
        }
        finally { returning.TrySetResult(); await close; page.Deactivate(); }
    }

    [Fact]
    public async Task Original_initial_local_peer_read_failure_is_covered_and_can_retry_without_authentication()
    {
        using var f = new FlowFixture(); using var environment = new PersonalSyncFlowTests.EnvironmentPort { FailConnectionRead = true }; var tokens = new Tokens();
        var page = Create(f, environment, tokens);
        var failure = await Record.ExceptionAsync(page.ActivateAsync);
        Assert.Null(failure); Assert.False(page.IsReady); Assert.False(page.CanInteract);
        Assert.Equal(f.Translator["Household_ActionFailed"], page.Error);
        environment.FailConnectionRead = false; await page.ReloadCommand.ExecuteAsync(null);
        Assert.True(page.IsReady); Assert.Equal("https://127.0.0.1:5048/", page.Address);
        Assert.Equal(0, tokens.Reads); Assert.Equal(0, environment.Opens); page.Deactivate();
    }

    [Fact]
    public async Task Changed_household_disclosure_cannot_accept_the_old_native_new_profile_prompt()
    {
        using var f = new FlowFixture(); using var environment = new PersonalSyncFlowTests.EnvironmentPort(); var tokens = new Tokens();
        var page = Create(f, environment, tokens); await page.ActivateAsync();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var answer = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        f.Platform.PendingInput = () => { entered.TrySetResult(); return answer.Task; };
        var command = page.CreateSeparateProfileCommand.ExecuteAsync(null);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10), Ct);
            f.Localization.SetLanguage(f.Localization.SupportedLanguages.Single(item => item.CultureName == "de"));
            answer.TrySetResult("Fictitious retired household destination"); await command;
            Assert.Null(page.Error); Assert.Equal(0, f.Platform.ProfileOpened);
            Assert.Equal(0, tokens.Reads); Assert.Equal(0, environment.Opens);
        }
        finally { answer.TrySetResult(null); await command; page.Deactivate(); }
    }

    [Fact]
    public async Task Failed_original_household_reload_covers_old_controls_and_preserves_literal_peer_and_name_for_retry()
    {
        using var f = new FlowFixture(); using var environment = new PersonalSyncFlowTests.EnvironmentPort(); var tokens = new Tokens();
        var page = Create(f, environment, tokens); await page.ActivateAsync();
        page.Address = "https://127.0.0.1:5099/"; page.Fingerprint = new('2', 64); page.Name = "";
        environment.FailCapture = true; await page.ReloadCommand.ExecuteAsync(null);
        Assert.False(page.IsReady); Assert.False(page.CanInteract);
        Assert.Equal("https://127.0.0.1:5099/", page.Address); Assert.Empty(page.Name);
        environment.FailCapture = false; await page.ReloadCommand.ExecuteAsync(null);
        Assert.True(page.IsReady); Assert.Equal("https://127.0.0.1:5099/", page.Address);
        Assert.Equal(new string('2', 64), page.Fingerprint); Assert.Empty(page.Name);
        Assert.Equal(1, environment.Reads); Assert.Equal(0, tokens.Reads); Assert.Equal(0, environment.Opens); page.Deactivate();
    }

    [Fact]
    public async Task Replaced_original_root_refuses_the_old_native_household_profile_prompt_without_stale_error_publication()
    {
        using var f = new FlowFixture(); using var environment = new PersonalSyncFlowTests.EnvironmentPort(); var tokens = new Tokens();
        var page = Create(f, environment, tokens); var originalRoot = true; page.BindRootOwner(() => originalRoot); await page.ActivateAsync();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var answer = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        f.Platform.PendingInput = () => { entered.TrySetResult(); return answer.Task; };
        var command = page.CreateSeparateProfileCommand.ExecuteAsync(null);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10), Ct); originalRoot = false;
            answer.TrySetResult("Fictitious old household target"); await command;
            Assert.False(page.CanInteract); Assert.Null(page.Error); Assert.Equal(0, f.Platform.ProfileOpened);
            Assert.Equal(0, tokens.Reads); Assert.Equal(0, environment.Opens);
        }
        finally { answer.TrySetResult(null); await command; page.Deactivate(); }
    }

    [Fact]
    public async Task Retired_household_root_cannot_invoke_a_late_first_run_close_or_reactivate_its_source()
    {
        using var f = new FlowFixture(); using var environment = new PersonalSyncFlowTests.EnvironmentPort(); var tokens = new Tokens();
        var page = Create(f, environment, tokens); await page.ActivateAsync(); var closes = 0;
        page.BeginOnboarding(() => { closes++; return Task.CompletedTask; }, () => Task.CompletedTask);
        page.RetireRoot(); var captures = environment.Captures;
        await page.CloseCommand.ExecuteAsync(null); await page.ActivateAsync();
        Assert.Equal(0, closes); Assert.Equal(captures, environment.Captures);
        Assert.False(page.CanInteract); Assert.Equal(0, tokens.Reads); Assert.Equal(0, environment.Opens);
    }

    [Fact]
    public async Task Local_household_retry_does_not_parse_or_replace_an_unsubmitted_literal_peer_draft()
    {
        using var f = new FlowFixture(); using var environment = new PersonalSyncFlowTests.EnvironmentPort(); var tokens = new Tokens();
        var page = Create(f, environment, tokens); await page.ActivateAsync();
        page.Address = ""; page.Fingerprint = "  literal unsubmitted pin  "; page.Name = "  Fictitious draft  ";
        await page.ReloadCommand.ExecuteAsync(null);
        Assert.True(page.IsReady); Assert.Null(page.Error);
        Assert.Empty(page.Address); Assert.Equal("  literal unsubmitted pin  ", page.Fingerprint);
        Assert.Equal("  Fictitious draft  ", page.Name); Assert.Equal(1, environment.Reads);
        Assert.Equal(0, tokens.Reads); Assert.Equal(0, environment.Opens); page.Deactivate();
    }

    [Fact]
    public async Task Pending_original_household_read_can_leave_without_a_late_snapshot_or_second_factory_read()
    {
        using var f = new FlowFixture(); using var environment = new PersonalSyncFlowTests.EnvironmentPort(); var tokens = new Tokens();
        var factory = new PendingReadFactory(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>());
        var page = Create(f, environment, tokens, new(factory, f.Time));
        var pending = page.ActivateAsync(); await factory.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10), Ct);
        Assert.True(page.CanLeave); Assert.False(page.CanInteract); page.IsBusy = false;
        Assert.False(page.CanInteract); page.Deactivate(); factory.Release.TrySetResult();
        await pending.WaitAsync(TimeSpan.FromSeconds(10), Ct);
        Assert.False(page.IsReady); Assert.False(page.CanInteract); Assert.Null(page.Error);
        Assert.Equal(1, factory.Requests); Assert.Equal(0, factory.Created);
        Assert.Equal(0, tokens.Reads); Assert.Equal(0, environment.Opens);
    }

    [Fact]
    public async Task Original_household_appearance_coalesces_its_pending_read_instead_of_cancelling_and_restarting_it()
    {
        using var f = new FlowFixture(); using var environment = new PersonalSyncFlowTests.EnvironmentPort(); var tokens = new Tokens();
        var factory = new PendingReadFactory(f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>());
        var page = Create(f, environment, tokens, new(factory, f.Time));
        var first = page.ActivateAsync(); await factory.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10), Ct);
        var sameAppearance = page.ActivateAsync(); Assert.False(sameAppearance.IsCompleted); Assert.Equal(1, factory.Requests);
        factory.Release.TrySetResult(); await Task.WhenAll(first, sameAppearance).WaitAsync(TimeSpan.FromSeconds(10), Ct);
        Assert.True(page.IsReady); Assert.True(page.CanInteract); Assert.Equal(1, factory.Created);
        Assert.Equal(1, environment.Reads); Assert.Equal(0, tokens.Reads); Assert.Equal(0, environment.Opens); page.Deactivate();
    }

    [Fact]
    public async Task Failed_original_household_close_keeps_a_covered_read_opportunity_without_repeating_the_native_return()
    {
        using var f = new FlowFixture(); using var environment = new PersonalSyncFlowTests.EnvironmentPort(); var tokens = new Tokens();
        var page = Create(f, environment, tokens); await page.ActivateAsync(); var closes = 0;
        page.Name = "  Fictitious retained household name  ";
        page.BeginOnboarding(() => { closes++; throw new IOException("Fictitious native return failure."); }, () => Task.CompletedTask);
        Assert.Null(await Record.ExceptionAsync(() => page.CloseCommand.ExecuteAsync(null)));
        Assert.False(page.IsReady); Assert.False(page.CanInteract); Assert.True(page.CanRead); Assert.True(page.CanLeave);
        Assert.Equal(f.Translator["Household_ActionFailed"], page.Error);
        await page.ReloadCommand.ExecuteAsync(null);
        Assert.True(page.IsReady); Assert.Equal(1, closes); Assert.Equal("  Fictitious retained household name  ", page.Name);
        Assert.Equal(0, tokens.Reads); Assert.Equal(0, environment.Opens); page.Deactivate();
    }

    [Fact]
    public async Task Known_household_profile_creation_survives_a_failed_target_open_and_failed_read_without_another_creation()
    {
        using var f = new FlowFixture(); using var environment = new PersonalSyncFlowTests.EnvironmentPort(); var tokens = new Tokens();
        var runtime = new MetadataRuntime { FailSecondExclusive = true };
        var profiles = new ProfileService(f.Services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>(), f.Store,
            f.Preferences, runtime, f.Translator);
        var page = Create(f, environment, tokens, profileService: profiles); await page.ActivateAsync();
        f.Platform.Inputs.Enqueue("Fictitious acknowledged household target"); await page.CreateSeparateProfileCommand.ExecuteAsync(null);
        Assert.Equal(2, profiles.All.Count); Assert.True(profiles.Current.IsMain); Assert.Equal(2, runtime.ExclusiveCalls);
        Assert.False(page.IsReady); Assert.False(page.CanInteract); Assert.True(page.CanRead);
        Assert.Equal(f.Translator["Sync_AcceptedFollowUpFailed"], page.Error);
        environment.FailCapture = true; await page.ReloadCommand.ExecuteAsync(null);
        Assert.False(page.IsReady); Assert.Equal(f.Translator["Sync_AcceptedFollowUpFailed"], page.Error);
        environment.FailCapture = false; await page.ReloadCommand.ExecuteAsync(null);
        Assert.True(page.IsReady); Assert.Equal(2, profiles.All.Count); Assert.Equal(2, runtime.ExclusiveCalls);
        Assert.Empty(await f.Store.GetAccountsAsync(cancellationToken: Ct)); Assert.Empty(await f.Store.GetEntriesAsync(cancellationToken: Ct));
        Assert.Equal(0, tokens.Reads); Assert.Equal(0, environment.Opens); page.Deactivate();
    }

    [Fact]
    public async Task Unknown_household_catalogue_publication_outcome_is_covered_without_fabricating_an_acknowledgement()
    {
        using var f = new FlowFixture(); using var environment = new PersonalSyncFlowTests.EnvironmentPort(); var tokens = new Tokens();
        var runtime = new MetadataRuntime();
        var profiles = new ProfileService(f.Services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>(), f.Store,
            f.Preferences, runtime, f.Translator);
        profiles.Changed += (_, _) => throw new IOException("Fictitious callback after actual catalogue publication.");
        var page = Create(f, environment, tokens, profileService: profiles); await page.ActivateAsync();
        f.Platform.Inputs.Enqueue("Fictitious uncertain household target"); await page.CreateSeparateProfileCommand.ExecuteAsync(null);
        Assert.Equal(2, profiles.All.Count); Assert.Equal(1, runtime.ExclusiveCalls);
        Assert.False(page.IsReady); Assert.True(page.CanRead); Assert.Equal(f.Translator["Household_ActionFailed"], page.Error);
        await page.ReloadCommand.ExecuteAsync(null);
        Assert.True(page.IsReady); Assert.Equal(2, profiles.All.Count); Assert.Equal(1, runtime.ExclusiveCalls);
        Assert.Equal(0, f.Platform.ProfileOpened); Assert.Equal(0, tokens.Reads); Assert.Equal(0, environment.Opens); page.Deactivate();
    }

    [Fact]
    public async Task Original_household_profile_writer_rechecks_its_native_owner_after_the_exclusive_wait()
    {
        using var f = new FlowFixture(); using var environment = new PersonalSyncFlowTests.EnvironmentPort(); var tokens = new Tokens();
        var originalRoot = true; var runtime = new MetadataRuntime { BeforeExclusive = () => originalRoot = false };
        var profiles = new ProfileService(f.Services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>(), f.Store,
            f.Preferences, runtime, f.Translator);
        var page = Create(f, environment, tokens, profileService: profiles); page.BindRootOwner(() => originalRoot); await page.ActivateAsync();
        f.Platform.Inputs.Enqueue("Fictitious retired writer target"); await page.CreateSeparateProfileCommand.ExecuteAsync(null);
        Assert.Single(profiles.All); Assert.Null(page.Error); Assert.False(page.CanInteract);
        Assert.Equal(1, runtime.ExclusiveCalls); Assert.Equal(0, f.Platform.ProfileOpened);
        Assert.Equal(0, tokens.Reads); Assert.Equal(0, environment.Opens); page.Deactivate();
    }

    [Fact]
    public async Task Visible_household_source_error_survives_another_appearance_until_explicit_Reload()
    {
        using var f = new FlowFixture(); using var environment = new PersonalSyncFlowTests.EnvironmentPort { FailConnectionRead = true }; var tokens = new Tokens();
        var page = Create(f, environment, tokens); await page.ActivateAsync();
        Assert.False(page.IsReady); var error = page.Error; var captures = environment.Captures;
        environment.FailConnectionRead = false; await page.ActivateAsync();
        Assert.Equal(error, page.Error); Assert.False(page.IsReady); Assert.Equal(captures, environment.Captures); Assert.Equal(1, environment.Reads);
        await page.ReloadCommand.ExecuteAsync(null);
        Assert.True(page.IsReady); Assert.Null(page.Error); Assert.Equal(2, environment.Reads);
        Assert.Equal(0, tokens.Reads); Assert.Equal(0, environment.Opens); page.Deactivate();
    }

    [Fact]
    public void Construction_cannot_open_credentials_or_publish_a_default_household_role()
    {
        using var f = new FlowFixture(); using var environment = new PersonalSyncFlowTests.EnvironmentPort(); var tokens = new Tokens();
        var page = Create(f, environment, tokens);
        Assert.False(page.IsReady); Assert.False(page.CanInteract); Assert.False(page.IsOwner); Assert.False(page.CanSetUp);
        Assert.Empty(page.Members); Assert.Empty(page.Pending); Assert.Equal(0, tokens.Reads); Assert.Equal(0, environment.Captures);
    }

    [Fact]
    public async Task Failed_initial_access_keeps_the_original_household_form_covered_and_retry_does_not_sign_in()
    {
        using var f = new FlowFixture(); using var environment = new PersonalSyncFlowTests.EnvironmentPort { FailCapture = true }; var tokens = new Tokens();
        var page = Create(f, environment, tokens); await page.ActivateAsync();
        Assert.False(page.IsReady); Assert.Equal(f.Translator["Household_ActionFailed"], page.Error);
        environment.FailCapture = false; await page.ReloadCommand.ExecuteAsync(null);
        Assert.True(page.IsReady); Assert.False(page.CanSetUp); Assert.False(page.CanSynchronize); Assert.Equal(0, tokens.Reads);
        Assert.Equal(0, environment.Opens); Assert.Empty(page.Members); page.Deactivate();
    }

    [Fact]
    public async Task An_existing_personal_binding_and_financial_data_cannot_be_relabelled_by_opening_household_setup()
    {
        using var f = new FlowFixture(); using var environment = new PersonalSyncFlowTests.EnvironmentPort(); var tokens = new Tokens();
        var account = new Account { Name = "Fictitious private account", CurrencyCode = "EUR", OpeningBalance = 4567, OpeningDate = new(2026, 10, 1) };
        await f.Store.SaveAccountAsync(account, Ct);
        var original = f.Services.GetRequiredService<PersonalSyncStore>();
        await original.ConnectAsync(await original.ReviewAsync(Ct), new("https://127.0.0.1:5048/", new('1', 64)), "fictitious-owner",
            Guid.NewGuid(), new(Guid.NewGuid(), "Fictitious private sync", SyncSchema.Hash, 0, false), Ct);
        var before = await original.ReviewAsync(Ct); var page = Create(f, environment, tokens);
        await page.ActivateAsync(); await page.BeginOwnerCommand.ExecuteAsync(null);
        Assert.False(page.CanSetUp); Assert.False(page.ShowSetup); Assert.Equal(f.Translator["Household_SeparateRequired"], page.StatusText);
        Assert.Equal(before, await original.ReviewAsync(Ct)); Assert.Equal(0, tokens.Reads); Assert.Empty(f.Platform.ConfirmationDialogs);
        Assert.Equal(4567, (await f.Store.GetAccountsAsync(cancellationToken: Ct)).Single().OpeningBalance); page.Deactivate();
    }

    [Fact]
    public async Task Retiring_the_page_clears_visible_invitation_and_password_without_a_credential_or_financial_read()
    {
        using var f = new FlowFixture(); using var environment = new PersonalSyncFlowTests.EnvironmentPort(); var tokens = new Tokens();
        var page = Create(f, environment, tokens); await page.ActivateAsync();
        page.InvitationText = "Fictitious private invitation"; page.Password = "Fictitious in-memory password"; page.ConfirmedRoot = new('a', 64);
        page.RecoveryPassword = "Fictitious separate file password"; page.RecoveryPasswordConfirm = page.RecoveryPassword;
        page.Deactivate();
        Assert.Empty(page.InvitationText); Assert.Empty(page.Password); Assert.Empty(page.ConfirmedRoot);
        Assert.Empty(page.RecoveryPassword); Assert.Empty(page.RecoveryPasswordConfirm); Assert.False(page.CanEnableRecovery);
        Assert.False(page.CanImportRecovery); Assert.False(page.CanExportRecovery); Assert.False(page.IsOwnerRecoveryDraft);
        Assert.False(page.CanInteract); Assert.Equal(0, tokens.Reads); Assert.Equal(0, environment.Opens);
    }

    [Fact]
    public async Task A_household_transport_kind_opens_the_household_route_instead_of_enabling_personal_financial_publication()
    {
        using var f = new FlowFixture(); using var environment = new PersonalSyncFlowTests.EnvironmentPort();
        var original = f.Services.GetRequiredService<PersonalSyncStore>();
        await original.ConnectAsync(await original.ReviewAsync(Ct), new("https://127.0.0.1:5048/", new('1', 64)), "fictitious-owner",
            Guid.NewGuid(), new(Guid.NewGuid(), "Fictitious bound profile", SyncSchema.Hash, 0, false), Ct);
        // Even an unverified/corrupt kind marker must never select readable financial HTTP.
        // This is deliberately not accepted household membership or a decryption grant.
        await using (var db = await f.Services.GetRequiredService<IDbContextFactory<ZananceDbContext>>().CreateDbContextAsync(Ct))
            await db.Database.ExecuteSqlRawAsync("UPDATE SyncBinding SET TransportKind=1", Ct);
        var page = new PersonalSyncViewModel(original, f.Services.GetRequiredService<PersonalSyncCoordinator>(), environment,
            f.Platform, f.Translator, new DateFormatter(f.Localization), f.Localization);
        await page.ActivateAsync();
        Assert.True(page.IsHouseholdConnection); Assert.False(page.CanConnect); Assert.False(page.CanSync);
        Assert.False(page.ShowPersonalConnection); Assert.False(page.ShowPersonalSignedIn); Assert.True(page.HouseholdReviewEmpty);
        Assert.Equal(f.Translator["Household_Review_Title"], page.PageTitle); Assert.Equal("HouseholdReview", page.HelpTopic);
        await page.SignInCommand.ExecuteAsync(null); await page.DiscoverCommand.ExecuteAsync(null);
        await page.PauseCommand.ExecuteAsync(null); await page.SignOutCommand.ExecuteAsync(null);
        Assert.Empty(f.Platform.ConfirmationDialogs);
        Assert.Equal(0, environment.Opens);
        await page.OpenHouseholdCommand.ExecuteAsync(null);
        Assert.Equal(App.AppRoutes.HouseholdRoute, Assert.Single(f.Platform.Routes).Route);
        Assert.Equal(0, environment.Opens); page.Deactivate();
    }

    [Fact]
    public async Task Personal_source_accounts_are_explicit_and_page_retirement_discards_unsaved_choices_without_financial_or_identity_effects()
    {
        using var f = new FlowFixture(); using var environment = new PersonalSyncFlowTests.EnvironmentPort(); var tokens = new Tokens();
        var account = new Account { Name = "Fictitious source only", CurrencyCode = "EUR", OpeningBalance = 654321, OpeningDate = new(2026, 1, 1) };
        await f.Store.SaveAccountAsync(account, Ct);
        var page = Create(f, environment, tokens); await page.ActivateAsync(); await page.LoadSourceAccountsCommand.ExecuteAsync(null);
        Assert.Null(page.Error); var choice = Assert.Single(page.SourceAccounts); Assert.Equal(account.Id, choice.Id); Assert.False(choice.Selected);
        Assert.False(page.SourceHistory); Assert.False(page.SourceAttachments); Assert.False(page.HasSourceReview); Assert.False(page.HasSourceHandoff);
        choice.Selected = true; page.SourceAttachments = true; Assert.True(page.SourceHistory);
        page.SourceHistory = false; Assert.False(page.SourceAttachments);
        page.Deactivate(); Assert.Empty(page.SourceAccounts); Assert.False(page.HasSourceReview); Assert.False(page.CanChooseSource);
        var retained = Assert.Single(await f.Store.GetAccountsAsync(cancellationToken: Ct));
        Assert.Equal(account.Id, retained.Id); Assert.Equal(account.OpeningBalance, retained.OpeningBalance);
        Assert.Equal(0, tokens.Reads); Assert.Equal(0, environment.Opens); Assert.Empty(f.Platform.ConfirmationDialogs);
    }

    /// <summary>Holds only actual original context creation; no household projection or authentication is replaced.</summary>
    private sealed class PendingReadFactory(IDbContextFactory<ZananceDbContext> original) : IDbContextFactory<ZananceDbContext>
    {
        internal readonly TaskCompletionSource Entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal readonly TaskCompletionSource Release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal int Requests, Created;
        public ZananceDbContext CreateDbContext() { Created++; return original.CreateDbContext(); }
        public async Task<ZananceDbContext> CreateDbContextAsync(CancellationToken ct = default)
        {
            Requests++; Entered.TrySetResult(); await Release.Task.WaitAsync(ct); ct.ThrowIfCancellationRequested(); return CreateDbContext();
        }
    }

    /// <summary>Only the original native credential effect is counted; no authentication or encryption algorithm is replaced.</summary>
    private sealed class Tokens : IPersonalSyncTokenStore
    {
        internal int Reads;
        public Task<PersonalSyncSession?> ReadAsync(string peer, CancellationToken ct) { Reads++; throw new InvalidOperationException("No credential read belongs to this local page action."); }
        public Task WriteAsync(string peer, PersonalSyncSession session, CancellationToken ct) => throw new InvalidOperationException();
        public Task RemoveAsync(string peer, CancellationToken ct) => throw new InvalidOperationException();
    }
    /// <summary>No native profile transition belongs to constructor/local-read/retirement checks.</summary>
    private sealed class MetadataRuntime : IProfileRuntime
    {
        internal int ExclusiveCalls;
        internal bool FailSecondExclusive;
        internal Action? BeforeExclusive;
        public async Task RunExclusiveAsync(Func<Task> action)
        {
            ExclusiveCalls++; BeforeExclusive?.Invoke();
            if (FailSecondExclusive && ExclusiveCalls == 2) throw new IOException("Fictitious target-open failure after original creation.");
            await action();
        }
        public Task MigrateAsync(DatabaseAccessGate.ExclusiveLease owner) => throw new InvalidOperationException("No target migration belongs to this metadata test.");
        public Task<bool> AuthenticateAsync(string reason) => throw new InvalidOperationException("No authentication belongs to this metadata test.");
        public Task ReloadLockAsync() => throw new InvalidOperationException("No target lock belongs to this metadata test.");
        public void DismissUndo() { }
    }

    /// <summary>No native profile transition belongs to constructor/local-read/retirement checks.</summary>
    private sealed class UnusedRuntime : IProfileRuntime
    {
        public Task RunExclusiveAsync(Func<Task> action) => throw new InvalidOperationException();
        public Task MigrateAsync(DatabaseAccessGate.ExclusiveLease owner) => throw new InvalidOperationException();
        public Task<bool> AuthenticateAsync(string reason) => throw new InvalidOperationException();
        public Task ReloadLockAsync() => throw new InvalidOperationException();
        public void DismissUndo() => throw new InvalidOperationException();
    }
}
