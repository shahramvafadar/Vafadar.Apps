using System.Security.Cryptography;
using System.Text.Json;
using Vafadar.Cryptography;
using Vafadar.Zanance.Core.Rates;
using Vafadar.Zanance.Core.Sharing;
using Vafadar.Zanance.Data.Sharing;

namespace Vafadar.Zanance.Data.Tests;

/// <summary>Actual signed publication/roster-horizon verifier with new bounded source companions, not another crypto implementation.</summary>
[Trait("AT", "AT-299")]
public sealed class HouseholdMarketSourceTests
{
    private const string Owner = "fictitious-source-owner";
    private static readonly string Zero = new('0', 64);
    private sealed class Fixture : IDisposable
    {
        internal readonly Guid Room = Guid.NewGuid(), Device = Guid.NewGuid(), NextDevice = Guid.NewGuid();
        internal readonly SigningRecipientIdentity Root = SigningRecipientIdentity.Create(), Author = SigningRecipientIdentity.Create(), NextAuthor = SigningRecipientIdentity.Create();
        internal readonly HouseholdEpochMaterial Epoch;
        internal readonly HouseholdCryptography Crypto = new(TimeProvider.System);
        internal readonly HouseholdMemberDefinition Member;
        internal readonly HouseholdVerifiedMembership Membership;
        internal Fixture()
        {
            using var firstEpoch = HouseholdEpochMaterial.Create();
            var firstMember = new HouseholdMemberDefinition(Owner, HouseholdRole.Owner, Zero, [new(Device, Public(Author))]);
            var initial = Statement(firstEpoch, 0, Zero, HouseholdMembershipChange.Genesis, firstMember);
            var first = HouseholdTrustChain.Genesis(Room, Root.Public, Proof(initial));
            Epoch = firstEpoch.Next(); Member = firstMember with { Devices = [.. firstMember.Devices, new(NextDevice, Public(NextAuthor))] };
            var approval = Statement(Epoch, first.Version, first.Hash, HouseholdMembershipChange.ApproveDevice, Member);
            Membership = HouseholdTrustChain.Advance(first, Proof(approval));
        }
        internal HouseholdMembershipStatement Statement(HouseholdEpochMaterial epoch, long previous, string hash, HouseholdMembershipChange change, HouseholdMemberDefinition member) =>
            new(1, Room, Guid.NewGuid(), change, null, previous, previous + 1, hash, Owner, Owner, Public(Root), epoch.Epoch,
                epoch.KeyCommitment, epoch.AddressKeyCommitment, TimeProvider.System.GetUtcNow().AddMinutes(5).ToUnixTimeSeconds(), [member]);
        internal HouseholdMembershipProof Proof(HouseholdMembershipStatement value) => new(StandardJoseSignatures.Sign(
            JsonSerializer.SerializeToUtf8Bytes(value, HouseholdMetadataJsonContext.Default.HouseholdMembershipStatement), Root, "zanance-household-membership+jwt"), null, null, new('e', 64));
        public void Dispose() { Epoch.Dispose(); NextAuthor.Dispose(); Author.Dispose(); Root.Dispose(); }
    }
    private static HouseholdPublicKeys Public(SigningRecipientIdentity identity) => new(identity.Public.SigningKey, identity.Public.WrappingKey);
    private static MarketReferenceDocument Document()
    {
        var now = TimeProvider.System.GetUtcNow(); var date = DateOnly.FromDateTime(now.Date);
        var symbol = new MarketSymbol("EURUSD", "Fictitious source symbol", "EUR", "USD", MarketUnit.Currency);
        var catalog = new MarketCatalog(1, "fictitious-shared-source", "fixture-v1", "Fictitious original source",
            "https://source.example.invalid/reference", "https://source.example.invalid/licence", "Fictitious local qualification only",
            "Europe/Berlin", 60, 3600, true, new(2020, 1, 1), true, [symbol]);
        var quote = new MarketQuote(1, new(catalog.Provider, catalog.Revision, symbol.Id, date), catalog, symbol,
            new("fictitious-source-observation", date, now.AddMinutes(-10), 1.23456789m), now, now.AddMinutes(1), now.AddHours(1));
        var target = new ExchangeRate { Date = date, FromCurrencyCode = "EUR", ToCurrencyCode = "USD", Rate = quote.Observation.Value, IsEstimate = true };
        return new(1, Guid.CreateVersion7(), MarketReferenceTarget.ExchangeRate, target.Id, MarketApplication.Fingerprint(target),
            MarketReference.Digest(quote, now), JsonSerializer.Serialize(quote, MarketJson.Default.MarketQuote),
            JsonSerializer.Serialize(new MarketAppliedMapping(1, MarketReferenceTarget.ExchangeRate, date, "USD", "EUR", target.Rate, null, null, false),
                MarketMappingJson.Default.MarketAppliedMapping), now);
    }
    private static (HouseholdPublicationReceipt Receipt, HouseholdOpaqueEvent[] Events) Published(Fixture f, HouseholdPreparedPublication prepared)
    {
        var round = Guid.NewGuid(); var cursor = prepared.Open.ExpectedCursor + 1;
        var parts = prepared.Parts.Select(part => new HouseholdPartCommitment(part.Index, part.RecordId, part.BaseVersion, part.Digest)).ToArray();
        return (new(round, prepared.Commit.SignedCommit, parts), prepared.Parts.Select(part => new HouseholdOpaqueEvent(cursor, round, part.RecordId,
            prepared.Open.KeyEpoch, prepared.Open.MembershipVersion, prepared.Open.DeviceId, prepared.Open.OperationId, prepared.Open.AttemptId,
            part.Index, part.BaseVersion, part.Ciphertext, part.Digest)).ToArray());
    }
    private static HouseholdPreparedPublication Prepare(Fixture f, HouseholdFinancialReplica replica, MarketReferenceDocument document) =>
        replica.Prepare(f.Membership, f.Epoch, f.Device, f.Author, Guid.NewGuid(), Guid.NewGuid(), [], [], new Dictionary<string, byte[]>(), f.Crypto, [document]);

