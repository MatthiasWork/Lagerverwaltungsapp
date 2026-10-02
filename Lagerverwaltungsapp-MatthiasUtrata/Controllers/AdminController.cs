using Lagerverwaltungsapp_MatthiasUtrata.Extensions;
using Lagerverwaltungsapp_MatthiasUtrata.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Lagerverwaltungsapp_MatthiasUtrata.Controllers
{
    // Die Adminübersicht ist nur für Admins sichtbar
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly LagerverwaltungContext _context;

        /// <summary>
        /// Konstruktor der AdminController-Klasse.
        /// </summary>
        /// <param name="context">Der Datenbankkontext der Lagerverwaltung</param>
        public AdminController(LagerverwaltungContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Methode, die die Benutzerverwaltung anzeigt, in der man nach Suchbegriff und Rolle filtern kann.
        /// </summary>
        /// <param name="suche">Suchbegriff für Vorname, Nachname, Benutzername oder E-Mail</param>
        /// <param name="rolleID">Die ID der Rolle, nach der gefiltert werden soll</param>
        /// <param name="id">Die ID des Benutzers, der rechts angezeigt wird (ohne: der erste der Liste)</param>
        /// <returns>Gibt eine Task zurück</returns>
        // GET: Admin
        public async Task<IActionResult> Index(string? suche, int? rolleID, int? id)
        {
            suche = suche?.Trim();

            var abfrage = _context.Person
                .Include(p => p.Rolle)
                .Include(p => p.Raum).ThenInclude(r => r.Raumart)
                .AsQueryable();

            if (!string.IsNullOrEmpty(suche))
            {
                abfrage = abfrage.Where(p => p.Vorname.Contains(suche)
                    || p.Nachname.Contains(suche)
                    || p.Username.Contains(suche)
                    || p.Email.Contains(suche));
            }

            if (rolleID != null)
            {
                abfrage = abfrage.Where(p => p.RolleID == rolleID);
            }

            var benutzer = await abfrage.OrderBy(p => p.Nachname).ThenBy(p => p.Vorname).ToListAsync();

            var uebersicht = new AdminUebersichtViewModel
            {
                AnzahlBenutzer = await _context.Person.CountAsync(),
                AnzahlJeRolle = await _context.Person
                    .GroupBy(p => p.RolleID)
                    .Select(g => new { RolleID = g.Key, Anzahl = g.Count() })
                    .ToDictionaryAsync(g => g.RolleID, g => g.Anzahl),
                Rollen = await _context.Rolle.OrderBy(r => r.Name).ToListAsync(),
                Benutzer = benutzer,
                OffeneTransfers = await _context.Lagerbewegung
                    .Where(Lagerbewegung.IstOffen)
                    .GroupBy(l => l.PersonID)
                    .Select(g => new { PersonID = g.Key, Anzahl = g.Count() })
                    .ToDictionaryAsync(g => g.PersonID, g => g.Anzahl),
                Ausgewaehlt = benutzer.FirstOrDefault(p => p.ID == id) ?? benutzer.FirstOrDefault(),
                Suche = suche,
                RolleID = rolleID,
                AngemeldeteBenutzerID = User.GetPersonID()
            };

            return View(uebersicht);
        }
    }
}
