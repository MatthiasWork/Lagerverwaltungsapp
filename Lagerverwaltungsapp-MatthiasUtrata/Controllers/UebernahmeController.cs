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
        /// Methode, die alle offenen Lagerbewegungen anzeigt, die die angemeldete Person freigeben muss: Transfers in ihren Raum
        /// (z. B. an sie ausgeborgte Gegenstände) und Geräte, die andere aus ihrem Lager holen. Darunter die zuletzt erledigten.
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

            var raumID = await _lagerService.RaumDerPersonAsync(personID);
            if (raumID == null)
            {
                return View(new UebernahmeViewModel());
            }

            var uebernahme = new UebernahmeViewModel
            {
                Raum = await _context.Raum.Include(r => r.Raumart).FirstAsync(r => r.ID == raumID),
                Offen = await _context.Lagerbewegung
                    .Include(l => l.Gegenstand)
                    .Include(l => l.Bewegungsart)
                    .Include(l => l.Person)
                    .Include(l => l.VonRaum).ThenInclude(r => r.Raumart)
                    .Include(l => l.NachRaum).ThenInclude(r => r.Raumart)
                    .Where(Lagerbewegung.IstOffen)
                    .Where(Lagerbewegung.FreigabeFuer(raumID))
                    .OrderBy(l => l.ErstelltAm)
                    .ToListAsync(),
                // Bestätigt oder storniert. Ein Wareneingang ist keine Freigabe, daher nur Bewegungen zwischen zwei Räumen
                Erledigt = await _context.Lagerbewegung
                    .Include(l => l.Gegenstand)
                    .Include(l => l.Bewegungsart)
                    .Where(l => l.BestaetigtAm != null && l.VonRaumID != l.NachRaumID)
                    .Where(Lagerbewegung.FreigabeFuer(raumID))
                    .OrderByDescending(l => l.BestaetigtAm)
                    .Take(10)
                    .ToListAsync()
            };

            return View(uebernahme);
        }

        /// <summary>
        /// Methode, mit der die angemeldete Person eine oder mehrere offene Lagerbewegungen freigibt (bestätigt).
        /// Die Geräte kommen damit in den Nach-Raum.
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
                var nachRaumIDs = await _context.Lagerbewegung.Where(l => ids.Contains(l.ID)).Select(l => l.NachRaumID).Distinct().ToListAsync();
                string wo;
                if (nachRaumIDs.Count == 1)
                {
                    wo = $"im Bestand von Raum {nachRaumIDs[0]}";
                }
                else
                {
                    wo = $"in den Räumen {string.Join(", ", nachRaumIDs.Order())}";
                }

                var anzahl = ids.Distinct().Count();
                if (anzahl == 1)
                {
                    TempData["Meldung"] = $"Die Anfrage wurde freigegeben. Das Gerät ist jetzt {wo}.";
                }
                else
                {
                    TempData["Meldung"] = $"{anzahl} Anfragen wurden freigegeben. Die Geräte sind jetzt {wo}.";
                }
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Methode, mit der die angemeldete Person eine offene Lagerbewegung ablehnt, die sie freigeben müsste.
        /// Die Menge geht zurück in den Raum, aus dem sie kam.
        /// </summary>
        /// <param name="id">Die ID der Lagerbewegung, die abgelehnt werden soll</param>
        /// <returns>Gibt eine Task zurück</returns>
        // POST: Uebernahme/Ablehnen/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ablehnen(int id)
        {
            var personID = User.GetPersonID();
            if (personID == null)
            {
                return Forbid();
            }

            var fehler = await _lagerService.AblehnenAsync(id, personID.Value);
            if (fehler != null)
            {
                TempData["Fehler"] = fehler;
            }
            else
            {
                var vonRaumID = await _context.Lagerbewegung.Where(l => l.ID == id).Select(l => l.VonRaumID).FirstAsync();
                TempData["Meldung"] = $"Die Anfrage wurde abgelehnt. Das Gerät ist wieder im Bestand von Raum {vonRaumID}.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
