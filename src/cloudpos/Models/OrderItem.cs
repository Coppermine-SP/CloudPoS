namespace CloudInteractive.CloudPos.Models;

public class OrderItem
{
    public virtual Order Order { get; set; } = null!;
    public int OrderId { get; set; }
    public virtual Item Item { get; set; } = null!;
    public int ItemId { get; set; }
    
    public int  Quantity { get; set; }
}