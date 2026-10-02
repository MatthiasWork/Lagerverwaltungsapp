using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Lagerverwaltungsapp_MatthiasUtrata.Models;

namespace Lagerverwaltungsapp_MatthiasUtrata.Controllers;

// Lagerbewegungen ansehen darf nur ein Admin. Anlegen, Bearbeiten und Löschen gibt es nicht:
// Lagerbewegungen entstehen nur über Buchungen im LagerService und werden nie geändert oder gelöscht, sonst passen Raumbestand und Historie nicht mehr zusammen
[Authorize(Roles = "Admin")]
public class LagerbewegungController : Controller
{
    private readonly LagerverwaltungContext _context;

    public LagerbewegungController(LagerverwaltungContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Methode, die die Historie anzeigt: alle Lagerbewegungen, die neueste zuerst, seitenweise.
    /// Die Liste kann nach Suchbegriff, Raum, Bewegungsart, Status und Zeitraum gefiltert werden.
    /// </summary>
    /// <param name="suche">Suchbegriff für Gegenstand, Seriennummer oder die Person, die gebucht hat</param>
    /// <param name="raumID">Die ID des Raums, aus dem oder in den gebucht wurde</param>
    /// <param name="bewegungsartID">Die ID der Bewegungsart, nach der gefiltert werden soll</param>
    /// <param name="status">Der Status, nach dem gefiltert werden soll (siehe HistorieViewModel)</param>
    /// <param name="von">Der erste Tag, an dem gebucht wurde</param>
    /// <param name="bis">Der letzte Tag, an dem gebucht wurde (einschließlich)</param>
    /// <param name="seite">Die Seite, die angezeigt werden soll (ab 1)</param>
    /// <returns>Gibt eine Task zurück</returns>
    // GET: Lagerbewegung
    public async Task<IActionResult> Index(string? suche, string? raumID, int? bewegungsartID, string? status, DateTime? von, DateTime? bis, int seite = 1)
    {
        suche = suche?.Trim();

        var abfrage = _context.Lagerbewegung.AsQueryable();

        if (!string.IsNullOrEmpty(suche))
        {
            abfrage = abfrage.Where(l => l.Gegenstand.Name.Contains(suche)
                || (l.Gegenstand.Seriennummer != null && l.Gegenstand.Seriennummer.Contains(suche))
                || (l.Person.Vorname + " " + l.Person.Nachname).Contains(suche)
                || l.Person.Username.Contains(suche));
        }

        if (!string.IsNullOrEmpty(raumID))
        {
            abfrage = abfrage.Where(l => l.VonRaumID == raumID || l.NachRaumID == raumID);
        }

        if (bewegungsartID != null)
        {
            abfrage = abfrage.Where(l => l.BewegungsartID == bewegungsartID);
        }

        if (status == HistorieViewModel.FreigabeOffen)
        {
            abfrage = abfrage.Where(Lagerbewegung.IstOffen);
        }
        else if (status == HistorieViewModel.Storniert)
        {
            abfrage = abfrage.Where(l => l.BestaetigtAm != null && l.Bewegungsart.Name == Bewegungsart.Storniert);
        }
        else if (status == HistorieViewModel.Bestaetigt)
        {
            abfrage = abfrage.Where(l => l.BestaetigtAm != null && l.Bewegungsart.Name != Bewegungsart.Storniert);
        }

        if (von != null)
        {
            var anfang = von.Value.Date;
            abfrage = abfrage.Where(l => l.ErstelltAm >= anfang);
        }

        if (bis != null)
        {
            // Bis einschließlich: alles vor dem Beginn des nächsten Tages
            var ende = bis.Value.Date.AddDays(1);
            abfrage = abfrage.Where(l => l.ErstelltAm < ende);
        }

        // Eine Seite zu weit (z. B. nach einem neuen Filter in einem alten Link) zeigt die letzte Seite
        var anzahlGefiltert = await abfrage.CountAsync();
        var anzahlSeiten = Math.Max(1, (int)Math.Ceiling(anzahlGefiltert / (double)HistorieViewModel.EintraegeJeSeite));
        seite = Math.Clamp(seite, 1, anzahlSeiten);

        var historie = new HistorieViewModel
        {
            Lagerbewegungen = await abfrage
                .Include(l => l.Gegenstand)
                .Include(l => l.Bewegungsart)
                .Include(l => l.Person)
                .Include(l => l.VonRaum).ThenInclude(r => r.Person)
                .Include(l => l.NachRaum).ThenInclude(r => r.Person)
                .OrderByDescending(l => l.ErstelltAm)
                .ThenByDescending(l => l.ID)
                .Skip((seite - 1) * HistorieViewModel.EintraegeJeSeite)
                .Take(HistorieViewModel.EintraegeJeSeite)
                .ToListAsync(),
            AnzahlGesamt = await _context.Lagerbewegung.CountAsync(),
            AnzahlGefiltert = anzahlGefiltert,
            Seite = seite,
            AnzahlSeiten = anzahlSeiten,
            Suche = suche,
            RaumID = raumID,
            BewegungsartID = bewegungsartID,
            Status = status,
            Von = von,
            Bis = bis,
            Raeume = await _context.Raum.OrderBy(r => r.ID).Select(r => r.ID).ToListAsync(),
            Bewegungsarten = await _context.Bewegungsart.OrderBy(b => b.Name).ToListAsync()
        };

        return View(historie);
    }

    /// <summary>
    /// Methode, die die Details einer Lagerbewegung anzeigt. Sie ist nur für Admins sichtbar, da sie sensible Daten enthält.
    /// </summary>
    /// <param name="id">Die ID der Lagerbewegung, deren Details angezeigt werden sollen</param>
    /// <returns>Gibt eine Task zurück</returns>
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
    /// Methode, die alle Lagerbewegungen als CSV-Datei zum Herunterladen liefert.
    /// Es wird chronologisch nach ErstelltAm sortiert
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
