using Microsoft.EntityFrameworkCore;

namespace AlQalam.Memorization.Infrastructure.Persistence;

public class MemorizationDbContext(DbContextOptions<MemorizationDbContext> options) : DbContext(options)
{
    public const string Schema = "memorization";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema);
    }
}
