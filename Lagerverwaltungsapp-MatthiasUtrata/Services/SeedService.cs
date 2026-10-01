using Lagerverwaltungsapp_MatthiasUtrata.Models;
using Microsoft.EntityFrameworkCore;

namespace Lagerverwaltungsapp_MatthiasUtrata.Services
{
    /// <summary>
    /// Legt beim Start alle Daten an, ohne die die Anwendung nicht funktioniert.
    /// Es wird nur angelegt, was noch fehlt, daher kann der Seed bei jedem Start laufen.
    /// Benutzer legt der Seed keine an: Solange es keinen gibt, registriert sich der erste Benutzer als Admin.
    /// </summary>
    public class SeedService
    {
        private readonly LagerverwaltungContext _context;

        /// <summary>
        /// Konstruktor für den SeedService.
        /// </summary>
        /// <param name="context">Der Datenbankkontext der Lagerverwaltung</param>
        public SeedService(LagerverwaltungContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Methode, die alle fehlenden Stammdaten und bei der Ersteinrichtung Beispieldaten anlegt. Die Reihenfolge ist wichtig,
        /// da die Beispielräume eine Raumart brauchen.
        /// </summary>
        /// <returns>Gibt eine Task zurück</returns>
        public async Task SeedAsync()
        {
            await LehrerInRolleAnlegenAsync();
            await RaumartenAnlegenAsync();
            await BewegungsartenAnlegenAsync();
            await FesteBewegungsartenAnlegenAsync();
            await RaeumeAnlegenAsync();
        }

        /// <summary>
        /// Methode, die die Standardrolle für alle anlegt, die keine Admins sind.
        /// Der Admin kann sie beim Anlegen neuer Benutzer vergeben.
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
        /// Methode, die die Bewegungsarten anlegt, die nur der LagerService vergibt: "Storniert" für abgelehnte und zurückgezogene
        /// Transfers und "Korrektur" für Korrekturbuchungen im Lagerbestand. Anders als die Beispiel-Bewegungsarten bei jedem Start,
        /// da der LagerService sie braucht. Sie müssen nach den Beispiel-Bewegungsarten kommen, sonst gäbe es schon eine Bewegungsart
        /// und diese würden übersprungen.
        /// </summary>
        /// <returns>Gibt eine Task zurück</returns>
        private async Task FesteBewegungsartenAnlegenAsync()
        {
            foreach (var name in new[] { Bewegungsart.Storniert, Bewegungsart.Korrektur })
            {
                if (!await _context.Bewegungsart.AnyAsync(b => b.Name == name))
                {
                    _context.Bewegungsart.Add(new Bewegungsart { Name = name });
                }
            }
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Methode, die bei der Ersteinrichtung Beispielräume anlegt, falls es noch keinen Raum gibt.
        /// Die Räume haben noch keine verantwortliche Person, die vergibt der Admin später in der Raumverwaltung.
        /// </summary>
        /// <returns>Gibt eine Task zurück</returns>
        private async Task RaeumeAnlegenAsync()
        {
            // Nur bei der Ersteinrichtung, sonst kämen gelöschte Räume beim nächsten Start wieder
            if (await _context.Raum.AnyAsync())
            {
                return;
            }

            await RaumAnlegenAsync("HL", "Hauptlager");
            await RaumAnlegenAsync("UL", "Umbuchungslager");
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Methode, die einen Raum ohne verantwortliche Person anlegt.
        /// </summary>
        /// <param name="raumID">Die ID des neuen Raums</param>
        /// <param name="raumartName">Der Name der Raumart, die den Raum beschreibt</param>
        /// <returns>Gibt eine Task zurück</returns>
        private async Task RaumAnlegenAsync(string raumID, string raumartName)
        {
            // Die Raumart kann inzwischen umbenannt oder gelöscht worden sein, dann wird sie neu angelegt
            var raumart = await _context.Raumart.FirstOrDefaultAsync(r => r.Name == raumartName)
                ?? new Raumart { Name = raumartName };

            _context.Raum.Add(new Raum { ID = raumID, Raumart = raumart });
        }
    }
}
