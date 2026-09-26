using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using MPFAI.Api.Domain;
using MPFAI.Api.Repositories;

namespace MPFAI.Api.Services;

public interface ISowAnalysisProvider
{
    string Name { get; }
    SowAnalysisJob Analyze(Guid organizationId, Guid engagementId, string sowText);
}

public sealed class LocalSafeSowAnalysisProvider : ISowAnalysisProvider
{
    public string Name => "local-text-review-only";

    public SowAnalysisJob Analyze(Guid organizationId, Guid engagementId, string sowText)
    {
        var snippets = sowText.Split('\n', StringSplitOptions.None)
            .Select((line, index) => new { Text = line.TrimEnd('\r').Trim(), Line = index + 1 })
            .Where(line => line.Text.Length > 0)
            .Take(20)
            .Select(line => new Citation("local-sow-input", "1", $"line {line.Line}", line.Text[..Math.Min(line.Text.Length, 500)]))
            .ToArray();

        return new SowAnalysisJob(
            Guid.NewGuid(), organizationId, engagementId, JobState.ReadyForReview, Name, snippets,
            ["Partner qualifications", "Program-specific eligible activities", "Customer consent and approval status"],
            DateTimeOffset.UtcNow);
    }
}

public sealed class WorkflowService(
    IEngagementRepository engagements,
    IFundingGuidelineRepository calculatorGuidelines,
    IAuditRepository audit,
    ISowAnalysisProvider sowProvider,
    IImmutableDocumentStore documentStore)
{
    private readonly ConcurrentDictionary<Guid, GuidelineDocument> _guidelines = new();
    private readonly ConcurrentDictionary<Guid, SowAnalysisJob> _jobs = new();
    private readonly ConcurrentDictionary<Guid, DeliveryTask> _tasks = new();
    private readonly ConcurrentDictionary<Guid, AttributionRecord> _attribution = new();
    private readonly ConcurrentDictionary<Guid, EvidenceItem> _evidence = new();
    private readonly ConcurrentDictionary<Guid, ClaimRecord> _claims = new();

    public async Task<GuidelineDocument> UploadGuidelineAsync(
        Guid organizationId, string fileName, byte[] content, string programId, string version,
        decimal rate, decimal cap, string currency, DateOnly from, DateOnly to, string uploader,
        CancellationToken token)
    {
        if (organizationId == Guid.Empty || string.IsNullOrWhiteSpace(fileName) ||
            string.IsNullOrWhiteSpace(programId) || string.IsNullOrWhiteSpace(version) || string.IsNullOrWhiteSpace(uploader))
            throw new WorkflowException("invalid_guideline", "Organization, filename, program, version, and uploader are required.");
        if (!string.Equals(Path.GetExtension(fileName), ".txt", StringComparison.OrdinalIgnoreCase))
            throw new WorkflowException("unsupported_guideline_file", "Local mode accepts UTF-8 .txt guideline files only; production malware scanning and document extraction are not configured.");
        if (content.Length is 0 or > 1_048_576)
            throw new WorkflowException("guideline_size_invalid", "Guideline text must be between 1 byte and 1 MiB.");
        try
        {
            _ = new UTF8Encoding(false, true).GetString(content);
        }
        catch (DecoderFallbackException)
        {
            throw new WorkflowException("guideline_encoding_invalid", "Guideline text must be valid UTF-8.");
        }
        if (rate is < 0 or > 1 || cap < 0 || string.IsNullOrWhiteSpace(currency) || from > to)
            throw new WorkflowException("guideline_parameters_invalid", "Rate, cap, currency, and effective dates must be valid.");

        var checksum = Convert.ToHexString(SHA256.HashData(content));
        var existing = _guidelines.Values.Where(x => x.OrganizationId == organizationId && x.ProgramId == programId).ToArray();
        if (existing.Any(x => x.Sha256 == checksum))
            throw new WorkflowException("guideline_duplicate", "This exact guideline file is already registered.");
        if (existing.Any(x => x.Version == version))
            throw new WorkflowException("guideline_version_duplicate", "Guideline versions must be unique within a program.");

        var overlap = existing.FirstOrDefault(x => x.State == GuidelineState.Approved &&
            x.EffectiveFrom <= to && from <= x.EffectiveTo &&
            (x.Rate != rate || x.Cap != cap || !string.Equals(x.Currency, currency, StringComparison.OrdinalIgnoreCase)));
        var conflict = overlap is null ? null : $"Conflicts with approved {overlap.Version} for an overlapping effective period.";
        var decodedContent = Encoding.UTF8.GetString(content);
        var citation = new Citation(fileName, version, "whole text document", decodedContent[..Math.Min(decodedContent.Length, 500)]);
        var guideline = new GuidelineDocument(Guid.NewGuid(), organizationId, programId, version, Path.GetFileName(fileName),
            checksum, content.Length, rate, cap, currency.ToUpperInvariant(), from, to, uploader, null,
            GuidelineState.UnderReview, [citation], DateTimeOffset.UtcNow, ConflictReason: conflict);
        await documentStore.PutAsync(organizationId, guideline.Id.ToString(), guideline.FileName, content, token);
        _guidelines[guideline.Id] = guideline;
        await audit.AppendAsync(new AuditEvent(Guid.NewGuid(), organizationId, uploader, "guideline.uploaded", "guideline", guideline.Id.ToString(), DateTimeOffset.UtcNow, checksum), token);
        return guideline;
    }

    public async Task<GuidelineDocument> ApproveGuidelineAsync(Guid organizationId, Guid id, string approver, CancellationToken token)
    {
        if (!_guidelines.TryGetValue(id, out var current) || current.OrganizationId != organizationId)
            throw new WorkflowException("guideline_not_found", "Guideline not found.");
        if (current.State != GuidelineState.UnderReview || current.ConflictReason is not null)
            throw new WorkflowException("guideline_not_approvable", "Guideline is not in a clean review state.");
        if (string.Equals(current.UploadedBy, approver, StringComparison.OrdinalIgnoreCase))
            throw new WorkflowException("self_approval_forbidden", "The guideline uploader cannot approve their own version.");
        var approved = current with { State = GuidelineState.Approved, ApprovedBy = approver, ApprovedAt = DateTimeOffset.UtcNow };
        if (!_guidelines.TryUpdate(id, approved, current))
            throw new WorkflowException("concurrent_update", "Guideline changed while it was being approved; reload and retry.");

        foreach (var previous in _guidelines.Values.Where(x => x.Id != id && x.OrganizationId == organizationId &&
            x.ProgramId == approved.ProgramId && x.State == GuidelineState.Approved &&
            x.EffectiveFrom <= approved.EffectiveTo && approved.EffectiveFrom <= x.EffectiveTo).ToArray())
        {
            var superseded = previous with { State = GuidelineState.Superseded };
            if (_guidelines.TryUpdate(previous.Id, superseded, previous))
            {
                await calculatorGuidelines.DeactivateAsync(organizationId, previous.Id.ToString(), token);
                await audit.AppendAsync(new AuditEvent(Guid.NewGuid(), organizationId, approver, "guideline.superseded", "guideline", previous.Id.ToString(), DateTimeOffset.UtcNow), token);
            }
        }

        var citations = approved.Citations;
        await calculatorGuidelines.SaveAsync(new FundingGuideline(approved.Id.ToString(), approved.OrganizationId, approved.Version, approved.Rate, approved.Cap,
            approved.Currency, approved.EffectiveFrom, approved.EffectiveTo, true, false, citations), token);
        await audit.AppendAsync(new AuditEvent(Guid.NewGuid(), organizationId, approver, "guideline.approved", "guideline", id.ToString(), DateTimeOffset.UtcNow), token);
        return approved;
    }

    public IReadOnlyList<GuidelineDocument> ListGuidelines(Guid organizationId) =>
        _guidelines.Values.Where(x => x.OrganizationId == organizationId).OrderBy(x => x.ProgramId).ThenBy(x => x.Version).ToArray();

    public async Task<SowAnalysisJob> StartSowJobAsync(Guid organizationId, Guid engagementId, string sowText, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(sowText) || sowText.Length > 100_000)
            throw new WorkflowException("sow_text_invalid", "Local SOW text must be between 1 and 100,000 characters.");
        if (await engagements.GetAsync(organizationId, engagementId, token) is null)
            throw new WorkflowException("engagement_not_found", "Engagement not found.");
        var job = sowProvider.Analyze(organizationId, engagementId, sowText);
        _jobs[job.Id] = job;
        await audit.AppendAsync(new AuditEvent(Guid.NewGuid(), organizationId, "local-user", "sow.analysis.queued", "analysis", job.Id.ToString(), DateTimeOffset.UtcNow, sowProvider.Name), token);
        return job;
    }

    public IReadOnlyList<SowAnalysisJob> ListJobs(Guid organizationId) =>
        _jobs.Values.Where(x => x.OrganizationId == organizationId).OrderByDescending(x => x.CreatedAt).ToArray();

    public async Task<DeliveryTask> CreateTaskAsync(Guid organizationId, Guid engagementId, string title, string owner, DateOnly? dueDate, CancellationToken token)
    {
        if (await engagements.GetAsync(organizationId, engagementId, token) is null)
            throw new WorkflowException("engagement_not_found", "Engagement not found.");
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(owner))
            throw new WorkflowException("task_invalid", "Task title and owner are required.");
        var task = new DeliveryTask(Guid.NewGuid(), organizationId, engagementId, title.Trim(), owner.Trim(), dueDate, "open", DateTimeOffset.UtcNow);
        _tasks[task.Id] = task;
        await audit.AppendAsync(new AuditEvent(Guid.NewGuid(), organizationId, owner, "task.created", "task", task.Id.ToString(), DateTimeOffset.UtcNow), token);
        return task;
    }

    public IReadOnlyList<DeliveryTask> ListTasks(Guid organizationId) =>
        _tasks.Values.Where(x => x.OrganizationId == organizationId).OrderBy(x => x.DueDate).ToArray();

    public async Task<DeliveryTask> ChangeTaskStateAsync(Guid organizationId, Guid id, string state, string actor, CancellationToken token)
    {
        if (!_tasks.TryGetValue(id, out var current) || current.OrganizationId != organizationId)
            throw new WorkflowException("task_not_found", "Task not found.");
        var valid = (current.State, state) switch
        {
            ("open", "in_progress" or "blocked") => true,
            ("in_progress", "blocked" or "done" or "open") => true,
            ("blocked", "in_progress" or "open") => true,
            _ => false
        };
        if (!valid)
            throw new WorkflowException("task_transition_invalid", $"Task cannot move from {current.State} to {state}.");
        var updated = current with { State = state };
        _tasks[id] = updated;
        await audit.AppendAsync(new AuditEvent(Guid.NewGuid(), organizationId, actor, $"task.{state}", "task", id.ToString(), DateTimeOffset.UtcNow), token);
        return updated;
    }

    public async Task<AttributionRecord> CreateAttributionAsync(AttributionRecord record, CancellationToken token)
    {
        if (await engagements.GetAsync(record.OrganizationId, record.EngagementId, token) is null)
            throw new WorkflowException("engagement_not_found", "Engagement not found.");
        var created = record with { Id = Guid.NewGuid(), State = AttributionState.SetupRequested };
        _attribution[created.Id] = created;
        await audit.AppendAsync(new AuditEvent(Guid.NewGuid(), created.OrganizationId, "local-user", "attribution.setup_requested", "attribution", created.Id.ToString(), DateTimeOffset.UtcNow), token);
        return created;
    }

    public async Task<AttributionRecord> VerifyAttributionAsync(Guid organizationId, Guid id, string verifier, string method, string evidence, CancellationToken token)
    {
        if (!_attribution.TryGetValue(id, out var current) || current.OrganizationId != organizationId)
            throw new WorkflowException("attribution_not_found", "Attribution record not found.");
        if (string.IsNullOrWhiteSpace(verifier) || string.IsNullOrWhiteSpace(method) || string.IsNullOrWhiteSpace(evidence))
            throw new WorkflowException("verification_evidence_required", "An independent verifier, verification method, and evidence reference are required.");
        if (current.State != AttributionState.VerificationPending)
            throw new WorkflowException("verification_state_invalid", "Attribution must be reported complete before verification.");
        if (string.Equals(verifier, current.DeliveryIdentity, StringComparison.OrdinalIgnoreCase))
            throw new WorkflowException("independent_verifier_required", "A delivery identity cannot verify its own attribution.");
        var verified = current with { State = AttributionState.Verified, VerifiedBy = verifier, VerificationMethod = method, EvidenceReference = evidence, VerifiedAt = DateTimeOffset.UtcNow };
        _attribution[id] = verified;
        await audit.AppendAsync(new AuditEvent(Guid.NewGuid(), organizationId, verifier, "attribution.verified", "attribution", id.ToString(), DateTimeOffset.UtcNow, method), token);
        return verified;
    }

    public IReadOnlyList<AttributionRecord> ListAttribution(Guid organizationId) =>
        _attribution.Values.Where(x => x.OrganizationId == organizationId).ToArray();

    public async Task<AttributionRecord> ReportAttributionAsync(Guid organizationId, Guid id, string reporter, CancellationToken token)
    {
        if (!_attribution.TryGetValue(id, out var current) || current.OrganizationId != organizationId)
            throw new WorkflowException("attribution_not_found", "Attribution record not found.");
        var reported = current with { State = AttributionState.VerificationPending };
        _attribution[id] = reported;
        await audit.AppendAsync(new AuditEvent(Guid.NewGuid(), organizationId, reporter, "attribution.reported_complete", "attribution", id.ToString(), DateTimeOffset.UtcNow), token);
        return reported;
    }

    public async Task<EvidenceItem> SubmitEvidenceAsync(Guid organizationId, Guid engagementId, string requirement, string fileName, string submittedBy, CancellationToken token)
    {
        if (await engagements.GetAsync(organizationId, engagementId, token) is null)
            throw new WorkflowException("engagement_not_found", "Engagement not found.");
        var item = new EvidenceItem(Guid.NewGuid(), organizationId, engagementId, requirement, Path.GetFileName(fileName), submittedBy, EvidenceState.SubmittedForReview, CreatedAt: DateTimeOffset.UtcNow);
        _evidence[item.Id] = item;
        await audit.AppendAsync(new AuditEvent(Guid.NewGuid(), organizationId, submittedBy, "evidence.submitted", "evidence", item.Id.ToString(), DateTimeOffset.UtcNow), token);
        return item;
    }

    public async Task<EvidenceItem> ReviewEvidenceAsync(Guid organizationId, Guid id, string reviewer, bool accepted, string comment, CancellationToken token)
    {
        if (!_evidence.TryGetValue(id, out var current) || current.OrganizationId != organizationId)
            throw new WorkflowException("evidence_not_found", "Evidence item not found.");
        if (current.State != EvidenceState.SubmittedForReview)
            throw new WorkflowException("evidence_transition_invalid", "Only evidence awaiting review can receive a decision.");
        var reviewed = current with { State = accepted ? EvidenceState.Accepted : EvidenceState.Rejected, Reviewer = reviewer, ReviewComment = comment };
        _evidence[id] = reviewed;
        await audit.AppendAsync(new AuditEvent(Guid.NewGuid(), organizationId, reviewer, accepted ? "evidence.accepted" : "evidence.rejected", "evidence", id.ToString(), DateTimeOffset.UtcNow), token);
        return reviewed;
    }

    public IReadOnlyList<EvidenceItem> ListEvidence(Guid organizationId) =>
        _evidence.Values.Where(x => x.OrganizationId == organizationId).ToArray();

    public async Task<ClaimRecord> CreateClaimAsync(Guid organizationId, Guid engagementId, string externalId, string currency, decimal amount, CancellationToken token)
    {
        if (await engagements.GetAsync(organizationId, engagementId, token) is null)
            throw new WorkflowException("engagement_not_found", "Engagement not found.");
        if (amount < 0 || string.IsNullOrWhiteSpace(externalId) || string.IsNullOrWhiteSpace(currency))
            throw new WorkflowException("claim_invalid", "External claim ID, currency, and a non-negative amount are required.");
        var claim = new ClaimRecord(Guid.NewGuid(), organizationId, engagementId, externalId, currency.ToUpperInvariant(), amount, ClaimState.Draft, DateTimeOffset.UtcNow);
        _claims[claim.Id] = claim;
        await audit.AppendAsync(new AuditEvent(Guid.NewGuid(), organizationId, "local-user", "claim.created", "claim", claim.Id.ToString(), DateTimeOffset.UtcNow), token);
        return claim;
    }

    public IReadOnlyList<ClaimRecord> ListClaims(Guid organizationId) =>
        _claims.Values.Where(x => x.OrganizationId == organizationId).ToArray();

    public async Task<ClaimRecord> ChangeClaimStateAsync(Guid organizationId, Guid id, ClaimState state, CancellationToken token)
    {
        if (!_claims.TryGetValue(id, out var current) || current.OrganizationId != organizationId)
            throw new WorkflowException("claim_not_found", "Claim not found.");
        if (!AllowedClaimTransition(current.State, state))
            throw new WorkflowException("claim_transition_invalid", $"Claim cannot move from {current.State} to {state}.");
        var updated = current with { State = state, SubmittedAt = state == ClaimState.Submitted ? DateTimeOffset.UtcNow : current.SubmittedAt, PaidAt = state == ClaimState.Paid ? DateTimeOffset.UtcNow : current.PaidAt };
        _claims[id] = updated;
        await audit.AppendAsync(new AuditEvent(Guid.NewGuid(), organizationId, "local-user", $"claim.{state.ToString().ToLowerInvariant()}", "claim", id.ToString(), DateTimeOffset.UtcNow), token);
        return updated;
    }

    private static bool AllowedClaimTransition(ClaimState current, ClaimState next) => (current, next) switch
    {
        (ClaimState.Draft, ClaimState.ConsentPending or ClaimState.ReadyToSubmit or ClaimState.Cancelled) => true,
        (ClaimState.ConsentPending, ClaimState.ReadyToSubmit or ClaimState.Cancelled) => true,
        (ClaimState.ReadyToSubmit, ClaimState.Submitted or ClaimState.Cancelled) => true,
        (ClaimState.Submitted, ClaimState.UnderReview or ClaimState.ActionRequired or ClaimState.Approved or ClaimState.Rejected or ClaimState.Disputed) => true,
        (ClaimState.UnderReview, ClaimState.ActionRequired or ClaimState.Approved or ClaimState.Rejected or ClaimState.Disputed) => true,
        (ClaimState.ActionRequired, ClaimState.Resubmitted or ClaimState.Cancelled) => true,
        (ClaimState.Resubmitted, ClaimState.UnderReview or ClaimState.Approved or ClaimState.Rejected or ClaimState.Disputed) => true,
        (ClaimState.Approved, ClaimState.PaymentPending or ClaimState.Paid) => true,
        (ClaimState.PaymentPending, ClaimState.Paid) => true,
        _ => false
    };
}

public sealed class WorkflowException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
