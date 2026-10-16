using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Sharing;

namespace Vafadar.Zanance.Core.Tests;

/// <summary>Real shared financial/authority invariants, without a fake service, paid grant or copied application algorithm.</summary>
[Trait("AT", "AT-294")]
public sealed class HouseholdRulesTests
{
    /// <summary>Removing another owner device retires its delegated root; removing an ordinary recipient must not silently change owner authority.</summary>
    [Fact]
    public void Revoking_owner_device_requires_both_root_roles_to_rotate_while_recipient_revocation_preserves_root()
    {
        var root = new HouseholdPublicKeys("fictitious-root-sign", "fictitious-root-wrap");
        var first = new HouseholdDeviceDefinition(Guid.NewGuid(), new("fictitious-owner-first-sign", "fictitious-owner-first-wrap"));
        var second = new HouseholdDeviceDefinition(Guid.NewGuid(), new("fictitious-owner-second-sign", "fictitious-owner-second-wrap"));
        var owner = new HouseholdMemberDefinition("fictitious-owner", HouseholdRole.Owner, new('0', 64), [first, second]);
        var viewer = new HouseholdMemberDefinition("fictitious-viewer", HouseholdRole.Viewer, new('a', 64),
            [new(Guid.NewGuid(), new("fictitious-viewer-sign", "fictitious-viewer-wrap"))]);
        var previous = new HouseholdMembershipStatement(1, Guid.NewGuid(), Guid.NewGuid(), HouseholdMembershipChange.ApproveDevice, null,
            1, 2, new('0', 64), owner.IdentityId, owner.IdentityId, root, 2, new('1', 64), new('a', 64), 1, [owner, viewer]);
        var next = previous with { OperationId = Guid.NewGuid(), ExpectedVersion = 2, Version = 3, PreviousHash = new('b', 64),
            Change = HouseholdMembershipChange.RevokeDevice, KeyEpoch = 3, KeyCommitment = new('2', 64), Members = [owner with { Devices = [first] }, viewer] };
        foreach (var retained in new[] { root, root with { SigningKey = "fictitious-new-sign" }, root with { WrappingKey = "fictitious-new-wrap" } })
            Assert.Contains("HouseholdOwnerKeysMustBothRotate", HouseholdMembershipRules.ValidateContinuity(next with { OwnerKeys = retained }, previous, new('b', 64)));
        var changed = next with { OwnerKeys = new("fictitious-new-sign", "fictitious-new-wrap") };
        Assert.Empty(HouseholdMembershipRules.ValidateContinuity(changed, previous, new('b', 64)));
        Assert.Empty(HouseholdMembershipRules.ValidateRosterTransition(changed, previous));
        var recipient = next with { Members = [owner, viewer with { Devices = [] }] };
        Assert.Empty(HouseholdMembershipRules.ValidateContinuity(recipient, previous, new('b', 64)));
        Assert.Empty(HouseholdMembershipRules.ValidateRosterTransition(recipient, previous));
        Assert.Contains("HouseholdOwnerTransitionProofRequired", HouseholdMembershipRules.ValidateContinuity(recipient with { OwnerKeys = changed.OwnerKeys }, previous, new('b', 64)));
        Assert.NotEmpty(HouseholdMembershipRules.ValidateShape(previous with { OwnerKeys = root with { SigningKey = first.Keys.SigningKey } }));
        Assert.NotEmpty(HouseholdMembershipRules.ValidateShape(previous with { Members = [owner, viewer with
            { Devices = [new(viewer.Devices[0].Id, viewer.Devices[0].Keys with { WrappingKey = first.Keys.WrappingKey })] }] }));
    }

