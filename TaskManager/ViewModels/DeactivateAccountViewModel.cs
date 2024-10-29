using TaskManager.Models;
using System.ComponentModel.DataAnnotations;

namespace TaskManager.ViewModels;

public class DeactivateAccountViewModel {
    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; }

    [Required]
    [Display(Name = "I confirm that I want to deactivate my account")]
    public bool ConfirmDeactivation { get; set; }

}