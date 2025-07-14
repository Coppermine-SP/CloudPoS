using CloudInteractive.CloudPos.Components.Modal;
using CloudInteractive.CloudPos.Services;
using Microsoft.AspNetCore.Components;

namespace CloudInteractive.CloudPos.Pages.Shared;

public abstract class PageLayoutBase(InteractiveInteropService interop, ModalService modal) : LayoutComponentBase
{
    protected struct MenuItem
    {
        public string Name;
        public string Url;
    }

    protected abstract MenuItem[] GetMenuItems();
    
    protected async Task OnInfoBtnClick()
    {
        await modal.ShowAsync<AboutModal, object?>("정보");
    }

    protected async Task OnColorChangeBtnClickAsync()
    {
        int current = (int)await interop.GetPreferredColorSchemeAsync();
        int next = (current + 1) % 3;
        await interop.SetPreferredColorSchemeAsync((InteractiveInteropService.ColorScheme)next);
        await interop.ShowNotifyAsync($"테마가 {interop.CurrentColorScheme.Item1}으로 변경되었습니다.",
            InteractiveInteropService.NotifyType.Info);
        StateHasChanged();
    }
}