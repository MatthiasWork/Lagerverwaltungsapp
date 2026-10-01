using System.Linq.Expressions;

namespace Lagerverwaltungsapp_MatthiasUtrata.Models;

// Ergänzung zur generierten Klasse Lagerbewegung, damit sie beim erneuten Generieren nicht überschrieben wird.
// Eine Lagerbewegung ist offen (BestaetigtAm leer), bestätigt oder storniert. Storniert heißt: abgelehnt oder zurückgezogen,
// die Bewegungsart ist dann "Storniert" und BestaetigtAm enthält den Zeitpunkt der Stornierung (also "abgeschlossen am")
public partial class Lagerbewegung
{
    /// <summary>
    /// Bedingung für eine offene Lagerbewegung: schon aus dem Von-Raum abgebucht, aber weder bestätigt noch storniert.
    /// Als Ausdruck, damit EF Core sie in SQL übersetzen kann, z. B. _context.Lagerbewegung.Where(Lagerbewegung.IstOffen).
    /// </summary>
    public static readonly Expression<Func<Lagerbewegung, bool>> IstOffen = l => l.BestaetigtAm == null;

    /// <summary>
    /// Gibt an, ob die Lagerbewegung noch offen ist (für bereits geladene Lagerbewegungen, in Abfragen IstOffen verwenden).
    /// </summary>
    public bool Offen => BestaetigtAm == null;

    /// <summary>
    /// Gibt an, ob die Lagerbewegung abgelehnt oder zurückgezogen wurde. Dafür muss die Bewegungsart geladen sein.
    /// </summary>
    public bool Storniert => BestaetigtAm != null && Bewegungsart?.Name == Models.Bewegungsart.Storniert;
}
