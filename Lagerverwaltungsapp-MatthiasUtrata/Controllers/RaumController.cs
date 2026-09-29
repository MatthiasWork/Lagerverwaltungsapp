
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Lagerverwaltungsapp_MatthiasUtrata.Models;
using Lagerverwaltungsapp_MatthiasUtrata.Services;

// Räume verwalten darf nur ein Admin
[Authorize(Roles = "Admin")]
public class RaumController : Controller
{
    private readonly LagerverwaltungContext _context;
    private readonly LagerService _lagerService;

    public RaumController(LagerverwaltungContext context, LagerService lagerService)
    {
        _context = context;
        _lagerService = lagerService;
    }

    // GET: Raum
    public async Task<IActionResult> Index()    
    {
        return View(await _context.Raum.ToListAsync());
    }

    // GET: Raum/Details/A101
    public async Task<IActionResult> Details(string? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var raum = await _context.Raum
            .FirstOrDefaultAsync(m => m.ID == id);
        if (raum == null)
        {
            return NotFound();
        }

        return View(raum);
    }

    // GET: Raum/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: Raum/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("ID,PersonID,RaumartID")] Raum raum)
    {
        // Navigationseigenschaften werden nicht gebunden, sonst schlägt die Validierung fehl
        ModelState.Remove(nameof(Raum.Raumart));
        await VerantwortlichePruefenAsync(raum);

        if (ModelState.IsValid)
        {
            _context.Add(raum);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(raum);
    }

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
        return View(raum);
    }

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

        await VerantwortlichePruefenAsync(raum);

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
            return RedirectToAction(nameof(Index));
        }
        return View(raum);
    }

    // GET: Raum/Delete/A101
    public async Task<IActionResult> Delete(string? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var raum = await _context.Raum
            .FirstOrDefaultAsync(m => m.ID == id);
        if (raum == null)
        {
            return NotFound();
        }

        return View(raum);
    }

    // POST: Raum/Delete/A101
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string? id)
    {
        var raum = await _context.Raum.FindAsync(id);
        if (raum != null)
        {
            _context.Raum.Remove(raum);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool RaumExists(string? id)
    {
        return _context.Raum.Any(e => e.ID == id);
    }

    /// <summary>
    /// Methode, die überprüft, ob die gewählte Person schon für einen anderen Raum verantwortlich ist.
    /// Eine Person kann nur für einen Raum verantwortlich sein, sonst wird ein Fehler im ModelState eingetragen.
    /// </summary>
    /// <param name="raum">Der Raum, der gespeichert werden soll</param>
    /// <returns>Gibt eine Task zurück</returns>
    private async Task VerantwortlichePruefenAsync(Raum raum)
    {
        if (raum.PersonID == null)
        {
            return;
        }

        var andererRaum = await _lagerService.AndererRaumAsync(raum.PersonID.Value, raum.ID);
        if (andererRaum != null)
        {
            ModelState.AddModelError(nameof(Raum.PersonID),
                $"Diese Person ist bereits für den Raum {andererRaum} verantwortlich. Eine Person kann nur für einen Raum verantwortlich sein.");
        }
    }
}
