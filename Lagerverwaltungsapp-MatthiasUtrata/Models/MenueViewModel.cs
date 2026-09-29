namespace Lagerverwaltungsapp_MatthiasUtrata.Models
{
    public class MenueViewModel
    {
        // Rollenbasiert: Stammdaten, Übersichten und Benutzerverwaltung
        public bool IstAdmin { get; set; }

        // Zuständigkeitsbasiert: für welche Raumarten die angemeldete Person mindestens einen Raum verantwortet
        public bool HauptlagerVerantwortlich { get; set; }

        public bool UmbuchungslagerVerantwortlich { get; set; }

        public bool LaborVerantwortlich { get; set; }

        // Zahlen für die Badges neben den Menüpunkten
        public int OffeneUebernahmen { get; set; }

        public int OffeneRueckgaben { get; set; }
    }
}
