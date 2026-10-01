namespace Lagerverwaltungsapp_MatthiasUtrata.Models;

// Ergänzung zur generierten Klasse Bewegungsart, damit sie beim erneuten Generieren nicht überschrieben wird
public partial class Bewegungsart
{
    /// <summary>
    /// Name der Bewegungsart, die bei einer neuen Ausleihe vorausgewählt wird. Sie ist nur beschreibend und kann im Formular geändert werden.
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

    /// <summary>
    /// Name der Bewegungsart, die eine Korrekturbuchung bekommt, mit der ein Admin den Bestand eines Raums korrigiert (z. B. nach
    /// einer Inventur). Wie "Storniert" wird sie nur vom LagerService vergeben, kann im Formular nicht gewählt werden und kann
    /// weder umbenannt noch gelöscht werden. Der Start legt sie an, falls sie fehlt.
    /// </summary>
    public const string Korrektur = "Korrektur";

    /// <summary>
    /// Methode, die für eine Bewegungsart, die nur der LagerService vergibt ("Storniert" und "Korrektur"), beschreibt, welche
    /// Lagerbewegungen sie bekommen (für Fehlermeldungen). Ohne Beachtung der Groß-/Kleinschreibung, wie SQL Server beim Suchen.
    /// </summary>
    /// <param name="name">Der Name der Bewegungsart</param>
    /// <returns>Die Beschreibung oder null, wenn die Bewegungsart beim Buchen frei gewählt werden kann</returns>
    public static string? NurVergebenFuer(string? name)
    {
        if (string.Equals(name?.Trim(), Storniert, StringComparison.OrdinalIgnoreCase))
        {
            return "abgelehnte und zurückgezogene Transfers";
        }
        if (string.Equals(name?.Trim(), Korrektur, StringComparison.OrdinalIgnoreCase))
        {
            return "Korrekturbuchungen im Lagerbestand";
        }
        return null;
    }
}
