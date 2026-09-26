using MPFAI.Api.Services;

namespace MPFAI.Api.Tests;

public sealed class IntegrationContractTests
{
    [Fact]
    public async Task Local_document_store_is_immutable_and_organization_scoped()
    {
        var store = new InMemoryDocumentStore();
        var organizationId = Guid.NewGuid();
        var content = new byte[] { 1, 2, 3 };
        await store.PutAsync(organizationId, "document-1", "source.txt", content, CancellationToken.None);
        content[0] = 9;

        var stored = await store.GetAsync(organizationId, "document-1", CancellationToken.None);
        Assert.Equal(new byte[] { 1, 2, 3 }, stored);
        stored[1] = 9;
        Assert.Equal(new byte[] { 1, 2, 3 }, await store.GetAsync(organizationId, "document-1", CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            store.PutAsync(organizationId, "document-1", "replacement.txt", new byte[] { 4 }, CancellationToken.None));
        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            store.GetAsync(Guid.NewGuid(), "document-1", CancellationToken.None));
    }

    [Fact]
    public async Task Unavailable_external_integrations_are_never_reported_as_configured_or_successful()
    {
        var scanner = new UnconfiguredMalwareScanner();
        var importer = new UnconfiguredPartnerCenterReportImporter();
        Assert.False(await scanner.IsConfiguredAsync(CancellationToken.None));
        Assert.False(await importer.IsConfiguredAsync(CancellationToken.None));
        Assert.Equal("malware_scanning_not_configured",
            (await Assert.ThrowsAsync<IntegrationUnavailableException>(() => scanner.ScanAsync(new byte[] { 1 }, CancellationToken.None))).Code);
        Assert.Equal("partner_center_not_configured",
            (await Assert.ThrowsAsync<IntegrationUnavailableException>(() => importer.ImportAsync(Guid.NewGuid(), Stream.Null, CancellationToken.None))).Code);
        Assert.Contains(new LocalIntegrationStatusProvider().GetStatuses(), status => status.Name == "azure_ai" && status.State == "not_configured");
    }
}
