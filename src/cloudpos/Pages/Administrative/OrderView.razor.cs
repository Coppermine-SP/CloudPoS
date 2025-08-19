using CloudInteractive.CloudPos.Components.Modal;
using CloudInteractive.CloudPos.Contexts;
using CloudInteractive.CloudPos.Event;
using CloudInteractive.CloudPos.Models;
using CloudInteractive.CloudPos.Services;
using CloudInteractive.CloudPos.Services.Debounce;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace CloudInteractive.CloudPos.Pages.Administrative;

public partial class OrderView(IDbContextFactory<ServerDbContext> factory, TableEventBroker broker, ModalService modal, TableService table, InteractiveInteropService interop, IDebounceService debounce) : ComponentBase, IAsyncDisposable
{
    private string _memoContent = string.Empty;
    private bool _shouldUpdateMemo;
    private int? _selectedOrderId;
    private List<Order>? _activeOrders;
    private IDebouncedTask? _refreshTask;
    private bool _disposed;
    
    private string CurrencyFormat(int x) => $"{x:₩#,###}";
    
    protected override async Task OnInitializedAsync()
    {
        broker.Subscribe(TableEventBroker.BroadcastId, OnBroadcastEvent);
        var policy = new DebouncePolicy(
            Debounce: TimeSpan.FromMilliseconds(500),
            MaxInterval: TimeSpan.FromMilliseconds(2000));

        _refreshTask = debounce.Create(policy, async ct =>
        {
            await using var context = await factory.CreateDbContextAsync(ct);
            _activeOrders = await context.Orders
                .Where(x => x.Status == Order.OrderStatus.Received)
                .Include(x => x.Session)
                .ThenInclude(x => x!.Table)
                .Include(x => x.OrderItems)
                .ThenInclude(x => x.Item)
                .OrderBy(x => x.CreatedAt)
                .AsNoTracking()
                .ToListAsync(cancellationToken: ct);

            if (!_disposed)
                await InvokeAsync(StateHasChanged);
        });

        await _refreshTask.TriggerNowAsync();
    }
    
    private void OnBroadcastEvent(object? sender, TableEventArgs e)
    {
        if (e.EventType == TableEventArgs.TableEventType.Order)
        {
            if(e.Data is not OrderEventArgs args) return;
            if(_selectedOrderId is not null && args.Order.OrderId == _selectedOrderId && args.EventType is OrderEventType.MemoUpdated)
                _shouldUpdateMemo = true;
        }

        _refreshTask?.Request();
    }

    private async Task OnCompleteOrderAsync()
    {
        if (_selectedOrderId is null) return;
        if (!await modal.ShowAsync<AlertModal, bool>("주문 완료", ModalService.Params()
                .Add("IsCancelable", true)
                .Add("InnerHtml", $"정말 주문 #{_selectedOrderId}를 완료하시겠습니까?")
                .Build())) return;

        if (_selectedOrderId is null) return;
        if (!await table.ChangeOrderStatusAsync(_selectedOrderId.Value, Order.OrderStatus.Completed))
            _ =interop.ShowNotifyAsync("서버 오류가 발생하여 주문 상태를 변경하지 못했습니다.", InteractiveInteropService.NotifyType.Error);

        _selectedOrderId = null;
        StateHasChanged();
    }

    private async Task OnUpdateMemoAsync()
    {
        if (!await table.UpdateOrderMemoAsync(_selectedOrderId!.Value, _memoContent))
            _ = interop.ShowNotifyAsync("서버 오류가 발생하여 주문 메모를 변경하지 못했습니다.", InteractiveInteropService.NotifyType.Error);
        
        _shouldUpdateMemo = true;
        StateHasChanged();
    }
    
    private async Task OnCancelOrderAsync()
    {
        if (_selectedOrderId is null) return;
        if (!await modal.ShowAsync<AlertModal, bool>("주문 취소", ModalService.Params()
                .Add("IsCancelable", true)
                .Add("InnerHtml", $"정말 주문 #{_selectedOrderId}를 취소하시겠습니까?")
                .Build())) return;

        if (_selectedOrderId is null) return;
        if (!await table.ChangeOrderStatusAsync(_selectedOrderId.Value, Order.OrderStatus.Cancelled))
            _ =interop.ShowNotifyAsync("서버 오류가 발생하여 주문 상태를 변경하지 못했습니다.", InteractiveInteropService.NotifyType.Error);

        _selectedOrderId = null;
    }

    private async Task SetOrderAsync(Order o)
    {
        _selectedOrderId = o.OrderId;
        _shouldUpdateMemo = true;
        StateHasChanged();
        
        await interop.ShowOffCanvasAsync();
    }
    
    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        broker.Unsubscribe(TableEventBroker.BroadcastId, OnBroadcastEvent);
        if (_refreshTask is not null) await _refreshTask.DisposeAsync();
    }
}