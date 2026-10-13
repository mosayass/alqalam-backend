namespace AlQalam.Users.Core.Entities;

public class RefreshToken
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = null!;
    public Guid FamilyId { get; private set; }
    public string? DeviceId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }

    // Required for EF Core
    private RefreshToken() { }

    private RefreshToken(
        Guid id,
        Guid userId,
        string tokenHash,
        Guid familyId,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc,
        string? deviceId)
    {
        Id = id;
        UserId = userId;
        TokenHash = tokenHash;
        FamilyId = familyId;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        DeviceId = deviceId;
        RevokedAtUtc = null;
    }

    public static RefreshToken Create(
        Guid userId,
        string tokenHash,
        Guid familyId,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc,
        string? deviceId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);

        return new RefreshToken(
            Guid.NewGuid(),
            userId,
            tokenHash,
            familyId,
            createdAtUtc,
            expiresAtUtc,
            deviceId);
    }

    public bool IsActive(DateTimeOffset utcNow) =>
        RevokedAtUtc is null && ExpiresAtUtc > utcNow;

    public void Revoke(DateTimeOffset utcNow)
    {
        if (RevokedAtUtc is null)
        {
            RevokedAtUtc = utcNow;
        }
    }
}
