using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PPMTool.Pages.Account
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        //public async Task OnGet(string redirectUri)
        //{
        //    // Challenge to force authentication
        //    var props = new AuthenticationProperties { RedirectUri = "/" };
        //    await HttpContext.ChallengeAsync("CAS", props);
        //}

        public IActionResult OnGet(string scheme)
        {
            // Local debugging so just sign in
            var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
            identity.AddClaim(new Claim(identity.NameClaimType, "Test User"));
            return SignIn(new ClaimsPrincipal(identity),
                new AuthenticationProperties { RedirectUri = "/" },
                CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }
}
