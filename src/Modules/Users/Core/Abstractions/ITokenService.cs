using AlQalam.Users.Core.DTOs;
using AlQalam.Users.Core.Entities;

namespace AlQalam.Users.Core.Abstractions;

public interface ITokenService
{
    Task<AuthTokensDto> IssueTokensAsync(
        Guid userId,
        string? deviceId = null,
        CancellationToken cancellationToken = default);

    Task<AuthTokensDto> RotateTokensAsync(
        RefreshToken currentToken,
        CancellationToken cancellationToken = default);

    string HashToken(string tokenPlaintext);
}
