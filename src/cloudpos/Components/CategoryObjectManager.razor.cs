using CloudInteractive.CloudPos.Components.Modal;
using CloudInteractive.CloudPos.Contexts;
using CloudInteractive.CloudPos.Models;
using CloudInteractive.CloudPos.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace CloudInteractive.CloudPos.Components;

public partial class CategoryObjectManager(IDbContextFactory<ServerDbContext> factory, ModalService modal, ILogger<CategoryObjectManager> logger, InteractiveInteropService interop) : ComponentBase
{
    private List<Category> GetCategories()
    {
        using var context = factory.CreateDbContext();
        return context.Categories.Include(x => x.Items).ToList();
    }

    private int GetCategoriesCount()
    {
        using var context = factory.CreateDbContext();
        return context.Categories.Count();
    }
    
    private async Task DeleteCategoryAsync(int c)
    {
        await using var context = await factory.CreateDbContextAsync();
        var item = await context.Categories.Include(x => x.Items).FirstAsync(x => x.CategoryId == c);
        if (item.Items.Count > 0)
        {
            await modal.ShowAsync<AlertModal, object?>("데이터 정합성 경고", ModalService.Params()
                .Add("InnerHtml", "이 카테고리 객체를 참조하는 모든 메뉴 객체를 제거하기 전까지는 이 객체를 제거할 수 없습니다.<br><br><strong>자세한 사항은 사용자 메뉴얼을 참조하십시오.</strong>")
                .Build());
        }
        else
        {
            if(await modal.ShowAsync<AlertModal, bool>("데이터 삭제 경고", ModalService.Params()
                .Add("InnerHtml", "정말로 이 카테고리 객체를 삭제하시겠습니까?<br><br><strong>이 작업은 되돌릴 수 없습니다.</strong>")
                .Build())) {
                try
                {
                    context.Categories.Remove(item);
                    await context.SaveChangesAsync();
                }
                catch (Exception e)
                {
                    DbSaveChangesErrorHandler(e);
                }

                StateHasChanged();
            }
        }
    }

    private async Task EditCategoryAsync(int c)
    {
        await using var context = await factory.CreateDbContextAsync();
        await modal.ShowAsync<EditCategoryModal, object?>("카테고리 수정", ModalService.Params()
            .Add("Category", await context.Categories.FirstAsync(x => x.CategoryId == c))
            .Build());

        try
        {
            await context.SaveChangesAsync();
        }
        catch (Exception e)
        {
            DbSaveChangesErrorHandler(e);
        }
        
    }

    private async Task AddCategoryAsync()
    {
        await using var context = await factory.CreateDbContextAsync();
        var x = await modal.ShowAsync<EditCategoryModal, Category>("카테고리 추가");
        if (x is null) return;
        try
        {
            context.Categories.Add(x);
            await context.SaveChangesAsync();
        }
        catch (Exception e)
        {
            DbSaveChangesErrorHandler(e);
        }
        StateHasChanged();
    }
    
    private void DbSaveChangesErrorHandler(Exception e){
        logger.LogError(e.ToString());
        _ = interop.ShowNotifyAsync("서버 오류가 발생하여 변경 사항을 저장할 수 없었습니다.", InteractiveInteropService.NotifyType.Error);
    }
}