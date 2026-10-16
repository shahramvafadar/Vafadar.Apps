using Vafadar.Zanance.App.Features.Commerce.Billing;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Actual optional identity form with native-effect ports; no credentials or financial data are persisted.</summary>
[Trait("AT", "AT-295")]
public sealed class BillingIdentityFlowTests
{
    /// <summary>All invalid inputs publish together; canceling registration and logout has no account effect in any language.</summary>
    [Theory]
    [InlineData("en")][InlineData("fa")][InlineData("de")][InlineData("es")][InlineData("fr")][InlineData("it")]
    public async Task Invalid_fields_and_canceled_identity_actions_never_submit(string language)
    {
        using var f = new FlowFixture(); f.Localization.SetLanguage(f.Localization.SupportedLanguages.Single(l => l.CultureName == language));
        var before = await GoalChooserFlowTests.SnapshotAsync(f); var effects = new Effects();
        var model = new BillingIdentityViewModel(effects, f.Translator, f.Platform); await model.ActivateAsync();
        foreach (var mode in Enumerable.Range(0, 5))
        {
            model.ModeIndex = mode; await model.SubmitCommand.ExecuteAsync(null);
            Assert.Equal(model.ShowEmail, model.EmailError is not null);
            Assert.Equal(model.ShowPassword, model.PasswordError is not null);
            Assert.Equal(model.ShowChallenge, model.ChallengeError is not null);
            Assert.Equal(model.ShowChallenge, model.TokenError is not null);
        }
        model.ModeIndex = 1; model.Email = "fictional-identity@example.invalid"; model.Password = "Fictional-only-test-passphrase";
        await model.SubmitCommand.ExecuteAsync(null); Assert.Single(f.Platform.ConfirmationDialogs); Assert.Equal(0, effects.Submissions);
        await model.SignOutCommand.ExecuteAsync(null); Assert.Equal(2, f.Platform.ConfirmationDialogs.Count); Assert.True(model.Connected);
        Assert.Equal(0, effects.Submissions); model.Deactivate(); Assert.Empty(model.Password); Assert.Empty(model.Token);
        Assert.False(model.CanSubmit); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
    }

    /// <summary>Secrets leave the form before network wait and a retired response cannot publish onto the next form.</summary>
    [Fact]
    public async Task Retired_signin_response_cannot_publish_and_secrets_are_cleared_before_await()
    {
        using var f = new FlowFixture(); var effects = new Effects();
        var pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); effects.SignIn = pending.Task;
        var model = new BillingIdentityViewModel(effects, f.Translator, f.Platform); await model.ActivateAsync();
        model.Email = "fictional-identity@example.invalid"; model.Password = "Fictional-only-test-passphrase";
        var original = model.SubmitCommand.ExecuteAsync(null); Assert.Empty(model.Password); Assert.True(model.IsBusy);
        model.Deactivate(); effects.Connected = false; await model.ActivateAsync(); Assert.False(model.Connected);
        pending.SetResult(); await original; Assert.False(model.Connected); Assert.Null(model.Status); Assert.True(model.CanSubmit);
        Assert.Equal(1, effects.Submissions); model.Deactivate();
    }

    private sealed class Effects : IBillingIdentityEnvironment
    {
        public bool Available => true;
        internal bool Connected = true;
        internal int Submissions;
        internal Task SignIn = Task.CompletedTask;
        public Task<bool> ConnectedAsync(CancellationToken ct) => Task.FromResult(Connected);
        public Task SignInAsync(string email, string password, CancellationToken ct) { Submissions++; return SignIn; }
        public Task<bool> SignOutAsync(CancellationToken ct) { Submissions++; Connected = false; return Task.FromResult(true); }
        public Task RegisterAsync(string email, string password, CancellationToken ct) { Submissions++; return Task.CompletedTask; }
        public Task RecoverAsync(string email, CancellationToken ct) { Submissions++; return Task.CompletedTask; }
        public Task CompleteAsync(Guid challenge, string token, string? newPassword, CancellationToken ct) { Submissions++; return Task.CompletedTask; }
    }
}
