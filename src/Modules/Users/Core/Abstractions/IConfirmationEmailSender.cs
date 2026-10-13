namespace AlQalam.Users.Core.Abstractions;

public interface IConfirmationEmailSender
{
    Task SendConfirmationEmailAsync(
        string email,
        Guid userId,
        string confirmationToken,
        CancellationToken cancellationToken = default);
}
