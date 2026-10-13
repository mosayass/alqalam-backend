using AlQalam.Users.Core.Commands.ConfirmEmail;
using AlQalam.Users.Core.Commands.Login;
using AlQalam.Users.Core.Commands.RegisterUser;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AlQalam.Users.Core;

public static class DependencyInjection
{
    public static IServiceCollection AddUsersCore(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);

        services.AddScoped<RegisterUserHandler>();
        services.AddScoped<ConfirmEmailHandler>();
        services.AddScoped<LoginHandler>();

        return services;
    }
}
