namespace Lagerverwaltungsapp_MatthiasUtrata.Models;

// Ergänzung zur generierten Klasse Raumart, damit sie beim erneuten Generieren nicht überschrieben wird
public partial class Raumart
{
    /// <summary>
    /// Name der Raumart des Hauptlagers. Davon gibt es genau einen Raum; dort kommt die Ware an und wird ausgegeben.
    /// </summary>
    public const string Hauptlager = "Hauptlager";

    /// <summary>
    /// Name der Raumart des Umbuchungslagers. Davon gibt es genau einen Raum; dorthin gehen alle Rückgaben aus den Laboren.
    /// </summary>
    public const string Umbuchungslager = "Umbuchungslager";

    /// <summary>
    /// Name der Raumart für Labore. Davon kann es beliebig viele Räume geben.
    /// </summary>
    public const string Labor = "Labor";

    /// <summary>
    /// Alle Raumarten, die beim Start angelegt werden und von denen die Anwendung abhängt.
    /// </summary>
    public static readonly IReadOnlyList<string> Systemeintraege = new[] { Hauptlager, Umbuchungslager, Labor };
}
