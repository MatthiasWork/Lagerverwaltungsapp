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

    /// <summary>
    /// Name der Bewegungsart, die eine Lagerbewegung bekommt, wenn der Zielraum sie ablehnt oder der Von-Raum sie zurückzieht.
    /// Anders als die übrigen Bewegungsarten hat sie eine Bedeutung: Sie wird nur vom LagerService vergeben, kann im Formular
    /// nicht gewählt werden und kann weder umbenannt noch gelöscht werden. Der Start legt sie an, falls sie fehlt.
    /// </summary>
    public const string Storniert = "Storniert";
}
