using AlQalam.Shared.Results;
using AlQalam.Users.Core.Abstractions;

namespace AlQalam.Users.Core.Commands.ConfirmEmail;

public sealed class ConfirmEmailHandler(IIdentityService identityService)
{
    public async Task<Result> HandleAsync(
        ConfirmEmailCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.UserId == Guid.Empty || string.IsNullOrWhiteSpace(command.Token))
        {
            return Error.Validation("Users.InvalidConfirmationToken", "Invalid or expired confirmation token.");
        }

        return await identityService.ConfirmEmailAsync(command.UserId, command.Token.Trim(), cancellationToken);
    }
}
