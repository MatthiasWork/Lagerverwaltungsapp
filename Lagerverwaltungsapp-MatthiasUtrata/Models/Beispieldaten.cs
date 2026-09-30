namespace Lagerverwaltungsapp_MatthiasUtrata.Models
{
    // Feste Beispieldaten aus dem Design "Geräteverleih Final" für Funktionen, die es im Datenmodell noch nicht gibt:
    // Verleih an Personen mit Rückgabefrist, Erinnerungen, Eskalation an die Admins, QR-Etiketten und Abgleich mit der Schulverwaltung.
    // Sobald eine dieser Funktionen umgesetzt ist, die zugehörigen Daten hier entfernen und aus der Datenbank lesen
    public static class Beispieldaten
    {
        // Kennzahlen der Übersicht
        public const int Verliehen = 87;
        public const int HeuteFaellig = 5;
        public const int Ueberfaellig = 6;
        public const int Eskaliert = 2;

        // Rückgaben: Anzahl der Ausleihen, die diese Woche fällig werden, und die Regeln für Erinnerung und Eskalation
        public const int DieseWocheFaellig = 23;
        public const int ErinnerungTageVorher = 1;
        public const int EskalationNachTagen = 5;

        // Gerätedetail: QR-Etiketten gibt es noch nicht
        public const string EtikettGedrucktAm = "12.08.2025";

        // Benutzer: einen Abgleich mit der Schulverwaltung gibt es noch nicht
        public const string SynchronisiertAm = "heute 06:00";

        // Überfällige und bald fällige Ausleihen (Übersicht, Rückgaben)
        public static readonly IReadOnlyList<FaelligeAusleihe> FaelligeAusleihen = new List<FaelligeAusleihe>
        {
            new("Epson EB-W49 Beamer", "BSV-BEA-0007", "Petra Schneider", "24.09.", "6 Tage", "Eskaliert"),
            new("iPad 10. Gen #09", "BSV-TAB-0098", "Jonas Weber", "27.09.", "3 Tage", "Überfällig"),
            new("MacBook Air M2", "BSV-LAP-0044", "Aylin Öztürk", "29.09.", "1 Tag", "Überfällig"),
            new("iPad 10. Gen #14", "BSV-TAB-0142", "Murat Yilmaz", "morgen", null, "Erinnert"),
            new("Rode Mikrofon-Set", "BSV-AUD-0005", "Karin Vogel", "morgen", null, "Erinnert")
        };

        // Aktuelle Ausleihen einer Person (Profil)
        public static readonly IReadOnlyList<EigeneAusleihe> EigeneAusleihen = new List<EigeneAusleihe>
        {
            new("iPad 10. Gen #14", "BSV-TAB-0142", "01.10.", "morgen, 01.10.", "Erinnert"),
            new("Dokumentenkamera Elmo", "BSV-DOK-0015", "14.10.", "14.10.", "Verliehen")
        };
    }

    // Eine Ausleihe mit Rückgabefrist; Faellig ist schon so formatiert, wie es angezeigt wird ("24.09.", "morgen")
    public record FaelligeAusleihe(string Geraet, string InventarNr, string Ausleiher, string Faellig, string? Ueberfaellig, string Status);

    // Eine eigene Ausleihe; Bis ist das Rückgabedatum, BisMobil die längere Angabe der Mobilansicht ("morgen, 01.10.")
    public record EigeneAusleihe(string Geraet, string InventarNr, string Bis, string BisMobil, string Status);
}
