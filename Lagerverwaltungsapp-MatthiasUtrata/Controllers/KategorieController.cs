using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Lagerverwaltungsapp_MatthiasUtrata.Models;

namespace Lagerverwaltungsapp_MatthiasUtrata.Controllers;

// Kategorien verwalten darf nur ein Admin
[Authorize(Roles = "Admin")]
public class KategorieController : Controller
{
    private readonly LagerverwaltungContext _context;

    /// <summary>
    /// Konstruktor der KategorieController-Klasse.
    /// </summary>
    /// <param name="context">Der Datenbankkontext der Lagerverwaltung</param>
    public KategorieController(LagerverwaltungContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Methode, die alle Kategorien mit der Anzahl ihrer Gegenstände anzeigt.
    /// </summary>
    /// <returns>Gibt eine Task zurück</returns>
    // GET: Kategorie
    public async Task<IActionResult> Index()
    {
        // Die Gegenstände werden mitgeladen, um anzuzeigen, wie viele einer Kategorie zugeordnet sind
        return View(await _context.Kategorie.Include(k => k.Gegenstand).OrderBy(k => k.Name).ToListAsync());
    }

    /// <summary>
    /// Methode, die die Details einer Kategorie anzeigt.
    /// </summary>
    /// <param name="id">Die ID der Kategorie, die angezeigt werden soll</param>
    /// <returns>Gibt eine Task zurück</returns>
    // GET: Kategorie/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var kategorie = await _context.Kategorie
            .Include(k => k.Gegenstand)
            .FirstOrDefaultAsync(m => m.ID == id);
        if (kategorie == null)
        {
            return NotFound();
        }

        return View(kategorie);
    }

    /// <summary>
    /// Methode, die das Formular zum Anlegen einer neuen Kategorie anzeigt.
    /// </summary>
    /// <returns>Gibt ein IActionResult zurück</returns>
    // GET: Kategorie/Create
    public IActionResult Create()
    {
        return View();
    }

    /// <summary>
    /// Methode, die eine neue Kategorie anlegt, sofern es noch keine Kategorie mit diesem Namen gibt.
    /// </summary>
    /// <param name="kategorie">Die Kategorie mit den Daten aus dem Formular</param>
    /// <returns>Gibt eine Task zurück</returns>
    // POST: Kategorie/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name")] Kategorie kategorie)
    {
        await NameEindeutigPruefenAsync(kategorie);

        if (ModelState.IsValid)
        {
            _context.Add(kategorie);
            await _context.SaveChangesAsync();
            TempData["Meldung"] = $"Kategorie \"{kategorie.Name}\" wurde angelegt.";
            return RedirectToAction(nameof(Index));
        }
        return View(kategorie);
    }

