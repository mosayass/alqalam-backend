namespace AlQalam.Host.Configuration;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    public string InternalEndpoint { get; set; } = "http://localhost:9000";
    public string PublicEndpoint { get; set; } = "http://localhost:9000";
    public string Region { get; set; } = "us-east-1";
    public string BucketName { get; set; } = "alqalam-dataset";
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
}
