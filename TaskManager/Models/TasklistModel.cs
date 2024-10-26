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

    public string? CreatedByUserName { get; set; }

    public string? UserRole {get; set;}

    // New list to store each contributor's details
    public List<ContributorModel> Contributors { get; set; } = new List<ContributorModel>();
}

public class ContributorModel {
    public string UserName { get; set; }
    public string ProfileImage { get; set; }
}