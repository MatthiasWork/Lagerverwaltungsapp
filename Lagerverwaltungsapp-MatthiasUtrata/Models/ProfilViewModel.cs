namespace Lagerverwaltungsapp_MatthiasUtrata.Models
{
    // Profil (eigenes oder, für Admins, das einer anderen Person)
    public class ProfilViewModel
    {
        // Die Person mit Rolle und den Räumen (mit Raumart), für die sie verantwortlich ist
        public Person Person { get; set; } = null!;

        // true, wenn die angemeldete Person ihr eigenes Profil ansieht (dann mobil mit weiteren Bereichen und Abmelden)
        public bool IstEigenesProfil { get; set; }

        // Stück im Bestand je Raum, für den die Person verantwortlich ist (Schlüssel = RaumID)
        public Dictionary<string, int> StueckJeRaum { get; set; } = new Dictionary<string, int>();

        // Ausleihen, die die Person angelegt hat und die noch auf die Übernahme warten ("Meine Anfragen")
        public List<Lagerbewegung> OffeneAusleihen { get; set; } = new List<Lagerbewegung>();

        // Die letzten Buchungen der Person und Übernahmen in ihre Räume, neueste zuerst
        public List<Aktivitaet> Verlauf { get; set; } = new List<Aktivitaet>();
    }
}
