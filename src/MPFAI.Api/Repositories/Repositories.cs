using MPFAI.Api.Domain;

namespace MPFAI.Api.Repositories;

public interface ICustomerRepository
{
    Task<IReadOnlyList<Customer>> ListAsync(Guid organizationId, CancellationToken cancellationToken);
    Task<Customer?> GetAsync(Guid organizationId, Guid customerId, CancellationToken cancellationToken);
    Task<Customer> CreateAsync(Customer customer, CancellationToken cancellationToken);
}

public interface IEngagementRepository
{
    Task<IReadOnlyList<Engagement>> ListAsync(Guid organizationId, CancellationToken cancellationToken);
    Task<Engagement?> GetAsync(Guid organizationId, Guid engagementId, CancellationToken cancellationToken);
    Task<Engagement> CreateAsync(Engagement engagement, CancellationToken cancellationToken);
}

public interface IFundingGuidelineRepository
{
    Task<IReadOnlyList<FundingGuideline>> ListAsync(CancellationToken cancellationToken);
    Task<FundingGuideline?> GetAsync(string guidelineId, CancellationToken cancellationToken);
    Task<FundingGuideline> SaveAsync(FundingGuideline guideline, CancellationToken cancellationToken);
}

public interface IAuditRepository
{
    Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken);
    Task<IReadOnlyList<AuditEvent>> ListAsync(Guid organizationId, CancellationToken cancellationToken);
}

public sealed record AuditEvent(
    Guid Id,
    Guid OrganizationId,
    string Actor,
    string Action,
    string EntityType,
    string EntityId,
    DateTimeOffset OccurredAt,
    string? Details = null);
