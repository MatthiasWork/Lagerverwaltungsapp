namespace Lagerverwaltungsapp_MatthiasUtrata.Models;

// Ergänzung zur generierten Klasse Bewegungsart, damit sie beim erneuten Generieren nicht überschrieben wird
public partial class Bewegungsart
{
    /// <summary>
    /// Name der Bewegungsart, die beim Ausborgen vorausgewählt wird. Sie ist nur beschreibend und kann im Formular geändert werden.
    /// </summary>
    public const string Ausgabe = "Ausgabe";

    /// <summary>
    /// Name der Bewegungsart, die beim Wareneingang vorausgewählt wird. Auch sie kann im Formular geändert werden.
    /// </summary>
    public const string Wareneingang = "Wareneingang";
}
