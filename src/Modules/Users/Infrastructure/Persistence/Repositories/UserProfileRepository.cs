using AlQalam.Users.Core.Abstractions;
using AlQalam.Users.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlQalam.Users.Infrastructure.Persistence.Repositories;

public class UserProfileRepository(UsersDbContext context) : IUserProfileRepository
{
    public async Task AddAsync(UserProfile profile, CancellationToken cancellationToken = default)
    {
        await context.UserProfiles.AddAsync(profile, cancellationToken);
    }

    public async Task<UserProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await context.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
    }
}
