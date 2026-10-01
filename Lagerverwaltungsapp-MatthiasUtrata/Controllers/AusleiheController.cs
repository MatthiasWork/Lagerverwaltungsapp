using Lagerverwaltungsapp_MatthiasUtrata.Extensions;
using Lagerverwaltungsapp_MatthiasUtrata.Models;
using Lagerverwaltungsapp_MatthiasUtrata.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Lagerverwaltungsapp_MatthiasUtrata.Controllers
{
    // Neue Ausleihe: Eine Ausleihe ist eine Lagerbewegung wie ein Transfer. Die für einen Raum verantwortliche Person bucht
    // Gegenstände aus ihrem Raum in einen anderen Raum, dessen verantwortliche Person die Übernahme unter "Freigaben" bestätigt.
    // Daher dasselbe ViewModel und dieselbe Buchung wie beim Transfer (MeinRaum/Ausborgen), nur mit der Oberfläche aus dem Design.
    // Zeitraum und Zweck zeigt das Formular an, gespeichert werden sie (wie beim Transfer) nicht
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
        /// Methode, die das Formular "Neue Ausleihe" anzeigt. Wer für keinen Raum verantwortlich ist, bekommt nur einen Hinweis,
        /// da nur die verantwortliche Person Gegenstände aus einem Raum buchen darf.
        /// </summary>
        /// <returns>Gibt eine Task zurück</returns>
        // GET: Ausleihe
        public async Task<IActionResult> Index()
        {
            var raumID = await EigenerRaumAsync();
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
        /// Methode, die die ausgewählten Gegenstände aus dem eigenen Raum an den gewählten Raum ausleiht. Wie beim Transfer
        /// werden sie sofort abgebucht und sind unterwegs, bis die Person des Zielraums die Übernahme bestätigt.
        /// </summary>
        /// <param name="ausleihe">Das AusborgenViewModel mit Zielraum, Bewegungsart und Mengen aus dem Formular</param>
        /// <returns>Gibt eine Task zurück</returns>
        // POST: Ausleihe
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index([Bind("NachRaumID,BewegungsartID,Mengen")] AusborgenViewModel ausleihe)
        {
            var personID = User.GetPersonID();
            var raumID = await EigenerRaumAsync();
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
                    var wartetAuf = nachRaum.Person == null ? "die verantwortliche Person" : $"{nachRaum.Person.Vorname} {nachRaum.Person.Nachname}";
                    var anzahl = positionen.Count == 1 ? "1 Gerät" : $"{positionen.Count} Geräten";
                    TempData["Meldung"] = $"Ausleihe von {anzahl} an Raum {nachRaum.ID} angefragt. Bis {wartetAuf} sie freigibt, sind die Geräte unterwegs.";
                    return RedirectToAction("Index", "MeinRaum");
                }
                ModelState.AddModelError(string.Empty, fehler);
            }

            await AnzeigeSetzenAsync(ausleihe, raumID);
            return View(ausleihe);
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
        /// Methode, die alles für die Anzeige des Formulars setzt: den eigenen Raum mit seinem Bestand, die möglichen Zielräume
        /// und die Bewegungsarten.
        /// </summary>
        /// <param name="ausleihe">Das AusborgenViewModel, das angezeigt werden soll</param>
        /// <param name="raumID">Die ID des eigenen Raums, aus dem ausgeliehen wird</param>
        /// <returns>Gibt eine Task zurück</returns>
        private async Task AnzeigeSetzenAsync(AusborgenViewModel ausleihe, string raumID)
        {
            ausleihe.VonRaum = await _context.Raum.Include(r => r.Raumart).Include(r => r.Person).FirstAsync(r => r.ID == raumID);

            ausleihe.Bestand = await _context.Raumbestand
                .Include(r => r.Gegenstand).ThenInclude(g => g.Kategorie)
                .Where(r => r.RaumID == raumID)
                .OrderBy(r => r.Gegenstand.Name).ThenBy(r => r.Gegenstand.Seriennummer)
                .ToListAsync();

            // Nur Räume mit verantwortlicher Person, denn diese muss die Übernahme bestätigen
            ausleihe.Raeume = await _context.Raum
                .Include(r => r.Raumart)
                .Include(r => r.Person)
                .Where(r => r.ID != raumID && r.PersonID != null)
                .OrderBy(r => r.ID)
                .ToListAsync();

            // "Storniert" vergibt nur der LagerService beim Ablehnen oder Zurückziehen
            ausleihe.Bewegungsarten = new SelectList(await _context.Bewegungsart
                .Where(b => b.Name != Bewegungsart.Storniert)
                .OrderBy(b => b.Name)
                .ToListAsync(), "ID", "Name", ausleihe.BewegungsartID);
        }
    }
}
