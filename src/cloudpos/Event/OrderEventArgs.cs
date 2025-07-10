using CloudInteractive.CloudPos.Models;

namespace CloudInteractive.CloudPos.Event;

public enum OrderEventType {Created, Cancelled, Completed}
public class OrderEventArgs
{
    public required Order Order;
    public required OrderEventType EventType;
}