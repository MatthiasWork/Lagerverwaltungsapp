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
    }

    // Ein Eintrag in der Liste "Aktivität" der Übersicht
    public class Aktivitaet
    {
        public DateTime Zeitpunkt { get; set; }

        public string Text { get; set; } = null!;
    }
}
