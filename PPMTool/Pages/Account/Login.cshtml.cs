// SPDX-FileCopyrightText: 2025 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

#if LOCAL
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using PPMTool.Enums;
#endif
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PPMTool.Data.Context;
using PPMTool.Services;

namespace PPMTool.Pages.Account
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private UserService userService;
        private ILogger<LoginModel> logger;
        private IDbContextFactory<PPMToolContext> contextFactory;

        [FromQuery(Name = "returnUrl")]
        public string ReturnUrl { get; set; }

        [FromQuery(Name = "username")]
        public string Username { get; set; }

        public LoginModel(UserService userService, ILogger<LoginModel> logger, IDbContextFactory<PPMToolContext> contextFactory)
        {
            this.userService = userService;
            this.logger = logger;
            this.contextFactory = contextFactory;
        }

#if !LOCAL
        public async Task OnGet()
        {
            // Challenge to force authentication
            var props = new AuthenticationProperties { RedirectUri = $"{(string.IsNullOrWhiteSpace(ReturnUrl) ? "/" : ReturnUrl)}" };
            await HttpContext.ChallengeAsync("CAS", props);
        }
#else
        public async Task OnGet()
        {
            // Local debugging so just sign in
            var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
            identity.AddClaim(new Claim(ClaimTypes.Name, Username));

            // Add roles from DB for this user
            var username = identity.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Name)?.Value ?? "";
            var user = userService.GetByUsername(contextFactory.CreateDbContext(), username.Trim().ToLower());
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
                userService.UpdateLastLoggedIn(contextFactory.CreateDbContext(), user);
            }
            logger?.LogInformation($"{identity.Name}: Logged In");
        }
#endif
    }
}
