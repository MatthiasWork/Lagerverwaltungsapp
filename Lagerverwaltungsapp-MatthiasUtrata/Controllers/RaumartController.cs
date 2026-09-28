
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Lagerverwaltungsapp_MatthiasUtrata.Models;

public class RaumartController : Controller
{
    private readonly LagerverwaltungContext _context;

    public RaumartController(LagerverwaltungContext context)
    {
        _context = context;
    }

    // GET: RAUMARTS
    public async Task<IActionResult> Index()    
    {
        return View(await _context.Raumart.ToListAsync());
    }

    // GET: RAUMARTS/Details/5
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

    // GET: RAUMARTS/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: RAUMARTS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("ID,Name,Raum")] Raumart raumart)
    {
        if (ModelState.IsValid)
        {
            _context.Add(raumart);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(raumart);
    }

    // GET: RAUMARTS/Edit/5
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

    // POST: RAUMARTS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? id, [Bind("ID,Name,Raum")] Raumart raumart)
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

    // GET: RAUMARTS/Delete/5
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

        return View(raumart);
    }

    // POST: RAUMARTS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var raumart = await _context.Raumart.FindAsync(id);
        if (raumart != null)
        {
            _context.Raumart.Remove(raumart);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool RaumartExists(int? id)
    {
        return _context.Raumart.Any(e => e.ID == id);
    }
}
