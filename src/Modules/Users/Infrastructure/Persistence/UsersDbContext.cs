using Microsoft.EntityFrameworkCore;

namespace AlQalam.Users.Infrastructure.Persistence;

public class UsersDbContext(DbContextOptions<UsersDbContext> options) : DbContext(options)
{
    public const string Schema = "users";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema);
    }
}
