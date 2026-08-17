using RTS.Domain.TradingModels;

namespace RTS.Domain.Tests.TradingModels;

public sealed class TradingModelTests
{
    [Fact]
    public void Create_WithUserOwner_CreatesActiveUserModel()
    {
        var model = TradingModel.Create(
            "  Growth Model  ",
            "  Evaluates growth candidates.  ",
            ownerUserId: 42);

        Assert.NotEqual(Guid.Empty, model.ExternalId);
        Assert.Equal(42, model.OwnerUserId);
        Assert.Equal("Growth Model", model.Name);
        Assert.Equal(
            "Evaluates growth candidates.",
            model.Description);
        Assert.True(model.IsActive);
    }

    [Fact]
    public void Create_WithoutOwner_CreatesSystemTemplate()
    {
        var model = TradingModel.Create(
            "RTS Standard",
            description: null,
            ownerUserId: null);

        Assert.Null(model.OwnerUserId);
        Assert.Null(model.Description);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankName_ThrowsArgumentException(
        string name)
    {
        Assert.Throws<ArgumentException>(
            () => TradingModel.Create(name, null, 1));
    }

    [Fact]
    public void Create_WithNullName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => TradingModel.Create(null!, null, 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithInvalidOwnerId_ThrowsArgumentOutOfRangeException(
        long ownerUserId)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => TradingModel.Create(
                "Test Model",
                null,
                ownerUserId));
    }

    [Fact]
    public void Create_AutomaticallyCreatesFirstDraftVersion()
    {
        var model = CreateModel();

        var version = Assert.Single(model.Versions);

        Assert.NotEqual(Guid.Empty, version.ExternalId);
        Assert.Equal(1, version.VersionNumber);
        Assert.Equal(
            TradingModelVersionStatus.Draft,
            version.Status);
        Assert.NotEqual(default, version.CreatedUtc);
        Assert.Null(version.PublishedUtc);
    }

    [Fact]
    public void CreateDraftVersion_WhenDraftExists_ThrowsInvalidOperationException()
    {
        var model = CreateModel();

        Assert.Throws<InvalidOperationException>(
            () => model.CreateDraftVersion());
    }

    [Fact]
    public void CreateDraftVersion_AfterPublishing_CreatesNextVersion()
    {
        var model = CreateModel();
        var firstVersion = Assert.Single(model.Versions);

        model.PublishDraftVersion(
            firstVersion.ExternalId,
            firstVersion.CreatedUtc.AddMinutes(1));

        var secondVersion = model.CreateDraftVersion();

        Assert.Equal(2, secondVersion.VersionNumber);
        Assert.Equal(
            TradingModelVersionStatus.Draft,
            secondVersion.Status);
    }

    [Fact]
    public void PublishDraftVersion_PublishesRequestedDraft()
    {
        var model = CreateModel();
        var draft = Assert.Single(model.Versions);
        var publishedUtc = draft.CreatedUtc.AddMinutes(1);

        var publishedVersion = model.PublishDraftVersion(
            draft.ExternalId,
            publishedUtc);

        Assert.Same(draft, publishedVersion);
        Assert.Equal(
            TradingModelVersionStatus.Published,
            publishedVersion.Status);
        Assert.Equal(
            publishedUtc,
            publishedVersion.PublishedUtc);
    }

    [Fact]
    public void PublishDraftVersion_RetiresPreviouslyPublishedVersion()
    {
        var model = CreateModel();
        var firstVersion = Assert.Single(model.Versions);

        model.PublishDraftVersion(
            firstVersion.ExternalId,
            firstVersion.CreatedUtc.AddMinutes(1));

        var secondVersion = model.CreateDraftVersion();

        model.PublishDraftVersion(
            secondVersion.ExternalId,
            secondVersion.CreatedUtc.AddMinutes(1));

        Assert.Equal(
            TradingModelVersionStatus.Retired,
            firstVersion.Status);
        Assert.Equal(
            TradingModelVersionStatus.Published,
            secondVersion.Status);
    }

    [Fact]
    public void PublishDraftVersion_WithUnknownVersion_ThrowsInvalidOperationException()
    {
        var model = CreateModel();

        Assert.Throws<InvalidOperationException>(
            () => model.PublishDraftVersion(
                Guid.NewGuid(),
                DateTime.UtcNow));
    }

    [Fact]
    public void CreateDraftVersion_WhenModelInactive_ThrowsInvalidOperationException()
    {
        var model = CreateModel();
        model.Deactivate();

        Assert.Throws<InvalidOperationException>(
            () => model.CreateDraftVersion());
    }

    [Fact]
    public void PublishDraftVersion_WhenModelInactive_ThrowsInvalidOperationException()
    {
        var model = CreateModel();
        var draft = Assert.Single(model.Versions);
        model.Deactivate();

        Assert.Throws<InvalidOperationException>(
            () => model.PublishDraftVersion(
                draft.ExternalId,
                draft.CreatedUtc.AddMinutes(1)));
    }

    [Fact]
    public void PublishDraftVersion_WithNonUtcTimestamp_ThrowsArgumentException()
    {
        var model = CreateModel();
        var draft = Assert.Single(model.Versions);

        var localTimestamp = DateTime.SpecifyKind(
            draft.CreatedUtc.AddMinutes(1),
            DateTimeKind.Local);

        Assert.Throws<ArgumentException>(
            () => model.PublishDraftVersion(
                draft.ExternalId,
                localTimestamp));
    }

    [Fact]
    public void PublishDraftVersion_BeforeCreationTime_ThrowsArgumentOutOfRangeException()
    {
        var model = CreateModel();
        var draft = Assert.Single(model.Versions);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => model.PublishDraftVersion(
                draft.ExternalId,
                draft.CreatedUtc.AddMinutes(-1)));
    }

    private static TradingModel CreateModel()
    {
        return TradingModel.Create(
            "Test Model",
            "Model used by domain tests.",
            ownerUserId: 42);
    }
}