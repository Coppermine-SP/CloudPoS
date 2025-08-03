using CloudInteractive.CloudPos.Components.Modal;
using CloudInteractive.CloudPos.Models;
using CloudInteractive.CloudPos.Services;
using Microsoft.AspNetCore.Components;

namespace CloudInteractive.CloudPos.Components.TableView;

public partial class SessionSummary(ModalService modal): ComponentBase
{
    [Parameter, EditorRequired]
    public TableSession TableSession { get; set; } = null!;

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
    private async Task ShowShareSessionModalAsync()
    {
        await modal.ShowAsync<ShareSessionModal, bool>(
            "세션 공유",
            ModalService.Params().Add("Session", TableSession).Build()
        );
    }
}