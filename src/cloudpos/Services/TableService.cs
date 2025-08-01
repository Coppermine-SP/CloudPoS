using CloudInteractive.CloudPos.Contexts;
using CloudInteractive.CloudPos.Event;
using CloudInteractive.CloudPos.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace CloudInteractive.CloudPos.Services;

public class TableService
{
    private readonly IDbContextFactory<ServerDbContext> _factory;
    private readonly TableEventBroker _broker;
    private readonly IAuthorizationHandler _handler;
    private readonly ILogger _logger;
    public TableService(IDbContextFactory<ServerDbContext> factory, TableEventBroker broker,
        IAuthorizationHandler handler, ILogger<TableService> logger)
    {
        _broker = broker;
        _handler = handler;
        _logger = logger;
        _factory = factory;
    }
    private AuthorizationHandler AuthHandler => (AuthorizationHandler)_handler;

    public async Task<TableSession?> GetSessionAsync(int? sessionId = null)
    {
        await using var context = await _factory.CreateDbContextAsync();
        return sessionId is null
            ? AuthHandler.Session
            : await context.Sessions.AsNoTracking().Include(x => x.Table).FirstOrDefaultAsync(x => x.SessionId == sessionId);
    }
    
    public TableSession? GetSession(int? sessionId = null)
    {
        using var context = _factory.CreateDbContext();
        return sessionId is null
            ? AuthHandler.Session
            : context.Sessions.AsNoTracking().Include(x => x.Table).FirstOrDefault(x => x.SessionId == sessionId);
    }

    public async Task<bool> CompleteSessionAsync(int sessionId)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var session = await context.Sessions.FirstOrDefaultAsync(s => s.SessionId == sessionId);
        if (session is null)
        {
            _logger.LogWarning("존재하지 않는 세션을 완료하려고 시도했습니다: {SessionId}", sessionId);
            return false;
        }

        if (session.State != TableSession.SessionState.Billing)
        {
            _logger.LogWarning("완료하려는 세션의 상태가 잘못되었습니다: {SessionId}", sessionId);
            return false;
        }