    /// <summary>Replacing only a joint fingerprint's public half must not preserve an old kit's ability to sign as the new owner root.</summary>
    [Theory]
    [InlineData(HouseholdMembershipChange.TransferOwner, true)]
    [InlineData(HouseholdMembershipChange.TransferOwner, false)]
    [InlineData(HouseholdMembershipChange.RecoverOwner, true)]
    [InlineData(HouseholdMembershipChange.RecoverOwner, false)]
    [InlineData(HouseholdMembershipChange.RotateOwnerRoot, true)]
    [InlineData(HouseholdMembershipChange.RotateOwnerRoot, false)]
    public void Owner_root_transition_replaces_both_independent_key_roles(HouseholdMembershipChange change, bool retainSigning)
    {
        var root = new HouseholdPublicKeys("fictitious-original-signing", "fictitious-original-wrapping");
        var first = new HouseholdMemberDefinition("fictitious-owner", HouseholdRole.Owner, new('0', 64),
            [new(Guid.CreateVersion7(), new("fictitious-owner-device-signing", "fictitious-owner-device-wrapping"))]);
        var other = new HouseholdMemberDefinition("fictitious-second", HouseholdRole.Editor, new('1', 64),
            [new(Guid.CreateVersion7(), new("fictitious-second-device-signing", "fictitious-second-device-wrapping"))]);
        var previous = new HouseholdMembershipStatement(1, Guid.CreateVersion7(), Guid.CreateVersion7(), HouseholdMembershipChange.ChangeRole, null,
            1, 2, new('0', 64), first.IdentityId, first.IdentityId, root, 2, new('1', 64), new('a', 64), 1, [first, other]);
        var members = change == HouseholdMembershipChange.TransferOwner
            ? new[] { first with { Role = HouseholdRole.Editor }, other with { Role = HouseholdRole.Owner } }
            : change == HouseholdMembershipChange.RecoverOwner
                ? [first with { Devices = [new(Guid.CreateVersion7(), new("fictitious-recovered-signing", "fictitious-recovered-wrapping"))] }, other]
                : new[] { first, other };
        var next = previous with { OperationId = Guid.CreateVersion7(), ExpectedVersion = 2, Version = 3,
            PreviousHash = new('b', 64), Change = change, OwnerId = change == HouseholdMembershipChange.TransferOwner ? other.IdentityId : first.IdentityId,
            OwnerKeys = new(retainSigning ? root.SigningKey : "fictitious-fresh-signing", retainSigning ? "fictitious-fresh-wrapping" : root.WrappingKey),
            KeyEpoch = 3, KeyCommitment = new('2', 64), Members = members };
        Assert.Contains("HouseholdOwnerKeysMustBothRotate", HouseholdMembershipRules.ValidateContinuity(next, previous, new('b', 64)));
        next = next with { OwnerKeys = new("fictitious-fresh-signing", "fictitious-fresh-wrapping") };
        Assert.Empty(HouseholdMembershipRules.ValidateContinuity(next, previous, new('b', 64)));
        Assert.Empty(HouseholdMembershipRules.ValidateRosterTransition(next, previous));
    }

