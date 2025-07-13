using CloudInteractive.CloudPos.Contexts;
using CloudInteractive.CloudPos.Models;
using CloudInteractive.CloudPos.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace CloudInteractive.CloudPos.Pages.Administrative;

public partial class ObjectManager : ComponentBase

{
    [Inject]
    protected ServerDbContext Db { get; set; } = null!;
    [Inject]
    protected InteractiveInteropService Interop { get; set; } = null!;
    
    protected string Mode { get; private set; } = "Category";

    protected List<Category> Categories = [];
    protected bool     IsCategoryModalOpen;
    protected Category EditingCategory = new()
    {
        Name = ""
    };
    
    protected List<Item> Items = new();
    protected bool       IsItemModalOpen;
    protected Item       EditingItem = new()
    {
        Name = ""
    };

    protected override async Task OnInitializedAsync()
    {
        await LoadAll();
    }

    protected async Task LoadAll()
    {
        Categories = await Db.Categories.ToListAsync();
        Items      = await Db.Items.Include(x => x.Category).ToListAsync();
    }

    // 모드 전환
    protected void SelectMode(string mode)
    {
        Mode = mode;
    }
    
    protected void AddCategory()
    {
        EditingCategory = new()
        {
            Name = ""
        };
        IsCategoryModalOpen = true;
    }

    protected void EditCategory(Category c)
    {
        EditingCategory = new Category {
            CategoryId = c.CategoryId,
            Name       = c.Name
        };
        IsCategoryModalOpen = true;
    }

    protected void CloseCategoryModal()
    {
        IsCategoryModalOpen = false;
    }

    protected async Task SaveCategoryAsync()
    {
        try
        {
            if (EditingCategory.CategoryId == 0)
            {
                Db.Categories.Add(EditingCategory);
            }
            else
            {
                var existing = await Db.Categories.FindAsync(EditingCategory.CategoryId);
                if (existing is not null)
                {
                    existing.Name = EditingCategory.Name;
                }
            }

            await Db.SaveChangesAsync();
            IsCategoryModalOpen = false;
            await LoadAll();
        }
        catch (Exception ex)
        {
            Console.WriteLine("카테고리 저장 실패");
            Console.WriteLine(ex.Message);
            await Interop.ShowNotifyAsync($"저장 오류: {ex.Message}",
                InteractiveInteropService.NotifyType.Error);
        }
    }

    protected async Task DeleteCategory(int id)
    {
        var c = await Db.Categories.FindAsync(id);
        if (c != null)
        {
            Db.Categories.Remove(c);
            await Db.SaveChangesAsync();
            await LoadAll();
        }
    }
    
    protected void AddItem()
    {
        EditingItem = new Item
        {
            Name = ""
        };
        IsItemModalOpen = true;
    }

    protected void EditItem(Item i)
    {
        EditingItem = new Item {
            ItemId      = i.ItemId,
            CategoryId  = i.CategoryId,
            Name        = i.Name,
            Price       = i.Price,
            IsAvailable = i.IsAvailable
        };
        IsItemModalOpen = true;
    }

    protected void CloseItemModal()
    {
        IsItemModalOpen = false;
    }

    protected async Task SaveItemAsync()
    {
        try
        {
            if (EditingItem.ItemId == 0)
            {
                Db.Items.Add(EditingItem);
            }
            else
            {
                var existing = await Db.Items.FindAsync(EditingItem.ItemId);
                if (existing is not null)
                {
                    existing.Name        = EditingItem.Name;
                    existing.Price       = EditingItem.Price;
                    existing.IsAvailable = EditingItem.IsAvailable;
                    existing.CategoryId  = EditingItem.CategoryId;
                }
            }

            await Db.SaveChangesAsync();
            
            IsItemModalOpen = false;
            await LoadAll();
        }
        catch (Exception ex)
        {
            Console.WriteLine("아이템 저장 실패");
            Console.WriteLine(ex.Message);
            
            await Interop.ShowNotifyAsync(
                $"저장 오류: {ex.Message}",
                InteractiveInteropService.NotifyType.Error);
        }
    }

    protected async Task DeleteItem(int id)
    {
        var i = await Db.Items.FindAsync(id);
        if (i != null)
        {
            Db.Items.Remove(i);
            await Db.SaveChangesAsync();
            await LoadAll();
        }
    }
}