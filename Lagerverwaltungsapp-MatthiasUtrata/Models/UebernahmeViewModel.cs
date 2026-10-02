namespace Lagerverwaltungsapp_MatthiasUtrata.Models
{
    public class UebernahmeViewModel
    {
        // Der Raum, für den die angemeldete Person verantwortlich ist
        public Raum? Raum { get; set; }

        // Offene Lagerbewegungen, die die Person freigeben muss (Ausleihen in den Raum, aus ihm geholte Geräte)
        public List<Lagerbewegung> Offen { get; set; } = new List<Lagerbewegung>();

        // Die zuletzt erledigten (bestätigten oder stornierten) Lagerbewegungen, die die Person freigeben musste
        public List<Lagerbewegung> Erledigt { get; set; } = new List<Lagerbewegung>();
    }
}
