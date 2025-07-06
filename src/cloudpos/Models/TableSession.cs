using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CloudInteractive.CloudPos.Models;

public class TableSession
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int SessionId { get; set; }
    public virtual Table Table { get; set; } = null!;
    public int TableId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public bool IsPaymentCompleted { get; set; }
    
    [StringLength(4, MinimumLength = 4)]
    public required string AuthCode { get; set; }
    public ICollection<Order> Orders { get; } = new List<Order>();
    
}