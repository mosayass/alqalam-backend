using System.Net.Mail;
using AlQalam.Shared.Results;
using AlQalam.Users.Core.Abstractions;
using AlQalam.Users.Core.DTOs;
using AlQalam.Users.Core.Entities;

namespace AlQalam.Users.Core.Commands.RegisterUser;

public sealed class RegisterUserHandler(
    IIdentityService identityService,
    IUserProfileRepository profileRepository,
    IUsersUnitOfWork unitOfWork,
    IConfirmationEmailSender emailSender,
    TimeProvider timeProvider)
{
    public async Task<Result<UserSummaryDto>> HandleAsync(
        RegisterUserCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Email))
        {
            return Error.Validation("Users.EmptyEmail", "Email address is required.");
        }

        var trimmedEmail = command.Email.Trim();
        if (!MailAddress.TryCreate(trimmedEmail, out var mailAddress) || mailAddress.Address != trimmedEmail)
        {
            return Error.Validation("Users.InvalidEmail", "Email address format is invalid.");
        }

        if (string.IsNullOrWhiteSpace(command.Password))
        {
            return Error.Validation("Users.EmptyPassword", "Password is required.");
        }

        var userId = Guid.NewGuid();
        var profileResult = UserProfile.Create(
            userId,
            command.FullName,
            command.InitialLevel,
            timeProvider.GetUtcNow());

        if (profileResult.IsFailure)
        {
            return profileResult.Error;
        }

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        var accountResult = await identityService.CreateAccountAsync(
            userId,
            trimmedEmail,
            command.Password,
            cancellationToken);

        if (accountResult.IsFailure)
        {
            await transaction.RollbackAsync(cancellationToken);
            return accountResult.Error;
        }

        await profileRepository.AddAsync(profileResult.Value, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var tokenResult = await identityService.GenerateEmailConfirmationTokenAsync(userId, cancellationToken);
        if (tokenResult.IsSuccess)
        {
            await emailSender.SendConfirmationEmailAsync(
                trimmedEmail,
                userId,
                tokenResult.Value,
                cancellationToken);
        }

        return new UserSummaryDto(
            userId,
            profileResult.Value.FullName,
            trimmedEmail,
            profileResult.Value.MemorizationLevel,
            EmailConfirmed: false);
    }
}
