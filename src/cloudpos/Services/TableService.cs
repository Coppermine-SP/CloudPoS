using CloudInteractive.CloudPos.Contexts;
using CloudInteractive.CloudPos.Event;
using CloudInteractive.CloudPos.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace CloudInteractive.CloudPos.Services;

public class TableService(
    IDbContextFactory<ServerDbContext> factory,
    TableEventBroker broker,
    IAuthorizationHandler handler,
    ILogger<TableService> logger)
{

    private AuthorizationHandler AuthHandler => (AuthorizationHandler)handler;

    public async Task<TableSession?> GetSessionAsync(int? sessionId = null)
    {
        await using var context = await factory.CreateDbContextAsync();
        return sessionId is null
            ? AuthHandler.Session
            : await context.Sessions.AsNoTracking().Include(x => x.Table).FirstOrDefaultAsync(x => x.SessionId == sessionId);
    }
    
    public async Task<bool> CompleteSessionAsync(int sessionId)
    {
        await using var context = await factory.CreateDbContextAsync();
        var session = await context.Sessions.FirstOrDefaultAsync(s => s.SessionId == sessionId);
        if (session is null)
        {
            logger.LogWarning("존재하지 않는 세션을 완료하려고 시도했습니다: {SessionId}", sessionId);
            return false;
        }

        session.State = TableSession.SessionState.Completed;
        session.EndedAt ??= DateTime.Now;
        await context.SaveChangesAsync();
        return true;
    }
        
    public async Task<bool> EndSessionAsync(int? sessionId = null)
    {
        await using var context = await factory.CreateDbContextAsync();

        var targetSessionId = sessionId ?? (await GetSessionAsync())?.SessionId;
        if (targetSessionId is null)
        {
            logger.LogWarning("EndSessionAsync가 유효한 세션 ID 없이 호출되었습니다.");
            return false;
        }
        
        var session = await context.Sessions.FirstOrDefaultAsync(s => s.SessionId == targetSessionId.Value);
        if (session is null)
        {
            logger.LogWarning("존재하지 않는 세션을 종료하려고 시도했습니다: {SessionId}", targetSessionId.Value);
            return false;
        }
        
        if (await context.Orders.AnyAsync(x => x.SessionId == session.SessionId && x.Status == Order.OrderStatus.Received))
        {
            logger.LogInformation("세션 {SessionId}에 처리되지 않은 주문이 있어 종료할 수 없습니다.", session.SessionId);
            return false;
        }
        
        session.EndedAt = DateTime.Now;
        session.State = TableSession.SessionState.Billing;
        session.AuthCode = null;
        await context.SaveChangesAsync();
        AuthHandler.ClearSessionCache(session.SessionId);
        broker.Publish(new TableEventArgs()
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
        await using var context = await factory.CreateDbContextAsync();
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
        await using var context = await factory.CreateDbContextAsync();
        var session = await GetSessionAsync(sessionId);

        if (session is null) return false;
        var order = new Order()
        {
            SessionId = session.SessionId,
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
            logger.LogError("MakeOrderAsync exception: " + e.Message);
            return false;
        }

        order.Session = session;
        broker.Publish(new TableEventArgs()
        {
            TableId = session!.TableId,
            EventType = TableEventArgs.TableEventType.Order,
            Data = new OrderEventArgs()
            {
                Order = order,
                EventType = OrderEventType.Created
            }
        });
        return true;
    }

    public async Task<bool> UpdateOrderMemoAsync(int orderId, string memo)
    {
        await using var context = await factory.CreateDbContextAsync();
        var order = await context.Orders
            .FirstOrDefaultAsync(x => x.OrderId == orderId);

        if (order is null) return false;
        if(order.Status is not Order.OrderStatus.Received) return false;
        order.Memo = memo;
        try
        {
            await context.SaveChangesAsync();
            broker.Broadcast(new TableEventArgs()
            {
                EventType = TableEventArgs.TableEventType.Order,
                TableId = TableEventBroker.BroadcastId,
                Data = new OrderEventArgs()
                {
                    Order = order,
                    EventType = OrderEventType.MemoUpdated
                }
            });
        }
        catch (Exception e)
        {
            logger.LogError("UpdateOrderMemoAsync exception: " + e.Message);
            return false;
        }
        return true;
    }

    public async Task<bool> ChangeOrderStatusAsync(int orderId, Order.OrderStatus status)
    {
        await using var context = await factory.CreateDbContextAsync();
        var order = await context.Orders
            .Include(x => x.OrderItems)
            .ThenInclude(x => x.Item)
            .Include(x => x.Session)
            .ThenInclude(x => x!.Table)
            .FirstOrDefaultAsync(x => x.OrderId == orderId);

        if (order is null) return false;
        order.Status = status;

        try
        {
            await context.SaveChangesAsync();
            broker.Publish(new TableEventArgs()
            {
                EventType = TableEventArgs.TableEventType.Order,
                TableId = order.Session!.Table.TableId,
                Data = new OrderEventArgs()
                {
                    EventType = status == Order.OrderStatus.Completed ? OrderEventType.Completed : OrderEventType.Cancelled,
                    Order = order
                }
            });
            return true;
        }
        catch (Exception e)
        {
            logger.LogError("ChangeOrderStatusAsync exception: " + e.Message);
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

    public async Task<bool> MoveSessionAsync(int sessionId, int destTableId)
    {
        await using var context = await factory.CreateDbContextAsync();
        var table = await context.Tables
            .Include(x => x.Sessions)
            .FirstOrDefaultAsync(x => x.TableId == destTableId);
        var session = await context.Sessions.FirstOrDefaultAsync(x => x.SessionId == sessionId);
        if (table is null)
        {
            logger.LogWarning("destTableId가 올바르지 않습니다: {table}", destTableId);
            return false;
        }

        if (session is null)
        {
            logger.LogWarning("sessionId가 올바르지 않습니다: {session}", sessionId);
            return false;
        }

        if (session.State != TableSession.SessionState.Active)
        {
            logger.LogWarning("세션이 올바른 상태가 아닙니다: {session}", sessionId);
            return false;
        }

        if (table.Sessions.Any(x => x.State == TableSession.SessionState.Active))
        {
            logger.LogWarning("이동하려는 테이블에 이미 활성 세션이 있습니다: {table}", destTableId);
            return false;
        }

        session.TableId = destTableId;
        await context.SaveChangesAsync();
        return true;
    }
    
    public async Task<TableSession?> CreateSessionAsync(int tableId)
    {
        await using var context = await factory.CreateDbContextAsync();
        if (!await context.Tables.AnyAsync(x => x.TableId == tableId))
        {
            logger.LogWarning("tableId가 올바르지 않습니다: {table}", tableId);
            return null;
        }
        
        if(await context.Sessions.AnyAsync(x => x.TableId == tableId && x.State != TableSession.SessionState.Completed))
        {
            logger.LogWarning("완료되지 않은 세션이 있습니다: {table}", tableId);
            return null;
        }

        var authCode = _generateAuthCode();
        while(await context.Sessions.AnyAsync(x => x.AuthCode == authCode))
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
    
}