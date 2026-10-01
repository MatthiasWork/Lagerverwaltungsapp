using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Lagerverwaltungsapp_MatthiasUtrata.Models;
using Lagerverwaltungsapp_MatthiasUtrata.Services;

namespace Lagerverwaltungsapp_MatthiasUtrata.Controllers;

// Räume verwalten darf nur ein Admin
[Authorize(Roles = "Admin")]
public class RaumController : Controller
{
    private readonly LagerverwaltungContext _context;
    private readonly LagerService _lagerService;

    /// <summary>
    /// Konstruktor der RaumController-Klasse.
    /// </summary>
    /// <param name="context">Der Datenbankkontext der Lagerverwaltung</param>
    /// <param name="lagerService">Der Service, der weiß, für welchen Raum eine Person verantwortlich ist</param>
    public RaumController(LagerverwaltungContext context, LagerService lagerService)
    {
        _context = context;
        _lagerService = lagerService;
    }

    /// <summary>
    /// Methode, die den Grundriss mit den Räumen eines Stockwerks anzeigt. Häuser, Stockwerke und die Lage der Räume
    /// gibt es im Datenmodell noch nicht, daher ist die Seite statisch mit den Beispieldaten aus dem Design.
    /// </summary>
    /// <returns>Gibt ein IActionResult zurück</returns>
    // GET: Raum/Plan
    public IActionResult Plan()
    {
        return View();
    }

    /// <summary>
    /// Methode, die alle Räume mit Raumart und verantwortlicher Person anzeigt.
    /// </summary>
    /// <returns>Gibt eine Task zurück</returns>
    // GET: Raum
    public async Task<IActionResult> Index()
    {
        var raeume = await _context.Raum
            .Include(r => r.Raumart)
            .Include(r => r.Person)
            .OrderBy(r => r.ID)
            .ToListAsync();
        return View(raeume);
    }

    /// <summary>
    /// Methode, die die Details eines Raums mit Raumart und verantwortlicher Person anzeigt.
    /// </summary>
    /// <param name="id">Die ID des Raums, der angezeigt werden soll</param>
    /// <returns>Gibt eine Task zurück</returns>
    // GET: Raum/Details/A101
    public async Task<IActionResult> Details(string? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var raum = await _context.Raum
            .Include(r => r.Raumart)
            .Include(r => r.Person)
            .FirstOrDefaultAsync(m => m.ID == id);
        if (raum == null)
        {
            return NotFound();
        }

        return View(raum);
    }

    /// <summary>
    /// Methode, die das Formular zum Anlegen eines neuen Raums anzeigt.
    /// </summary>
    /// <returns>Gibt eine Task zurück</returns>
    // GET: Raum/Create
    public async Task<IActionResult> Create()
    {
        await AuswahllistenSetzenAsync(null);
        return View();
    }

    /// <summary>
    /// Methode, die einen neuen Raum anlegt, sofern die Raumnummer noch nicht vergeben ist
    /// </summary>
    /// <param name="raum">Der Raum mit den Daten aus dem Formular</param>
    /// <returns>Gibt eine Task zurück</returns>
    // POST: Raum/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("ID,PersonID,RaumartID")] Raum raum)
    {
        // Navigationseigenschaften werden nicht gebunden, sonst schlägt die Validierung fehl
        ModelState.Remove(nameof(Raum.Raumart));

        // Die Raumnummer ist der Primärschlüssel und steht in der Adresse der Seiten (z. B. Raum/Details/309),
        // daher muss sie eindeutig sein und darf keine Zeichen wie / oder Leerzeichen enthalten
        if (raum.ID != null)
        {
            raum.ID = raum.ID.Trim();
            if (!Regex.IsMatch(raum.ID, "^[A-Za-z0-9._-]+$"))
            {
                ModelState.AddModelError(nameof(Raum.ID), "Die Raumnummer darf nur Buchstaben, Ziffern, Punkte, Binde- und Unterstriche enthalten.");
            }
            else if (await _context.Raum.AnyAsync(r => r.ID == raum.ID))
            {
                ModelState.AddModelError(nameof(Raum.ID), $"Den Raum {raum.ID} gibt es bereits.");
            }
        }

        await EingabenPruefenAsync(raum);

        if (ModelState.IsValid)
        {
            _context.Add(raum);
            await _context.SaveChangesAsync();
            TempData["Meldung"] = $"Raum {raum.ID} wurde angelegt.";
            return RedirectToAction(nameof(Index));
        }
        await AuswahllistenSetzenAsync(raum);
        return View(raum);
    }

