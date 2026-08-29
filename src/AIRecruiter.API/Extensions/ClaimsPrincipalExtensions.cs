using System.Security.Claims;

namespace AIRecruiter.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static int GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return int.Parse(value ?? throw new InvalidOperationException("User id claim not found."));
    }

    public static string GetRole(this ClaimsPrincipal user)
    {
        return user.FindFirstValue(ClaimTypes.Role) ?? throw new InvalidOperationException("Role claim not found.");
    }
}
