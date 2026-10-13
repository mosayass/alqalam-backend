using AlQalam.Shared.Results;

namespace AlQalam.Users.Core.Abstractions;

public interface IIdentityService
{
    Task<Result<Guid>> CreateAccountAsync(
        Guid userId,
        string email,
        string password,
        CancellationToken cancellationToken = default);

    Task<Result<string>> GenerateEmailConfirmationTokenAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<Result> ConfirmEmailAsync(
        Guid userId,
        string token,
        CancellationToken cancellationToken = default);

    Task<Result<Guid>> CheckCredentialsAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default);
}
