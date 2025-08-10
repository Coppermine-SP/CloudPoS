using CloudInteractive.CloudPos.Services;
using Microsoft.AspNetCore.Components;

namespace CloudInteractive.CloudPos.Pages.Customer;

public partial class Receipt(ConfigurationService config, TableService table) : ComponentBase
{
    private int? _sessionId;

    protected override async Task OnInitializedAsync()
    {
        _sessionId = (await table.GetSessionAsync())!.SessionId;
    }
}