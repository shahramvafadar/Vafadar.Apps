using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vafadar.Zanance.App.Features.Accounts;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Data;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Original installment startup notifications cannot retain Busy or read after retiring their reviewed owner.</summary>
public sealed class LoanInstallmentPublicationTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Loan_publication_start_owner_edit_and_restore_never_acquires_another_source_context()
    {
        using var f = new FlowFixture(); var s = await PrepareAsync(f);
        var before = await GoalChooserFlowTests.SnapshotAsync(f); var changed = false;
        s.Session.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(LoanInstallmentSession.IsBusy) || !s.Session.IsBusy || changed) return;
            changed = true; s.Session.SetOwner(s.LoanId, false); s.Session.SetOwner(s.LoanId, true);
        };
        s.Factory.Armed = true;
        await s.Session.RecordAsync(s.Review, s.LoanId);
        Assert.True(changed); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        Assert.Equal(0, s.Factory.Acquired); Assert.False(s.Session.IsBusy); Assert.Null(s.Session.AcceptedLoanId);
        Assert.Empty(f.Platform.ConfirmationDialogs); Assert.Empty(f.Platform.Routes);
    }

    [Fact]
    public async Task Loan_publication_start_busy_subscriber_failure_releases_the_original_lifetime()
    {
        using var f = new FlowFixture(); var s = await PrepareAsync(f);
        var before = await GoalChooserFlowTests.SnapshotAsync(f); var notified = false;
        s.Session.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(LoanInstallmentSession.IsBusy) || !s.Session.IsBusy || notified) return;
            notified = true; throw new IOException("D949 original Busy notification failure");
        };
        s.Factory.Armed = true;
        var exception = await Record.ExceptionAsync(() => s.Session.RecordAsync(s.Review, s.LoanId));
        Assert.True(notified); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        Assert.False(s.Session.IsBusy); Assert.Null(exception); Assert.Equal(0, s.Factory.Acquired);
        Assert.NotNull(s.Session.Error); Assert.Null(s.Session.AcceptedLoanId); Assert.Empty(f.Platform.ConfirmationDialogs);
    }

    [Fact]
    public async Task Loan_publication_start_cleared_error_cannot_revalidate_a_retired_owner()
    {
        using var f = new FlowFixture(); var s = await PrepareAsync(f);
        s.Factory.Armed = true; await s.Session.RecordAsync(s.Review, s.LoanId);
        Assert.NotNull(s.Session.Error); Assert.Equal(1, s.Factory.Acquired); Assert.False(s.Session.IsBusy);
        var before = await GoalChooserFlowTests.SnapshotAsync(f); var changed = false; s.Factory.Acquired = 0;
        s.Session.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(LoanInstallmentSession.Error) || s.Session.Error is not null || changed) return;
            changed = true; s.Session.SetOwner(s.LoanId, false); s.Session.SetOwner(s.LoanId, true);
        };
        s.Factory.Armed = true; await s.Session.RecordAsync(s.Review, s.LoanId);
        Assert.True(changed); Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f));
        Assert.Equal(0, s.Factory.Acquired); Assert.False(s.Session.IsBusy); Assert.Null(s.Session.AcceptedLoanId);
        Assert.Empty(f.Platform.ConfirmationDialogs); Assert.Empty(f.Platform.Routes);
    }

    [Fact]
    public async Task Loan_publication_current_owner_still_reaches_original_source_validation()
    {
        using var f = new FlowFixture(); var s = await PrepareAsync(f);
        var before = await GoalChooserFlowTests.SnapshotAsync(f); s.Factory.Armed = true;
        await s.Session.RecordAsync(s.Review, s.LoanId);
        Assert.Equal(before, await GoalChooserFlowTests.SnapshotAsync(f)); Assert.Equal(1, s.Factory.Acquired);
        Assert.False(s.Session.IsBusy); Assert.NotNull(s.Session.Error); Assert.Null(s.Session.AcceptedLoanId);
        Assert.Empty(f.Platform.ConfirmationDialogs); Assert.Empty(f.Platform.Routes);
    }

    /// <summary>Creates only two new raw metadata accounts; the sentinel prevents source revalidation before any payment command.</summary>
    private static async Task<Prepared> PrepareAsync(FlowFixture f)
    {
        var original = AccountDetailOriginalBoundaryTests.Factory(f);
        var loan = new Account { Name = "D949 reviewed loan", Type = AccountType.Loan, CurrencyCode = "EUR", OpeningDate = new(2026, 1, 1), OpeningBalance = -25700, Installment = 4000, InterestRate = 12 };
        await using (var db = await original.CreateDbContextAsync(Ct))
        {
            db.Accounts.AddRange(loan, new Account { Name = "D949 cash", Type = AccountType.Checking, CurrencyCode = "EUR", OpeningDate = loan.OpeningDate, OpeningBalance = 0 });
            await db.SaveChangesAsync(Ct);
        }
        var factory = new SourceProbe(original); var reader = new LoanWorkStore(factory, f.Services.GetRequiredService<ICommercialWriteAccessSource>(), f.Time);
        var review = await reader.ReadAsync(Ct); var session = new LoanInstallmentSession(reader, f.Store, f.Translator, f.Localization, f.Platform);
        session.SetOwner(loan.Id, true); return new(review, loan.Id, factory, session);
    }

    private sealed record Prepared(LoanReadReview Review, Guid LoanId, SourceProbe Factory, LoanInstallmentSession Session);

    /// <summary>Counts the original factory boundary and refuses before opening a reader or reaching financial work.</summary>
    private sealed class SourceProbe(IDbContextFactory<ZananceDbContext> original) : IDbContextFactory<ZananceDbContext>
    {
        internal bool Armed; internal int Acquired;
        public ZananceDbContext CreateDbContext() => Observe(original.CreateDbContext());
        public async Task<ZananceDbContext> CreateDbContextAsync(CancellationToken ct = default) => Observe(await original.CreateDbContextAsync(ct));
        private ZananceDbContext Observe(ZananceDbContext db)
        {
            if (!Armed) return db;
            Armed = false; Acquired++; db.Dispose(); throw new IOException("D949 original source boundary sentinel");
        }
    }
}
