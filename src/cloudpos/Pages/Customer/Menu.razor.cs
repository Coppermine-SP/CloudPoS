using CloudInteractive.CloudPos.Contexts;
using CloudInteractive.CloudPos.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;

namespace CloudInteractive.CloudPos.Pages.Customer;

public partial class Menu(ServerDbContext context, IJSRuntime js) : ComponentBase, IAsyncDisposable
{
    private IJSObjectReference? _module;
    private List<Category>? _categories;
    private int _selectedCategoryId = -1;
    private int? _nextScrollCategoryId;
    
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