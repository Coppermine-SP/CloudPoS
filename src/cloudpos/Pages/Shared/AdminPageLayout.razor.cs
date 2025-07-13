using CloudInteractive.CloudPos.Contexts;
using CloudInteractive.CloudPos.Models;
using CloudInteractive.CloudPos.Services;
using CloudInteractive.CloudPos.Event;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace CloudInteractive.CloudPos.Pages.Shared;

public partial class AdminPageLayout(ILogger<AdminPageLayout> logger, TableService table, InteractiveInteropService interop, TableEventBroker broker, NavigationManager navigation, ConfigurationService config, IJSRuntime js) : PageLayoutBase(interop), IDisposable
{
    private readonly InteractiveInteropService _interop = interop;
    private bool _init = false;
    
    protected override MenuItem[] GetMenuItems() =>
    [
        new() { Name = "테이블 뷰", Url = "Administrative/TableView" },
        new() { Name = "주문 뷰", Url = "Administrative/OrderView" },
        new() { Name = "통계", Url = "Administrative/Statistics" },
        new() { Name = "객체 관리자", Url = "Administrative/ObjectManager" },
        new() { Name = "개발자 도구", Url = "Administrative/DevTool" }
    ];

    protected override void OnInitialized()
    {
        if (table.ValidateSession(true) != TableService.ValidateResult.Ok)
        {
            logger.LogInformation("Unauthorized. Redirecting to /Administrative/Authorize");
            navigation.NavigateTo("/Administrative/Authorize", replace: true, forceLoad: true);
            return;
        }
        
        _init = true;
        broker.Subscribe(TableEventBroker.BroadcastId, OnBroadcastEvent);
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            _ = interop.ShowNotifyAsync("브라우저 정책으로 인해 알림음을 들으려면 아무 요소나 클릭해야 합니다.",
                InteractiveInteropService.NotifyType.Warning);
        }
    }

    private void OnBroadcastEvent(object? sender, TableEventArgs e)
    {
        if (e.EventType == TableEventArgs.TableEventType.StaffCall)
            OnStaffCalled((TableSession)e.Data!);
        else if(e.EventType == TableEventArgs.TableEventType.Order)
            OnTransaction((OrderEventArgs)e.Data!);
    }
    
    private void OnStaffCalled(TableSession session)
    {
        _ = _interop.PlaySoundAsync(InteractiveInteropService.Sound.Ding);
        ShowAlert(AlertType.Call, "테이블 콜", $"<strong>테이블 {session.Table!.Name}</strong><br>세션 #{session.SessionId} ({DateTime.Now:yyyy-MM-dd HH:mm:ss})");
    }

    private void OnTransaction(OrderEventArgs args)
    {
        _ = _interop.PlaySoundAsync(InteractiveInteropService.Sound.Notify);
        var listHtml = string.Join(
            "\n",
            args.Order.OrderItems.Select(t => $"<li>{t.Item.Name} × {t.Quantity}</li>")
        );
        ShowAlert(AlertType.Transation, "주문 접수", $"""
                                                 <strong>주문 #{args.Order.OrderId} [세션 #{args.Order.Session!.SessionId}, 테이블 {args.Order.Session!.Table.Name}]</strong><br>
                                                 <ul>
                                                    {listHtml}
                                                 </ul>
                                                 """, 20000);
    }
    
    private enum AlertType {Warning, Call, Transation}

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
        if (await _interop.ShowModalAsync("로그아웃", "정말 로그아웃하시겠습니까?"))
        {
            navigation.NavigateTo("/Administrative/Authorize?SignOut=true", replace: true, forceLoad: true);
        }
    }
    
    public void Dispose()
    {
        if(_init) broker.Unsubscribe(TableEventBroker.BroadcastId, OnBroadcastEvent);
    }
}