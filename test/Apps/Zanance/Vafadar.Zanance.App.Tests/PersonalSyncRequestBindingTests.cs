using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Sync;
using Vafadar.Zanance.Data.Sync;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Original request normalization must clear nullable input without bypassing presentation authority.</summary>
public sealed class PersonalSyncRequestBindingTests
{
    /// <summary>Current/retired empty input and retired nonempty input retain the actual model's ownership contract.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task Original_adapter_clears_null_and_preserves_retired_request_refusal(int scenario)
    {
        using var fixture = new FlowFixture();
        using var environment = new PersonalSyncFlowTests.EnvironmentPort();
        var model = new PersonalSyncViewModel(fixture.Services.GetRequiredService<PersonalSyncStore>(),
            fixture.Services.GetRequiredService<PersonalSyncCoordinator>(), environment, fixture.Platform,
            fixture.Translator, new DateFormatter(fixture.Localization), fixture.Localization);
        var adapter = new PersonalSyncRequestTextConverter();
        var culture = CultureInfo.GetCultureInfo("fa-IR");
        var raw = "  fictitious\u00A0request\u200F  ";
        await model.ActivateAsync();
        try
        {
            var original = adapter.ConvertBack(raw, typeof(string), null, culture);
            Assert.Same(raw, original);
            Assert.Same(raw, adapter.Convert(raw, typeof(string), null, culture));
            model.Password = Assert.IsType<string>(original);
            Assert.Equal(raw, model.Password);
            if (scenario != 0) model.Deactivate();
            var input = adapter.ConvertBack(scenario == 2 ? raw : null, typeof(string), null, culture);
            var escaped = Record.Exception(() => model.Password = Assert.IsType<string>(input));
            Assert.Null(escaped);
            Assert.Empty(model.Password);
            Assert.Equal(scenario == 0, model.CanInteract);
            Assert.Equal(1, environment.Captures);
            Assert.Equal(0, environment.Opens);
            Assert.False(model.HasPendingOperation);
        }
        finally { model.Deactivate(); }
    }
}
