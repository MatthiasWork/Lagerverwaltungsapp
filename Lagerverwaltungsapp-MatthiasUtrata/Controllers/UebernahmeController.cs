using Lagerverwaltungsapp_MatthiasUtrata.Extensions;
using Lagerverwaltungsapp_MatthiasUtrata.Models;
using Lagerverwaltungsapp_MatthiasUtrata.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Lagerverwaltungsapp_MatthiasUtrata.Controllers
{
    public class UebernahmeController : Controller
    {
        private readonly LagerverwaltungContext _context;
        private readonly LagerService _lagerService;

        /// <summary>
        /// Konstruktor der UebernahmeController-Klasse.
        /// </summary>
        /// <param name="context">Der Datenbankkontext der Lagerverwaltung</param>
        /// <param name="lagerService">Der Service, der die Zuständigkeiten kennt und die Lagerbewegungen bestätigt</param>
        public UebernahmeController(LagerverwaltungContext context, LagerService lagerService)
        {
            _context = context;
            _lagerService = lagerService;
        }

        /// <summary>
        /// Methode, die alle offenen Lagerbewegungen in den Raum der angemeldeten Person anzeigt (z. B. an sie ausgeborgte Gegenstände)
        /// und darunter die zuletzt bestätigten.
        /// </summary>
        /// <returns>Gibt eine Task zurück</returns>
        // GET: Uebernahme
        public async Task<IActionResult> Index()
        {
            var personID = User.GetPersonID();
            if (personID == null)
            {
                return Forbid();
            }

            var raumID = await _lagerService.RaumDerPersonAsync(personID.Value);

            var uebernahme = new UebernahmeViewModel
            {
                Raum = raumID == null ? null : await _context.Raum.Include(r => r.Raumart).FirstAsync(r => r.ID == raumID),
                // Über die aktuelle Zuständigkeit des Nach-Raums, wie beim Bestätigen im LagerService
                Offen = await _context.Lagerbewegung
                    .Include(l => l.Gegenstand)
                    .Include(l => l.Bewegungsart)
                    .Include(l => l.Person)
                    .Include(l => l.VonRaum).ThenInclude(r => r.Raumart)
                    .Include(l => l.NachRaum).ThenInclude(r => r.Raumart)
                    .Where(l => l.BestaetigtAm == null && l.NachRaum.PersonID == personID)
                    .OrderBy(l => l.ErstelltAm)
                    .ToListAsync(),
                // Ein Wareneingang ist keine Übernahme, daher nur Bewegungen aus einem anderen Raum
                Erledigt = await _context.Lagerbewegung
                    .Include(l => l.Gegenstand)
                    .Include(l => l.Bewegungsart)
                    .Where(l => l.BestaetigtAm != null && l.NachRaumID == raumID && l.VonRaumID != raumID)
                    .OrderByDescending(l => l.BestaetigtAm)
                    .Take(10)
                    .ToListAsync()
            };

            return View(uebernahme);
        }

        /// <summary>
        /// Methode, mit der die angemeldete Person eine oder mehrere offene Lagerbewegungen in ihren Raum bestätigt.
        /// </summary>
        /// <param name="ids">Die IDs der Lagerbewegungen, die bestätigt werden sollen</param>
        /// <returns>Gibt eine Task zurück</returns>
        // POST: Uebernahme/Bestaetigen
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Bestaetigen(int[] ids)
        {
            var personID = User.GetPersonID();
            if (personID == null)
            {
                return Forbid();
            }

            var fehler = await _lagerService.BestaetigenAsync(ids, personID.Value);
            if (fehler != null)
            {
                TempData["Fehler"] = fehler;
            }
            else
            {
                var anzahl = ids.Distinct().Count();
                TempData["Meldung"] = anzahl == 1
                    ? "Der Transfer wurde freigegeben. Das Gerät ist jetzt im Bestand Ihres Raums."
                    : $"{anzahl} Transfers wurden freigegeben. Die Geräte sind jetzt im Bestand Ihres Raums.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
