using Microsoft.AspNetCore.Components;

namespace CloudInteractive.CloudPos.Components.Modal;

public class ModalComponentBase : ComponentBase
{
    [Parameter] public Func<object?, Task> Close { get; set; } = _ => Task.CompletedTask;
    
    protected Task CloseModal(object? result = null) => Close(result);
}