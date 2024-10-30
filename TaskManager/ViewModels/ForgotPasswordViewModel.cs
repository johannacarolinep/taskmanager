
using System.ComponentModel.DataAnnotations;

namespace TaskManager.ViewModels;

public class ForgotPasswordViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; }
}