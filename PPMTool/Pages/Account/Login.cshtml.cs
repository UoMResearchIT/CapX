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
        private RolesService _roleService;
        private ILogger<LoginModel> _logger;
        private IDbContextFactory<PPMToolContext> _contextFactory;

        [FromQuery(Name = "returnUrl")]
        public string ReturnUrl { get; set; }

        public LoginModel(RolesService rolesService, ILogger<LoginModel> logger, IDbContextFactory<PPMToolContext> contextFactory)
        {
            _roleService = rolesService;
            _logger = logger;
            _contextFactory = contextFactory;
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
            identity.AddClaim(new Claim(ClaimTypes.Name, "mfztsphb"));

            // Add roles from DB for this user
            var username = identity.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Name)?.Value ?? "";
            var roleEntity = _roleService.GetByUsername(_contextFactory.CreateDbContext(), username.Trim().ToLower());
            var role = string.IsNullOrWhiteSpace(username) || roleEntity == null ? RoleType.None : roleEntity.RoleType;
            identity.AddClaim(new Claim(ClaimTypes.Role, role.ToString()));

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties { RedirectUri = $"{(string.IsNullOrWhiteSpace(ReturnUrl) ? "/" : ReturnUrl)}" }
            );

            // Update last logged in and log
            if (roleEntity != null)
            {
                _roleService.UpdateLastLoggedIn(_contextFactory.CreateDbContext(), roleEntity);
            }
            _logger?.LogInformation($"{identity.Name}: Logged In");
        }
#endif
    }
}
