namespace MPFAI.Api.Domain;

public sealed record Customer(Guid Id, Guid OrganizationId, string Name, string? Domain, DateTimeOffset CreatedAt);

public sealed record Engagement(
    Guid Id,
    Guid OrganizationId,
    Guid CustomerId,
    string Name,
    string Status,
    DateTimeOffset CreatedAt);

public sealed record Citation(string DocumentId, string Version, string Location, string Excerpt);

public sealed record FundingGuideline(
    string Id,
    string Version,
    decimal Rate,
    decimal Cap,
    string Currency,
    DateOnly EffectiveFrom,
    DateOnly EffectiveTo,
    bool Approved,
    bool Conflicted,
    IReadOnlyList<Citation> Citations);

public sealed record FundingCalculationRequest(
    string GuidelineId,
    decimal EligibleAmount,
    DateOnly AsOf,
    string? Currency = null);

public sealed record CalculationStep(string Name, decimal Input, decimal Result, string Formula);

public sealed record FundingCalculationResult(
    string GuidelineId,
    string GuidelineVersion,
    string Currency,
    decimal EligibleAmount,
    decimal Rate,
    decimal UncappedAmount,
    decimal Cap,
    decimal Amount,
    string FormulaVersion,
    IReadOnlyList<CalculationStep> Trace,
    IReadOnlyList<Citation> Citations);
