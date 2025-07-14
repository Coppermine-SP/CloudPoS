using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
namespace CloudInteractive.CloudPos.Components.Modal;

public sealed class ModalErrorBoundary : ErrorBoundary
{
    /// <summary>예외가 발생하면 호출되는 콜백</summary>
    [Parameter] public Func<Exception, Task>? NotifyError { get; set; }

    protected override async Task OnErrorAsync(Exception exception)
    {
        await base.OnErrorAsync(exception);
        
        if (NotifyError is not null)
            await NotifyError.Invoke(exception);
    }
}