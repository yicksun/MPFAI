using MPFAI.Api.Domain;
using MPFAI.Api.Repositories;

namespace MPFAI.Api.Services;

public sealed class FundingCalculator(IFundingGuidelineRepository guidelines)
{
    public const string FormulaVersion = "percentage-with-cap-v1";

    public async Task<FundingCalculationResult> CalculateAsync(
        FundingCalculationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.EligibleAmount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "Eligible amount cannot be negative.");
        }

        var guideline = await guidelines.GetAsync(request.GuidelineId, cancellationToken)
            ?? throw new FundingCalculationException("guideline_missing", "The requested guideline does not exist.");

        if (!guideline.Approved)
        {
            throw new FundingCalculationException("guideline_unapproved", "The guideline has not been approved.");
        }

        if (guideline.Conflicted)
        {
            throw new FundingCalculationException("guideline_conflicted", "The guideline has an unresolved conflict.");
        }

        if (request.AsOf < guideline.EffectiveFrom || request.AsOf > guideline.EffectiveTo)
        {
            throw new FundingCalculationException("guideline_expired", "The guideline is not effective for the requested date.");
        }

        if (guideline.Rate < 0 || guideline.Rate > 1 || guideline.Cap < 0 || string.IsNullOrWhiteSpace(guideline.Currency))
        {
            throw new FundingCalculationException("guideline_invalid", "The guideline has invalid calculation parameters.");
        }

        if (request.Currency is not null && !string.Equals(request.Currency, guideline.Currency, StringComparison.OrdinalIgnoreCase))
        {
            throw new FundingCalculationException("currency_mismatch", "The request currency does not match the guideline currency.");
        }

        var uncapped = decimal.Round(request.EligibleAmount * guideline.Rate, 2, MidpointRounding.AwayFromZero);
        var amount = Math.Min(uncapped, guideline.Cap);
        var trace = new[]
        {
            new CalculationStep("percentage", request.EligibleAmount, uncapped, $"{request.EligibleAmount} × {guideline.Rate} = {uncapped}"),
            new CalculationStep("cap", uncapped, amount, $"min({uncapped}, {guideline.Cap}) = {amount}")
        };

        return new FundingCalculationResult(
            guideline.Id,
            guideline.Version,
            guideline.Currency,
            request.EligibleAmount,
            guideline.Rate,
            uncapped,
            guideline.Cap,
            amount,
            FormulaVersion,
            trace,
            guideline.Citations);
    }
}

public sealed class FundingCalculationException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
