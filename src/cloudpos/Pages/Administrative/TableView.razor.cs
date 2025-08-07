using CloudInteractive.CloudPos.Contexts;
using CloudInteractive.CloudPos.Event;
using CloudInteractive.CloudPos.Models;
using CloudInteractive.CloudPos.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;

namespace CloudInteractive.CloudPos.Pages.Administrative;

public partial class TableView(TableService tableService, ConfigurationService config, IDbContextFactory<ServerDbContext> factory, TableEventBroker broker) : ComponentBase, IDisposable
{
    [Inject] private IJSRuntime JsRuntime { get; set; } = null!;
    private IJSObjectReference? _tableViewModule;
    private int? _selectedTableId;
    private TableSession? _selectedTableSession;
    private ServerDbContext? _context;
    private ServerDbContext GetContext() => _context ??= factory.CreateDbContext();

    protected override void OnInitialized() => broker.Subscribe(TableEventBroker.BroadcastId, OnTableEvent);
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _tableViewModule = await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/showOffcanvas.js");
        }
    }
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
            .FirstOrDefaultAsync();
        
        if (_tableViewModule is not null)
            await _tableViewModule.InvokeVoidAsync("showOffcanvas", "offcanvasResponsive");
        
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
    public async ValueTask DisposeAsync()
    {
        if (_tableViewModule is not null)
        {
            await _tableViewModule.DisposeAsync();
        }
    }
}