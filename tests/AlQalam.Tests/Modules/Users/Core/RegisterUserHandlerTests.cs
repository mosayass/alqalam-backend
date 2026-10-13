using AlQalam.Shared.Results;
using AlQalam.Tests.Common;
using AlQalam.Users.Core.Abstractions;
using AlQalam.Users.Core.Commands.RegisterUser;
using AlQalam.Users.Core.Entities;
using AlQalam.Users.Core.Enums;
using Xunit;

namespace AlQalam.Tests.Modules.Users.Core;

public class RegisterUserHandlerTests
{
    private readonly FakeIdentityService _identityService = new();
    private readonly FakeProfileRepository _profileRepo = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakeEmailSender _emailSender = new();
    private readonly TestTimeProvider _timeProvider = new(DateTimeOffset.UtcNow);

    private RegisterUserHandler CreateHandler() =>
        new(_identityService, _profileRepo, _unitOfWork, _emailSender, _timeProvider);

    [Fact]
    public async Task HandleAsync_WithValidInput_SucceedsAndSendsEmail()
    {
        var handler = CreateHandler();
        var command = new RegisterUserCommand("Zaid Khaled", "zaid@example.com", "P@ssword123!", MemorizationLevel.Intermediate);

        var result = await handler.HandleAsync(command);

        Assert.True(result.IsSuccess);
        Assert.Equal("Zaid Khaled", result.Value.FullName);
        Assert.Equal("zaid@example.com", result.Value.Email);
        Assert.False(result.Value.EmailConfirmed);

        // Transaction committed
        Assert.True(_unitOfWork.Committed);
        Assert.False(_unitOfWork.RolledBack);

        // Profile persisted
        Assert.NotNull(_profileRepo.AddedProfile);
        Assert.Equal(result.Value.Id, _profileRepo.AddedProfile.UserId);

        // Email sent
        Assert.Equal("zaid@example.com", _emailSender.LastSentEmail);
        Assert.Equal(result.Value.Id, _emailSender.LastSentUserId);
        Assert.Equal("fake-token-123", _emailSender.LastSentToken);
    }

    [Fact]
    public async Task HandleAsync_WhenDuplicateEmail_ReturnsConflictAndRollsBack()
    {
        _identityService.ShouldReturnDuplicateEmail = true;
        var handler = CreateHandler();
        var command = new RegisterUserCommand("Zaid", "duplicate@example.com", "P@ssword123!");

        var result = await handler.HandleAsync(command);

        Assert.True(result.IsFailure);
        Assert.Equal("Users.DuplicateEmail", result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);

        Assert.True(_unitOfWork.RolledBack);
        Assert.Null(_profileRepo.AddedProfile);
        Assert.Null(_emailSender.LastSentEmail);
    }

    [Fact]
    public async Task HandleAsync_WhenIdentityFails_RollsBackWithoutSavingProfile()
    {
        _identityService.FailureError = Error.Validation("Users.RegistrationFailed", "Password requires digit.");
        var handler = CreateHandler();
        var command = new RegisterUserCommand("Zaid", "zaid@example.com", "weakpass");

        var result = await handler.HandleAsync(command);

        Assert.True(result.IsFailure);
        Assert.Equal("Users.RegistrationFailed", result.Error.Code);
        Assert.True(_unitOfWork.RolledBack);
        Assert.Null(_profileRepo.AddedProfile);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("user@")]
    public async Task HandleAsync_WithInvalidEmail_ReturnsValidation(string invalidEmail)
    {
        var handler = CreateHandler();
        var command = new RegisterUserCommand("Zaid", invalidEmail, "P@ssword123!");

        var result = await handler.HandleAsync(command);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    private sealed class FakeIdentityService : IIdentityService
    {
        public bool ShouldReturnDuplicateEmail { get; set; }
        public Error? FailureError { get; set; }

        public Task<Result<Guid>> CreateAccountAsync(Guid userId, string email, string password, CancellationToken cancellationToken = default)
        {
            if (ShouldReturnDuplicateEmail)
            {
                return Task.FromResult<Result<Guid>>(Error.Conflict("Users.DuplicateEmail", "Duplicate email"));
            }

            if (FailureError is not null)
            {
                return Task.FromResult<Result<Guid>>(FailureError.Value);
            }

            return Task.FromResult<Result<Guid>>(userId);
        }

        public Task<Result<string>> GenerateEmailConfirmationTokenAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result<string>.Success("fake-token-123"));

        public Task<Result> ConfirmEmailAsync(Guid userId, string token, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Success());

        public Task<Result<Guid>> CheckCredentialsAsync(string email, string password, CancellationToken cancellationToken = default) =>
            Task.FromResult<Result<Guid>>(Guid.NewGuid());
    }

    private sealed class FakeProfileRepository : IUserProfileRepository
    {
        public UserProfile? AddedProfile { get; private set; }

        public Task AddAsync(UserProfile profile, CancellationToken cancellationToken = default)
        {
            AddedProfile = profile;
            return Task.CompletedTask;
        }

        public Task<UserProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(AddedProfile?.UserId == userId ? AddedProfile : null);
    }

    private sealed class FakeUnitOfWork : IUsersUnitOfWork
    {
        public bool Committed { get; private set; }
        public bool RolledBack { get; private set; }

        public Task<IUsersTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IUsersTransaction>(new FakeTransaction(this));

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        private sealed class FakeTransaction(FakeUnitOfWork parent) : IUsersTransaction
        {
            public Task CommitAsync(CancellationToken cancellationToken = default)
            {
                parent.Committed = true;
                return Task.CompletedTask;
            }

            public Task RollbackAsync(CancellationToken cancellationToken = default)
            {
                parent.RolledBack = true;
                return Task.CompletedTask;
            }

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }

    private sealed class FakeEmailSender : IConfirmationEmailSender
    {
        public string? LastSentEmail { get; private set; }
        public Guid? LastSentUserId { get; private set; }
        public string? LastSentToken { get; private set; }

        public Task SendConfirmationEmailAsync(string email, Guid userId, string confirmationToken, CancellationToken cancellationToken = default)
        {
            LastSentEmail = email;
            LastSentUserId = userId;
            LastSentToken = confirmationToken;
            return Task.CompletedTask;
        }
    }
}
