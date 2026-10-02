using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Lagerverwaltungsapp_MatthiasUtrata.Models;

namespace Lagerverwaltungsapp_MatthiasUtrata.Controllers;

// Rollen verwalten darf nur ein Admin
[Authorize(Roles = "Admin")]
public class RolleController : Controller
{
    private readonly LagerverwaltungContext _context;

    /// <summary>
    /// Konstruktor für den RolleController, der den Datenbankkontext injiziert.
    /// </summary>
    /// <param name="context">Der Datenbankkontext</param>
    public RolleController(LagerverwaltungContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Methode, die die Liste aller Rollen aus der Datenbank abruft und an die View übergibt.
    /// </summary>
    /// <returns>Gibt eine Task zurück</returns>
    // GET: Rolle
    public async Task<IActionResult> Index()
    {
        return View(await _context.Rolle.ToListAsync());
    }

    /// <summary>
    /// Methode, die die Details einer bestimmten Rolle basierend auf der übergebenen ID abruft und an die View übergibt.
    /// </summary>
    /// <param name="id">Die ID der Rolle, deren Details abgerufen werden sollen</param>
    /// <returns>Gibt eine Task zurück</returns>
    // GET: Rolle/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var rolle = await _context.Rolle
            .FirstOrDefaultAsync(m => m.ID == id);
        if (rolle == null)
        {
            return NotFound();
        }

        return View(rolle);
    }

    /// <summary>
    /// Methode, die die View zum Erstellen einer neuen Rolle zurückgibt.
    /// </summary>
    /// <returns>Gibt eine Task zurück</returns>
    // GET: Rolle/Create
    public IActionResult Create()
    {
        return View();
    }

    /// <summary>
    /// Methode, die eine neue Rolle in der Datenbank erstellt, wenn das Model gültig ist.
    /// </summary>
    /// <param name="rolle">Die Rolle, die erstellt werden soll</param>
    /// <returns>Gibt eine Task zurück</returns>
    // POST: Rolle/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("ID,Name,Admin")] Rolle rolle)
    {
        if (ModelState.IsValid)
        {
            _context.Add(rolle);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(rolle);
    }

    /// <summary>
    /// Methode, die die View zum Bearbeiten einer bestehenden Rolle basierend auf der übergebenen ID zurückgibt.
    /// </summary>
    /// <param name="id">Die ID der Rolle, die bearbeitet werden soll</param>
    /// <returns>Gibt eine Task zurück</returns>
    // GET: Rolle/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var rolle = await _context.Rolle.FindAsync(id);
        if (rolle == null)
        {
            return NotFound();
        }
        return View(rolle);
    }

    /// <summary>
    /// Methode, die eine bestehende Rolle in der Datenbank aktualisiert, wenn das Model gültig ist.
    /// </summary>
    /// <param name="id">Die ID der Rolle, die bearbeitet werden soll</param>
    /// <param name="rolle">Die Rolle mit den geänderten Daten aus dem Formular</param>
    /// <returns>Gibt eine Task zurück</returns>
    // POST: Rolle/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? id, [Bind("ID,Name,Admin")] Rolle rolle)
    {
        if (id != rolle.ID)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(rolle);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RolleExists(rolle.ID))
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
        return View(rolle);
    }

    /// <summary>
    /// Methode, die die View zum Löschen einer bestehenden Rolle basierend auf der übergebenen ID zurückgibt.
    /// </summary>
    /// <param name="id">Die ID der Rolle, die gelöscht werden soll</param>
    /// <returns>Gibt eine Task zurück</returns>
    // GET: Rolle/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var rolle = await _context.Rolle
            .FirstOrDefaultAsync(m => m.ID == id);
        if (rolle == null)
        {
            return NotFound();
        }

        return View(rolle);
    }

    /// <summary>
    /// Methode, die eine bestehende Rolle in der Datenbank löscht, wenn die Löschbestätigung erfolgt ist.
    /// </summary>
    /// <param name="id">Die ID der Rolle, die gelöscht werden soll</param>
    /// <returns>Gibt eine Task zurück</returns>
    // POST: Rolle/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var rolle = await _context.Rolle.FindAsync(id);
        if (rolle != null)
        {
            _context.Rolle.Remove(rolle);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Methode, die überprüft, ob eine Rolle mit der angegebenen ID in der Datenbank existiert.
    /// </summary>
    /// <param name="id">Die ID der Rolle, die überprüft werden soll</param>
    /// <returns>Gibt True oder False zurück</returns>
    private bool RolleExists(int? id)
    {
        return _context.Rolle.Any(e => e.ID == id);
    }
}
