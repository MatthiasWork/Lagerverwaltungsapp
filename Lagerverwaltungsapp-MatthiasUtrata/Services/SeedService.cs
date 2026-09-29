using Lagerverwaltungsapp_MatthiasUtrata.Models;
using Microsoft.EntityFrameworkCore;

namespace Lagerverwaltungsapp_MatthiasUtrata.Services
{
    /// <summary>
    /// Legt beim Start alle Daten an, ohne die die Anwendung nicht funktioniert.
    /// Es wird nur angelegt, was noch fehlt, daher kann der Seed bei jedem Start laufen.
    /// </summary>
    public class SeedService
    {
        private readonly LagerverwaltungContext _context;
        private readonly PasswordService _passwordService;

        /// <summary>
        /// Konstruktor für den SeedService.
        /// </summary>
        /// <param name="context">Der Datenbankkontext der Lagerverwaltung</param>
        /// <param name="passwordService">Der Service zum Hashen der Start-Passwörter</param>
        public SeedService(LagerverwaltungContext context, PasswordService passwordService)
        {
            _context = context;
            _passwordService = passwordService;
        }

        /// <summary>
        /// Methode, die alle fehlenden Stammdaten und bei der Ersteinrichtung Beispieldaten anlegt. Die Reihenfolge ist wichtig,
        /// da die Beispielräume eine Raumart und die LehrerIn-Rolle für ihre verantwortliche Person brauchen.
        /// </summary>
        /// <returns>Gibt eine Task zurück</returns>
        public async Task SeedAsync()
        {
            await AdminAnlegenAsync();
            await LehrerInRolleAnlegenAsync();
            await RaumartenAnlegenAsync();
            await BewegungsartenAnlegenAsync();
            await RaeumeAnlegenAsync();
        }

        /// <summary>
        /// Methode, die bei der Ersteinrichtung einen Admin anlegt, falls es noch keine Person gibt,
        /// damit man sich überhaupt anmelden kann (Passwort danach ändern!).
        /// </summary>
        /// <returns>Gibt eine Task zurück</returns>
        private async Task AdminAnlegenAsync()
        {
            if (await _context.Person.AnyAsync())
            {
                return;
            }

            var adminRolle = await _context.Rolle.FirstOrDefaultAsync(r => r.Admin)
                ?? new Rolle { Name = "Administrator", Admin = true };

            var admin = new Person
            {
                Vorname = "Admin",
                Nachname = "Admin",
                Username = "admin",
                Email = "admin@lagerverwaltung.local",
                Rolle = adminRolle
            };
            admin.Password = _passwordService.HashPassword(admin, "admin");

            _context.Person.Add(admin);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Methode, die die Standardrolle für alle anlegt, die keine Admins sind.
        /// Neu registrierte Benutzer bekommen sie automatisch.
        /// </summary>
        /// <returns>Gibt eine Task zurück</returns>
        private async Task LehrerInRolleAnlegenAsync()
        {
            if (!await _context.Rolle.AnyAsync(r => r.Name == Rolle.LehrerIn))
            {
                _context.Rolle.Add(new Rolle { Name = Rolle.LehrerIn, Admin = false });
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Methode, die bei der Ersteinrichtung Beispiel-Raumarten anlegt, falls es noch keine Raumart gibt.
        /// Eine Raumart beschreibt einen Raum nur näher und hat keinen Einfluss auf den Ablauf.
        /// </summary>
        /// <returns>Gibt eine Task zurück</returns>
        private async Task RaumartenAnlegenAsync()
        {
            // Nur bei der Ersteinrichtung, sonst kämen umbenannte oder gelöschte Raumarten beim nächsten Start wieder
            if (await _context.Raumart.AnyAsync())
            {
                return;
            }

            foreach (var name in new[] { "Hauptlager", "Umbuchungslager", "Labor" })
            {
                _context.Raumart.Add(new Raumart { Name = name });
            }
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Methode, die bei der Ersteinrichtung Beispiel-Bewegungsarten anlegt, falls es noch keine Bewegungsart gibt.
        /// Eine Bewegungsart beschreibt eine Lagerbewegung nur näher und gibt keine Regeln für den Ablauf vor.
        /// </summary>
        /// <returns>Gibt eine Task zurück</returns>
        private async Task BewegungsartenAnlegenAsync()
        {
            // Nur bei der Ersteinrichtung, sonst kämen umbenannte oder gelöschte Bewegungsarten beim nächsten Start wieder
            if (await _context.Bewegungsart.AnyAsync())
            {
                return;
            }

            foreach (var name in new[] { "Wareneingang", "Ausgabe", "Rueckgabe", "Einlagerung" })
            {
                _context.Bewegungsart.Add(new Bewegungsart { Name = name });
            }
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Methode, die bei der Ersteinrichtung Beispielräume anlegt, falls es noch keinen Raum gibt.
        /// Da eine Person nur für einen Raum verantwortlich sein kann, bekommt jeder Raum eine eigene LehrerIn.
        /// Die Zuständigkeit kann später in der Raumverwaltung geändert werden.
        /// </summary>
        /// <returns>Gibt eine Task zurück</returns>
        private async Task RaeumeAnlegenAsync()
        {
            // Nur bei der Ersteinrichtung, sonst kämen gelöschte Räume beim nächsten Start wieder
            if (await _context.Raum.AnyAsync())
            {
                return;
            }

            await RaumAnlegenAsync("HL", "Hauptlager", "hauptlager");
            await RaumAnlegenAsync("UL", "Umbuchungslager", "umbuchungslager");
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Methode, die einen Raum samt verantwortlicher LehrerIn anlegt (Passwort = Benutzername, danach ändern!).
        /// </summary>
        /// <param name="raumID">Die ID des neuen Raums</param>
        /// <param name="raumartName">Der Name der Raumart, die den Raum beschreibt</param>
        /// <param name="username">Der Benutzername der verantwortlichen LehrerIn</param>
        /// <returns>Gibt eine Task zurück</returns>
        private async Task RaumAnlegenAsync(string raumID, string raumartName, string username)
        {
            // Die Raumart kann inzwischen umbenannt oder gelöscht worden sein, dann wird sie neu angelegt
            var raumart = await _context.Raumart.FirstOrDefaultAsync(r => r.Name == raumartName)
                ?? new Raumart { Name = raumartName };

            // Da es noch keinen Raum gibt, hat auch keine Person einen Raum. Eine vorhandene Person kann also übernommen werden
            var person = await _context.Person.FirstOrDefaultAsync(p => p.Username == username);
            if (person == null)
            {
                person = new Person
                {
                    Vorname = "Lehrer",
                    Nachname = raumartName,
                    Username = username,
                    Email = $"{username}@lagerverwaltung.local",
                    Rolle = await _context.Rolle.FirstAsync(r => r.Name == Rolle.LehrerIn)
                };
                person.Password = _passwordService.HashPassword(person, username);
            }

            _context.Raum.Add(new Raum { ID = raumID, Raumart = raumart, Person = person });
        }
    }
}
