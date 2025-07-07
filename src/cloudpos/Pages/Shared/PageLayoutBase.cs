using CloudInteractive.CloudPos.Services;
using Microsoft.AspNetCore.Components;

namespace CloudInteractive.CloudPos.Pages.Shared;

public abstract class PageLayoutBase(InteractiveInteropService interop) : LayoutComponentBase
{
    protected struct MenuItem
    {
        public string Name;
        public string Url;
    }

    protected abstract MenuItem[] GetMenuItems();
    
    protected void OnInfoBtnClick()
    {
        _ = interop.ShowModalAsync("정보", """
                                         <div class='alert alert-dark shadow py-2 fw-normal m-0 d-flex flex-row justify-content-center align-items-center' style='font-size: 14px''>
                                            <div>
                                                <i class="bi bi-github d-inline-block" style="margin-right: 8px;"></i>
                                            </div>
                                            <div>
                                                GitHub에서 이 레포지토리 보기:
                                                <wbr> <a href='https://github.com/Coppermine-SP/CloudPoS'>Coppermine-SP/CloudPoS</a>
                                            </div>
                                         </div>
                                         <div class='d-flex justify-content-center mt-5 mb-1'>
                                            <img src='/images/logo.png' class='img-fluid rounded' style='width: 80px'/>
                                         </div>
                                         <h5 class='text-center fw-bold m-0'>CloudInteractive</h5>
                                         <h5 class='text-center fw-lighter m-0'>CloudPoS</h6>
                                         <p class='monospace-font text-secondary text-center mb-0 mt-5' style='font-size: 12px'>0.0.1-alpha / Microsoft ASP.NET Core 9.0
                                         <br>Copyright (C) 2025 Coppermine-SP.</p>
                                         """, false);
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