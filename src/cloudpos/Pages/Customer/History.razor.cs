using CloudInteractive.CloudPos.Components.Modal;
using CloudInteractive.CloudPos.Contexts;
using CloudInteractive.CloudPos.Event;
using CloudInteractive.CloudPos.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace CloudInteractive.CloudPos.Pages.Customer;

public partial class History(IDbContextFactory<ServerDbContext> factory, InteractiveInteropService interop, ModalService modal, ConfigurationService config, TableService table, TableEventBroker broker) : ComponentBase, IDisposable
{
    private int _totalOrderCount = 0;
    private int _totalAmount = 0;
    private List<Models.Order>? _orders;
    private Models.TableSession? _session;
    private string CurrencyFormat(int x) =>  x == 0 ? "￦0": $"￦{x:#,###}";
    private async Task UpdateTotalAsync()
    {
        await using var context = await factory.CreateDbContextAsync();
        var orders = context.Orders.Where(x => x.SessionId == _session!.SessionId)
            .Include(x => x.OrderItems)
            .ThenInclude(x => x.Item)
            .OrderByDescending(x => x.CreatedAt);
        _totalOrderCount = await orders.CountAsync();
        _orders = await orders.ToListAsync();
        
        _totalAmount = await orders.Where(x => x.Status != Models.Order.OrderStatus.Cancelled)
            .SumAsync(x => x.OrderItems.Sum(y => y.Quantity * y.Item.Price));
    }

    private async Task OnSessionEndBtnClickAsync()
    {
        if (await modal.ShowAsync<AlertModal, bool>("결제 요청하기", ModalService.Params()
                .Add("InnerHtml", "정말 결제 요청을 하시겠습니까?<br>계산 요청을 하면 더 이상 주문을 할 수 없습니다.")
                .Add("IsCancelable", true)
                .Build()))
        {
            if (!await table.EndSessionAsync())
            {
                _ = interop.ShowNotifyAsync("미완료 주문으로 인해 결제 요청을 할 수 없습니다.", InteractiveInteropService.NotifyType.Error);
            }
        }
    }

    protected override async Task OnInitializedAsync()
    {
        _session = await table.GetSessionAsync();
        broker.Subscribe(_session!.TableId, OnTableEvent);
        await UpdateTotalAsync();
    }
    public void Dispose() => broker.Unsubscribe(_session!.TableId, OnTableEvent);
    private void OnTableEvent(object? sender, TableEventArgs e)
    {
        if (e.EventType == TableEventArgs.TableEventType.Order) StateHasChanged();
    }
}