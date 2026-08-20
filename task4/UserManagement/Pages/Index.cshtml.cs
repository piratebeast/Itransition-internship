using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace UserManagement.Pages;

public class IndexModel : PageModel
{
    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToPage("/Users/Index");
        }

        return RedirectToPage("/Account/Login", new { area = "Identity" });
    }
}