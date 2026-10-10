using AlQalam.Dataset.Core;
using AlQalam.Dataset.Infrastructure;
using AlQalam.Host.Endpoints.Health;
using AlQalam.Memorization.Core;
using AlQalam.Memorization.Infrastructure;
using AlQalam.Users.Core;
using AlQalam.Users.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Cross-cutting / host services
builder.Services.AddOpenApi();

// Module registrations
builder.Services
    .AddUsersCore()
    .AddUsersInfrastructure()
    .AddMemorizationCore()
    .AddMemorizationInfrastructure()
    .AddDatasetCore()
    .AddDatasetInfrastructure();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Endpoints
app.MapHealthEndpoints();

app.Run();

public partial class Program;
