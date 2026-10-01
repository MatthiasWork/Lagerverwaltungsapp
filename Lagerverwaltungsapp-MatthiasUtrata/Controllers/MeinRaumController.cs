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
        /// Methode, die den Raum der angemeldeten Person mit seinem Bestand anzeigt, dazu die ausgeborgten Gegenstände,
        /// deren Übernahme noch nicht bestätigt ist. Der Bestand kann nach Suchbegriff und Kategorie gefiltert werden.
        /// </summary>
        /// <param name="suche">Suchbegriff für Bezeichnung oder Seriennummer</param>
        /// <param name="kategorieID">Die ID der Kategorie, nach der gefiltert werden soll</param>
        /// <returns>Gibt eine Task zurück</returns>
        // GET: MeinRaum
        public async Task<IActionResult> Index(string? suche, int? kategorieID)
        {
            var raumID = await EigenerRaumAsync();
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
                // Ein Wareneingang ist immer sofort bestätigt, offene Bewegungen aus dem Raum sind also immer ausgeborgt
                Unterwegs = await _context.Lagerbewegung
                    .Include(l => l.Gegenstand)
                    .Include(l => l.Bewegungsart)
                    .Include(l => l.NachRaum).ThenInclude(r => r.Person)
                    .Where(l => l.BestaetigtAm == null && l.VonRaumID == raumID)
                    .OrderByDescending(l => l.ErstelltAm)
                    .ToListAsync(),
                OffeneUebernahmen = await _context.Lagerbewegung.CountAsync(l => l.BestaetigtAm == null && l.NachRaumID == raumID),
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
        /// Methode, die das Formular zum Ausborgen von Gegenständen aus dem eigenen Raum an einen anderen Raum anzeigt.
        /// </summary>
        /// <returns>Gibt eine Task zurück</returns>
        // GET: MeinRaum/Ausborgen
        public async Task<IActionResult> Ausborgen()
        {
            var raumID = await EigenerRaumAsync();
            if (raumID == null)
            {
                return Forbid();
            }

            var ausborgen = new AusborgenViewModel
            {
                BewegungsartID = await _context.Bewegungsart
                    .Where(b => b.Name == Bewegungsart.Ausgabe)
                    .Select(b => (int?)b.ID)
                    .FirstOrDefaultAsync()
            };
            await AnzeigeSetzenAsync(ausborgen, raumID);
            return View(ausborgen);
        }

        /// <summary>
        /// Methode, die die ausgewählten Gegenstände an den gewählten Raum ausborgt.
        /// </summary>
        /// <param name="ausborgen">Das AusborgenViewModel mit Zielraum, Bewegungsart und Mengen aus dem Formular</param>
        /// <returns>Gibt eine Task zurück</returns>
        // POST: MeinRaum/Ausborgen
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ausborgen([Bind("NachRaumID,BewegungsartID,Mengen")] AusborgenViewModel ausborgen)
        {
            var personID = User.GetPersonID();
            var raumID = await EigenerRaumAsync();
            if (personID == null || raumID == null)
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                // Ein leeres Feld oder 0 heißt "nicht ausborgen". Negative Mengen bleiben drin, damit der LagerService sie ablehnt
                var positionen = ausborgen.Mengen
                    .Where(m => m.Value != null && m.Value != 0)
                    .ToDictionary(m => m.Key, m => m.Value!.Value);

                var fehler = await _lagerService.AusborgenAsync(positionen, raumID, ausborgen.NachRaumID!, ausborgen.BewegungsartID!.Value, personID.Value);
                if (fehler == null)
                {
                    var nachRaum = await _context.Raum.Include(r => r.Person).FirstAsync(r => r.ID == ausborgen.NachRaumID);
                    var wartetAuf = nachRaum.Person == null ? "die verantwortliche Person" : $"{nachRaum.Person.Vorname} {nachRaum.Person.Nachname}";
                    var anzahl = positionen.Count == 1 ? "1 Gerät" : $"{positionen.Count} Geräte";
                    TempData["Meldung"] = $"Transfer von {anzahl} nach Raum {nachRaum.ID} angefragt. Bis {wartetAuf} ihn freigibt, sind die Geräte unterwegs.";
                    return RedirectToAction(nameof(Index));
                }
                ModelState.AddModelError(string.Empty, fehler);
            }

            await AnzeigeSetzenAsync(ausborgen, raumID);
            return View(ausborgen);
        }

        /// <summary>
        /// Methode, die das Formular für einen Wareneingang in den eigenen Raum anzeigt.
        /// </summary>
        /// <param name="gegenstandID">Die ID des Gegenstands, der vorausgewählt werden soll (z. B. aus dem Gerätedetail)</param>
        /// <returns>Gibt eine Task zurück</returns>
        // GET: MeinRaum/Wareneingang
        public async Task<IActionResult> Wareneingang(int? gegenstandID)
        {
            var raumID = await EigenerRaumAsync();
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
        /// da die Person für den Raum verantwortlich ist, und steht danach in der Historie des Gegenstands.
        /// </summary>
        /// <param name="wareneingang">Das WareneingangViewModel mit Gegenstand, Menge und Bewegungsart aus dem Formular</param>
        /// <returns>Gibt eine Task zurück</returns>
        // POST: MeinRaum/Wareneingang
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Wareneingang([Bind("GegenstandID,Menge,BewegungsartID")] WareneingangViewModel wareneingang)
        {
            var personID = User.GetPersonID();
            var raumID = await EigenerRaumAsync();
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
                    var was = gegenstand.Seriennummer != null ? $"\"{gegenstand.Name}\" ({gegenstand.Seriennummer})" : $"{wareneingang.Menge} Stück \"{gegenstand.Name}\"";
                    TempData["Meldung"] = $"{was} {(wareneingang.Menge == 1 ? "wurde" : "wurden")} in Raum {raumID} eingebucht.";
                    return RedirectToAction(nameof(Index));
                }
                ModelState.AddModelError(string.Empty, fehler);
            }

            await AnzeigeSetzenAsync(wareneingang, raumID);
            return View(wareneingang);
        }

        /// <summary>
        /// Methode, die den Raum ermittelt, für den die angemeldete Person gerade verantwortlich ist.
        /// </summary>
        /// <returns>Die ID des Raums oder null, wenn die Person für keinen Raum verantwortlich ist</returns>
        private async Task<string?> EigenerRaumAsync()
        {
            var personID = User.GetPersonID();
            return personID == null ? null : await _lagerService.RaumDerPersonAsync(personID.Value);
        }

        /// <summary>
        /// Methode, die alles für die Anzeige des Formulars zum Ausborgen setzt.
        /// </summary>
        /// <param name="ausborgen">Das AusborgenViewModel, das angezeigt werden soll</param>
        /// <param name="raumID">Die ID des eigenen Raums, aus dem ausgeborgt wird</param>
        /// <returns>Gibt eine Task zurück</returns>
        private async Task AnzeigeSetzenAsync(AusborgenViewModel ausborgen, string raumID)
        {
            ausborgen.VonRaum = await _context.Raum.Include(r => r.Raumart).Include(r => r.Person).FirstAsync(r => r.ID == raumID);

            ausborgen.Bestand = await _context.Raumbestand
                .Include(r => r.Gegenstand).ThenInclude(g => g.Kategorie)
                .Where(r => r.RaumID == raumID)
                .OrderBy(r => r.Gegenstand.Name).ThenBy(r => r.Gegenstand.Seriennummer)
                .ToListAsync();

            ausborgen.Raeume = await _context.Raum
                .Include(r => r.Raumart)
                .Include(r => r.Person)
                .Where(r => r.ID != raumID && r.PersonID != null)
                .OrderBy(r => r.ID)
                .ToListAsync();

            ausborgen.Bewegungsarten = new SelectList(await _context.Bewegungsart.OrderBy(b => b.Name).ToListAsync(), "ID", "Name", ausborgen.BewegungsartID);
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

            wareneingang.Bewegungsarten = new SelectList(await _context.Bewegungsart.OrderBy(b => b.Name).ToListAsync(), "ID", "Name", wareneingang.BewegungsartID);
        }
    }
}
