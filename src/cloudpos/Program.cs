using CloudInteractive.CloudPos.Contexts;
using CloudInteractive.CloudPos.Event;
using CloudInteractive.CloudPos.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace CloudInteractive.CloudPos;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddRazorPages();
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();
        builder.Services.AddServerSideBlazor();
        builder.Services.AddSignalR();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.HttpOnly = true;
                options.ExpireTimeSpan = TimeSpan.FromHours(4);
                options.LoginPath = "/Customer/Authorize";
            });
        builder.Services.AddAuthorization();
        builder.Services.AddDbContext<ServerDbContext>(options =>
        {
            options.UseInMemoryDatabase("ServerDbContext");
        });
        builder.Services.AddSingleton<ConfigurationService>();
        builder.Services.AddScoped<ModalService>();
        builder.Services.AddScoped<ColorSchemeService>();
        builder.Services.AddScoped<SoundService>();
        builder.Services.AddSingleton<TableEventBroker>();
        
        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }
        
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseAntiforgery();
        app.MapBlazorHub();
        app.MapRazorPages()
            .WithStaticAssets();
        app.MapFallbackToPage("/_Host");
        app.MapGet("/", ctx =>
        { 
            ctx.Response.Redirect("/Customer/Menu");
            return Task.CompletedTask;
        });
        app.UseStaticFiles();
        app.Run();
    }
}