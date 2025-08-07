using CloudInteractive.CloudPos.Contexts;
using CloudInteractive.CloudPos.Models;
using CloudInteractive.CloudPos.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace CloudInteractive.CloudPos.Components;

public partial class SessionReceipt(IDbContextFactory<ServerDbContext> factory, TableService table, ConfigurationService config) : ComponentBase
{
    private TableSession? _session;
    private List<TableService.OrderSummary>? _orderSummaries;
    private int _totalAmount = 0;
    
    private string CurrencyFormat(int x) => x == 0 ? "0" : $"{x:#,###}";
    protected override async Task OnInitializedAsync()
    {
        await using var context = await factory.CreateDbContextAsync();
        _session = await context.Sessions
            .Include(x => x.Table)
            .FirstAsync(x => x.SessionId == SessionId);
        _orderSummaries = await table.SessionOrderSummaryAsync(SessionId);
        _totalAmount = _orderSummaries.Sum(x => x.TotalPrice);
        StateHasChanged();
    }
}