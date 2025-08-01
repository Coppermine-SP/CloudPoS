using CloudInteractive.CloudPos.Components.Modal;
using CloudInteractive.CloudPos.Contexts;
using CloudInteractive.CloudPos.Event;
using CloudInteractive.CloudPos.Models;
using CloudInteractive.CloudPos.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace CloudInteractive.CloudPos.Pages.Administrative;

public partial class TableView(
    IDbContextFactory<ServerDbContext> factory, TableService service,
    TableEventBroker broker, ModalService modal, ConfigurationService config) : ComponentBase
{
    private ServerDbContext Context => factory.CreateDbContext();
    private List<Table> _tables = [];
    private Table? _selectedTable;
    private TableSession? _selectedTableSession;
    
    
    protected override async Task OnInitializedAsync()
    { 
        await LoadTablesAsync(); 
    }
    
    private async Task LoadTablesAsync()
    { 
        _tables = await Context.Tables.Include(t => t.Cell).ToListAsync();
        StateHasChanged();
    }

    private async Task GetTableSessionAsync(int tableId)
    {
        _selectedTable = await Context.Tables.FirstOrDefaultAsync(t => t.TableId == tableId);
        if (_selectedTable != null)
        {
            _selectedTableSession = await Context.Sessions
                .Include(s => s.Orders)
                .ThenInclude(o => o.OrderItems)
                .ThenInclude(oi => oi.Item)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.TableId == tableId);
        }
    }
    
}