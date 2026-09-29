namespace Lagerverwaltungsapp_MatthiasUtrata.Models
{
    public class MenueViewModel
    {
        // Rollenbasiert: Stammdaten, Übersichten und Benutzerverwaltung
        public bool IstAdmin { get; set; }

        // Zuständigkeitsbasiert: ID des Raums, für den die angemeldete Person verantwortlich ist (null = kein Raum).
        // Die Raumart spielt dafür keine Rolle
        public string? RaumID { get; set; }

        // Zahl für das Badge neben "Offene Übernahmen"
        public int OffeneUebernahmen { get; set; }
    }
}
