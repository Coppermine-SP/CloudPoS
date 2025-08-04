using CloudInteractive.CloudPos.Components.Modal;
using CloudInteractive.CloudPos.Contexts;
using CloudInteractive.CloudPos.Models;
using CloudInteractive.CloudPos.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace CloudInteractive.CloudPos.Components.TableView;

public partial class SessionManagement(ModalService modal, TableService service, InteractiveInteropService interop, IDbContextFactory<ServerDbContext> factory): ComponentBase
{
    [Parameter, EditorRequired] public TableSession TableSession { get; set; } = null!;
    [Parameter] public EventCallback OnEventCallback{ get; set; }

    private int TotalAmount => TableSession.Orders
        .SelectMany(order => order.OrderItems)
        .Sum(item => item.Quantity * item.Item.Price);
    private string CurrencyFormat(int x) => $"{x:₩#,###}";
    
    private string StateToKorean => TableSession.State switch
    {
        TableSession.SessionState.Active => "활성",
        TableSession.SessionState.Billing => "결제 중",
        TableSession.SessionState.Completed => "결제 완료",
        _ => TableSession.State.ToString()
    };
    private async Task ShowEndSessionAsyncModalAsync(int sessionId)
    {
        var confirm = await modal.ShowAsync<AlertModal, bool>(
            "주문 완료 확인", ModalService.Params()
                .Add("InnerHtml", "정말로 주문 완료 처리를 하시겠습니까?<br><br><strong>이 작업은 되돌릴 수 없습니다.</strong>")
                .Add("IsCancelable", true)
                .Build());
        if (confirm)
        {
            var success = await service.EndSessionAsync(sessionId);
            if (!success)
                _ = interop.ShowNotifyAsync("처리되지 않은 주문이 있어 종료할 수 없습니다.", InteractiveInteropService.NotifyType.Error);
        }
        await OnEventCallback.InvokeAsync();
    }
    private async Task ShowShareSessionModalAsync()
    {
        await modal.ShowAsync<ShareSessionModal, bool>(
            "세션 공유",
            ModalService.Params().Add("Session", TableSession).Build()
        );
    }

    private async Task ShowCompleteSessionModalAsync(int sessionId)
    {
        var confirm = await modal.ShowAsync<AlertModal, bool>(
            "결제 완료 확인", ModalService.Params()
                .Add("InnerHtml", "정말로 결제 완료 처리를 하시겠습니까?<br><br><strong>이 작업은 되돌릴 수 없습니다.</strong>")
                .Add("IsCancelable", true)
                .Build());
        if (confirm)
        {
            await service.CompleteSessionAsync(sessionId);
            await OnEventCallback.InvokeAsync();
        }
    }

    private async Task ShowTableSelectModalAsync(int sessionId)
    {
        await using var context = await factory.CreateDbContextAsync();
        var availableTables = await context.Tables
            .Include(x => x.Sessions)
            .Where(x => x.Sessions.All(y => y.State != TableSession.SessionState.Active))
            .ToListAsync();

        var confirm = await modal.ShowAsync<TableSelectModal, bool>("테이블 이동",
            ModalService.Params()
                .Add("AvailableTables", availableTables)
                .Build());
        if (confirm)
        {
            
        }
    }
}   