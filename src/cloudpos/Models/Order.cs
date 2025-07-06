using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CloudInteractive.CloudPos.Models;

public class Order
{
    public enum OrderStatus {Received, Cancelled, Completed}
    
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int OrderId { get; set; }
    public virtual TableSession? Session { get; set; }
    public int SessionId { get; set; }
    
    public OrderStatus Status { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public ICollection<OrderItem> OrderItems { get; } = new List<OrderItem>();
}