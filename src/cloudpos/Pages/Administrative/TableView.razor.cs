using CloudInteractive.CloudPos.Contexts;
using CloudInteractive.CloudPos.Event;
using CloudInteractive.CloudPos.Models;
using CloudInteractive.CloudPos.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace CloudInteractive.CloudPos.Pages.Administrative;

public partial class TableView(TableService tableService, ConfigurationService config, IDbContextFactory<ServerDbContext> factory, TableEventBroker broker) : ComponentBase, IDisposable
{
    private int? _selectedTableId;
    private TableSession? _selectedTableSession;
    private ServerDbContext? _context;
    private ServerDbContext GetContext() => _context ??= factory.CreateDbContext();

    protected override void OnInitialized() => broker.Subscribe(TableEventBroker.BroadcastId, OnTableEvent);
    private void OnTableEvent(object? sender, TableEventArgs e)
    {
        if(e.EventType == TableEventArgs.TableEventType.TableUpdate) StateHasChanged();
    }
    
    private async Task SetTableSessionAsync(int tableId)
    {
        _selectedTableId = tableId;
        await using var context = await factory.CreateDbContextAsync();
        _selectedTableSession = await context.Sessions
            .AsNoTracking()
            .Where(s => s.TableId == tableId && s.State != TableSession.SessionState.Completed)
            .Include(s => s.Table)
            .FirstOrDefaultAsync();;
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

    public void Dispose() => broker.Unsubscribe(TableEventBroker.BroadcastId, OnTableEvent);
}