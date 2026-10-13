using System.Text;
using System.Threading.RateLimiting;
using AlQalam.Dataset.Core;
using AlQalam.Dataset.Infrastructure;
using AlQalam.Dataset.Infrastructure.Persistence;
using AlQalam.Host.Configuration;
using AlQalam.Host.Endpoints.Health;
using AlQalam.Host.Endpoints.Users;
using AlQalam.Host.Errors;
using AlQalam.Memorization.Core;
using AlQalam.Memorization.Infrastructure;
using AlQalam.Memorization.Infrastructure.Persistence;
using AlQalam.Users.Core;
using AlQalam.Users.Infrastructure;
using AlQalam.Users.Infrastructure.Configuration;
using AlQalam.Users.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Cross-cutting / host services
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

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

// Authentication & Authorization
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        if (jwtOptions is not null && !string.IsNullOrWhiteSpace(jwtOptions.SigningKey))
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtOptions.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30)
            };
        }
    });
builder.Services.AddAuthorization();

// Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("auth-endpoints", opt =>
    {
        opt.PermitLimit = 10;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0;
    });
});

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

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// Endpoints
app.MapHealthEndpoints();
app.MapUserEndpoints();

app.Run();

public partial class Program;
