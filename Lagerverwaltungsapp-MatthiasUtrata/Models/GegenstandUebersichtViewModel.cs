namespace Lagerverwaltungsapp_MatthiasUtrata.Models
{
    // Katalog (Gegenstand/Index)
    public class GegenstandUebersichtViewModel
    {
        // Mögliche Status eines Gegenstands im Katalog
        public const string Verfuegbar = "Verfügbar";
        public const string InTransfer = "In Transfer";
        public const string KeinBestand = "Kein Bestand";

        // Gefilterte Liste der Gegenstände
        public List<KatalogEintrag> Eintraege { get; set; } = new List<KatalogEintrag>();

        // Anzahl aller Gegenstände und Räume für die Zeile unter der Überschrift
        public int AnzahlGesamt { get; set; }

        public int AnzahlRaeume { get; set; }

        // Aktuelle Filterwerte, damit sie im Formular und in den Links erhalten bleiben
        public string? Suche { get; set; }

        public int? KategorieID { get; set; }

        public string? RaumID { get; set; }

        public string? Status { get; set; }

        // Kategorien nach Anzahl der Gegenstände, die größten zuerst (die ersten stehen direkt als Schalter da)
        public List<Kategorie> Kategorien { get; set; } = new List<Kategorie>();

        // IDs aller Räume für den Filter "Raum"
        public List<string> Raeume { get; set; } = new List<string>();
    }

    // Eine Zeile im Katalog: der Gegenstand mit seinem Standort und Status
    public class KatalogEintrag
    {
        public Gegenstand Gegenstand { get; set; } = null!;

        // Räume, in denen der Gegenstand liegt; der gefilterte Raum bzw. der mit der größten Menge zuerst
        public List<Raumbestand> Standorte { get; set; } = new List<Raumbestand>();

        // Stück in allen Räumen zusammen
        public int Stueck { get; set; }

        public string Status { get; set; } = null!;

        // Zusatz in der letzten Spalte, z. B. wohin der Gegenstand gerade unterwegs ist
        public string? Hinweis { get; set; }
    }
}
