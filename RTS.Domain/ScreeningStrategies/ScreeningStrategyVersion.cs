using RTS.Domain.Rules;

namespace RTS.Domain.ScreeningStrategies;

public sealed class ScreeningStrategyVersion
{
    private readonly List<ScreeningRule> _rules = [];

    private ScreeningStrategyVersion()
    {
    }

    public long Id { get; private set; }

    public Guid ExternalId { get; private set; }

    public long ScreeningStrategyId { get; private set; }

    public int VersionNumber { get; private set; }

    public ScreeningStrategyVersionStatus Status { get; private set; }

    public DateTime CreatedUtc { get; private set; }

    public DateTime? PublishedUtc { get; private set; }

    public IReadOnlyCollection<ScreeningRule> Rules =>
        _rules;

    internal static ScreeningStrategyVersion CreateDraft(
        int versionNumber)
    {
        if (versionNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(versionNumber),
                "A version number must be positive.");
        }

        return new ScreeningStrategyVersion
        {
            ExternalId = Guid.NewGuid(),
            VersionNumber = versionNumber,
            Status = ScreeningStrategyVersionStatus.Draft,
            CreatedUtc = DateTime.UtcNow
        };
    }

    public ScreeningRule AddRule(
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
            _rules.Any(
                rule =>
                    string.Equals(
                        rule.Name,
                        name.Trim(),
                        StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                "A screening rule with the same name already exists.");
        }

        var rule = ScreeningRule.Create(
            name,
            metricKey,
            purpose,
            comparisonOperator,
            primaryValue,
            secondaryValue,
            weight,
            displayOrder: _rules.Count + 1);

        _rules.Add(rule);

        return rule;
    }

    internal void Publish(
        DateTime publishedUtc)
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

        Status = ScreeningStrategyVersionStatus.Published;
        PublishedUtc = publishedUtc;
    }

    internal void Retire()
    {
        if (Status != ScreeningStrategyVersionStatus.Published)
        {
            throw new InvalidOperationException(
                "Only a published screening-strategy version can be retired.");
        }

        Status = ScreeningStrategyVersionStatus.Retired;
    }

    private void EnsureDraft()
    {
        if (Status != ScreeningStrategyVersionStatus.Draft)
        {
            throw new InvalidOperationException(
                "Only a draft screening-strategy version can be changed.");
        }
    }
}