using AlQalam.Users.Core.Entities;
using Xunit;

namespace AlQalam.Tests.Modules.Users.Core;

public class RefreshTokenTests
{
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    [Fact]
    public void Create_InitializesCorrectProperties()
    {
        var userId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        var expires = _now.AddDays(30);

        var token = RefreshToken.Create(userId, "hashed-val-123", familyId, _now, expires, "device-1");

        Assert.NotEqual(Guid.Empty, token.Id);
        Assert.Equal(userId, token.UserId);
        Assert.Equal("hashed-val-123", token.TokenHash);
        Assert.Equal(familyId, token.FamilyId);
        Assert.Equal("device-1", token.DeviceId);
        Assert.Equal(_now, token.CreatedAtUtc);
        Assert.Equal(expires, token.ExpiresAtUtc);
        Assert.Null(token.RevokedAtUtc);
    }

    [Fact]
    public void IsActive_WhenValidAndNotRevoked_ReturnsTrue()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "hash", Guid.NewGuid(), _now, _now.AddDays(7));

        Assert.True(token.IsActive(_now.AddDays(1)));
    }

    [Fact]
    public void IsActive_WhenExpired_ReturnsFalse()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "hash", Guid.NewGuid(), _now, _now.AddDays(7));

        Assert.False(token.IsActive(_now.AddDays(8)));
    }

    [Fact]
    public void IsActive_WhenRevoked_ReturnsFalse()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "hash", Guid.NewGuid(), _now, _now.AddDays(7));
        token.Revoke(_now.AddDays(1));

        Assert.False(token.IsActive(_now.AddDays(2)));
    }

    [Fact]
    public void Revoke_IsIdempotent()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "hash", Guid.NewGuid(), _now, _now.AddDays(7));
        var firstRevoke = _now.AddHours(2);
        var secondRevoke = _now.AddHours(5);

        token.Revoke(firstRevoke);
        var originalTimestamp = token.RevokedAtUtc;

        token.Revoke(secondRevoke);

        Assert.Equal(firstRevoke, token.RevokedAtUtc);
        Assert.Equal(originalTimestamp, token.RevokedAtUtc);
    }
}
