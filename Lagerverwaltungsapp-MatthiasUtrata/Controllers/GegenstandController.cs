using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Lagerverwaltungsapp_MatthiasUtrata.Extensions;
using Lagerverwaltungsapp_MatthiasUtrata.Models;
using Lagerverwaltungsapp_MatthiasUtrata.Services;

namespace Lagerverwaltungsapp_MatthiasUtrata.Controllers;

public class GegenstandController : Controller
{
    private readonly LagerverwaltungContext _context;

    private readonly LagerService _lagerService;

    /// <summary>
    /// Konstruktor der GegenstandController-Klasse.
    /// </summary>
    /// <param name="context">Der Datenbankkontext der Lagerverwaltung</param>
    /// <param name="lagerService">Der Service, der die Zuständigkeiten für Räume kennt</param>
    public GegenstandController(LagerverwaltungContext context, LagerService lagerService)
    {
        _context = context;
        _lagerService = lagerService;
    }

    /// <summary>
    /// Methode, die den Katalog anzeigt: alle Gegenstände mit Standort und Status.
    /// Die Liste kann nach Suchbegriff, Kategorie, Hersteller, Raum und Status gefiltert werden.
    /// </summary>
    /// <param name="suche">Suchbegriff für Bezeichnung, Seriennummer oder Raum</param>
    /// <param name="kategorieID">Die ID der Kategorie, nach der gefiltert werden soll</param>
    /// <param name="herstellerID">Die ID des Herstellers, nach dem gefiltert werden soll (Link bei den Herstellern)</param>
    /// <param name="raumID">Die ID des Raums, in dem der Gegenstand liegen muss</param>
    /// <param name="status">Der Status, nach dem gefiltert werden soll</param>
    /// <returns>Gibt eine Task zurück</returns>
    // GET: Gegenstand
    public async Task<IActionResult> Index(string? suche, int? kategorieID, int? herstellerID, string? raumID, string? status)
    {
        suche = suche?.Trim();

        // Mit Bestand und den noch nicht bestätigten Bewegungen
        var abfrage = _context.Gegenstand
            .Include(g => g.Kategorie)
            .Include(g => g.Raumbestand).ThenInclude(r => r.Raum).ThenInclude(r => r.Raumart)
            .Include(g => g.Lagerbewegung.Where(l => l.BestaetigtAm == null))
            .AsQueryable();

        if (!string.IsNullOrEmpty(suche))
        {
            abfrage = abfrage.Where(g => g.Name.Contains(suche)
                || (g.Seriennummer != null && g.Seriennummer.Contains(suche))
                || g.Raumbestand.Any(r => r.RaumID.Contains(suche)));
        }

        if (kategorieID != null)
        {
            abfrage = abfrage.Where(g => g.KategorieID == kategorieID);
        }

        if (herstellerID != null)
        {
            abfrage = abfrage.Where(g => g.HerstellerID == herstellerID);
        }

        if (!string.IsNullOrEmpty(raumID))
        {
            abfrage = abfrage.Where(g => g.Raumbestand.Any(r => r.RaumID == raumID));
        }

        var eintraege = (await abfrage.OrderBy(g => g.Name).ThenBy(g => g.Seriennummer).ToListAsync())
            .Select(g => KatalogEintrag.Erstellen(g, raumID))
            .ToList();

        if (!string.IsNullOrEmpty(status))
        {
            eintraege = eintraege.Where(e => e.Status == status).ToList();
        }

        var uebersicht = new GegenstandUebersichtViewModel
        {
            Eintraege = eintraege,
            AnzahlGesamt = await _context.Gegenstand.CountAsync(),
            AnzahlRaeume = await _context.Raum.CountAsync(),
            Suche = suche,
            KategorieID = kategorieID,
            HerstellerID = herstellerID,
            RaumID = raumID,
            Status = status,
            Kategorien = await _context.Kategorie.OrderBy(k => k.Name).ToListAsync(),
            Raeume = await _context.Raum.OrderBy(r => r.ID).Select(r => r.ID).ToListAsync()
        };

        // Den Filter "Hersteller" gibt es nur, wenn man über einen Link bei den Herstellern kommt, daher die Hersteller nur dann laden
        if (herstellerID != null)
        {
            uebersicht.Hersteller = await _context.Hersteller.OrderBy(h => h.Name).ToListAsync();
        }

        return View(uebersicht);
    }

