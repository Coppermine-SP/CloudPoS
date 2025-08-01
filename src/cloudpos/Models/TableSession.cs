using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CloudInteractive.CloudPos.Models;

public class TableSession
{
    public enum SessionState {Active, Billing, Completed}
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int SessionId { get; set; }
    public virtual Table Table { get; set; } = null!;
    public int TableId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? EndedAt { get; set; }

    public SessionState State { get; set; }
    
    [StringLength(4, MinimumLength = 4)]
    public string? AuthCode { get; set; }
    public ICollection<Order> Orders { get; } = new List<Order>();
}