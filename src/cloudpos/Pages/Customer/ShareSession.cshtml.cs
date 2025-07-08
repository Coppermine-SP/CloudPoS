using System.Security.Claims;
using CloudInteractive.CloudPos.Contexts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CloudInteractive.CloudPos.Pages.Customer;

[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
public class ShareSession(ServerDbContext context, LinkGenerator link) : PageModel
{
    public required string? QrUrl;
    public IActionResult OnGet()
    {
        var claim = HttpContext.User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Sid);
        if (claim is null) return Unauthorized();

        var session = context.Sessions.FirstOrDefault(x => x.SessionId == Convert.ToInt32(claim.Value));
        if (session is null) return BadRequest();
        
        QrUrl = link.GetUriByPage(HttpContext, "/Customer/Authorize", values: new {code = session.AuthCode});
        return Page();
    }
}