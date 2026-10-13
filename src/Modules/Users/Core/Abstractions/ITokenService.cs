using AlQalam.Users.Core.DTOs;

namespace AlQalam.Users.Core.Abstractions;

public interface ITokenService
{
    Task<AuthTokensDto> IssueTokensAsync(
        Guid userId,
        string? deviceId = null,
        CancellationToken cancellationToken = default);
}
