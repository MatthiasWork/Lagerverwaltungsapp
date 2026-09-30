
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Lagerverwaltungsapp_MatthiasUtrata.Models;

// Gegenstände verwalten darf nur ein Admin
[Authorize(Roles = "Admin")]
public class GegenstandController : Controller
{
    private readonly LagerverwaltungContext _context;

    /// <summary>
    /// Konstruktor der GegenstandController-Klasse.
    /// </summary>
    /// <param name="context">Der Datenbankkontext der Lagerverwaltung</param>
    public GegenstandController(LagerverwaltungContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Methode, die alle Gegenstände mit Kategorie und Hersteller anzeigt.
    /// Die Liste kann nach Suchbegriff, Kategorie, Hersteller und mit/ohne Seriennummer gefiltert werden.
    /// </summary>
    /// <param name="suche">Suchbegriff für Bezeichnung oder Seriennummer</param>
    /// <param name="kategorieID">Die ID der Kategorie, nach der gefiltert werden soll</param>
    /// <param name="herstellerID">Die ID des Herstellers, nach dem gefiltert werden soll</param>
    /// <param name="mitSeriennummer">True = nur mit Seriennummer, False = nur ohne Seriennummer, null = alle</param>
    /// <returns>Gibt eine Task zurück</returns>
    // GET: Gegenstand
    public async Task<IActionResult> Index(string? suche, int? kategorieID, int? herstellerID, bool? mitSeriennummer)
    {
        suche = suche?.Trim();

        var abfrage = _context.Gegenstand
            .Include(g => g.Kategorie)
            .Include(g => g.Hersteller)
            .AsQueryable();

        if (!string.IsNullOrEmpty(suche))
        {
            abfrage = abfrage.Where(g => g.Name.Contains(suche)
                || (g.Seriennummer != null && g.Seriennummer.Contains(suche)));
        }

        if (kategorieID != null)
        {
            abfrage = abfrage.Where(g => g.KategorieID == kategorieID);
        }

        if (herstellerID != null)
        {
            abfrage = abfrage.Where(g => g.HerstellerID == herstellerID);
        }

        if (mitSeriennummer == true)
        {
            abfrage = abfrage.Where(g => g.Seriennummer != null);
        }
        else if (mitSeriennummer == false)
        {
            abfrage = abfrage.Where(g => g.Seriennummer == null);
        }

        var uebersicht = new GegenstandUebersichtViewModel
        {
            Gegenstaende = await abfrage.OrderBy(g => g.Name).ThenBy(g => g.Seriennummer).ToListAsync(),
            AnzahlGesamt = await _context.Gegenstand.CountAsync(),
            Suche = suche,
            KategorieID = kategorieID,
            HerstellerID = herstellerID,
            MitSeriennummer = mitSeriennummer,
            Kategorien = new SelectList(await _context.Kategorie.OrderBy(k => k.Name).ToListAsync(), "ID", "Name", kategorieID),
            Hersteller = new SelectList(await _context.Hersteller.OrderBy(h => h.Name).ToListAsync(), "ID", "Name", herstellerID)
        };

        return View(uebersicht);
    }

    /// <summary>
    /// Methode, die die Details eines Gegenstands anzeigt: wo er sich gerade befindet
    /// (Bestand in den Räumen und noch nicht bestätigte Lagerbewegungen) und seine Bewegungshistorie.
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

        // AsSplitQuery, da sonst alle Bestände mit allen Lagerbewegungen in einer einzigen Abfrage kombiniert würden
        var gegenstand = await _context.Gegenstand
            .Include(g => g.Kategorie)
            .Include(g => g.Hersteller)
            .Include(g => g.Raumbestand).ThenInclude(r => r.Raum).ThenInclude(r => r.Raumart)
            .Include(g => g.Raumbestand).ThenInclude(r => r.Raum).ThenInclude(r => r.Person)
            .Include(g => g.Lagerbewegung).ThenInclude(l => l.Bewegungsart)
            .Include(g => g.Lagerbewegung).ThenInclude(l => l.NachRaum).ThenInclude(r => r.Person)
            .Include(g => g.Lagerbewegung).ThenInclude(l => l.Person)
            .AsSplitQuery()
            .FirstOrDefaultAsync(m => m.ID == id);
        if (gegenstand == null)
        {
            return NotFound();
        }

        return View(gegenstand);
    }

    /// <summary>
    /// Methode, die das Formular zum Anlegen eines neuen Gegenstands anzeigt.
    /// </summary>
    /// <returns>Gibt eine Task zurück</returns>
    // GET: Gegenstand/Create
    public async Task<IActionResult> Create()
    {
        await AuswahllistenSetzenAsync(null);
        return View();
    }

    /// <summary>
    /// Methode, die einen neuen Gegenstand anlegt. Es werden nur die Stammdaten gespeichert,
    /// der Bestand ändert sich ausschließlich über Lagerbewegungen.
    /// </summary>
    /// <param name="gegenstand">Der Gegenstand mit den Daten aus dem Formular</param>
    /// <returns>Gibt eine Task zurück</returns>
    // POST: Gegenstand/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name,Seriennummer,KategorieID,HerstellerID")] Gegenstand gegenstand)
    {
        // Navigationseigenschaften werden nicht gebunden, sonst schlägt die Validierung fehl
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
    /// Ob er eine Seriennummer hat, kann nur geändert werden, solange er weder Bestand noch Lagerbewegungen hat.
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
    public async Task<IActionResult> Edit(int? id, [Bind("ID,Name,Seriennummer,KategorieID,HerstellerID")] Gegenstand gegenstand)
    {
        // Navigationseigenschaften werden nicht gebunden, sonst schlägt die Validierung fehl
        ModelState.Remove(nameof(Gegenstand.Hersteller));
        ModelState.Remove(nameof(Gegenstand.Kategorie));

        if (id != gegenstand.ID)
        {
            return NotFound();
        }

        await EingabenPruefenAsync(gegenstand);

        // Mit Seriennummer ist der Gegenstand ein einzelnes Gerät (Menge 1), ohne ein Artikel mit beliebiger Menge.
        // Gibt es schon Bestand oder Lagerbewegungen (dieselbe Bedingung wie beim Löschen), würden diese Mengen sonst nicht mehr stimmen
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
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var gegenstand = await _context.Gegenstand.FindAsync(id);
        if (gegenstand == null)
        {
            return RedirectToAction(nameof(Index));
        }

        // Nochmals prüfen, da der POST auch ohne die Bestätigungsseite abgeschickt werden kann
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
        // Leerzeichen am Rand würden sonst zu scheinbar unterschiedlichen Seriennummern führen
        gegenstand.Seriennummer = string.IsNullOrWhiteSpace(gegenstand.Seriennummer) ? null : gegenstand.Seriennummer.Trim();

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

        // Lagerbewegungen werden nie gelöscht, sonst wäre die Historie nicht mehr vollständig
        var anzahlLagerbewegungen = await _context.Lagerbewegung.CountAsync(l => l.GegenstandID == gegenstand.ID);
        if (anzahlLagerbewegungen > 0)
        {
            return $"\"{gegenstand.Name}\" kommt in {anzahlLagerbewegungen} Lagerbewegung(en) vor und kann daher nicht gelöscht werden, sonst ginge die Historie verloren.";
        }

        return null;
    }
}
