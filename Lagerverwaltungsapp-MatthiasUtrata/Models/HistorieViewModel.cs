namespace Lagerverwaltungsapp_MatthiasUtrata.Models
{
    // Historie (Lagerbewegung/Index)
    public class HistorieViewModel
    {
        // Mögliche Status einer Lagerbewegung, für den Filter "Status" und die Tags
        public const string FreigabeOffen = "Freigabe offen";
        public const string Bestaetigt = "Bestätigt";
        public const string Storniert = "Storniert";

        // So viele Lagerbewegungen stehen auf einer Seite
        public const int EintraegeJeSeite = 50;

        // Lagerbewegungen der aktuellen Seite, die neueste zuerst
        public List<Lagerbewegung> Lagerbewegungen { get; set; } = new List<Lagerbewegung>();

        // Anzahl aller Lagerbewegungen für die Zeile unter der Überschrift und der gefilterten für die Zeile unter der Tabelle
        public int AnzahlGesamt { get; set; }

        public int AnzahlGefiltert { get; set; }

        // Aktuelle Seite (ab 1) und Anzahl der Seiten für das Blättern
        public int Seite { get; set; }

        public int AnzahlSeiten { get; set; }

        // Aktuelle Filterwerte, damit sie im Formular und in den Links erhalten bleiben
        public string? Suche { get; set; }

        public string? RaumID { get; set; }

        public int? BewegungsartID { get; set; }

        public string? Status { get; set; }

        public DateTime? Von { get; set; }

        public DateTime? Bis { get; set; }

        // Gibt an, ob nach irgendetwas gefiltert wird (dann gibt es "Zurücksetzen")
        public bool Gefiltert => !string.IsNullOrEmpty(Suche) || !string.IsNullOrEmpty(RaumID) || BewegungsartID != null
            || !string.IsNullOrEmpty(Status) || Von != null || Bis != null;

        // IDs aller Räume für den Filter "Raum"
        public List<string> Raeume { get; set; } = new List<string>();

        // Alle Bewegungsarten nach Name für den Filter "Bewegungsart", auch "Storniert" und "Korrektur"
        public List<Bewegungsart> Bewegungsarten { get; set; } = new List<Bewegungsart>();

        /// <summary>
        /// Methode, die den Status einer Lagerbewegung für die Anzeige liefert. Die Bewegungsart muss geladen sein,
        /// da eine stornierte Lagerbewegung wie eine bestätigte einen Zeitpunkt in BestaetigtAm hat.
        /// </summary>
        /// <param name="lagerbewegung">Die Lagerbewegung mit Bewegungsart</param>
        /// <returns>FreigabeOffen, Bestaetigt oder Storniert</returns>
        public static string StatusVon(Lagerbewegung lagerbewegung)
        {
            if (lagerbewegung.Offen)
            {
                return FreigabeOffen;
            }
            if (lagerbewegung.Storniert)
            {
                return Storniert;
            }
            return Bestaetigt;
        }
    }
}
