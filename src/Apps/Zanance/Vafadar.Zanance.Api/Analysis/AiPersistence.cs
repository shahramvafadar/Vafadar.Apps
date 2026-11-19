using Microsoft.EntityFrameworkCore;
using Vafadar.Zanance.Core.Analysis;

namespace Vafadar.Zanance.Api.Analysis;

/// <summary>One original offer, issued without financial content and bound to the actual authenticated identity.</summary>
public sealed class AiOfferRow
{
    /// <summary>Unpredictable server-issued offer id.</summary>
    public Guid Id { get; set; }
    /// <summary>Actual service identity, never supplied in request JSON.</summary>
    public string UserId { get; set; } = "";
    /// <summary>Immutable provider identity.</summary>
    public string Provider { get; set; } = "";
    /// <summary>Immutable model revision.</summary>
    public string Model { get; set; } = "";
    /// <summary>One bounded read-only purpose.</summary>
    public AiPurpose Purpose { get; set; }
    /// <summary>Original maximum consented credit units, unrelated to financial minor units.</summary>
    public long MaximumUnits { get; set; }
    /// <summary>Original operator selection for an isolated provider pilot offer.</summary>
    public long? PilotRouteId { get; set; }
    /// <summary>Server-time offer boundary; it never expires purchased credit.</summary>
    public DateTimeOffset ExpiresAt { get; set; }
}

/// <summary>The durable create boundary for newly admitted synthetic operations.</summary>
public enum AiPilotDispatchStage
{
    /// <summary>No process has obtained permission to make the original create call.</summary>
    Queued,
    /// <summary>A process could have reached the vendor. This attempt may never be reclaimed.</summary>
    Claimed,
}

/// <summary>Durable original dispatch intent and protected minimal payload; retries never replace its identity or consent.</summary>
public sealed class AiRequestRow
{
    /// <summary>Client's original id retained before its first HTTP submission.</summary>
    public Guid Id { get; set; }
    /// <summary>Actual authenticated wallet owner.</summary>
    public string UserId { get; set; } = "";
    /// <summary>Only one distinct request may consume an offer.</summary>
    public Guid OfferId { get; set; }
    /// <summary>Canonical exact approved aggregate digest.</summary>
    public string PayloadDigest { get; set; } = "";
    /// <summary>Existing service DataProtection, with identity/request-bound purpose, not plaintext financial storage.</summary>
    public string ProtectedPayload { get; set; } = "";
    /// <summary>Original purchased allocation; preserved through process loss.</summary>
    public long PurchasedReserved { get; set; }
    /// <summary>Original gifted allocation, kept distinct from purchased credit.</summary>
    public long GiftedReserved { get; set; }
    /// <summary>One-way terminal state; uncertainty stays Pending.</summary>
    public AiRequestState State { get; set; }
    /// <summary>Confirmed actual units, never above the original maximum.</summary>
    public long ChargedUnits { get; set; }
    /// <summary>Untrusted read-only provider text protected by the existing service key ring.</summary>
    public string? ProtectedText { get; set; }
    /// <summary>Original server authorization time.</summary>
    public DateTimeOffset CreatedAt { get; set; }
    /// <summary>Bounded original-status lookup time; never permission to submit again.</summary>
    public DateTimeOffset? StatusCheckedAt { get; set; }
    /// <summary>Opaque original vendor execution ID, never returned to the client.</summary>
    public string? VendorOperationId { get; set; }
    /// <summary>Frozen uncached Standard ceiling in millionths of a US dollar for an accepted synthetic dispatch.</summary>
    public long? PilotCostCeilingMicros { get; set; }
    /// <summary>Null preserves the previous dispatch path; only new synthetic admissions enter the queue.</summary>
    public AiPilotDispatchStage? PilotDispatchStage { get; set; }
    /// <summary>Server time at which the one original create became possible.</summary>
    public DateTimeOffset? PilotDispatchClaimedAt { get; set; }
    /// <summary>Actual vendor input usage recorded on an accepted completion.</summary>
    public int? VendorInputTokens { get; set; }
    /// <summary>Actual vendor billable output usage recorded on an accepted completion.</summary>
    public int? VendorOutputTokens { get; set; }
}

