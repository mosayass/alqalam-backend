using AlQalam.Shared.Results;
using AlQalam.Users.Core.Abstractions;
using AlQalam.Users.Core.DTOs;

namespace AlQalam.Users.Core.Commands.Login;

public sealed class LoginHandler(
    IIdentityService identityService,
    ITokenService tokenService)
{
    private static readonly Error InvalidCredentialsError =
        Error.Unauthorized("Auth.InvalidCredentials", "Invalid credentials or email not yet confirmed.");

    public async Task<Result<AuthTokensDto>> HandleAsync(
        LoginCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Email) || string.IsNullOrWhiteSpace(command.Password))
        {
            return InvalidCredentialsError;
        }

        var credResult = await identityService.CheckCredentialsAsync(
            command.Email.Trim(),
            command.Password,
            cancellationToken);

        if (credResult.IsFailure)
        {
            return InvalidCredentialsError;
        }

        var tokens = await tokenService.IssueTokensAsync(
            credResult.Value,
            command.DeviceId,
            cancellationToken);

        return tokens;
    }
}
