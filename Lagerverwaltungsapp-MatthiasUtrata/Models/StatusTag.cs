namespace Lagerverwaltungsapp_MatthiasUtrata.Models
{
    public static class StatusTag
    {
        /// <summary>
        /// Methode, die zu einem Status aus dem Design die CSS-Klasse des Tags liefert (siehe .tag-* in site.css).
        /// </summary>
        /// <param name="status">Der angezeigte Status, z. B. "Verfügbar" oder "In Ausleihe"</param>
        /// <returns>Die CSS-Klasse für die Farbe des Tags</returns>
        public static string Klasse(string status)
        {
            return status switch
            {
                "Verfügbar" or "Bestätigt" => "tag-accent",
                "Freigabe offen" => "tag-outline",
                "In Ausleihe" => "tag-outline-neutral",
                _ => "tag-neutral"
            };
        }
    }
}
