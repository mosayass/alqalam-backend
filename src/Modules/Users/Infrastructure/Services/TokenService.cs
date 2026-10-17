using System.Buffers.Text;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AlQalam.Users.Core.Abstractions;
using AlQalam.Users.Core.DTOs;
using AlQalam.Users.Core.Entities;
using AlQalam.Users.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace AlQalam.Users.Infrastructure.Services;

public class TokenService(
    IOptions<JwtOptions> jwtOptions,
    IRefreshTokenRepository refreshTokenRepository,
    IUsersUnitOfWork unitOfWork,
    TimeProvider timeProvider) : ITokenService
{
    private readonly JwtOptions _options = jwtOptions.Value;

    public async Task<AuthTokensDto> IssueTokensAsync(
        Guid userId,
        string? deviceId = null,
        CancellationToken cancellationToken = default)
    {
        var utcNow = timeProvider.GetUtcNow();
        var accessTokenExpiresUtc = utcNow.AddMinutes(_options.AccessTokenMinutes);
        var refreshTokenExpiresUtc = utcNow.AddDays(_options.RefreshTokenDays);

        // 1. Issue Access Token (JWT)
        var accessToken = GenerateAccessToken(userId, accessTokenExpiresUtc);

        // 2. Issue Refresh Token (32 cryptographically random bytes -> Base64Url plaintext)
        var randomBytes = new byte[32];
        RandomNumberGenerator.Fill(randomBytes);
        var refreshTokenPlaintext = Base64Url.EncodeToString(randomBytes);

        var tokenHash = ComputeSha256Hex(refreshTokenPlaintext);
        var familyId = Guid.NewGuid();

        var refreshTokenEntity = RefreshToken.Create(
            userId,
            tokenHash,
            familyId,
            utcNow,
            refreshTokenExpiresUtc,
            deviceId);

        await refreshTokenRepository.AddAsync(refreshTokenEntity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthTokensDto(
            accessToken,
            accessTokenExpiresUtc,
            refreshTokenPlaintext,
            refreshTokenExpiresUtc);
    }

    public async Task<AuthTokensDto> RotateTokensAsync(
        RefreshToken currentToken,
        CancellationToken cancellationToken = default)
    {
        var utcNow = timeProvider.GetUtcNow();
        var accessTokenExpiresUtc = utcNow.AddMinutes(_options.AccessTokenMinutes);
        var refreshTokenExpiresUtc = utcNow.AddDays(_options.RefreshTokenDays);

        // 1. Revoke the current token
        currentToken.Revoke(utcNow);
        await refreshTokenRepository.UpdateAsync(currentToken, cancellationToken);

        // 2. Issue new Access Token (JWT)
        var accessToken = GenerateAccessToken(currentToken.UserId, accessTokenExpiresUtc);

        // 3. Issue replacement Refresh Token (same familyId and deviceId)
        var randomBytes = new byte[32];
        RandomNumberGenerator.Fill(randomBytes);
        var newRefreshTokenPlaintext = Base64Url.EncodeToString(randomBytes);

        var newRefreshTokenEntity = RefreshToken.Create(
            currentToken.UserId,
            ComputeSha256Hex(newRefreshTokenPlaintext),
            currentToken.FamilyId,
            utcNow,
            refreshTokenExpiresUtc,
            currentToken.DeviceId);

        await refreshTokenRepository.AddAsync(newRefreshTokenEntity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthTokensDto(
            accessToken,
            accessTokenExpiresUtc,
            newRefreshTokenPlaintext,
            refreshTokenExpiresUtc);
    }

    public string HashToken(string tokenPlaintext) => ComputeSha256Hex(tokenPlaintext);

    private string GenerateAccessToken(Guid userId, DateTimeOffset expiresUtc)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
            ]),
            Expires = expiresUtc.UtcDateTime,
            SigningCredentials = credentials
        };

        var handler = new JsonWebTokenHandler();
        return handler.CreateToken(tokenDescriptor);
    }

    public static string ComputeSha256Hex(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexStringLower(hash);
    }
}
