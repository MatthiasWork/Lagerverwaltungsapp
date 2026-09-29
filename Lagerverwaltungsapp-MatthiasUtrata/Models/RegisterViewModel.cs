using System.ComponentModel.DataAnnotations;

namespace Lagerverwaltungsapp_MatthiasUtrata.Models
{
    public class RegisterViewModel
    {
        // Die Maximallängen entsprechen den Spalten der Tabelle Person
        [Required(ErrorMessage = "Bitte Vornamen eingeben.")]
        [StringLength(250, ErrorMessage = "Der Vorname darf höchstens {1} Zeichen lang sein.")]
        [Display(Name = "Vorname")]
        public string Vorname { get; set; } = null!;

        [Required(ErrorMessage = "Bitte Nachnamen eingeben.")]
        [StringLength(250, ErrorMessage = "Der Nachname darf höchstens {1} Zeichen lang sein.")]
        [Display(Name = "Nachname")]
        public string Nachname { get; set; } = null!;

        [Required(ErrorMessage = "Bitte Benutzernamen eingeben.")]
        [StringLength(20, ErrorMessage = "Der Benutzername darf höchstens {1} Zeichen lang sein.")]
        [Display(Name = "Benutzername")]
        public string Username { get; set; } = null!;

        [Required(ErrorMessage = "Bitte E-Mail-Adresse eingeben.")]
        [EmailAddress(ErrorMessage = "Bitte eine gültige E-Mail-Adresse eingeben.")]
        [StringLength(100, ErrorMessage = "Die E-Mail-Adresse darf höchstens {1} Zeichen lang sein.")]
        [Display(Name = "E-Mail")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Bitte Passwort eingeben.")]
        [DataType(DataType.Password)]
        [Display(Name = "Passwort")]
        public string Password { get; set; } = null!;

        [Required(ErrorMessage = "Bitte Passwort wiederholen.")]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Die Passwörter stimmen nicht überein.")]
        [Display(Name = "Passwort wiederholen")]
        public string PasswordWiederholung { get; set; } = null!;
    }
}
