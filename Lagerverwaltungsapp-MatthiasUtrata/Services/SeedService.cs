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
        // IDs der beiden Lager, falls sie neu angelegt werden müssen
        private const string HauptlagerID = "HL";
        private const string UmbuchungslagerID = "UL";

        private readonly LagerverwaltungContext _context;
        private readonly PasswordService _passwordService;

        /// <summary>
        /// Konstruktor für den SeedService.
        /// </summary>
        /// <param name="context">Der Datenbankkontext der Lagerverwaltung</param>
        /// <param name="passwordService">Der Service zum Hashen des Admin-Passworts</param>
        public SeedService(LagerverwaltungContext context, PasswordService passwordService)
        {
            _context = context;
            _passwordService = passwordService;
        }

        /// <summary>
        /// Methode, die alle fehlenden Stammdaten anlegt. Die Reihenfolge ist wichtig,
        /// da die Lager eine Raumart und einen Admin als verantwortliche Person brauchen.
        /// </summary>
        /// <returns>Gibt eine Task zurück</returns>
        public async Task SeedAsync()
        {
            await AdminAnlegenAsync();
            await LehrerInRolleAnlegenAsync();
            await RaumartenAnlegenAsync();
            await BewegungsartenAnlegenAsync();
            await LagerAnlegenAsync();
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
        /// Methode, die die Raumarten Hauptlager, Umbuchungslager und Labor anlegt, sofern sie noch fehlen.
        /// </summary>
        /// <returns>Gibt eine Task zurück</returns>
        private async Task RaumartenAnlegenAsync()
        {
            foreach (var name in Raumart.Systemeintraege)
            {
                if (!await _context.Raumart.AnyAsync(r => r.Name == name))
                {
                    _context.Raumart.Add(new Raumart { Name = name });
                }
            }
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Methode, die die Bewegungsarten Wareneingang, Ausgabe, Rückgabe und Einlagerung anlegt, sofern sie noch fehlen.
        /// </summary>
        /// <returns>Gibt eine Task zurück</returns>
        private async Task BewegungsartenAnlegenAsync()
        {
            foreach (var name in Bewegungsart.Systemeintraege)
            {
                if (!await _context.Bewegungsart.AnyAsync(b => b.Name == name))
                {
                    _context.Bewegungsart.Add(new Bewegungsart { Name = name });
                }
            }
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Methode, die das Hauptlager und das Umbuchungslager anlegt, falls es noch keinen Raum dieser Raumart gibt.
        /// Verantwortlich ist zunächst der erste Admin; das kann später in der Raumverwaltung geändert werden.
        /// </summary>
        /// <returns>Gibt eine Task zurück</returns>
        private async Task LagerAnlegenAsync()
        {
            // Jeder Raum braucht eine verantwortliche Person. Ohne Admin werden die Lager erst beim nächsten Start angelegt.
            var admin = await _context.Person
                .Where(p => p.Rolle.Admin)
                .OrderBy(p => p.ID)
                .FirstOrDefaultAsync();
            if (admin == null)
            {
                return;
            }

            await LagerAnlegenAsync(HauptlagerID, Raumart.Hauptlager, admin.ID);
            await LagerAnlegenAsync(UmbuchungslagerID, Raumart.Umbuchungslager, admin.ID);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Methode, die einen Raum der angegebenen Raumart anlegt, sofern es noch keinen gibt.
        /// </summary>
        /// <param name="raumID">Die ID des neuen Raums</param>
        /// <param name="raumartName">Der Name der Raumart, von der es genau einen Raum geben soll</param>
        /// <param name="personID">Die ID der verantwortlichen Person</param>
        /// <returns>Gibt eine Task zurück</returns>
        private async Task LagerAnlegenAsync(string raumID, string raumartName, int personID)
        {
            if (await _context.Raum.AnyAsync(r => r.Raumart.Name == raumartName))
            {
                return;
            }

            var raumart = await _context.Raumart.FirstAsync(r => r.Name == raumartName);
            _context.Raum.Add(new Raum { ID = raumID, RaumartID = raumart.ID, PersonID = personID });
        }
    }
}
