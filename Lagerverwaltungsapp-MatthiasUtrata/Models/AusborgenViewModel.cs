using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Lagerverwaltungsapp_MatthiasUtrata.Models
{
    public class AusborgenViewModel
    {
        // Der Raum, an den ausgeborgt wird. Die dort verantwortliche Person muss die Übernahme bestätigen
        [Required(ErrorMessage = "Bitte einen Raum auswählen.")]
        [Display(Name = "Ausborgen an Raum")]
        public string? NachRaumID { get; set; }

        [Required(ErrorMessage = "Bitte eine Bewegungsart auswählen.")]
        [Display(Name = "Bewegungsart")]
        public int? BewegungsartID { get; set; }

        // Eingegebene Mengen je Gegenstand (Schlüssel = GegenstandID), im Formular als Mengen[GegenstandID].
        // Ein Gerät mit Seriennummer schickt über die Checkbox 1, ein leeres Feld oder 0 heißt "nicht ausborgen"
        public Dictionary<int, int?> Mengen { get; set; } = new Dictionary<int, int?>();

        // Ab hier nur für die Anzeige: Diese Werte kommen nicht aus dem Formular, daher nicht validieren

        // Der eigene Raum (mit Raumart und verantwortlicher Person), aus dem ausgeborgt wird
        [ValidateNever]
        public Raum VonRaum { get; set; } = null!;

        // Bestand des eigenen Raums, also alles, was ausgeborgt werden kann
        [ValidateNever]
        public List<Raumbestand> Bestand { get; set; } = new List<Raumbestand>();

        // Mögliche Zielräume mit Raumart und verantwortlicher Person (für die Auswahl und die Zusammenfassung)
        [ValidateNever]
        public List<Raum> Raeume { get; set; } = new List<Raum>();

        [ValidateNever]
        public SelectList Bewegungsarten { get; set; } = null!;
    }
}
