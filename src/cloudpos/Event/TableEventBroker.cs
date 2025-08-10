using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace CloudInteractive.CloudPos.Event;

public class TableEventBroker(ILogger<TableEventBroker> logger)
{
    public const int BroadcastId = -1;
    private readonly ConcurrentDictionary<int, ImmutableArray<(EventHandler<TableEventArgs> h, SynchronizationContext ctx)>> _subs = new();

    public void Subscribe(int tableId, EventHandler<TableEventArgs> handler)
    {
        var pair = (handler, SynchronizationContext.Current ?? new SynchronizationContext());
        _subs.AddOrUpdate(
            tableId,
            _ => [pair],              
            (_, arr) => arr.Add(pair)
        );
    }

    public void Unsubscribe(int tableId, EventHandler<TableEventArgs> handler)
    {
        _subs.AddOrUpdate(
            tableId,
            _ => ImmutableArray<(EventHandler<TableEventArgs>, SynchronizationContext)>.Empty,
            (_, arr) => arr.RemoveAll(p => p.h == handler) 
        );
    }

    public void Publish(TableEventArgs arg)
    {
        logger.LogDebug($"Publish (key={arg.TableId},type={arg.EventType},data={arg.Data?.ToString() ?? "null"})");
        if (_subs.TryGetValue(arg.TableId, out var list))
            foreach (var (h, ctx) in list)
                ctx.Post(_ => h(this, arg), null); 
        
        if (_subs.TryGetValue(BroadcastId, out var all))
            foreach (var (h, ctx) in all)
                ctx.Post(_ => h(this, arg), null);  
    }
    
    public void Broadcast(TableEventArgs arg)
    {
        logger.LogDebug($"Broadcast (key={arg.TableId},type={arg.EventType},data={arg.Data?.ToString() ?? "null"})");
        foreach (var t in _subs.Values)
            foreach(var s in t)
                s.ctx.Post(_ => s.h(this, arg), null);
    }
    
}


