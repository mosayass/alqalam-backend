using AlQalam.Dataset.Infrastructure.Persistence;
using AlQalam.Memorization.Infrastructure.Persistence;
using AlQalam.Users.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AlQalam.Tests.Infrastructure;

public class DbContextSchemaTests
{
    private const string DummyConnectionString = "Host=localhost;Database=test;Username=postgres;Password=postgres";

    [Fact]
    public void UsersDbContext_ShouldConfigureUsersSchema()
    {
        var options = new DbContextOptionsBuilder<UsersDbContext>()
            .UseNpgsql(DummyConnectionString)
            .Options;

        using var context = new UsersDbContext(options);

        Assert.Equal("users", context.Model.GetDefaultSchema());
    }

    [Fact]
    public void MemorizationDbContext_ShouldConfigureMemorizationSchema()
    {
        var options = new DbContextOptionsBuilder<MemorizationDbContext>()
            .UseNpgsql(DummyConnectionString)
            .Options;

        using var context = new MemorizationDbContext(options);

        Assert.Equal("memorization", context.Model.GetDefaultSchema());
    }

    [Fact]
    public void DatasetDbContext_ShouldConfigureDatasetSchema()
    {
        var options = new DbContextOptionsBuilder<DatasetDbContext>()
            .UseNpgsql(DummyConnectionString)
            .Options;

        using var context = new DatasetDbContext(options);

        Assert.Equal("dataset", context.Model.GetDefaultSchema());
    }
}
