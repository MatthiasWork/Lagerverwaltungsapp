
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Lagerverwaltungsapp_MatthiasUtrata.Models;

// Gegenstände verwalten darf nur ein Admin
[Authorize(Roles = "Admin")]
public class GegenstandController : Controller
{
    private readonly LagerverwaltungContext _context;

    public GegenstandController(LagerverwaltungContext context)
    {
        _context = context;
    }

    // GET: Gegenstand
    public async Task<IActionResult> Index()    
    {
        return View(await _context.Gegenstand.ToListAsync());
    }

    // GET: Gegenstand/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var gegenstand = await _context.Gegenstand
            .FirstOrDefaultAsync(m => m.ID == id);
        if (gegenstand == null)
        {
            return NotFound();
        }

        return View(gegenstand);
    }

    // GET: Gegenstand/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: Gegenstand/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("ID,Name,Seriennummer,KategorieID,HerstellerID")] Gegenstand gegenstand)
    {
        // Navigationseigenschaften werden nicht gebunden, sonst schlägt die Validierung fehl
        ModelState.Remove(nameof(Gegenstand.Hersteller));
        ModelState.Remove(nameof(Gegenstand.Kategorie));

        if (ModelState.IsValid)
        {
            _context.Add(gegenstand);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(gegenstand);
    }

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
        return View(gegenstand);
    }

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
            return RedirectToAction(nameof(Index));
        }
        return View(gegenstand);
    }

    // GET: Gegenstand/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var gegenstand = await _context.Gegenstand
            .FirstOrDefaultAsync(m => m.ID == id);
        if (gegenstand == null)
        {
            return NotFound();
        }

        return View(gegenstand);
    }

    // POST: Gegenstand/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var gegenstand = await _context.Gegenstand.FindAsync(id);
        if (gegenstand != null)
        {
            _context.Gegenstand.Remove(gegenstand);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool GegenstandExists(int? id)
    {
        return _context.Gegenstand.Any(e => e.ID == id);
    }
}
