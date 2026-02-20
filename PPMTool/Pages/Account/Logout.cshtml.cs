// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
#if !LOCAL
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
#endif

namespace PPMTool.Pages.Account
{
    public class LogoutModel : PageModel
    {
        private IConfiguration configuration;

        public LogoutModel(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

#if !LOCAL
        public IActionResult OnGet()
        {
            var authType = configuration.GetValue<string>("Authentication:Type", "CAS");
            if (authType == "CAS")
            {
                return SignOut(CookieAuthenticationDefaults.AuthenticationScheme);
            }
            else if (authType == "AzureAd")
            {
                return SignOut(
                    new AuthenticationProperties
                    {
                        RedirectUri = "/"
                    },
                    OpenIdConnectDefaults.AuthenticationScheme,
                    CookieAuthenticationDefaults.AuthenticationScheme);
            }
            throw new InvalidOperationException($"Unsupported authentication type: {authType}");
        }
#else
        public IActionResult OnGet()
        {
            HttpContext.SignOutAsync().GetAwaiter().GetResult();
            return Redirect("/");
        }
#endif
    }
}
