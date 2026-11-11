using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Sync;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Retiring original request/preview fields cannot retain data behind failed native feedback.</summary>
public sealed class PersonalSyncDepartureInputTests
{
    /// <summary>Changing/changed failures and reentrant old bindings cannot preserve or repopulate retired input.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public async Task Departure_clears_request_and_preview_before_reporting_feedback_failure(int scenario)
    {
        using var fixture = new FlowFixture();
        using var environment = new PersonalSyncFlowTests.EnvironmentPort();
        var model = new PersonalSyncViewModel(fixture.Services.GetRequiredService<PersonalSyncStore>(),
            fixture.Services.GetRequiredService<PersonalSyncCoordinator>(), environment, fixture.Platform,
            fixture.Translator, new DateFormatter(fixture.Localization), fixture.Localization);
        var closes = 0;
        model.BeginOnboarding(() => { closes++; return Task.CompletedTask; }, () => Task.CompletedTask);
        await model.ActivateAsync();
        using var frame = new PersonalSyncRenderedPage(1, [1, 2, 3]);
        using var repopulated = new PersonalSyncRenderedPage(1, [4, 5, 6]);
        model.AttachmentPreview = frame;
        model.Password = "fictitious request-only input";
        var captures = environment.Captures;
        var request = scenario is 0 or 1 or 4;
        var changing = scenario % 2 == 0 || scenario >= 4;
        var failures = 0;
        void Fail(string? name)
        {
            if (name != (request ? nameof(model.Password) : nameof(model.AttachmentPreview)) || failures != 0) return;
            failures++;
            if (scenario == 4) model.Password = "fictitious stale callback";
            else if (scenario == 5) model.AttachmentPreview = repopulated;
            else throw new IOException("Fictitious departure input feedback failure.");
        }
        PropertyChangingEventHandler before = (_, e) => { if (changing) Fail(e.PropertyName); };
        PropertyChangedEventHandler after = (_, e) => { if (!changing) Fail(e.PropertyName); };
        model.PropertyChanging += before; model.PropertyChanged += after;
        Exception? escaped;
        try { escaped = await Record.ExceptionAsync(() => model.CloseOnboardingCommand.ExecuteAsync(null)); }
        finally { model.PropertyChanging -= before; model.PropertyChanged -= after; }
        try
        {
            Assert.Equal(1, failures);
            Assert.Null(escaped);
            Assert.Equal(0, closes);
            Assert.Empty(model.Password);
            Assert.Null(model.AttachmentPreview);
            Assert.All(frame.Data, value => Assert.Equal(0, value));
            Assert.True(model.CanRead);
            Assert.True(model.CanLeave);
            Assert.False(model.IsReady);
            Assert.NotNull(model.Error);
            Assert.Equal(captures, environment.Captures);
            Assert.Equal(0, environment.Opens);
            Assert.Empty(await fixture.Store.GetEntriesAsync(cancellationToken: TestContext.Current.CancellationToken));
        }
        finally { model.Deactivate(); }
    }
}
