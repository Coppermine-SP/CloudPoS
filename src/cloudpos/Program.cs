using CloudInteractive.CloudPos.Contexts;
using CloudInteractive.CloudPos.Event;
using CloudInteractive.CloudPos.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace CloudInteractive.CloudPos;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddRazorPages();
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();
        builder.Services.AddServerSideBlazor();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddAuthorizationCore();
        builder.Services.AddMemoryCache();
        builder.Services.AddCascadingAuthenticationState();
        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.HttpOnly = true;
                options.ExpireTimeSpan = TimeSpan.FromHours(4);
            });
        builder.Services.AddDbContextFactory<ServerDbContext>(opt =>
            opt.UseMySQL(builder.Configuration.GetConnectionString("ServerDbContext")!));
        builder.Services.AddScoped<IAuthorizationHandler, AuthorizationHandler>();
        builder.Services.AddAuthorization(option =>
        {
            // 활성 세션(메뉴, 주문내역 등)을 요구하는 정책
            option.AddPolicy(AuthorizationHandler.ActiveSessionPolicy, policy =>
            {
                policy.AddRequirements(new SessionValidateRequirement { RequireEndedSession = false });
            });
            // 종료된 세션(영수증)을 요구하는 정책
            option.AddPolicy(AuthorizationHandler.EndedSessionPolicy, policy =>
            {
                policy.AddRequirements(new SessionValidateRequirement { RequireEndedSession = true });
            });
        });
        builder.Services.AddSingleton<ConfigurationService>();
        builder.Services.AddScoped<ModalService>();
        builder.Services.AddScoped<InteractiveInteropService>();
        builder.Services.AddScoped<TableService>();
        builder.Services.AddSingleton<TableEventBroker>();
        var app = builder.Build();
        
        app.UseStaticFiles();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseAntiforgery();
        app.MapGet("/manifest.json", (HttpContext context) =>
        {
            context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            context.Response.Headers["Pragma"] = "no-cache";
            context.Response.Headers["Expires"] = "0";
            return context.User.IsInRole(AuthorizationHandler.AdminRole) ? Results.File("manifest", "application/manifest+json") : Results.NotFound();
        });

        app.MapBlazorHub();
        app.MapRazorPages()
            .WithStaticAssets();
        app.MapFallbackToPage("/_Host");
        app.MapGet("/", ctx =>
        { 
            ctx.Response.Redirect("/Customer/Menu");
            return Task.CompletedTask;
        });
        app.Run();
    }
}