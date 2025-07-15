using System.Collections;
using CloudInteractive.CloudPos.Components.Modal;
using CloudInteractive.CloudPos.Contexts;
using CloudInteractive.CloudPos.Models;
using CloudInteractive.CloudPos.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace CloudInteractive.CloudPos.Components;

public partial class ItemObjectManager(ServerDbContext context, ModalService modal, InteractiveInteropService interop) : ComponentBase
{
    private int _selectedCategoryId = -1;
    private string _searchText = string.Empty;
    private string CurrencyFormat(int x) => $"￦{x:#,###}";
    private List<Item> GetItems()
    {
        List<Item> list;
        if (_selectedCategoryId == -1)
        {
            list = context.Items
                .Include(x => x.Category)
                .ToList();
        }
        else
        {
            list = context.Items
                .Include(x => x.Category)
                .Where(x => x.CategoryId == _selectedCategoryId)
                .ToList();
        }

        if (!String.IsNullOrWhiteSpace(_searchText))
            list = list.Where(x => x.Name.Contains(_searchText)).ToList();
        
        return list;
    }
    
    private List<Category> Categories => context.Categories.ToList();

    private async Task AddItemAsync()
    {
        var categories = context.Categories.ToList();
        var item = await modal.ShowAsync<EditItemModal, Item>("메뉴 수정", ModalService.Params()
            .Add("Categories", categories)
            .Build());

        if (item is null) return;
        context.Items.Add(item);
        try
        {
            await context.SaveChangesAsync();
        }
        catch
        {
            DbSaveChangesErrorHandler();
        }
    }

    private async Task EditItemAsync(Item i)
    {
        var categories = context.Categories.ToList();
        var item = await modal.ShowAsync<EditItemModal, Item>("메뉴 수정", ModalService.Params()
            .Add("Item", i)
            .Add("Categories", categories)
            .Build());
        try
        {
            await context.SaveChangesAsync();
        }
        catch
        {
            DbSaveChangesErrorHandler();
        }
    }

    private async Task DeleteItemAsync(Item i)
    {
        bool isExist = context.OrderItems.Any(x => x.ItemId == i.ItemId);

        if (isExist)
        {
            if (await modal.ShowAsync<AlertModal, bool>("데이터 정합성 경고", ModalService.Params()
                    .Add("InnerHtml",
                        "이 객체는 다른 객체가 참조하고 있습니다. 이 객체를 삭제하거나 변경하면, 이 객체를 참조하는 모든 객체(주문 내역 또는 세션)에 영향을 끼칩니다.<br><br><strong>정말 이 내용을 이해했습니까?<br>데이터 정합성에 대한 자세한 내용은 사용자 매뉴얼을 참조하십시오.</strong>")
                    .Add("IsCancelable", true)
                    .Build()))
            {
                if (await modal.ShowAsync<ConfirmDelete, bool>("데이터 정합성 경고"))
                {
                    context.Items.Remove(i);
                    try
                    {
                        await context.SaveChangesAsync();
                    }
                    catch
                    {
                        DbSaveChangesErrorHandler();
                    }
                }
            }
        }
    }
    
    private void DbSaveChangesErrorHandler(){
        _ = interop.ShowNotifyAsync("서버 오류가 발생하여 변경 사항을 저장할 수 없었습니다.", InteractiveInteropService.NotifyType.Error);
    }
}