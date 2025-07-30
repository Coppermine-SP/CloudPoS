namespace CloudInteractive.CloudPos.Models;

public class TableViewCell
{
    public int X { get; set; }
    public int Y { get; set; }
    public int TableId { get; set; }
    public virtual Table Table { get; set; } = null!;
}