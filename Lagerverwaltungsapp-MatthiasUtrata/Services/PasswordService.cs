using Lagerverwaltungsapp_MatthiasUtrata.Models;
using Microsoft.AspNetCore.Identity;

namespace Lagerverwaltungsapp_MatthiasUtrata.Services
{
    
    public class PasswordService
    {
        private readonly IPasswordHasher<Person> _passwordHasher;
        private readonly string _pepper;

        /// <summary>
        /// Konstruktor für den PasswordService.
        /// </summary>
        /// <param name="passwordHasher">Der PasswordHasher</param>
        /// <param name="configuration">Die Konfiguration aus der appsettings.json</param>
        /// <exception cref="InvalidOperationException">Exception, falls der Pepper in appsettings.json fehlt</exception>
        public PasswordService(IPasswordHasher<Person> passwordHasher, IConfiguration configuration)
        {
            _passwordHasher = passwordHasher;
            _pepper = configuration["Password:Pepper"]
                ?? throw new InvalidOperationException("Password:Pepper fehlt in der appsettings.json");
        }

        /// <summary>
        /// Methode, die ein Passwort mit dem Pepper hasht und zurückgibt.
        /// </summary>
        /// <param name="person">Die Person, für die das Passwort gehasht werden soll</param>
        /// <param name="password">Das Passwort, das gehasht werden soll</param>
        /// <returns>Der Hashwert des Passworts</returns>
        public string HashPassword(Person person, string password)
        {
            return _passwordHasher.HashPassword(person, password + _pepper);
        }

        /// <summary>
        /// Methode, die überprüft, ob das eingegebene Passwort mit dem gespeicherten Passwort des Benutzers übereinstimmt.
        /// </summary>
        /// <param name="person">Die Person, für die das Passwort verifiziert werden soll</param>
        /// <param name="password">Das Passwort, das verifiziert werden soll</param>
        /// <returns>Gibt True oder False zurück</returns>
        public bool VerifyPassword(Person person, string password)
        {
            try
            {
                var result = _passwordHasher.VerifyHashedPassword(person, person.Password, password + _pepper);
                return result != PasswordVerificationResult.Failed;
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
