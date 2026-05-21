#if RELEASE
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
#else
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
#endif
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PPMTool.Data.Context;
using PPMTool.Services;
using PPMTool.Data.Enums;
using PPMTool.Data;

namespace PPMTool.Pages.Account
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private UserService userService;
        private ILogger<LoginModel> logger;
        private IDbContextFactory<PPMToolContext> contextFactory;
        private IConfiguration configuration;

        [FromQuery(Name = "returnUrl")]
        public string ReturnUrl { get; set; }

        [FromQuery(Name = "username")]
        public string Username { get; set; }

        public LoginModel(
            UserService userService,
            ILogger<LoginModel> logger,
            IDbContextFactory<PPMToolContext> contextFactory,
            IConfiguration configuration)
        {
            this.userService = userService;
            this.logger = logger;
            this.contextFactory = contextFactory;
            this.configuration = configuration;
        }

#if RELEASE
        public async Task OnGet()
        {

            // Set up properties
            var redirectUri = string.IsNullOrWhiteSpace(ReturnUrl) ? "/" : ReturnUrl;
            var props = new AuthenticationProperties { RedirectUri = redirectUri };
            var authType = configuration.GetValue<string>("Authentication:Type", "CAS");

            // Challenge to force authentication
            if (authType == "CAS")
            {
                await HttpContext.ChallengeAsync("CAS", props);
            }
            else if (authType == "AzureAd")
            {

                await HttpContext.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme, props);
            }
            else
            {
                throw new InvalidOperationException($"Unsupported authentication type: {authType}");
            }
        }
#else
        public async Task OnGet()
        {
            // Local debugging so just sign in
            var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
            identity.AddClaim(new Claim(ClaimTypes.Name, Username));

            // Add roles from DB for this user
            using (var context = contextFactory.CreateDbContext())
            {
                var username = identity.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Name)?.Value ?? "";
                var user = userService.GetByUsername(context, username.Clean());
                var role = string.IsNullOrWhiteSpace(username) || user == null ? RoleType.None : user.RoleType;
                identity.AddClaim(new Claim(ClaimTypes.Role, role.ToString()));

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(identity),
                    new AuthenticationProperties { RedirectUri = $"{(string.IsNullOrWhiteSpace(ReturnUrl) ? "/" : ReturnUrl)}" }
                );

                // Update last logged in and log
                if (user != null)
                {
                    userService.UpdateLastLoggedIn(context, user);
                }
                logger?.LogInformation($"{identity.Name}: Logged In");
            }
        }
#endif
    }
}
