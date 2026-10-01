using Lagerverwaltungsapp_MatthiasUtrata.Extensions;
using Lagerverwaltungsapp_MatthiasUtrata.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Lagerverwaltungsapp_MatthiasUtrata.Controllers
{
    // Das eigene Profil sieht jede angemeldete Person, das einer anderen Person nur ein Admin
    public class ProfilController : Controller
    {
        private readonly LagerverwaltungContext _context;

        /// <summary>
        /// Konstruktor der ProfilController-Klasse.
        /// </summary>
        /// <param name="context">Der Datenbankkontext der Lagerverwaltung</param>
        public ProfilController(LagerverwaltungContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Methode, die das Profil einer Person anzeigt: Rolle, Raumverantwortung, offene Transfers und die letzten Buchungen.
        /// </summary>
        /// <param name="id">Die ID der Person; ohne ID das eigene Profil</param>
        /// <returns>Gibt eine Task zurück</returns>
        // GET: Profil, Profil/Index/5
        public async Task<IActionResult> Index(int? id)
        {
            var personID = User.GetPersonID();
            if (personID == null)
            {
                return Forbid();
            }

            if (id != null && id != personID && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            var anzeigenID = id ?? personID.Value;
            var person = await _context.Person
                .Include(p => p.Rolle)
                .Include(p => p.Raum).ThenInclude(r => r.Raumart)
                .FirstOrDefaultAsync(p => p.ID == anzeigenID);
            if (person == null)
            {
                return NotFound();
            }

            var raumIDs = person.Raum.Select(r => r.ID).ToList();

            var profil = new ProfilViewModel
            {
                Person = person,
                IstEigenesProfil = anzeigenID == personID,
                StueckJeRaum = await _context.Raumbestand
                    .Where(r => raumIDs.Contains(r.RaumID))
                    .GroupBy(r => r.RaumID)
                    .Select(g => new { RaumID = g.Key, Stueck = g.Sum(r => r.Menge) })
                    .ToDictionaryAsync(g => g.RaumID, g => g.Stueck),
                OffeneTransfers = await _context.Lagerbewegung
                    .Include(l => l.Gegenstand)
                    .Include(l => l.NachRaum).ThenInclude(r => r.Person)
                    .Where(Lagerbewegung.IstOffen)
                    .Where(l => l.PersonID == anzeigenID)
                    .OrderBy(l => l.ErstelltAm)
                    .ToListAsync()
            };

            var bewegungen = await _context.Lagerbewegung
                .Include(l => l.Gegenstand)
                .Include(l => l.Bewegungsart)
                .Where(l => l.PersonID == anzeigenID
                    || (l.BestaetigtAm != null && raumIDs.Contains(l.NachRaumID))
                    || (l.BestaetigtAm != null && l.Bewegungsart.Name == Bewegungsart.Storniert && raumIDs.Contains(l.VonRaumID)))
                .OrderByDescending(l => l.BestaetigtAm ?? l.ErstelltAm)
                .Take(8)
                .ToListAsync();

            foreach (var l in bewegungen)
            {
                var was = l.Menge == 1 ? l.Gegenstand.Name : $"{l.Gegenstand.Name} × {l.Menge}";
                var sofortBestaetigt = !l.Storniert && l.BestaetigtAm == l.ErstelltAm;

                if (l.PersonID == anzeigenID)
                {
                    var text = !sofortBestaetigt ? $"Transfer {was} {l.VonRaumID} → {l.NachRaumID} angefragt"
                        : l.VonRaumID == l.NachRaumID ? $"{was} in {l.NachRaumID} eingebucht"
                        : $"{was} {l.VonRaumID} → {l.NachRaumID} umgebucht";
                    profil.Verlauf.Add(new Aktivitaet { Zeitpunkt = l.ErstelltAm, Text = text });
                }

                if (l.Storniert)
                {
                    profil.Verlauf.Add(new Aktivitaet { Zeitpunkt = l.BestaetigtAm!.Value, Text = $"Transfer {was} {l.VonRaumID} → {l.NachRaumID} storniert" });
                }
                else if (l.BestaetigtAm != null && !sofortBestaetigt && raumIDs.Contains(l.NachRaumID))
                {
                    profil.Verlauf.Add(new Aktivitaet { Zeitpunkt = l.BestaetigtAm.Value, Text = $"{was} in {l.NachRaumID} übernommen" });
                }
            }
            profil.Verlauf = profil.Verlauf.OrderByDescending(a => a.Zeitpunkt).Take(8).ToList();

            return View(profil);
        }
    }
}
