using Lagerverwaltungsapp_MatthiasUtrata.Extensions;
using Lagerverwaltungsapp_MatthiasUtrata.Models;
using Lagerverwaltungsapp_MatthiasUtrata.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Lagerverwaltungsapp_MatthiasUtrata.Controllers
{
    public class MeinRaumController : Controller
    {
        private readonly LagerverwaltungContext _context;
        private readonly LagerService _lagerService;

        /// <summary>
        /// Konstruktor der MeinRaumController-Klasse.
        /// </summary>
        /// <param name="context">Der Datenbankkontext der Lagerverwaltung</param>
        /// <param name="lagerService">Der Service, der die Zuständigkeiten kennt und die Lagerbewegungen bucht</param>
        public MeinRaumController(LagerverwaltungContext context, LagerService lagerService)
        {
            _context = context;
            _lagerService = lagerService;
        }

        /// <summary>
        /// Methode, die den Raum der angemeldeten Person mit seinem Bestand anzeigt, dazu die für den Raum angefragten
        /// Lagerbewegungen, die noch nicht freigegeben sind.
        /// Der Bestand kann nach Suchbegriff und Kategorie gefiltert werden.
        /// </summary>
        /// <param name="suche">Suchbegriff für Bezeichnung oder Seriennummer</param>
        /// <param name="kategorieID">Die ID der Kategorie, nach der gefiltert werden soll</param>
        /// <returns>Gibt eine Task zurück</returns>
        // GET: MeinRaum
        public async Task<IActionResult> Index(string? suche, int? kategorieID)
        {
            var raumID = await _lagerService.RaumDerPersonAsync(User.GetPersonID());
            if (raumID == null)
            {
                return Forbid();
            }

            suche = suche?.Trim();

            var abfrage = _context.Raumbestand
                .Include(r => r.Gegenstand).ThenInclude(g => g.Kategorie)
                .Where(r => r.RaumID == raumID);

            if (!string.IsNullOrEmpty(suche))
            {
                abfrage = abfrage.Where(r => r.Gegenstand.Name.Contains(suche)
                    || (r.Gegenstand.Seriennummer != null && r.Gegenstand.Seriennummer.Contains(suche)));
            }

            if (kategorieID != null)
            {
                abfrage = abfrage.Where(r => r.Gegenstand.KategorieID == kategorieID);
            }

            var meinRaum = new MeinRaumViewModel
            {
                Raum = await _context.Raum.Include(r => r.Raumart).FirstAsync(r => r.ID == raumID),
                Bestand = await abfrage.OrderBy(r => r.Gegenstand.Name).ThenBy(r => r.Gegenstand.Seriennummer).ToListAsync(),
                AnzahlGesamt = await _context.Raumbestand.CountAsync(r => r.RaumID == raumID),
                StueckGesamt = await _context.Raumbestand.Where(r => r.RaumID == raumID).SumAsync(r => r.Menge),
                // Was die Person angefragt hat und daher zurückziehen kann. Ein Wareneingang ist immer sofort bestätigt
                Unterwegs = await _context.Lagerbewegung
                    .Include(l => l.Gegenstand)
                    .Include(l => l.Bewegungsart)
                    .Include(l => l.VonRaum).ThenInclude(r => r.Person)
                    .Include(l => l.NachRaum).ThenInclude(r => r.Person)
                    .Where(Lagerbewegung.IstOffen)
                    .Where(Lagerbewegung.AngefragtFuer(raumID))
                    .OrderByDescending(l => l.ErstelltAm)
                    .ToListAsync(),
                OffeneUebernahmen = await _context.Lagerbewegung
                    .Where(Lagerbewegung.IstOffen)
                    .Where(Lagerbewegung.FreigabeFuer(raumID))
                    .CountAsync(),
                Suche = suche,
                KategorieID = kategorieID,
                // Nur Kategorien, von denen es im Raum auch etwas gibt
                Kategorien = new SelectList(await _context.Kategorie
                    .Where(k => k.Gegenstand.Any(g => g.Raumbestand.Any(r => r.RaumID == raumID)))
                    .OrderBy(k => k.Name)
                    .ToListAsync(), "ID", "Name", kategorieID)
            };

            return View(meinRaum);
        }

        /// <summary>
        /// Methode, mit der die angemeldete Person eine Lagerbewegung zurückzieht, die sie angefragt hat 
        /// </summary>
        /// <param name="id">Die ID der Lagerbewegung, die zurückgezogen werden soll</param>
        /// <returns>Gibt eine Task zurück</returns>
        // POST: MeinRaum/Zurueckziehen/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Zurueckziehen(int id)
        {
            var personID = User.GetPersonID();
            if (personID == null)
            {
                return Forbid();
            }

            var fehler = await _lagerService.ZurueckziehenAsync(id, personID.Value);
            if (fehler != null)
            {
                TempData["Fehler"] = fehler;
            }
            else
            {
                var vonRaumID = await _context.Lagerbewegung.Where(l => l.ID == id).Select(l => l.VonRaumID).FirstAsync();
                TempData["Meldung"] = $"Die Anfrage wurde zurückgezogen. Das Gerät ist wieder im Bestand von Raum {vonRaumID}.";
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Methode, die das Formular für einen Wareneingang in den eigenen Raum anzeigt.
        /// </summary>
        /// <param name="gegenstandID">Die ID des Gegenstands, der vorausgewählt werden soll (z. B. aus dem Gerätedetail)</param>
        /// <returns>Gibt eine Task zurück</returns>
        // GET: MeinRaum/Wareneingang
        public async Task<IActionResult> Wareneingang(int? gegenstandID)
        {
            var raumID = await _lagerService.RaumDerPersonAsync(User.GetPersonID());
            if (raumID == null)
            {
                return Forbid();
            }

            var wareneingang = new WareneingangViewModel
            {
                GegenstandID = gegenstandID,
                BewegungsartID = await _context.Bewegungsart
                    .Where(b => b.Name == Bewegungsart.Wareneingang)
                    .Select(b => (int?)b.ID)
                    .FirstOrDefaultAsync()
            };
            await AnzeigeSetzenAsync(wareneingang, raumID);
            return View(wareneingang);
        }

        /// <summary>
        /// Methode, die einen Wareneingang in den eigenen Raum bucht. Die Lagerbewegung ist sofort bestätigt,
        /// da die Person für den Raum verantwortlich ist und die Ware außerhalb des Inventars hinzugefügt wird.
        /// </summary>
        /// <param name="wareneingang">Das WareneingangViewModel mit Gegenstand, Menge und Bewegungsart aus dem Formular</param>
        /// <returns>Gibt eine Task zurück</returns>
        // POST: MeinRaum/Wareneingang
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Wareneingang([Bind("GegenstandID,Menge,BewegungsartID")] WareneingangViewModel wareneingang)
        {
            var personID = User.GetPersonID();
            var raumID = await _lagerService.RaumDerPersonAsync(personID);
            if (personID == null || raumID == null)
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                var fehler = await _lagerService.WareneingangAsync(wareneingang.GegenstandID!.Value, wareneingang.Menge!.Value, raumID,
                    wareneingang.BewegungsartID!.Value, personID.Value);
                if (fehler == null)
                {
                    var gegenstand = await _context.Gegenstand.FirstAsync(g => g.ID == wareneingang.GegenstandID);
                    if (gegenstand.Seriennummer != null)
                    {
                        TempData["Meldung"] = $"\"{gegenstand.Name}\" ({gegenstand.Seriennummer}) wurde in Raum {raumID} eingebucht.";
                    }
                    else if (wareneingang.Menge == 1)
                    {
                        TempData["Meldung"] = $"1 Stück \"{gegenstand.Name}\" wurde in Raum {raumID} eingebucht.";
                    }
                    else
                    {
                        TempData["Meldung"] = $"{wareneingang.Menge} Stück \"{gegenstand.Name}\" wurden in Raum {raumID} eingebucht.";
                    }
                    return RedirectToAction(nameof(Index));
                }
                ModelState.AddModelError(string.Empty, fehler);
            }

            await AnzeigeSetzenAsync(wareneingang, raumID);
            return View(wareneingang);
        }

        /// <summary>
        /// Methode, die alles für die Anzeige des Formulars für einen Wareneingang setzt.
        /// </summary>
        /// <param name="wareneingang">Das WareneingangViewModel, das angezeigt werden soll</param>
        /// <param name="raumID">Die ID des eigenen Raums, in den eingebucht wird</param>
        /// <returns>Gibt eine Task zurück</returns>
        private async Task AnzeigeSetzenAsync(WareneingangViewModel wareneingang, string raumID)
        {
            wareneingang.Raum = await _context.Raum.Include(r => r.Raumart).FirstAsync(r => r.ID == raumID);

            // Ein Gerät mit Seriennummer gibt es nur einmal: Liegt es schon in einem Raum oder ist es unterwegs, kann es nicht eingehen
            wareneingang.Gegenstaende = await _context.Gegenstand
                .Include(g => g.Kategorie)
                .Where(g => g.Seriennummer == null
                    || (!g.Raumbestand.Any() && !g.Lagerbewegung.Any(l => l.BestaetigtAm == null)))
                .OrderBy(g => g.Name).ThenBy(g => g.Seriennummer)
                .ToListAsync();

            wareneingang.Bewegungsarten = new SelectList(await _lagerService.WaehlbareBewegungsartenAsync(), "ID", "Name", wareneingang.BewegungsartID);
        }
    }
}
