using AlQalam.Users.Core.Entities;

namespace AlQalam.Users.Core.Abstractions;

public interface IUserProfileRepository
{
    Task AddAsync(UserProfile profile, CancellationToken cancellationToken = default);
    Task<UserProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
