using AlQalam.Host.Configuration;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace AlQalam.Tests.Host;

public class ConfigurationTests
{
    [Fact]
    public void StorageOptions_DefaultValues_AreSensible()
    {
        var options = new StorageOptions();

        Assert.Equal("http://localhost:9000", options.InternalEndpoint);
        Assert.Equal("http://localhost:9000", options.PublicEndpoint);
        Assert.Equal("us-east-1", options.Region);
        Assert.Equal("alqalam-dataset", options.BucketName);
    }

    [Fact]
    public void StorageOptions_BindsFromConfiguration_Correctly()
    {
        var configData = new Dictionary<string, string?>
        {
            ["Storage:InternalEndpoint"] = "http://minio:9000",
            ["Storage:PublicEndpoint"] = "https://s3.alqalam.app",
            ["Storage:Region"] = "eu-central-1",
            ["Storage:BucketName"] = "prod-dataset",
            ["Storage:AccessKey"] = "custom-access",
            ["Storage:SecretKey"] = "custom-secret"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var options = new StorageOptions();
        configuration.GetSection(StorageOptions.SectionName).Bind(options);

        Assert.Equal("http://minio:9000", options.InternalEndpoint);
        Assert.Equal("https://s3.alqalam.app", options.PublicEndpoint);
        Assert.Equal("eu-central-1", options.Region);
        Assert.Equal("prod-dataset", options.BucketName);
        Assert.Equal("custom-access", options.AccessKey);
        Assert.Equal("custom-secret", options.SecretKey);
    }
}
