using AlQalam.Shared.Results;
using AlQalam.Tests.Common;
using AlQalam.Users.Core.Abstractions;
using AlQalam.Users.Core.Commands.RefreshToken;
using AlQalam.Users.Core.DTOs;
using AlQalam.Users.Core.Entities;
using Xunit;

namespace AlQalam.Tests.Modules.Users.Core;

public class RefreshTokenHandlerTests
{
    private readonly FakeRefreshTokenRepository _repo = new();
    private readonly FakeTokenService _tokenService = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly TestTimeProvider _timeProvider = new(DateTimeOffset.UtcNow);

    private RefreshTokenHandler CreateHandler() =>
        new(_repo, _tokenService, _unitOfWork, _timeProvider);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task HandleAsync_WithEmptyToken_ReturnsUnauthorized(string token)
    {
        var handler = CreateHandler();
        var command = new RefreshTokenCommand(token);

        var result = await handler.HandleAsync(command);

        Assert.True(result.IsFailure);
        Assert.Equal("Auth.InvalidRefreshToken", result.Error.Code);
        Assert.Equal(ErrorType.Unauthorized, result.Error.Type);
    }

    [Fact]
    public async Task HandleAsync_WhenTokenNotFound_ReturnsUnauthorized()
    {
        var handler = CreateHandler();
        var command = new RefreshTokenCommand("unknown-token");

        var result = await handler.HandleAsync(command);

        Assert.True(result.IsFailure);
        Assert.Equal("Auth.InvalidRefreshToken", result.Error.Code);
        Assert.Equal(ErrorType.Unauthorized, result.Error.Type);
    }

    [Fact]
    public async Task HandleAsync_WhenTokenExpired_ReturnsUnauthorized()
    {
        var now = _timeProvider.GetUtcNow();
        var expiredToken = RefreshToken.Create(
            Guid.NewGuid(),
            "hashed-token",
            Guid.NewGuid(),
            now.AddDays(-10),
            now.AddDays(-1)); // expired yesterday

        _repo.StoredToken = expiredToken;

        var handler = CreateHandler();
        var command = new RefreshTokenCommand("raw-token");

        var result = await handler.HandleAsync(command);

        Assert.True(result.IsFailure);
        Assert.Equal("Auth.InvalidRefreshToken", result.Error.Code);
        Assert.Equal(ErrorType.Unauthorized, result.Error.Type);
    }

    [Fact]
    public async Task HandleAsync_WhenTokenAlreadyRevoked_TriggersFamilyRevocationAndReturnsUnauthorized()
    {
        var now = _timeProvider.GetUtcNow();
        var familyId = Guid.NewGuid();
        var revokedToken = RefreshToken.Create(
            Guid.NewGuid(),
            "hashed-token",
            familyId,
            now.AddDays(-5),
            now.AddDays(25));
        revokedToken.Revoke(now.AddDays(-1));

        _repo.StoredToken = revokedToken;

        var handler = CreateHandler();
        var command = new RefreshTokenCommand("raw-token");

        var result = await handler.HandleAsync(command);

        // Assert failure
        Assert.True(result.IsFailure);
        Assert.Equal("Auth.InvalidRefreshToken", result.Error.Code);

        // Assert family was revoked to mitigate token theft
        Assert.Equal(familyId, _repo.RevokedFamilyId);
        Assert.True(_unitOfWork.SavedChanges);
    }

    [Fact]
    public async Task HandleAsync_WithActiveToken_RotatesTokensSuccessfully()
    {
        var now = _timeProvider.GetUtcNow();
        var familyId = Guid.NewGuid();
        var activeToken = RefreshToken.Create(
            Guid.NewGuid(),
            "hashed-token",
            familyId,
            now.AddDays(-1),
            now.AddDays(29),
            "ipad-client");

        _repo.StoredToken = activeToken;

        var handler = CreateHandler();
        var command = new RefreshTokenCommand("raw-token");

        var result = await handler.HandleAsync(command);

        Assert.True(result.IsSuccess);
        Assert.Equal("rotated.jwt.token", result.Value.AccessToken);
        Assert.Equal("rotated-refresh-plain", result.Value.RefreshToken);
        Assert.Same(activeToken, _tokenService.RotatedToken);
    }

    private sealed class FakeRefreshTokenRepository : IRefreshTokenRepository
    {
        public RefreshToken? StoredToken { get; set; }
        public Guid? RevokedFamilyId { get; private set; }

        public Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
            Task.FromResult(StoredToken);

        public Task UpdateAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task RevokeFamilyAsync(Guid familyId, DateTimeOffset revokedAtUtc, CancellationToken cancellationToken = default)
        {
            RevokedFamilyId = familyId;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeTokenService : ITokenService
    {
        public RefreshToken? RotatedToken { get; private set; }

        public Task<AuthTokensDto> IssueTokensAsync(Guid userId, string? deviceId = null, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<AuthTokensDto> RotateTokensAsync(RefreshToken currentToken, CancellationToken cancellationToken = default)
        {
            RotatedToken = currentToken;
            var now = DateTimeOffset.UtcNow;
            return Task.FromResult(new AuthTokensDto(
                "rotated.jwt.token",
                now.AddMinutes(15),
                "rotated-refresh-plain",
                now.AddDays(30)));
        }

        public string HashToken(string tokenPlaintext) => "hashed-token";
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
