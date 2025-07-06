using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace CloudInteractive.CloudPos.Models;

public class Category
{
    [Key]
    public int CategoryId { get; set; }
    
    [StringLength(30)]
    public required string Name { get; set; }

    public ICollection<Item> Items { get; } = new List<Item>();
}