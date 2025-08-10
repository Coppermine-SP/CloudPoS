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
    private List<TableSession>? _sessions;
    private List<Table>? _tables;

    protected override async Task OnInitializedAsync()
    {
        broker.Subscribe(TableEventBroker.BroadcastId, OnTableEvent);
        _sessions = await GetSessionsAsync();
        _tables = await GetTablesAsync();
    }

    private async Task<List<TableSession>> GetSessionsAsync()
    {
        await using var context = await factory.CreateDbContextAsync();
        return await context.Sessions
            .Where(x => x.State != TableSession.SessionState.Completed)
            .AsNoTracking()
            .ToListAsync();
    }

    private async Task<List<Table>> GetTablesAsync()
    {
        await using var context = await factory.CreateDbContextAsync();
        return await context.Tables
            .Include(x => x.Cell)
            .AsNoTracking()
            .ToListAsync();
    }
    
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _tableViewModule = await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/showOffcanvas.js");
        }
    }
    private async void OnTableEvent(object? sender, TableEventArgs e)
    {
        if (e.EventType == TableEventArgs.TableEventType.TableUpdate)
        {
            _sessions = await GetSessionsAsync();
            _tables = await GetTablesAsync();
            StateHasChanged();
        }
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