using CloudInteractive.CloudPos.Contexts;
using CloudInteractive.CloudPos.Models;
using CloudInteractive.CloudPos.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace CloudInteractive.CloudPos.Components;

public partial class SessionObjectManager(IDbContextFactory<ServerDbContext> factory, ModalService modal, InteractiveInteropService interop) : ComponentBase
{
    private int _selectedTableId = -1;
    private int _selectedState = -1;
    private int GetSessionCount()
    {
        using var context = factory.CreateDbContext();
        return context.Sessions.Count();
    }

    private List<Table> GetTables()
    {
        using var context = factory.CreateDbContext();
        return context.Tables.ToList();
    }

    private List<TableSession> GetSessions()
    {
        using var context = factory.CreateDbContext();
        var query = context.Sessions.AsQueryable();

        if (_selectedTableId != -1)
            query = query.Where(x => x.TableId == _selectedTableId);

        if (_selectedState != -1)
            query = query.Where(x => x.State == (TableSession.SessionState)_selectedState);

        return query.Include(x => x.Orders)
            .ThenInclude(x => x.OrderItems)
            .ThenInclude(x => x.Item)
            .Include(x => x.Table)
            .ToList();
    }
    
    private string CurrencyFormat(int x) => $"￦{x:#,###}";
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