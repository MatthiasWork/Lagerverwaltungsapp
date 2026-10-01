namespace Lagerverwaltungsapp_MatthiasUtrata.Models;

// Ergänzung zur generierten Klasse Raumart, damit sie beim erneuten Generieren nicht überschrieben wird
public partial class Raumart
{
    /// <summary>
    /// Wortteil, an dem ein Lager erkannt wird: Ein Raum ist ein Lager, wenn der Name seiner Raumart ihn enthält, egal ob groß
    /// oder klein geschrieben (z. B. "Hauptlager", "Umbuchungslager"). Aus einem Lager kann die Person eines anderen Raums
    /// Geräte in ihren Raum holen. In Abfragen mit Name.ToLower().Contains(Lager), damit es nicht von der Sortierung der Datenbank abhängt.
    /// </summary>
    public const string Lager = "lager";

    /// <summary>
    /// Gibt an, ob Räume mit dieser Raumart Lager sind (siehe Lager).
    /// </summary>
    public bool IstLager => Name.Contains(Lager, StringComparison.OrdinalIgnoreCase);
}