    /// <summary>
    /// Methode, die die Details eines Gegenstands anzeigt.
    /// </summary>
    /// <param name="id">Die ID des Gegenstands, der angezeigt werden soll</param>
    /// <returns>Gibt eine Task zurück</returns>
    // GET: Gegenstand/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var gegenstand = await _context.Gegenstand
            .Include(g => g.Kategorie)
            .Include(g => g.Hersteller)
            .Include(g => g.Raumbestand).ThenInclude(r => r.Raum).ThenInclude(r => r.Raumart)
            .Include(g => g.Raumbestand).ThenInclude(r => r.Raum).ThenInclude(r => r.Person)
            .Include(g => g.Lagerbewegung).ThenInclude(l => l.Bewegungsart)
            .Include(g => g.Lagerbewegung).ThenInclude(l => l.VonRaum).ThenInclude(r => r.Person)
            .Include(g => g.Lagerbewegung).ThenInclude(l => l.NachRaum).ThenInclude(r => r.Person)
            .Include(g => g.Lagerbewegung).ThenInclude(l => l.Person)
            .FirstOrDefaultAsync(m => m.ID == id);
        if (gegenstand == null)
        {
            return NotFound();
        }

        var details = new GegenstandDetailsViewModel
        {
            Eintrag = KatalogEintrag.Erstellen(gegenstand),
            Unterwegs = gegenstand.Lagerbewegung.Where(l => l.Offen).OrderBy(l => l.ErstelltAm).ToList(),
            EigenerRaumID = await _lagerService.RaumDerPersonAsync(User.GetPersonID())
        };

        // Verlauf: jede Bewegung ergibt einen Eintrag beim Anlegen und, falls schon abgeschlossen, einen bei der Übernahme oder der Stornierung
        foreach (var l in gegenstand.Lagerbewegung)
        {
            var person = l.Person.VollerName;

            // Bei einem Gerät mit Seriennummer ist die Menge immer 1 und wird daher nicht genannt.
            // Bei einer Korrektur ist sie die Änderung und kann negativ sein, die Richtung steht im Text
            var menge = "";
            if (gegenstand.Seriennummer == null)
            {
                menge = $"{Math.Abs(l.Menge)} Stück ";
            }

            if (l.IstKorrektur)
            {
                string text;
                if (l.Menge > 0)
                {
                    text = $"{menge}in {l.NachRaumID} eingebucht von {person} (Korrektur)";
                }
                else
                {
                    text = $"{menge}aus {l.NachRaumID} ausgebucht von {person} (Korrektur)";
                }
                details.Verlauf.Add(new Aktivitaet { Zeitpunkt = l.ErstelltAm, Text = GrossAnfang(text) });
                continue;
            }

            // Eine stornierte Bewegung hat in BestaetigtAm den Zeitpunkt der Stornierung und ihre ursprüngliche Bewegungsart verloren
            if (l.Storniert)
            {
                details.Verlauf.Add(new Aktivitaet { Zeitpunkt = l.ErstelltAm, Text = GrossAnfang($"{menge}von {l.VonRaumID} nach {l.NachRaumID} gebucht von {person}") });
                details.Verlauf.Add(new Aktivitaet { Zeitpunkt = l.BestaetigtAm!.Value, Text = $"Ausleihe nach {l.NachRaumID} storniert, {menge}zurück in {l.VonRaumID}" });
                continue;
            }

            if (l.BestaetigtAm == l.ErstelltAm)
            {
                // Sofort bestätigt: Wareneingang
                string text;
                if (l.VonRaumID == l.NachRaumID)
                {
                    text = $"{menge}in {l.NachRaumID} eingebucht von {person}";
                }
                else
                {
                    text = $"{menge}von {l.VonRaumID} nach {l.NachRaumID} umgebucht von {person}";
                }
                details.Verlauf.Add(new Aktivitaet { Zeitpunkt = l.ErstelltAm, Text = GrossAnfang(text) });
                continue;
            }

            details.Verlauf.Add(new Aktivitaet { Zeitpunkt = l.ErstelltAm, Text = GrossAnfang($"{menge}von {l.VonRaumID} nach {l.NachRaumID} gebucht von {person} ({l.Bewegungsart.Name})") });
            if (l.BestaetigtAm != null)
            {
                details.Verlauf.Add(new Aktivitaet { Zeitpunkt = l.BestaetigtAm.Value, Text = GrossAnfang($"{menge}in {l.NachRaumID} übernommen") });
            }
        }
        details.Verlauf = details.Verlauf.OrderByDescending(a => a.Zeitpunkt).ToList();

