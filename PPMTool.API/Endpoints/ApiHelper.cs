using PPMTool.Data.Entities;
using PPMTool.Enums;

namespace PPMTool.API.Endpoints;

public static class ApiHelper
{
    /// <summary>
    /// Gets the authenticated user from the HttpContext.
    /// </summary>
    public static User? GetCurrentUser(HttpContext http)
    {
        return http.Items["User"] as User;
    }

    /// <summary>
    /// Checks if a given user has the Superuser role.
    /// </summary>
    public static bool IsSuperUser(User? user)
    {
        if (user == null) return false;
        return user.RoleType == RoleType.Superuser;
    }
}