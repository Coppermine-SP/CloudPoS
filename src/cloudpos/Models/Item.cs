using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CloudInteractive.CloudPos.Models;

public class Item
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ItemId { get; set; }
    
    public int CategoryId { get; set; }
    
    [StringLength(30)]
    public required string Name { get; set; }
    
    [StringLength(50)]
    public string? Description { get; set; }
    
    public int ImageId { get; set; }
    
    public ICollection<OrderItem> OrderItems { get; } = new List<OrderItem>();

    public virtual Category Category { get; set; } = null!;
    
    public int Price { get; set; }
    
    public bool IsAvailable { get; set; }
}