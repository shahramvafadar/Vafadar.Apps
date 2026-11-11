using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Sync;
using Vafadar.Zanance.Core.Sync;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Accepted nonfinancial sync metadata survives failed terminal busy publication without primary replay.</summary>
public sealed class PersonalSyncAcceptedPublicationTests
{
    /// <summary>Only an explicit source reread can release an accepted operation's failed follow-through.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Accepted_pause_retains_its_receipt_when_final_busy_feedback_fails(bool changing)
    {
        using var fixture = new FlowFixture();
        using var environment = new PersonalSyncFlowTests.EnvironmentPort();
        var token = TestContext.Current.CancellationToken;
        var store = fixture.Services.GetRequiredService<PersonalSyncStore>();
        await store.ConnectAsync(await store.ReviewAsync(token), new("https://127.0.0.1:5048/", new('1', 64)),
            "fictitious-busy-receipt-owner", Guid.NewGuid(),
            new(Guid.NewGuid(), "Fictitious busy receipt input", SyncSchema.Hash, 0, false), token);
        var model = new PersonalSyncViewModel(store, fixture.Services.GetRequiredService<PersonalSyncCoordinator>(),
            environment, fixture.Platform, fixture.Translator, new DateFormatter(fixture.Localization), fixture.Localization);
        await model.ActivateAsync();
        model.NewProfileName = "Fictitious unsubmitted receipt draft";
        fixture.Platform.Confirmations.Enqueue(true);
        var failures = 0;
        void Fail(string? name)
        {
            if (name != nameof(model.IsBusy) || failures != 0 || model.IsBusy != changing) return;
            failures++;
            throw new IOException("Fictitious accepted busy publication failure.");
        }
        PropertyChangingEventHandler before = (_, e) => { if (changing) Fail(e.PropertyName); };
        PropertyChangedEventHandler after = (_, e) => { if (!changing) Fail(e.PropertyName); };
        model.PropertyChanging += before; model.PropertyChanged += after;
        try { await model.PauseCommand.ExecuteAsync(null); }
        finally { model.PropertyChanging -= before; model.PropertyChanged -= after; }
        try
        {
            var accepted = await store.StatusAsync(token);
            Assert.Equal(1, failures);
            Assert.False(accepted.Connected);
            Assert.Equal(fixture.Translator["Sync_AcceptedFollowUpFailed"], model.Error);
            Assert.False(model.IsReady);
            Assert.False(model.CanInteract);
            Assert.True(model.CanRead);
            Assert.True(model.CanLeave);
            await model.PauseCommand.ExecuteAsync(null);
            Assert.Equal(1, environment.ConnectionsChanged);
            Assert.Equal(accepted, await store.StatusAsync(token));
            await model.ReloadCommand.ExecuteAsync(null);
            Assert.True(model.IsReady);
            Assert.Null(model.Error);
            Assert.Equal(1, environment.ConnectionsChanged);
            Assert.Equal("Fictitious unsubmitted receipt draft", model.NewProfileName);
            Assert.Empty(await fixture.Store.GetEntriesAsync(cancellationToken: token));
        }
        finally { model.Deactivate(); }
    }
}
