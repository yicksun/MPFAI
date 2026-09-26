using System.Text;
using MPFAI.Api.Domain;
using MPFAI.Api.Repositories;
using MPFAI.Api.Services;

namespace MPFAI.Api.Tests;

public sealed class WorkflowServiceTests
{
    private static readonly Guid OrganizationId = Guid.Parse("8182a48a-ad19-41a8-b7e6-dc13d7bb20c7");
    private static readonly Guid OtherOrganizationId = Guid.Parse("6825d599-0b7f-487e-85c3-1173527b77f8");
    private static readonly DateOnly Today = new(2026, 9, 26);

    private static (WorkflowService Service, InMemoryEngagementRepository Engagements, InMemoryFundingGuidelineRepository Calculations, InMemoryAuditRepository Audit) Create()
    {
        var engagements = new InMemoryEngagementRepository();
        var calculations = new InMemoryFundingGuidelineRepository();
        var audit = new InMemoryAuditRepository();
        var service = new WorkflowService(engagements, calculations, audit, new LocalSafeSowAnalysisProvider(), new InMemoryDocumentStore());
        return (service, engagements, calculations, audit);
    }

    private static async Task<Guid> CreateEngagementAsync(InMemoryEngagementRepository repository)
    {
        var customer = new Customer(Guid.NewGuid(), OrganizationId, "Contoso", null, DateTimeOffset.UtcNow);
        await new InMemoryCustomerRepository().CreateAsync(customer, CancellationToken.None);
        var engagement = new Engagement(Guid.NewGuid(), OrganizationId, customer.Id, "Cloud migration", "proposed", DateTimeOffset.UtcNow);
        await repository.CreateAsync(engagement, CancellationToken.None);
        return engagement.Id;
    }

