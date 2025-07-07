using CloudInteractive.CloudPos.Contexts;
using CloudInteractive.CloudPos.Event;
using CloudInteractive.CloudPos.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace CloudInteractive.CloudPos.Pages.Customer;

public partial class History(ServerDbContext context, InteractiveInteropService interop, TableEventBroker broker, ConfigurationService config, TableService table) : ComponentBase
{

    protected override void OnParametersSet()
    {
        UpdateTotal();
    }
    
    private int _totalOrderCount = 0;
    private int _totalAmount = 0;
    private List<Models.Order>? _orders;
    private string CurrencyFormat(int x) => string.Format("￦{0:#,###}", x);
    private void UpdateTotal()
    {
        var orders = context.Orders.Where(x => x.SessionId == table.GetSession()!.SessionId)
            .Include(x => x.OrderItems)
            .ThenInclude(x => x.Item);
        _totalOrderCount = orders.Count();
        _orders = orders.ToList();
        
        _totalAmount = orders.Where(x => x.Status != Models.Order.OrderStatus.Cancelled)
            .Sum(x => x.OrderItems.Sum(y => y.Quantity * y.Item.Price));
    }

    private async Task OnSessionEndBtnClickAsync()
    {
        if (await interop.ShowModalAsync("계산 요청", "정말 계산 요청을 하시겠습니까?<br>계산 요청을 하면 더 이상 주문을 할 수 없습니다.", true))
        {
            await table.EndSessionAsync();
        }
    }


}