using CloudInteractive.CloudPos.Models;

namespace CloudInteractive.CloudPos.Event;

public enum OrderEventType {Created, Cancelled, Completed}
public class OrderEventArgs
{
    public required int OrderId;
    public required OrderEventType EventType;
}