using System.ComponentModel.DataAnnotations;

namespace TaskManager.ViewModels
{
    public class ReactivateAccountViewModel
    {
        [Required(ErrorMessage = "Email or Username is required.")]
        public string EmailOrUsername { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}