
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Lagerverwaltungsapp_MatthiasUtrata.Models;

// Hersteller verwalten darf nur ein Admin
[Authorize(Roles = "Admin")]
public class HerstellerController : Controller
{
    private readonly LagerverwaltungContext _context;

    /// <summary>
    /// Konstruktor der HerstellerController-Klasse.
    /// </summary>
    /// <param name="context">Der Datenbankkontext der Lagerverwaltung</param>
    public HerstellerController(LagerverwaltungContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Methode, die alle Hersteller mit der Anzahl ihrer Gegenstände anzeigt.
    /// </summary>
    /// <returns>Gibt eine Task zurück</returns>
    // GET: Hersteller
    public async Task<IActionResult> Index()
    {
        // Die Gegenstände werden mitgeladen, um anzuzeigen, wie viele einem Hersteller zugeordnet sind
        return View(await _context.Hersteller.Include(h => h.Gegenstand).OrderBy(h => h.Name).ToListAsync());
    }

    /// <summary>
    /// Methode, die die Details eines Herstellers anzeigt.
    /// </summary>
    /// <param name="id">Die ID des Herstellers, der angezeigt werden soll</param>
    /// <returns>Gibt eine Task zurück</returns>
    // GET: Hersteller/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var hersteller = await _context.Hersteller
            .Include(h => h.Gegenstand)
            .FirstOrDefaultAsync(m => m.ID == id);
        if (hersteller == null)
        {
            return NotFound();
        }

        return View(hersteller);
    }

    /// <summary>
    /// Methode, die das Formular zum Anlegen eines neuen Herstellers anzeigt.
    /// </summary>
    /// <returns>Gibt ein IActionResult zurück</returns>
    // GET: Hersteller/Create
    public IActionResult Create()
    {
        return View();
    }

    /// <summary>
    /// Methode, die einen neuen Hersteller anlegt, sofern es noch keinen Hersteller mit diesem Namen gibt.
    /// </summary>
    /// <param name="hersteller">Der Hersteller mit den Daten aus dem Formular</param>
    /// <returns>Gibt eine Task zurück</returns>
    // POST: Hersteller/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name")] Hersteller hersteller)
    {
        await NameEindeutigPruefenAsync(hersteller);

        if (ModelState.IsValid)
        {
            _context.Add(hersteller);
            await _context.SaveChangesAsync();
            TempData["Meldung"] = $"Hersteller \"{hersteller.Name}\" wurde angelegt.";
            return RedirectToAction(nameof(Index));
        }
        return View(hersteller);
    }

