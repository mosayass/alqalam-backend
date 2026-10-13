using System.Buffers.Text;
using System.Text;
using AlQalam.Shared.Results;
using AlQalam.Users.Core.Abstractions;
using AlQalam.Users.Infrastructure.Entities;
using Microsoft.AspNetCore.Identity;

namespace AlQalam.Users.Infrastructure.Services;

public class IdentityService(UserManager<AppUser> userManager) : IIdentityService
{
    private static readonly Error InvalidCredentialsError =
        Error.Unauthorized("Auth.InvalidCredentials", "Invalid credentials or email not yet confirmed.");

    private static readonly Error InvalidConfirmationTokenError =
        Error.Validation("Users.InvalidConfirmationToken", "Invalid or expired confirmation token.");

    public async Task<Result<Guid>> CreateAccountAsync(
        Guid userId,
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser is not null)
        {
            return Error.Conflict("Users.DuplicateEmail", "A user with this email already exists.");
        }

        var user = new AppUser(email)
        {
            Id = userId
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            if (result.Errors.Any(e => e.Code is "DuplicateEmail" or "DuplicateUserName"))
            {
                return Error.Conflict("Users.DuplicateEmail", "A user with this email already exists.");
            }

            var description = string.Join(" ", result.Errors.Select(e => e.Description));
            return Error.Validation("Users.RegistrationFailed", description);
        }

        return user.Id;
    }

    public async Task<Result<string>> GenerateEmailConfirmationTokenAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return Error.NotFound("Users.UserNotFound", "User not found.");
        }

        var rawToken = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var tokenBytes = Encoding.UTF8.GetBytes(rawToken);
        var encodedToken = Base64Url.EncodeToString(tokenBytes);

        return encodedToken;
    }

    public async Task<Result> ConfirmEmailAsync(
        Guid userId,
        string token,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return InvalidConfirmationTokenError;
        }

        if (user.EmailConfirmed)
        {
            return Result.Success();
        }

        string rawToken;
        try
        {
            var decodedBytes = Base64Url.DecodeFromUtf8(Encoding.UTF8.GetBytes(token));
            rawToken = Encoding.UTF8.GetString(decodedBytes);
        }
        catch
        {
            return InvalidConfirmationTokenError;
        }

        var result = await userManager.ConfirmEmailAsync(user, rawToken);
        if (!result.Succeeded)
        {
            return InvalidConfirmationTokenError;
        }

        return Result.Success();
    }

    public async Task<Result<Guid>> CheckCredentialsAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return InvalidCredentialsError;
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            return InvalidCredentialsError;
        }

        if (!user.EmailConfirmed)
        {
            return InvalidCredentialsError;
        }

        var isPasswordValid = await userManager.CheckPasswordAsync(user, password);
        if (!isPasswordValid)
        {
            await userManager.AccessFailedAsync(user);
            return InvalidCredentialsError;
        }

        await userManager.ResetAccessFailedCountAsync(user);
        return user.Id;
    }
}
