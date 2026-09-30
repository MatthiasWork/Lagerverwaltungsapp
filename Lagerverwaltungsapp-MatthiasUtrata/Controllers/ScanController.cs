using Microsoft.AspNetCore.Mvc;

namespace Lagerverwaltungsapp_MatthiasUtrata.Controllers
{
    // QR-Codes und das Scannen mit Kamera oder Handscanner gibt es noch nicht.
    // Bis dahin ist "Scannen" eine statische Seite mit den Beispieldaten aus dem Design
    public class ScanController : Controller
    {
        /// <summary>
        /// Methode, die die Seite zum Scannen anzeigt (statisch, ohne Funktion).
        /// </summary>
        /// <returns>Gibt ein IActionResult zurück</returns>
        // GET: Scan
        public IActionResult Index()
        {
            return View();
        }
    }
}
