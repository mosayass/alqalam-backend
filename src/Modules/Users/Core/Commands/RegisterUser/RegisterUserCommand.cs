using AlQalam.Users.Core.Enums;

namespace AlQalam.Users.Core.Commands.RegisterUser;

public sealed record RegisterUserCommand(
    string FullName,
    string Email,
    string Password,
    MemorizationLevel? InitialLevel = null);
