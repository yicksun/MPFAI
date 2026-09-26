namespace MPFAI.Api.Domain;

public enum GuidelineState { Draft, UnderReview, Approved, Superseded, Retired }
public enum JobState { Queued, Running, ReadyForReview, Failed }
public enum AttributionState { NotPlanned, SetupRequested, ReportedComplete, VerificationPending, Verified, Missing, Mismatched, Stale, Unknown }
public enum EvidenceState { Draft, SubmittedForReview, Accepted, Rejected, Replaced, Expired }
public enum ClaimState { NotStarted, Draft, ConsentPending, ReadyToSubmit, Submitted, UnderReview, ActionRequired, Resubmitted, Approved, PaymentPending, Paid, Rejected, Disputed, Cancelled, Expired }

public sealed record GuidelineDocument(
    Guid Id,
    Guid OrganizationId,
    string ProgramId,
    string Version,
    string FileName,
    string Sha256,
    int SizeBytes,
    decimal Rate,
    decimal Cap,
    string Currency,
    DateOnly EffectiveFrom,
    DateOnly EffectiveTo,
    string UploadedBy,
    string? ApprovedBy,
    GuidelineState State,
    IReadOnlyList<Citation> Citations,
    DateTimeOffset UploadedAt,
    DateTimeOffset? ApprovedAt = null,
    string? ConflictReason = null);

public sealed record SowAnalysisJob(
    Guid Id,
    Guid OrganizationId,
    Guid EngagementId,
    JobState State,
    string Provider,
    IReadOnlyList<Citation> SourceCitations,
    IReadOnlyList<string> MissingInputs,
    DateTimeOffset CreatedAt,
    string? FailureReason = null);

public sealed record DeliveryTask(
    Guid Id,
    Guid OrganizationId,
    Guid EngagementId,
    string Title,
    string Owner,
    DateOnly? DueDate,
    string State,
    DateTimeOffset CreatedAt);

public sealed record AttributionRecord(
    Guid Id,
    Guid OrganizationId,
    Guid EngagementId,
    string PartnerId,
    string CustomerTenantId,
    string AzureScope,
    string DeliveryIdentity,
    string AssociationType,
    AttributionState State,
    string? VerificationMethod = null,
    string? EvidenceReference = null,
    string? VerifiedBy = null,
    DateTimeOffset? VerifiedAt = null);

public sealed record EvidenceItem(
    Guid Id,
    Guid OrganizationId,
    Guid EngagementId,
    string Requirement,
    string FileName,
    string SubmittedBy,
    EvidenceState State,
    string? Reviewer = null,
    string? ReviewComment = null,
    DateTimeOffset CreatedAt = default);

public sealed record ClaimRecord(
    Guid Id,
    Guid OrganizationId,
    Guid EngagementId,
    string ExternalClaimId,
    string Currency,
    decimal RequestedAmount,
    ClaimState State,
    DateTimeOffset CreatedAt,
    DateTimeOffset? SubmittedAt = null,
    DateTimeOffset? PaidAt = null);
