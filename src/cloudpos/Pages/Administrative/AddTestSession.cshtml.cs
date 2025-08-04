using CloudInteractive.CloudPos.Models;
using CloudInteractive.CloudPos.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CloudInteractive.CloudPos.Pages.Administrative;

public class AddTestSession(TableService table) : PageModel
{
    public TableSession? session;
    public async Task OnGet(int tableId)
    {
        session = await table.CreateSessionAsync(tableId);
    }
}