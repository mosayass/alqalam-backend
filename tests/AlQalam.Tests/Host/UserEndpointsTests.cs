using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AlQalam.Shared.Results;
using AlQalam.Users.Core.Abstractions;
using AlQalam.Users.Core.DTOs;
using AlQalam.Users.Core.Enums;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AlQalam.Tests.Host;

public class UserEndpointsTests : IClassFixture<AlQalamWebApplicationFactory>
{
    private readonly AlQalamWebApplicationFactory _factory;

    public UserEndpointsTests(AlQalamWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_WithValidData_Returns201Created()
    {
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddScoped<IIdentityService>(_ => new StubIdentityService());
                services.AddScoped<IUserProfileRepository>(_ => new StubProfileRepository());
                services.AddScoped<IUsersUnitOfWork>(_ => new StubUnitOfWork());
                services.AddScoped<IConfirmationEmailSender>(_ => new StubEmailSender());
            });
        }).CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/users/register", new
        {
            fullName = "Ahmad Omar",
            email = "ahmad@example.com",
            password = "P@ssword123!",
            initialLevel = "Intermediate"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<UserSummaryDto>();
        Assert.NotNull(body);
        Assert.Equal("Ahmad Omar", body.FullName);
        Assert.Equal("ahmad@example.com", body.Email);
        Assert.Equal(MemorizationLevel.Intermediate, body.MemorizationLevel);
        Assert.False(body.EmailConfirmed);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_Returns409Conflict()
    {
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddScoped<IIdentityService>(_ => new StubIdentityService { ShouldReturnDuplicate = true });
                services.AddScoped<IUserProfileRepository>(_ => new StubProfileRepository());
                services.AddScoped<IUsersUnitOfWork>(_ => new StubUnitOfWork());
                services.AddScoped<IConfirmationEmailSender>(_ => new StubEmailSender());
            });
        }).CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/users/register", new
        {
            fullName = "Ahmad Omar",
            email = "duplicate@example.com",
            password = "P@ssword123!"
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        Assert.Equal("Users.DuplicateEmail", doc.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task Register_WithInvalidEmail_Returns400BadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/users/register", new
        {
            fullName = "Ahmad Omar",
            email = "invalid-email",
            password = "P@ssword123!"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        Assert.Equal("Users.InvalidEmail", doc.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task ConfirmEmail_WithValidToken_Returns204NoContent()
    {
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddScoped<IIdentityService>(_ => new StubIdentityService());
            });
        }).CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/users/confirm-email", new
        {
            userId = Guid.NewGuid(),
            token = "valid-token-123"
        });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task ConfirmEmail_WithInvalidToken_Returns400BadRequest()
    {
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddScoped<IIdentityService>(_ => new StubIdentityService { InvalidToken = true });
            });
        }).CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/users/confirm-email", new
        {
            userId = Guid.NewGuid(),
            token = "bad-token"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithValidCredentials_Returns200WithTokens()
    {
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddScoped<IIdentityService>(_ => new StubIdentityService());
                services.AddScoped<ITokenService>(_ => new StubTokenService());
            });
        }).CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/users/login", new
        {
            email = "ahmad@example.com",
            password = "P@ssword123!"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tokens = await response.Content.ReadFromJsonAsync<AuthTokensDto>();
        Assert.NotNull(tokens);
        Assert.Equal("jwt.access.token", tokens.AccessToken);
        Assert.Equal("refresh-plain-token", tokens.RefreshToken);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsGeneric401Unauthorized()
    {
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddScoped<IIdentityService>(_ => new StubIdentityService { ShouldFailLogin = true });
                services.AddScoped<ITokenService>(_ => new StubTokenService());
            });
        }).CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/users/login", new
        {
            email = "ahmad@example.com",
            password = "wrong-password"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        Assert.Equal("Auth.InvalidCredentials", doc.RootElement.GetProperty("title").GetString());
    }

    private sealed class StubIdentityService : IIdentityService
    {
        public bool ShouldReturnDuplicate { get; set; }
        public bool InvalidToken { get; set; }
        public bool ShouldFailLogin { get; set; }

        public Task<Result<Guid>> CreateAccountAsync(Guid userId, string email, string password, CancellationToken cancellationToken = default)
        {
            if (ShouldReturnDuplicate)
            {
                return Task.FromResult<Result<Guid>>(Error.Conflict("Users.DuplicateEmail", "A user with this email already exists."));
            }
            return Task.FromResult<Result<Guid>>(userId);
        }

        public Task<Result<string>> GenerateEmailConfirmationTokenAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result<string>.Success("test-token"));

        public Task<Result> ConfirmEmailAsync(Guid userId, string token, CancellationToken cancellationToken = default)
        {
            if (InvalidToken)
            {
                return Task.FromResult<Result>(Error.Validation("Users.InvalidConfirmationToken", "Invalid token"));
            }
            return Task.FromResult(Result.Success());
        }

        public Task<Result<Guid>> CheckCredentialsAsync(string email, string password, CancellationToken cancellationToken = default)
        {
            if (ShouldFailLogin)
            {
                return Task.FromResult<Result<Guid>>(Error.Unauthorized("Auth.InvalidCredentials", "Invalid credentials"));
            }
            return Task.FromResult(Result<Guid>.Success(Guid.NewGuid()));
        }
    }

    private sealed class StubProfileRepository : IUserProfileRepository
    {
        public Task AddAsync(AlQalam.Users.Core.Entities.UserProfile profile, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<AlQalam.Users.Core.Entities.UserProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<AlQalam.Users.Core.Entities.UserProfile?>(null);
    }

    private sealed class StubUnitOfWork : IUsersUnitOfWork
    {
        public Task<IUsersTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IUsersTransaction>(new StubTransaction());

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        private sealed class StubTransaction : IUsersTransaction
        {
            public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task RollbackAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }

    private sealed class StubEmailSender : IConfirmationEmailSender
    {
        public Task SendConfirmationEmailAsync(string email, Guid userId, string confirmationToken, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class StubTokenService : ITokenService
    {
        public Task<AuthTokensDto> IssueTokensAsync(Guid userId, string? deviceId = null, CancellationToken cancellationToken = default)
        {
            var now = DateTimeOffset.UtcNow;
            return Task.FromResult(new AuthTokensDto(
                "jwt.access.token",
                now.AddMinutes(15),
                "refresh-plain-token",
                now.AddDays(30)));
        }
    }
}
