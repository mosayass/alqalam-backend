using AlQalam.Shared.Results;
using AlQalam.Users.Infrastructure.Entities;
using AlQalam.Users.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace AlQalam.Tests.Modules.Users.Infrastructure;

public class IdentityServiceTests : IDisposable
{
    private readonly InMemoryUserStore _store;
    private readonly UserManager<AppUser> _userManager;
    private readonly IdentityService _service;

    public void Dispose()
    {
        _userManager.Dispose();
        _store.Dispose();
        GC.SuppressFinalize(this);
    }

    public IdentityServiceTests()
    {
        _store = new InMemoryUserStore();
        var options = Options.Create(new IdentityOptions
        {
            User = new UserOptions { RequireUniqueEmail = true },
            SignIn = new SignInOptions { RequireConfirmedEmail = true },
            Lockout = new LockoutOptions
            {
                AllowedForNewUsers = true,
                MaxFailedAccessAttempts = 2,
                DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15)
            },
            Password = new PasswordOptions
            {
                RequiredLength = 8,
                RequireDigit = true,
                RequireLowercase = true,
                RequireUppercase = true,
                RequireNonAlphanumeric = true
            }
        });

        var passwordHasher = new PasswordHasher<AppUser>();
        var userValidators = new List<IUserValidator<AppUser>> { new UserValidator<AppUser>() };
        var passwordValidators = new List<IPasswordValidator<AppUser>> { new PasswordValidator<AppUser>() };
        var keyNormalizer = new UpperInvariantLookupNormalizer();
        var errors = new IdentityErrorDescriber();

        _userManager = new UserManager<AppUser>(
            _store,
            options,
            passwordHasher,
            userValidators,
            passwordValidators,
            keyNormalizer,
            errors,
            services: null!,
            logger: NullLogger<UserManager<AppUser>>.Instance);

        _userManager.RegisterTokenProvider(TokenOptions.DefaultProvider, new EmailTokenProvider<AppUser>());

