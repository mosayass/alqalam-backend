using Microsoft.AspNetCore.Identity;

namespace AlQalam.Users.Infrastructure.Entities;

public class AppUser : IdentityUser<Guid>
{
    public AppUser()
    {
        SecurityStamp = Guid.NewGuid().ToString("N");
    }

    public AppUser(string email) : this()
    {
        Email = email;
        UserName = email;
    }
}
