using RTS.Domain.ScreeningStrategies;
using RTS.Domain.TradingModels;

namespace RTS.Application.TradingConfiguration;

public sealed class TradingConfigurationPublicationService
    : ITradingConfigurationPublicationService
{
    private readonly ITradingConfigurationValidator _validator;

    public TradingConfigurationPublicationService(
        ITradingConfigurationValidator validator)
    {
        _validator = validator;
    }

    public ConfigurationPublicationResult<TradingModelVersion>
        PublishTradingModel(
            TradingModel model,
            Guid versionExternalId,
            DateTime publishedUtc)
    {
        ArgumentNullException.ThrowIfNull(model);

        var version = model.Versions.SingleOrDefault(
            item =>
                item.ExternalId ==
                versionExternalId);

        if (version is null)
        {
            throw new InvalidOperationException(
                "The requested trading-model version does not belong to this model.");
        }

        var validation = _validator.Validate(version);

        if (!validation.IsValid)
        {
            return new ConfigurationPublicationResult<
                TradingModelVersion>(
                PublishedVersion: null,
                validation.Issues);
        }

        var publishedVersion =
            model.PublishDraftVersion(
                versionExternalId,
                publishedUtc);

        return new ConfigurationPublicationResult<
            TradingModelVersion>(
                publishedVersion,
                validation.Issues);
    }

    public ConfigurationPublicationResult<
        ScreeningStrategyVersion>
        PublishScreeningStrategy(
            ScreeningStrategy strategy,
            Guid versionExternalId,
            DateTime publishedUtc)
    {
        ArgumentNullException.ThrowIfNull(strategy);

        var version = strategy.Versions.SingleOrDefault(
            item =>
                item.ExternalId ==
                versionExternalId);

        if (version is null)
        {
            throw new InvalidOperationException(
                "The requested screening-strategy version does not belong to this strategy.");
        }

        var validation = _validator.Validate(version);

        if (!validation.IsValid)
        {
            return new ConfigurationPublicationResult<
                ScreeningStrategyVersion>(
                PublishedVersion: null,
                validation.Issues);
        }

        var publishedVersion =
            strategy.PublishDraftVersion(
                versionExternalId,
                publishedUtc);

        return new ConfigurationPublicationResult<
            ScreeningStrategyVersion>(
                publishedVersion,
                validation.Issues);
    }
}