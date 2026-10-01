using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Lagerverwaltungsapp_MatthiasUtrata.Models
{
    // Lagerbestand (Raumbestand/Index): Ein Admin wählt einen Raum, sieht dessen Bestand und korrigiert ihn (z. B. nach einer Inventur)
    public class KorrekturViewModel
    {
        // Der Raum, dessen Bestand angezeigt und korrigiert wird
        [Required(ErrorMessage = "Bitte einen Raum auswählen.")]
        [Display(Name = "Raum")]
        public string? RaumID { get; set; }

        // Neue Mengen je Gegenstand (Schlüssel = GegenstandID), im Formular als Mengen[GegenstandID].
        // Bei einem Gerät mit Seriennummer 1 (im Raum) oder 0 (ausbuchen)
        public Dictionary<int, int?> Mengen { get; set; } = new Dictionary<int, int?>();

        // Die Mengen, die beim Anzeigen im Raum waren, im Formular als Bisher[GegenstandID]. Daran erkennt der LagerService,
        // ob sich der Bestand inzwischen geändert hat
        public Dictionary<int, int> Bisher { get; set; } = new Dictionary<int, int>();

        // Ein Gegenstand, der noch nicht im Raum ist, mit seiner Menge (optional)
        [Display(Name = "Gegenstand hinzufügen")]
        public int? NeuGegenstandID { get; set; }

        [Display(Name = "Menge")]
        public int? NeuMenge { get; set; }

        // Ab hier nur für die Anzeige: Diese Werte kommen nicht aus dem Formular, daher nicht validieren

        // Alle Räume zur Auswahl (mit Raumart)
        [ValidateNever]
        public List<Raum> Raeume { get; set; } = new List<Raum>();

        // Der gewählte Raum oder null, wenn noch keiner gewählt ist
        [ValidateNever]
        public Raum? Raum { get; set; }

        // Bestand des gewählten Raums
        [ValidateNever]
        public List<Raumbestand> Bestand { get; set; } = new List<Raumbestand>();

        // Gegenstände, die noch nicht im Raum sind und hinzugefügt werden können
        [ValidateNever]
        public List<Gegenstand> Hinzufuegbar { get; set; } = new List<Gegenstand>();
    }
}
