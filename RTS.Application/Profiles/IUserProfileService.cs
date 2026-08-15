using RTS.Contracts.Profiles;

namespace RTS.Application.Profiles;

public interface IUserProfileService
{
    Task<UserProfileDetails> GetOrCreateAsync(
        Guid userExternalId,
        CancellationToken cancellationToken = default);

    Task<UpdateUserProfileResult> UpdateAsync(
        Guid userExternalId,
        UpdateUserProfileRequest request,
        CancellationToken cancellationToken = default);
}