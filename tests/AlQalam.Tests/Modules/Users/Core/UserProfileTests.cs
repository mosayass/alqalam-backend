using AlQalam.Users.Core.Entities;
using AlQalam.Users.Core.Enums;
using Xunit;

namespace AlQalam.Tests.Modules.Users.Core;

public class UserProfileTests
{
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    [Fact]
    public void Create_WithValidInputs_Succeeds()
    {
        var userId = Guid.NewGuid();
        var result = UserProfile.Create(userId, "Ahmad Ali", MemorizationLevel.Intermediate, _now);

        Assert.True(result.IsSuccess);
        Assert.Equal(userId, result.Value.UserId);
        Assert.Equal("Ahmad Ali", result.Value.FullName);
        Assert.Equal(MemorizationLevel.Intermediate, result.Value.MemorizationLevel);
        Assert.Equal(_now, result.Value.CreatedAtUtc);
    }

    [Fact]
    public void Create_WithWhitespaceFullName_TrimsInput()
    {
        var userId = Guid.NewGuid();
        var result = UserProfile.Create(userId, "  Fatima Omar  ", MemorizationLevel.Beginner, _now);

        Assert.True(result.IsSuccess);
        Assert.Equal("Fatima Omar", result.Value.FullName);
    }

    [Fact]
    public void Create_WithEmptyUserId_FailsValidation()
    {
        var result = UserProfile.Create(Guid.Empty, "Zaid", MemorizationLevel.Beginner, _now);

        Assert.True(result.IsFailure);
        Assert.Equal("UserProfile.EmptyUserId", result.Error.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithEmptyOrWhitespaceFullName_FailsValidation(string? fullName)
    {
        var result = UserProfile.Create(Guid.NewGuid(), fullName!, MemorizationLevel.Beginner, _now);

        Assert.True(result.IsFailure);
        Assert.Equal("UserProfile.EmptyFullName", result.Error.Code);
    }

    [Fact]
    public void Create_WithFullNameExceedingMaxLength_FailsValidation()
    {
        var longName = new string('A', UserProfile.MaxFullNameLength + 1);
        var result = UserProfile.Create(Guid.NewGuid(), longName, MemorizationLevel.Advanced, _now);

        Assert.True(result.IsFailure);
        Assert.Equal("UserProfile.FullNameTooLong", result.Error.Code);
    }
}
