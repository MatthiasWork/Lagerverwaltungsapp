using Microsoft.AspNetCore.Mvc.Rendering;

namespace Lagerverwaltungsapp_MatthiasUtrata.Models
{
    public class AdminUebersichtViewModel
    {
        // Kennzahlen für die Kacheln oben auf der Seite
        public int AnzahlBenutzer { get; set; }

        public int AnzahlAdmins { get; set; }

        public int AnzahlRollen { get; set; }

        // Gefilterte Benutzerliste für die Benutzerverwaltung
        public List<Person> Benutzer { get; set; } = new List<Person>();

        // Aktuelle Filterwerte, damit sie im Formular erhalten bleiben
        public string? Suche { get; set; }

        public int? RolleID { get; set; }

        public SelectList Rollen { get; set; } = null!;

        // Damit der eigene Eintrag markiert und nicht löschbar angezeigt wird
        public int? AngemeldeteBenutzerID { get; set; }
    }
}
