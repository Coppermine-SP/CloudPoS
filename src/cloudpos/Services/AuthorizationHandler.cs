using System.Security.Claims;
using CloudInteractive.CloudPos.Contexts;
using CloudInteractive.CloudPos.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace CloudInteractive.CloudPos.Services;

public class SessionValidateRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// true로 설정하면, 종료된 세션(EndedAt is not null)을 요구합니다.
    /// false(기본값)로 설정하면, 활성 세션(EndedAt is null)을 요구합니다.
    /// </summary>
    public bool RequireEndedSession { get; init; }
}

public class AuthorizationHandler : AuthorizationHandler<SessionValidateRequirement>
{
    public const string ActiveSessionPolicy = "ActiveSessionPolicy";
    public const string EndedSessionPolicy = "EndedSessionPolicy";
    public const string AdminRole = "admin";
    public const string CustomerRole = "customer";
    
    public TableSession? Session { get; private set; }
    
    private readonly IDbContextFactory<ServerDbContext> _factory;
    private readonly IMemoryCache _cache;
    private readonly ILogger _logger;
    private record SessionCacheEntry(TableSession Session);
    public AuthorizationHandler(IDbContextFactory<ServerDbContext> factory, IMemoryCache cache, ILogger<AuthorizationHandler> logger)
    {
        _factory = factory;
        _cache = cache;
        _logger = logger;
    }

    private string GetCacheKey(int sessionId) => $"session-status-{sessionId}";

    public void ClearSessionCache(int sessionId)
    {
        _cache.Remove(GetCacheKey(sessionId));
        _logger.LogInformation($"Session cache cleared. (key={GetCacheKey(sessionId)})");
    }
    
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context,
        SessionValidateRequirement requirement)
    {
        var claim = context.User.FindFirst(ClaimTypes.Sid);
        if (claim is null)
        {
            _logger.LogInformation($"Authentication failed. (claim was null)");
            context.Fail();
            return;
        }
        
        var sessionId = Convert.ToInt32(claim.Value);
        var cacheKey = GetCacheKey(sessionId);

        if (!_cache.TryGetValue(cacheKey, out SessionCacheEntry? cacheEntry))
        {
            await using var dbContext = await _factory.CreateDbContextAsync();
            var session = await dbContext.Sessions
                .AsNoTracking()
                .Include(x => x.Table)
                .FirstOrDefaultAsync(x => x.SessionId == sessionId);

            if (session is null)
            {
                _logger.LogInformation($"Authentication failed. (session was null)");
                context.Fail();
                return;
            }
            
            cacheEntry = new SessionCacheEntry(Session: session);
            _cache.Set(cacheKey, cacheEntry, TimeSpan.FromSeconds(30));
            _logger.LogInformation($"Session cache set. (key={cacheKey})");
        }

        if (cacheEntry is null)
        {
            _logger.LogInformation($"Authentication failed. (cacheEntry was null)");
            context.Fail();
            return;
        }

        bool isSessionEnded =
            cacheEntry.Session.State is TableSession.SessionState.Billing and not TableSession.SessionState.Completed;
        bool isAuthorized = requirement.RequireEndedSession ? isSessionEnded : !isSessionEnded;
        if (isAuthorized)
        {
            _logger.LogInformation($"Authentication succeeded. (RequireEndedSession={requirement.RequireEndedSession},sessionId={sessionId})");
            Session = cacheEntry.Session;
            context.Succeed(requirement);
        }
        else
        {
            _logger.LogInformation($"Authentication failed. (RequireEndedSession={requirement.RequireEndedSession},sessionId={sessionId})");
            Session = null;
            context.Fail();
        }
    }
}