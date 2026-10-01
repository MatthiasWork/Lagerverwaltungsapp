using Lagerverwaltungsapp_MatthiasUtrata.Extensions;
using Lagerverwaltungsapp_MatthiasUtrata.Models;
using Lagerverwaltungsapp_MatthiasUtrata.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Lagerverwaltungsapp_MatthiasUtrata.Controllers
{
    // Aus dem Lager holen: Die für einen Raum verantwortliche Person sucht sich ein Lager aus (Raumart mit "Lager" im Namen)
    // und fragt Geräte daraus für ihren Raum an. Das ist eine Lagerbewegung wie jede andere, nur dass hier der Nach-Raum anfragt
    // und daher die Person des Lagers freigibt (siehe Lagerbewegung.FreigabeRaum)
    public class LagerController : Controller
    {
        private readonly LagerverwaltungContext _context;
        private readonly LagerService _lagerService;

        /// <summary>
        /// Konstruktor der LagerController-Klasse.
        /// </summary>
        /// <param name="context">Der Datenbankkontext der Lagerverwaltung</param>
        /// <param name="lagerService">Der Service, der die Zuständigkeiten kennt und die Lagerbewegungen bucht</param>
        public LagerController(LagerverwaltungContext context, LagerService lagerService)
        {
            _context = context;
            _lagerService = lagerService;
        }

        /// <summary>
        /// Methode, die die Lager zur Auswahl und den Bestand des gewählten Lagers anzeigt.
        /// </summary>
        /// <param name="id">Die ID des gewählten Lagers; ohne ID das erste</param>
        /// <returns>Gibt eine Task zurück</returns>
        // GET: Lager, Lager/Index/HL
        public async Task<IActionResult> Index(string? id)
        {
            var raumID = await _lagerService.RaumDerPersonAsync(User.GetPersonID());
            if (raumID == null)
            {
                return Forbid();
            }

            var holen = new HolenViewModel
            {
                VonRaumID = id,
                BewegungsartID = await _context.Bewegungsart
                    .Where(b => b.Name == Bewegungsart.Ausgabe)
                    .Select(b => (int?)b.ID)
                    .FirstOrDefaultAsync()
            };
            await AnzeigeSetzenAsync(holen, raumID);
            return View(holen);
        }

        /// <summary>
        /// Methode, die die ausgewählten Geräte aus dem Lager für den eigenen Raum anfragt. Sie werden sofort im Lager abgebucht
        /// (reserviert) und kommen in den eigenen Raum, sobald die Person des Lagers unter "Freigaben" freigibt.
        /// </summary>
        /// <param name="holen">Das HolenViewModel mit Lager, Bewegungsart und Mengen aus dem Formular</param>
        /// <returns>Gibt eine Task zurück</returns>
        // POST: Lager
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index([Bind("VonRaumID,BewegungsartID,Mengen")] HolenViewModel holen)
        {
            var personID = User.GetPersonID();
            var raumID = await _lagerService.RaumDerPersonAsync(personID);
            if (personID == null || raumID == null)
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                // Ein leeres Feld oder 0 heißt "nicht holen". Negative Mengen bleiben drin, damit der LagerService sie ablehnt
                var positionen = holen.Mengen
                    .Where(m => m.Value != null && m.Value != 0)
                    .ToDictionary(m => m.Key, m => m.Value!.Value);

                // Dieselbe Buchung wie beim Transfer; der LagerService erlaubt sie, weil der Von-Raum ein Lager ist
                var fehler = await _lagerService.AusborgenAsync(positionen, holen.VonRaumID!, raumID, holen.BewegungsartID!.Value, personID.Value);
                if (fehler == null)
                {
                    var lager = await _context.Raum.Include(r => r.Person).FirstAsync(r => r.ID == holen.VonRaumID);
                    var wartetAuf = lager.Person?.VollerName ?? "die verantwortliche Person";
                    if (positionen.Count == 1)
                    {
                        TempData["Meldung"] = $"1 Gerät aus Lager {lager.ID} angefragt. Sobald {wartetAuf} freigibt, ist es in Raum {raumID}.";
                    }
                    else
                    {
                        TempData["Meldung"] = $"{positionen.Count} Geräte aus Lager {lager.ID} angefragt. Sobald {wartetAuf} freigibt, sind sie in Raum {raumID}.";
                    }
                    return RedirectToAction("Index", "MeinRaum");
                }
                ModelState.AddModelError(string.Empty, fehler);
            }

            await AnzeigeSetzenAsync(holen, raumID);
            return View(holen);
        }

        /// <summary>
        /// Methode, die alles für die Anzeige setzt: den eigenen Raum, die Lager zur Auswahl, das gewählte Lager mit seinem
        /// Bestand und die Bewegungsarten.
        /// </summary>
        /// <param name="holen">Das HolenViewModel, das angezeigt werden soll</param>
        /// <param name="raumID">Die ID des eigenen Raums, in den geholt wird</param>
        /// <returns>Gibt eine Task zurück</returns>
        private async Task AnzeigeSetzenAsync(HolenViewModel holen, string raumID)
        {
            holen.NachRaum = await _context.Raum.Include(r => r.Raumart).FirstAsync(r => r.ID == raumID);

            // Nur Lager mit verantwortlicher Person, denn diese muss freigeben. Das eigene Lager nicht, dafür gibt es den Transfer
            holen.Lager = await _context.Raum
                .Include(r => r.Raumart)
                .Include(r => r.Person)
                .Where(r => r.Raumart.Name.ToLower().Contains(Raumart.Lager) && r.PersonID != null && r.ID != raumID)
                .OrderBy(r => r.ID)
                .ToListAsync();

            // Ohne gültige Auswahl das erste Lager. Groß-/Kleinschreibung egal, wie bei der Suche in der Datenbank
            holen.VonRaum = holen.Lager.FirstOrDefault(r => string.Equals(r.ID, holen.VonRaumID, StringComparison.OrdinalIgnoreCase))
                ?? holen.Lager.FirstOrDefault();
            holen.VonRaumID = holen.VonRaum?.ID;

            if (holen.VonRaum != null)
            {
                holen.Bestand = await _context.Raumbestand
                    .Include(r => r.Gegenstand).ThenInclude(g => g.Kategorie)
                    .Where(r => r.RaumID == holen.VonRaum.ID)
                    .OrderBy(r => r.Gegenstand.Name).ThenBy(r => r.Gegenstand.Seriennummer)
                    .ToListAsync();
            }

            holen.Bewegungsarten = new SelectList(await _lagerService.WaehlbareBewegungsartenAsync(), "ID", "Name", holen.BewegungsartID);
        }
    }
}
