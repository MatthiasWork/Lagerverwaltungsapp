
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Lagerverwaltungsapp_MatthiasUtrata.Models;

public class GegenstandController : Controller
{
    private readonly LagerverwaltungContext _context;

    public GegenstandController(LagerverwaltungContext context)
    {
        _context = context;
    }

    // GET: GEGENSTANDS
    public async Task<IActionResult> Index()    
    {
        return View(await _context.Gegenstand.ToListAsync());
    }

    // GET: GEGENSTANDS/Details/5
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

    // GET: GEGENSTANDS/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: GEGENSTANDS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("ID,Name,Seriennummer,KategorieID,HerstellerID,Hersteller,Kategorie,Lagerbewegung,Raumbestand")] Gegenstand gegenstand)
    {
        if (ModelState.IsValid)
        {
            _context.Add(gegenstand);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(gegenstand);
    }

    // GET: GEGENSTANDS/Edit/5
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

    // POST: GEGENSTANDS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? id, [Bind("ID,Name,Seriennummer,KategorieID,HerstellerID,Hersteller,Kategorie,Lagerbewegung,Raumbestand")] Gegenstand gegenstand)
    {
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

    // GET: GEGENSTANDS/Delete/5
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

    // POST: GEGENSTANDS/Delete/5
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
