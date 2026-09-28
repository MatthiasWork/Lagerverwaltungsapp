using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Lagerverwaltungsapp_MatthiasUtrata.Models;

// Raumbestand hat einen zusammengesetzten Schlüssel (GegenstandID + RaumID),
// daher kann dotnet-scaffold diesen Controller nicht generieren.
public class RaumbestandController : Controller
{
    private readonly LagerverwaltungContext _context;

    public RaumbestandController(LagerverwaltungContext context)
    {
        _context = context;
    }

    // GET: Raumbestand
    public async Task<IActionResult> Index()
    {
        return View(await _context.Raumbestand
            .Include(r => r.Gegenstand)
            .Include(r => r.Raum)
            .ToListAsync());
    }

    // GET: Raumbestand/Details?gegenstandID=5&raumID=A101
    public async Task<IActionResult> Details(int? gegenstandID, string? raumID)
    {
        if (gegenstandID == null || raumID == null)
        {
            return NotFound();
        }

        var raumbestand = await _context.Raumbestand
            .Include(r => r.Gegenstand)
            .Include(r => r.Raum)
            .FirstOrDefaultAsync(m => m.GegenstandID == gegenstandID && m.RaumID == raumID);
        if (raumbestand == null)
        {
            return NotFound();
        }

        return View(raumbestand);
    }

    // GET: Raumbestand/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: Raumbestand/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("GegenstandID,RaumID,Menge")] Raumbestand raumbestand)
    {
        // Navigationseigenschaften werden nicht gebunden, sonst schlägt die Validierung fehl
        ModelState.Remove(nameof(Raumbestand.Gegenstand));
        ModelState.Remove(nameof(Raumbestand.Raum));

        if (ModelState.IsValid)
        {
            _context.Add(raumbestand);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(raumbestand);
    }

    // GET: Raumbestand/Edit?gegenstandID=5&raumID=A101
    public async Task<IActionResult> Edit(int? gegenstandID, string? raumID)
    {
        if (gegenstandID == null || raumID == null)
        {
            return NotFound();
        }

        var raumbestand = await _context.Raumbestand.FindAsync(gegenstandID, raumID);
        if (raumbestand == null)
        {
            return NotFound();
        }
        return View(raumbestand);
    }

    // POST: Raumbestand/Edit
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit([Bind("GegenstandID,RaumID,Menge")] Raumbestand raumbestand)
    {
        ModelState.Remove(nameof(Raumbestand.Gegenstand));
        ModelState.Remove(nameof(Raumbestand.Raum));

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(raumbestand);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RaumbestandExists(raumbestand.GegenstandID, raumbestand.RaumID))
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
        return View(raumbestand);
    }

    // GET: Raumbestand/Delete?gegenstandID=5&raumID=A101
    public async Task<IActionResult> Delete(int? gegenstandID, string? raumID)
    {
        if (gegenstandID == null || raumID == null)
        {
            return NotFound();
        }

        var raumbestand = await _context.Raumbestand
            .Include(r => r.Gegenstand)
            .Include(r => r.Raum)
            .FirstOrDefaultAsync(m => m.GegenstandID == gegenstandID && m.RaumID == raumID);
        if (raumbestand == null)
        {
            return NotFound();
        }

        return View(raumbestand);
    }

    // POST: Raumbestand/Delete
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int gegenstandID, string raumID)
    {
        var raumbestand = await _context.Raumbestand.FindAsync(gegenstandID, raumID);
        if (raumbestand != null)
        {
            _context.Raumbestand.Remove(raumbestand);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool RaumbestandExists(int gegenstandID, string raumID)
    {
        return _context.Raumbestand.Any(e => e.GegenstandID == gegenstandID && e.RaumID == raumID);
    }
}
