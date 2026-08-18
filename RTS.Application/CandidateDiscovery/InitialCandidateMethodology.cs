using RTS.Domain.Rules;
using RTS.Domain.ScreeningStrategies;

namespace RTS.Application.CandidateDiscovery;

public static class InitialCandidateMethodology
{
    public static ScreeningStrategy Create(DateTime publishedUtc)
    {
        var strategy = ScreeningStrategy.Create(
            "RTS Initial Candidate Screen",
            "Liquid, reasonably priced securities with positive revenue and earnings growth.",
            ownerUserId: null);
        var version = strategy.Versions.Single();

        version.AddRule("Minimum price", "Market.Price", RulePurpose.Filter, ComparisonOperator.GreaterThanOrEqual, RuleValue.FromDecimal(5m));
        version.AddRule("Minimum liquidity", "Market.AverageDailyDollarVolume", RulePurpose.Filter, ComparisonOperator.GreaterThanOrEqual, RuleValue.FromDecimal(20_000_000m));
        version.AddRule("Revenue growth", "Fundamental.RevenueGrowth", RulePurpose.Score, ComparisonOperator.GreaterThan, RuleValue.FromPercentage(0m), weight: 40m);
        version.AddRule("Earnings growth", "Fundamental.EarningsGrowth", RulePurpose.Score, ComparisonOperator.GreaterThan, RuleValue.FromPercentage(0m), weight: 40m);
        version.AddRule("Elevated valuation", "Fundamental.ForwardPriceToEarningsRatio", RulePurpose.Warning, ComparisonOperator.GreaterThan, RuleValue.FromDecimal(40m));

        if (publishedUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("The publication timestamp must be UTC.", nameof(publishedUtc));

        strategy.PublishDraftVersion(
            version.ExternalId,
            publishedUtc < version.CreatedUtc ? version.CreatedUtc : publishedUtc);
        return strategy;
    }
}
