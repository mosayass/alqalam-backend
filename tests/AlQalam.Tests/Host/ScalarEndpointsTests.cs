using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace AlQalam.Tests.Host;

public class ScalarEndpointsTests : IClassFixture<AlQalamWebApplicationFactory>
{
    private readonly AlQalamWebApplicationFactory _factory;

    public ScalarEndpointsTests(AlQalamWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ScalarApiReference_WhenDevelopment_ReturnsOk()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/scalar/v1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
    }
}
