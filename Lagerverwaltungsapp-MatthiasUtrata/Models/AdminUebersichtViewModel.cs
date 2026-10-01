namespace Lagerverwaltungsapp_MatthiasUtrata.Models
{
    // Benutzer (Admin/Index)
    public class AdminUebersichtViewModel
    {
        // Anzahl aller Benutzer und je Rolle (Schlüssel = RolleID) für die Auswahlliste "Rolle: alle · 48", "Rolle: Lehrkräfte · 41" ...
        public int AnzahlBenutzer { get; set; }

        public Dictionary<int, int> AnzahlJeRolle { get; set; } = new Dictionary<int, int>();

        public List<Rolle> Rollen { get; set; } = new List<Rolle>();

        // Gefilterte Benutzerliste mit Rolle und den Räumen, für die sie verantwortlich sind
        public List<Person> Benutzer { get; set; } = new List<Person>();

        // Noch nicht übernommene Transfers je Person (Schlüssel = PersonID), die sie angelegt hat
        public Dictionary<int, int> OffeneTransfers { get; set; } = new Dictionary<int, int>();

        // Der Benutzer, der rechts im Detail angezeigt wird
        public Person? Ausgewaehlt { get; set; }

        // Aktuelle Filterwerte, damit sie im Formular und in den Links erhalten bleiben
        public string? Suche { get; set; }

        public int? RolleID { get; set; }

        // Damit das eigene Konto nicht löschbar angezeigt wird
        public int? AngemeldeteBenutzerID { get; set; }
    }
}
