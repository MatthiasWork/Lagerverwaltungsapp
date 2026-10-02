namespace Lagerverwaltungsapp_MatthiasUtrata.Models
{
    // Katalog (Gegenstand/Index)
    public class GegenstandUebersichtViewModel
    {
        // Mögliche Status eines Gegenstands im Katalog
        public const string Verfuegbar = "Verfügbar";
        public const string InAusleihe = "In Ausleihe";
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

        /// <summary>
        /// Methode, die für einen Gegenstand Standort, Stückzahl und Status berechnet.
        /// Dafür müssen Raumbestand (mit Raum und Raumart) und Lagerbewegung geladen sein; gezählt wird nur der geladene Raumbestand.
        /// </summary>
        /// <param name="gegenstand">Der Gegenstand mit Raumbestand und Lagerbewegungen</param>
        /// <param name="raumID">Die ID des Raums, der bei den Standorten vorne stehen soll (Filter im Katalog)</param>
        /// <returns>Der Eintrag mit Standorten, Stückzahl, Status und Hinweis</returns>
        public static KatalogEintrag Erstellen(Gegenstand gegenstand, string? raumID = null)
        {
            var eintrag = new KatalogEintrag
            {
                Gegenstand = gegenstand,
                // Ist nach einem Raum gefiltert, steht dieser Raum vorne, sonst der mit der größten Menge
                Standorte = gegenstand.Raumbestand.Where(r => r.Menge > 0)
                    .OrderByDescending(r => r.RaumID == raumID)
                    .ThenByDescending(r => r.Menge)
                    .ToList()
            };
            eintrag.Stueck = eintrag.Standorte.Sum(r => r.Menge);

            // Unterwegs ist, was schon abgebucht, aber weder im Zielraum übernommen noch storniert wurde
            var offen = gegenstand.Lagerbewegung.Where(l => l.Offen).ToList();
            var unterwegs = offen.Sum(l => l.Menge);
            if (unterwegs > 0)
            {
                var ziele = string.Join(", ", offen.Select(l => l.NachRaumID).Distinct());
                if (gegenstand.Seriennummer != null)
                {
                    eintrag.Hinweis = $"unterwegs nach {ziele}";
                }
                else
                {
                    eintrag.Hinweis = $"{unterwegs} Stück unterwegs nach {ziele}";
                }
            }

            if (eintrag.Stueck > 0)
            {
                eintrag.Status = GegenstandUebersichtViewModel.Verfuegbar;
            }
            else if (unterwegs > 0)
            {
                eintrag.Status = GegenstandUebersichtViewModel.InAusleihe;
            }
            else
            {
                eintrag.Status = GegenstandUebersichtViewModel.KeinBestand;
            }
            return eintrag;
        }
    }

    // Liste der Geräte wie im Katalog (Partial _KatalogListe), im Katalog selbst und zur Auswahl bei der Neuen Ausleihe
    public class KatalogListeViewModel
    {
        public List<KatalogEintrag> Eintraege { get; set; } = new List<KatalogEintrag>();

        // false: ein Klick öffnet das Gerät (Katalog). true: ein Klick wählt das Gerät aus bzw. wieder ab, das Skript der Seite
        // reagiert auf data-auswahl. Die Spalte "Standort" entfällt dann, da alle Geräte im selben Raum liegen
        public bool Auswahl { get; set; }
    }
}
