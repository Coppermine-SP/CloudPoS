using System.Security.Claims;
using CloudInteractive.CloudPos.Contexts;
using CloudInteractive.CloudPos.Event;
using CloudInteractive.CloudPos.Models;
using CloudInteractive.CloudPos.Pages.Shared;
using Microsoft.EntityFrameworkCore;

namespace CloudInteractive.CloudPos.Services;

public class TableService
{
    private readonly ServerDbContext _context;
    private readonly TableEventBroker _eventBroker;
    private readonly IHttpContextAccessor _accessor;
    private TableSession? _session;

    public TableService(ServerDbContext context, TableEventBroker broker, IHttpContextAccessor accessor)
    {
        _context = context;
        _eventBroker = broker;
        _accessor = accessor;
    }
    
    public enum ValidateResult { Ok, Unauthorized, InvalidSessionId, SessionExpired }
    public ValidateResult ValidateSession(bool isAdmin)
    {
        var sessionId = _accessor.HttpContext?.User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Sid);
        var role = _accessor.HttpContext?.User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Role);
        
        if((role == null) || (isAdmin && role?.Value != Pages.Administrative.Authorize.AdminRole) || (!isAdmin && ((role?.Value != Pages.Customer.Authorize.CustomerRole) || sessionId == null)))
            return ValidateResult.Unauthorized;

        if (isAdmin) return ValidateResult.Ok;
        _session = _context.Sessions
            .Include(x => x.Table).
            FirstOrDefault(x => x.SessionId == Convert.ToInt32(sessionId.Value));
        
        if(_session is null) return ValidateResult.InvalidSessionId;
        if(_session.EndedAt is not null && _session.IsPaymentCompleted) return ValidateResult.SessionExpired;
        
        return ValidateResult.Ok;
    }
    
    public TableSession? GetSession(int? sessionId = null) 
        => sessionId is null ? _session :  _context.Sessions.First(x => x.SessionId == sessionId);
    
    public async Task<bool> EndSessionAsync(int? sessionId = null)
    {
        var session = GetSession(sessionId);
        if (_context.Orders.Any(x => x.SessionId == session!.SessionId && x.Status == Order.OrderStatus.Received))
            return false;
        
        session!.EndedAt = DateTime.Now;
        session.IsPaymentCompleted = false;
        await _context.SaveChangesAsync();
        await _eventBroker.PublishAsync(new TableEventArgs()
        {
            TableId = sessionId ?? _session!.TableId,
            EventType = TableEventArgs.TableEventType.SessionEnd
        });

        return true;
    }
    
    public record OrderSummary(int ItemId, string ItemName, int Price, int TotalQty, int TotalPrice);
    public async Task<List<OrderSummary>> SessionOrderSummaryAsync()
    {
        return await _context.Orders
            .Where(x => x.SessionId == _session!.SessionId)
            .Where(x => x.Status == Order.OrderStatus.Completed)
            .SelectMany(x => x.OrderItems)
            .GroupBy(x => x.ItemId)
            .Select(x => 
                new OrderSummary(x.Key, x.First().Item.Name, x.First().Item.Price, x.Sum(y => y.Quantity), x.Sum(y => y.Quantity * y.Item.Price)))
            .ToListAsync();
    }
}