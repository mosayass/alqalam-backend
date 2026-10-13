using AlQalam.Dataset.Core;
using AlQalam.Dataset.Infrastructure;
using AlQalam.Dataset.Infrastructure.Persistence;
using AlQalam.Host.Configuration;
using AlQalam.Host.Endpoints.Health;
using AlQalam.Host.Errors;
using AlQalam.Memorization.Core;
using AlQalam.Memorization.Infrastructure;
using AlQalam.Memorization.Infrastructure.Persistence;
using AlQalam.Users.Core;
using AlQalam.Users.Infrastructure;
using AlQalam.Users.Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Cross-cutting / host services
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Strongly typed configuration options
builder.Services.Configure<StorageOptions>(
    builder.Configuration.GetSection(StorageOptions.SectionName));

// Module registrations
builder.Services
    .AddUsersCore()
    .AddUsersInfrastructure(builder.Configuration)
    .AddMemorizationCore()
    .AddMemorizationInfrastructure(builder.Configuration)
    .AddDatasetCore()
    .AddDatasetInfrastructure(builder.Configuration);

// Health checks
var healthChecksBuilder = builder.Services.AddHealthChecks();
var connectionString = builder.Configuration.GetConnectionString("Database");
if (!string.IsNullOrWhiteSpace(connectionString))
{
    healthChecksBuilder
        .AddDbContextCheck<UsersDbContext>("users_db", tags: ["ready"])
        .AddDbContextCheck<MemorizationDbContext>("memorization_db", tags: ["ready"])
        .AddDbContextCheck<DatasetDbContext>("dataset_db", tags: ["ready"]);
}
else
{
    healthChecksBuilder.AddCheck(
        "database_config",
        () => HealthCheckResult.Unhealthy("Database connection string is not configured."),
        tags: ["ready"]);
}

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

// Endpoints
app.MapHealthEndpoints();

app.Run();

public partial class Program;
