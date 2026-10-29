using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Features.Accounts;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Tests;

/// <summary>D-639: requested original account identities cannot silently become a new financial form.</summary>
public sealed class AccountEditorOriginalSourceTests
{
    /// <summary>Refuses an unavailable requested edit before exposing any unrelated new-account input.</summary>
    [Fact]
    public async Task Requested_missing_account_never_exposes_an_unrelated_new_account_form()
    {
        using var fixture = new FlowFixture();
        var editor = new AccountEditorViewModel(fixture.Store,
            fixture.Services.GetRequiredService<PlanStore>(), fixture.Translator,
            fixture.Localization, fixture.Time, fixture.Platform);

        await editor.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = Guid.NewGuid() });

        Assert.False(editor.CanInteract, "An unavailable original edit must remain covered instead of becoming a new account.");
        Assert.Empty(await fixture.Store.GetAccountsAsync(cancellationToken: TestContext.Current.CancellationToken));
        Assert.Empty(await fixture.Store.GetEntriesAsync(cancellationToken: TestContext.Current.CancellationToken));
    }
}
