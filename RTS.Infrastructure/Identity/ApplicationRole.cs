using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Identity;

namespace RTS.Infrastructure.Identity;

public sealed class ApplicationRole : IdentityRole<long>
{
    public Guid ExternalId { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedUtc { get; set; }

    public long? CreatedByUserId { get; set; }

    public DateTime? ModifiedUtc { get; set; }

    public long? ModifiedByUserId { get; set; }

    public byte[] RowVersion { get; set; } = [];
}