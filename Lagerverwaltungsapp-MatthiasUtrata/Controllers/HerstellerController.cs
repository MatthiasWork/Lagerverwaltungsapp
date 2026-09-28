
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Lagerverwaltungsapp_MatthiasUtrata.Models;

public class HerstellerController : Controller
{
    private readonly LagerverwaltungContext _context;

    public HerstellerController(LagerverwaltungContext context)
    {
        _context = context;
    }

    // GET: HERSTELLERS
    public async Task<IActionResult> Index()    
    {
        return View(await _context.Hersteller.ToListAsync());
    }

    // GET: HERSTELLERS/Details/5
    public async Task<IActionResult> Details(int? id)
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

        return View(hersteller);
    }

    // GET: HERSTELLERS/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: HERSTELLERS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("ID,Name,Gegenstand")] Hersteller hersteller)
    {
        if (ModelState.IsValid)
        {
            _context.Add(hersteller);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(hersteller);
    }

    // GET: HERSTELLERS/Edit/5
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

    // POST: HERSTELLERS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? id, [Bind("ID,Name,Gegenstand")] Hersteller hersteller)
    {
        if (id != hersteller.ID)
        {
            return NotFound();
        }

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
            return RedirectToAction(nameof(Index));
        }
        return View(hersteller);
    }

    // GET: HERSTELLERS/Delete/5
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

        return View(hersteller);
    }

    // POST: HERSTELLERS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var hersteller = await _context.Hersteller.FindAsync(id);
        if (hersteller != null)
        {
            _context.Hersteller.Remove(hersteller);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool HerstellerExists(int? id)
    {
        return _context.Hersteller.Any(e => e.ID == id);
    }
}
