using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Sync;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Original first-run departure cannot retain its owner after failed preview feedback.</summary>
public sealed class PersonalSyncDepartureFeedbackTests
{
    /// <summary>A refused departure releases its closing guard and leaves an explicit local recovery opportunity.</summary>
    [Fact]
    public async Task Close_feedback_failure_does_not_strand_the_original_modal()
    {
        using var fixture = new FlowFixture();
        using var environment = new PersonalSyncFlowTests.EnvironmentPort();
        var model = new PersonalSyncViewModel(fixture.Services.GetRequiredService<PersonalSyncStore>(),
            fixture.Services.GetRequiredService<PersonalSyncCoordinator>(), environment, fixture.Platform,
            fixture.Translator, new DateFormatter(fixture.Localization), fixture.Localization);
        var closes = 0;
        model.BeginOnboarding(() => { closes++; return Task.CompletedTask; }, () => Task.CompletedTask);
        await model.ActivateAsync();
        var captures = environment.Captures;
        var failures = 0;
        PropertyChangedEventHandler fault = (_, e) =>
        {
            if (e.PropertyName != nameof(model.HasAttachmentPreview) || failures != 0) return;
            failures++;
            throw new IOException("Fictitious original departure feedback failure.");
        };
        model.PropertyChanged += fault;
        Exception? escaped;
        try { escaped = await Record.ExceptionAsync(() => model.CloseOnboardingCommand.ExecuteAsync(null)); }
        finally { model.PropertyChanged -= fault; }
        try
        {
            Assert.Equal(1, failures);
            Assert.Null(escaped);
            Assert.Equal(0, closes);
            Assert.False(model.HasPendingOperation);
            Assert.True(model.CanRead);
            Assert.True(model.CanLeave);
            Assert.True(model.CanCloseOnboarding);
            Assert.False(model.IsReady);
            Assert.NotNull(model.Error);
            Assert.Equal(captures, environment.Captures);
            await model.ReloadCommand.ExecuteAsync(null);
            Assert.True(model.IsReady);
            await model.CloseOnboardingCommand.ExecuteAsync(null);
            Assert.Equal(1, closes);
            Assert.Equal(0, environment.Opens);
            Assert.Empty(await fixture.Store.GetEntriesAsync(cancellationToken: TestContext.Current.CancellationToken));
        }
        finally { model.Deactivate(); }
    }
}
