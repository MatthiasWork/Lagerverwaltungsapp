namespace Lagerverwaltungsapp_MatthiasUtrata.Models
{
    public static class StatusTag
    {
        /// <summary>
        /// Methode, die zu einem Status aus dem Design die CSS-Klasse des Tags liefert (siehe .tag-* in site.css).
        /// </summary>
        /// <param name="status">Der angezeigte Status, z. B. "Verfügbar" oder "Überfällig"</param>
        /// <returns>Die CSS-Klasse für die Farbe des Tags</returns>
        public static string Klasse(string status)
        {
            return status switch
            {
                "Verfügbar" or "Erinnert" => "tag-accent",
                "Überfällig" or "Eskaliert" => "tag-dark",
                "Freigabe offen" => "tag-outline",
                "In Transfer" => "tag-outline-neutral",
                _ => "tag-neutral"
            };
        }
    }
}
