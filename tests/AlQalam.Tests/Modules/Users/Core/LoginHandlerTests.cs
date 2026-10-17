using AlQalam.Shared.Results;
using AlQalam.Users.Core.Abstractions;
using AlQalam.Users.Core.Commands.Login;
using AlQalam.Users.Core.DTOs;
using AlQalam.Users.Core.Entities;
using Xunit;

namespace AlQalam.Tests.Modules.Users.Core;

public class LoginHandlerTests
{
    private readonly FakeIdentityService _identityService = new();
    private readonly FakeTokenService _tokenService = new();

    private LoginHandler CreateHandler() => new(_identityService, _tokenService);

    [Fact]
    public async Task HandleAsync_WithValidCredentials_ReturnsTokens()
    {
        var handler = CreateHandler();
        var command = new LoginCommand("user@example.com", "P@ssword123!", "ipad-pro");

        var result = await handler.HandleAsync(command);

        Assert.True(result.IsSuccess);
        Assert.Equal("jwt.access.token", result.Value.AccessToken);
        Assert.Equal("refresh-token-plaintext", result.Value.RefreshToken);
        Assert.Equal("ipad-pro", _tokenService.LastDeviceId);
    }

    [Theory]
    [InlineData("", "password")]
    [InlineData("user@example.com", "")]
    [InlineData("   ", "   ")]
    public async Task HandleAsync_WithEmptyCredentials_ReturnsGenericUnauthorized(string email, string password)
    {
        var handler = CreateHandler();
        var command = new LoginCommand(email, password);

        var result = await handler.HandleAsync(command);

        Assert.True(result.IsFailure);
        Assert.Equal("Auth.InvalidCredentials", result.Error.Code);
        Assert.Equal(ErrorType.Unauthorized, result.Error.Type);
    }

    [Fact]
    public async Task HandleAsync_WhenCredentialsInvalid_ReturnsGenericUnauthorized()
    {
        _identityService.ShouldFailCredentials = true;
        var handler = CreateHandler();
        var command = new LoginCommand("user@example.com", "wrong-password");

        var result = await handler.HandleAsync(command);

        Assert.True(result.IsFailure);
        Assert.Equal("Auth.InvalidCredentials", result.Error.Code);
        Assert.Equal(ErrorType.Unauthorized, result.Error.Type);
    }

    private sealed class FakeIdentityService : IIdentityService
    {
        public bool ShouldFailCredentials { get; set; }

        public Task<Result<Guid>> CreateAccountAsync(Guid userId, string email, string password, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result<Guid>.Success(userId));

        public Task<Result<string>> GenerateEmailConfirmationTokenAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result<string>.Success("token"));

        public Task<Result> ConfirmEmailAsync(Guid userId, string token, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Success());

        public Task<Result<Guid>> CheckCredentialsAsync(string email, string password, CancellationToken cancellationToken = default)
        {
            if (ShouldFailCredentials)
            {
                return Task.FromResult<Result<Guid>>(Error.Unauthorized("Auth.InvalidCredentials", "Invalid credentials"));
            }
            return Task.FromResult(Result<Guid>.Success(Guid.NewGuid()));
        }
    }

    private sealed class FakeTokenService : ITokenService
    {
        public string? LastDeviceId { get; private set; }

        public Task<AuthTokensDto> IssueTokensAsync(Guid userId, string? deviceId = null, CancellationToken cancellationToken = default)
        {
            LastDeviceId = deviceId;
            var now = DateTimeOffset.UtcNow;
            return Task.FromResult(new AuthTokensDto(
                "jwt.access.token",
                now.AddMinutes(15),
                "refresh-token-plaintext",
                now.AddDays(30)));
        }

        public Task<AuthTokensDto> RotateTokensAsync(RefreshToken currentToken, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public string HashToken(string tokenPlaintext) => "dummy-hash";
    }
}
