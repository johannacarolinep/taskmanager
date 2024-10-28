using System;
using System.ComponentModel.DataAnnotations;
using System.IO.Pipes;
using TaskManager.Models;

namespace TaskManager.ViewModels;

public class InviteUserViewModel {

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    public string Email { get; set; }

    [Required(ErrorMessage = "Role is required.")]
    public UserListRole Role { get; set; }

    [Required(ErrorMessage = "The form is not connected to a tasklist. Please come back later.")]
    public int ListId { get; set; }
    public string TasklistTitle { get; set; } = "unnamed tasklist";
    public string InvitingUsername { get; set; } = "anonymous";
}