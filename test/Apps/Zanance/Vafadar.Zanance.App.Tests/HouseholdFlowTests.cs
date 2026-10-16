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
    private static HouseholdViewModel Create(FlowFixture f, PersonalSyncFlowTests.EnvironmentPort environment, Tokens tokens)
    {
        var profiles = new ProfileService(f.Services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>(), f.Store,
            f.Preferences, new UnusedRuntime(), f.Translator);
        return new(f.Services.GetRequiredService<HouseholdWorkspace>(), f.Services.GetRequiredService<HouseholdJournal>(),
            f.Services.GetRequiredService<HouseholdSyncCoordinator>(), f.Services.GetRequiredService<PersonalSyncStore>(), f.Store,
            environment, tokens, profiles, f.Platform, f.Platform, f.Translator, f.Time,
            f.Services.GetRequiredService<HouseholdProjectionStore>(), new HouseholdProjectionHandoff());
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

    /// <summary>Only the original native credential effect is counted; no authentication or encryption algorithm is replaced.</summary>
    private sealed class Tokens : IPersonalSyncTokenStore
    {
        internal int Reads;
        public Task<PersonalSyncSession?> ReadAsync(string peer, CancellationToken ct) { Reads++; throw new InvalidOperationException("No credential read belongs to this local page action."); }
        public Task WriteAsync(string peer, PersonalSyncSession session, CancellationToken ct) => throw new InvalidOperationException();
        public Task RemoveAsync(string peer, CancellationToken ct) => throw new InvalidOperationException();
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