    /// <summary>Shared-cash entries count once; private advances/refunds and actual repayments remain separate.</summary>
    [Fact]
    public void Linked_cash_is_not_double_counted_and_private_refunds_and_repayments_balance_exactly()
    {
        var a = new HouseholdContact { Name = "Fictitious A" }; var b = new HouseholdContact { Name = "Fictitious B" };
        var date = new DateOnly(2026, 10, 16);
        var account = new Account { Name = "Fictitious shared cash", CurrencyCode = "EUR", OpeningDate = date, OpeningBalance = 1000 };
        var movement = new LedgerEntry { AccountId = account.Id, Kind = EntryKind.Expense, Amount = 150, Date = date, Title = "Fictitious groceries" };
        var shared = new HouseholdCost { Kind = HouseholdCostKind.Expense, PaymentKind = HouseholdPaymentKind.SharedAccount,
            Amount = 150, CurrencyCode = "EUR", Date = date, EntryId = movement.Id, Title = "Fictitious shared groceries" };
        var advance = new HouseholdCost { Kind = HouseholdCostKind.Expense, PaymentKind = HouseholdPaymentKind.Private,
            Amount = 200, CurrencyCode = "EUR", Date = date, PaidByContactId = a.Id, Title = "Fictitious private advance" };
        var refund = new HouseholdCost { Kind = HouseholdCostKind.Refund, PaymentKind = HouseholdPaymentKind.Private,
            Amount = 50, CurrencyCode = "EUR", Date = date, PaidByContactId = a.Id, RefundOfCostId = advance.Id, Title = "Fictitious actual refund" };
        HouseholdCostAllocation Share(HouseholdCost cost, HouseholdContact contact, long amount) => new() { CostId = cost.Id, ContactId = contact.Id, Amount = amount };
        var shares = new[] { Share(shared, a, 75), Share(shared, b, 75), Share(advance, a, 100), Share(advance, b, 100), Share(refund, a, 25), Share(refund, b, 25) };
        var people = new[] { a, b }; var costs = new[] { shared, advance, refund };
        var entries = new Dictionary<Guid, LedgerEntry> { [movement.Id] = movement };
        var accounts = new Dictionary<Guid, Account> { [account.Id] = account };
        Assert.Empty(HouseholdCostLedger.Validate(people, costs, shares, [], entries, accounts));
        var total = Assert.Single(HouseholdCostLedger.Totals(costs));
        Assert.Equal(300m, total.NetCost); Assert.Equal(150m, total.PaidFromSharedAccounts); Assert.Equal(150m, total.PaidPrivately);
        var before = HouseholdCostLedger.Members(people, costs, shares, []);
        Assert.Equal(75m, before.Single(row => row.ContactId == a.Id).PrivateAdvanceBalance);
        Assert.Equal(-75m, before.Single(row => row.ContactId == b.Id).PrivateAdvanceBalance);
        var repayment = new HouseholdSettlement { FromContactId = b.Id, ToContactId = a.Id, Amount = 75, CurrencyCode = "EUR", Date = date };
        Assert.Empty(HouseholdCostLedger.Validate(people, costs, shares, [repayment], entries, accounts));
        Assert.All(HouseholdCostLedger.Members(people, costs, shares, [repayment]), row =>
        { Assert.Equal(150m, row.AttributedNetCost); Assert.Equal(0m, row.PrivateAdvanceBalance); });
        Assert.Equal(150L, movement.Amount); Assert.Equal(1000L, account.OpeningBalance);
    }

    /// <summary>An altered allocation/refund cannot guess a balancing value or silently take another contact's share.</summary>
    [Fact]
    public void Allocation_mismatch_and_excessive_person_refund_are_explicit_errors()
    {
        var person = new HouseholdContact { Name = "Fictitious member" }; var other = new HouseholdContact { Name = "Fictitious second member" };
        var cost = new HouseholdCost { Title = "Fictitious original", Amount = 100, CurrencyCode = "KWD", Date = new(2026, 10, 16),
            Kind = HouseholdCostKind.Expense, PaymentKind = HouseholdPaymentKind.Private, PaidByContactId = person.Id };
        var refund = new HouseholdCost { Title = "Fictitious refund", Amount = 40, CurrencyCode = "KWD", Date = cost.Date,
            Kind = HouseholdCostKind.Refund, PaymentKind = HouseholdPaymentKind.Private, PaidByContactId = person.Id, RefundOfCostId = cost.Id };
        var shares = new[] { new HouseholdCostAllocation { CostId = cost.Id, ContactId = person.Id, Amount = 20 },
            new HouseholdCostAllocation { CostId = cost.Id, ContactId = other.Id, Amount = 80 },
            new HouseholdCostAllocation { CostId = refund.Id, ContactId = person.Id, Amount = 40 } };
        var issues = HouseholdCostLedger.Validate([person, other], [cost, refund], shares, [], new Dictionary<Guid, LedgerEntry>(), new Dictionary<Guid, Account>());
        Assert.Contains(issues, issue => issue.Code == "HouseholdRefundExceedsContactShare");
        shares[0].Amount = 19;
        issues = HouseholdCostLedger.Validate([person, other], [cost, refund], shares, [], new Dictionary<Guid, LedgerEntry>(), new Dictionary<Guid, Account>());
        Assert.Contains(issues, issue => issue.Code == "HouseholdAllocationSumMismatch");
        Assert.Equal(19L, shares[0].Amount);
    }

