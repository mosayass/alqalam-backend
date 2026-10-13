namespace AlQalam.Users.Core.Commands.Login;

public sealed record LoginCommand(
    string Email,
    string Password,
    string? DeviceId = null);
