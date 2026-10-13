using AlQalam.Users.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace AlQalam.Users.Infrastructure.Services;

public class LoggingConfirmationEmailSender(ILogger<LoggingConfirmationEmailSender> logger) : IConfirmationEmailSender
{
    public Task SendConfirmationEmailAsync(
        string email,
        Guid userId,
        string confirmationToken,
        CancellationToken cancellationToken = default)
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "[DEV EMAIL] Confirmation link for {Email} (UserId: {UserId}): Token: {Token}",
                email,
                userId,
                confirmationToken);
        }

        return Task.CompletedTask;
    }
}