    /// <summary>
    /// Methode, die das Formular zum Bearbeiten einer Kategorie anzeigt.
    /// </summary>
    /// <param name="id">Die ID der Kategorie, die bearbeitet werden soll</param>
    /// <returns>Gibt eine Task zurück</returns>
    // GET: Kategorie/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var kategorie = await _context.Kategorie.FindAsync(id);
        if (kategorie == null)
        {
            return NotFound();
        }
        return View(kategorie);
    }

    /// <summary>
    /// Methode, die die Änderungen an einer Kategorie speichert, sofern der Name noch nicht vergeben ist.
    /// </summary>
    /// <param name="id">Die ID der Kategorie, die bearbeitet werden soll</param>
    /// <param name="kategorie">Die Kategorie mit den geänderten Daten aus dem Formular</param>
    /// <returns>Gibt eine Task zurück</returns>
    /// <exception cref="DbUpdateConcurrencyException">Exception, falls die Kategorie gleichzeitig von jemand anderem geändert wurde</exception>
    // POST: Kategorie/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? id, [Bind("ID,Name")] Kategorie kategorie)
    {
        if (id != kategorie.ID)
        {
            return NotFound();
        }

        await NameEindeutigPruefenAsync(kategorie);

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(kategorie);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!KategorieExists(kategorie.ID))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            TempData["Meldung"] = $"Kategorie \"{kategorie.Name}\" wurde gespeichert.";
            return RedirectToAction(nameof(Index));
        }
        return View(kategorie);
    }

    /// <summary>
    /// Methode, die die Bestätigungsseite zum Löschen einer Kategorie anzeigt.
    /// </summary>
    /// <param name="id">Die ID der Kategorie, die gelöscht werden soll</param>
    /// <returns>Gibt eine Task zurück</returns>
    // GET: Kategorie/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var kategorie = await _context.Kategorie
            .FirstOrDefaultAsync(m => m.ID == id);
        if (kategorie == null)
        {
            return NotFound();
        }

        ViewData["LoeschHindernis"] = await LoeschHindernisAsync(kategorie);
        return View(kategorie);
    }

    /// <summary>
    /// Methode, die eine Kategorie nach der Bestätigung löscht, sofern ihr keine Gegenstände mehr zugeordnet sind.
    /// </summary>
    /// <param name="id">Die ID der Kategorie, die gelöscht werden soll</param>
    /// <returns>Gibt eine Task zurück</returns>
    // POST: Kategorie/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var kategorie = await _context.Kategorie.FindAsync(id);
        if (kategorie == null)
        {
            return RedirectToAction(nameof(Index));
        }

        // Nochmals prüfen, da der POST auch ohne die Bestätigungsseite abgeschickt werden kann
        var hindernis = await LoeschHindernisAsync(kategorie);
        if (hindernis != null)
        {
            TempData["Fehler"] = hindernis;
            return RedirectToAction(nameof(Index));
        }

        _context.Kategorie.Remove(kategorie);
        await _context.SaveChangesAsync();

        TempData["Meldung"] = $"Kategorie \"{kategorie.Name}\" wurde gelöscht.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Methode, die überprüft, ob eine Kategorie mit der angegebenen ID existiert.
    /// </summary>
    /// <param name="id">Die ID der Kategorie, die überprüft werden soll</param>
    /// <returns>Gibt True oder False zurück</returns>
    private bool KategorieExists(int? id)
    {
        return _context.Kategorie.Any(e => e.ID == id);
    }

    /// <summary>
    /// Methode, die überprüft, ob es schon eine andere Kategorie mit demselben Namen gibt, und dann einen Fehler im ModelState einträgt.
    /// Zwei gleichnamige Kategorien könnte man in den Auswahllisten nicht unterscheiden.
    /// </summary>
    /// <param name="kategorie">Die Kategorie, die gespeichert werden soll</param>
    /// <returns>Gibt eine Task zurück</returns>
    private async Task NameEindeutigPruefenAsync(Kategorie kategorie)
    {
        if (await _context.Kategorie.AnyAsync(k => k.Name == kategorie.Name && k.ID != kategorie.ID))
        {
            ModelState.AddModelError(nameof(Kategorie.Name), "Diese Kategorie gibt es bereits.");
        }
    }

    /// <summary>
    /// Methode, die überprüft, ob eine Kategorie gelöscht werden darf.
    /// </summary>
    /// <param name="kategorie">Die Kategorie, die gelöscht werden soll</param>
    /// <returns>Der Grund, warum die Kategorie nicht gelöscht werden darf, oder null, wenn das Löschen erlaubt ist</returns>
    private async Task<string?> LoeschHindernisAsync(Kategorie kategorie)
    {
        var anzahlGegenstaende = await _context.Gegenstand.CountAsync(g => g.KategorieID == kategorie.ID);
        if (anzahlGegenstaende > 0)
        {
            string gegenstaende;
            if (anzahlGegenstaende == 1)
            {
                gegenstaende = "ist noch 1 Gegenstand";
            }
            else
            {
                gegenstaende = $"sind noch {anzahlGegenstaende} Gegenstände";
            }
            return $"Der Kategorie \"{kategorie.Name}\" {gegenstaende} zugeordnet. "
                + "Diese müssen zuerst einer anderen Kategorie zugeordnet oder gelöscht werden.";
        }

        return null;
    }
}
