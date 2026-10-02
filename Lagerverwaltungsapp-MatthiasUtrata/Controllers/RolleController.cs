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

    public RolleController(LagerverwaltungContext context)
    {
        _context = context;
    }

    // GET: Rolle
    public async Task<IActionResult> Index()
    {
        return View(await _context.Rolle.ToListAsync());
    }

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

    // GET: Rolle/Create
    public IActionResult Create()
    {
        return View();
    }

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

        ViewData["LoeschHindernis"] = await LoeschHindernisAsync(rolle);
        return View(rolle);
    }

    // POST: Rolle/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var rolle = await _context.Rolle.FindAsync(id);
        if (rolle != null)
        {
            // Nochmals prüfen, da der POST auch ohne die Bestätigungsseite abgeschickt werden kann
            var hindernis = await LoeschHindernisAsync(rolle);
            if (hindernis != null)
            {
                TempData["Fehler"] = hindernis;
                return RedirectToAction(nameof(Index));
            }

            _context.Rolle.Remove(rolle);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool RolleExists(int? id)
    {
        return _context.Rolle.Any(e => e.ID == id);
    }

    /// <summary>
    /// Methode, die überprüft, ob eine Rolle gelöscht werden darf. Jede Person braucht eine Rolle.
    /// </summary>
    /// <param name="rolle">Die Rolle, die gelöscht werden soll</param>
    /// <returns>Der Grund, warum die Rolle nicht gelöscht werden darf, oder null, wenn das Löschen erlaubt ist</returns>
    private async Task<string?> LoeschHindernisAsync(Rolle rolle)
    {
        var anzahlPersonen = await _context.Person.CountAsync(p => p.RolleID == rolle.ID);
        if (anzahlPersonen > 0)
        {
            string personen;
            if (anzahlPersonen == 1)
            {
                personen = "ist noch 1 Person zugeordnet. Diese muss";
            }
            else
            {
                personen = $"sind noch {anzahlPersonen} Personen zugeordnet. Diese müssen";
            }
            return $"Der Rolle \"{rolle.Name}\" {personen} zuerst eine andere Rolle bekommen.";
        }

        return null;
    }
}
