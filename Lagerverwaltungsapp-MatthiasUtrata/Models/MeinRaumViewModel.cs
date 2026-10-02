using Microsoft.AspNetCore.Mvc.Rendering;

namespace Lagerverwaltungsapp_MatthiasUtrata.Models
{
    public class MeinRaumViewModel
    {
        // Der Raum, für den die angemeldete Person verantwortlich ist (mit Raumart)
        public Raum Raum { get; set; } = null!;

        // Gefilterter Bestand des Raums
        public List<Raumbestand> Bestand { get; set; } = new List<Raumbestand>();

        // Anzahl aller Bestandszeilen, damit angezeigt werden kann, wie viele der Filter ausblendet
        public int AnzahlGesamt { get; set; }

        // Summe aller Mengen im Raum (für die Kennzahl oben)
        public int StueckGesamt { get; set; }

        // Für den Raum angefragte Lagerbewegungen (Ausleihen aus dem Raum, aus einem Lager geholte Geräte), die schon
        // abgebucht, aber noch nicht freigegeben sind. Die Person kann sie zurückziehen
        public List<Lagerbewegung> Unterwegs { get; set; } = new List<Lagerbewegung>();

        // Offene Bewegungen, die die Person freigeben muss: Ausleihen in den eigenen Raum und aus ihm geholte Geräte
        public int OffeneUebernahmen { get; set; }

        // Aktuelle Filterwerte, damit sie im Formular erhalten bleiben
        public string? Suche { get; set; }

        public int? KategorieID { get; set; }

        public SelectList Kategorien { get; set; } = null!;
    }
}
