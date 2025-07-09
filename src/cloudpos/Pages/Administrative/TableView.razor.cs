using CloudInteractive.CloudPos.Event;
using Microsoft.AspNetCore.Components;

namespace CloudInteractive.CloudPos.Pages.Administrative;

public partial class TableView(TableEventBroker broker) : ComponentBase
{
    public string testvalue;
    private void Test()
    {
        broker.Broadcast(new TableEventArgs()
        {
            Data = new MessageEventArgs()
            {
                Message = testvalue,
                ShowAsModal = true
            },
            EventType = TableEventArgs.TableEventType.Message
        });
    }
}