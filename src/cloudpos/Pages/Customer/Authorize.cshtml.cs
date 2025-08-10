using System.Security.Claims;
using CloudInteractive.CloudPos.Contexts;
using CloudInteractive.CloudPos.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CloudInteractive.CloudPos.Pages.Customer;

public class Authorize(ILogger<Authorize> logger, ServerDbContext context, ConfigurationService config) : PageModel
{
    [TempData] public string? Message { get; set; }
    public string WelcomeMessage = config.WelcomeMessage;
    
    public async Task<IActionResult> OnGet(string? code, int? error)
    {
        if (code is not null) return await OnPost(code);
        if (error is not null)
        {
            Message = error switch
            {
                0 => null,
                1 => "세션이 만료되었습니다.",
                2 => "올바르지 않은 세션입니다.",
                _ => "알 수 없는 오류"
            };
            
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToPage();
        }
        return Page();
    }

    public async Task<IActionResult> OnPost(string code)
    {
        var session = await context.Sessions.FirstOrDefaultAsync(x => x.AuthCode != null && x.AuthCode!.Equals(code));

        if (session is null)
        {
            logger.LogInformation($"Auth failed (status=invalid, code={code}).");
            Message = "인증 코드가 올바르지 않습니다.";
            return Page();
        }

        var identity = new ClaimsIdentity([
            new Claim(ClaimTypes.Role, AuthorizationHandler.CustomerRole),
            new Claim(ClaimTypes.Sid, session.SessionId.ToString())
        ], CookieAuthenticationDefaults.AuthenticationScheme);
        
        logger.LogInformation($"Auth success (code={code}).");
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
        return Redirect("/Customer/Menu");
    }
}