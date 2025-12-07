using System.ComponentModel.DataAnnotations;

namespace BlazorApp.Features.Demo.Entities;

public class Department
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation property
    public ICollection<User> Users { get; set; } = new List<User>();
}