    private static Task<GuidelineDocument> UploadAsync(WorkflowService service, string version, decimal rate, decimal cap, string content) =>
        service.UploadGuidelineAsync(OrganizationId, "guide.txt", Encoding.UTF8.GetBytes(content), "pilot-program", version,
            rate, cap, "USD", new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), $"uploader-{version}", CancellationToken.None);

    [Fact]
    public async Task Guideline_requires_independent_approval_and_calculation_is_organization_scoped()
    {
        var (service, _, calculations, _) = Create();
        var draft = await UploadAsync(service, "v1", 0.15m, 1000m, "Eligible work is reimbursed at 15 percent.");
        Assert.Equal(GuidelineState.UnderReview, draft.State);
        Assert.Equal(Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes("Eligible work is reimbursed at 15 percent."))), draft.Sha256);

        var selfApproval = await Assert.ThrowsAsync<WorkflowException>(() =>
            service.ApproveGuidelineAsync(OrganizationId, draft.Id, "uploader-v1", CancellationToken.None));
        Assert.Equal("self_approval_forbidden", selfApproval.Code);

        var approved = await service.ApproveGuidelineAsync(OrganizationId, draft.Id, "program-manager", CancellationToken.None);
        Assert.Equal(GuidelineState.Approved, approved.State);
        var calculator = new FundingCalculator(calculations);
        var result = await calculator.CalculateAsync(new FundingCalculationRequest(draft.Id.ToString(), OrganizationId, 1000m, Today));
        Assert.Equal(150m, result.Amount);

        var wrongOrganization = await Assert.ThrowsAsync<FundingCalculationException>(() =>
            calculator.CalculateAsync(new FundingCalculationRequest(draft.Id.ToString(), OtherOrganizationId, 1000m, Today)));
        Assert.Equal("guideline_missing", wrongOrganization.Code);
    }

    [Fact]
    public async Task Conflicting_overlapping_guideline_is_blocked_from_approval()
    {
        var (service, _, _, _) = Create();
        var first = await UploadAsync(service, "v1", 0.15m, 1000m, "First valid source.");
        await service.ApproveGuidelineAsync(OrganizationId, first.Id, "manager", CancellationToken.None);
        var second = await UploadAsync(service, "v2", 0.2m, 1500m, "Conflicting replacement source.");

        Assert.NotNull(second.ConflictReason);
        var exception = await Assert.ThrowsAsync<WorkflowException>(() =>
            service.ApproveGuidelineAsync(OrganizationId, second.Id, "manager", CancellationToken.None));
        Assert.Equal("guideline_not_approvable", exception.Code);
        Assert.Equal(GuidelineState.Approved, service.ListGuidelines(OrganizationId).Single(x => x.Id == first.Id).State);
    }

    [Fact]
    public async Task Replacing_a_guideline_supersedes_and_deactivates_old_calculations()
    {
        var (service, _, calculations, _) = Create();
        var first = await UploadAsync(service, "v1", 0.15m, 1000m, "Initial approved source.");
        await service.ApproveGuidelineAsync(OrganizationId, first.Id, "manager", CancellationToken.None);
        var second = await UploadAsync(service, "v2", 0.15m, 1000m, "Replacement approved source.");
        await service.ApproveGuidelineAsync(OrganizationId, second.Id, "manager", CancellationToken.None);

        Assert.Equal(GuidelineState.Superseded, service.ListGuidelines(OrganizationId).Single(x => x.Id == first.Id).State);
        var calculator = new FundingCalculator(calculations);
        var exception = await Assert.ThrowsAsync<FundingCalculationException>(() =>
            calculator.CalculateAsync(new FundingCalculationRequest(first.Id.ToString(), OrganizationId, 100m, Today)));
        Assert.Equal("guideline_unapproved", exception.Code);
    }

    [Fact]
    public async Task Attribution_requires_report_and_independent_verifier()
    {
        var (service, engagements, _, _) = Create();
        var engagementId = await CreateEngagementAsync(engagements);
        var record = await service.CreateAttributionAsync(new AttributionRecord(Guid.Empty, OrganizationId, engagementId,
            "partner-123", "tenant-1", "/subscriptions/sub-1", "engineer@example.test", "PAL", AttributionState.NotPlanned), CancellationToken.None);

        var premature = await Assert.ThrowsAsync<WorkflowException>(() =>
            service.VerifyAttributionAsync(OrganizationId, record.Id, "manager", "portal-review", "evidence-1", CancellationToken.None));
        Assert.Equal("verification_state_invalid", premature.Code);

        await service.ReportAttributionAsync(OrganizationId, record.Id, "engineer@example.test", CancellationToken.None);
        var selfVerification = await Assert.ThrowsAsync<WorkflowException>(() =>
            service.VerifyAttributionAsync(OrganizationId, record.Id, "engineer@example.test", "portal-review", "evidence-1", CancellationToken.None));
        Assert.Equal("independent_verifier_required", selfVerification.Code);

        var verified = await service.VerifyAttributionAsync(OrganizationId, record.Id, "manager@example.test", "portal-review", "evidence-1", CancellationToken.None);
        Assert.Equal(AttributionState.Verified, verified.State);
        Assert.NotNull(verified.VerifiedAt);
    }

    [Fact]
    public async Task Local_sow_analysis_is_explicitly_review_only_and_cites_user_text()
    {
        var (service, engagements, _, _) = Create();
        var engagementId = await CreateEngagementAsync(engagements);
        var job = await service.StartSowJobAsync(OrganizationId, engagementId, "Deploy Azure workloads.\nDeliver migration report.", CancellationToken.None);

        Assert.Equal(JobState.ReadyForReview, job.State);
        Assert.Equal("local-text-review-only", job.Provider);
        Assert.Equal(2, job.SourceCitations.Count);
        Assert.Equal("line 2", job.SourceCitations[1].Location);
        Assert.Contains("Partner qualifications", job.MissingInputs);
        Assert.Empty(service.ListJobs(OtherOrganizationId));
    }

    [Fact]
    public async Task Evidence_decision_is_single_submission_reviewed_once()
    {
        var (service, engagements, _, _) = Create();
        var engagementId = await CreateEngagementAsync(engagements);
        var evidence = await service.SubmitEvidenceAsync(OrganizationId, engagementId, "delivery report", "report.txt", "contributor", CancellationToken.None);
        var accepted = await service.ReviewEvidenceAsync(OrganizationId, evidence.Id, "reviewer", true, "Verified against scope.", CancellationToken.None);

        Assert.Equal(EvidenceState.Accepted, accepted.State);
        var duplicateDecision = await Assert.ThrowsAsync<WorkflowException>(() =>
            service.ReviewEvidenceAsync(OrganizationId, evidence.Id, "reviewer", false, "Changed decision.", CancellationToken.None));
        Assert.Equal("evidence_transition_invalid", duplicateDecision.Code);
    }

    [Fact]
    public async Task Claim_approval_and_payment_are_separate_audited_transitions()
    {
        var (service, engagements, _, audit) = Create();
        var engagementId = await CreateEngagementAsync(engagements);
        var claim = await service.CreateClaimAsync(OrganizationId, engagementId, "external-42", "USD", 250m, CancellationToken.None);
        var ready = await service.ChangeClaimStateAsync(OrganizationId, claim.Id, ClaimState.ReadyToSubmit, CancellationToken.None);
        var submitted = await service.ChangeClaimStateAsync(OrganizationId, ready.Id, ClaimState.Submitted, CancellationToken.None);
        var approved = await service.ChangeClaimStateAsync(OrganizationId, submitted.Id, ClaimState.Approved, CancellationToken.None);
        Assert.Null(approved.PaidAt);
        var paid = await service.ChangeClaimStateAsync(OrganizationId, approved.Id, ClaimState.Paid, CancellationToken.None);
        Assert.NotNull(paid.PaidAt);
        Assert.Contains(await audit.ListAsync(OrganizationId, CancellationToken.None), x => x.Action == "claim.paid");

        var impossible = await Assert.ThrowsAsync<WorkflowException>(() =>
            service.ChangeClaimStateAsync(OrganizationId, claim.Id, ClaimState.Paid, CancellationToken.None));
        Assert.Equal("claim_transition_invalid", impossible.Code);
    }

    [Fact]
    public async Task Task_transitions_are_organization_scoped_and_audited()
    {
        var (service, engagements, _, audit) = Create();
        var engagementId = await CreateEngagementAsync(engagements);
        var task = await service.CreateTaskAsync(OrganizationId, engagementId, "Collect delivery evidence", "owner", Today, CancellationToken.None);

        var hidden = await Assert.ThrowsAsync<WorkflowException>(() =>
            service.ChangeTaskStateAsync(OtherOrganizationId, task.Id, "done", "attacker", CancellationToken.None));
        Assert.Equal("task_not_found", hidden.Code);
        var done = await service.ChangeTaskStateAsync(OrganizationId, task.Id, "in_progress", "owner", CancellationToken.None);
        done = await service.ChangeTaskStateAsync(OrganizationId, done.Id, "done", "owner", CancellationToken.None);

        Assert.Equal("done", done.State);
        Assert.Contains(await audit.ListAsync(OrganizationId, CancellationToken.None), x => x.Action == "task.done");
    }
}
