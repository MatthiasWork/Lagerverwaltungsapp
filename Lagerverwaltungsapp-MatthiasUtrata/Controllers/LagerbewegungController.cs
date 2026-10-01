using System.Text;
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

    /// <summary>
    /// Methode, die alle Lagerbewegungen als CSV-Datei zum Herunterladen liefert, chronologisch nach dem Zeitpunkt, an dem sie
    /// gebucht (angelegt) wurden, die älteste zuerst. Getrennt wird mit Strichpunkt, damit Excel mit deutschen Einstellungen
    /// die Spalten erkennt.
    /// </summary>
    /// <returns>Gibt eine Task zurück</returns>
    // GET: Lagerbewegung/Export
    public async Task<IActionResult> Export()
    {
        var lagerbewegungen = await _context.Lagerbewegung
            .Include(l => l.Bewegungsart)
            .Include(l => l.Gegenstand)
            .Include(l => l.Person)
            .OrderBy(l => l.ErstelltAm)
            .ThenBy(l => l.ID)
            .ToListAsync();

        var csv = new StringBuilder();
        csv.AppendLine("Gebucht am;Abgeschlossen am;Status;Bewegungsart;Gegenstand;Seriennummer;Menge;Von Raum;Nach Raum;Gebucht von");

        foreach (var l in lagerbewegungen)
        {
            // Zuerst prüfen: Eine stornierte Lagerbewegung hat in BestaetigtAm den Zeitpunkt der Stornierung
            string status;
            if (l.Offen)
            {
                status = "Offen";
            }
            else if (l.Storniert)
            {
                status = "Storniert";
            }
            else
            {
                status = "Bestätigt";
            }

            var abgeschlossen = "";
            if (l.BestaetigtAm != null)
            {
                abgeschlossen = l.BestaetigtAm.Value.ToString("dd.MM.yyyy HH:mm");
            }

            // Bei einer Korrektur ist die Menge die Änderung des Bestands und kann negativ sein
            csv.AppendLine(string.Join(";",
                l.ErstelltAm.ToString("dd.MM.yyyy HH:mm"),
                abgeschlossen,
                status,
                CsvWert(l.Bewegungsart.Name),
                CsvWert(l.Gegenstand.Name),
                CsvWert(l.Gegenstand.Seriennummer),
                l.Menge,
                CsvWert(l.VonRaumID),
                CsvWert(l.NachRaumID),
                CsvWert(l.Person.VollerName)));
        }

        // Mit BOM (Byte Order Mark) am Anfang, sonst erkennt Excel die Datei nicht als UTF-8 und zeigt Umlaute falsch an
        var inhalt = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
        return File(inhalt, "text/csv; charset=utf-8", $"Lagerbewegungen_{DateTime.Now:yyyy-MM-dd}.csv");
    }

    /// <summary>
    /// Methode, die einen Text für eine Zelle der CSV-Datei aufbereitet. Enthält er einen Strichpunkt, ein Anführungszeichen
    /// oder einen Zeilenumbruch, wird er in Anführungszeichen gesetzt, sonst würde er die Spalten verschieben.
    /// Beginnt er mit =, +, - oder @, bekommt er ein ' davor, damit Excel ihn nicht als Formel ausführt.
    /// </summary>
    /// <param name="wert">Der Text, darf leer sein</param>
    /// <returns>Der Text für die CSV-Datei, leer, wenn der Text leer ist</returns>
    private static string CsvWert(string? wert)
    {
        if (string.IsNullOrEmpty(wert))
        {
            return "";
        }

        if ("=+-@".Contains(wert[0]))
        {
            wert = "'" + wert;
        }

        if (wert.IndexOfAny([';', '"', '\r', '\n']) >= 0)
        {
            wert = "\"" + wert.Replace("\"", "\"\"") + "\"";
        }
        return wert;
    }
}
