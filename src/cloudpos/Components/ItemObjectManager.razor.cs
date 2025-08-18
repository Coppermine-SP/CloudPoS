using CloudInteractive.CloudPos.Components.Modal;
using CloudInteractive.CloudPos.Contexts;
using CloudInteractive.CloudPos.Event;
using CloudInteractive.CloudPos.Models;
using CloudInteractive.CloudPos.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace CloudInteractive.CloudPos.Components;

public partial class ItemObjectManager(IDbContextFactory<ServerDbContext> factory, ModalService modal, InteractiveInteropService interop, TableEventBroker broker, ILogger<ItemObjectManager> logger) : ComponentBase
{
    private List<Category>? _categories;
    private List<Item>? _items;
    private int _selectedCategoryId = -1;
    private string _searchText = string.Empty;
    private string CurrencyFormat(int x) => $"￦{x:#,###}";

    protected override async Task OnInitializedAsync()
    {
        await LoadCatalog();
        broker.Subscribe(TableEventBroker.BroadcastId, OnTableEvent);
    }

    private async Task LoadCatalog()
    {
        _categories = await GetCategoriesAsync();
        _items = await GetItemsAsync();
    }

    private async void OnTableEvent(object? sender, TableEventArgs e)
    {
        try
        {
            if (e.EventType != TableEventArgs.TableEventType.CatalogUpdated) return;
            _ = interop.ShowNotifyAsync("카탈로그가 업데이트 되었습니다.", InteractiveInteropService.NotifyType.Success);
            await LoadCatalog();
            StateHasChanged();
        }
        catch(Exception ex)
        {
            DbSaveChangesErrorHandler(ex);
        }
    }

    private async Task<List<Item>> GetItemsAsync()
    {
        await using var context = await factory.CreateDbContextAsync();
        var query = context.Items.AsQueryable();
        
        if (_selectedCategoryId != -1)
            query = query.Where(x => x.CategoryId == _selectedCategoryId);

        if (!String.IsNullOrWhiteSpace(_searchText))
            query = query.Where(x => x.Name.Contains(_searchText));

        return await query.Include(x => x.Category)
            .AsNoTracking()
            .ToListAsync();
    }

    private async Task<List<Category>> GetCategoriesAsync()
    { 
        await using var context = await factory.CreateDbContextAsync();
        return await context.Categories.ToListAsync();
    }

    private async Task AddItemAsync()
    {
        await using var context = await factory.CreateDbContextAsync();
        var item = await modal.ShowAsync<EditItemModal, Item>("메뉴 추가", ModalService.Params()
            .Add("Categories", _categories)
            .Build());

        if (item is null) return;
        context.Items.Add(item);
        try
        {
            await context.SaveChangesAsync();
            broker.Broadcast(new TableEventArgs()
            {
                EventType = TableEventArgs.TableEventType.CatalogUpdated
            });
        }
        catch(Exception e)
        {
            DbSaveChangesErrorHandler(e);
        }
    }

    private async Task EditItemAsync(int i)
    {
        await using var context = await factory.CreateDbContextAsync();
        _ = await modal.ShowAsync<EditItemModal, Item>("메뉴 수정", ModalService.Params()
            .Add("Item", await context.Items.FirstAsync(x => x.ItemId == i))
            .Add("Categories", _categories)
            .Build());
        try
        {
            await context.SaveChangesAsync();
            broker.Broadcast(new TableEventArgs()
            {
                EventType = TableEventArgs.TableEventType.CatalogUpdated
            });
        }
        catch(Exception e)
        {
            DbSaveChangesErrorHandler(e);
        }
    }

    private async Task DeleteItemAsync(int i)
    {
        await using var context = await factory.CreateDbContextAsync();
        bool isExist = await context.OrderItems.AnyAsync(x => x.ItemId == i);

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
                    await context.Items.Where(x => x.ItemId == i).ExecuteDeleteAsync();
                    try
                    {
                        await context.SaveChangesAsync();
                        broker.Broadcast(new TableEventArgs()
                        {
                            EventType = TableEventArgs.TableEventType.CatalogUpdated
                        });
                    }
                    catch(Exception e)
                    {
                        DbSaveChangesErrorHandler(e);
                    }
                }
            }
        }
        else
        {
            if (await modal.ShowAsync<AlertModal, bool>("메뉴 삭제", ModalService.Params()
                    .Add("InnerHtml",
                        "정말 이 객체를 삭제하시겠습니까?<br><br><strong>이 작업은 되돌릴 수 없습니다.</strong>")
                    .Add("IsCancelable", true)
                    .Build()))
            {
                await context.Items.Where(x => x.ItemId == i).ExecuteDeleteAsync();
                try
                {
                    await context.SaveChangesAsync();
                    broker.Broadcast(new TableEventArgs()
                    {
                        EventType = TableEventArgs.TableEventType.CatalogUpdated
                    });
                }
                catch(Exception e)
                {
                    DbSaveChangesErrorHandler(e);
                }
            }
        }
    }
    
    private void DbSaveChangesErrorHandler(Exception e){
        logger.LogError(e.ToString());
        _ = interop.ShowNotifyAsync("서버 오류가 발생하여 변경 사항을 저장할 수 없었습니다.", InteractiveInteropService.NotifyType.Error);
    }
}