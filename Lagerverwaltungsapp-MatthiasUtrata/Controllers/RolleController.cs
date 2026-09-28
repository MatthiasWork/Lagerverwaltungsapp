
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Lagerverwaltungsapp_MatthiasUtrata.Models;

public class RolleController : Controller
{
    private readonly LagerverwaltungContext _context;

    public RolleController(LagerverwaltungContext context)
    {
        _context = context;
    }

    // GET: ROLLES
    public async Task<IActionResult> Index()    
    {
        return View(await _context.Rolle.ToListAsync());
    }

    // GET: ROLLES/Details/5
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

    // GET: ROLLES/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: ROLLES/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("ID,Name,Admin,Person")] Rolle rolle)
    {
        if (ModelState.IsValid)
        {
            _context.Add(rolle);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(rolle);
    }

    // GET: ROLLES/Edit/5
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

    // POST: ROLLES/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? id, [Bind("ID,Name,Admin,Person")] Rolle rolle)
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

    // GET: ROLLES/Delete/5
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

        return View(rolle);
    }

    // POST: ROLLES/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var rolle = await _context.Rolle.FindAsync(id);
        if (rolle != null)
        {
            _context.Rolle.Remove(rolle);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool RolleExists(int? id)
    {
        return _context.Rolle.Any(e => e.ID == id);
    }
}
