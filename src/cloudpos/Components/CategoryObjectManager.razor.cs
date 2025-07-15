using CloudInteractive.CloudPos.Components.Modal;
using CloudInteractive.CloudPos.Contexts;
using CloudInteractive.CloudPos.Models;
using CloudInteractive.CloudPos.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace CloudInteractive.CloudPos.Components;

public partial class CategoryObjectManager(ServerDbContext context, ModalService modal) : ComponentBase
{
    private List<Category> Categories => context.Categories.Include(x => x.Items).ToList();
    private async Task DeleteCategoryAsync(Category c)
    {
        if (c.Items.Count > 0)
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
                context.Categories.Remove(c);
                await context.SaveChangesAsync();
                StateHasChanged();
            }
        }
    }

    private async Task EditCategoryAsync(Category c)
    {
        await modal.ShowAsync<EditCategoryModal, object?>("카테고리 수정", ModalService.Params()
            .Add("Category", c)
            .Build());
        await context.SaveChangesAsync();
    }

    private async Task AddCategoryAsync()
    {
        var x = await modal.ShowAsync<EditCategoryModal, Category>("카테고리 추가");
        if (x is null) return;
        context.Categories.Add(x);
        await context.SaveChangesAsync();
        StateHasChanged();
    }
}