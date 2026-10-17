using AlQalam.Shared.Results;
using AlQalam.Users.Core.Abstractions;
using AlQalam.Users.Core.DTOs;

namespace AlQalam.Users.Core.Commands.RefreshToken;

public sealed class RefreshTokenHandler(
    IRefreshTokenRepository refreshTokenRepository,
    ITokenService tokenService,
    IUsersUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    private static readonly Error InvalidRefreshTokenError =
        Error.Unauthorized("Auth.InvalidRefreshToken", "Invalid or revoked refresh token.");

    public async Task<Result<AuthTokensDto>> HandleAsync(
        RefreshTokenCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.RefreshToken))
        {
            return InvalidRefreshTokenError;
        }

        var tokenHash = tokenService.HashToken(command.RefreshToken.Trim());
        var token = await refreshTokenRepository.GetByHashAsync(tokenHash, cancellationToken);

        if (token is null)
        {
            return InvalidRefreshTokenError;
        }

        var utcNow = timeProvider.GetUtcNow();

        // Reuse detection: If token was already revoked, someone is attempting to reuse an old token.
        // Invalidate the entire token family to mitigate token theft.
        if (token.RevokedAtUtc is not null)
        {
            await refreshTokenRepository.RevokeFamilyAsync(token.FamilyId, utcNow, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return InvalidRefreshTokenError;
        }

        // Check if token has expired
        if (!token.IsActive(utcNow))
        {
            return InvalidRefreshTokenError;
        }

        // Rotate: revoke the current token and issue new token pair within the same family
        var tokens = await tokenService.RotateTokensAsync(token, cancellationToken);
        return tokens;
    }
}
