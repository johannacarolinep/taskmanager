using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TaskManager.Models;

namespace TaskManager.Models;

public enum UserListRole
{
    Owner,
    Admin,
    Contributor
}

public enum InvitationStatus
{
    Pending,
    Accepted
}

public class ListUserModel
{
    [Key]
    public int Id { get; set; }

    // Foreign key to UserModel
    public int? UserId { get; set; }

    // Foreign key to TaskListModel
    [Required]
    public int ListId { get; set; }

    // Role field with constraints
    [Required]
    [MaxLength(20)]
    public UserListRole Role { get; set; } = UserListRole.Contributor;

    [MaxLength(100)]
    public string? InviteEmail { get; set; }

    [Required]
    [MaxLength(10)]
    public InvitationStatus InvitationStatus { get; set; } = InvitationStatus.Pending;

    public DateTime? InvitationSentAt { get; set; } = DateTime.Now;

    [Required]
    public bool IsActive { get; set; } = false;
}