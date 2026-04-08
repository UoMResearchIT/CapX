using System.Security.Claims;
using System.Web;
using GSS.Authentication.CAS.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.EntityFrameworkCore;
using PPMTool.Data;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;
using PPMTool.Data.Enums;
using PPMTool.Services;

namespace PPMTool
{
    /// <summary>
    /// Helper class to handle authentication callbacks for both Azure AD / Entra and CAS logins./
    /// </summary>
    internal static class AuthenticationCallbackHelper
    {
        /// <summary>
        /// Process the user login by looking up in role database
        /// </summary>
        /// <param name="httpContext"></param>
        /// <param name="identity"></param>
        /// <param name="username"></param>
        /// <param name="logger"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        private static bool ProcessUserLogin(
            HttpContext httpContext,
            ClaimsIdentity identity,
            string username,
            ILogger logger,
            out User user)
        {
            // Set default
            user = null;

            // Check the username given is not null
            if (string.IsNullOrWhiteSpace(username))
            {
                logger.LogWarning("No usable username claim");
                return false;
            }

            logger.LogInformation($"Signing in {username}");

            // Create a temporaru DB context
            var dbContextFactory =
                httpContext.RequestServices
                    .GetRequiredService<IDbContextFactory<PPMToolContext>>();
            using var dbContext = dbContextFactory.CreateDbContext();

            // Find matching user if exists
            user = dbContext.Users
                .Include(x => x.Person)
                .ToList()
                .FirstOrDefault(x => x.MatchesClaim(username.Clean()));

            // Add claims to the identity object
            identity.AddClaim(new Claim(ClaimTypes.Role, user?.RoleType.ToString() ?? RoleType.None.ToString()));
            identity.AddClaim(new Claim(ClaimTypes.Name, username));

            // Fail if not
            if (user == null)
            {
                logger.LogWarning($"User {username} logged in but not found in access DB");
                return false;
            }

            // Update last logged in
            var userService =
                httpContext.RequestServices.GetRequiredService<UserService>();
            userService.UpdateLastLoggedIn(dbContext, user);

            logger.LogInformation($"{username} logged in successfully");

            return true;
        }

        /// <summary>
        /// What to do when the token is validated after Azure AD / Entra login
        /// </summary>
        public static void OnAzureAdTokenValidated(TokenValidatedContext context)
        {
            // Check the identity is present
            if (context.Principal?.Identity is not ClaimsIdentity identity)
            {
                context.Fail("No identity found");
                return;
            }

            // Create loggin object
            var logger = context.HttpContext.RequestServices
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("AzureAdAuth");

            // Parse the username
            var username =
                identity.FindFirst("preferred_username")?.Value ??
                identity.FindFirst(ClaimTypes.Email)?.Value ??
                identity.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Process the useranme
            var success = ProcessUserLogin(
                context.HttpContext,
                identity,
                username,
                logger,
                out _);
        }

        /// <summary>
        /// What to do when a ticket is to be created from a CAS callback
        /// </summary>
        public static async Task OnCreatingTicket(CasCreatingTicketContext context)
        {
            // Check the identity is present
            if (context.Principal?.Identity is not ClaimsIdentity identity)
            {
                return;
            }

            // Get the username from the assertion
            var assertion = context.Assertion;
            var username = assertion.PrincipalName;

            // Add the claims
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, username));
            identity.AddClaim(new Claim(ClaimTypes.Name, username));

            // Get a logging instance
            var logger =
                context.HttpContext.RequestServices
                    .GetRequiredService<ILogger<CasEvents>>();

            // Process the user
            var success = ProcessUserLogin(
                context.HttpContext,
                identity,
                username,
                logger,
                out var user);

            // Explicitly sign in the user with the created principal to ensure the cookie is issued
            await context.HttpContext.SignInAsync(context.Principal);

            logger.LogInformation($"{identity.Name}: Logged In");
        }

        /// <summary>
        /// What to do when the user signs out from a CAS session
        /// </summary>
        public static async Task OnCookieSigningOut(CookieSigningOutContext context, IConfiguration configuration)
        {
            // Single Sign-Out
            var casUrl = new Uri(configuration["Authentication:CAS:ServerUrlBase"]);
            var redirectUri = UriHelper.BuildAbsolute(
                casUrl.Scheme,
                new HostString(casUrl.Host, casUrl.Port),
                casUrl.LocalPath,
                "/logout",
                QueryString.Create("service", configuration["HostUrl"])
            );

            var logoutRedirectContext = new RedirectContext<CookieAuthenticationOptions>(
                context.HttpContext,
                context.Scheme,
                context.Options,
                context.Properties,
                redirectUri
            );
            context.Response.StatusCode = 204; // Prevent RedirectToReturnUrl
            await context.Options.Events.RedirectToLogout(logoutRedirectContext);
        }

        /// <summary>
        /// What to do when there is a failure during login
        /// </summary>
        public static async Task OnRemoteFailure(RemoteFailureContext context)
        {
            var failure = context.Failure;
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<CasEvents>>();
            if (!string.IsNullOrWhiteSpace(failure?.Message))
            {
                logger.LogError(failure, "Authentication failed: {Exception}", failure?.Message);
            }

            // Clear local cookie
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            context.Response.Redirect($"/Account/ExternalLoginFailure?message={HttpUtility.UrlEncode(failure?.Message)}");
            context.HandleResponse();
        }
    }
}
