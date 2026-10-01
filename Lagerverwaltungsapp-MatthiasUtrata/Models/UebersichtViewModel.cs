namespace Lagerverwaltungsapp_MatthiasUtrata.Models
{
    public class UebersichtViewModel
    {
        // Stück, die gerade in einem Raum liegen
        public int Verfuegbar { get; set; }

        // Alle Stück, also auch die, die gerade unterwegs sind (Übernahme noch nicht bestätigt)
        public int Gesamt { get; set; }

        // Bewegungen, die noch auf die Übernahme warten: für Admins alle, sonst nur die in den eigenen Raum
        public int OffeneFreigaben { get; set; }

        // Die letzten Buchungen, neueste zuerst
        public List<Aktivitaet> Aktivitaeten { get; set; } = new List<Aktivitaet>();

        // Statistik "Bestand nach Kategorie" (Balken): verfügbare Stück je Kategorie, die größte zuerst (nur Kategorien mit Bestand)
        public List<KategorieStatistik> BestandJeKategorie { get; set; } = new List<KategorieStatistik>();

        // Statistik "Bestand nach Raum" (Säulen): verfügbare Stück je Raum, der größte zuerst (nur Räume mit Bestand)
        public List<RaumStatistik> BestandJeRaum { get; set; } = new List<RaumStatistik>();
    }

    // Ein Balken der Statistik "Bestand nach Kategorie" in der Übersicht
    public class KategorieStatistik
    {
        public int KategorieID { get; set; }

        public string Name { get; set; } = null!;

        // Summe der Mengen im Raumbestand, also wie bei "Verfügbar" ohne das, was gerade unterwegs ist
        public int Stueck { get; set; }
    }

    // Eine Säule der Statistik "Bestand nach Raum" in der Übersicht
    public class RaumStatistik
    {
        public string RaumID { get; set; } = null!;

        // Name der Raumart, z. B. "Labor" (für den Hinweis beim Darüberfahren)
        public string Raumart { get; set; } = null!;

        // Summe der Mengen im Raumbestand des Raums
        public int Stueck { get; set; }
    }

    // Ein Eintrag in der Liste "Aktivität" der Übersicht
    public class Aktivitaet
    {
        public DateTime Zeitpunkt { get; set; }

        public string Text { get; set; } = null!;
    }
}
