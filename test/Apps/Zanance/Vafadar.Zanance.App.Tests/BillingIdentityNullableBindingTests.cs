using Vafadar.Zanance.App.Features.Commerce.Billing;

namespace Vafadar.Zanance.App.Tests;

/// <summary>All original identity modes validate cleared bindings before consent or any primary effect.</summary>
public sealed class BillingIdentityNullableBindingTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private sealed class ReadOnlyEnvironment : IBillingIdentityEnvironment
    {
        internal int Reads, Primary;
        public bool Available => true;
        public Task<bool> ConnectedAsync(CancellationToken ct) { Reads++; return Task.FromResult(false); }
        private Task Refuse() { Primary++; throw new InvalidOperationException("An invalid draft cannot submit an identity request."); }
        public Task SignInAsync(string email, string password, CancellationToken ct) => Refuse();
        public Task RegisterAsync(string email, string password, CancellationToken ct) => Refuse();
        public Task RecoverAsync(string email, CancellationToken ct) => Refuse();
        public Task CompleteAsync(Guid challenge, string token, string? newPassword, CancellationToken ct) => Refuse();
        public Task<bool> SignOutAsync(CancellationToken ct) { Primary++; throw new InvalidOperationException(); }
    }

    /// <summary>Each mode includes four null bindings and its original all-empty control.</summary>
    public static TheoryData<int, string> Inputs
    {
        get
        {
            var rows = new TheoryData<int, string>();
            foreach (var mode in Enumerable.Range(0, 5))
                foreach (var input in new[] { "email", "password", "challenge", "token", "empty" }) rows.Add(mode, input);
            return rows;
        }
    }

    /// <summary>Invalid visible and hidden inputs keep raw nulls and produce only the current required-field errors.</summary>
    [Theory]
    [MemberData(nameof(Inputs))]
    public async Task Cleared_identity_fields_never_escape_original_validation(int mode, string field)
    {
        using var f = new FlowFixture(); await f.Store.GetSettingsAsync(Ct);
        var before = await GoalChooserFlowTests.SnapshotAsync(f);
        var environment = new ReadOnlyEnvironment();
        var model = new BillingIdentityViewModel(environment, f.Translator, f.Platform);
        try
        {
            await model.ActivateAsync(); Assert.True(model.CanSubmit); model.ModeIndex = mode;
            switch (field)
            {
                case "email": model.Email = null!; break;
                case "password": model.Password = null!; break;
                case "challenge": model.Challenge = null!; break;
                case "token": model.Token = null!; break;
                case "empty": break;
                default: throw new InvalidOperationException();
            }
            await model.SubmitCommand.ExecuteAsync(null); model.RefreshDisplay();
            Assert.Equal(model.ShowEmail, model.EmailError is not null);
            Assert.Equal(model.ShowPassword, model.PasswordError is not null);
            Assert.Equal(model.ShowChallenge, model.ChallengeError is not null);
            Assert.Equal(model.ShowChallenge, model.TokenError is not null);
            Assert.Equal(field == "email" ? null : string.Empty, model.Email);
            Assert.Equal(field == "password" ? null : string.Empty, model.Password);
            Assert.Equal(field == "challenge" ? null : string.Empty, model.Challenge);
            Assert.Equal(field == "token" ? null : string.Empty, model.Token);
            Assert.True(model.IsReady); Assert.True(model.CanSubmit); Assert.False(model.HasPendingOperation);
            Assert.Null(model.Status); Assert.Equal(1, environment.Reads); Assert.Equal(0, environment.Primary);
            Assert.Empty(f.Platform.ConfirmationDialogs);
            Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        }
        finally { model.Deactivate(); }
        Assert.Empty(model.Password); Assert.Empty(model.Token); Assert.False(model.CanSubmit);
    }
}
