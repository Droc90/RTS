using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RTS.Application.Administration;
using RTS.Application.Identity;
using RTS.Contracts.Administration;
using RTS.Infrastructure.Identity;
using RTS.Infrastructure.Persistence;
using RTS.Application.Auditing;
using System.Data;

namespace RTS.Infrastructure.Administration;

public sealed class UserAdministrationService
    : IUserAdministrationService
{
    private readonly RtsDbContext _dbContext;
    private readonly IAuditService _auditService;

    public UserAdministrationService(
        RtsDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        IAuditService auditService)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _auditService = auditService;
    }

    private readonly UserManager<ApplicationUser> _userManager;

    public async Task<UserSearchResult> SearchUsersAsync(
        UserSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var searchTerm = request.SearchTerm?.Trim();

        var query = _dbContext.Users
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(user =>
                user.Email != null &&
                user.Email.Contains(searchTerm));
        }

        var totalCount = await query.CountAsync(
            cancellationToken);

        var users = await query
            .OrderBy(user => user.Email)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(user => new
            {
                user.Id,
                user.ExternalId,
                user.Email,
                user.IsActive,
                user.IsDeleted,
                user.CreatedUtc,
                user.LastLoginUtc
            })
            .ToListAsync(cancellationToken);

        var userIds = users
            .Select(user => user.Id)
            .ToArray();

        var roleAssignments = await (
            from userRole in _dbContext
                .Set<IdentityUserRole<long>>()
            join role in _dbContext.Roles
                on userRole.RoleId equals role.Id
            where userIds.Contains(userRole.UserId)
            select new
            {
                userRole.UserId,
                role.Name
            })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var rolesByUserId = roleAssignments
            .GroupBy(assignment => assignment.UserId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyCollection<string>)group
                    .Select(assignment =>
                        assignment.Name ?? string.Empty)
                    .Where(roleName =>
                        !string.IsNullOrWhiteSpace(roleName))
                    .OrderBy(roleName => roleName)
                    .ToArray());

        var summaries = users
            .Select(user => new UserSummary(
                user.ExternalId,
                user.Email ?? string.Empty,
                user.IsActive,
                user.IsDeleted,
                user.CreatedUtc,
                user.LastLoginUtc,
                rolesByUserId.GetValueOrDefault(
                    user.Id,
                    Array.Empty<string>())))
            .ToArray();

        return new UserSearchResult(
            summaries,
            totalCount,
            pageNumber,
            pageSize);
    }

    public async Task<UserDetails?> GetUserAsync(
    Guid externalId,
    CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .Where(user => user.ExternalId == externalId)
            .Select(user => new
            {
                user.Id,
                user.ExternalId,
                user.Email,
                user.UserName,
                user.EmailConfirmed,
                user.PhoneNumber,
                user.PhoneNumberConfirmed,
                user.TwoFactorEnabled,
                user.LockoutEnd,
                user.LockoutEnabled,
                user.AccessFailedCount,
                user.IsActive,
                user.CreatedUtc,
                user.ModifiedUtc,
                user.LastLoginUtc,
                user.IsDeleted,
                user.DeletedUtc
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            return null;
        }

        var roles = await (
            from userRole in _dbContext
                .Set<IdentityUserRole<long>>()
            join role in _dbContext.Roles
                on userRole.RoleId equals role.Id
            where userRole.UserId == user.Id
            orderby role.Name
            select role.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return new UserDetails(
            user.ExternalId,
            user.Email ?? string.Empty,
            user.UserName ?? string.Empty,
            user.EmailConfirmed,
            user.PhoneNumber,
            user.PhoneNumberConfirmed,
            user.TwoFactorEnabled,
            user.LockoutEnd,
            user.LockoutEnabled,
            user.AccessFailedCount,
            user.IsActive,
            user.CreatedUtc,
            user.ModifiedUtc,
            user.LastLoginUtc,
            user.IsDeleted,
            user.DeletedUtc,
            roles
                .Where(roleName =>
                    !string.IsNullOrWhiteSpace(roleName))
                .Select(roleName => roleName!)
                .ToArray());
    }

    public async Task<UserAdministrationResult>
    SetActiveStatusAsync(
        Guid targetUserExternalId,
        bool isActive,
        Guid actingUserExternalId,
        CancellationToken cancellationToken = default)
    {
        var actingUser = await _dbContext.Users
            .SingleOrDefaultAsync(
                user => user.ExternalId ==
                    actingUserExternalId,
                cancellationToken);

        if (actingUser is null ||
            !actingUser.IsActive ||
            actingUser.IsDeleted)
        {
            return UserAdministrationResult.Failure(
                "The acting administrator account is invalid.");
        }

        if (!await _userManager.IsInRoleAsync(
            actingUser,
            SystemRoleNames.Administrator))
        {
            return UserAdministrationResult.Failure(
                "Administrator access is required.");
        }

        var targetUser = await _dbContext.Users
            .SingleOrDefaultAsync(
                user => user.ExternalId ==
                    targetUserExternalId,
                cancellationToken);

        if (targetUser is null)
        {
            return UserAdministrationResult.Failure(
                "The requested user was not found.");
        }

        if (targetUser.IsDeleted)
        {
            return UserAdministrationResult.Failure(
                "A deleted account cannot be activated or deactivated.");
        }

        if (!isActive &&
            targetUser.Id == actingUser.Id)
        {
            return UserAdministrationResult.Failure(
                "You cannot deactivate your own account.");
        }

        if (targetUser.IsActive == isActive)
        {
            return UserAdministrationResult.Success();
        }

        var previousIsActive = targetUser.IsActive;

        if (!isActive &&
            await _userManager.IsInRoleAsync(
                targetUser,
                SystemRoleNames.Administrator))
        {
            var administrators =
                await _userManager.GetUsersInRoleAsync(
                    SystemRoleNames.Administrator);

            var activeAdministratorCount =
                administrators.Count(user =>
                    user.IsActive &&
                    !user.IsDeleted);

            if (activeAdministratorCount <= 1)
            {
                return UserAdministrationResult.Failure(
                    "The last active administrator cannot be deactivated.");
            }
        }

        targetUser.IsActive = isActive;
        targetUser.ModifiedUtc = DateTime.UtcNow;
        targetUser.ModifiedByUserId = actingUser.Id;

        var updateResult =
            await _userManager.UpdateSecurityStampAsync(
                targetUser);

        if (!updateResult.Succeeded)
        {
            var errors = updateResult.Errors
                .Select(error => error.Description)
                .ToArray();

            return new UserAdministrationResult(
                false,
                errors);
        }

        await _auditService.RecordAsync(
            new RecordAuditRequest(
                actingUser.ExternalId,
                isActive
                    ? AuditAction.UserActivated
                    : AuditAction.UserDeactivated,
                "UserAdministration",
                "User",
                targetUser.ExternalId,
                new Dictionary<string, object?>
                {
                    ["IsActive"] = previousIsActive
                },
                new Dictionary<string, object?>
                {
                    ["IsActive"] = isActive
                },
                true,
                CorrelationId: Guid.NewGuid()),
            cancellationToken);

        return UserAdministrationResult.Success();
    }

    public async Task<UserAdministrationResult>
    SetAdministratorStatusAsync(
        Guid targetUserExternalId,
        bool isAdministrator,
        Guid actingUserExternalId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

        var actingUser = await _dbContext.Users
            .SingleOrDefaultAsync(
                user => user.ExternalId ==
                    actingUserExternalId,
                cancellationToken);

        if (actingUser is null ||
            !actingUser.IsActive ||
            actingUser.IsDeleted)
        {
            return UserAdministrationResult.Failure(
                "The acting administrator account is invalid.");
        }

        if (!await _userManager.IsInRoleAsync(
            actingUser,
            SystemRoleNames.Administrator))
        {
            return UserAdministrationResult.Failure(
                "Administrator access is required.");
        }

        var targetUser = await _dbContext.Users
            .SingleOrDefaultAsync(
                user => user.ExternalId ==
                    targetUserExternalId,
                cancellationToken);

        if (targetUser is null)
        {
            return UserAdministrationResult.Failure(
                "The requested user was not found.");
        }

        if (!targetUser.IsActive || targetUser.IsDeleted)
        {
            return UserAdministrationResult.Failure(
                "Administrator access cannot be changed " +
                "for an inactive or deleted account.");
        }

        var currentlyAdministrator =
            await _userManager.IsInRoleAsync(
                targetUser,
                SystemRoleNames.Administrator);

        if (currentlyAdministrator == isAdministrator)
        {
            return UserAdministrationResult.Success();
        }

        if (!isAdministrator &&
            targetUser.Id == actingUser.Id)
        {
            return UserAdministrationResult.Failure(
                "You cannot remove your own " +
                "administrator access.");
        }

        if (!isAdministrator)
        {
            var administrators =
                await _userManager.GetUsersInRoleAsync(
                    SystemRoleNames.Administrator);

            var activeAdministratorCount =
                administrators.Count(user =>
                    user.IsActive &&
                    !user.IsDeleted);

            if (activeAdministratorCount <= 1)
            {
                return UserAdministrationResult.Failure(
                    "The last active administrator " +
                    "cannot be demoted.");
            }
        }

        IdentityResult roleResult;

        if (isAdministrator)
        {
            if (!await _userManager.IsInRoleAsync(
                targetUser,
                SystemRoleNames.User))
            {
                var userRoleResult =
                    await _userManager.AddToRoleAsync(
                        targetUser,
                        SystemRoleNames.User);

                if (!userRoleResult.Succeeded)
                {
                    var userRoleErrors =
                        userRoleResult.Errors
                            .Select(error =>
                                error.Description)
                            .ToArray();

                    return new UserAdministrationResult(
                        false,
                        userRoleErrors);
                }
            }

            roleResult = await _userManager.AddToRoleAsync(
                targetUser,
                SystemRoleNames.Administrator);
        }
        else
        {
            roleResult =
                await _userManager.RemoveFromRoleAsync(
                    targetUser,
                    SystemRoleNames.Administrator);
        }

        if (!roleResult.Succeeded)
        {
            var errors = roleResult.Errors
                .Select(error => error.Description)
                .ToArray();

            return new UserAdministrationResult(
                false,
                errors);
        }

        targetUser.ModifiedUtc = DateTime.UtcNow;
        targetUser.ModifiedByUserId = actingUser.Id;

        var stampResult =
            await _userManager.UpdateSecurityStampAsync(
                targetUser);

        if (!stampResult.Succeeded)
        {
            var errors = stampResult.Errors
                .Select(error => error.Description)
                .ToArray();

            return new UserAdministrationResult(
                false,
                errors);
        }

        await _auditService.RecordAsync(
            new RecordAuditRequest(
                actingUser.ExternalId,
                isAdministrator
                    ? AuditAction.AdministratorPromoted
                    : AuditAction.AdministratorDemoted,
                "UserAdministration",
                "User",
                targetUser.ExternalId,
                new Dictionary<string, object?>
                {
                    ["IsAdministrator"] =
                        currentlyAdministrator
                },
                new Dictionary<string, object?>
                {
                    ["IsAdministrator"] =
                        isAdministrator
                },
                true,
                CorrelationId: Guid.NewGuid()),
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);

        return UserAdministrationResult.Success();
    }
}