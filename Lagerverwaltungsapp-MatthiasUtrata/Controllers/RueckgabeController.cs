using Microsoft.AspNetCore.Mvc;

namespace Lagerverwaltungsapp_MatthiasUtrata.Controllers
{
    // Rückgabefristen, Erinnerungen und Eskalation gibt es im Datenmodell noch nicht.
    // Bis dahin ist "Rückgaben" eine statische Seite mit den Beispieldaten aus dem Design (Models/Beispieldaten.cs)
    public class RueckgabeController : Controller
    {
        /// <summary>
        /// Methode, die die überfälligen und bald fälligen Ausleihen anzeigt (statisch, ohne Funktion).
        /// </summary>
        /// <returns>Gibt ein IActionResult zurück</returns>
        // GET: Rueckgabe
        public IActionResult Index()
        {
            return View();
        }
    }
}
