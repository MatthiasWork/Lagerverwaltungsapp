using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Lagerverwaltungsapp_MatthiasUtrata.Models
{
    // Aus dem Lager holen (Lager/Index): Geräte aus einem Lager in den eigenen Raum holen
    public class HolenViewModel
    {
        // Das Lager, aus dem geholt wird. Die dort verantwortliche Person muss freigeben
        [Required(ErrorMessage = "Bitte ein Lager auswählen.")]
        [Display(Name = "Lager")]
        public string? VonRaumID { get; set; }

        [Required(ErrorMessage = "Bitte eine Bewegungsart auswählen.")]
        [Display(Name = "Bewegungsart")]
        public int? BewegungsartID { get; set; }

        // Eingegebene Mengen je Gegenstand (Schlüssel = GegenstandID), im Formular als Mengen[GegenstandID].
        // Ein Gerät mit Seriennummer schickt 1, ein leeres Feld oder 0 heißt "nicht holen"
        public Dictionary<int, int?> Mengen { get; set; } = new Dictionary<int, int?>();

        // Ab hier nur für die Anzeige: Diese Werte kommen nicht aus dem Formular, daher nicht validieren

        // Der eigene Raum (mit Raumart), in den geholt wird
        [ValidateNever]
        public Raum NachRaum { get; set; } = null!;

        // Alle Lager, aus denen geholt werden kann (mit Raumart und verantwortlicher Person)
        [ValidateNever]
        public List<Raum> Lager { get; set; } = new List<Raum>();

        // Das gewählte Lager oder null, wenn es keines gibt
        [ValidateNever]
        public Raum? VonRaum { get; set; }

        // Bestand des gewählten Lagers, also alles, was geholt werden kann
        [ValidateNever]
        public List<Raumbestand> Bestand { get; set; } = new List<Raumbestand>();

        [ValidateNever]
        public SelectList Bewegungsarten { get; set; } = null!;
    }
}
