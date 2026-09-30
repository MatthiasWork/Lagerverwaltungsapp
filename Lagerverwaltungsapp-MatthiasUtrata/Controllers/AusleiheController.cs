using Microsoft.AspNetCore.Mvc;

namespace Lagerverwaltungsapp_MatthiasUtrata.Controllers
{
    // Verleih an Personen mit Zeitraum und Freigabe gibt es im Datenmodell noch nicht.
    // Bis dahin ist "Neue Ausleihe" eine statische Seite mit den Beispieldaten aus dem Design
    public class AusleiheController : Controller
    {
        /// <summary>
        /// Methode, die die Seite "Neue Ausleihe" anzeigt (statisch, ohne Funktion).
        /// </summary>
        /// <returns>Gibt ein IActionResult zurück</returns>
        // GET: Ausleihe
        public IActionResult Index()
        {
            return View();
        }
    }
}
