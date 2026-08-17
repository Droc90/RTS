using RTS.Domain.ScreeningStrategies;
using RTS.Domain.TradingModels;

namespace RTS.Application.TradingConfiguration;

public interface ITradingConfigurationPublicationService
{
    ConfigurationPublicationResult<TradingModelVersion>
        PublishTradingModel(
            TradingModel model,
            Guid versionExternalId,
            DateTime publishedUtc);

    ConfigurationPublicationResult<ScreeningStrategyVersion>
        PublishScreeningStrategy(
            ScreeningStrategy strategy,
            Guid versionExternalId,
            DateTime publishedUtc);
}

public sealed record ConfigurationPublicationResult<TVersion>(
    TVersion? PublishedVersion,
    IReadOnlyCollection<ConfigurationValidationIssue> Issues)
{
    public bool IsPublished =>
        PublishedVersion is not null;
}