using CloudInteractive.CloudPos.Components.Modal;
using CloudInteractive.CloudPos.Contexts;
using CloudInteractive.CloudPos.Event;
using CloudInteractive.CloudPos.Models;
using CloudInteractive.CloudPos.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace CloudInteractive.CloudPos.Components.TableView;

public partial class SessionManagement(ModalService modal, TableService service, InteractiveInteropService interop, IDbContextFactory<ServerDbContext> factory, TableEventBroker broker): ComponentBase, IDisposable
{
    [Parameter, EditorRequired] public int SessionId { get; set; }
    private TableSession? _session;

    private async Task UpdateTableSessionAsync()
    {
        await using var context = await factory.CreateDbContextAsync();
        _session = await context.Sessions
            .Include(x => x.Table)
            .Include(x => x.Orders)
            .ThenInclude(x => x.OrderItems)
            .ThenInclude(x => x.Item)
            .FirstAsync(x => x.SessionId == SessionId);
    }

    protected override void OnInitialized() => broker.Subscribe(TableEventBroker.BroadcastId, OnTableEvent);
    protected override async Task OnParametersSetAsync()
    {
        await UpdateTableSessionAsync();
        StateHasChanged();
    }

    private async void OnTableEvent(object? sender, TableEventArgs e)
    {
        try
        {
            if (e.EventType is TableEventArgs.TableEventType.TableUpdate or TableEventArgs.TableEventType.Order or TableEventArgs.TableEventType.SessionEnd)
            {
                await UpdateTableSessionAsync();
                StateHasChanged();
            }
        }
        catch (Exception ex)
        {
            throw; // TODO handle exception
        }
    }

    private int TotalAmount => _session!.Orders
        .Where(x => x.Status != Order.OrderStatus.Cancelled)
        .SelectMany(order => order.OrderItems)
        .Sum(item => item.Quantity * item.Item.Price);
    
    private string CurrencyFormat(int x) => x == 0 ? "￦0": $"￦{x:#,###}";

    private string SessionStateToString(TableSession.SessionState state) => state switch
    {
        TableSession.SessionState.Active => "활성",
        TableSession.SessionState.Billing => "완료",
        TableSession.SessionState.Completed => "종료",
        _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
    };
    
    private async Task ShowEndSessionAsyncModalAsync(int sessionId)
    {
        var confirm = await modal.ShowAsync<AlertModal, bool>(
            "세션 완료", ModalService.Params()
                .Add("InnerHtml", "정말로 세션 완료 처리를 하시겠습니까?<br>더 이상 사용자가 주문을 할 수 없게 됩니다.<br><strong>이 작업은 되돌릴 수 없습니다.</strong>")
                .Add("IsCancelable", true)
                .Build());
        if (confirm)
        {
            var success = await service.EndSessionAsync(sessionId);
            if (!success)
                _ = interop.ShowNotifyAsync("처리되지 않은 주문이 있어 종료할 수 없습니다.", InteractiveInteropService.NotifyType.Error);
            broker.Broadcast(new TableEventArgs()
            {
                EventType = TableEventArgs.TableEventType.TableUpdate
            });
        }
    }
    
    private async Task ShowShareSessionModalAsync()
    {
        await modal.ShowAsync<ShareSessionModal, bool>(
            "세션 공유",
            ModalService.Params().Add("Session", _session).Build()
        );
    }

    private async Task ShowCompleteSessionModalAsync(int sessionId)
    {
        var confirm = await modal.ShowAsync<AlertModal, bool>(
            "결제 완료", ModalService.Params()
                .Add("InnerHtml", "정말로 결제 완료 처리를 하시겠습니까?<br><strong>이 작업은 되돌릴 수 없습니다.</strong>")
                .Add("IsCancelable", true)
                .Build());
        if (confirm)
        {
            await service.CompleteSessionAsync(sessionId);
            broker.Broadcast(new TableEventArgs()
            {
                EventType = TableEventArgs.TableEventType.TableUpdate
            });
            StateHasChanged();
        }
    }

    private async Task ShowTableSelectModalAsync(int sessionId)
    {
        await using var context = await factory.CreateDbContextAsync();
        var availableTables = await context.Tables
            .Include(x => x.Sessions)
            .Where(x => x.Sessions.All(y => y.State != TableSession.SessionState.Active))
            .ToListAsync();

        int? selectedTableId = await modal.ShowAsync<TableSelectModal, int?>(
            "테이블 이동", ModalService.Params()
                .Add("AvailableTables", availableTables)
                .Build());
        if (selectedTableId.HasValue)
        {
            int targetTableId = selectedTableId.Value;
            bool success = await service.MoveSessionAsync(sessionId, targetTableId);
            
            if (success) 
                broker.Broadcast(new TableEventArgs()
                {
                    EventType = TableEventArgs.TableEventType.TableUpdate
                });
            else
                _ =  interop.ShowNotifyAsync("오류가 발생하여 테이블 이동에 실패했습니다.", InteractiveInteropService.NotifyType.Error);
        }
    }

    public void Dispose() => broker.Unsubscribe(TableEventBroker.BroadcastId, OnTableEvent);
}   