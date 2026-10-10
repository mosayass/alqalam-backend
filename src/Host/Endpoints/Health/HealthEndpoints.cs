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

        return endpoints;
    }
}
