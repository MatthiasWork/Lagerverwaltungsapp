using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Lagerverwaltungsapp_MatthiasUtrata.Models;

namespace Lagerverwaltungsapp_MatthiasUtrata.Controllers;

// Bewegungsarten verwalten darf nur ein Admin
[Authorize(Roles = "Admin")]
public class BewegungsartController : Controller
{
    private readonly LagerverwaltungContext _context;

    public BewegungsartController(LagerverwaltungContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Methode, die alle Bewegungsarten anzeigt.
    /// </summary>
    /// <returns>Gibt eine Task zurück</returns>
    // GET: Bewegungsart
    public async Task<IActionResult> Index()    
    {
        return View(await _context.Bewegungsart.ToListAsync());
    }

    /// <summary>
    /// Methode, die Details zu einer Bewegungsart anzeigt.
    /// </summary>
    /// <param name="id">Die ID der Bewegungsart</param>
    /// <returns>Gibt eine Task zurück</returns>
    // GET: Bewegungsart/Details/5
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

    /// <summary>
    /// Methode, die das Formular zum Erstellen einer neuen Bewegungsart anzeigt.
    /// </summary>
    /// <returns>Gibt eine Task zurück</returns>
    // GET: Bewegungsart/Create
    public IActionResult Create()
    {
        return View();
    }

    /// <summary>
    /// Methode, die eine neue Bewegungsart erstellt.
    /// </summary>
    /// <param name="bewegungsart">Die zu erstellende Bewegungsart</param>
    /// <returns>Gibt eine Task zurück</returns>
    // POST: Bewegungsart/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("ID,Name")] Bewegungsart bewegungsart)
    {
        FesteNichtNeuVergeben(bewegungsart);

        if (ModelState.IsValid)
        {
            _context.Add(bewegungsart);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(bewegungsart);
    }

    /// <summary>
    /// Methode, die das Formular zum Bearbeiten einer Bewegungsart anzeigt.
    /// </summary>
    /// <param name="id">Die ID der Bewegungsart</param>
    /// <returns>Gibt eine Task zurück</returns>
    // GET: Bewegungsart/Edit/5
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

    /// <summary>
    /// Methode, die eine Bewegungsart bearbeitet.
    /// </summary>
    /// <param name="id">Die ID der Bewegungsart</param>
    /// <param name="bewegungsart">Die zu bearbeitende Bewegungsart</param>
    /// <returns>Gibt eine Task zurück</returns>
    // POST: Bewegungsart/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? id, [Bind("ID,Name")] Bewegungsart bewegungsart)
    {
        if (id != bewegungsart.ID)
        {
            return NotFound();
        }

        var bisherigerName = await _context.Bewegungsart.Where(b => b.ID == bewegungsart.ID).Select(b => b.Name).FirstOrDefaultAsync();
        if (bisherigerName == null)
        {
            return NotFound();
        }

        var nurVergebenFuer = Bewegungsart.NurVergebenFuer(bisherigerName);
        if (nurVergebenFuer != null)
        {
            // Der LagerService sucht sie über den Namen, umbenannt gäbe es sie beim nächsten Start doppelt
            if (bewegungsart.Name != bisherigerName)
            {
                ModelState.AddModelError(nameof(Bewegungsart.Name),
                    $"Die Bewegungsart \"{bisherigerName}\" bekommen {nurVergebenFuer}, daher kann sie nicht umbenannt werden.");
            }
        }
        else
        {
            FesteNichtNeuVergeben(bewegungsart);
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

    /// <summary>
    /// Methode, die das Formular zum Löschen einer Bewegungsart anzeigt.
    /// </summary>
    /// <param name="id">Die ID der Bewegungsart</param>
    /// <returns>Gibt eine Task zurück</returns>
    // GET: Bewegungsart/Delete/5
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

        ViewData["LoeschHindernis"] = await LoeschHindernisAsync(bewegungsart);
        return View(bewegungsart);
    }

    /// <summary>
    /// Methode, die eine Bewegungsart löscht.
    /// </summary>
    /// <param name="id">Die ID der Bewegungsart</param>
    /// <returns>Gibt eine Task zurück</returns>
    // POST: Bewegungsart/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var bewegungsart = await _context.Bewegungsart.FindAsync(id);
        if (bewegungsart != null)
        {
            // Nochmals prüfen, da der POST auch ohne die Bestätigungsseite abgeschickt werden kann
            var hindernis = await LoeschHindernisAsync(bewegungsart);
            if (hindernis != null)
            {
                TempData["Fehler"] = hindernis;
                return RedirectToAction(nameof(Index));
            }

            _context.Bewegungsart.Remove(bewegungsart);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Methode, die prüft, ob eine Bewegungsart mit der angegebenen ID existiert.
    /// </summary>
    /// <param name="id">Die ID der Bewegungsart</param>
    /// <returns>Gibt True oder False zurück</returns>
    private bool BewegungsartExists(int? id)
    {
        return _context.Bewegungsart.Any(e => e.ID == id);
    }

    /// <summary>
    /// Methode, die überprüft, ob eine Bewegungsart gelöscht werden darf.
    /// </summary>
    /// <param name="bewegungsart">Die Bewegungsart, die gelöscht werden soll</param>
    /// <returns>Der Grund, warum die Bewegungsart nicht gelöscht werden darf, oder null, wenn das Löschen erlaubt ist</returns>
    private async Task<string?> LoeschHindernisAsync(Bewegungsart bewegungsart)
    {
        // "Storniert" und "Korrektur" vergibt der LagerService selbst, er sucht sie über den Namen
        var nurVergebenFuer = Bewegungsart.NurVergebenFuer(bewegungsart.Name);
        if (nurVergebenFuer != null)
        {
            return $"Die Bewegungsart \"{bewegungsart.Name}\" bekommen {nurVergebenFuer}, daher kann sie nicht gelöscht werden.";
        }

        // Lagerbewegungen werden nie gelöscht, sonst wäre die Historie nicht mehr vollständig
        var anzahlLagerbewegungen = await _context.Lagerbewegung.CountAsync(l => l.BewegungsartID == bewegungsart.ID);
        if (anzahlLagerbewegungen > 0)
        {
            return $"Die Bewegungsart \"{bewegungsart.Name}\" kommt in {anzahlLagerbewegungen} Lagerbewegung(en) vor und kann daher nicht gelöscht werden, sonst ginge die Historie verloren.";
        }

        return null;
    }

    /// <summary>
    /// Methode, die verhindert, dass eine weitere Bewegungsart "Storniert" oder "Korrektur" heißt. 
    /// Die werden im Testdaten.sql angelegt, und der LagerService sucht sie über den Namen. 
    /// </summary>
    /// <param name="bewegungsart">Die Bewegungsart aus dem Formular</param>
    private void FesteNichtNeuVergeben(Bewegungsart bewegungsart)
    {
        var nurVergebenFuer = Bewegungsart.NurVergebenFuer(bewegungsart.Name);
        if (nurVergebenFuer != null)
        {
            ModelState.AddModelError(nameof(Bewegungsart.Name),
                $"Die Bewegungsart \"{bewegungsart.Name!.Trim()}\" gibt es schon. Sie bekommen nur {nurVergebenFuer}.");
        }
    }
}
