namespace RTS.Domain.TradingModels;

public sealed class TradingModel
{
    private readonly List<TradingModelVersion> _versions = [];

    // Required by EF Core.
    private TradingModel()
    {
    }

    public long Id { get; private set; }

    public Guid ExternalId { get; private set; }

    // Null for an RTS-provided system template.
    public long? OwnerUserId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public bool IsActive { get; private set; }

    public IReadOnlyCollection<TradingModelVersion> Versions => _versions;

    public static TradingModel Create(
        string name,
        string? description,
        long? ownerUserId)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "A trading model name is required.",
                nameof(name));
        }

        if (ownerUserId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(ownerUserId),
                "The owner user ID must be positive when supplied.");
        }

        var model = new TradingModel
        {
            ExternalId = Guid.NewGuid(),
            OwnerUserId = ownerUserId,
            Name = name.Trim(),
            Description = NormalizeOptionalText(description),
            IsActive = true
        };

        model._versions.Add(
            TradingModelVersion.CreateDraft(versionNumber: 1));

        return model;
    }


    public TradingModelVersion CreateDraftVersion()
    {
        EnsureActive();

        if (_versions.Any(
                version =>
                    version.Status == TradingModelVersionStatus.Draft))
        {
            throw new InvalidOperationException(
                "The trading model already has a draft version.");
        }

        var nextVersionNumber = _versions.Count == 0
            ? 1
            : _versions.Max(version => version.VersionNumber) + 1;

        var draft = TradingModelVersion.CreateDraft(nextVersionNumber);

        _versions.Add(draft);

        return draft;
    }

    public TradingModelVersion PublishDraftVersion(
        Guid versionExternalId,
        DateTime publishedUtc)
    {
        EnsureActive();

        var draft = _versions.SingleOrDefault(
            version => version.ExternalId == versionExternalId);

        if (draft is null)
        {
            throw new InvalidOperationException(
                "The requested trading-model version does not belong to this model.");
        }

        // Publish first so validation finishes before changing another version.
        draft.Publish(publishedUtc);

        foreach (var publishedVersion in _versions.Where(
                     version =>
                         version != draft &&
                         version.Status ==
                         TradingModelVersionStatus.Published))
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
                "An inactive trading model cannot be changed.");
        }
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}