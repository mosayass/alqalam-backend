using AlQalam.Host.Errors;
using AlQalam.Users.Core.Commands.ConfirmEmail;
using AlQalam.Users.Core.Commands.Login;
using AlQalam.Users.Core.Commands.RegisterUser;
using AlQalam.Users.Core.DTOs;
using AlQalam.Users.Core.Enums;

namespace AlQalam.Host.Endpoints.Users;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/users")
            .WithTags("Users")
            .RequireRateLimiting("auth-endpoints");

        group.MapPost("/register", RegisterAsync)
            .WithName("RegisterUser")
            .WithSummary("Register a new learner account and send confirmation email")
            .Produces<UserSummaryDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/confirm-email", ConfirmEmailAsync)
            .WithName("ConfirmEmail")
            .WithSummary("Confirm user email address using verification token")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPost("/login", LoginAsync)
            .WithName("Login")
            .WithSummary("Authenticate user credentials and issue access/refresh tokens")
            .Produces<AuthTokensDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return endpoints;
    }

    public sealed record RegisterUserRequest(
        string FullName,
        string Email,
        string Password,
        MemorizationLevel? InitialLevel = null);

    public sealed record ConfirmEmailRequest(
        Guid UserId,
        string Token);

    public sealed record LoginRequest(
        string Email,
        string Password,
        string? DeviceId = null);

    private static async Task<IResult> RegisterAsync(
        RegisterUserRequest request,
        RegisterUserHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new RegisterUserCommand(
            request.FullName,
            request.Email,
            request.Password,
            request.InitialLevel);

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.ToHttpResult(user => Results.Created($"/api/v1/users/{user.Id}", user));
    }

    private static async Task<IResult> ConfirmEmailAsync(
        ConfirmEmailRequest request,
        ConfirmEmailHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new ConfirmEmailCommand(request.UserId, request.Token);
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? Results.NoContent() : result.ToHttpResult();
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        LoginHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new LoginCommand(request.Email, request.Password, request.DeviceId);
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.ToHttpResult();
    }
}
