using RTS.Domain.TradingModels.Indicators;
using RTS.Domain.TradingModels.Timeframes;
using RTS.Domain.Rules;
using RTS.Domain.TradingModels.Criteria;

namespace RTS.Domain.TradingModels;

public sealed class TradingModelVersion
{
    private readonly List<TradingModelTimeframe> _timeframes = [];
    private readonly List<TradingModelCriterion> _criteria = [];
    public IReadOnlyCollection<TradingModelCriterion> Criteria =>
    _criteria;

    private TradingModelVersion()
    {
    }

    public long Id { get; private set; }

    public Guid ExternalId { get; private set; }

    public long TradingModelId { get; private set; }

    public int VersionNumber { get; private set; }

    public TradingModelVersionStatus Status { get; private set; }

    public DateTime CreatedUtc { get; private set; }

    public DateTime? PublishedUtc { get; private set; }

    public IReadOnlyCollection<TradingModelTimeframe> Timeframes =>
        _timeframes;

    internal static TradingModelVersion CreateDraft(
        int versionNumber)
    {
        if (versionNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(versionNumber),
                "A version number must be positive.");
        }

        return new TradingModelVersion
        {
            ExternalId = Guid.NewGuid(),
            VersionNumber = versionNumber,
            Status = TradingModelVersionStatus.Draft,
            CreatedUtc = DateTime.UtcNow
        };
    }

    public TradingModelTimeframe AddTimeframe(
        string name,
        int lookbackValue,
        TimeframeLookbackUnit lookbackUnit,
        int barIntervalValue,
        BarIntervalUnit barIntervalUnit,
        MarketSessionMode marketSessionMode)
    {
        EnsureDraft();

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "A timeframe name is required.",
                nameof(name));
        }

        var normalizedName = name.Trim();

        if (_timeframes.Any(
                timeframe =>
                    string.Equals(
                        timeframe.Name,
                        normalizedName,
                        StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                "A timeframe with the same name already exists.");
        }

        var timeframe = TradingModelTimeframe.Create(
            normalizedName,
            displayOrder: _timeframes.Count + 1,
            lookbackValue,
            lookbackUnit,
            barIntervalValue,
            barIntervalUnit,
            marketSessionMode);

        _timeframes.Add(timeframe);

        return timeframe;
    }

    public TradingModelIndicator AddSimpleMovingAverage(
        Guid timeframeExternalId,
        int period)
    {
        EnsureDraft();

        return GetTimeframe(timeframeExternalId)
            .AddSimpleMovingAverage(period);
    }

    public TradingModelIndicator AddBollingerBands(
        Guid timeframeExternalId,
        int period,
        decimal standardDeviations)
    {
        EnsureDraft();

        return GetTimeframe(timeframeExternalId)
            .AddBollingerBands(
                period,
                standardDeviations);
    }

    public TradingModelIndicator AddMacd(
        Guid timeframeExternalId,
        int fastPeriod,
        int slowPeriod,
        int signalPeriod)
    {
        EnsureDraft();

        return GetTimeframe(timeframeExternalId)
            .AddMacd(
                fastPeriod,
                slowPeriod,
                signalPeriod);
    }

    public TradingModelIndicator AddRelativeStrengthIndex(
        Guid timeframeExternalId,
        int period)
    {
        EnsureDraft();

        return GetTimeframe(timeframeExternalId)
            .AddRelativeStrengthIndex(period);
    }

    public TradingModelIndicator AddVolume(
        Guid timeframeExternalId)
    {
        EnsureDraft();

        return GetTimeframe(timeframeExternalId)
            .AddVolume();
    }

    public TradingModelCriterion AddCriterion(
    string name,
    string metricKey,
    RulePurpose purpose,
    ComparisonOperator comparisonOperator,
    RuleValue primaryValue,
    RuleValue? secondaryValue = null,
    decimal? weight = null)
    {
        EnsureDraft();

        if (!string.IsNullOrWhiteSpace(name) &&
            _criteria.Any(
                criterion =>
                    string.Equals(
                        criterion.Name,
                        name.Trim(),
                        StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                "A criterion with the same name already exists.");
        }

        var criterion = TradingModelCriterion.Create(
            name,
            metricKey,
            purpose,
            comparisonOperator,
            primaryValue,
            secondaryValue,
            weight,
            displayOrder: _criteria.Count + 1);

        _criteria.Add(criterion);

        return criterion;
    }

    internal void Publish(DateTime publishedUtc)
    {
        EnsureDraft();

        if (publishedUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "The publication timestamp must be UTC.",
                nameof(publishedUtc));
        }

        if (publishedUtc < CreatedUtc)
        {
            throw new ArgumentOutOfRangeException(
                nameof(publishedUtc),
                "The publication timestamp cannot precede creation.");
        }

        Status = TradingModelVersionStatus.Published;
        PublishedUtc = publishedUtc;
    }

    internal void Retire()
    {
        if (Status != TradingModelVersionStatus.Published)
        {
            throw new InvalidOperationException(
                "Only a published trading-model version can be retired.");
        }

        Status = TradingModelVersionStatus.Retired;
    }

    private TradingModelTimeframe GetTimeframe(
        Guid timeframeExternalId)
    {
        var timeframe = _timeframes.SingleOrDefault(
            item =>
                item.ExternalId ==
                timeframeExternalId);

        if (timeframe is null)
        {
            throw new InvalidOperationException(
                "The requested timeframe does not belong to this model version.");
        }

        return timeframe;
    }

    private void EnsureDraft()
    {
        if (Status != TradingModelVersionStatus.Draft)
        {
            throw new InvalidOperationException(
                "Only a draft trading-model version can be changed.");
        }
    }
}