    /// <summary>Security revocation, missing device approval and wrong scope always win over role and retention.</summary>
    [Fact]
    public void Revoked_pending_unapproved_or_cross_scope_authority_never_grants_remote_work()
    {
        var space = Guid.NewGuid(); var identity = "fictitious-owner";
        var revoked = new HouseholdMembershipAuthority(space, identity, HouseholdRole.Owner, true, true, true, 1, 1);
        var pending = new HouseholdMembershipAuthority(space, identity, HouseholdRole.Owner, false, false, true, 1, 1);
        var unapproved = new HouseholdMembershipAuthority(space, identity, HouseholdRole.Owner, true, false, false, 1, 1);
        var active = new HouseholdMembershipAuthority(space, identity, HouseholdRole.Owner, true, false, true, 1, 1);
        foreach (var operation in Enum.GetValues<HouseholdRemoteOperation>())
        {
            Assert.False(HouseholdAccessPolicy.Allows(revoked, space, operation, HouseholdRetentionState.Active));
            Assert.False(HouseholdAccessPolicy.Allows(pending, space, operation, HouseholdRetentionState.Active));
            Assert.False(HouseholdAccessPolicy.Allows(unapproved, space, operation, HouseholdRetentionState.Active));
            Assert.False(HouseholdAccessPolicy.Allows(active, Guid.NewGuid(), operation, HouseholdRetentionState.Active));
        }
    }

    /// <summary>Viewer is genuinely read/export only, and exact30/120-day boundaries cannot be prolonged by another read.</summary>
    [Fact]
    public void Viewer_and_expired_host_cannot_write_and_retention_has_exact_exclusive_deadlines()
    {
        var expiry = new DateTimeOffset(2026, 10, 16, 8, 0, 0, TimeSpan.Zero); var space = Guid.NewGuid();
        var viewer = new HouseholdMembershipAuthority(space, "fictitious-viewer", HouseholdRole.Viewer, true, false, true, 1, 1);
        Assert.True(HouseholdAccessPolicy.Allows(viewer, space, HouseholdRemoteOperation.Read, HouseholdRetentionState.Active));
        Assert.True(HouseholdAccessPolicy.Allows(viewer, space, HouseholdRemoteOperation.Export, HouseholdRetentionState.Active));
        Assert.False(HouseholdAccessPolicy.Allows(viewer, space, HouseholdRemoteOperation.WriteFinancial, HouseholdRetentionState.Active));
        Assert.False(HouseholdAccessPolicy.Allows(viewer, space, HouseholdRemoteOperation.Invite, HouseholdRetentionState.Active));
        Assert.Equal(HouseholdRetentionState.Active, HouseholdAccessPolicy.Retention(expiry.AddTicks(-1), expiry, false));
        Assert.Equal(HouseholdRetentionState.ReadOnlyGrace, HouseholdAccessPolicy.Retention(expiry, expiry, false));
        Assert.Equal(HouseholdRetentionState.ReadOnlyGrace, HouseholdAccessPolicy.Retention(expiry.AddDays(30).AddTicks(-1), expiry, false));
        Assert.Equal(HouseholdRetentionState.Retained, HouseholdAccessPolicy.Retention(expiry.AddDays(30), expiry, false));
        Assert.Equal(HouseholdRetentionState.DeletionDue, HouseholdAccessPolicy.Retention(expiry.AddDays(120), expiry, false));
        Assert.Equal(HouseholdRetentionState.Deleted, HouseholdAccessPolicy.Retention(expiry.AddDays(-1), expiry, true));
        Assert.False(HouseholdAccessPolicy.Allows(viewer, space, HouseholdRemoteOperation.Read, HouseholdRetentionState.Retained));
        var owner = new HouseholdMembershipAuthority(space, "fictitious-owner", HouseholdRole.Owner, true, false, true, 1, 1);
        Assert.True(HouseholdAccessPolicy.Allows(owner, space, HouseholdRemoteOperation.Revoke, HouseholdRetentionState.Retained));
        Assert.False(HouseholdAccessPolicy.Allows(owner, space, HouseholdRemoteOperation.WriteFinancial, HouseholdRetentionState.ReadOnlyGrace));
    }
}
