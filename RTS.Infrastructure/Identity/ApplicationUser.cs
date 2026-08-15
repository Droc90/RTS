using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Identity;

namespace RTS.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<long>
{
    public ApplicationUser()
    {
        LockoutEnabled = true;
    }

    public Guid ExternalId { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedUtc { get; set; }

    public long? CreatedByUserId { get; set; }

    public DateTime? ModifiedUtc { get; set; }

    public long? ModifiedByUserId { get; set; }

    public DateTime? LastLoginUtc { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedUtc { get; set; }

    public long? DeletedByUserId { get; set; }

    public byte[] RowVersion { get; set; } = [];
}