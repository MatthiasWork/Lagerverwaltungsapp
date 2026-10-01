using System.Linq.Expressions;

namespace Lagerverwaltungsapp_MatthiasUtrata.Models;

// Ergänzung zur generierten Klasse Lagerbewegung, damit sie beim erneuten Generieren nicht überschrieben wird.
// Eine Lagerbewegung ist offen (BestaetigtAm leer), bestätigt oder storniert. Storniert heißt: abgelehnt oder zurückgezogen,
// die Bewegungsart ist dann "Storniert" und BestaetigtAm enthält den Zeitpunkt der Stornierung (also "abgeschlossen am").
//
// Freigeben (bestätigen oder ablehnen) muss immer die Person des anderen Raums als die, die angefragt hat; zurückziehen darf,
// wer angefragt hat. Meist fragt die Person des Von-Raums an (Transfer aus dem eigenen Raum), dann gibt der Nach-Raum frei.
// Holt die Person des Nach-Raums Geräte aus einem Lager in ihren Raum, gibt der Von-Raum (das Lager) frei.
// Welche Seite angefragt hat, steht nicht in der Tabelle: Erkannt wird es an der Person, die die Lagerbewegung angelegt hat
// (PersonID), verglichen mit der aktuell für den Nach-Raum verantwortlichen Person
public partial class Lagerbewegung
{
    /// <summary>
    /// Bedingung für eine offene Lagerbewegung: schon aus dem Von-Raum abgebucht, aber weder bestätigt noch storniert.
    /// Als Ausdruck, damit EF Core sie in SQL übersetzen kann, z. B. _context.Lagerbewegung.Where(Lagerbewegung.IstOffen).
    /// </summary>
    public static readonly Expression<Func<Lagerbewegung, bool>> IstOffen = l => l.BestaetigtAm == null;

    /// <summary>
    /// Methode, die die Bedingung für die Lagerbewegungen liefert, die die Person eines Raums freigeben muss: angefragt von der
    /// Person des Nach-Raums und aus dem Raum, oder angefragt von jemand anderem und in den Raum. Als Ausdruck für Abfragen,
    /// für offene Lagerbewegungen zusammen mit IstOffen.
    /// </summary>
    /// <param name="raumID">Die ID des Raums, dessen verantwortliche Person freigibt</param>
    /// <returns>Die Bedingung für Where</returns>
    public static Expression<Func<Lagerbewegung, bool>> FreigabeFuer(string raumID)
    {
        return l => (l.NachRaum.PersonID == l.PersonID && l.VonRaumID == raumID)
            || (l.NachRaum.PersonID != l.PersonID && l.NachRaumID == raumID);
    }

    /// <summary>
    /// Methode, die die Bedingung für die Lagerbewegungen liefert, die für einen Raum angefragt wurden und daher von dessen
    /// Person zurückgezogen werden können: aus dem Raum (Transfer) oder in den Raum (aus einem Lager geholt).
    /// Als Ausdruck für Abfragen, für offene Lagerbewegungen zusammen mit IstOffen.
    /// </summary>
    /// <param name="raumID">Die ID des Raums, für den angefragt wurde</param>
    /// <returns>Die Bedingung für Where</returns>
    public static Expression<Func<Lagerbewegung, bool>> AngefragtFuer(string raumID)
    {
        return l => (l.NachRaum.PersonID == l.PersonID && l.NachRaumID == raumID)
            || (l.NachRaum.PersonID != l.PersonID && l.VonRaumID == raumID);
    }

    /// <summary>
    /// Gibt an, ob die Lagerbewegung noch offen ist (für bereits geladene Lagerbewegungen, in Abfragen IstOffen verwenden).
    /// </summary>
    public bool Offen => BestaetigtAm == null;

    /// <summary>
    /// Gibt an, ob die Lagerbewegung abgelehnt oder zurückgezogen wurde. Dafür muss die Bewegungsart geladen sein.
    /// </summary>
    public bool Storniert => BestaetigtAm != null && Bewegungsart?.Name == Models.Bewegungsart.Storniert;

    /// <summary>
    /// Gibt an, ob die Person des Nach-Raums die Lagerbewegung angefragt hat (aus einem Lager in ihren Raum geholt).
    /// Dafür muss der Nach-Raum geladen sein.
    /// </summary>
    public bool VomNachRaumAngefragt => NachRaum.PersonID == PersonID;

    /// <summary>
    /// Der Raum, dessen verantwortliche Person freigibt: der Von-Raum, wenn der Nach-Raum angefragt hat, sonst der Nach-Raum.
    /// Dafür müssen Von-Raum und Nach-Raum geladen sein.
    /// </summary>
    public Raum FreigabeRaum => VomNachRaumAngefragt ? VonRaum : NachRaum;

    /// <summary>
    /// Der Raum, für den angefragt wurde und dessen verantwortliche Person zurückziehen darf (das Gegenstück zu FreigabeRaum).
    /// Dafür müssen Von-Raum und Nach-Raum geladen sein.
    /// </summary>
    public Raum AnfrageRaum => VomNachRaumAngefragt ? NachRaum : VonRaum;
}
