using CloudInteractive.CloudPos.Components.Modal;
using CloudInteractive.CloudPos.Contexts;
using CloudInteractive.CloudPos.Event;
using CloudInteractive.CloudPos.Models;
using CloudInteractive.CloudPos.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace CloudInteractive.CloudPos.Components.TableView;

public partial class ManualOrder(IDbContextFactory<ServerDbContext> dbFactory, TableService tableService, InteractiveInteropService interop, TableEventBroker broker, ModalService modal) : ComponentBase
{
    [Parameter, EditorRequired] public int SessionId { get; set; }
    
    private List<Item> _allItems = new();
    private readonly List<OrderItem> _currentOrderItems = new();
    private TableSession? _session;
    
    private string _searchTerm = string.Empty;
    private List<Category> _allCategories = new();
    private int _selectedCategoryId = -1; // 전체를 -1로 정의
    private string CurrencyFormat(int x) => x == 0 ? "￦0": $"￦{x:#,###}";
    
    protected override async Task OnInitializedAsync()
    {
        await using var context = await dbFactory.CreateDbContextAsync();
        _allItems = await context.Items.AsNoTracking().Where(i => i.IsAvailable).ToListAsync();
        _allCategories = await context.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync();
        _allCategories.Insert(0, new Category { CategoryId = -1, Name = "전체" });
        _session = await context.Sessions.FirstAsync(x => x.SessionId == SessionId);
    }
    
    protected override void OnParametersSet()
    {
        _currentOrderItems.Clear();
        _searchTerm = string.Empty;
        _selectedCategoryId = -1;
    }
    private ICollection<Item> FilteredItems => _allItems
        .Where(i => _selectedCategoryId == -1 || i.CategoryId == _selectedCategoryId)
        .Where(i => string.IsNullOrWhiteSpace(_searchTerm) || i.Name.Contains(_searchTerm, StringComparison.OrdinalIgnoreCase))
        .ToList();

    private int TotalAmount => _currentOrderItems.Sum(oi => oi.Item.Price * oi.Quantity);
    
    private void AddToOrder(Item item)
    {
        var existingItem = _currentOrderItems.FirstOrDefault(oi => oi.Item.ItemId == item.ItemId);
        if (existingItem != null)
        {
            if (existingItem.Quantity < 10)
            {
                existingItem.Quantity++;
            }
        }
        else
        {
            _currentOrderItems.Add(new OrderItem { Item = item, Quantity = 1 });
        }
    }
    private void Remove(OrderItem item)
    {
        _currentOrderItems.Remove(item);
    }
    
    private void IncreaseQuantity(OrderItem orderItem)
    {
        if (orderItem.Quantity < 10) orderItem.Quantity++;
        else 
            _ = interop.ShowNotifyAsync("주문의 품목당 최대 개수는 10개입니다.", InteractiveInteropService.NotifyType.Error);
    }
    
    private void DecreaseQuantity(OrderItem orderItem)
    {
        orderItem.Quantity--;
        if (orderItem.Quantity <= 0)
        {
            Remove(orderItem);
        }   
    }
    
    private async Task SubmitOrderAsync()
    {
        if (_currentOrderItems.Count == 0) return;
        
        var listHtml = string.Join(
            "\n",
            _currentOrderItems.Select(t => $"<li>{t.Item.Name} × {t.Quantity}</li>")
        );
        
        string innerHtml = $"""
                            <ul style='line-height: 1.8'>
                                {listHtml}
                            </ul>
                            <strong class='fw-bold'>주문 합계: {@CurrencyFormat(_currentOrderItems.Sum(x => x.Quantity * x.Item.Price))}</strong><br>
                            주문하실 내용이 맞습니까?
                            """;
        if (!await modal.ShowAsync<AlertModal, bool>("주문서 확인", ModalService.Params()
                .Add("InnerHtml", innerHtml)
                .Add("IsCancelable", true)
                .Build()))
            return;

        var orderData = _currentOrderItems
            .Select(oi => new TableService.OrderItem(oi.Item.ItemId, oi.Quantity))
            .ToList();

        var success = await tableService.MakeOrderAsync(orderData, SessionId);
        if (success)
            broker.Publish(new TableEventArgs()
            {
                TableId = TableEventBroker.BroadcastId,
                EventType = TableEventArgs.TableEventType.TableUpdate
            });
        
        _currentOrderItems.Clear();
    }
}