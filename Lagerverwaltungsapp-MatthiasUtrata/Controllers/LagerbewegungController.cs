
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Lagerverwaltungsapp_MatthiasUtrata.Models;

public class LagerbewegungController : Controller
{
    private readonly LagerverwaltungContext _context;

    public LagerbewegungController(LagerverwaltungContext context)
    {
        _context = context;
    }

    // GET: Lagerbewegung
    public async Task<IActionResult> Index()    
    {
        return View(await _context.Lagerbewegung.ToListAsync());
    }

    // GET: Lagerbewegung/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var lagerbewegung = await _context.Lagerbewegung
            .FirstOrDefaultAsync(m => m.ID == id);
        if (lagerbewegung == null)
        {
            return NotFound();
        }

        return View(lagerbewegung);
    }

    // GET: Lagerbewegung/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: Lagerbewegung/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("ID,Menge,ErstelltAm,BestaetigtAm,BewegungsartID,VonRaumID,NachRaumID,GegenstandID,PersonID")] Lagerbewegung lagerbewegung)
    {
        // Navigationseigenschaften werden nicht gebunden, sonst schlägt die Validierung fehl
        ModelState.Remove(nameof(Lagerbewegung.Bewegungsart));
        ModelState.Remove(nameof(Lagerbewegung.Gegenstand));
        ModelState.Remove(nameof(Lagerbewegung.NachRaum));
        ModelState.Remove(nameof(Lagerbewegung.Person));
        ModelState.Remove(nameof(Lagerbewegung.VonRaum));

        if (ModelState.IsValid)
        {
            _context.Add(lagerbewegung);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(lagerbewegung);
    }

    // GET: Lagerbewegung/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var lagerbewegung = await _context.Lagerbewegung.FindAsync(id);
        if (lagerbewegung == null)
        {
            return NotFound();
        }
        return View(lagerbewegung);
    }

    // POST: Lagerbewegung/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? id, [Bind("ID,Menge,ErstelltAm,BestaetigtAm,BewegungsartID,VonRaumID,NachRaumID,GegenstandID,PersonID")] Lagerbewegung lagerbewegung)
    {
        // Navigationseigenschaften werden nicht gebunden, sonst schlägt die Validierung fehl
        ModelState.Remove(nameof(Lagerbewegung.Bewegungsart));
        ModelState.Remove(nameof(Lagerbewegung.Gegenstand));
        ModelState.Remove(nameof(Lagerbewegung.NachRaum));
        ModelState.Remove(nameof(Lagerbewegung.Person));
        ModelState.Remove(nameof(Lagerbewegung.VonRaum));

        if (id != lagerbewegung.ID)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(lagerbewegung);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!LagerbewegungExists(lagerbewegung.ID))
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
        return View(lagerbewegung);
    }

    // GET: Lagerbewegung/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var lagerbewegung = await _context.Lagerbewegung
            .FirstOrDefaultAsync(m => m.ID == id);
        if (lagerbewegung == null)
        {
            return NotFound();
        }

        return View(lagerbewegung);
    }

    // POST: Lagerbewegung/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var lagerbewegung = await _context.Lagerbewegung.FindAsync(id);
        if (lagerbewegung != null)
        {
            _context.Lagerbewegung.Remove(lagerbewegung);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool LagerbewegungExists(int? id)
    {
        return _context.Lagerbewegung.Any(e => e.ID == id);
    }
}
