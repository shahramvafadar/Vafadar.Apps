using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Sync;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Repairing failed idle feedback cannot lend its callback a competing original read.</summary>
public sealed class PersonalSyncFeedbackReentryTests
{
    /// <summary>Explicit recovery owns feedback until its changing callback has returned.</summary>
    [Fact]
    public async Task Idle_feedback_reentry_is_refused_before_original_access_capture()
    {
        using var fixture = new FlowFixture();
        using var environment = new PersonalSyncFlowTests.EnvironmentPort();
        var model = new PersonalSyncViewModel(fixture.Services.GetRequiredService<PersonalSyncStore>(),
            fixture.Services.GetRequiredService<PersonalSyncCoordinator>(), environment, fixture.Platform,
            fixture.Translator, new DateFormatter(fixture.Localization), fixture.Localization);
        await model.ActivateAsync();
        var failed = false;
        PropertyChangingEventHandler failure = (_, e) =>
        {
            if (e.PropertyName != nameof(model.IsBusy) || !model.IsBusy || failed) return;
            failed = true;
            throw new IOException("Fictitious failed original idle publication.");
        };
        model.PropertyChanging += failure;
        try { await model.ReloadCommand.ExecuteAsync(null); }
        finally { model.PropertyChanging -= failure; }
        Assert.True(failed);
        var before = environment.Captures;
        var entered = false;
        var capturesInCallback = -1;
        Task? nested = null;
        PropertyChangingEventHandler reenter = (_, e) =>
        {
            if (e.PropertyName != nameof(model.IsBusy) || !model.IsBusy || entered) return;
            entered = true;
            nested = model.ReloadCommand.ExecuteAsync(null);
            capturesInCallback = environment.Captures;
        };
        model.PropertyChanging += reenter;
        try { await model.ReloadCommand.ExecuteAsync(null); }
        finally { model.PropertyChanging -= reenter; }
        if (nested is not null) await nested.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        try
        {
            Assert.True(entered);
            Assert.Equal(before, capturesInCallback);
            Assert.Equal(before + 1, environment.Captures);
            Assert.False(model.HasPendingOperation);
            Assert.True(model.CanRead);
            Assert.Equal(0, environment.Opens);
            Assert.Empty(await fixture.Store.GetEntriesAsync(cancellationToken: TestContext.Current.CancellationToken));
        }
        finally { model.Deactivate(); }
    }
}
