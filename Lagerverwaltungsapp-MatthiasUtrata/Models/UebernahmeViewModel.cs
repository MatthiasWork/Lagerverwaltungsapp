namespace Lagerverwaltungsapp_MatthiasUtrata.Models
{
    public class UebernahmeViewModel
    {
        // Der Raum, für den die angemeldete Person verantwortlich ist
        public Raum? Raum { get; set; }

        // Offene Lagerbewegungen in den Raum, die die Person bestätigen muss
        public List<Lagerbewegung> Offen { get; set; } = new List<Lagerbewegung>();

        // Die zuletzt bestätigten Lagerbewegungen in den Raum
        public List<Lagerbewegung> Erledigt { get; set; } = new List<Lagerbewegung>();
    }
}
