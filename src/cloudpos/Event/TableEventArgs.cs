namespace CloudInteractive.CloudPos.Event;

public class TableEventArgs : EventArgs
{
    public enum TableEventType
    {
        SessionEnd,
        StaffCall,
        CatalogUpdated,
        Message,
        Order,
    }

    public int TableId;
    public TableEventType EventType;
    public object? Data;
}