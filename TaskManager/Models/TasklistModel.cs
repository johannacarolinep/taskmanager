using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskManager.Models;

// Represents a tasklist in the system
public class TasklistModel
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Title { get; set; } = "Unnamed tasklist";

    [MaxLength(255)]
    public string? Description { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Required]
    public bool IsActive { get; set; } = true;

    // Nullable foreign key relationship to UserModel (Tbl_User)
    public int? CreatedBy { get; set; }

    public string? UserRole {get; set;}

    public string? Contributors {get; set;}
}