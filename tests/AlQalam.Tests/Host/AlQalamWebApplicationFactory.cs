using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace AlQalam.Tests.Host;

public class AlQalamWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string TestSigningKey = "test_signing_key_at_least_32_characters_long_for_testing!";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "AlQalam.Tests",
                ["Jwt:Audience"] = "AlQalam.Tests.Clients",
                ["Jwt:SigningKey"] = TestSigningKey,
                ["Jwt:AccessTokenMinutes"] = "15",
                ["Jwt:RefreshTokenDays"] = "30"
            });
        });
    }
}
