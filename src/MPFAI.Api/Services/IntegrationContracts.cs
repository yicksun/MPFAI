using System.Collections.Concurrent;

namespace MPFAI.Api.Services;

public interface IImmutableDocumentStore
{
    Task PutAsync(Guid organizationId, string documentId, string fileName, ReadOnlyMemory<byte> content, CancellationToken token);
    Task<byte[]> GetAsync(Guid organizationId, string documentId, CancellationToken token);
}

public sealed class InMemoryDocumentStore : IImmutableDocumentStore
{
    private readonly ConcurrentDictionary<string, byte[]> _documents = new(StringComparer.Ordinal);

    public Task PutAsync(Guid organizationId, string documentId, string fileName, ReadOnlyMemory<byte> content, CancellationToken token)
    {
        var key = $"{organizationId:N}:{documentId}";
        if (!_documents.TryAdd(key, content.ToArray()))
            throw new InvalidOperationException("An immutable document with this identifier already exists.");
        return Task.CompletedTask;
    }

    public Task<byte[]> GetAsync(Guid organizationId, string documentId, CancellationToken token) =>
        _documents.TryGetValue($"{organizationId:N}:{documentId}", out var content)
            ? Task.FromResult(content.ToArray())
            : Task.FromException<byte[]>(new FileNotFoundException("Document not found in this organization."));
}

public interface IMalwareScanner
{
    Task<bool> IsConfiguredAsync(CancellationToken token);
    Task ScanAsync(ReadOnlyMemory<byte> content, CancellationToken token);
}

public sealed class UnconfiguredMalwareScanner : IMalwareScanner
{
    public Task<bool> IsConfiguredAsync(CancellationToken token) => Task.FromResult(false);
    public Task ScanAsync(ReadOnlyMemory<byte> content, CancellationToken token) =>
        Task.FromException(new IntegrationUnavailableException("malware_scanning_not_configured", "Malware scanning is not configured in local mode."));
}

public interface IPartnerCenterReportImporter
{
    Task ImportAsync(Guid organizationId, Stream report, CancellationToken token);
    Task<bool> IsConfiguredAsync(CancellationToken token);
}

public sealed class UnconfiguredPartnerCenterReportImporter : IPartnerCenterReportImporter
{
    public Task ImportAsync(Guid organizationId, Stream report, CancellationToken token) =>
        Task.FromException(new IntegrationUnavailableException("partner_center_not_configured", "Partner Center report import is not configured."));

    public Task<bool> IsConfiguredAsync(CancellationToken token) => Task.FromResult(false);
}

public sealed record IntegrationStatus(string Name, string State, string Detail);

public interface IIntegrationStatusProvider
{
    IReadOnlyList<IntegrationStatus> GetStatuses();
}

public sealed class LocalIntegrationStatusProvider : IIntegrationStatusProvider
{
    public IReadOnlyList<IntegrationStatus> GetStatuses() =>
    [
        new("authentication", "local_only", "Development profile only; Microsoft Entra authentication is not configured."),
        new("document_storage", "local_only", "Original guideline text is held in process memory."),
        new("malware_scanning", "not_configured", "Uploads are limited to UTF-8 text; no malware scan is performed."),
        new("partner_center", "not_configured", "No Partner Center report connector or data access is configured."),
        new("azure_ai", "not_configured", "No model, document extraction, or search service is configured.")
    ];
}

public sealed class IntegrationUnavailableException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
