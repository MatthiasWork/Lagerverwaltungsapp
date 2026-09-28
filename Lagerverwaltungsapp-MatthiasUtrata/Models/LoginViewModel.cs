using System.ComponentModel.DataAnnotations;

namespace Lagerverwaltungsapp_MatthiasUtrata.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Bitte Benutzernamen eingeben.")]
        [Display(Name = "Benutzername")]
        public string Username { get; set; } = null!;

        [Required(ErrorMessage = "Bitte Passwort eingeben.")]
        [DataType(DataType.Password)]
        [Display(Name = "Passwort")]
        public string Password { get; set; } = null!;
    }
}
