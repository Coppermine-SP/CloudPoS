using CloudInteractive.CloudPos.Contexts;
using CloudInteractive.CloudPos.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.EntityFrameworkCore;

namespace CloudInteractive.CloudPos.Components;

public partial class SessionObjectManager(IDbContextFactory<ServerDbContext> factory) : ComponentBase
{
    private int _selectedTableId = -1;
    private int _selectedState = -1;
    private List<Table>? _tables;

    protected override async Task OnInitializedAsync()
    {
        _tables = await GetTablesAsync();
    }
    
    private async Task<List<Table>> GetTablesAsync()
    {
        await using var context = await factory.CreateDbContextAsync();
        return await context.Tables.ToListAsync();
    }

    private async ValueTask<ItemsProviderResult<TableSession>> LoadSessionsAsync(ItemsProviderRequest request)
    {
        await using var context = await factory.CreateDbContextAsync();
        var ct = request.CancellationToken;
        var query = context.Sessions.AsNoTracking();

        if (_selectedTableId != -1)
            query = query.Where(x => x.TableId == _selectedTableId);

        if (_selectedState != -1)
            query = query.Where(x => x.State == (TableSession.SessionState)_selectedState);

        var totalCount = await query.CountAsync(ct);
        var page = await query.Include(x => x.Orders)
            .ThenInclude(x => x.OrderItems)
            .ThenInclude(x => x.Item)
            .Include(x => x.Table)
            .Skip(request.StartIndex)
            .Take(request.Count)
            .ToListAsync(ct);
        
        return new ItemsProviderResult<TableSession>(page, totalCount);
    }
    
    private string CurrencyFormat(int x) => x == 0 ? "￦0": $"￦{x:#,###}";
    private List<Tuple<string, int, int, int>> GetSessionOrderSummary(TableSession session)
    {
        return session.Orders.Where(x => x.Status != Order.OrderStatus.Cancelled)
            .SelectMany(x => x.OrderItems)
            .GroupBy(x => x.ItemId)
            .Select(x =>
                new Tuple<string, int, int, int>(x.First().Item.Name, x.First().Item.Price, x.Sum(y => y.Quantity),
                    x.Sum(y => y.Quantity * y.Item.Price)))
            .ToList();
    }
}