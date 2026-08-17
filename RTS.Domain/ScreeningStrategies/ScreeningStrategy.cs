namespace RTS.Domain.ScreeningStrategies;

public sealed class ScreeningStrategy
{
    private readonly List<ScreeningStrategyVersion> _versions = [];

    private ScreeningStrategy()
    {
    }

    public long Id { get; private set; }

    public Guid ExternalId { get; private set; }

    // Null for an RTS-provided system template.
    public long? OwnerUserId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public bool IsActive { get; private set; }

    public IReadOnlyCollection<ScreeningStrategyVersion> Versions =>
        _versions;

    public static ScreeningStrategy Create(
        string name,
        string? description,
        long? ownerUserId)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "A screening-strategy name is required.",
                nameof(name));
        }

        if (ownerUserId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(ownerUserId),
                "The owner user ID must be positive when supplied.");
        }

        var strategy = new ScreeningStrategy
        {
            ExternalId = Guid.NewGuid(),
            OwnerUserId = ownerUserId,
            Name = name.Trim(),
            Description = NormalizeOptionalText(description),
            IsActive = true
        };

        strategy._versions.Add(
            ScreeningStrategyVersion.CreateDraft(
                versionNumber: 1));

        return strategy;
    }

    public ScreeningStrategyVersion CreateDraftVersion()
    {
        EnsureActive();

        if (_versions.Any(
                version =>
                    version.Status ==
                    ScreeningStrategyVersionStatus.Draft))
        {
            throw new InvalidOperationException(
                "The screening strategy already has a draft version.");
        }

        var nextVersionNumber = _versions.Count == 0
            ? 1
            : _versions.Max(
                version =>
                    version.VersionNumber) + 1;

        var draft =
            ScreeningStrategyVersion.CreateDraft(
                nextVersionNumber);

        _versions.Add(draft);

        return draft;
    }

    public ScreeningStrategyVersion PublishDraftVersion(
        Guid versionExternalId,
        DateTime publishedUtc)
    {
        EnsureActive();

        var draft = _versions.SingleOrDefault(
            version =>
                version.ExternalId ==
                versionExternalId);

        if (draft is null)
        {
            throw new InvalidOperationException(
                "The requested screening-strategy version does not belong to this strategy.");
        }

        draft.Publish(publishedUtc);

        foreach (var publishedVersion in _versions.Where(
                     version =>
                         version != draft &&
                         version.Status ==
                         ScreeningStrategyVersionStatus.Published))
        {
            publishedVersion.Retire();
        }

        return draft;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    private void EnsureActive()
    {
        if (!IsActive)
        {
            throw new InvalidOperationException(
                "An inactive screening strategy cannot be changed.");
        }
    }

    private static string? NormalizeOptionalText(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}