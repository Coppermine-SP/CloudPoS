using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CloudInteractive.CloudPos.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Org.BouncyCastle.Crypto.Generators;

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
            return Redirect("/administrative/authorize");
        }
        
        if(HttpContext.User.Identity!.IsAuthenticated && HttpContext.User.IsInRole(AuthorizationHandler.AdminRole))
            return Redirect("/administrative/tableview");
        
        return Page();
    }
    
    public async Task<IActionResult> OnPost(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            Message = "필드가 공백일 수 없습니다.";
            return Page();
        }
        
        if (BCrypt.Net.BCrypt.Verify(code, config.AdminPasswordHash))
        {
            var identity = new ClaimsIdentity([
                new Claim(ClaimTypes.Role, AuthorizationHandler.AdminRole)
            ], CookieAuthenticationDefaults.AuthenticationScheme);
            
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
            return Redirect("/administrative/tableview");
        }
        
        
        Message = "암호가 올바르지 않습니다.";
        return Page();
    }
}