using Microsoft.AspNetCore.Mvc.Rendering;

namespace Lagerverwaltungsapp_MatthiasUtrata.Models
{
    public class GegenstandUebersichtViewModel
    {
        // Gefilterte Liste der Gegenstände
        public List<Gegenstand> Gegenstaende { get; set; } = new List<Gegenstand>();

        // Anzahl aller Gegenstände, damit angezeigt werden kann, wie viele der Filter ausblendet
        public int AnzahlGesamt { get; set; }

        // Aktuelle Filterwerte, damit sie im Formular erhalten bleiben
        public string? Suche { get; set; }

        public int? KategorieID { get; set; }

        public int? HerstellerID { get; set; }

        // null = alle, true = nur mit Seriennummer (einzelne Geräte), false = nur ohne Seriennummer (Artikel mit Menge)
        public bool? MitSeriennummer { get; set; }

        public SelectList Kategorien { get; set; } = null!;

        public SelectList Hersteller { get; set; } = null!;
    }
}
