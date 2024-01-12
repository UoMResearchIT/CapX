using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PPMTool.Pages.Account
{
    public class LogoutModel : PageModel
    {
#if !LOCAL
        public IActionResult OnGet()
        {

            return SignOut();
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
