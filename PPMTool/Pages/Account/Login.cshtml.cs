#if LOCAL
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
#endif
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;
using PPMTool.Data.Context;
using PPMTool.Services;
using System.Linq;

namespace PPMTool.Pages.Account
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private RolesService _roleService;

        public LoginModel(RolesService rolesService)
        {
            _roleService = rolesService;
        }

#if !LOCAL
        public async Task OnGet()
        {
            // Challenge to force authentication
            var props = new AuthenticationProperties { RedirectUri = "/" };
            await HttpContext.ChallengeAsync("CAS", props);
        }
#else
        public async Task OnGet()
        {
            // Local debugging so just sign in
            var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, "mbgm6ah3"));
            identity.AddClaim(new Claim(ClaimTypes.Name, "mbgm6ah3"));

            // Add roles from DB for this user
            var username = identity.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Name)?.Value ?? "";
            var role = string.IsNullOrWhiteSpace(username) ? RoleType.None : _roleService.GetRoleTypeForUsername(new PPMToolContext(), username.Trim().ToLower());
            identity.AddClaim(new Claim(ClaimTypes.Role, role.ToString()));

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties { RedirectUri = "/" }
            );
        }
#endif
    }
}
