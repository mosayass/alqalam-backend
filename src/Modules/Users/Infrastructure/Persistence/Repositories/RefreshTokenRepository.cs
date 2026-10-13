using AlQalam.Users.Core.Abstractions;
using AlQalam.Users.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlQalam.Users.Infrastructure.Persistence.Repositories;

public class RefreshTokenRepository(UsersDbContext context) : IRefreshTokenRepository
{
    public async Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        await context.RefreshTokens.AddAsync(refreshToken, cancellationToken);
    }

    public async Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return await context.RefreshTokens
            .FirstOrDefaultAsync(r => r.TokenHash == tokenHash, cancellationToken);
    }

    public Task UpdateAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        context.RefreshTokens.Update(refreshToken);
        return Task.CompletedTask;
    }
}
