using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Lagerverwaltungsapp_MatthiasUtrata.Extensions;
using Lagerverwaltungsapp_MatthiasUtrata.Models;
using Lagerverwaltungsapp_MatthiasUtrata.Services;

namespace Lagerverwaltungsapp_MatthiasUtrata.Controllers;

// Benutzerverwaltung darf nur ein Admin, sonst könnte sich jeder selbst zum Admin machen
[Authorize(Roles = "Admin")]
public class PersonController : Controller
{
    private readonly LagerverwaltungContext _context;
    private readonly PasswordService _passwordService;
    private readonly LagerService _lagerService;

    /// <summary>
    /// Konstruktor der PersonController-Klasse.
    /// </summary>
    /// <param name="context">Der Datenbankkontext der Lagerverwaltung</param>
    /// <param name="passwordService">Der Service zum Hashen und Prüfen von Passwörtern</param>
    /// <param name="lagerService">Der Service, der weiß, für welchen Raum eine Person verantwortlich ist</param>
    public PersonController(LagerverwaltungContext context, PasswordService passwordService, LagerService lagerService)
    {
        _context = context;
        _passwordService = passwordService;
        _lagerService = lagerService;
    }

    /// <summary>
    /// Methode, die zur Adminübersicht weiterleitet, da die Benutzerliste dort angezeigt wird.
    /// </summary>
    /// <returns>Gibt ein IActionResult zurück</returns>
    // GET: Person
    public IActionResult Index()
    {
        return RedirectToAction("Index", "Admin");
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
    /// <returns>Gibt eine Task zurück</returns>
    // GET: Person/Create
    public async Task<IActionResult> Create()
    {
        await RollenSetzenAsync(null);
        return View();
    }

    /// <summary>
    /// Methode, die eine neue Person anlegt, sofern der Benutzername noch nicht vergeben ist.
    /// Das Passwort wird dabei gehasht und danach gespeichert.
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
            TempData["Meldung"] = $"Benutzer \"{person.Username}\" wurde angelegt.";
            return RedirectToAction("Index", "Admin");
        }
        await RollenSetzenAsync(person.RolleID);
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
        await RollenSetzenAsync(person.RolleID);
        return View(person);
    }

    /// <summary>
    /// Methode, die die Änderungen an einer Person speichert. 
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

        // Der letzte Admin darf seine Admin-Rolle nicht verlieren, sonst kann niemand mehr Benutzer verwalten
        var warAdmin = await _context.Person.Where(p => p.ID == person.ID).Select(p => p.Rolle.Admin).FirstOrDefaultAsync();
        var wirdAdmin = await _context.Rolle.Where(r => r.ID == person.RolleID).Select(r => r.Admin).FirstOrDefaultAsync();
        if (warAdmin && !wirdAdmin && !await GibtEsAndereAdminsAsync(person.ID))
        {
            ModelState.AddModelError(nameof(Person.RolleID), "Der letzte Administrator muss eine Admin-Rolle behalten.");
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
            TempData["Meldung"] = $"Benutzer \"{person.Username}\" wurde gespeichert.";
            return RedirectToAction("Index", "Admin");
        }
        await RollenSetzenAsync(person.RolleID);
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

        ViewData["LoeschHindernis"] = await LoeschHindernisAsync(person);
        return View(person);
    }

    /// <summary>
    /// Methode, die eine Person nach der Bestätigung löscht, sofern nichts dagegen spricht.
    /// </summary>
    /// <param name="id">Die ID der Person, die gelöscht werden soll</param>
    /// <returns>Gibt eine Task zurück</returns>
    // POST: Person/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var person = await _context.Person
            .Include(p => p.Rolle)
            .FirstOrDefaultAsync(p => p.ID == id);
        if (person == null)
        {
            return RedirectToAction("Index", "Admin");
        }

        var hindernis = await LoeschHindernisAsync(person);
        if (hindernis != null)
        {
            TempData["Fehler"] = hindernis;
            return RedirectToAction("Index", "Admin");
        }

        _context.Person.Remove(person);
        await _context.SaveChangesAsync();

        TempData["Meldung"] = $"Benutzer \"{person.Username}\" wurde gelöscht.";
        return RedirectToAction("Index", "Admin");
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

    /// <summary>
    /// Methode, die die Auswahlliste der Rollen für das Formular setzt.
    /// </summary>
    /// <param name="rolleID">Die ID der Rolle, die vorausgewählt werden soll, oder null bei einer neuen Person</param>
    /// <returns>Gibt eine Task zurück</returns>
    private async Task RollenSetzenAsync(int? rolleID)
    {
        ViewData["RolleID"] = new SelectList(await _context.Rolle.OrderBy(r => r.Name).ToListAsync(), "ID", "Name", rolleID);
    }

    /// <summary>
    /// Methode, die überprüft, ob es außer der angegebenen Person noch weitere Admins gibt.
    /// </summary>
    /// <param name="personID">Die ID der Person, die nicht mitgezählt werden soll</param>
    /// <returns>Gibt True oder False zurück</returns>
    private async Task<bool> GibtEsAndereAdminsAsync(int personID)
    {
        return await _context.Person.AnyAsync(p => p.ID != personID && p.Rolle.Admin);
    }

    /// <summary>
    /// Methode, die überprüft, ob eine Person gelöscht werden darf.
    /// Die Rolle der Person muss dafür geladen sein.
    /// </summary>
    /// <param name="person">Die Person, die gelöscht werden soll</param>
    /// <returns>Der Grund, warum die Person nicht gelöscht werden darf, oder null, wenn das Löschen erlaubt ist</returns>
    private async Task<string?> LoeschHindernisAsync(Person person)
    {
        if (person.ID == User.GetPersonID())
        {
            return "Du kannst dein eigenes Benutzerkonto nicht löschen.";
        }

        if (person.Rolle.Admin && !await GibtEsAndereAdminsAsync(person.ID))
        {
            return "Der letzte Administrator kann nicht gelöscht werden.";
        }

        // Jeder Raum braucht eine verantwortliche Person, sonst könnte niemand mehr für ihn buchen oder bestätigen
        var raumID = await _lagerService.RaumDerPersonAsync(person.ID);
        if (raumID != null)
        {
            return $"\"{person.Username}\" ist für den Raum {raumID} verantwortlich und kann daher nicht gelöscht werden. Weise den Raum zuerst einer anderen Person zu.";
        }

        // Lagerbewegungen brauchen zwingend eine Person, sonst wäre nicht mehr nachvollziehbar, wer sie erfasst hat
        var anzahlLagerbewegungen = await _context.Lagerbewegung.CountAsync(l => l.PersonID == person.ID);
        if (anzahlLagerbewegungen > 0)
        {
            return $"\"{person.Username}\" hat {anzahlLagerbewegungen} Lagerbewegung(en) erfasst und kann daher nicht gelöscht werden.";
        }

        return null;
    }
}
