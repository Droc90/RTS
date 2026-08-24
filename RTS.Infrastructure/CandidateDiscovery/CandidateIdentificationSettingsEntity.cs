namespace RTS.Infrastructure.CandidateDiscovery;

public sealed class CandidateIdentificationSettingsEntity
{
    public long Id { get; set; }
    public Guid ExternalId { get; set; }
    public long UserId { get; set; }
    public string SettingsJson { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }
    public DateTime? ModifiedUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