        return View(details);
    }

    /// <summary>
    /// Methode, die den ersten Buchstaben eines Textes großschreibt (Verlaufstexte beginnen je nach Menge klein).
    /// </summary>
    /// <param name="text">Der Text</param>
    /// <returns>Der Text mit großem Anfangsbuchstaben</returns>
    private static string GrossAnfang(string text)
    {
        if (text.Length == 0)
        {
            return text;
        }
        return char.ToUpper(text[0]) + text[1..];
    }

    /// <summary>
    /// Methode, die das Formular zum Anlegen eines neuen Gegenstands anzeigt.
    /// </summary>
    /// <returns>Gibt eine Task zurück</returns>
    // GET: Gegenstand/Create
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create()
    {
        await AuswahllistenSetzenAsync(null);
        return View();
    }

    /// <summary>
    /// Methode, die einen neuen Gegenstand anlegt.
    /// </summary>
    /// <param name="gegenstand">Der Gegenstand mit den Daten aus dem Formular</param>
    /// <returns>Gibt eine Task zurück</returns>
    // POST: Gegenstand/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([Bind("Name,Seriennummer,KategorieID,HerstellerID")] Gegenstand gegenstand)
    {
        ModelState.Remove(nameof(Gegenstand.Hersteller));
        ModelState.Remove(nameof(Gegenstand.Kategorie));

        await EingabenPruefenAsync(gegenstand);

        if (ModelState.IsValid)
        {
            _context.Add(gegenstand);
            await _context.SaveChangesAsync();
            TempData["Meldung"] = $"Gegenstand \"{gegenstand.Name}\" wurde angelegt.";
            return RedirectToAction(nameof(Index));
        }
        await AuswahllistenSetzenAsync(gegenstand);
        return View(gegenstand);
    }

    /// <summary>
    /// Methode, die das Formular zum Bearbeiten eines Gegenstands anzeigt.
    /// </summary>
    /// <param name="id">Die ID des Gegenstands, der bearbeitet werden soll</param>
    /// <returns>Gibt eine Task zurück</returns>
    // GET: Gegenstand/Edit/5
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var gegenstand = await _context.Gegenstand.FindAsync(id);
        if (gegenstand == null)
        {
            return NotFound();
        }
        await AuswahllistenSetzenAsync(gegenstand);
        return View(gegenstand);
    }

    /// <summary>
    /// Methode, die die Änderungen an einem Gegenstand speichert.
    /// </summary>
    /// <param name="id">Die ID des Gegenstands, der bearbeitet werden soll</param>
    /// <param name="gegenstand">Der Gegenstand mit den geänderten Daten aus dem Formular</param>
    /// <returns>Gibt eine Task zurück</returns>
    /// <exception cref="DbUpdateConcurrencyException">Exception, falls der Gegenstand gleichzeitig von jemand anderem geändert wurde</exception>
    // POST: Gegenstand/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int? id, [Bind("ID,Name,Seriennummer,KategorieID,HerstellerID")] Gegenstand gegenstand)
    {
        ModelState.Remove(nameof(Gegenstand.Hersteller));
        ModelState.Remove(nameof(Gegenstand.Kategorie));

        if (id != gegenstand.ID)
        {
            return NotFound();
        }

        await EingabenPruefenAsync(gegenstand);

        // Mit Seriennummer wird der Gegenstand als einzelnes Gerät gebucht (Menge 1), ohne Seriennummer über eine beliebige Menge.
        // Gibt es schon Bestand oder Lagerbewegungen, würden diese Mengen sonst nicht mehr stimmen
        var bisherigeSeriennummer = await _context.Gegenstand
            .Where(g => g.ID == gegenstand.ID)
            .Select(g => g.Seriennummer)
            .FirstOrDefaultAsync();
        if ((bisherigeSeriennummer == null) != (gegenstand.Seriennummer == null) && await LoeschHindernisAsync(gegenstand) != null)
        {
            ModelState.AddModelError(nameof(Gegenstand.Seriennummer),
                "Ob der Gegenstand eine Seriennummer hat, kann nur geändert werden, solange er weder Bestand noch Lagerbewegungen hat.");
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(gegenstand);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!GegenstandExists(gegenstand.ID))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            TempData["Meldung"] = $"Gegenstand \"{gegenstand.Name}\" wurde gespeichert.";
            return RedirectToAction(nameof(Index));
        }
        await AuswahllistenSetzenAsync(gegenstand);
        return View(gegenstand);
    }

    /// <summary>
    /// Methode, die die Bestätigungsseite zum Löschen eines Gegenstands anzeigt.
    /// </summary>
    /// <param name="id">Die ID des Gegenstands, der gelöscht werden soll</param>
    /// <returns>Gibt eine Task zurück</returns>
    // GET: Gegenstand/Delete/5
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var gegenstand = await _context.Gegenstand
            .Include(g => g.Kategorie)
            .Include(g => g.Hersteller)
            .FirstOrDefaultAsync(m => m.ID == id);
        if (gegenstand == null)
        {
            return NotFound();
        }

        ViewData["LoeschHindernis"] = await LoeschHindernisAsync(gegenstand);
        return View(gegenstand);
    }

    /// <summary>
    /// Methode, die einen Gegenstand nach der Bestätigung löscht, sofern nichts dagegen spricht.
    /// </summary>
    /// <param name="id">Die ID des Gegenstands, der gelöscht werden soll</param>
    /// <returns>Gibt eine Task zurück</returns>
    // POST: Gegenstand/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var gegenstand = await _context.Gegenstand.FindAsync(id);
        if (gegenstand == null)
        {
            return RedirectToAction(nameof(Index));
        }

        var hindernis = await LoeschHindernisAsync(gegenstand);
        if (hindernis != null)
        {
            TempData["Fehler"] = hindernis;
            return RedirectToAction(nameof(Index));
        }

        _context.Gegenstand.Remove(gegenstand);
        await _context.SaveChangesAsync();

        TempData["Meldung"] = $"Gegenstand \"{gegenstand.Name}\" wurde gelöscht.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Methode, die überprüft, ob ein Gegenstand mit der angegebenen ID existiert.
    /// </summary>
    /// <param name="id">Die ID des Gegenstands, der überprüft werden soll</param>
    /// <returns>Gibt True oder False zurück</returns>
    private bool GegenstandExists(int? id)
    {
        return _context.Gegenstand.Any(e => e.ID == id);
    }

    /// <summary>
    /// Methode, die die Auswahllisten für Kategorie und Hersteller für das Formular setzt.
    /// </summary>
    /// <param name="gegenstand">Der Gegenstand, dessen Kategorie und Hersteller vorausgewählt werden sollen, oder null</param>
    /// <returns>Gibt eine Task zurück</returns>
    private async Task AuswahllistenSetzenAsync(Gegenstand? gegenstand)
    {
        ViewData["KategorieID"] = new SelectList(await _context.Kategorie.OrderBy(k => k.Name).ToListAsync(), "ID", "Name", gegenstand?.KategorieID);
        ViewData["HerstellerID"] = new SelectList(await _context.Hersteller.OrderBy(h => h.Name).ToListAsync(), "ID", "Name", gegenstand?.HerstellerID);
    }

    /// <summary>
    /// Methode, die die Eingaben prüft, die nicht über Attribute geprüft werden können, und Fehler im ModelState einträgt:
    /// Kategorie und Hersteller müssen existieren und die Seriennummer muss eindeutig sein.
    /// </summary>
    /// <param name="gegenstand">Der Gegenstand, der gespeichert werden soll</param>
    /// <returns>Gibt eine Task zurück</returns>
    private async Task EingabenPruefenAsync(Gegenstand gegenstand)
    {
        if (string.IsNullOrWhiteSpace(gegenstand.Seriennummer))
        {
            gegenstand.Seriennummer = null;
        }
        else
        {
            gegenstand.Seriennummer = gegenstand.Seriennummer.Trim();
        }

        if (!await _context.Kategorie.AnyAsync(k => k.ID == gegenstand.KategorieID))
        {
            ModelState.AddModelError(nameof(Gegenstand.KategorieID), "Bitte eine Kategorie auswählen.");
        }

        if (!await _context.Hersteller.AnyAsync(h => h.ID == gegenstand.HerstellerID))
        {
            ModelState.AddModelError(nameof(Gegenstand.HerstellerID), "Bitte einen Hersteller auswählen.");
        }

        // Ein Gegenstand mit Seriennummer ist genau ein Gerät, daher darf es jede Seriennummer nur einmal geben
        if (gegenstand.Seriennummer != null
            && await _context.Gegenstand.AnyAsync(g => g.Seriennummer == gegenstand.Seriennummer && g.ID != gegenstand.ID))
        {
            ModelState.AddModelError(nameof(Gegenstand.Seriennummer), "Diese Seriennummer ist bereits vergeben.");
        }
    }

    /// <summary>
    /// Methode, die überprüft, ob ein Gegenstand gelöscht werden darf.
    /// </summary>
    /// <param name="gegenstand">Der Gegenstand, der gelöscht werden soll</param>
    /// <returns>Der Grund, warum der Gegenstand nicht gelöscht werden darf, oder null, wenn das Löschen erlaubt ist</returns>
    private async Task<string?> LoeschHindernisAsync(Gegenstand gegenstand)
    {
        var raeume = await _context.Raumbestand
            .Where(r => r.GegenstandID == gegenstand.ID)
            .Select(r => r.RaumID)
            .OrderBy(r => r)
            .ToListAsync();
        if (raeume.Any())
        {
            return $"\"{gegenstand.Name}\" ist noch im Bestand (Raum {string.Join(", ", raeume)}) und kann daher nicht gelöscht werden.";
        }

        var anzahlLagerbewegungen = await _context.Lagerbewegung.CountAsync(l => l.GegenstandID == gegenstand.ID);
        if (anzahlLagerbewegungen > 0)
        {
            return $"\"{gegenstand.Name}\" kommt in {anzahlLagerbewegungen} Lagerbewegung(en) vor und kann daher nicht gelöscht werden, sonst ginge die Historie verloren.";
        }

        return null;
    }
}
