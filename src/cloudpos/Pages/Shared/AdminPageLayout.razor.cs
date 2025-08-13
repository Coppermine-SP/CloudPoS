using CloudInteractive.CloudPos.Components.Modal;
using CloudInteractive.CloudPos.Contexts;
using CloudInteractive.CloudPos.Models;
using CloudInteractive.CloudPos.Services;
using CloudInteractive.CloudPos.Event;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;

namespace CloudInteractive.CloudPos.Pages.Shared;

public partial class AdminPageLayout(ILogger<AdminPageLayout> logger, IDbContextFactory<ServerDbContext> factory, InteractiveInteropService interop, TableEventBroker broker, NavigationManager navigation, ConfigurationService config, IJSRuntime js, ModalService modal, TableService table) : PageLayoutBase(interop, modal), IDisposable
{
    private readonly InteractiveInteropService _interop = interop;
    private readonly ModalService _modal = modal;

    protected override MenuItem[] GetMenuItems() =>
    [
        new() { Name = "테이블 뷰", Url = "administrative/tableview" },
        new() { Name = "주문 뷰", Url = "administrative/orderview" },
        new() { Name = "통계", Url = "administrative/statistics" },
        new() { Name = "객체 관리자", Url = "administrative/objectmanager" },
    ];

    protected override void OnInitialized() => broker.Subscribe(TableEventBroker.BroadcastId, OnBroadcastEvent);
    

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            if (!await _interop.IsPwaDisplayMode())
                _ = _interop.ShowNotifyAsync(
                    "최대 호환성을 위해 CloudPOS Administrative Portal Web App을 설치하는 것이 권장됩니다.",
                    InteractiveInteropService.NotifyType.Warning);
            else
                _ = _interop.ShowNotifyAsync("CloudInteractive CloudPOS Administrative Portal에 오신 것을 환영합니다.", InteractiveInteropService.NotifyType.Success);
        }
    }

    private async void OnBroadcastEvent(object? sender, TableEventArgs e)
    {
        try
        {
            if (e.EventType == TableEventArgs.TableEventType.StaffCall)
                await OnStaffCalledAsync((int)e.Data!);
            else if(e.EventType == TableEventArgs.TableEventType.Order)
                await OnTransactionAsync((OrderEventArgs)e.Data!);
            else if (e.EventType == TableEventArgs.TableEventType.SessionEnd)
                await OnSessionEndAsync((int)e.Data!);
        }
        catch(Exception ex)
        {
            logger.LogError("Exception on OnBroadcastEvent(): " + ex);
        }
    }
    
    private async Task OnStaffCalledAsync(int sessionId)
    {
        var context = await factory.CreateDbContextAsync();
        var session = await table.GetSessionAsync(sessionId);
        if (session is null) return;
        _ = _interop.PlaySoundAsync(InteractiveInteropService.Sound.Ding);
        ShowAlert(AlertType.Call, "테이블 콜", $"<strong>테이블 {session.Table!.Name}</strong><br>세션 #{session.SessionId} ({DateTime.Now:yyyy-MM-dd HH:mm:ss})");
    }

    private async Task OnSessionEndAsync(int sessionId)
    {
        var context = await factory.CreateDbContextAsync();
        var session = await table.GetSessionAsync(sessionId);
        if (session is null) return;
        _ = _interop.PlaySoundAsync(InteractiveInteropService.Sound.Ding);
        ShowAlert(AlertType.Call, "계산 요청", $"<strong>테이블 {session.Table!.Name}</strong><br>세션 #{session.SessionId} ({DateTime.Now:yyyy-MM-dd HH:mm:ss})");
    }

    private async Task OnTransactionAsync(OrderEventArgs args)
    {
        var context = await factory.CreateDbContextAsync();
        var order = await context.Orders
            .Include(x => x.Session)
            .ThenInclude(x => x!.Table)
            .Include(x => x.OrderItems)
            .ThenInclude(x => x.Item)
            .FirstOrDefaultAsync(x => x.OrderId == args.OrderId);
        if (order is null) return;
        if (args.EventType == OrderEventType.Created)
        {
            var listHtml = string.Join(
                "\n",
                order.OrderItems.Select(t => $"<li>{t.Item.Name} × {t.Quantity}</li>")
            );
            ShowAlert(AlertType.Transaction, "주문 접수", $"""
                                                      <strong>주문 #{order.OrderId} [세션 #{order.Session!.SessionId}, 테이블 {order.Session!.Table.Name}]</strong><br>
                                                      <ul>
                                                         {listHtml}
                                                      </ul>
                                                      """, 20000);
            _ = _interop.PlaySoundAsync(InteractiveInteropService.Sound.Notify);
        }
        else if (args.EventType == OrderEventType.Completed)
        {
            ShowAlert(AlertType.Transaction, "주문 업데이트", $"""
                                                      <strong>주문 #{order.OrderId} [세션 #{order.Session!.SessionId}, 테이블 {order.Session!.Table.Name}]</strong><br>
                                                      주문 상태 완료로 변경됨.
                                                      """, 20000);
            _ = _interop.PlaySoundAsync(InteractiveInteropService.Sound.Chimes);
        }
        else if(args.EventType == OrderEventType.Cancelled)
        {
            ShowAlert(AlertType.Transaction, "주문 업데이트", $"""
                                                        <strong>주문 #{order.OrderId} [세션 #{order.Session!.SessionId}, 테이블 {order.Session!.Table.Name}]</strong><br>
                                                        주문 상태 취소로 변경됨.
                                                        """, 20000);
            _ = _interop.PlaySoundAsync(InteractiveInteropService.Sound.Chimes);
        }

    }
    
    private enum AlertType {Warning, Call, Transaction}

    private void ShowAlert(AlertType type, string title, string message, int duration = 60000)
    {
        string theme;
        string icon;
        switch (type)
        {
            case AlertType.Warning:
                theme = "warning";
                icon = "bi-exclamation-triangle-fill";
                break;
            case AlertType.Call:
                theme = "call";
                icon = "bi-bell-fill";
                break;
            default:
                theme = "transaction";
                icon = "bi-check-circle-fill";
                break;
        }
        _ = js.InvokeVoidAsync("showAlertCard", new
        {
            theme = theme,
            title = title,
            html = message,
            icon = icon,
            duration = duration
        });
    }

    private async Task OnLogoutBtnClickAsync()
    {
        if (await _modal.ShowAsync<AlertModal, bool>("로그아웃",ModalService.Params()
                .Add("InnerHtml", "정말 로그아웃 하시겠습니까?")
                .Add("IsCancelable", true)
                .Build()))
        {
            navigation.NavigateTo("/administrative/authorize?signout=true", replace: true, forceLoad: true);
        }
    }
    
    public void Dispose() => broker.Unsubscribe(TableEventBroker.BroadcastId, OnBroadcastEvent);
}