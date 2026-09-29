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
        /// Methode, die die Adminübersicht mit Kennzahlen und der Benutzerverwaltung anzeigt.
        /// Die Benutzerliste kann nach Suchbegriff und Rolle gefiltert werden.
        /// </summary>
        /// <param name="suche">Suchbegriff für Vorname, Nachname, Benutzername oder E-Mail</param>
        /// <param name="rolleID">Die ID der Rolle, nach der gefiltert werden soll</param>
        /// <returns>Gibt eine Task zurück</returns>
        // GET: Admin
        public async Task<IActionResult> Index(string? suche, int? rolleID)
        {
            suche = suche?.Trim();

            var abfrage = _context.Person.Include(p => p.Rolle).AsQueryable();

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

            var uebersicht = new AdminUebersichtViewModel
            {
                AnzahlBenutzer = await _context.Person.CountAsync(),
                AnzahlAdmins = await _context.Person.CountAsync(p => p.Rolle.Admin),
                AnzahlRollen = await _context.Rolle.CountAsync(),
                Benutzer = await abfrage.OrderBy(p => p.Nachname).ThenBy(p => p.Vorname).ToListAsync(),
                Suche = suche,
                RolleID = rolleID,
                Rollen = new SelectList(await _context.Rolle.OrderBy(r => r.Name).ToListAsync(), "ID", "Name", rolleID),
                AngemeldeteBenutzerID = User.GetPersonID()
            };

            return View(uebersicht);
        }
    }
}
