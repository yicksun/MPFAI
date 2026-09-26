using System.Collections.Concurrent;
using MPFAI.Api.Domain;

namespace MPFAI.Api.Repositories;

public sealed class InMemoryCustomerRepository : ICustomerRepository
{
    private readonly ConcurrentDictionary<Guid, Customer> _customers = new();

    public Task<IReadOnlyList<Customer>> ListAsync(Guid organizationId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Customer>>(_customers.Values.Where(x => x.OrganizationId == organizationId).OrderBy(x => x.Name).ToArray());

    public Task<Customer?> GetAsync(Guid organizationId, Guid customerId, CancellationToken cancellationToken) =>
        Task.FromResult(_customers.TryGetValue(customerId, out var customer) && customer.OrganizationId == organizationId ? customer : null);

    public Task<Customer> CreateAsync(Customer customer, CancellationToken cancellationToken)
    {
        if (!_customers.TryAdd(customer.Id, customer))
        {
            throw new InvalidOperationException("A customer with this identifier already exists.");
        }

        return Task.FromResult(customer);
    }
}

public sealed class InMemoryEngagementRepository : IEngagementRepository
{
    private readonly ConcurrentDictionary<Guid, Engagement> _engagements = new();

    public Task<IReadOnlyList<Engagement>> ListAsync(Guid organizationId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Engagement>>(_engagements.Values.Where(x => x.OrganizationId == organizationId).OrderBy(x => x.Name).ToArray());

    public Task<Engagement?> GetAsync(Guid organizationId, Guid engagementId, CancellationToken cancellationToken) =>
        Task.FromResult(_engagements.TryGetValue(engagementId, out var engagement) && engagement.OrganizationId == organizationId ? engagement : null);

    public Task<Engagement> CreateAsync(Engagement engagement, CancellationToken cancellationToken)
    {
        if (!_engagements.TryAdd(engagement.Id, engagement))
        {
            throw new InvalidOperationException("An engagement with this identifier already exists.");
        }

        return Task.FromResult(engagement);
    }
}

public sealed class InMemoryFundingGuidelineRepository : IFundingGuidelineRepository
{
    private readonly ConcurrentDictionary<string, FundingGuideline> _guidelines = new(StringComparer.Ordinal);

    public InMemoryFundingGuidelineRepository(IEnumerable<FundingGuideline>? initial = null)
    {
        if (initial is null)
        {
            return;
        }

        foreach (var guideline in initial)
        {
            _guidelines[Key(guideline.OrganizationId, guideline.Id)] = guideline;
        }
    }

    public Task<IReadOnlyList<FundingGuideline>> ListAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<FundingGuideline>>(_guidelines.Values.OrderBy(x => x.Id).ToArray());

    public Task<FundingGuideline?> GetAsync(Guid organizationId, string guidelineId, CancellationToken cancellationToken) =>
        Task.FromResult(_guidelines.GetValueOrDefault(Key(organizationId, guidelineId)));

    public Task<FundingGuideline> SaveAsync(FundingGuideline guideline, CancellationToken cancellationToken)
    {
        _guidelines.AddOrUpdate(Key(guideline.OrganizationId, guideline.Id), guideline, (_, existing) =>
        {
            if (string.CompareOrdinal(guideline.Version, existing.Version) <= 0)
            {
                throw new InvalidOperationException("Guideline versions must increase; approved versions are immutable.");
            }

            return guideline;
        });
        return Task.FromResult(guideline);
    }

    public Task DeactivateAsync(Guid organizationId, string guidelineId, CancellationToken cancellationToken)
    {
        var key = Key(organizationId, guidelineId);
        if (_guidelines.TryGetValue(key, out var guideline))
        {
            _guidelines[key] = guideline with { Approved = false };
        }

        return Task.CompletedTask;
    }

    private static string Key(Guid organizationId, string guidelineId) => $"{organizationId:N}:{guidelineId}";
}

public sealed class InMemoryAuditRepository : IAuditRepository
{
    private readonly ConcurrentQueue<AuditEvent> _events = new();

    public Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken)
    {
        _events.Enqueue(auditEvent);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<AuditEvent>> ListAsync(Guid organizationId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<AuditEvent>>(_events.Where(x => x.OrganizationId == organizationId).OrderBy(x => x.OccurredAt).ToArray());
}