        _service = new IdentityService(_userManager);
    }

    [Fact]
    public async Task CreateAccount_WhenEmailAlreadyExists_ReturnsConflict()
    {
        var userId1 = Guid.NewGuid();
        var result1 = await _service.CreateAccountAsync(userId1, "test@example.com", "P@ssword123!");
        Assert.True(result1.IsSuccess);

        var userId2 = Guid.NewGuid();
        var result2 = await _service.CreateAccountAsync(userId2, "test@example.com", "P@ssword123!");

        Assert.True(result2.IsFailure);
        Assert.Equal("Users.DuplicateEmail", result2.Error.Code);
        Assert.Equal(ErrorType.Conflict, result2.Error.Type);
    }

    [Fact]
    public async Task CreateAccount_WhenPasswordFailsComplexity_ReturnsValidation()
    {
        var result = await _service.CreateAccountAsync(Guid.NewGuid(), "weak@example.com", "short");

        Assert.True(result.IsFailure);
        Assert.Equal("Users.RegistrationFailed", result.Error.Code);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public async Task CheckCredentials_WhenUserNotFound_ReturnsGenericUnauthorized()
    {
        var result = await _service.CheckCredentialsAsync("nonexistent@example.com", "P@ssword123!");

        Assert.True(result.IsFailure);
        Assert.Equal("Auth.InvalidCredentials", result.Error.Code);
        Assert.Equal(ErrorType.Unauthorized, result.Error.Type);
    }

    [Fact]
    public async Task CheckCredentials_WhenEmailNotConfirmed_ReturnsGenericUnauthorized()
    {
        var userId = Guid.NewGuid();
        await _service.CreateAccountAsync(userId, "unconfirmed@example.com", "P@ssword123!");

        var result = await _service.CheckCredentialsAsync("unconfirmed@example.com", "P@ssword123!");

        Assert.True(result.IsFailure);
        Assert.Equal("Auth.InvalidCredentials", result.Error.Code);
        Assert.Equal(ErrorType.Unauthorized, result.Error.Type);
    }

    [Fact]
    public async Task CheckCredentials_WhenEmailConfirmedAndValidPassword_Succeeds()
    {
        var userId = Guid.NewGuid();
        await _service.CreateAccountAsync(userId, "confirmed@example.com", "P@ssword123!");

        var user = await _userManager.FindByIdAsync(userId.ToString());
        user!.EmailConfirmed = true;
        await _userManager.UpdateAsync(user);

        var result = await _service.CheckCredentialsAsync("confirmed@example.com", "P@ssword123!");

        Assert.True(result.IsSuccess);
        Assert.Equal(userId, result.Value);
    }

    [Fact]
    public async Task CheckCredentials_WhenWrongPassword_ReturnsIdenticalUnauthorizedAndIncrementsFailure()
    {
        var userId = Guid.NewGuid();
        await _service.CreateAccountAsync(userId, "auth@example.com", "P@ssword123!");
        var user = await _userManager.FindByIdAsync(userId.ToString());
        user!.EmailConfirmed = true;
        await _userManager.UpdateAsync(user);

        // Unknown email error for comparison
        var unknownResult = await _service.CheckCredentialsAsync("unknown@example.com", "P@ssword123!");

        var wrongPasswordResult = await _service.CheckCredentialsAsync("auth@example.com", "WrongPassword123!");

        Assert.True(wrongPasswordResult.IsFailure);
        Assert.Equal(unknownResult.Error.Code, wrongPasswordResult.Error.Code);
        Assert.Equal(unknownResult.Error.Description, wrongPasswordResult.Error.Description);
        Assert.Equal(unknownResult.Error.Type, wrongPasswordResult.Error.Type);

        var updatedUser = await _userManager.FindByIdAsync(userId.ToString());
        Assert.Equal(1, updatedUser!.AccessFailedCount);
    }

    [Fact]
    public async Task CheckCredentials_WhenFailedRepeatedly_LocksOut()
    {
        var userId = Guid.NewGuid();
        await _service.CreateAccountAsync(userId, "lockout@example.com", "P@ssword123!");
        var user = await _userManager.FindByIdAsync(userId.ToString());
        user!.EmailConfirmed = true;
        await _userManager.UpdateAsync(user);

        // Max failed attempts is 2
        await _service.CheckCredentialsAsync("lockout@example.com", "WrongPassword1!");
        await _service.CheckCredentialsAsync("lockout@example.com", "WrongPassword2!");

        var updatedUser = await _userManager.FindByIdAsync(userId.ToString());
        Assert.True(await _userManager.IsLockedOutAsync(updatedUser!));

        // Even with the correct password, login is denied
        var result = await _service.CheckCredentialsAsync("lockout@example.com", "P@ssword123!");
        Assert.True(result.IsFailure);
        Assert.Equal("Auth.InvalidCredentials", result.Error.Code);
    }

    [Fact]
    public async Task ConfirmEmail_WhenAlreadyConfirmed_IsIdempotent()
    {
        var userId = Guid.NewGuid();
        await _service.CreateAccountAsync(userId, "idempotent@example.com", "P@ssword123!");
        var user = await _userManager.FindByIdAsync(userId.ToString());
        user!.EmailConfirmed = true;
        await _userManager.UpdateAsync(user);

        // Calling ConfirmEmailAsync when already confirmed
        var result = await _service.ConfirmEmailAsync(userId, "any-token");

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task ConfirmEmail_WithInvalidToken_ReturnsGenericValidation()
    {
        var userId = Guid.NewGuid();
        await _service.CreateAccountAsync(userId, "badtoken@example.com", "P@ssword123!");

        var result = await _service.ConfirmEmailAsync(userId, "invalid_base64_url_token");

        Assert.True(result.IsFailure);
        Assert.Equal("Users.InvalidConfirmationToken", result.Error.Code);
    }

    private sealed class InMemoryUserStore :
        IUserStore<AppUser>,
        IUserPasswordStore<AppUser>,
        IUserEmailStore<AppUser>,
        IUserLockoutStore<AppUser>
    {
        private readonly Dictionary<string, AppUser> _users = new();

        public Task<IdentityResult> CreateAsync(AppUser user, CancellationToken cancellationToken)
        {
            _users[user.Id.ToString()] = user;
            return Task.FromResult(IdentityResult.Success);
        }

        public Task<IdentityResult> DeleteAsync(AppUser user, CancellationToken cancellationToken)
        {
            _users.Remove(user.Id.ToString());
            return Task.FromResult(IdentityResult.Success);
        }

        public Task<AppUser?> FindByIdAsync(string userId, CancellationToken cancellationToken) =>
            Task.FromResult(_users.GetValueOrDefault(userId));

        public Task<AppUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken) =>
            Task.FromResult(_users.Values.FirstOrDefault(u => u.NormalizedUserName == normalizedUserName));

        public Task<AppUser?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken) =>
            Task.FromResult(_users.Values.FirstOrDefault(u => u.NormalizedEmail == normalizedEmail));

        public Task<IdentityResult> UpdateAsync(AppUser user, CancellationToken cancellationToken)
        {
            _users[user.Id.ToString()] = user;
            return Task.FromResult(IdentityResult.Success);
        }

        public Task SetPasswordHashAsync(AppUser user, string? passwordHash, CancellationToken cancellationToken)
        {
            user.PasswordHash = passwordHash;
            return Task.CompletedTask;
        }

        public Task<string?> GetPasswordHashAsync(AppUser user, CancellationToken cancellationToken) =>
            Task.FromResult(user.PasswordHash);

        public Task<bool> HasPasswordAsync(AppUser user, CancellationToken cancellationToken) =>
            Task.FromResult(user.PasswordHash != null);

        public Task SetEmailAsync(AppUser user, string? email, CancellationToken cancellationToken)
        {
            user.Email = email;
            return Task.CompletedTask;
        }

        public Task<string?> GetEmailAsync(AppUser user, CancellationToken cancellationToken) =>
            Task.FromResult(user.Email);

        public Task<bool> GetEmailConfirmedAsync(AppUser user, CancellationToken cancellationToken) =>
            Task.FromResult(user.EmailConfirmed);

        public Task SetEmailConfirmedAsync(AppUser user, bool confirmed, CancellationToken cancellationToken)
        {
            user.EmailConfirmed = confirmed;
            return Task.CompletedTask;
        }

        public Task SetNormalizedEmailAsync(AppUser user, string? normalizedEmail, CancellationToken cancellationToken)
        {
            user.NormalizedEmail = normalizedEmail;
            return Task.CompletedTask;
        }

        public Task<string?> GetNormalizedEmailAsync(AppUser user, CancellationToken cancellationToken) =>
            Task.FromResult(user.NormalizedEmail);

        public Task SetNormalizedUserNameAsync(AppUser user, string? normalizedName, CancellationToken cancellationToken)
        {
            user.NormalizedUserName = normalizedName;
            return Task.CompletedTask;
        }

        public Task<string?> GetNormalizedUserNameAsync(AppUser user, CancellationToken cancellationToken) =>
            Task.FromResult(user.NormalizedUserName);

        public Task SetUserNameAsync(AppUser user, string? userName, CancellationToken cancellationToken)
        {
            user.UserName = userName;
            return Task.CompletedTask;
        }

        public Task<string?> GetUserNameAsync(AppUser user, CancellationToken cancellationToken) =>
            Task.FromResult(user.UserName);

        public Task<string> GetUserIdAsync(AppUser user, CancellationToken cancellationToken) =>
            Task.FromResult(user.Id.ToString());

        public Task<DateTimeOffset?> GetLockoutEndDateAsync(AppUser user, CancellationToken cancellationToken) =>
            Task.FromResult(user.LockoutEnd);

        public Task SetLockoutEndDateAsync(AppUser user, DateTimeOffset? lockoutEnd, CancellationToken cancellationToken)
        {
            user.LockoutEnd = lockoutEnd;
            return Task.CompletedTask;
        }

        public Task<int> IncrementAccessFailedCountAsync(AppUser user, CancellationToken cancellationToken)
        {
            user.AccessFailedCount++;
            return Task.FromResult(user.AccessFailedCount);
        }

        public Task ResetAccessFailedCountAsync(AppUser user, CancellationToken cancellationToken)
        {
            user.AccessFailedCount = 0;
            return Task.CompletedTask;
        }

        public Task<int> GetAccessFailedCountAsync(AppUser user, CancellationToken cancellationToken) =>
            Task.FromResult(user.AccessFailedCount);

        public Task<bool> GetLockoutEnabledAsync(AppUser user, CancellationToken cancellationToken) =>
            Task.FromResult(user.LockoutEnabled);

        public Task SetLockoutEnabledAsync(AppUser user, bool enabled, CancellationToken cancellationToken)
        {
            user.LockoutEnabled = enabled;
            return Task.CompletedTask;
        }

        public void Dispose() { }
    }
}
