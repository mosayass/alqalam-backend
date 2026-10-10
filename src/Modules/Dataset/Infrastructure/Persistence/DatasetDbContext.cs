using Microsoft.EntityFrameworkCore;

namespace AlQalam.Dataset.Infrastructure.Persistence;

public class DatasetDbContext(DbContextOptions<DatasetDbContext> options) : DbContext(options)
{
    public const string Schema = "dataset";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema);
    }
}
