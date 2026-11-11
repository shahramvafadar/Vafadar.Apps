using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Sync;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Original local sync reads release command ownership even when busy feedback cannot be published.</summary>
public sealed class PersonalSyncPublicationOwnershipTests
{
    /// <summary>Beginning and ending changing/changed faults must not strand native Back or a later activation.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task Busy_publication_failure_does_not_retain_a_finished_command(int scenario)
    {
        using var fixture = new FlowFixture();
        using var environment = new PersonalSyncFlowTests.EnvironmentPort();
        var model = new PersonalSyncViewModel(fixture.Services.GetRequiredService<PersonalSyncStore>(),
            fixture.Services.GetRequiredService<PersonalSyncCoordinator>(), environment, fixture.Platform,
            fixture.Translator, new DateFormatter(fixture.Localization), fixture.Localization);
        var failures = 0;
        var changing = scenario % 2 == 0;
        var ending = scenario >= 2;
        void Fail(string? name)
        {
            if (name != nameof(model.IsBusy) || failures != 0) return;
            // Changing sees the previous state; Changed sees the new state.
            if (model.IsBusy != (changing ? ending : !ending)) return;
            failures++;
            throw new IOException("Fictitious original busy publication failure.");
        }
        PropertyChangingEventHandler before = (_, e) => { if (changing) Fail(e.PropertyName); };
        PropertyChangedEventHandler after = (_, e) => { if (!changing) Fail(e.PropertyName); };
        model.PropertyChanging += before; model.PropertyChanged += after;
        Exception? escaped;
        try
        {
            escaped = await Record.ExceptionAsync(model.ActivateAsync);
        }
        finally { model.PropertyChanging -= before; model.PropertyChanged -= after; }
        try
        {
            Assert.Equal(1, failures);
            Assert.Null(escaped);
            Assert.False(model.HasPendingOperation);
            Assert.True(model.CanLeave);
            model.Deactivate();
            await model.ActivateAsync().WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
            Assert.True(model.IsReady);
            Assert.True(model.CanRead);
            Assert.Equal(0, environment.Opens);
            Assert.Equal(0, environment.ConnectionsChanged);
            Assert.Empty(await fixture.Store.GetEntriesAsync(cancellationToken: TestContext.Current.CancellationToken));
        }
        finally { model.Deactivate(); }
    }
}
