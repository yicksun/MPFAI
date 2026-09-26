using MPFAI.Api.Domain;
using MPFAI.Api.Repositories;

namespace MPFAI.Api.Tests;

public sealed class OrganizationIsolationTests
{
    [Fact]
    public async Task Customer_and_engagement_repositories_hide_other_organizations()
    {
        var firstOrganization = Guid.NewGuid();
        var otherOrganization = Guid.NewGuid();
        var customerRepository = new InMemoryCustomerRepository();
        var engagementRepository = new InMemoryEngagementRepository();
        var customer = new Customer(Guid.NewGuid(), firstOrganization, "Contoso", null, DateTimeOffset.UtcNow);
        var engagement = new Engagement(Guid.NewGuid(), firstOrganization, customer.Id, "Migration", "proposed", DateTimeOffset.UtcNow);
        await customerRepository.CreateAsync(customer, CancellationToken.None);
        await engagementRepository.CreateAsync(engagement, CancellationToken.None);

        Assert.Empty(await customerRepository.ListAsync(otherOrganization, CancellationToken.None));
        Assert.Null(await customerRepository.GetAsync(otherOrganization, customer.Id, CancellationToken.None));
        Assert.Empty(await engagementRepository.ListAsync(otherOrganization, CancellationToken.None));
        Assert.Null(await engagementRepository.GetAsync(otherOrganization, engagement.Id, CancellationToken.None));
    }
}
