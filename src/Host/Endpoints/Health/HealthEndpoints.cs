using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AlQalam.Host.Endpoints.Health;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/health");

        group.MapGet("/live", () => Results.Ok(new { status = "live" }))
            .WithName("HealthLive")
            .WithSummary("Liveness probe")
            .WithDescription("Returns 200 OK if the host process is running.");

        group.MapGet("/ready", async (HealthCheckService healthCheckService, CancellationToken cancellationToken) =>
        {
            var report = await healthCheckService.CheckHealthAsync(check => check.Tags.Contains("ready"), cancellationToken);

            return report.Status == HealthStatus.Healthy
                ? Results.Ok(new { status = "ready" })
                : Results.Json(new { status = "unhealthy" }, statusCode: StatusCodes.Status503ServiceUnavailable);
        })
        .WithName("HealthReady")
        .WithSummary("Readiness probe")
        .WithDescription("Returns 200 OK if critical dependencies are healthy, 503 otherwise.");

        return endpoints;
    }
}