/// <summary>An audited local operator selection. An off row retires previous unaccepted quotes.</summary>
public sealed class AiPilotRouteRow
{
    /// <summary>Database sequence used to order selections across instances.</summary>
    public long Id { get; set; }
    /// <summary>Fixed provider identifier or off.</summary>
    public string Provider { get; set; } = "";
    /// <summary>Work addressed by this selection. Existing rows belong to the period explanation.</summary>
    public AiPurpose Purpose { get; set; }
    /// <summary>Selected model. Null preserves the original provider-only pilot revisions.</summary>
    public string? Model { get; set; }
    /// <summary>Approved transport and project reference.</summary>
    public string Revision { get; set; } = "";
    /// <summary>Account executing the private operator command.</summary>
    public string Actor { get; set; } = "";
    /// <summary>Server timestamp of the switch.</summary>
    public DateTimeOffset SelectedAt { get; set; }
}

/// <summary>Durable authorization of a single operator status lookup, recorded before provider I/O.</summary>
public sealed class AiPilotStatusCheckRow
{
    /// <summary>Original accepted service request.</summary>
    public Guid RequestId { get; set; }
    /// <summary>One-based lookup number within the operator attempt budget.</summary>
    public int Attempt { get; set; }
    /// <summary>Original authenticated pilot operator.</summary>
    public string Actor { get; set; } = "";
    /// <summary>UTC time at which the lookup was reserved, even if the process stopped before I/O.</summary>
    public DateTimeOffset CheckedAt { get; set; }
}

/// <summary>Bounded receipt of an authenticated provider completion notice in the isolated pilot.</summary>
public sealed class AiPilotCompletionNoticeRow
{
    /// <summary>Original provider namespace for the signed delivery and native reference.</summary>
    public string Provider { get; set; } = "";
    /// <summary>Deduplicated native response ID. No payload or output is stored here.</summary>
    public string ResponseId { get; set; } = "";
    /// <summary>Signed delivery ID for replay refusal.</summary>
    public string DeliveryId { get; set; } = "";
    /// <summary>The first validated receipt time.</summary>
    public DateTimeOffset ReceivedAt { get; set; }
    /// <summary>Number of claimed authenticated read attempts.</summary>
    public int Attempts { get; set; }
    /// <summary>Last validation lookup, including calls interrupted before I/O.</summary>
    public DateTimeOffset? CheckedAt { get; set; }
    /// <summary>The notice no longer needs a validation lookup.</summary>
    public bool Handled { get; set; }
}

