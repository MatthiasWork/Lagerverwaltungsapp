namespace Lagerverwaltungsapp_MatthiasUtrata.Models;

// Ergänzung zur generierten Klasse Bewegungsart, damit sie beim erneuten Generieren nicht überschrieben wird
// (Namen höchstens 20 Zeichen wegen Bewegungsart.Name nvarchar(20))
public partial class Bewegungsart
{
    /// <summary>
    /// Neue Ware kommt ins Hauptlager (Hauptlager → Hauptlager, es wird nur zugebucht).
    /// </summary>
    public const string Wareneingang = "Wareneingang";

    /// <summary>
    /// Ausgabe aus dem Hauptlager an ein Labor (Hauptlager → Labor).
    /// </summary>
    public const string Ausgabe = "Ausgabe";

    /// <summary>
    /// Rückgabe aus einem Labor ins Umbuchungslager (Labor → Umbuchungslager).
    /// </summary>
    public const string Rueckgabe = "Rueckgabe";

    /// <summary>
    /// Einlagerung aus dem Umbuchungslager zurück ins Hauptlager (Umbuchungslager → Hauptlager).
    /// </summary>
    public const string Einlagerung = "Einlagerung";

    /// <summary>
    /// Alle Bewegungsarten, die beim Start angelegt werden und von denen die Anwendung abhängt.
    /// </summary>
    public static readonly IReadOnlyList<string> Systemeintraege = new[] { Wareneingang, Ausgabe, Rueckgabe, Einlagerung };
}