        session.State = TableSession.SessionState.Completed;
        await context.SaveChangesAsync();
        return true;
    }
        
    public async Task<bool> EndSessionAsync(int? sessionId = null)
    {
        await using var context = await _factory.CreateDbContextAsync();

        var targetSessionId = sessionId ?? (await GetSessionAsync())?.SessionId;
        if (targetSessionId is null)
        {
            _logger.LogWarning("EndSessionAsync가 유효한 세션 ID 없이 호출되었습니다.");
            return false;
        }
        
        var session = await context.Sessions.FirstOrDefaultAsync(s => s.SessionId == targetSessionId.Value);
        if (session is null)
        {
            _logger.LogWarning("존재하지 않는 세션을 종료하려고 시도했습니다: {SessionId}", targetSessionId.Value);
            return false;
        }
        
        if (await context.Orders.AnyAsync(x => x.SessionId == session.SessionId && x.Status == Order.OrderStatus.Received))
        {
            _logger.LogInformation("세션 {SessionId}에 처리되지 않은 주문이 있어 종료할 수 없습니다.", session.SessionId);
            return false;
        }
        
        session.EndedAt = DateTime.Now;
        session.State = TableSession.SessionState.Billing;
        session.AuthCode = null;
        await context.SaveChangesAsync();
        AuthHandler.ClearSessionCache(session.SessionId);
        _broker.Publish(new TableEventArgs()
        {
            TableId = session.TableId,
            EventType = TableEventArgs.TableEventType.SessionEnd,
            Data = session.SessionId
        });

        return true;
    }
    
    public record OrderSummary(int ItemId, string ItemName, int Price, int TotalQty, int TotalPrice);
    public async Task<List<OrderSummary>> SessionOrderSummaryAsync(int? sessionId = null)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var session = await GetSessionAsync(sessionId);
        return await context.Orders
            .Where(x => x.SessionId == session!.SessionId)
            .Where(x => x.Status == Order.OrderStatus.Completed)
            .SelectMany(x => x.OrderItems)
            .GroupBy(x => x.ItemId)
            .Select(x => 
                new OrderSummary(x.Key, x.First().Item.Name, x.First().Item.Price, x.Sum(y => y.Quantity), x.Sum(y => y.Quantity * y.Item.Price)))
            .ToListAsync();
    }

    public record OrderItem(int ItemId, int Quantity);
    public async Task<bool> MakeOrderAsync(List<OrderItem> orderItems, int? sessionId = null)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var session = await GetSessionAsync(sessionId);

        var order = new Order()
        {
            SessionId = session!.SessionId,
            CreatedAt = DateTime.Now,
            Status = Order.OrderStatus.Received
        };
        
        if(orderItems.Count == 0) return false;
        foreach (var pair in orderItems)
        {
            if (pair.Quantity > 10) return false;
            var item = await context.Items.FirstOrDefaultAsync(x => x.ItemId == pair.ItemId);
            if (item is null) return false;
            if (!item.IsAvailable) return false;
            order.OrderItems.Add(new()
            {
                Item = item,
                Quantity = pair.Quantity
            });
        }
        
        try
        {
            await context.Orders.AddAsync(order);
            await context.SaveChangesAsync();
        }
        catch (Exception e)
        {
            _logger.LogError("MakeOrderAsync exception: " + e.Message);
            return false;
        }
        
        _broker.Publish(new TableEventArgs()
        {
            TableId = session.TableId,
            EventType = TableEventArgs.TableEventType.Order,
            Data = new OrderEventArgs()
            {
                OrderId = order.OrderId,
                EventType = OrderEventType.Created
            }
        });
        return true;
    }

    public async Task<bool> ChangeOrderStatusAsync(int orderId, Order.OrderStatus status)
    {
        await using var context = await _factory.CreateDbContextAsync();
        var order = await context.Orders
            .Include(x => x.Session)
            .ThenInclude(x => x!.Table)
            .FirstOrDefaultAsync(x => x.OrderId == orderId);

        if (order is null) return false;
        order.Status = status;

        try
        {
            await context.SaveChangesAsync();
            _broker.Publish(new TableEventArgs()
            {
                EventType = TableEventArgs.TableEventType.Order,
                TableId = order.Session!.Table.TableId,
                Data = new OrderEventArgs()
                {
                    EventType = status == Order.OrderStatus.Completed ? OrderEventType.Completed : OrderEventType.Cancelled,
                    OrderId = order.OrderId
                }
            });
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError("ChangeOrderStatusAsync exception: " + e.Message);
            return false;
        }
        
    }

    private string _generateAuthCode()
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        Span<byte> bytes = stackalloc byte[16];
        Guid.NewGuid().TryWriteBytes(bytes);

        char[] code = new char[4];
        for (int i = 0; i < code.Length; i++)
            code[i] = alphabet[bytes[i] % alphabet.Length];
        return new string(code);
    }
    
    public async Task<TableSession?> CreateSessionAsync(int tableId)
    {
        await using var context = await _factory.CreateDbContextAsync();
        if (!await context.Tables.AnyAsync(x => x.TableId == tableId))
        {
            _logger.LogWarning("tableId가 올바르지 않습니다: {table}", tableId);
            return null;
        }
        
        if(await context.Sessions.AnyAsync(x => x.TableId == tableId && x.State != TableSession.SessionState.Completed))
        {
            _logger.LogWarning("완료되지 않은 세션이 있습니다: {table}", tableId);
            return null;
        }

        var authCode = _generateAuthCode();
        while(context.Sessions.Any(x => x.AuthCode == authCode))
            authCode = _generateAuthCode();
        
        var session = new TableSession()
        {
            AuthCode = authCode,
            CreatedAt = DateTime.Now,
            State = TableSession.SessionState.Active,
            TableId = tableId
        };
        await context.Sessions.AddAsync(session);
        await context.SaveChangesAsync();
        return session;
    }
    
    public async Task<List<Table>> GetAllTablesAsync()
    {
        await using var context = await _factory.CreateDbContextAsync();
        return await context.Tables.AsNoTracking().Include(t => t.Cell).ToListAsync();
    }
    public async Task<TableSession?> GetActiveSessionByTableIdAsync(int tableId)
    {
        await using var context = await _factory.CreateDbContextAsync();
        return await context.Sessions
            .AsNoTracking()
            .Where(s => s.TableId == tableId && s.EndedAt == null) // 해당 테이블의 활성 세션만 조회
            .Include(s => s.Table)
            .Include(s => s.Orders)
            .ThenInclude(o => o.OrderItems)
            .ThenInclude(oi => oi.Item)
            .FirstOrDefaultAsync();
    }
    
}