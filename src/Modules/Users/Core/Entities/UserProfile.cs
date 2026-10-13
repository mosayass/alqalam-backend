using AlQalam.Shared.Results;
using AlQalam.Users.Core.Enums;

namespace AlQalam.Users.Core.Entities;

public class UserProfile
{
    public const int MaxFullNameLength = 100;

    public Guid UserId { get; private set; }
    public string FullName { get; private set; } = null!;
    public MemorizationLevel? MemorizationLevel { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    // Required for EF Core
    private UserProfile() { }

    private UserProfile(Guid userId, string fullName, MemorizationLevel? memorizationLevel, DateTimeOffset createdAtUtc)
    {
        UserId = userId;
        FullName = fullName;
        MemorizationLevel = memorizationLevel;
        CreatedAtUtc = createdAtUtc;
    }

    public static Result<UserProfile> Create(
        Guid userId,
        string fullName,
        MemorizationLevel? memorizationLevel,
        DateTimeOffset createdAtUtc)
    {
        if (userId == Guid.Empty)
        {
            return Error.Validation("UserProfile.EmptyUserId", "User identifier cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(fullName))
        {
            return Error.Validation("UserProfile.EmptyFullName", "Full name is required.");
        }

        var trimmedName = fullName.Trim();

        if (trimmedName.Length > MaxFullNameLength)
        {
            return Error.Validation(
                "UserProfile.FullNameTooLong",
                $"Full name cannot exceed {MaxFullNameLength} characters.");
        }

        return new UserProfile(userId, trimmedName, memorizationLevel, createdAtUtc);
    }
}
