using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CloudInteractive.CloudPos.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CloudInteractive.CloudPos.Pages.Administrative;

public class Authorize(ConfigurationService config) : PageModel
{
    [TempData] public string? Message { get; set; }
    public string StoreName = config.StoreName;
    public async Task<IActionResult> OnGet(bool signOut)
    {
        if(signOut)
        {
            Message = "로그아웃 되었습니다.";
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Redirect("/Administrative/Authorize");
        }
        
        if(HttpContext.User.Identity!.IsAuthenticated && HttpContext.User.IsInRole(AuthorizationHandler.AdminRole))
            return Redirect("/Administrative/TableView");
        
        return Page();
    }

    public async Task<IActionResult> OnPost(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            Message = "필드가 공백일 수 없습니다.";
            return Page();
        }

        var sha256 = SHA256.Create();
        byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(code));
        
        if (config.AdminPasswordHash == Convert.ToBase64String(hash))
        {
            var identity = new ClaimsIdentity([
                new Claim(ClaimTypes.Role, AuthorizationHandler.AdminRole)
            ], CookieAuthenticationDefaults.AuthenticationScheme);
            
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
            return Redirect("/Administrative/TableView");
        }
        
        
        Message = "암호가 올바르지 않습니다.";
        return Page();
    }
}