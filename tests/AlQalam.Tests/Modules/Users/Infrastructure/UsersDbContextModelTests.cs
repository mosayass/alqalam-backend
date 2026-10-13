using AlQalam.Users.Core.Entities;
using AlQalam.Users.Infrastructure.Entities;
using AlQalam.Users.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AlQalam.Tests.Modules.Users.Infrastructure;

public class UsersDbContextModelTests
{
    private const string DummyConnectionString = "Host=localhost;Database=test;Username=postgres;Password=postgres";

    [Fact]
    public void UsersDbContext_ModelConfiguration_MatchesArchitectureRequirements()
    {
        var options = new DbContextOptionsBuilder<UsersDbContext>()
            .UseNpgsql(DummyConnectionString)
            .Options;

        using var context = new UsersDbContext(options);
        var model = context.Model;

        // Default schema
        Assert.Equal("users", model.GetDefaultSchema());

        // Tables and entity mapping
        var userEntity = model.FindEntityType(typeof(AppUser));
        Assert.NotNull(userEntity);
        Assert.Equal("users", userEntity.GetTableName());
        Assert.Equal("users", userEntity.GetSchema());

        var profileEntity = model.FindEntityType(typeof(UserProfile));
        Assert.NotNull(profileEntity);
        Assert.Equal("user_profiles", profileEntity.GetTableName());
        Assert.Equal("users", profileEntity.GetSchema());

        var fullNameProperty = profileEntity.FindProperty(nameof(UserProfile.FullName));
        Assert.NotNull(fullNameProperty);
        Assert.False(fullNameProperty.IsNullable);
        Assert.Equal(UserProfile.MaxFullNameLength, fullNameProperty.GetMaxLength());

        // Relational FK user_profiles.user_id -> users.id
        var foreignKey = profileEntity.GetForeignKeys().FirstOrDefault();
        Assert.NotNull(foreignKey);
        Assert.Equal(userEntity, foreignKey.PrincipalEntityType);

        var tokenEntity = model.FindEntityType(typeof(RefreshToken));
        Assert.NotNull(tokenEntity);
        Assert.Equal("refresh_tokens", tokenEntity.GetTableName());
        Assert.Equal("users", tokenEntity.GetSchema());

        // Refresh token indexes
        var tokenHashIndex = tokenEntity.GetIndexes()
            .FirstOrDefault(i => i.Properties.Any(p => p.Name == nameof(RefreshToken.TokenHash)));
        Assert.NotNull(tokenHashIndex);
        Assert.True(tokenHashIndex.IsUnique);

        var userIdIndex = tokenEntity.GetIndexes()
            .FirstOrDefault(i => i.Properties.Any(p => p.Name == nameof(RefreshToken.UserId)));
        Assert.NotNull(userIdIndex);
    }
}
