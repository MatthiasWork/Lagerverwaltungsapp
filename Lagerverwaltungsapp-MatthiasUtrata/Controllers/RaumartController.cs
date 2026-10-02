using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Lagerverwaltungsapp_MatthiasUtrata.Models;

namespace Lagerverwaltungsapp_MatthiasUtrata.Controllers;

// Raumarten verwalten darf nur ein Admin
[Authorize(Roles = "Admin")]
public class RaumartController : Controller
{
    private readonly LagerverwaltungContext _context;

    /// <summary>
    /// Konstruktor der RaumartController-Klasse.
    /// </summary>
    /// <param name="context"></param>
    public RaumartController(LagerverwaltungContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Methode, die alle Raumarten anzeigt.
    /// </summary>
    /// <returns>Gibt eine Task zurück</returns>
    // GET: Raumart
    public async Task<IActionResult> Index()
    {
        // Die Räume werden mitgeladen, um anzuzeigen, welche Räume diese Raumart haben
        return View(await _context.Raumart.Include(r => r.Raum).ToListAsync());
    }

    /// <summary>
    /// Methode, die Details einer Raumart anzeigt.
    /// </summary>
    /// <param name="id">Die ID der Raumart, deren Details angezeigt werden sollen</param>
    /// <returns>Gibt eine Task zurück</returns>
    // GET: Raumart/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var raumart = await _context.Raumart
            .FirstOrDefaultAsync(m => m.ID == id);
        if (raumart == null)
        {
            return NotFound();
        }

        return View(raumart);
    }

    /// <summary>
    /// Methode, die das Formular zum Erstellen einer neuen Raumart anzeigt.
    /// </summary>
    /// <returns>Gibt ein IActionResult zurück</returns>
    // GET: Raumart/Create
    public IActionResult Create()
    {
        return View();
    }

    /// <summary>
    /// Methode, die eine neue Raumart erstellt und in der Datenbank speichert.
    /// </summary>
    /// <param name="raumart">Die zu erstellende Raumart</param>
    /// <returns>Gibt eine Task zurück</returns>
    // POST: Raumart/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("ID,Name")] Raumart raumart)
    {
        if (ModelState.IsValid)
        {
            _context.Add(raumart);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(raumart);
    }

    /// <summary>
    /// Methode, die das Formular zum Bearbeiten einer Raumart anzeigt.
    /// </summary>
    /// <param name="id">Die ID der Raumart, die bearbeitet werden soll</param>
    /// <returns>Gibt eine Task zurück</returns>
    // GET: Raumart/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var raumart = await _context.Raumart.FindAsync(id);
        if (raumart == null)
        {
            return NotFound();
        }
        return View(raumart);
    }

    /// <summary>
    /// Methode, die eine bearbeitete Raumart speichert und in der Datenbank aktualisiert.
    /// </summary>
    /// <param name="id">Die ID der Raumart, die aktualisiert werden soll</param>
    /// <param name="raumart">Die aktualisierte Raumart</param>
    /// <returns>Gibt eine Task zurück</returns>
    // POST: Raumart/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? id, [Bind("ID,Name")] Raumart raumart)
    {
        if (id != raumart.ID)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(raumart);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RaumartExists(raumart.ID))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }
        return View(raumart);
    }

    /// <summary>
    /// Methode, die das Formular zum Löschen einer Raumart anzeigt.
    /// </summary>
    /// <param name="id">Die ID der Raumart, die gelöscht werden soll</param>
    /// <returns>Gibt eine Task zurück</returns>
    // GET: Raumart/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var raumart = await _context.Raumart
            .FirstOrDefaultAsync(m => m.ID == id);
        if (raumart == null)
        {
            return NotFound();
        }

        ViewData["LoeschHindernis"] = await LoeschHindernisAsync(raumart);
        return View(raumart);
    }

    /// <summary>
    /// Methode, die eine Raumart aus der Datenbank löscht.
    /// </summary>
    /// <param name="id">Die ID der Raumart, die gelöscht werden soll</param>
    /// <returns>Gibt eine Task zurück</returns>
    // POST: Raumart/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var raumart = await _context.Raumart.FindAsync(id);
        if (raumart != null)
        {
            // Nochmals prüfen, da der POST auch ohne die Bestätigungsseite abgeschickt werden kann
            var hindernis = await LoeschHindernisAsync(raumart);
            if (hindernis != null)
            {
                TempData["Fehler"] = hindernis;
                return RedirectToAction(nameof(Index));
            }

            _context.Raumart.Remove(raumart);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Methode, die überprüft, ob eine Raumart mit der angegebenen ID existiert.
    /// </summary>
    /// <param name="id">Die ID der Raumart, die überprüft werden soll</param>
    /// <returns>Gibt True oder False zurück</returns>
    private bool RaumartExists(int? id)
    {
        return _context.Raumart.Any(e => e.ID == id);
    }

    /// <summary>
    /// Methode, die überprüft, ob eine Raumart gelöscht werden darf. Jeder Raum braucht eine Raumart.
    /// </summary>
    /// <param name="raumart">Die Raumart, die gelöscht werden soll</param>
    /// <returns>Der Grund, warum die Raumart nicht gelöscht werden darf, oder null, wenn das Löschen erlaubt ist</returns>
    private async Task<string?> LoeschHindernisAsync(Raumart raumart)
    {
        var raeume = await _context.Raum.Where(r => r.RaumartID == raumart.ID).OrderBy(r => r.ID).Select(r => r.ID).ToListAsync();
        if (raeume.Count > 0)
        {
            string zugeordnet;
            if (raeume.Count == 1)
            {
                zugeordnet = $"ist noch der Raum {raeume[0]} zugeordnet. Dieser muss";
            }
            else
            {
                zugeordnet = $"sind noch {raeume.Count} Räume zugeordnet ({string.Join(", ", raeume)}). Diese müssen";
            }
            return $"Der Raumart \"{raumart.Name}\" {zugeordnet} zuerst eine andere Raumart bekommen oder gelöscht werden.";
        }

        return null;
    }
}
