using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityServer.Areas.Identity.Pages.Account
{
    [Authorize]
    public class LoginModel : PageModel
    {
        public void OnGet()
        {
        }

        public void OnPost()
        {
        }
    }
}