using Microsoft.AspNetCore.Mvc;

namespace Lagerverwaltungsapp_MatthiasUtrata.Controllers
{
    // Rückgabefristen, Erinnerungen und Eskalation gibt es im Datenmodell noch nicht.
    // Bis dahin zeigt "Rückgaben" nur einen Hinweis darauf
    public class RueckgabeController : Controller
    {
        /// <summary>
        /// Methode, die die Seite "Rückgaben" anzeigt (nur ein Hinweis, da es Rückgabefristen noch nicht gibt).
        /// </summary>
        /// <returns>Gibt ein IActionResult zurück</returns>
        // GET: Rueckgabe
        public IActionResult Index()
        {
            return View();
        }
    }
}
