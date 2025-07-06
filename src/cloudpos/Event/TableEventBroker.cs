using System.Collections.Concurrent;

namespace CloudInteractive.CloudPos.Event;

public class TableEventBroker(ILogger<TableEventBroker> logger)
{
    public const int BroadcastId = -1;
    private readonly ConcurrentDictionary<int, List<(EventHandler<TableEventArgs> h, SynchronizationContext ctx)>> _subs = new();

    public void Subscribe(int tableId, EventHandler<TableEventArgs> handler)
    {
        var pair = (handler, SynchronizationContext.Current ?? new SynchronizationContext());
        _subs.AddOrUpdate(tableId, _ => [pair], (_, list) =>
        {
            lock (list) list.Add(pair);
            logger.LogInformation($"Subscribe (key={tableId},count={list.Count})");
            return list;
        });
    }

    public void Unsubscribe(int tableId, EventHandler<TableEventArgs> handler)
    {
        if (_subs.TryGetValue(tableId, out var list))
        {
            lock (list) list.Remove(list.First(x => x.h == handler));
            logger.LogInformation($"Unsubscribe (key={tableId},count={list.Count})");
        }
    }

    public Task PublishAsync(TableEventArgs arg)
    {
        logger.LogDebug($"Publish (key={arg.TableId},type={arg.EventType},data={arg.Data?.ToString() ?? "null"})");
        if (_subs.TryGetValue(arg.TableId, out var list))
            foreach (var (h, ctx) in list)
                ctx.Post(_ => h(this, arg), null); 
        
        if (_subs.TryGetValue(BroadcastId, out var all))
            foreach (var (h, ctx) in all)
                ctx.Post(_ => h(this, arg), null);  
        
        return Task.CompletedTask;
    }
}


