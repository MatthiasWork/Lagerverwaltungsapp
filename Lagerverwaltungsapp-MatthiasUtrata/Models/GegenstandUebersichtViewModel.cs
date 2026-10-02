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

        public int? HerstellerID { get; set; }

        public string? RaumID { get; set; }

        public string? Status { get; set; }

        // Alle Kategorien nach Name für den Filter "Kategorie"
        public List<Kategorie> Kategorien { get; set; } = new List<Kategorie>();

        // Alle Hersteller nach Name für den Filter "Hersteller"; nur geladen, wenn nach einem Hersteller gefiltert wird
        public List<Hersteller> Hersteller { get; set; } = new List<Hersteller>();

        // IDs aller Räume für den Filter "Raum"
        public List<string> Raeume { get; set; } = new List<string>();
    }

    // Eine Zeile im Katalog: der Gegenstand mit seinem Standort und Status
    public class KatalogEintrag
    {
        public Gegenstand Gegenstand { get; set; } = null!;

        // Räume, in denen der Gegenstand liegt; der gefilterte Raum bzw. der mit der größten Menge zuerst
        public List<Raumbestand> Standorte { get; set; } = new List<Raumbestand>();

        // Der erste Raum aus Standorte oder null, wenn der Gegenstand in keinem Raum liegt
        public Raum? ErsterRaum => Standorte.FirstOrDefault()?.Raum;

        // Standort für die Anzeige: der erste Raum mit Raumart, bei mehreren Räumen mit der Zahl der übrigen,
        // z. B. "309 · Klassenraum +2". Null, wenn der Gegenstand in keinem Raum liegt
        public string? Standort
        {
            get
            {
                if (ErsterRaum == null)
                {
                    return null;
                }

                var text = $"{ErsterRaum.ID} · {ErsterRaum.Raumart.Name}";
                if (Standorte.Count > 1)
                {
                    text += $" +{Standorte.Count - 1}";
                }
                return text;
            }
        }

        // Stück in allen Räumen zusammen
        public int Stueck { get; set; }

        public string Status { get; set; } = null!;

        // Zusatz in der letzten Spalte, z. B. wohin der Gegenstand gerade unterwegs ist
        public string? Hinweis { get; set; }
    }
}
