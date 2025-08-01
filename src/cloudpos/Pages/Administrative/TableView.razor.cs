using CloudInteractive.CloudPos.Models;
using CloudInteractive.CloudPos.Services;
using Microsoft.AspNetCore.Components;

namespace CloudInteractive.CloudPos.Pages.Administrative;

public partial class TableView(TableService tableService, ModalService modal, ConfigurationService config) : ComponentBase
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
        _tables = await tableService.GetAllTablesAsync();
        StateHasChanged();
    }

    private async Task GetTableSessionAsync(int tableId)
    {
        _selectedTableSession = await tableService.GetActiveSessionByTableIdAsync(tableId);
        _selectedTable = _selectedTableSession?.Table;
    }
}