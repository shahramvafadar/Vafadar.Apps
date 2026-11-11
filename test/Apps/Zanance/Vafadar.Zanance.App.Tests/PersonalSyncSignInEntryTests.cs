using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Sync;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Entry-time sign-in inputs cannot be replaced by busy or input-clearing feedback.</summary>
public sealed class PersonalSyncSignInEntryTests
{
    /// <summary>Changed request inputs refuse transport construction while an unchanged request reaches its local refusal port.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public async Task Original_sign_in_owns_inputs_before_fallible_entry_feedback(int scenario)
    {
        using var fixture = new FlowFixture();
        using var environment = new PersonalSyncFlowTests.EnvironmentPort();
        var model = new PersonalSyncViewModel(fixture.Services.GetRequiredService<PersonalSyncStore>(),
            fixture.Services.GetRequiredService<PersonalSyncCoordinator>(), environment, fixture.Platform,
            fixture.Translator, new DateFormatter(fixture.Localization), fixture.Localization);
        await model.ActivateAsync();
        model.Email = "fictitious@example.invalid";
        model.Password = "fictitious-request-only";
        var fired = false;
        void Mutate(string? property, bool changed)
        {
            if (fired || scenario == 5) return;
            if ((scenario is 0 or 4 && property == nameof(model.IsBusy) && !changed) ||
                (scenario == 1 && property == nameof(model.IsBusy) && changed) ||
                (scenario == 2 && property == nameof(model.Password) && !changed) ||
                (scenario == 3 && property == nameof(model.Password) && changed))
            {
                fired = true;
                switch (scenario)
                {
                    case 0: model.Address = "https://127.0.0.1:5099/"; break;
                    case 1: model.Email = "newer@example.invalid"; break;
                    case 2: model.Fingerprint = new string('2', 64); break;
                    case 3: model.DeviceName = "Newer fictitious device"; break;
                    case 4: model.Password = "refused-late-fictitious-input"; break;
                }
            }
        }
        model.PropertyChanging += (_, e) => Mutate(e.PropertyName, false);
        model.PropertyChanged += (_, e) => Mutate(e.PropertyName, true);
        try
        {
            await model.SignInCommand.ExecuteAsync(null);
            Assert.Equal(scenario != 5, fired);
            Assert.Equal(scenario == 5 ? 1 : 0, environment.Opens);
            Assert.Empty(model.Password);
            Assert.False(model.HasPendingOperation);
            Assert.True(model.CanInteract);
            Assert.Equal(2, environment.Captures);
            Assert.Equal(0, environment.RememberedPeers);
            Assert.Equal(0, environment.ConnectionsChanged);
            if (scenario == 0) Assert.Equal("https://127.0.0.1:5099/", model.Address);
            if (scenario == 1) Assert.Equal("newer@example.invalid", model.Email);
            if (scenario == 2) Assert.Equal(new string('2', 64), model.Fingerprint);
            if (scenario == 3) Assert.Equal("Newer fictitious device", model.DeviceName);
        }
        finally { model.Deactivate(); }
    }
}
