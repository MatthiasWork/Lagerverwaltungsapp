
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Lagerverwaltungsapp_MatthiasUtrata.Models;

public class BewegungsartController : Controller
{
    private readonly LagerverwaltungContext _context;

    public BewegungsartController(LagerverwaltungContext context)
    {
        _context = context;
    }

    // GET: BEWEGUNGSARTS
    public async Task<IActionResult> Index()    
    {
        return View(await _context.Bewegungsart.ToListAsync());
    }

    // GET: BEWEGUNGSARTS/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var bewegungsart = await _context.Bewegungsart
            .FirstOrDefaultAsync(m => m.ID == id);
        if (bewegungsart == null)
        {
            return NotFound();
        }

        return View(bewegungsart);
    }

    // GET: BEWEGUNGSARTS/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: BEWEGUNGSARTS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("ID,Name,Lagerbewegung")] Bewegungsart bewegungsart)
    {
        if (ModelState.IsValid)
        {
            _context.Add(bewegungsart);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(bewegungsart);
    }

    // GET: BEWEGUNGSARTS/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var bewegungsart = await _context.Bewegungsart.FindAsync(id);
        if (bewegungsart == null)
        {
            return NotFound();
        }
        return View(bewegungsart);
    }

    // POST: BEWEGUNGSARTS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? id, [Bind("ID,Name,Lagerbewegung")] Bewegungsart bewegungsart)
    {
        if (id != bewegungsart.ID)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(bewegungsart);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!BewegungsartExists(bewegungsart.ID))
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
        return View(bewegungsart);
    }

    // GET: BEWEGUNGSARTS/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var bewegungsart = await _context.Bewegungsart
            .FirstOrDefaultAsync(m => m.ID == id);
        if (bewegungsart == null)
        {
            return NotFound();
        }

        return View(bewegungsart);
    }

    // POST: BEWEGUNGSARTS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var bewegungsart = await _context.Bewegungsart.FindAsync(id);
        if (bewegungsart != null)
        {
            _context.Bewegungsart.Remove(bewegungsart);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool BewegungsartExists(int? id)
    {
        return _context.Bewegungsart.Any(e => e.ID == id);
    }
}
