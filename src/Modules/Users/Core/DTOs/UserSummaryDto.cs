using AlQalam.Users.Core.Enums;

namespace AlQalam.Users.Core.DTOs;

public sealed record UserSummaryDto(
    Guid Id,
    string FullName,
    string Email,
    MemorizationLevel? MemorizationLevel,
    bool EmailConfirmed);
