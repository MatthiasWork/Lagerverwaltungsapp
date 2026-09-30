namespace Lagerverwaltungsapp_MatthiasUtrata.Models;

// Ergänzung zur generierten Klasse Person, damit sie beim erneuten Generieren nicht überschrieben wird
public partial class Person
{
    /// <summary>
    /// Anfangsbuchstaben von Vor- und Nachname, z. B. für den runden Avatar.
    /// </summary>
    public string Initialen => (Vorname.Length > 0 ? Vorname[..1] : "") + (Nachname.Length > 0 ? Nachname[..1] : "");
}
