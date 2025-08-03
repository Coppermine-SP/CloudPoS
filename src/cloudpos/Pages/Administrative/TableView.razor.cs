using CloudInteractive.CloudPos.Contexts;
using CloudInteractive.CloudPos.Models;
using CloudInteractive.CloudPos.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace CloudInteractive.CloudPos.Pages.Administrative;

public partial class TableView(TableService tableService, ConfigurationService config, IDbContextFactory<ServerDbContext> factory) : ComponentBase
{
    private List<Table> _tables = [];
    private Table? _selectedTable;
    private TableSession? _selectedTableSession;
    
    protected override async Task OnInitializedAsync()
    {
        await LoadTablesAsync();
    }

    private async Task LoadTablesAsync()
    {
        await using var context = await factory.CreateDbContextAsync();
        _tables = await context.Tables.AsNoTracking().Include(t => t.Cell).ToListAsync();
        StateHasChanged();
    }

    private async Task GetTableSessionAsync(int tableId)
    {
        await using var context = await factory.CreateDbContextAsync();
        _selectedTable = _tables.FirstOrDefault(t => t.TableId == tableId);
        _selectedTableSession = await context.Sessions
            .AsNoTracking()
            .Where(s => s.TableId == tableId && s.State != TableSession.SessionState.Completed)
            .Include(s => s.Table)
            .Include(s => s.Orders)
            .ThenInclude(o => o.OrderItems)
            .ThenInclude(oi => oi.Item)
            .FirstOrDefaultAsync();;
        StateHasChanged();
    }

    private async Task OnCreateSessionClickAsync(int tableId)
    {
        await using var context = await factory.CreateDbContextAsync();
        var s = await tableService.CreateSessionAsync(tableId);
        if (s != null)
            _selectedTableSession = await context.Sessions.FirstOrDefaultAsync(x => x.SessionId == s.SessionId);
        StateHasChanged();
    }
}