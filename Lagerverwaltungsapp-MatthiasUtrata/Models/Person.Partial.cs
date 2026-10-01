namespace Lagerverwaltungsapp_MatthiasUtrata.Models;

// Ergänzung zur generierten Klasse Person, damit sie beim erneuten Generieren nicht überschrieben wird
public partial class Person
{
    /// <summary>
    /// Vor- und Nachname, z. B. "Lena Brandt". Nur für geladene Personen, in Abfragen Vorname und Nachname verwenden.
    /// </summary>
    public string VollerName => $"{Vorname} {Nachname}";

    /// <summary>
    /// Anfangsbuchstaben von Vor- und Nachname, z. B. für den runden Avatar.
    /// </summary>
    public string Initialen => InitialenAus(Vorname, Nachname);

    /// <summary>
    /// Methode, die die Anfangsbuchstaben von Vor- und Nachname liefert, z. B. "LB" für Lena Brandt.
    /// Auch für das Menü, das nur die Namen aus dem Login hat und keine Person.
    /// </summary>
    /// <param name="vorname">Der Vorname, darf leer sein</param>
    /// <param name="nachname">Der Nachname, darf leer sein</param>
    /// <returns>Die Anfangsbuchstaben; bei einem leeren Namen fehlt sein Buchstabe</returns>
    public static string InitialenAus(string vorname, string nachname)
    {
        return ErsterBuchstabe(vorname) + ErsterBuchstabe(nachname);
    }

    /// <summary>
    /// Methode, die den ersten Buchstaben eines Textes liefert.
    /// </summary>
    /// <param name="text">Der Text, darf leer sein</param>
    /// <returns>Der erste Buchstabe oder ein leerer Text, wenn der Text leer ist</returns>
    private static string ErsterBuchstabe(string text)
    {
        if (text.Length == 0)
        {
            return "";
        }
        return text[..1];
    }
}
