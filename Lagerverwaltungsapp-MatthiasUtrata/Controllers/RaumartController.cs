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

    public RaumartController(LagerverwaltungContext context)
    {
        _context = context;
    }

    // GET: Raumart
    public async Task<IActionResult> Index()
    {
        // Die Räume werden mitgeladen, um anzuzeigen, welche Räume diese Raumart haben
        return View(await _context.Raumart.Include(r => r.Raum).ToListAsync());
    }

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

    // GET: Raumart/Create
    public IActionResult Create()
    {
        return View();
    }

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
