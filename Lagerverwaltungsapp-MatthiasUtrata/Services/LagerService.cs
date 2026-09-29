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
        /// Methode, die den Raum ermittelt, für den eine Person verantwortlich ist (z. B. für das Menü).
        /// Eine Person ist für höchstens einen Raum verantwortlich.
        /// </summary>
        /// <param name="personID">Die ID der Person</param>
        /// <returns>Die ID ihres Raums oder null, wenn sie für keinen Raum verantwortlich ist</returns>
        public async Task<string?> RaumDerPersonAsync(int personID)
        {
            return await _context.Raum
                .Where(r => r.PersonID == personID)
                .Select(r => r.ID)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Methode, die sucht, ob eine Person schon für einen anderen Raum verantwortlich ist.
        /// Wird vor dem Zuweisen eines Raums aufgerufen, da eine Person nur für einen Raum verantwortlich sein kann.
        /// </summary>
        /// <param name="personID">Die ID der Person</param>
        /// <param name="raumID">Die ID des Raums, der der Person zugewiesen werden soll</param>
        /// <returns>Die ID des anderen Raums oder null, wenn die Person noch keinen anderen Raum hat</returns>
        public async Task<string?> AndererRaumAsync(int personID, string raumID)
        {
            return await _context.Raum
                .Where(r => r.PersonID == personID && r.ID != raumID)
                .Select(r => r.ID)
                .FirstOrDefaultAsync();
        }
    }
}
