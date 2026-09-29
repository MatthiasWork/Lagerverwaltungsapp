
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Lagerverwaltungsapp_MatthiasUtrata.Models;
using Lagerverwaltungsapp_MatthiasUtrata.Services;

// Benutzerverwaltung darf nur ein Admin, sonst könnte sich jeder selbst zum Admin machen
[Authorize(Roles = "Admin")]
public class PersonController : Controller
{
    private readonly LagerverwaltungContext _context;
    private readonly PasswordService _passwordService;

    /// <summary>
    /// Konstruktor der PersonController-Klasse.
    /// </summary>
    /// <param name="context">Der Datenbankkontext der Lagerverwaltung</param>
    /// <param name="passwordService">Der Service zum Hashen und Prüfen von Passwörtern</param>
    public PersonController(LagerverwaltungContext context, PasswordService passwordService)
    {
        _context = context;
        _passwordService = passwordService;
    }

    /// <summary>
    /// Methode, die alle Personen mit ihrer Rolle auflistet.
    /// </summary>
    /// <returns>Gibt eine Task zurück</returns>
    // GET: Person
    public async Task<IActionResult> Index()
    {
        return View(await _context.Person.Include(p => p.Rolle).ToListAsync());
    }

    /// <summary>
    /// Methode, die die Details einer Person mit ihrer Rolle anzeigt.
    /// </summary>
    /// <param name="id">Die ID der Person, die angezeigt werden soll</param>
    /// <returns>Gibt eine Task zurück</returns>
    // GET: Person/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var person = await _context.Person
            .Include(p => p.Rolle)
            .FirstOrDefaultAsync(m => m.ID == id);
        if (person == null)
        {
            return NotFound();
        }

        return View(person);
    }

    /// <summary>
    /// Methode, die das Formular zum Anlegen einer neuen Person anzeigt.
    /// </summary>
    /// <returns>Gibt ein IActionResult zurück</returns>
    // GET: Person/Create
    public IActionResult Create()
    {
        ViewData["RolleID"] = new SelectList(_context.Rolle, "ID", "Name");
        return View();
    }

    /// <summary>
    /// Methode, die eine neue Person anlegt, sofern der Benutzername noch nicht vergeben ist.
    /// Das Passwort wird dabei gehasht gespeichert.
    /// </summary>
    /// <param name="person">Die Person mit den Daten aus dem Formular</param>
    /// <returns>Gibt eine Task zurück</returns>
    // POST: Person/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("ID,Vorname,Nachname,Username,Password,Email,RolleID")] Person person)
    {
        // Navigationseigenschaften werden nicht gebunden, sonst schlägt die Validierung fehl
        ModelState.Remove(nameof(Person.Rolle));

        // Der Login sucht über den Benutzernamen, daher muss er eindeutig sein
        if (await _context.Person.AnyAsync(p => p.Username == person.Username))
        {
            ModelState.AddModelError(nameof(Person.Username), "Dieser Benutzername ist bereits vergeben.");
        }

        if (ModelState.IsValid)
        {
            // Passwort hashen
            person.Password = _passwordService.HashPassword(person, person.Password);

            _context.Add(person);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        ViewData["RolleID"] = new SelectList(_context.Rolle, "ID", "Name", person.RolleID);
        return View(person);
    }

    /// <summary>
    /// Methode, die das Formular zum Bearbeiten einer Person anzeigt.
    /// </summary>
    /// <param name="id">Die ID der Person, die bearbeitet werden soll</param>
    /// <returns>Gibt eine Task zurück</returns>
    // GET: Person/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var person = await _context.Person.FindAsync(id);
        if (person == null)
        {
            return NotFound();
        }
        ViewData["RolleID"] = new SelectList(_context.Rolle, "ID", "Name", person.RolleID);
        return View(person);
    }

    /// <summary>
    /// Methode, die die Änderungen an einer Person speichert. Bleibt das Passwort leer,
    /// wird das bisherige Passwort beibehalten, ansonsten wird das neue Passwort gehasht gespeichert.
    /// </summary>
    /// <param name="id">Die ID der Person, die bearbeitet werden soll</param>
    /// <param name="person">Die Person mit den geänderten Daten aus dem Formular</param>
    /// <returns>Gibt eine Task zurück</returns>
    /// <exception cref="DbUpdateConcurrencyException">Exception, falls die Person gleichzeitig von jemand anderem geändert wurde</exception>
    // POST: Person/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? id, [Bind("ID,Vorname,Nachname,Username,Password,Email,RolleID")] Person person)
    {
        // Navigationseigenschaften werden nicht gebunden, sonst schlägt die Validierung fehl
        ModelState.Remove(nameof(Person.Rolle));
        // Beim Bearbeiten ist das Passwort optional (leer = bisheriges Passwort behalten)
        ModelState.Remove(nameof(Person.Password));

        if (id != person.ID)
        {
            return NotFound();
        }

        if (await _context.Person.AnyAsync(p => p.Username == person.Username && p.ID != person.ID))
        {
            ModelState.AddModelError(nameof(Person.Username), "Dieser Benutzername ist bereits vergeben.");
        }

        if (ModelState.IsValid)
        {
            if (string.IsNullOrEmpty(person.Password))
            {
                // Bisherigen Hash aus der Datenbank übernehmen
                var bisherigerHash = await _context.Person
                    .Where(p => p.ID == person.ID)
                    .Select(p => p.Password)
                    .FirstOrDefaultAsync();
                if (bisherigerHash == null)
                {
                    return NotFound();
                }
                person.Password = bisherigerHash;
            }
            else
            {
                person.Password = _passwordService.HashPassword(person, person.Password);
            }

            try
            {
                _context.Update(person);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PersonExists(person.ID))
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
        ViewData["RolleID"] = new SelectList(_context.Rolle, "ID", "Name", person.RolleID);
        return View(person);
    }

    /// <summary>
    /// Methode, die die Bestätigungsseite zum Löschen einer Person anzeigt.
    /// </summary>
    /// <param name="id">Die ID der Person, die gelöscht werden soll</param>
    /// <returns>Gibt eine Task zurück</returns>
    // GET: Person/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var person = await _context.Person
            .Include(p => p.Rolle)
            .FirstOrDefaultAsync(m => m.ID == id);
        if (person == null)
        {
            return NotFound();
        }

        return View(person);
    }

    /// <summary>
    /// Methode, die eine Person nach der Bestätigung löscht.
    /// </summary>
    /// <param name="id">Die ID der Person, die gelöscht werden soll</param>
    /// <returns>Gibt eine Task zurück</returns>
    // POST: Person/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var person = await _context.Person.FindAsync(id);
        if (person != null)
        {
            _context.Person.Remove(person);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Methode, die überprüft, ob eine Person mit der angegebenen ID existiert.
    /// </summary>
    /// <param name="id">Die ID der Person, die überprüft werden soll</param>
    /// <returns>Gibt True oder False zurück</returns>
    private bool PersonExists(int? id)
    {
        return _context.Person.Any(e => e.ID == id);
    }
}
