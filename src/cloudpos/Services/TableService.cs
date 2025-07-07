using System.Security.Claims;
using CloudInteractive.CloudPos.Contexts;
using CloudInteractive.CloudPos.Event;
using CloudInteractive.CloudPos.Models;
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
        
        if((sessionId == null || role == null) || (isAdmin && role?.Value != "admin") || (!isAdmin && role?.Value != "customer"))
            return ValidateResult.Unauthorized;
        
        _session = _context.Sessions
            .Include(x => x.Table).
            FirstOrDefault(x => x.SessionId == Convert.ToInt32(sessionId.Value));
        
        if(_session is null) return ValidateResult.InvalidSessionId;
        if(_session.EndedAt is not null && _session.IsPaymentCompleted) return ValidateResult.SessionExpired;
        
        return ValidateResult.Ok;
    }
    
    public TableSession? GetSession() => _session;
    
    public async Task EndSessionAsync()
    {
        _session!.EndedAt = DateTime.Now;
        _session.IsPaymentCompleted = true;
        await _context.SaveChangesAsync();
        await _eventBroker.PublishAsync(new TableEventArgs()
        {
            TableId = _session!.TableId,
            EventType = TableEventArgs.TableEventType.SessionEnd
        });
    }
}