/// <summary>Append-only event types; no client can manufacture a purchased grant.</summary>
public enum AiCreditEvent { PurchasedGrant, GiftedGrant, Reserve, Settle, Refund }
/// <summary>Immutable credit journal outside personal/shared finance; purchased and gifted movements are independently traceable.</summary>
public sealed class AiCreditRow
{
    /// <summary>Unique service event identity.</summary>
    public Guid Id { get; set; }
    /// <summary>Actual wallet owner.</summary>
    public string UserId { get; set; } = "";
    /// <summary>Original request for reservation/settlement/refund, never an inferred financial posting.</summary>
    public Guid? RequestId { get; set; }
    /// <summary>Hashed trusted receipt/gift identity for exactly-once internal grants; no raw store reference.</summary>
    public string? GrantKey { get; set; }
    /// <summary>Explicit journal transition.</summary>
    public AiCreditEvent Kind { get; set; }
    /// <summary>Purchased available delta, with no expiry.</summary>
    public long PurchasedAvailable { get; set; }
    /// <summary>Gifted available delta.</summary>
    public long GiftedAvailable { get; set; }
    /// <summary>Purchased reserved delta.</summary>
    public long PurchasedReserved { get; set; }
    /// <summary>Gifted reserved delta.</summary>
    public long GiftedReserved { get; set; }
    /// <summary>Actual service operation time.</summary>
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>Independent service-only schema; no credit authority is copied into financial backup/sync or granted by a tier.</summary>
internal static class AiServiceModel
{
    /// <summary>Configures exact ownership/idempotence indexes; native append-only enforcement is added by its migration.</summary>
    internal static void Configure(ModelBuilder model)
    {
        var route = model.Entity<AiPilotRouteRow>(); route.ToTable("AiPilotRoutes"); route.HasKey(r => r.Id);
        route.Property(r => r.Provider).HasMaxLength(16); route.Property(r => r.Revision).HasMaxLength(64);
        route.Property(r => r.Actor).HasMaxLength(256);
        route.Property(r => r.Model).HasMaxLength(64);
        route.HasIndex(r => new { r.Purpose, r.Id });
        var offer = model.Entity<AiOfferRow>(); offer.ToTable("AiOffers"); offer.HasKey(r => r.Id);
        offer.Property(r => r.UserId).HasMaxLength(256); offer.Property(r => r.Provider).HasMaxLength(64); offer.Property(r => r.Model).HasMaxLength(64);
        offer.HasIndex(r => new { r.UserId, r.ExpiresAt });
        offer.HasOne<AiPilotRouteRow>().WithMany().HasForeignKey(r => r.PilotRouteId).OnDelete(DeleteBehavior.Restrict);
        offer.HasOne<ServiceUser>().WithMany().HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Restrict);
        var request = model.Entity<AiRequestRow>(); request.ToTable("AiRequests"); request.HasKey(r => r.Id);
        request.Property(r => r.UserId).HasMaxLength(256); request.Property(r => r.PayloadDigest).HasMaxLength(64);
        request.Property(r => r.ProtectedPayload).HasMaxLength(65536); request.Property(r => r.ProtectedText).HasMaxLength(65536);
        request.HasOne<ServiceUser>().WithMany().HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Restrict);
        request.Property(r => r.VendorOperationId).HasMaxLength(256);
        request.HasIndex(r => r.OfferId).IsUnique(); request.HasIndex(r => new { r.UserId, r.CreatedAt });
        request.HasOne<AiOfferRow>().WithMany().HasForeignKey(r => r.OfferId).OnDelete(DeleteBehavior.Restrict);
        var statusCheck = model.Entity<AiPilotStatusCheckRow>(); statusCheck.ToTable("AiPilotStatusChecks");
        statusCheck.HasKey(r => new { r.RequestId, r.Attempt });
        statusCheck.Property(r => r.Actor).HasMaxLength(256);
        statusCheck.HasOne<AiRequestRow>().WithMany().HasForeignKey(r => r.RequestId).OnDelete(DeleteBehavior.Restrict);

        var notice = model.Entity<AiPilotCompletionNoticeRow>(); notice.ToTable("AiPilotOpenAiNotices");
        notice.HasKey(r => new { r.Provider, r.ResponseId }); notice.Property(r => r.Provider).HasMaxLength(16).HasDefaultValue("openai");
        notice.Property(r => r.ResponseId).HasMaxLength(250);
        notice.Property(r => r.DeliveryId).HasMaxLength(250); notice.HasIndex(r => new { r.Provider, r.DeliveryId }).IsUnique();

        var credit = model.Entity<AiCreditRow>(); credit.ToTable("AiCredits"); credit.HasKey(r => r.Id);
        credit.Property(r => r.UserId).HasMaxLength(256); credit.Property(r => r.GrantKey).HasMaxLength(64);
        credit.HasIndex(r => r.GrantKey).IsUnique();
        credit.HasOne<AiRequestRow>().WithMany().HasForeignKey(r => r.RequestId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_AiCredits_Request"); credit.HasIndex(r => new { r.RequestId, r.Kind }).IsUnique();
        credit.HasIndex(r => r.UserId);
        credit.HasOne<ServiceUser>().WithMany().HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}
