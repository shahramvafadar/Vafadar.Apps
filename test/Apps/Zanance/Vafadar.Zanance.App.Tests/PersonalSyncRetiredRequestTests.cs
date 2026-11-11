using System.ComponentModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Sync;
using Vafadar.Zanance.Data.Sync;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Late original input callbacks cannot retain request text after their presentation owner retires.</summary>
public sealed class PersonalSyncRetiredRequestTests
{
    /// <summary>Retired visibility/root/input notifications leave no request text or escaped native feedback.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public async Task Late_request_input_cannot_repopulate_a_retired_presentation(int scenario)
    {
        using var fixture = new FlowFixture();
        using var environment = new PersonalSyncFlowTests.EnvironmentPort();
        var model = Create(fixture, environment);
        var ownsRoot = true;
        model.BindRootOwner(() => ownsRoot);
        await model.ActivateAsync();
        if (scenario == 1) model.RetireRoot();
        else if (scenario == 2) ownsRoot = false;
        else model.Deactivate();
        var feedback = 0;
        PropertyChangingEventHandler before = (_, e) =>
        {
            if (scenario != 3 || e.PropertyName != nameof(model.Password)) return;
            feedback++;
            throw new IOException("Fictitious retired input changing feedback.");
        };
        PropertyChangedEventHandler after = (_, e) =>
        {
            if (scenario != 4 || e.PropertyName != nameof(model.Password)) return;
            feedback++;
            throw new IOException("Fictitious retired input changed feedback.");
        };
        model.PropertyChanging += before; model.PropertyChanged += after;
        Exception? escaped;
        try { escaped = Record.Exception(() => model.Password = "fictitious late request input"); }
        finally { model.PropertyChanging -= before; model.PropertyChanged -= after; }
        try
        {
            Assert.Null(escaped);
            Assert.Empty(model.Password);
            Assert.False(model.CanRead);
            Assert.False(model.HasPendingOperation);
            Assert.Equal(1, environment.Captures);
            Assert.Equal(0, environment.Opens);
            if (scenario >= 3) Assert.Equal(1, feedback);
            Assert.Empty(await fixture.Store.GetEntriesAsync(cancellationToken: TestContext.Current.CancellationToken));
        }
        finally { model.Deactivate(); }
    }

    /// <summary>Ordinary active input retains the original notification failure contract.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Active_request_input_does_not_suppress_unrelated_feedback(bool changed)
    {
        using var fixture = new FlowFixture();
        using var environment = new PersonalSyncFlowTests.EnvironmentPort();
        var model = Create(fixture, environment);
        await model.ActivateAsync();
        PropertyChangingEventHandler before = (_, e) =>
        { if (!changed && e.PropertyName == nameof(model.Password)) throw new IOException("Fictitious active feedback."); };
        PropertyChangedEventHandler after = (_, e) =>
        { if (changed && e.PropertyName == nameof(model.Password)) throw new IOException("Fictitious active feedback."); };
        model.PropertyChanging += before; model.PropertyChanged += after;
        try { Assert.IsType<IOException>(Record.Exception(() => model.Password = "fictitious active request input")); }
        finally { model.PropertyChanging -= before; model.PropertyChanged -= after; model.Deactivate(); }
    }

    /// <summary>The disabled original form cannot accept a late value while read/cover/close authority is unavailable.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task Covered_or_pending_request_input_cannot_bypass_original_form_authority(int scenario)
    {
        using var fixture = new FlowFixture();
        using var environment = new PersonalSyncFlowTests.EnvironmentPort
        { FailConnectionRead = scenario == 1, Available = scenario != 2 };
        var factory = scenario == 0 ? new HoldingFactory() : null;
        var store = factory is null ? null : new PersonalSyncStore(factory,
            fixture.Services.GetRequiredService<ICommercialWriteAccessSource>());
        var model = Create(fixture, environment, store);
        var close = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        model.BeginOnboarding(() => { entered.TrySetResult(); return close.Task; }, () => Task.CompletedTask);
        var activation = model.ActivateAsync();
        Task? departure = null;
        try
        {
            if (factory is not null)
            {
                await factory.Entered.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
                model.IsBusy = false; // Shared feedback cannot authorize a field while its original command owns the form.
            }
            else await activation;
            if (scenario == 3)
            {
                departure = model.CloseOnboardingCommand.ExecuteAsync(null);
                await entered.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
            }
            Assert.False(model.CanInteract);
            var escaped = Record.Exception(() => model.Password = "fictitious unavailable-form input");
            Assert.Null(escaped);
            Assert.Empty(model.Password);
            Assert.Equal(0, environment.Opens);
            Assert.Empty(await fixture.Store.GetEntriesAsync(cancellationToken: TestContext.Current.CancellationToken));
        }
        finally
        {
            close.TrySetResult();
            if (departure is not null) await departure.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
            model.Deactivate();
            await activation.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        }
    }

    /// <summary>Original command cleanup still publishes an empty field after interaction authority closes.</summary>
    [Fact]
    public async Task Empty_request_cleanup_still_notifies_a_disabled_original_binding()
    {
        using var fixture = new FlowFixture();
        using var environment = new PersonalSyncFlowTests.EnvironmentPort();
        var model = Create(fixture, environment);
        await model.ActivateAsync();
        model.Password = "fictitious current request";
        environment.Available = false;
        Assert.False(model.CanInteract);
        var cleared = 0;
        PropertyChangedEventHandler observer = (_, e) =>
        { if (e.PropertyName == nameof(model.Password) && model.Password.Length == 0) cleared++; };
        model.PropertyChanged += observer;
        try
        {
            model.Password = "";
            Assert.Empty(model.Password);
            Assert.Equal(1, cleared);
        }
        finally { model.PropertyChanged -= observer; model.Deactivate(); }
    }

    private static PersonalSyncViewModel Create(FlowFixture fixture, PersonalSyncFlowTests.EnvironmentPort environment,
        PersonalSyncStore? store = null) =>
        new(store ?? fixture.Services.GetRequiredService<PersonalSyncStore>(), fixture.Services.GetRequiredService<PersonalSyncCoordinator>(),
            environment, fixture.Platform, fixture.Translator, new DateFormatter(fixture.Localization), fixture.Localization);

    private sealed class HoldingFactory : IDbContextFactory<ZananceDbContext>
    {
        internal TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public ZananceDbContext CreateDbContext() => throw new InvalidOperationException("No synchronous context.");
        public async Task<ZananceDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        {
            Entered.TrySetResult();
            return await new TaskCompletionSource<ZananceDbContext>(TaskCreationOptions.RunContinuationsAsynchronously)
                .Task.WaitAsync(cancellationToken);
        }
    }
}
