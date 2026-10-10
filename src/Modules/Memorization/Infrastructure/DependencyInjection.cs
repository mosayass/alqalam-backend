using AlQalam.Memorization.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AlQalam.Memorization.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddMemorizationInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database");

        services.AddDbContext<MemorizationDbContext>(options =>
        {
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                options.UseNpgsql(connectionString, npgsql =>
                {
                    npgsql.MigrationsHistoryTable("__EFMigrationsHistory", MemorizationDbContext.Schema);
                });
            }
        });

        return services;
    }
}
