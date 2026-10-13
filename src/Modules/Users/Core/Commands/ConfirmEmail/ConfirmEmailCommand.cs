namespace AlQalam.Users.Core.Commands.ConfirmEmail;

public sealed record ConfirmEmailCommand(Guid UserId, string Token);
