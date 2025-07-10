using CloudInteractive.CloudPos.Contexts;
using CloudInteractive.CloudPos.Models;
using CloudInteractive.CloudPos.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.JSInterop;
using ZstdSharp.Unsafe;

namespace CloudInteractive.CloudPos.Pages.Customer;

public partial class Menu(ServerDbContext context, IJSRuntime js, InteractiveInteropService interop, TableService table) : ComponentBase, IAsyncDisposable
{
    private IJSObjectReference? _module;
    private List<Category>? _categories;
    private List<(int, Item)> _cart = new();
    private bool _isCartOpen = false;
    private void ToggleCart() => _isCartOpen = !_isCartOpen;
    private int _selectedCategoryId = -1;
    private int? _nextScrollCategoryId;
    
    private string CurrencyFormat(int x) => string.Format("￦{0:#,###}", x);
    protected override async Task OnInitializedAsync()
    {
        _categories = await context.Categories.Include(x => x.Items).ToListAsync();
        var all = new Category
        {
            Name = "전체",
            CategoryId = -1
        };
        foreach(var x in await context.Items.ToListAsync()) all.Items.Add(x);
        _categories.Insert(0, all);
        
        _module = await js.InvokeAsync<IJSObjectReference>("import", "./Pages/Customer/Menu.razor.js");
        await _module.InvokeVoidAsync("initCategoryScroller", "category-wrapper");
    }
    
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_nextScrollCategoryId is not null)
        {
            await _module!.InvokeVoidAsync("scrollToCategory", "category-wrapper", _nextScrollCategoryId);
            _nextScrollCategoryId = null;
        }
    }

    private void OnCategorySelected(int categoryId)
    {
        _selectedCategoryId = categoryId;
        _nextScrollCategoryId = categoryId;
        StateHasChanged();
    }

    private void AddToCart(Item item)
    {
        var idx = _cart.FindIndex(t => t.Item2 == item);
        if (idx >= 0)
        {
            var tuple = _cart[idx];
            _cart[idx] = (tuple.Item1 + 1, tuple.Item2);

            _ = interop.ShowNotifyAsync(
                $"{item.Name}의 수량을 {_cart[idx].Item1}개로 변경했습니다.",
                InteractiveInteropService.NotifyType.Success);
        }
        else
        {
            _cart.Add((1, item));
            _ = interop.ShowNotifyAsync(
                $"{item.Name}을(를) 장바구니에 담았습니다.",
                InteractiveInteropService.NotifyType.Success);
        }
    }

    private async Task CheckoutAsync()
    {
        var listHtml = string.Join(
            "\n",
            _cart.Select(t => $"<li>{t.Item2.Name} × {t.Item1}</li>")
        );
        
        string innerHtml = $"""
                            <ul style='line-height: 1.8'>
                                {listHtml}
                            </ul>
                            <strong class='fw-bold'>주문 합계: {@CurrencyFormat(_cart.Sum(x => x.Item1 * x.Item2.Price))}</strong><br>
                            주문하실 내용이 맞습니까?
                            """;
        
        if (!await interop.ShowModalAsync("주문 확인", innerHtml))
            return;
        
        var order = new Models.Order()
        {
            SessionId = table.GetSession()!.SessionId
        };

        foreach (var item in _cart)
        {
            order.OrderItems.Add(new OrderItem()
            {
                Item = item.Item2,
                Quantity = item.Item1
            });
        }

        if (!await table.MakeOrderAsync(order))
            await interop.ShowNotifyAsync("오류가 발생하여 주문 생성에 실패했습니다.", InteractiveInteropService.NotifyType.Error);
        
        _cart.Clear();
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