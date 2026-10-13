using AlQalam.Users.Core.Entities;
using AlQalam.Users.Infrastructure.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AlQalam.Users.Infrastructure.Persistence;

public class UsersDbContext(DbContextOptions<UsersDbContext> options)
    : IdentityUserContext<AppUser, Guid>(options)
{
    public const string Schema = "users";

    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema(Schema);

        builder.Entity<AppUser>(b =>
        {
            b.ToTable("users");
        });

        builder.Entity<IdentityUserClaim<Guid>>(b =>
        {
            b.ToTable("user_claims");
        });

        builder.Entity<IdentityUserLogin<Guid>>(b =>
        {
            b.ToTable("user_logins");
        });

        builder.Entity<IdentityUserToken<Guid>>(b =>
        {
            b.ToTable("user_tokens");
        });

        builder.ApplyConfigurationsFromAssembly(typeof(UsersDbContext).Assembly);
    }
}
