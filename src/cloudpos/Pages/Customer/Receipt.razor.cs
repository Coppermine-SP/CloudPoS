using CloudInteractive.CloudPos.Services;
using Microsoft.AspNetCore.Components;

namespace CloudInteractive.CloudPos.Pages.Customer;

public partial class Receipt(TableService table, NavigationManager navigation, ConfigurationService config) : ComponentBase
{
    private List<TableService.OrderSummary>? _orderSummaries;
    private int _totalAmount = 0;
    
    private string CurrencyFormat(int x) => $"{x:#,###}";
    protected override async Task OnInitializedAsync()
    {
        var result = table.ValidateSession(false);
        if (result != TableService.ValidateResult.Ok)
        {
            navigation.NavigateTo("/Customer/Authorize?Error=0", replace:true, forceLoad:true);
            return;
        }

        if (table.GetSession()!.EndedAt is null)
        {
            navigation.NavigateTo("/Customer/Menu", replace:true, forceLoad: true);
            return;
        }

        _orderSummaries = await table.SessionOrderSummaryAsync();
        _totalAmount = _orderSummaries.Sum(x => x.TotalPrice);
        StateHasChanged();
    }
}