    /// <summary>
    /// Methode, die das Formular zum Bearbeiten eines Raums anzeigt.
    /// </summary>
    /// <param name="id">Die ID des Raums, der bearbeitet werden soll</param>
    /// <returns>Gibt eine Task zurück</returns>
    // GET: Raum/Edit/A101
    public async Task<IActionResult> Edit(string? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var raum = await _context.Raum.FindAsync(id);
        if (raum == null)
        {
            return NotFound();
        }
        await AuswahllistenSetzenAsync(raum);
        return View(raum);
    }

    /// <summary>
    /// Methode, die die Änderungen an einem Raum speichert. Wechselt die verantwortliche Person,
    /// ist ab sofort die neue Person für den Raum zuständig, auch für schon offene Lagerbewegungen.
    /// </summary>
    /// <param name="id">Die ID des Raums, der bearbeitet werden soll</param>
    /// <param name="raum">Der Raum mit den geänderten Daten aus dem Formular</param>
    /// <returns>Gibt eine Task zurück</returns>
    /// <exception cref="DbUpdateConcurrencyException">Exception, falls der Raum gleichzeitig von jemand anderem geändert wurde</exception>
    // POST: Raum/Edit/A101
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string? id, [Bind("ID,PersonID,RaumartID")] Raum raum)
    {
        // Navigationseigenschaften werden nicht gebunden, sonst schlägt die Validierung fehl
        ModelState.Remove(nameof(Raum.Raumart));

        if (id != raum.ID)
        {
            return NotFound();
        }

        await EingabenPruefenAsync(raum);

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(raum);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RaumExists(raum.ID))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            TempData["Meldung"] = $"Raum {raum.ID} wurde gespeichert.";
            return RedirectToAction(nameof(Index));
        }
        await AuswahllistenSetzenAsync(raum);
        return View(raum);
    }

    /// <summary>
    /// Methode, die die Bestätigungsseite zum Löschen eines Raums anzeigt.
    /// </summary>
    /// <param name="id">Die ID des Raums, der gelöscht werden soll</param>
    /// <returns>Gibt eine Task zurück</returns>
    // GET: Raum/Delete/A101
    public async Task<IActionResult> Delete(string? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var raum = await _context.Raum
            .Include(r => r.Raumart)
            .Include(r => r.Person)
            .FirstOrDefaultAsync(m => m.ID == id);
        if (raum == null)
        {
            return NotFound();
        }

        ViewData["LoeschHindernis"] = await LoeschHindernisAsync(raum);
        return View(raum);
    }

    /// <summary>
    /// Methode, die einen Raum nach der Bestätigung löscht, sofern er leer ist und in keiner Lagerbewegung vorkommt.
    /// </summary>
    /// <param name="id">Die ID des Raums, der gelöscht werden soll</param>
    /// <returns>Gibt eine Task zurück</returns>
    // POST: Raum/Delete/A101
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string? id)
    {
        var raum = await _context.Raum.FindAsync(id);
        if (raum == null)
        {
            return RedirectToAction(nameof(Index));
        }

        // Nochmals prüfen, da der POST auch ohne die Bestätigungsseite abgeschickt werden kann
        var hindernis = await LoeschHindernisAsync(raum);
        if (hindernis != null)
        {
            TempData["Fehler"] = hindernis;
            return RedirectToAction(nameof(Index));
        }

        _context.Raum.Remove(raum);
        await _context.SaveChangesAsync();

        TempData["Meldung"] = $"Raum {raum.ID} wurde gelöscht.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Methode, die überprüft, ob ein Raum mit der angegebenen ID existiert.
    /// </summary>
    /// <param name="id">Die ID des Raums, der überprüft werden soll</param>
    /// <returns>Gibt True oder False zurück</returns>
    private bool RaumExists(string? id)
    {
        return _context.Raum.Any(e => e.ID == id);
    }

    /// <summary>
    /// Methode, die die Auswahllisten für Raumart und verantwortliche Person für das Formular setzt.
    /// Angeboten werden nur Personen, die noch für keinen anderen Raum verantwortlich sind.
    /// </summary>
    /// <param name="raum">Der Raum, dessen Werte vorausgewählt werden sollen, oder null bei einem neuen Raum</param>
    /// <returns>Gibt eine Task zurück</returns>
    private async Task AuswahllistenSetzenAsync(Raum? raum)
    {
        ViewData["RaumartID"] = new SelectList(await _context.Raumart.OrderBy(r => r.Name).ToListAsync(), "ID", "Name", raum?.RaumartID);

        // Personen ohne Raum und die bisher verantwortliche Person dieses Raums
        var raumID = raum?.ID;
        var personen = await _context.Person
            .Where(p => !p.Raum.Any(r => r.ID != raumID))
            .OrderBy(p => p.Nachname).ThenBy(p => p.Vorname)
            .Select(p => new { p.ID, Name = p.Vorname + " " + p.Nachname + " (" + p.Username + ")" })
            .ToListAsync();
        ViewData["PersonID"] = new SelectList(personen, "ID", "Name", raum?.PersonID);
    }

    /// <summary>
    /// Methode, die Raumart und verantwortliche Person überprüft und Fehler im ModelState einträgt.
    /// </summary>
    /// <param name="raum">Der Raum, der gespeichert werden soll</param>
    /// <returns>Gibt eine Task zurück</returns>
    private async Task EingabenPruefenAsync(Raum raum)
    {
        if (!await _context.Raumart.AnyAsync(r => r.ID == raum.RaumartID))
        {
            ModelState.AddModelError(nameof(Raum.RaumartID), "Bitte eine Raumart auswählen.");
        }

        if (raum.PersonID == null)
        {
            return;
        }

        if (!await _context.Person.AnyAsync(p => p.ID == raum.PersonID))
        {
            ModelState.AddModelError(nameof(Raum.PersonID), "Diese Person gibt es nicht.");
            return;
        }

        var andererRaum = await _lagerService.AndererRaumAsync(raum.PersonID.Value, raum.ID);
        if (andererRaum != null)
        {
            ModelState.AddModelError(nameof(Raum.PersonID),
                $"Diese Person ist bereits für den Raum {andererRaum} verantwortlich. Eine Person kann nur für einen Raum verantwortlich sein.");
        }
    }

    /// <summary>
    /// Methode, die überprüft, ob ein Raum gelöscht werden darf.
    /// </summary>
    /// <param name="raum">Der Raum, der gelöscht werden soll</param>
    /// <returns>Der Grund, warum der Raum nicht gelöscht werden darf, oder null, wenn das Löschen erlaubt ist</returns>
    private async Task<string?> LoeschHindernisAsync(Raum raum)
    {
        // Solange noch etwas im Raum liegt, ginge dieser Bestand beim Löschen verloren
        if (await _context.Raumbestand.AnyAsync(r => r.RaumID == raum.ID))
        {
            return $"Im Raum {raum.ID} ist noch Bestand vorhanden. Der Raum kann erst gelöscht werden, wenn er leer ist.";
        }

        // Lagerbewegungen werden nie gelöscht, sonst wäre die Historie nicht mehr vollständig
        var anzahlLagerbewegungen = await _context.Lagerbewegung.CountAsync(l => l.VonRaumID == raum.ID || l.NachRaumID == raum.ID);
        if (anzahlLagerbewegungen > 0)
        {
            return $"Der Raum {raum.ID} kommt in {anzahlLagerbewegungen} Lagerbewegung(en) vor und kann daher nicht gelöscht werden, sonst ginge die Historie verloren.";
        }

        return null;
    }
}
