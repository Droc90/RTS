namespace RTS.Infrastructure.Profiles;

public sealed class UserProfile
{
    public long Id { get; set; }

    public Guid ExternalId { get; set; }

    public long UserId { get; set; }

    public string? DisplayName { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? TimeZoneId { get; set; }

    public string? Locale { get; set; }

    public string? PreferredDateFormat { get; set; }

    public bool IsOnboardingComplete { get; set; }

    public DateTime CreatedUtc { get; set; }

    public long? CreatedByUserId { get; set; }

    public DateTime? ModifiedUtc { get; set; }

    public long? ModifiedByUserId { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedUtc { get; set; }

    public long? DeletedByUserId { get; set; }

    public byte[] RowVersion { get; set; } = [];
}