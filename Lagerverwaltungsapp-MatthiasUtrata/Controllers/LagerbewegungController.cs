using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Lagerverwaltungsapp_MatthiasUtrata.Models;

namespace Lagerverwaltungsapp_MatthiasUtrata.Controllers;

// Lagerbewegungen ansehen darf nur ein Admin. Anlegen, Bearbeiten und Löschen gibt es nicht: Lagerbewegungen entstehen nur
// über Buchungen im LagerService und werden nie geändert oder gelöscht, sonst passen Raumbestand und Historie nicht mehr zusammen
// (z. B. wäre ein Gerät mit Seriennummer dann gleichzeitig in einem Raum und unterwegs)
[Authorize(Roles = "Admin")]
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
}