    /// <summary>
    /// Methode, die das Formular zum Bearbeiten eines Herstellers anzeigt.
    /// </summary>
    /// <param name="id">Die ID des Herstellers, der bearbeitet werden soll</param>
    /// <returns>Gibt eine Task zurück</returns>
    // GET: Hersteller/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var hersteller = await _context.Hersteller.FindAsync(id);
        if (hersteller == null)
        {
            return NotFound();
        }
        return View(hersteller);
    }

    /// <summary>
    /// Methode, die die Änderungen an einem Hersteller speichert, sofern der Name noch nicht vergeben ist.
    /// </summary>
    /// <param name="id">Die ID des Herstellers, der bearbeitet werden soll</param>
    /// <param name="hersteller">Der Hersteller mit den geänderten Daten aus dem Formular</param>
    /// <returns>Gibt eine Task zurück</returns>
    /// <exception cref="DbUpdateConcurrencyException">Exception, falls der Hersteller gleichzeitig von jemand anderem geändert wurde</exception>
    // POST: Hersteller/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? id, [Bind("ID,Name")] Hersteller hersteller)
    {
        if (id != hersteller.ID)
        {
            return NotFound();
        }

        await NameEindeutigPruefenAsync(hersteller);

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(hersteller);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!HerstellerExists(hersteller.ID))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            TempData["Meldung"] = $"Hersteller \"{hersteller.Name}\" wurde gespeichert.";
            return RedirectToAction(nameof(Index));
        }
        return View(hersteller);
    }

    /// <summary>
    /// Methode, die die Bestätigungsseite zum Löschen eines Herstellers anzeigt.
    /// </summary>
    /// <param name="id">Die ID des Herstellers, der gelöscht werden soll</param>
    /// <returns>Gibt eine Task zurück</returns>
    // GET: Hersteller/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var hersteller = await _context.Hersteller
            .FirstOrDefaultAsync(m => m.ID == id);
        if (hersteller == null)
        {
            return NotFound();
        }

        ViewData["LoeschHindernis"] = await LoeschHindernisAsync(hersteller);
        return View(hersteller);
    }

    /// <summary>
    /// Methode, die einen Hersteller nach der Bestätigung löscht, sofern ihm keine Gegenstände mehr zugeordnet sind.
    /// </summary>
    /// <param name="id">Die ID des Herstellers, der gelöscht werden soll</param>
    /// <returns>Gibt eine Task zurück</returns>
    // POST: Hersteller/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var hersteller = await _context.Hersteller.FindAsync(id);
        if (hersteller == null)
        {
            return RedirectToAction(nameof(Index));
        }

        // Nochmals prüfen, da der POST auch ohne die Bestätigungsseite abgeschickt werden kann
        var hindernis = await LoeschHindernisAsync(hersteller);
        if (hindernis != null)
        {
            TempData["Fehler"] = hindernis;
            return RedirectToAction(nameof(Index));
        }

        _context.Hersteller.Remove(hersteller);
        await _context.SaveChangesAsync();

        TempData["Meldung"] = $"Hersteller \"{hersteller.Name}\" wurde gelöscht.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Methode, die überprüft, ob ein Hersteller mit der angegebenen ID existiert.
    /// </summary>
    /// <param name="id">Die ID des Herstellers, der überprüft werden soll</param>
    /// <returns>Gibt True oder False zurück</returns>
    private bool HerstellerExists(int? id)
    {
        return _context.Hersteller.Any(e => e.ID == id);
    }

    /// <summary>
    /// Methode, die überprüft, ob es schon einen anderen Hersteller mit demselben Namen gibt, und dann einen Fehler im ModelState einträgt.
    /// Zwei gleichnamige Hersteller könnte man in den Auswahllisten nicht unterscheiden.
    /// </summary>
    /// <param name="hersteller">Der Hersteller, der gespeichert werden soll</param>
    /// <returns>Gibt eine Task zurück</returns>
    private async Task NameEindeutigPruefenAsync(Hersteller hersteller)
    {
        if (await _context.Hersteller.AnyAsync(h => h.Name == hersteller.Name && h.ID != hersteller.ID))
        {
            ModelState.AddModelError(nameof(Hersteller.Name), "Diesen Hersteller gibt es bereits.");
        }
    }

    /// <summary>
    /// Methode, die überprüft, ob ein Hersteller gelöscht werden darf.
    /// </summary>
    /// <param name="hersteller">Der Hersteller, der gelöscht werden soll</param>
    /// <returns>Der Grund, warum der Hersteller nicht gelöscht werden darf, oder null, wenn das Löschen erlaubt ist</returns>
    private async Task<string?> LoeschHindernisAsync(Hersteller hersteller)
    {
        var anzahlGegenstaende = await _context.Gegenstand.CountAsync(g => g.HerstellerID == hersteller.ID);
        if (anzahlGegenstaende > 0)
        {
            var gegenstaende = anzahlGegenstaende == 1 ? "ist noch 1 Gegenstand" : $"sind noch {anzahlGegenstaende} Gegenstände";
            return $"Dem Hersteller \"{hersteller.Name}\" {gegenstaende} zugeordnet. "
                + "Diese müssen zuerst einem anderen Hersteller zugeordnet oder gelöscht werden.";
        }

        return null;
    }
}
