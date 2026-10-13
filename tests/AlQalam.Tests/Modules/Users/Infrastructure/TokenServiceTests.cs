using System.IdentityModel.Tokens.Jwt;
using AlQalam.Tests.Common;
using AlQalam.Users.Core.Abstractions;
using AlQalam.Users.Core.Entities;
using AlQalam.Users.Infrastructure.Configuration;
using AlQalam.Users.Infrastructure.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace AlQalam.Tests.Modules.Users.Infrastructure;

public class TokenServiceTests
{
    private readonly FakeRefreshTokenRepository _tokenRepo = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly TestTimeProvider _timeProvider = new(DateTimeOffset.UtcNow);

    private readonly JwtOptions _options = new()
    {
        Issuer = "AlQalam.Tests",
        Audience = "AlQalam.Tests.Clients",
        SigningKey = "01234567890123456789012345678901",
        AccessTokenMinutes = 15,
        RefreshTokenDays = 30
    };

    [Fact]
    public async Task IssueTokensAsync_GeneratesValidJwtAndPersistsHashedRefreshToken()
    {
        var tokenService = new TokenService(
            Options.Create(_options),
            _tokenRepo,
            _unitOfWork,
            _timeProvider);

        var userId = Guid.NewGuid();
        var result = await tokenService.IssueTokensAsync(userId, "ipad-device");

        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(result.RefreshToken));

        // Validate JWT token claims
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(result.AccessToken);
        Assert.Equal(_options.Issuer, jwtToken.Issuer);
        Assert.Equal(_options.Audience, jwtToken.Audiences.First());
        Assert.Equal(userId.ToString(), jwtToken.Subject);

        // Verify refresh token storage
        Assert.NotNull(_tokenRepo.AddedToken);
        Assert.Equal(userId, _tokenRepo.AddedToken.UserId);
        Assert.Equal("ipad-device", _tokenRepo.AddedToken.DeviceId);

        // Verify SHA-256 hash stored, NEVER plaintext
        var expectedHash = TokenService.ComputeSha256Hex(result.RefreshToken);
        Assert.Equal(expectedHash, _tokenRepo.AddedToken.TokenHash);
        Assert.NotEqual(result.RefreshToken, _tokenRepo.AddedToken.TokenHash);

        // Unit of work committed
        Assert.True(_unitOfWork.SavedChanges);
    }

    private sealed class FakeRefreshTokenRepository : IRefreshTokenRepository
    {
        public RefreshToken? AddedToken { get; private set; }

        public Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
        {
            AddedToken = refreshToken;
            return Task.CompletedTask;
        }

        public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
            Task.FromResult(AddedToken?.TokenHash == tokenHash ? AddedToken : null);

        public Task UpdateAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class FakeUnitOfWork : IUsersUnitOfWork
    {
        public bool SavedChanges { get; private set; }

        public Task<IUsersTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SavedChanges = true;
            return Task.CompletedTask;
        }
    }
}
