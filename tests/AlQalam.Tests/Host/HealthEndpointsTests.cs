using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace AlQalam.Tests.Host;

public class HealthEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HealthEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task LiveEndpoint_ReturnsOkWithLiveStatus()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(content);
        Assert.Equal("live", json.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task ReadyEndpoint_WhenDatabaseUnreachable_Returns503WithoutLeakingDetails()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(content);
        Assert.Equal("unhealthy", json.RootElement.GetProperty("status").GetString());
        Assert.False(content.Contains("password", StringComparison.OrdinalIgnoreCase));
        Assert.False(content.Contains("Host=", StringComparison.OrdinalIgnoreCase));
    }
}
