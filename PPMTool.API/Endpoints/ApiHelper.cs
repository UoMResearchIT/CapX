using Microsoft.EntityFrameworkCore;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;
using PPMTool.Enums;

namespace PPMTool.API.Endpoints;

/// <summary>
/// Provides general helper methods for common repeatedable actions in minimal API endpoints.
/// </summary>
public static class APIHelper
{
    /// <summary>
    /// Gets the authenticated user from the HttpContext. Should always be present if the authentication middleware is correctly set up.
    /// </summary>
    /// <param name="context"></param>
    /// <returns>User entity if there is one in the context</returns>
    internal static User GetCurrentUser(HttpContext context)
    {
        context.Items.TryGetValue("User", out var user);
        var castUser = user as User;
        if (castUser == null)
        {
            throw new InvalidOperationException("User not found in the context!");
        }
        return castUser;
    }

    /// <summary>
    /// Checks if a given user has the Superuser role.
    /// </summary>
    /// <param name="user"></param>
    /// <returns>True if user is a superuser</returns>
    internal static bool IsSuperUser(User? user)
    {
        if (user == null) return false;
        return user.RoleType == RoleType.Superuser;
    }

    /// <summary>
    /// Get a matching person if they exist by name, case insensitive, underscores treated as spaces
    /// </summary>
    /// <param name="context"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    internal static async Task<Person?> FindPersonWithLineManagerByNameAsync(PPMToolContext context, string name)
    {
        return await context.People
                .Include(x => x.LineManager)
                .FirstOrDefaultAsync(x => x.Name.ToLower() == name.Trim().ToLower().Replace("_", " "));
    }

    /// <summary>
    /// Determines if the caller is a superuser, the line manager of the specified person, or the person themselves.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="http"></param>
    /// <param name="person"></param>
    /// <returns></returns>
    internal static bool IsSuperUserOrLineManagerOrSelf(PPMToolContext context, HttpContext http, Person person)
    {
        var caller = GetCurrentUser(http);
        var callerPersonId = caller.Person?.PersonId ?? 0;

        // Self?
        var isSelf = callerPersonId != 0 && callerPersonId == person.PersonId;

        // LM?
        var isLineManager = person.LineManager?.PersonId == callerPersonId;

        // SU?
        var isSuper = IsSuperUser(caller);

        return isSelf || isLineManager || isSuper;
    }
}