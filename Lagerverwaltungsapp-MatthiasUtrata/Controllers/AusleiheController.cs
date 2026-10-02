using Lagerverwaltungsapp_MatthiasUtrata.Extensions;
using Lagerverwaltungsapp_MatthiasUtrata.Models;
using Lagerverwaltungsapp_MatthiasUtrata.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Lagerverwaltungsapp_MatthiasUtrata.Controllers
{
    public class AusleiheController : Controller
    {
        private readonly LagerverwaltungContext _context;
        private readonly LagerService _lagerService;

        /// <summary>
        /// Konstruktor der AusleiheController-Klasse.
        /// </summary>
        /// <param name="context">Der Datenbankkontext der Lagerverwaltung</param>
        /// <param name="lagerService">Der Service, der die Zuständigkeiten kennt und die Lagerbewegungen bucht</param>
        public AusleiheController(LagerverwaltungContext context, LagerService lagerService)
        {
            _context = context;
            _lagerService = lagerService;
        }

        /// <summary>
        /// Methode, die das Formular "Neue Ausleihe" anzeigt. Wer für keinen Raum verantwortlich ist, bekommt nur einen Hinweis, weil sie keinen Raum hat und deswegen nichts buchen kann.
        /// </summary>
        /// <returns>Gibt eine Task zurück</returns>
        // GET: Ausleihe
        public async Task<IActionResult> Index()
        {
            var raumID = await _lagerService.RaumDerPersonAsync(User.GetPersonID());
            if (raumID == null)
            {
                return View();
            }

            var ausleihe = new AusborgenViewModel
            {
                BewegungsartID = await _context.Bewegungsart
                    .Where(b => b.Name == Bewegungsart.Ausgabe)
                    .Select(b => (int?)b.ID)
                    .FirstOrDefaultAsync()
            };
            await AnzeigeSetzenAsync(ausleihe, raumID);
            return View(ausleihe);
        }

        /// <summary>
        /// Methode, die die ausgewählten Gegenstände aus dem eigenen Raum an den gewählten Raum ausleiht. 
        /// </summary>
        /// <param name="ausleihe">Das AusborgenViewModel mit Zielraum, Bewegungsart und Mengen aus dem Formular</param>
        /// <returns>Gibt eine Task zurück</returns>
        // POST: Ausleihe
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index([Bind("NachRaumID,BewegungsartID,Mengen")] AusborgenViewModel ausleihe)
        {
            var personID = User.GetPersonID();
            var raumID = await _lagerService.RaumDerPersonAsync(personID);
            if (personID == null || raumID == null)
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                // Ein leeres Feld oder 0 heißt "nicht ausleihen". Negative Mengen bleiben drin, damit der LagerService sie ablehnt
                var positionen = ausleihe.Mengen
                    .Where(m => m.Value != null && m.Value != 0)
                    .ToDictionary(m => m.Key, m => m.Value!.Value);

                var fehler = await _lagerService.AusborgenAsync(positionen, raumID, ausleihe.NachRaumID!, ausleihe.BewegungsartID!.Value, personID.Value);
                if (fehler == null)
                {
                    var nachRaum = await _context.Raum.Include(r => r.Person).FirstAsync(r => r.ID == ausleihe.NachRaumID);
                    var wartetAuf = nachRaum.Person?.VollerName ?? "die verantwortliche Person";
                    if (positionen.Count == 1)
                    {
                        TempData["Meldung"] = $"Ausleihe von 1 Gerät an Raum {nachRaum.ID} angefragt. Bis {wartetAuf} sie freigibt, ist das Gerät unterwegs.";
                    }
                    else
                    {
                        TempData["Meldung"] = $"Ausleihe von {positionen.Count} Geräten an Raum {nachRaum.ID} angefragt. Bis {wartetAuf} sie freigibt, sind die Geräte unterwegs.";
                    }
                    return RedirectToAction("Index", "MeinRaum");
                }
                ModelState.AddModelError(string.Empty, fehler);
            }

            await AnzeigeSetzenAsync(ausleihe, raumID);
            return View(ausleihe);
        }

        /// <summary>
        /// Methode, die alles für die Anzeige des Formulars setzt: den eigenen Raum mit seinen Geräten (wie im Katalog), die möglichen Zielräume und die Bewegungsarten.
        /// </summary>
        /// <param name="ausleihe">Das AusborgenViewModel, das angezeigt werden soll</param>
        /// <param name="raumID">Die ID des eigenen Raums, aus dem ausgeliehen wird</param>
        /// <returns>Gibt eine Task zurück</returns>
        private async Task AnzeigeSetzenAsync(AusborgenViewModel ausleihe, string raumID)
        {
            ausleihe.VonRaum = await _context.Raum.Include(r => r.Raumart).Include(r => r.Person).FirstAsync(r => r.ID == raumID);

            // Wie im Katalog, aber nur mit dem Bestand im eigenen Raum: Stück und Standort im Eintrag beziehen sich dann auf das,
            // was ausgeliehen werden kann. Die offenen Bewegungen ergeben den Hinweis, z. B. was davon schon unterwegs ist
            var gegenstaende = await _context.Gegenstand
                .Include(g => g.Kategorie)
                .Include(g => g.Raumbestand.Where(r => r.RaumID == raumID)).ThenInclude(r => r.Raum).ThenInclude(r => r.Raumart)
                .Include(g => g.Lagerbewegung.Where(l => l.BestaetigtAm == null))
                .Where(g => g.Raumbestand.Any(r => r.RaumID == raumID && r.Menge > 0))
                .OrderBy(g => g.Name).ThenBy(g => g.Seriennummer)
                .ToListAsync();
            ausleihe.Geraete = gegenstaende.Select(g => KatalogEintrag.Erstellen(g)).ToList();

            // Nur Räume mit einer verantwortlichen Person, denn diese muss die Übernahme bestätigen
            ausleihe.Raeume = await _context.Raum
                .Include(r => r.Raumart)
                .Include(r => r.Person)
                .Where(r => r.ID != raumID && r.PersonID != null)
                .OrderBy(r => r.ID)
                .ToListAsync();

            ausleihe.Bewegungsarten = new SelectList(await _lagerService.WaehlbareBewegungsartenAsync(), "ID", "Name", ausleihe.BewegungsartID);
        }
    }
}
