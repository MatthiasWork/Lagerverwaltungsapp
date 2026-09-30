namespace Lagerverwaltungsapp_MatthiasUtrata.Models
{
    // Gerätedetail (Gegenstand/Details)
    public class GegenstandDetailsViewModel
    {
        // Der Gegenstand mit Standorten, Stückzahl und Status wie im Katalog
        public KatalogEintrag Eintrag { get; set; } = null!;

        // Bewegungen, die noch auf die Übernahme im Zielraum warten, älteste zuerst
        public List<Lagerbewegung> Unterwegs { get; set; } = new List<Lagerbewegung>();

        // Raum, für den die angemeldete Person verantwortlich ist (null = keiner); nur von dort aus kann sie einen Transfer anlegen
        public string? EigenerRaumID { get; set; }

        // Alle Buchungen des Gegenstands als Verlauf, neueste zuerst
        public List<Aktivitaet> Verlauf { get; set; } = new List<Aktivitaet>();
    }
}
