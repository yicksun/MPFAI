using MPFAI.Api.Domain;
using MPFAI.Api.Repositories;
using MPFAI.Api.Services;

namespace MPFAI.Api.Tests;

public sealed class FundingCalculatorTests
{
    private static readonly DateOnly CurrentDate = new(2026, 9, 26);

    private static FundingGuideline Guideline(
        bool approved = true,
        bool conflicted = false,
        DateOnly? from = null,
        DateOnly? to = null) =>
        new("pilot", "2", 0.15m, 5000m, "USD", from ?? new DateOnly(2026, 1, 1), to ?? new DateOnly(2026, 12, 31),
            approved, conflicted, [new Citation("guide.pdf", "2", "p. 4, Funding table", "Eligible work reimbursed at 15%, capped at $5,000.")]);

    [Theory]
    [InlineData(0, 0)]
    [InlineData(100, 15)]
    [InlineData(333.33, 50)]
    [InlineData(33333.33, 5000)]
    [InlineData(50000, 5000)]
    public async Task Calculates_percentage_and_cap_boundaries(decimal baseAmount, decimal expected)
    {
        var calculator = new FundingCalculator(new InMemoryFundingGuidelineRepository([Guideline()]));
        var result = await calculator.CalculateAsync(new FundingCalculationRequest("pilot", baseAmount, CurrentDate));
        Assert.Equal(expected, result.Amount);
        Assert.Equal(FundingCalculator.FormulaVersion, result.FormulaVersion);
        Assert.Equal(2, result.Trace.Count);
        Assert.Single(result.Citations);
    }

    [Fact]
    public async Task Repeated_calculation_is_identical()
    {
        var calculator = new FundingCalculator(new InMemoryFundingGuidelineRepository([Guideline()]));
        var request = new FundingCalculationRequest("pilot", 12345.67m, CurrentDate);
        var first = await calculator.CalculateAsync(request);
        var second = await calculator.CalculateAsync(request);
        Assert.Equal(first.Amount, second.Amount);
        Assert.Equal(first.GuidelineVersion, second.GuidelineVersion);
        Assert.Equal(first.FormulaVersion, second.FormulaVersion);
        Assert.Equal(first.Trace, second.Trace);
    }

    [Theory]
    [InlineData(false, false, "guideline_unapproved")]
    [InlineData(true, true, "guideline_conflicted")]
    public async Task Rejects_unapproved_or_conflicting_guidelines(bool approved, bool conflicted, string code)
    {
        var calculator = new FundingCalculator(new InMemoryFundingGuidelineRepository([Guideline(approved, conflicted)]));
        var exception = await Assert.ThrowsAsync<FundingCalculationException>(() =>
            calculator.CalculateAsync(new FundingCalculationRequest("pilot", 100, CurrentDate)));
        Assert.Equal(code, exception.Code);
    }

    [Fact]
    public async Task Rejects_missing_guideline()
    {
        var calculator = new FundingCalculator(new InMemoryFundingGuidelineRepository());
        var exception = await Assert.ThrowsAsync<FundingCalculationException>(() =>
            calculator.CalculateAsync(new FundingCalculationRequest("missing", 100, CurrentDate)));
        Assert.Equal("guideline_missing", exception.Code);
    }

    [Fact]
    public async Task Rejects_expired_guideline()
    {
        var calculator = new FundingCalculator(new InMemoryFundingGuidelineRepository([Guideline(to: new DateOnly(2026, 1, 31))]));
        var exception = await Assert.ThrowsAsync<FundingCalculationException>(() =>
            calculator.CalculateAsync(new FundingCalculationRequest("pilot", 100, CurrentDate)));
        Assert.Equal("guideline_expired", exception.Code);
    }

    [Fact]
    public async Task Rejects_currency_mismatch()
    {
        var calculator = new FundingCalculator(new InMemoryFundingGuidelineRepository([Guideline()]));
        var exception = await Assert.ThrowsAsync<FundingCalculationException>(() =>
            calculator.CalculateAsync(new FundingCalculationRequest("pilot", 100, CurrentDate, "EUR")));
        Assert.Equal("currency_mismatch", exception.Code);
    }
}
