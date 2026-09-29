
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Lagerverwaltungsapp_MatthiasUtrata.Models;

// Räume verwalten darf nur ein Admin
[Authorize(Roles = "Admin")]
public class RaumController : Controller
{
    private readonly LagerverwaltungContext _context;

    public RaumController(LagerverwaltungContext context)
    {
        _context = context;
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
}