    [Fact]
    public void Source_only_signed_publication_preserves_exact_document_without_creating_any_financial_row_or_version()
    {
        using var f = new Fixture(); using var producer = new HouseholdFinancialReplica(f.Room); using var receiver = new HouseholdFinancialReplica(f.Room);
        var document = Document(); var prepared = Prepare(f, producer, document);
        Assert.True(prepared.Plan.Succeeded); Assert.Single(prepared.Parts); Assert.Equal(0, prepared.FinancialCursor);
        Assert.DoesNotContain(document.QuoteJson, prepared.Parts[0].Ciphertext); var published = Published(f, prepared);
        receiver.Apply(f.Membership, f.Epoch, published.Receipt, published.Events, f.Crypto);
        Assert.Equal(document, Assert.Single(receiver.VerifiedReferences)); Assert.Empty(receiver.Snapshot());
        Assert.Equal(1, receiver.Cursor); Assert.Equal(0, receiver.FinancialCursor);
    }
    [Theory][InlineData(false)][InlineData(true)]
    public void Signed_membership_horizon_distinguishes_original_accepted_history_from_an_old_key_source_forgery(bool previouslyAccepted)
    {
        using var f = new Fixture(); using var producer = new HouseholdFinancialReplica(f.Room); using var receiver = new HouseholdFinancialReplica(f.Room);
        var prepared = Prepare(f, producer, Document()); var published = Published(f, prepared); using var nextEpoch = f.Epoch.Next();
        using var nextRoot = SigningRecipientIdentity.Create();
        var next = f.Statement(nextEpoch, f.Membership.Version, f.Membership.Hash, HouseholdMembershipChange.RevokeDevice,
            f.Member with { Devices = [new(f.NextDevice, Public(f.NextAuthor))] }) with
        { PublicationCursor = previouslyAccepted ? 1 : 0, PublicationHead = previouslyAccepted ? HouseholdTrustChain.Digest(published.Receipt.SignedCommit) : Zero,
            OwnerKeys = Public(nextRoot) };
        var proof = f.Proof(next);
        var acceptance = new HouseholdOwnerAcceptanceStatement(1, f.Room, next.OperationId, HouseholdTrustChain.Digest(proof.SignedStatement), proof.GrantsHash);
        proof = proof with { TransferAcceptance = StandardJoseSignatures.Sign(JsonSerializer.SerializeToUtf8Bytes(acceptance,
            HouseholdMetadataJsonContext.Default.HouseholdOwnerAcceptanceStatement), nextRoot, "zanance-household-owner-acceptance+jwt") };
        var verifiedNext = HouseholdTrustChain.Advance(f.Membership, proof);
        if (previouslyAccepted)
        { receiver.Apply(f.Membership, f.Epoch, published.Receipt, published.Events, f.Crypto, verifiedNext); Assert.Single(receiver.VerifiedReferences); Assert.Equal(1, receiver.Cursor); }
        else
        {
            Assert.Throws<CryptographicException>(() => receiver.Apply(f.Membership, f.Epoch, published.Receipt, published.Events, f.Crypto, verifiedNext));
            Assert.Empty(receiver.VerifiedReferences); Assert.Equal(0, receiver.Cursor);
        }
        Assert.Empty(receiver.Snapshot()); Assert.Equal(0, receiver.FinancialCursor);
    }
    [Fact]
    public void Original_source_identity_cannot_be_rewritten_inside_another_validly_signed_publication()
    {
        using var f = new Fixture(); using var replica = new HouseholdFinancialReplica(f.Room); var original = Document();
        var first = Published(f, Prepare(f, replica, original)); replica.Apply(f.Membership, f.Epoch, first.Receipt, first.Events, f.Crypto);
        var changed = original with { AcceptedAt = original.AcceptedAt.AddTicks(1) };
        var second = Published(f, Prepare(f, replica, changed));
        Assert.Throws<CryptographicException>(() => replica.Apply(f.Membership, f.Epoch, second.Receipt, second.Events, f.Crypto));
        Assert.Equal(original, Assert.Single(replica.VerifiedReferences)); Assert.Equal(1, replica.Cursor); Assert.Empty(replica.Snapshot());
    }
    [Fact]
    public void Valid_author_signature_and_encryption_do_not_authorize_a_forged_source_target_fingerprint()
    {
        using var f = new Fixture(); using var replica = new HouseholdFinancialReplica(f.Room); var source = Document() with { TargetFingerprint = new('f', 64) };
        var operation = Guid.NewGuid(); var attempt = Guid.NewGuid(); var round = Guid.NewGuid();
        var payload = new HouseholdFinancialPayload(2, HouseholdFinancialSchema.Hash, attempt, 0, HouseholdFinancialContentKind.SourceDocument, null, null, Source: source);
        var clear = JsonSerializer.SerializeToUtf8Bytes(payload, HouseholdFinancialJsonContext.Default.HouseholdFinancialPayload); string ciphertext;
        try { ciphertext = f.Epoch.Encrypt(f.Crypto, new(f.Room, f.Epoch.Epoch, operation, HouseholdProtectionPurpose.Financial, "members"), clear, f.Author); }
        finally { CryptographicOperations.ZeroMemory(clear); }
        var part = new HouseholdPartCommitment(0, f.Epoch.RecordAddress("source-document", source.Id.ToString("N")), 0, HouseholdPublicationDigest.Ciphertext(ciphertext));
        var statement = new HouseholdCommitStatement(1, f.Room, f.Device, operation, attempt, 0, 1, f.Membership.Version, f.Epoch.Epoch, Zero, 1, HouseholdPublicationDigest.Manifest([part]));
        var signed = StandardJoseSignatures.Sign(JsonSerializer.SerializeToUtf8Bytes(statement, HouseholdMetadataJsonContext.Default.HouseholdCommitStatement), f.Author, "zanance-household-commit+jwt");
        var receipt = new HouseholdPublicationReceipt(round, signed, [part]);
        Assert.Equal(1, HouseholdTrustChain.Publication(f.Membership, receipt, f.Device, operation, attempt, 0, Zero).Cursor);
        var item = new HouseholdOpaqueEvent(1, round, part.RecordId, f.Epoch.Epoch, f.Membership.Version, f.Device, operation, attempt, 0, 0, ciphertext, part.Digest);
        Assert.Throws<CryptographicException>(() => replica.Apply(f.Membership, f.Epoch, receipt, [item], f.Crypto));
        Assert.Equal(0, replica.Cursor); Assert.Empty(replica.VerifiedReferences); Assert.Empty(replica.Snapshot());
    }
    [Fact]
    public void Delivered_format_one_serialization_remains_exact_and_has_no_implicit_source_field()
    {
        var attempt = Guid.NewGuid(); var payload = new HouseholdFinancialPayload(1, HouseholdFinancialSchema.Hash, attempt, 0, HouseholdFinancialContentKind.Record, null, null);
        var json = JsonSerializer.Serialize(payload, HouseholdFinancialJsonContext.Default.HouseholdFinancialPayload);
        Assert.Equal($"{{\"format\":1,\"schemaHash\":\"{HouseholdFinancialSchema.Hash}\",\"attemptId\":\"{attempt:D}\",\"index\":0,\"kind\":1,\"record\":null,\"attachment\":null}}", json);
        Assert.True(payload.SupportedFormat()); Assert.DoesNotContain("\"source\"", json);
    }
}
