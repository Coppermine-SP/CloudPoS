using CloudInteractive.CloudPos.Components.Modal;
using CloudInteractive.CloudPos.Contexts;
using CloudInteractive.CloudPos.Models;
using CloudInteractive.CloudPos.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;

namespace CloudInteractive.CloudPos.Pages.Customer;

public partial class Menu(ServerDbContext context, IJSRuntime js, InteractiveInteropService interop, TableService table, ConfigurationService config, ModalService modal) : ComponentBase, IAsyncDisposable
{
    private record CartItem(int Quantity, Item Item)
    {
        public int Quantity = Quantity;
        public Item Item = Item;
    }
    
    private IJSObjectReference? _module;
    private List<Category>? _categories;
    private readonly List<CartItem> _cart = new();
    private bool _isCartOpen = false;
    private void ToggleCart() => _isCartOpen = !_isCartOpen;
    private int _selectedCategoryId = -1;
    
    private string CurrencyFormat(int x) => string.Format("￦{0:#,###}", x);
    private string GetImageUrl(int imageId) => $"{config.ImageBaseUrl}/static-assets/{imageId}.webp";
    protected override async Task OnInitializedAsync()
    {
        _categories = await context.Categories.Include(x => x.Items).ToListAsync();
        var all = new Category
        {
            Name = "전체",
            CategoryId = -1,
        };
        foreach(var x in _categories.SelectMany(x => x.Items)) all.Items.Add(x);
        _categories.Insert(0, all);
        _module = await js.InvokeAsync<IJSObjectReference>("import", "./Pages/Customer/Menu.razor.js");
        await _module!.InvokeVoidAsync("initCategoryScroller", "category-wrapper");
    }
    
    private void OnCategorySelected(int categoryId)
    {
        _selectedCategoryId = categoryId;
        _ = _module!.InvokeVoidAsync("scrollToCategory", "category-wrapper", categoryId);
    }

    private void AddToCart(Item item)
    {
        var idx = _cart.FindIndex(t => t.Item == item);
        if (idx >= 0)
        {
            var x = _cart[idx];
            if (AddItemQuantity(x))
            {
                _ = interop.ShowNotifyAsync(
                    $"{item.Name}의 수량을 {_cart[idx].Quantity}개로 변경했습니다.",
                    InteractiveInteropService.NotifyType.Success);
            }
        }
        else
        {
            _cart.Add(new CartItem(1, item));
            _ = interop.ShowNotifyAsync(
                $"{item.Name}을(를) 장바구니에 담았습니다.",
                InteractiveInteropService.NotifyType.Success);
        }
    }

    private async Task CheckoutAsync()
    {
        var listHtml = string.Join(
            "\n",
            _cart.Select(t => $"<li>{t.Item.Name} × {t.Quantity}</li>")
        );
        
        string innerHtml = $"""
                            <ul style='line-height: 1.8'>
                                {listHtml}
                            </ul>
                            <strong class='fw-bold'>주문 합계: {@CurrencyFormat(_cart.Sum(x => x.Quantity * x.Item.Price))}</strong><br>
                            주문하실 내용이 맞습니까?
                            """;
        if (!await modal.ShowAsync<AlertModal, bool>("주문서 확인", ModalService.Params()
                .Add("InnerHtml", innerHtml)
                .Add("IsCancelable", true)
                .Build()))
            return;
        
        var order = new Models.Order()
        {
            SessionId = table.GetSession()!.SessionId
        };

        foreach (var item in _cart)
        {
            order.OrderItems.Add(new OrderItem()
            {
                Item = item.Item,
                Quantity = item.Quantity,
            });
        }

        if (!await table.MakeOrderAsync(order))
            await interop.ShowNotifyAsync("오류가 발생하여 주문 생성에 실패했습니다.", InteractiveInteropService.NotifyType.Error);
        
        _cart.Clear();
    }

    private void RemoveFromCart(CartItem item)
    {
        _cart.Remove(item);
    }

    private bool AddItemQuantity(CartItem item)
    {
        if (item.Quantity >= 10)
        {
            _ = interop.ShowNotifyAsync("주문의 품목당 최대 개수는 10개입니다.", InteractiveInteropService.NotifyType.Error);
            return false;
        }
        item.Quantity += 1;
        return true;
    }

    private bool SubItemQuantity(CartItem item)
    {
        if (item.Quantity <= 1)
        {
            _ = interop.ShowNotifyAsync("주문의 품목당 최소 개수는 1개입니다.", InteractiveInteropService.NotifyType.Error);
            return false;
        }
        item.Quantity -= 1;
        return true;
    }
    
    public async ValueTask DisposeAsync()
    {
        if (_module is null) return;

        try
        {
            await _module.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
            //ignored
        }
    }
    
}