using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.Data;

public sealed partial class ZananceStore
{
    /// <summary>Revalidates original confirmed loan inputs and computes/saves the original whole payment batch under one writer.</summary>
    public async Task<SaveResult> SaveLoanInstallmentAsync(LoanReadReview reviewed, Guid loanAccountId, Guid cashAccountId,
        string interestTitle, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reviewed); ArgumentNullException.ThrowIfNull(interestTitle);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var write = await CommercialWriteTransaction.OpenAsync(db, commercialAccess, cancellationToken, requireTransaction: true);
        write.DemandFeature(CommercialFeature.LoanAnalysis);
        await reviewed.ValidateWriterAsync(db, cancellationToken: cancellationToken);
        var entries = reviewed.CreateInstallment(loanAccountId, cashAccountId, interestTitle);
        if (entries.Count == 0) throw new InvalidOperationException("The original loan installment has no financial movement.");
        return await SaveEntriesUnderWriterAsync(db, write, entries, [], null, new HashSet<Guid>(), cancellationToken, loanReview: reviewed);
    }
}
