using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Lagerverwaltungsapp_MatthiasUtrata.Models
{
    // Wareneingang (MeinRaum/Wareneingang): Gegenstände kommen von außen in den eigenen Raum
    public class WareneingangViewModel
    {
        [Required(ErrorMessage = "Bitte einen Gegenstand auswählen.")]
        [Display(Name = "Gegenstand")]
        public int? GegenstandID { get; set; }

        // Bei einem Gerät mit Seriennummer immer 1, das prüft der LagerService
        [Required(ErrorMessage = "Bitte eine Menge eingeben.")]
        [Range(1, int.MaxValue, ErrorMessage = "Die Menge muss mindestens 1 sein.")]
        [Display(Name = "Menge")]
        public int? Menge { get; set; } = 1;

        [Required(ErrorMessage = "Bitte eine Bewegungsart auswählen.")]
        [Display(Name = "Bewegungsart")]
        public int? BewegungsartID { get; set; }

        // Ab hier nur für die Anzeige: Diese Werte kommen nicht aus dem Formular, daher nicht validieren

        // Der eigene Raum (mit Raumart), in den eingebucht wird
        [ValidateNever]
        public Raum Raum { get; set; } = null!;

        // Gegenstände, die eingebucht werden können: alle ohne Seriennummer und die Geräte mit Seriennummer,
        // die weder in einem Raum liegen noch unterwegs sind
        [ValidateNever]
        public List<Gegenstand> Gegenstaende { get; set; } = new List<Gegenstand>();

        [ValidateNever]
        public SelectList Bewegungsarten { get; set; } = null!;
    }
}
