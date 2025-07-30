using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CloudInteractive.CloudPos.Models;

public class Table
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int TableId { get; set; }
    
    [StringLength(30)]
    public required string Name { get; set; }
    
    public ICollection<TableSession> Sessions { get; } = new List<TableSession>();
    public virtual TableViewCell Cell { get; set; } = null!;
}