#if LOCAL
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
#endif
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;

namespace PPMTool.Pages.Account
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
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
            identity.AddClaim(new Claim(identity.NameClaimType, "Test User"));
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties { RedirectUri = "/" }
            );
        }
#endif
    }
}
