using Lagerverwaltungsapp_MatthiasUtrata.Models;
using Microsoft.EntityFrameworkCore;

namespace Lagerverwaltungsapp_MatthiasUtrata.Services
{
    /// <summary>
    /// Kern der Geschäftslogik der Lagerverwaltung. Hier wird geprüft, wer für welchen Raum verantwortlich ist.
    /// Wer eine Bewegung anlegen oder bestätigen darf, hängt nur von Raum.PersonID ab, nicht von der Admin-Rolle.
    /// </summary>
    public class LagerService
    {
        private readonly LagerverwaltungContext _context;

        /// <summary>
        /// Konstruktor für den LagerService.
        /// </summary>
        /// <param name="context">Der Datenbankkontext der Lagerverwaltung</param>
        public LagerService(LagerverwaltungContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Methode, die das Hauptlager über seine Raumart sucht.
        /// </summary>
        /// <returns>Das Hauptlager oder null, wenn es noch nicht angelegt ist</returns>
        public async Task<Raum?> HauptlagerAsync()
        {
            return await RaumNachRaumartAsync(Raumart.Hauptlager);
        }

        /// <summary>
        /// Methode, die das Umbuchungslager über seine Raumart sucht.
        /// </summary>
        /// <returns>Das Umbuchungslager oder null, wenn es noch nicht angelegt ist</returns>
        public async Task<Raum?> UmbuchungslagerAsync()
        {
            return await RaumNachRaumartAsync(Raumart.Umbuchungslager);
        }

        /// <summary>
        /// Methode, die überprüft, ob eine Person für einen Raum verantwortlich ist.
        /// Es wird immer die aktuelle Zuständigkeit aus der Datenbank verwendet.
        /// </summary>
        /// <param name="raumID">Die ID des Raums</param>
        /// <param name="personID">Die ID der Person</param>
        /// <returns>Gibt True oder False zurück</returns>
        public async Task<bool> IstVerantwortlichAsync(string raumID, int personID)
        {
            return await _context.Raum.AnyAsync(r => r.ID == raumID && r.PersonID == personID);
        }

        /// <summary>
        /// Methode, die ermittelt, für welche Raumarten eine Person verantwortlich ist (z. B. für das Menü).
        /// </summary>
        /// <param name="personID">Die ID der Person</param>
        /// <returns>Die Namen der Raumarten, für die die Person mindestens einen Raum verantwortet</returns>
        public async Task<List<string>> VerantwortlicheRaumartenAsync(int personID)
        {
            return await _context.Raum
                .Where(r => r.PersonID == personID)
                .Select(r => r.Raumart.Name)
                .Distinct()
                .ToListAsync();
        }

        /// <summary>
        /// Methode, die den (einzigen) Raum einer Raumart sucht.
        /// </summary>
        /// <param name="raumartName">Der Name der Raumart</param>
        /// <returns>Der Raum oder null, wenn es keinen gibt</returns>
        private async Task<Raum?> RaumNachRaumartAsync(string raumartName)
        {
            return await _context.Raum.FirstOrDefaultAsync(r => r.Raumart.Name == raumartName);
        }
    }
}
