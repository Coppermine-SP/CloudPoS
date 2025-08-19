using CloudInteractive.CloudPos.Contexts;
using CloudInteractive.CloudPos.Event;
using CloudInteractive.CloudPos.Models;
using CloudInteractive.CloudPos.Services;
using CloudInteractive.CloudPos.Services.Debounce;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace CloudInteractive.CloudPos.Pages.Administrative;

public partial class TableView(TableService tableService, ConfigurationService config, IDbContextFactory<ServerDbContext> factory, TableEventBroker broker, InteractiveInteropService interop, IDebounceService debounce) : ComponentBase, IAsyncDisposable
{
    private int? _selectedTableId;
    private TableSession? _selectedTableSession;
    private List<TableSession>? _sessions;
    private List<Table>? _tables;
    private IDebouncedTask? _refreshTask;
    private bool _disposed;

    protected override async Task OnInitializedAsync()
    {
        broker.Subscribe(TableEventBroker.BroadcastId, OnTableEvent);
        var policy = new DebouncePolicy(
            Debounce: TimeSpan.FromMilliseconds(1000),
            MaxInterval: TimeSpan.FromMilliseconds(3000));
        
        _refreshTask = debounce.Create(policy, async ct =>
        {
            await using var context = await factory.CreateDbContextAsync(ct);
            
            _sessions = await context.Sessions
                .Where(x => x.State != TableSession.SessionState.Completed)
                .AsNoTracking()
                .ToListAsync(ct);
        
            _tables = await context.Tables
                .Include(x => x.Cell)
                .AsNoTracking()
                .ToListAsync(ct);
            
            if (!_disposed)
                await InvokeAsync(StateHasChanged);
        });

        await _refreshTask.TriggerNowAsync();
    }
    
    private void OnTableEvent(object? sender, TableEventArgs e)
    {
        if (e.EventType == TableEventArgs.TableEventType.TableUpdate)
            _refreshTask?.Request();
    }
    
    private async Task SetTableSessionAsync(int tableId)
    {
        _selectedTableId = tableId;
        await using var context = await factory.CreateDbContextAsync();
        _selectedTableSession = await context.Sessions
            .AsNoTracking()
            .Where(s => s.TableId == tableId && s.State != TableSession.SessionState.Completed)
            .Include(s => s.Table)
            .FirstOrDefaultAsync();

        await interop.ShowOffCanvasAsync();
        StateHasChanged();
    }

    private async Task OnCreateSessionClickAsync(int tableId)
    {
        await using var context = await factory.CreateDbContextAsync();
        var newSession = await tableService.CreateSessionAsync(tableId);
        if (newSession != null)
            _selectedTableSession = await context.Sessions
                .AsNoTracking()
                .Include(s => s.Table)
                .Include(s => s.Orders)
                .ThenInclude(o => o.OrderItems)
                .ThenInclude(oi => oi.Item)
                .FirstOrDefaultAsync(s => s.SessionId == newSession.SessionId);
        StateHasChanged();
        broker.Publish(new TableEventArgs()
        {
            TableId = TableEventBroker.BroadcastId,
            EventType = TableEventArgs.TableEventType.TableUpdate
        });
    }

    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        broker.Unsubscribe(TableEventBroker.BroadcastId, OnTableEvent);
        if (_refreshTask is not null) await _refreshTask.DisposeAsync();
    }
}