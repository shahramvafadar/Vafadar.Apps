using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Features.Accounts;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>D-639: actual original account reads and archive consent cannot adopt another owner.</summary>
public sealed class AccountEditorOriginalBoundaryTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private sealed class ChangedFactory(IDbContextFactory<ZananceDbContext> original,
        IDbContextFactory<ZananceDbContext> foreign) : IDbContextFactory<ZananceDbContext>
    {
        private int _calls;
        public ZananceDbContext CreateDbContext() =>
            (Interlocked.Increment(ref _calls) == 1 ? original : foreign).CreateDbContext();
        public Task<ZananceDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            (Interlocked.Increment(ref _calls) == 1 ? original : foreign).CreateDbContextAsync(cancellationToken);
    }

    /// <summary>Changing the factory after the first real read must not publish foreign account fields.</summary>
    [Fact]
    public async Task Actual_original_account_form_never_adopts_a_foreign_same_id_after_its_settings_read()
    {
        using var original = new FlowFixture(); using var foreign = new FlowFixture();
        var account = await AccountDetailOriginalBoundaryTests.AccountAsync(original);
        await original.Store.GetSettingsAsync(Ct); await foreign.Store.GetSettingsAsync(Ct);
        await using (var db = await AccountDetailOriginalBoundaryTests.Factory(foreign).CreateDbContextAsync(Ct))
        {
            db.Accounts.Add(new Account(account.Id) { Name = "Foreign D639 account", Type = account.Type,
                CurrencyCode = "EUR", OpeningDate = account.OpeningDate, OpeningBalance = 99900 });
            await db.SaveChangesAsync(Ct);
        }
        var factory = new ChangedFactory(AccountDetailOriginalBoundaryTests.Factory(original), AccountDetailOriginalBoundaryTests.Factory(foreign));
        var access = original.Services.GetRequiredService<ICommercialWriteAccessSource>();
        var store = new ZananceStore(factory, original.Time, access);
        var model = new AccountEditorViewModel(store, new PlanStore(factory, store, access),
            original.Translator, original.Localization, original.Time, original.Platform);

        await model.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = account.Id });

        Assert.True(!model.CanInteract || model.Form.Name == account.Name,
            "The original form must remain covered or publish the original account, never the foreign same-ID fields.");
    }

    /// <summary>A successful response from retired native consent cannot archive or navigate.</summary>
    [Fact]
    public async Task Actual_account_archive_consent_retired_by_departure_never_commits_or_navigates()
    {
        using var fixture = new FlowFixture(); var account = await AccountDetailOriginalBoundaryTests.AccountAsync(fixture);
        var model = new AccountEditorViewModel(fixture.Store, fixture.Services.GetRequiredService<PlanStore>(),
            fixture.Translator, fixture.Localization, fixture.Time, fixture.Platform);
        await model.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = account.Id });
        var before = await GoalChooserFlowTests.SnapshotAsync(fixture);
        fixture.Platform.PendingConfirmation = () => { model.SetPresentationActive(false); return Task.FromResult(true); };

        await model.ToggleArchiveCommand.ExecuteAsync(null);

        Assert.False((await fixture.Store.GetAccountsAsync(cancellationToken: Ct)).Single(x => x.Id == account.Id).IsArchived,
            "An actual departed account editor cannot accept its old archive consent.");
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(fixture));
        Assert.Empty(fixture.Platform.Routes);
    }
}
